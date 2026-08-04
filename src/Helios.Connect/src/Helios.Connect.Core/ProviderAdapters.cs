using System.Text.Json;

namespace Helios.Connect.Core;

public sealed class GitHubReadOnlyAdapter(IReadOnlyProcessRunner runner) : IReadOnlyDiscoveryAdapter
{
    public string Provider => "github";
    public async Task<IReadOnlyList<EvidenceItem>> DiscoverAsync(IReadOnlySet<string> approvedScopes, CancellationToken cancellationToken)
    {
        if (!approvedScopes.Contains("github:identity")) return [];
        var raw = await runner.RunAsync("gh", ["api", "user", "--method", "GET", "--header", "X-GitHub-Api-Version: 2022-11-28"], cancellationToken);
        using var json = JsonDocument.Parse(raw);
        var login = json.RootElement.GetProperty("login").GetString() ?? "unavailable";
        return [new("github.identity", EvidenceKind.Observed, login, "gh api user; X-GitHub-Api-Version=2022-11-28")];
    }
}

public sealed class AzureReadOnlyAdapter(IReadOnlyProcessRunner runner) : IReadOnlyDiscoveryAdapter
{
    public string Provider => "azure";
    public async Task<IReadOnlyList<EvidenceItem>> DiscoverAsync(IReadOnlySet<string> approvedScopes, CancellationToken cancellationToken)
    {
        if (!approvedScopes.Contains("azure:identity")) return [];
        var raw = await runner.RunAsync("az", ["account", "show", "--output", "json"], cancellationToken);
        using var json = JsonDocument.Parse(raw);
        var root = json.RootElement;
        var tenant = Get(root, "tenantId");
        var subscription = Get(root, "id");
        var name = Get(root, "name");
        return
        [
            new("azure.tenant", EvidenceKind.Observed, tenant, "az account show"),
            new("azure.subscription", EvidenceKind.Observed, $"{name} ({subscription})", "az account show")
        ];
    }

    private static string Get(JsonElement element, string property) => element.TryGetProperty(property, out var value) ? value.GetString() ?? "unavailable" : "unavailable";
}
