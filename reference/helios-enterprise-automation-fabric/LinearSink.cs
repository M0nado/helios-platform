using System.Net.Http.Json;
using System.Text.Json;
using HELIOS.Fabric.Contracts;

namespace HELIOS.Fabric.Worker.Delivery;

public sealed class LinearSink(
    IHttpClientFactory clients,
    SecretProvider secrets,
    IConfiguration configuration,
    DeliveryTemplateRenderer templates,
    ConnectorStateStore stateStore) : IDeliverySink
{
    public string ConnectorId => "linear-work";

    public async Task DeliverAsync(EventEnvelope envelope, DeliveryRoute route, CancellationToken cancellationToken)
    {
        var token = await secrets.RequiredAsync("linear-api-key", cancellationToken);
        var teamId = configuration["HELIOS_LINEAR_TEAM_ID"] ?? throw new InvalidOperationException("HELIOS_LINEAR_TEAM_ID is not configured.");
        var client = clients.CreateClient(nameof(LinearSink));
        client.BaseAddress = new Uri("https://api.linear.app/");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);

        var existing = await stateStore.ReadLinearAsync(envelope.CorrelationId, cancellationToken);
        if (existing is not null)
        {
            await CreateCommentAsync(client, existing.IssueId, envelope, cancellationToken);
            return;
        }

        var presentation = templates.RenderLinear(envelope, route);
        var mutation = "mutation HeliosIssue($input: IssueCreateInput!) { issueCreate(input: $input) { success issue { id identifier url } } }";
        using var response = await client.PostAsJsonAsync("graphql", new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    teamId,
                    title = presentation.Title,
                    description = presentation.Description,
                    priority = presentation.Priority
                }
            }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        ThrowOnErrors(document);
        var issue = document.RootElement.GetProperty("data").GetProperty("issueCreate").GetProperty("issue");
        var state = new LinearIssueState(
            issue.GetProperty("id").GetString()!,
            issue.GetProperty("identifier").GetString()!,
            issue.GetProperty("url").GetString()!,
            DateTimeOffset.UtcNow);
        await stateStore.WriteLinearOnceAsync(envelope.CorrelationId, state, cancellationToken);
    }

    private static async Task CreateCommentAsync(HttpClient client, string issueId, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        const string mutation = "mutation HeliosComment($input: CommentCreateInput!) { commentCreate(input: $input) { success comment { id } } }";
        using var response = await client.PostAsJsonAsync("graphql", new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    issueId,
                    body = $"HELIOS event `{envelope.EventType}` at {envelope.OccurredAt:O}\n\n{envelope.Summary}\n\nEvent: `{envelope.EventId}`"
                }
            }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        ThrowOnErrors(document);
    }

    private static void ThrowOnErrors(JsonDocument document)
    {
        if (document.RootElement.TryGetProperty("errors", out var errors))
        {
            throw new InvalidOperationException($"Linear rejected delivery: {errors}");
        }
    }
}
