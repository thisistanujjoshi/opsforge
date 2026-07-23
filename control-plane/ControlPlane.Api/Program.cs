using ControlPlane.Api;

var builder = WebApplication.CreateBuilder(args);

// Executor is pluggable: "Helm" shells out to helm; "Stub" (default) is
// deterministic for dev and tests — same pattern as the app services' providers.
if (string.Equals(builder.Configuration["ControlPlane:Executor"], "Helm", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IDeploymentExecutor, HelmDeploymentExecutor>();
else
    builder.Services.AddSingleton<IDeploymentExecutor, StubDeploymentExecutor>();

builder.Services.AddSingleton<IAuditLog, FileAuditLog>();

var app = builder.Build();

string[] validEnvironments = ["dev", "staging", "prod"];

// API-key auth for every /api route. Keys live in configuration
// ("ControlPlane:ApiKeys": [{ "Name": ..., "Key": ... }]); the matching
// name becomes the audit actor.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var supplied = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        var actor = app.Configuration.GetSection("ControlPlane:ApiKeys").GetChildren()
            .FirstOrDefault(k => k["Key"] == supplied && !string.IsNullOrEmpty(supplied))?["Name"];

        if (actor is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid X-Api-Key." });
            return;
        }

        context.Items["actor"] = actor;
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapGet("/api/v1/releases", async (IDeploymentExecutor executor, CancellationToken ct) =>
{
    var result = await executor.ListReleasesAsync(ct);
    return result.Success ? Results.Content(result.Output, "application/json") : Results.Problem(result.Output);
});

app.MapPost("/api/v1/deploy", async (
    DeployRequest request, IDeploymentExecutor executor, IAuditLog audit, HttpContext http, CancellationToken ct) =>
{
    if (!validEnvironments.Contains(request.Environment))
        return Results.BadRequest(new { error = $"Environment must be one of: {string.Join(", ", validEnvironments)}." });
    if (string.IsNullOrWhiteSpace(request.Release) || string.IsNullOrWhiteSpace(request.Tag))
        return Results.BadRequest(new { error = "Release and Tag are required." });

    var result = await executor.DeployAsync(request, ct);
    audit.Record(new AuditEntry(
        DateTime.UtcNow, (string)http.Items["actor"]!, "deploy",
        request.Release, request.Environment, $"tag={request.Tag}: {result.Output}", result.Success));

    return result.Success ? Results.Ok(result) : Results.Problem(result.Output);
});

app.MapPost("/api/v1/rollback", async (
    RollbackRequest request, IDeploymentExecutor executor, IAuditLog audit, HttpContext http, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Release))
        return Results.BadRequest(new { error = "Release is required." });

    var result = await executor.RollbackAsync(request, ct);
    audit.Record(new AuditEntry(
        DateTime.UtcNow, (string)http.Items["actor"]!, "rollback",
        request.Release, request.Environment, $"revision={request.Revision?.ToString() ?? "previous"}: {result.Output}", result.Success));

    return result.Success ? Results.Ok(result) : Results.Problem(result.Output);
});

app.MapGet("/api/v1/audit", (IAuditLog audit) => Results.Ok(audit.List()));

app.Run();

public partial class Program;
