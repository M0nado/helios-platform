namespace Helios.Connect.Core;

public sealed class ConnectOrchestrator(IEnumerable<IReadOnlyDiscoveryAdapter> adapters)
{
    public async Task<IReadOnlyList<DiscoveryResult>> DiscoverAsync(ConnectSession session, IReadOnlySet<string> scopes, CancellationToken cancellationToken)
    {
        var results = new List<DiscoveryResult>();
        foreach (var adapter in adapters)
        {
            try
            {
                var evidence = await adapter.DiscoverAsync(scopes, cancellationToken);
                foreach (var item in evidence) session.Evidence.Add(item);
                results.Add(new(adapter.Provider, true, evidence, null));
            }
catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException)
            {
                var item = new EvidenceItem($"{adapter.Provider}.unavailable", EvidenceKind.Unresolved, "unavailable", $"{adapter.Provider}: {ex.Message}");
                session.Evidence.Add(item);
                results.Add(new(adapter.Provider, false, [item], ex.Message));
            }
        }
        return results;
    }
}
