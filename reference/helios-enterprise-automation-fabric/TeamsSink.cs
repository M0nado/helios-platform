using System.Net.Http.Json;
using HELIOS.Fabric.Contracts;

namespace HELIOS.Fabric.Worker.Delivery;

public sealed class TeamsSink(
    IHttpClientFactory clients,
    SecretProvider secrets,
    DeliveryTemplateRenderer templates) : IDeliverySink
{
    public string ConnectorId => "teams-ops";

    public async Task DeliverAsync(EventEnvelope envelope, DeliveryRoute route, CancellationToken cancellationToken)
    {
        var webhook = await secrets.RequiredAsync("teams-workflow-webhook-url", cancellationToken);
        var card = templates.RenderTeams(envelope, route);
        var client = clients.CreateClient(nameof(TeamsSink));
        using var response = await client.PostAsJsonAsync(webhook, new
        {
            specVersion = envelope.SpecVersion,
            envelope.EventId,
            envelope.CorrelationId,
            envelope.EventType,
            envelope.Environment,
            envelope.Severity,
            envelope.Summary,
            sourceSha = envelope.Provenance.CommitSha,
            route.Transform,
            idempotencyKey = envelope.EventId,
            authoritativeApproval = "github-protected-environments",
            adaptiveCard = card
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
