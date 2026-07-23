using System.Net;
using System.Net.Http.Json;
using ControlPlane.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ControlPlane.Tests;

public class ControlPlaneApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ControlPlaneApiTests(WebApplicationFactory<Program> factory)
    {
        // Stub executor + a per-run audit file so tests never touch helm or shared state.
        var auditPath = Path.Combine(Path.GetTempPath(), $"cp-audit-{Guid.NewGuid()}.jsonl");
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ControlPlane:Executor"] = "Stub",
                ["ControlPlane:AuditPath"] = auditPath,
                ["ControlPlane:ApiKeys:0:Name"] = "test-actor",
                ["ControlPlane:ApiKeys:0:Key"] = "test-key",
            })));
    }

    private HttpClient AuthedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-key");
        return client;
    }

    [Fact]
    public async Task Health_IsAnonymous()
    {
        var response = await _factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Deploy_WithoutApiKey_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/deploy",
            new DeployRequest("nexus", "dev", "1.0.0"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Deploy_WithWrongApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong");
        var response = await client.PostAsJsonAsync("/api/v1/deploy",
            new DeployRequest("nexus", "dev", "1.0.0"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Deploy_InvalidEnvironment_Returns400()
    {
        var response = await AuthedClient().PostAsJsonAsync("/api/v1/deploy",
            new DeployRequest("nexus", "production!!", "1.0.0"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deploy_Valid_SucceedsAndIsAudited()
    {
        var client = AuthedClient();

        var deploy = await client.PostAsJsonAsync("/api/v1/deploy",
            new DeployRequest("nexus", "staging", "1.2.3"));
        Assert.Equal(HttpStatusCode.OK, deploy.StatusCode);

        var audit = await client.GetFromJsonAsync<List<AuditEntry>>("/api/v1/audit");
        var entry = Assert.Single(audit!, e => e.Action == "deploy" && e.Release == "nexus" && e.Environment == "staging");
        Assert.Equal("test-actor", entry.Actor);
        Assert.True(entry.Success);
        Assert.Contains("1.2.3", entry.Detail);
    }

    [Fact]
    public async Task Rollback_Valid_SucceedsAndIsAudited()
    {
        var client = AuthedClient();

        var rollback = await client.PostAsJsonAsync("/api/v1/rollback",
            new RollbackRequest("nexus", "prod", 4));
        Assert.Equal(HttpStatusCode.OK, rollback.StatusCode);

        var audit = await client.GetFromJsonAsync<List<AuditEntry>>("/api/v1/audit");
        var entry = Assert.Single(audit!, e => e.Action == "rollback");
        Assert.Contains("revision=4", entry.Detail);
    }

    [Fact]
    public async Task Rollback_MissingRelease_Returns400()
    {
        var response = await AuthedClient().PostAsJsonAsync("/api/v1/rollback",
            new RollbackRequest("", "dev", null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Releases_WithKey_ReturnsJson()
    {
        var response = await AuthedClient().GetAsync("/api/v1/releases");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }
}
