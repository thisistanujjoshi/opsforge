using System.Diagnostics;
using System.Text.Json;

namespace ControlPlane.Api;

public record DeployRequest(string Release, string Environment, string Tag);
public record RollbackRequest(string Release, string Environment, int? Revision);
public record OperationResult(bool Success, string Output);

public record AuditEntry(
    DateTime TimestampUtc,
    string Actor,
    string Action,
    string Release,
    string Environment,
    string Detail,
    bool Success);

public interface IDeploymentExecutor
{
    Task<OperationResult> DeployAsync(DeployRequest request, CancellationToken ct = default);
    Task<OperationResult> RollbackAsync(RollbackRequest request, CancellationToken ct = default);
    Task<OperationResult> ListReleasesAsync(CancellationToken ct = default);
}

/// <summary>Runs real helm commands. Selected with "ControlPlane:Executor": "Helm".</summary>
public class HelmDeploymentExecutor(IConfiguration configuration) : IDeploymentExecutor
{
    private string ChartPath => configuration["ControlPlane:ChartPath"]
        ?? throw new InvalidOperationException("'ControlPlane:ChartPath' is required.");

    public Task<OperationResult> DeployAsync(DeployRequest r, CancellationToken ct = default) =>
        RunAsync($"upgrade --install {r.Release} \"{ChartPath}\" " +
                 $"-f \"{ChartPath}/values-{r.Environment}.yaml\" --set image.tag={r.Tag} --wait --timeout 5m", ct);

    public Task<OperationResult> RollbackAsync(RollbackRequest r, CancellationToken ct = default) =>
        RunAsync($"rollback {r.Release} {r.Revision?.ToString() ?? ""} --wait", ct);

    public Task<OperationResult> ListReleasesAsync(CancellationToken ct = default) =>
        RunAsync("list --output json", ct);

    private static async Task<OperationResult> RunAsync(string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("helm", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start helm.");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return new OperationResult(process.ExitCode == 0, process.ExitCode == 0 ? stdout : stderr);
    }
}

/// <summary>Deterministic executor for dev and tests — records calls, touches nothing.</summary>
public class StubDeploymentExecutor : IDeploymentExecutor
{
    public List<string> Calls { get; } = [];

    public Task<OperationResult> DeployAsync(DeployRequest r, CancellationToken ct = default)
    {
        Calls.Add($"deploy {r.Release} {r.Environment} {r.Tag}");
        return Task.FromResult(new OperationResult(true, $"[stub] deployed {r.Release} tag {r.Tag} to {r.Environment}"));
    }

    public Task<OperationResult> RollbackAsync(RollbackRequest r, CancellationToken ct = default)
    {
        Calls.Add($"rollback {r.Release} {r.Revision}");
        return Task.FromResult(new OperationResult(true, $"[stub] rolled back {r.Release} to revision {r.Revision?.ToString() ?? "previous"}"));
    }

    public Task<OperationResult> ListReleasesAsync(CancellationToken ct = default) =>
        Task.FromResult(new OperationResult(true, "[]"));
}

public interface IAuditLog
{
    void Record(AuditEntry entry);
    IReadOnlyList<AuditEntry> List();
}

/// <summary>Append-only JSON-lines audit trail; every operation lands here, success or not.</summary>
public class FileAuditLog(IConfiguration configuration) : IAuditLog
{
    private readonly string _path = configuration["ControlPlane:AuditPath"] ?? "audit.jsonl";
    private readonly Lock _lock = new();

    public void Record(AuditEntry entry)
    {
        lock (_lock)
        {
            File.AppendAllText(_path, JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
    }

    public IReadOnlyList<AuditEntry> List()
    {
        lock (_lock)
        {
            if (!File.Exists(_path)) return [];
            return File.ReadAllLines(_path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => JsonSerializer.Deserialize<AuditEntry>(l)!)
                .ToList();
        }
    }
}
