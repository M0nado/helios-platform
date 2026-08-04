using System.Diagnostics;
using System.Security;

namespace Helios.Connect.Core;

/// <summary>Executes only an explicit set of read-only provider commands. No arbitrary command string is accepted.</summary>
public sealed class ReadOnlyProcessRunner : IReadOnlyProcessRunner
{
    private static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["gh"] = ["auth", "api", "repo", "pr", "issue", "project", "workflow", "ruleset", "codespace"],
        ["az"] = ["account", "cloud", "graph", "group", "identity", "role", "acr", "keyvault", "network", "policy", "deployment", "monitor", "resource", "rest"]
    };

    public async Task<string> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        if (!Allowed.TryGetValue(executable, out var verbs) || arguments.Count == 0 || !verbs.Contains(arguments[0], StringComparer.Ordinal))
            throw new SecurityException("Command is outside the read-only provider allowlist.");
        RejectMutationOrSecrets(arguments);

        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"{executable} is unavailable.");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0) throw new InvalidOperationException(Sanitize(error));
        return output;
    }

    private static void RejectMutationOrSecrets(IReadOnlyList<string> arguments)
    {
        var joined = string.Join(' ', arguments).ToLowerInvariant();
        string[] forbidden = [" create", " update", " delete", " set", " apply", " deploy", " assign", " install", " approve", " dispatch", "--method post", "--method put", "--method patch", "--method delete", " secret show", "credential list", "--query value"];
        if (forbidden.Any(term => joined.Contains(term, StringComparison.Ordinal))) throw new SecurityException("Mutation or secret-bearing command rejected.");
        if (arguments[0] == "rest" && !joined.Contains("--method get")) throw new SecurityException("Azure REST discovery must explicitly use GET.");
        if (arguments[0] == "api" && joined.Contains("--method") && !joined.Contains("--method get")) throw new SecurityException("GitHub REST discovery permits GET only.");
    }

    private static string Sanitize(string value) => value.Length > 512 ? value[..512] + "…" : value.Trim();
}
