using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using HELIOS.Fabric.Contracts;

namespace HELIOS.Fabric.Worker.Delivery;

public sealed class SharePointEvidenceSink(
    IHttpClientFactory clients,
    IConfiguration configuration,
    TokenCredential credential) : IDeliverySink
{
    public string ConnectorId => "sharepoint-evidence";

    public async Task DeliverAsync(EventEnvelope envelope, DeliveryRoute route, CancellationToken cancellationToken)
    {
        var siteId = configuration["HELIOS_SHAREPOINT_SITE_ID"] ?? throw new InvalidOperationException("HELIOS_SHAREPOINT_SITE_ID is not configured.");
        var driveId = configuration["HELIOS_SHAREPOINT_DRIVE_ID"] ?? throw new InvalidOperationException("HELIOS_SHAREPOINT_DRIVE_ID is not configured.");
        var token = await credential.GetTokenAsync(new TokenRequestContext(["https://graph.microsoft.com/.default"]), cancellationToken);
        var client = clients.CreateClient(nameof(SharePointEvidenceSink));
        client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var segments = new[]
        {
            "Automation Evidence",
            envelope.Environment,
            envelope.OccurredAt.ToString("yyyy"),
            envelope.OccurredAt.ToString("MM"),
            envelope.OccurredAt.ToString("dd"),
            envelope.CorrelationId.ToString(),
            $"{envelope.EventId}.json"
        };
        var path = string.Join('/', segments.Select(Uri.EscapeDataString));
        var content = JsonSerializer.Serialize(new
        {
            envelope,
            route.Transform,
            eventContentSha256 = envelope.Provenance.ContentSha256,
            archivedAt = DateTimeOffset.UtcNow,
            authoritativeApproval = "github-protected-environments"
        }, FabricJson.Options);
        var endpoint = $"sites/{Uri.EscapeDataString(siteId)}/drives/{Uri.EscapeDataString(driveId)}/root:/{path}:/content?@microsoft.graph.conflictBehavior=fail";
        using var response = await client.PutAsync(endpoint, new StringContent(content, Encoding.UTF8, "application/json"), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict) return; // Deterministic event path already exists.
        response.EnsureSuccessStatusCode();
    }
}
