using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HELIOS.Fabric.Contracts;

namespace HELIOS.Fabric.Worker.Delivery;

public sealed class SlackSink(
    IHttpClientFactory clients,
    SecretProvider secrets,
    IConfiguration configuration,
    DeliveryTemplateRenderer templates) : IDeliverySink
{
    public string ConnectorId => "slack-ops";

    public async Task DeliverAsync(EventEnvelope envelope, DeliveryRoute route, CancellationToken cancellationToken)
    {
        var token = await secrets.RequiredAsync("slack-bot-token", cancellationToken);
        var channel = configuration[$"HELIOS_SLACK_CHANNEL_{envelope.Environment.ToUpperInvariant()}"]
            ?? configuration["HELIOS_SLACK_CHANNEL_DEFAULT"]
            ?? throw new InvalidOperationException("Slack channel binding is not configured.");
        var client = clients.CreateClient(nameof(SlackSink));
        client.BaseAddress = new Uri("https://slack.com/api/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = templates.RenderSlack(envelope, route) ?? new JsonObject();
        payload["channel"] = channel;
        payload["text"] = $"[{envelope.Severity.ToUpperInvariant()}] {envelope.Summary}";
        payload["client_msg_id"] = envelope.EventId.ToString();
        payload["metadata"] = new JsonObject
        {
            ["event_type"] = "helios_event",
            ["event_payload"] = new JsonObject
            {
                ["eventId"] = envelope.EventId.ToString(),
                ["correlationId"] = envelope.CorrelationId.ToString(),
                ["eventType"] = envelope.EventType
            }
        };
        using var response = await client.PostAsJsonAsync("chat.postMessage", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SlackResponse>(cancellationToken: cancellationToken);
        if (result is not { Ok: true }) throw new InvalidOperationException($"Slack rejected delivery: {result?.Error ?? "unknown"}");
    }

    private sealed record SlackResponse(bool Ok, string? Error);
}
