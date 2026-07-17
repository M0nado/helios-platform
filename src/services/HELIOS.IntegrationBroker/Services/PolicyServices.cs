using HELIOS.IntegrationBroker.Contracts;

namespace HELIOS.IntegrationBroker.Services;

public static class IntegrationEventValidator
{
    private static readonly HashSet<string> Sources = new(StringComparer.OrdinalIgnoreCase)
    {
        "github", "azure", "hermes", "xcore", "codex", "github-copilot",
        "microsoft-copilot", "copilot-studio", "helios-platform", "monado-blade"
    };

    private static readonly HashSet<string> Environments = new(StringComparer.OrdinalIgnoreCase)
    {
        "local", "development", "test", "staging", "production"
    };

    private static readonly HashSet<string> Classifications = new(StringComparer.OrdinalIgnoreCase)
    {
        "public", "internal", "confidential", "restricted"
    };

    public static Dictionary<string, string[]> Validate(IntegrationEvent integrationEvent)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddIf(errors, "schemaVersion", integrationEvent.SchemaVersion != "1.0", "Schema version must be 1.0.");
        AddIf(errors, "eventId", string.IsNullOrWhiteSpace(integrationEvent.EventId) || integrationEvent.EventId.Length < 8,
            "Event ID must contain at least eight characters.");
        AddIf(errors, "source", !Sources.Contains(integrationEvent.Source), "Event source is not approved.");
        AddIf(errors, "eventType", string.IsNullOrWhiteSpace(integrationEvent.EventType), "Event type is required.");
        AddIf(errors, "correlationId", string.IsNullOrWhiteSpace(integrationEvent.CorrelationId) || integrationEvent.CorrelationId.Length < 4,
            "Correlation ID must contain at least four characters.");
        AddIf(errors, "environment", !Environments.Contains(integrationEvent.Environment), "Environment is not approved.");
        AddIf(errors, "occurredAt", integrationEvent.OccurredAt == default, "Occurred-at timestamp is required.");
        AddIf(errors, "dataClassification", !Classifications.Contains(integrationEvent.DataClassification),
            "Data classification is not approved.");
        AddIf(errors, "links", integrationEvent.Links is null, "Links must be supplied, even when empty.");

        if (integrationEvent.Actor is not null)
        {
            var actorTypeOk = integrationEvent.Actor.Type is "human" or "service" or "agent" or "workflow";
            AddIf(errors, "actor.type", !actorTypeOk, "Actor type is not approved.");
            AddIf(errors, "actor.id", string.IsNullOrWhiteSpace(integrationEvent.Actor.Id), "Actor ID is required.");
        }

        return errors;
    }

    private static void AddIf(IDictionary<string, string[]> errors, string key, bool condition, string message)
    {
        if (condition)
        {
            errors[key] = new[] { message };
        }
    }
}

public sealed class ToolPolicyService
{
    public ToolActionPreview Preview(ToolActionRequest request, ToolDescriptor tool)
    {
        var messages = new List<string>();
        var allowed = true;

        if (string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            allowed = false;
            messages.Add("A correlation ID is required.");
        }

        if (!tool.AllowedEnvironments.Contains(request.Environment, StringComparer.OrdinalIgnoreCase))
        {
            allowed = false;
            messages.Add($"Tool '{tool.Name}' is not approved for environment '{request.Environment}'.");
        }

        if (tool.Destructive)
        {
            allowed = false;
            messages.Add("Broker v1 rejects destructive tools.");
        }

        if (!tool.ReadOnly && string.Equals(request.Environment, "production", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tool.Approval, "production", StringComparison.OrdinalIgnoreCase))
        {
            allowed = false;
            messages.Add("Production writes require a separately reviewed production-approved tool definition.");
        }

        if (allowed)
        {
            messages.Add(tool.ReadOnly
                ? "Read-only invocation is permitted by catalog policy."
                : "The broker may record a pending request; connector execution remains approval-gated.");
        }

        return new ToolActionPreview(
            ToolName: tool.Name,
            Allowed: allowed,
            Approval: tool.Approval,
            ExecutionMode: tool.ReadOnly ? "read-only" : "pending-request",
            ReadOnly: tool.ReadOnly,
            OpenWorld: tool.OpenWorld,
            Destructive: tool.Destructive,
            PolicyMessages: messages);
    }
}
