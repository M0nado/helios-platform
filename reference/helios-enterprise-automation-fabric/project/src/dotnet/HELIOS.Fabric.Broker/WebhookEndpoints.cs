using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HELIOS.Fabric.Broker.Security;
using HELIOS.Fabric.Contracts;
using Microsoft.AspNetCore.WebUtilities;

namespace HELIOS.Fabric.Broker.Services;

public static class WebhookEndpoints
{
    private const int MaximumBodyBytes = 1_048_576;
    private static readonly HashSet<string> AllowedSlackCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "incident", "plan", "evidence", "quarantine", "help"
    };

    public static void Map(WebApplication app)
    {
        app.MapPost("/hooks/github", HandleGitHubAsync);
        app.MapPost("/hooks/slack", HandleSlackAsync);
        app.MapPost("/hooks/linear", HandleLinearAsync);
        app.MapPost("/hooks/teams", HandleTeamsAsync);
        app.MapPost("/hooks/github-actions", HandleGitHubActionsAsync);
        app.MapPost("/api/events", HandleCanonicalEventAsync);
        app.MapGet("/api/status/{correlationId:guid}", HandleCorrelationStatusAsync);
    }

    private static async Task<IResult> HandleGitHubAsync(
        HttpRequest request,
        HmacVerifier verifier,
        ISecretResolver secrets,
        WebhookNormalizer normalizer,
        IEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(request, cancellationToken);
        var secret = await secrets.GetAsync("GITHUB_WEBHOOK_SECRET", "github-webhook-secret", cancellationToken);
        if (secret is null || !verifier.VerifyGitHub(secret, body, Header(request, "X-Hub-Signature-256"))) return Results.Unauthorized();

        using var document = JsonDocument.Parse(body);
        var eventName = Header(request, "X-GitHub-Event");
        var delivery = Header(request, "X-GitHub-Delivery");
        if (string.IsNullOrWhiteSpace(eventName)) return Results.BadRequest(new { error = "missing X-GitHub-Event" });

        var repository = ReadString(document.RootElement, "repository", "full_name");
        var actor = ReadString(document.RootElement, "sender", "login") ?? "github";
        var action = ReadString(document.RootElement, "action");
        var eventType = $"github.{eventName.Replace('_', '-')}" +
            (string.IsNullOrWhiteSpace(action) ? string.Empty : $".{action.Replace('_', '-')}");
        var subject = repository ?? "GitHub webhook";
        var idempotencyKey = string.IsNullOrWhiteSpace(delivery)
            ? $"github:{Sha256Hex(body)}"
            : $"github:{delivery}";
        var envelope = normalizer.Normalize(
            "github",
            eventType,
            subject,
            $"GitHub {eventName} {action}".Trim(),
            document.RootElement,
            actor,
            idempotencyKey,
            requestedActions: EventActions(eventName, action),
            repository: repository,
            commitSha: ReadString(document.RootElement, "after") ?? ReadString(document.RootElement, "workflow_run", "head_sha"),
            workflow: ReadString(document.RootElement, "workflow_run", "name"),
            workflowRunId: ReadString(document.RootElement, "workflow_run", "id"));
        await publisher.PublishAsync(envelope, cancellationToken);
        return Results.Accepted(value: new { accepted = true, envelope.EventId, envelope.CorrelationId });
    }

    private static async Task<IResult> HandleSlackAsync(
        HttpRequest request,
        HmacVerifier verifier,
        ISecretResolver secrets,
        WebhookNormalizer normalizer,
        IEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(request, cancellationToken);
        var timestamp = Header(request, "X-Slack-Request-Timestamp");
        var signature = Header(request, "X-Slack-Signature");
        var secret = await secrets.GetAsync("SLACK_SIGNING_SECRET", "slack-signing-secret", cancellationToken);
        if (secret is null || !verifier.VerifySlack(secret, body, timestamp, signature)) return Results.Unauthorized();
        var idempotencyKey = $"slack:{timestamp}:{Sha256Hex(body)}";

        if (request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var document = JsonDocument.Parse(body);
            if (ReadString(document.RootElement, "type") == "url_verification")
            {
                return Results.Ok(new { challenge = ReadString(document.RootElement, "challenge") });
            }
            var team = ReadString(document.RootElement, "team_id") ?? ReadString(document.RootElement, "team", "id") ?? "slack";
            var actor = ReadString(document.RootElement, "event", "user") ?? "slack";
            var envelope = normalizer.Normalize(
                "slack",
                "slack.event.received",
                team,
                "Slack event received",
                document.RootElement,
                actor,
                idempotencyKey,
                requestedActions: ["track"]);
            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Ok(new { ok = true, envelope.EventId });
        }

        var form = QueryHelpers.ParseQuery(Encoding.UTF8.GetString(body));
        var interactionJson = form.GetValueOrDefault("payload").ToString();
        if (!string.IsNullOrWhiteSpace(interactionJson))
        {
            using var interaction = JsonDocument.Parse(interactionJson);
            var actionId = ReadString(interaction.RootElement, "actions", "0", "action_id")
                ?? FirstArrayString(interaction.RootElement, "actions", "action_id")
                ?? "unknown";
            if (!actionId.StartsWith("helios_", StringComparison.Ordinal))
            {
                return Results.BadRequest(new { error = "unsupported Slack action" });
            }
            var actor = ReadString(interaction.RootElement, "user", "id") ?? "slack";
            var envelope = normalizer.Normalize(
                "slack",
                "slack.interaction.requested",
                actionId,
                $"Slack interaction {actionId}",
                interaction.RootElement,
                actor,
                idempotencyKey,
                requestedActions: SlackActionEffects(actionId));
            await publisher.PublishAsync(envelope, cancellationToken);
            return Results.Ok();
        }

        var command = form.GetValueOrDefault("command").ToString();
        var text = form.GetValueOrDefault("text").ToString().Trim();
        var user = form.GetValueOrDefault("user_id").ToString();
        var channel = form.GetValueOrDefault("channel_id").ToString();
        if (!string.Equals(command, "/helios", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "unsupported command" });
        }
        var verb = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "help";
        if (!AllowedSlackCommands.Contains(verb))
        {
            return Results.Ok(new
            {
                response_type = "ephemeral",
                text = "HELIOS usage: /helios status | incident | plan | evidence <id> | quarantine <connector>."
            });
        }
        var payload = JsonSerializer.SerializeToElement(new { command, verb, text, user, channel });
        var commandEnvelope = normalizer.Normalize(
            "slack",
            $"slack.command.{verb.ToLowerInvariant()}.requested",
            command,
            $"{command} {text}".Trim(),
            payload,
            user,
            idempotencyKey,
            requestedActions: SlackCommandEffects(verb));
        await publisher.PublishAsync(commandEnvelope, cancellationToken);
        return Results.Ok(new
        {
            response_type = "ephemeral",
            text = $"HELIOS accepted correlation {commandEnvelope.CorrelationId}. Privileged work continues in GitHub."
        });
    }

    private static async Task<IResult> HandleLinearAsync(
        HttpRequest request,
        HmacVerifier verifier,
        ISecretResolver secrets,
        WebhookNormalizer normalizer,
        IEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(request, cancellationToken);
        var secret = await secrets.GetAsync("LINEAR_WEBHOOK_SECRET", "linear-webhook-secret", cancellationToken);
        if (secret is null || !verifier.VerifyLinear(
                secret,
                body,
                Header(request, "Linear-Signature"),
                Header(request, "Linear-Timestamp")))
        {
            return Results.Unauthorized();
        }

        using var document = JsonDocument.Parse(body);
        var action = ReadString(document.RootElement, "action") ?? "changed";
        var type = ReadString(document.RootElement, "type") ?? Header(request, "Linear-Event") ?? "issue";
        var delivery = Header(request, "Linear-Delivery");
        var idempotencyKey = string.IsNullOrWhiteSpace(delivery)
            ? $"linear:{Sha256Hex(body)}"
            : $"linear:{delivery}";
        var actor = ReadString(document.RootElement, "actor", "id") ?? "linear";
        var envelope = normalizer.Normalize(
            "linear",
            $"linear.{type.ToLowerInvariant()}.{action.ToLowerInvariant()}",
            type,
            $"Linear {type} {action}",
            document.RootElement,
            actor,
            idempotencyKey,
            requestedActions: ["track"]);
        await publisher.PublishAsync(envelope, cancellationToken);
        return Results.Ok(new { accepted = true, envelope.EventId });
    }

    private static async Task<IResult> HandleTeamsAsync(
        HttpRequest request,
        HmacVerifier verifier,
        ISecretResolver secrets,
        WebhookNormalizer normalizer,
        IEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(request, cancellationToken);
        var timestamp = Header(request, "X-HELIOS-Timestamp");
        var secret = await secrets.GetAsync("TEAMS_APPROVAL_CALLBACK_SECRET", "teams-approval-callback-secret", cancellationToken);
        if (secret is null || !verifier.VerifyTeams(secret, body, timestamp, Header(request, "X-HELIOS-Signature"))) return Results.Unauthorized();
        using var document = JsonDocument.Parse(body);
        var idempotencyKey = $"teams:{timestamp}:{Sha256Hex(body)}";
        var envelope = normalizer.Normalize(
            "teams",
            "teams.acknowledgement.recorded",
            "Teams advisory acknowledgement",
            "A Teams acknowledgement was recorded; GitHub remains authoritative.",
            document.RootElement,
            ReadString(document.RootElement, "actor") ?? "teams",
            idempotencyKey,
            requestedActions: ["archive"]);
        await publisher.PublishAsync(envelope, cancellationToken);
        return Results.Ok(new { recorded = true, authoritative = false, envelope.EventId });
    }

    private static async Task<IResult> HandleGitHubActionsAsync(
        HttpRequest request,
        HmacVerifier verifier,
        ISecretResolver secrets,
        IEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(request, cancellationToken);
        var secret = await secrets.GetAsync("HELIOS_FABRIC_INGRESS_HMAC", "fabric-ingress-hmac", cancellationToken);
        if (secret is null || !verifier.VerifyTeams(
                secret,
                body,
                Header(request, "X-HELIOS-Timestamp"),
                Header(request, "X-HELIOS-Signature")))
        {
            return Results.Unauthorized();
        }
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(body, FabricJson.Options);
        var validation = ValidateCanonicalEnvelope(envelope);
        if (validation is not null) return validation;
        await publisher.PublishAsync(envelope!, cancellationToken);
        return Results.Accepted(value: new { accepted = true, envelope!.EventId, envelope.CorrelationId });
    }

    private static async Task<IResult> HandleCanonicalEventAsync(
        HttpRequest request,
        ISecretResolver secrets,
        IEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var supplied = Header(request, "X-HELIOS-Connector-Key");
        var expected = await secrets.GetAsync("HELIOS_CONNECTOR_KEY", "fabric-connector-key", cancellationToken);
        if (expected is null || !HmacVerifier.FixedEquals(expected, supplied)) return Results.Unauthorized();
        var envelope = await request.ReadFromJsonAsync<EventEnvelope>(FabricJson.Options, cancellationToken);
        var validation = ValidateCanonicalEnvelope(envelope);
        if (validation is not null) return validation;
        await publisher.PublishAsync(envelope!, cancellationToken);
        return Results.Accepted(value: new { accepted = true, envelope!.EventId, envelope.CorrelationId });
    }


    private static async Task<IResult> HandleCorrelationStatusAsync(
        Guid correlationId,
        HttpRequest request,
        ISecretResolver secrets,
        CorrelationStatusReader statuses,
        CancellationToken cancellationToken)
    {
        var supplied = Header(request, "X-HELIOS-Connector-Key");
        var expected = await secrets.GetAsync("HELIOS_CONNECTOR_KEY", "fabric-connector-key", cancellationToken);
        if (expected is null || !HmacVerifier.FixedEquals(expected, supplied)) return Results.Unauthorized();
        using var status = await statuses.ReadAsync(correlationId, cancellationToken);
        return status is null
            ? Results.NotFound(new { error = "correlation not found", correlationId })
            : Results.Json(status.RootElement.Clone(), FabricJson.Options);
    }

    private static IResult? ValidateCanonicalEnvelope(EventEnvelope? envelope)
    {
        if (envelope is null || envelope.SpecVersion != "1.0") return Results.BadRequest(new { error = "invalid event envelope" });
        if (string.IsNullOrWhiteSpace(envelope.Provenance.IdempotencyKey)) return Results.BadRequest(new { error = "missing idempotency key" });
        if (!string.Equals(CanonicalHash.ComputeEventContentSha256(envelope), envelope.Provenance.ContentSha256, StringComparison.Ordinal))
        {
            return Results.BadRequest(new { error = "contentSha256 mismatch" });
        }
        return null;
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaximumBodyBytes)
        {
            throw new BadHttpRequestException("Request body too large", StatusCodes.Status413PayloadTooLarge);
        }
        using var stream = new MemoryStream();
        await request.Body.CopyToAsync(stream, cancellationToken);
        if (stream.Length > MaximumBodyBytes)
        {
            throw new BadHttpRequestException("Request body too large", StatusCodes.Status413PayloadTooLarge);
        }
        return stream.ToArray();
    }

    private static string Header(HttpRequest request, string name) => request.Headers[name].ToString();

    private static string Sha256Hex(ReadOnlySpan<byte> value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private static string? ReadString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
            {
                if (index < 0 || index >= current.GetArrayLength()) return null;
                current = current[index];
                continue;
            }
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next)) return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static string? FirstArrayString(JsonElement element, string arrayProperty, string property)
    {
        if (!element.TryGetProperty(arrayProperty, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0) return null;
        return ReadString(array[0], property);
    }

    private static IReadOnlyList<string> EventActions(string eventName, string? action) =>
        eventName switch
        {
            "pull_request" => ["track"],
            "workflow_run" => action == "completed" ? ["notify", "track"] : ["track"],
            "release" => ["archive", "notify", "track"],
            "deployment_status" => ["archive", "notify", "track"],
            _ => ["track"]
        };

    private static IReadOnlyList<string> SlackCommandEffects(string verb) =>
        verb.ToLowerInvariant() switch
        {
            "status" or "help" => ["notify"],
            "incident" => ["notify", "track"],
            "evidence" => ["archive"],
            "plan" or "quarantine" => ["track"],
            _ => ["track"]
        };

    private static IReadOnlyList<string> SlackActionEffects(string actionId) =>
        actionId switch
        {
            "helios_open_evidence" => ["archive"],
            "helios_plan_reject" => ["track", "archive"],
            "helios_connector_quarantine_request" => ["track"],
            _ => ["track", "archive"]
        };
}
