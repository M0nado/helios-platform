using System.Net.Http.Json;
using System.Text.Json;
using HELIOS.Fabric.Contracts;

namespace HELIOS.Fabric.Worker.Delivery;

/// <summary>
/// Opens or updates governance work in GitHub. It never merges, deploys, grants
/// permissions, or dispatches a privileged workflow. Those effects remain behind
/// protected GitHub environments and their dedicated workflows.
/// </summary>
public sealed class GitHubControlSink(
    IHttpClientFactory clients,
    GitHubAppTokenProvider tokens,
    IConfiguration configuration) : IDeliverySink
{
    public string ConnectorId => "github-control";

    public async Task DeliverAsync(EventEnvelope envelope, DeliveryRoute route, CancellationToken cancellationToken)
    {
        var repository = envelope.Provenance.Repository ?? configuration["HELIOS_GITHUB_DEFAULT_REPOSITORY"]
            ?? throw new InvalidOperationException("A GitHub repository is required for github-control delivery.");
        if (!IsAllowedRepository(repository)) throw new InvalidOperationException($"Repository {repository} is not in the HELIOS GitHub allowlist.");
        var token = await tokens.GetAsync(cancellationToken);
        var client = clients.CreateClient(nameof(GitHubControlSink));
        client.BaseAddress = new Uri("https://api.github.com/");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "helios-enterprise-fabric/2.0");

        var marker = $"HELIOS-EVENT:{envelope.EventId}";
        var existing = await FindIssueAsync(client, repository, marker, cancellationToken);
        if (existing is not null) return;

        var title = envelope.EventType.StartsWith("incident.", StringComparison.Ordinal)
            ? $"[HELIOS] {envelope.Severity} incident — {envelope.Summary}"
            : $"[HELIOS] {envelope.EventType} — {envelope.Summary}";
        var body = $"""
        <!-- {marker} -->
        ## HELIOS automation event

        - Event: `{envelope.EventType}`
        - Event ID: `{envelope.EventId}`
        - Correlation: `{envelope.CorrelationId}`
        - Environment: `{envelope.Environment}`
        - Severity: `{envelope.Severity}`
        - Source SHA: `{envelope.Provenance.CommitSha ?? "n/a"}`
        - Transform: `{route.Transform}`

        {envelope.Summary}

        This issue records and routes work. It does **not** authorize a merge,
        Azure deployment, RBAC mutation, tenant consent, or connector secret access.
        """;
        using var response = await client.PostAsJsonAsync($"repos/{repository}/issues", new
        {
            title,
            body,
            labels = new[] { "helios:fabric", (envelope.Severity is "critical" or "error") ? "helios:incident" : "helios:connector" }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private bool IsAllowedRepository(string repository)
    {
        var configured = configuration["HELIOS_GITHUB_REPOSITORY_ALLOWLIST"]
            ?? "M0nado/helios-platform,M0nado/Helios-Control-Center,Heli0s-Dynamics/helios-platform,Yolkster64/monado-blade";
        return configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(repository, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<int?> FindIssueAsync(HttpClient client, string repository, string marker, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString($"repo:{repository} is:issue in:body \"{marker}\"");
        using var response = await client.GetAsync($"search/issues?q={query}&per_page=1", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var items = document.RootElement.GetProperty("items");
        return items.GetArrayLength() == 0 ? null : items[0].GetProperty("number").GetInt32();
    }
}
