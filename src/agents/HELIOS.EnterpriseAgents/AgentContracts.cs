using System.Text.Json.Serialization;

namespace HELIOS.EnterpriseAgents;

public sealed class EnterpriseAgentFleet
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("fleetId")]
    public required string FleetId { get; init; }

    [JsonPropertyName("canonicalRepository")]
    public required string CanonicalRepository { get; init; }

    [JsonPropertyName("companionFleetRepository")]
    public string? CompanionFleetRepository { get; init; }

    [JsonPropertyName("parentIssue")]
    public int ParentIssue { get; init; }

    [JsonPropertyName("productionBlockerIssue")]
    public int ProductionBlockerIssue { get; init; }

    [JsonPropertyName("productionEnabled")]
    public bool ProductionEnabled { get; init; }

    [JsonPropertyName("defaultMode")]
    public required string DefaultMode { get; init; }

    [JsonPropertyName("correlationRequired")]
    public bool CorrelationRequired { get; init; }

    [JsonPropertyName("evidenceLinkRequired")]
    public bool EvidenceLinkRequired { get; init; }

    [JsonPropertyName("forbiddenGlobalCapabilities")]
    public List<string> ForbiddenGlobalCapabilities { get; init; } = [];

    [JsonPropertyName("identities")]
    public Dictionary<string, string> Identities { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("canonicalResources")]
    public Dictionary<string, string> CanonicalResources { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("agents")]
    public List<EnterpriseAgentDefinition> Agents { get; init; } = [];
}

public sealed class EnterpriseAgentDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("runtime")]
    public required string Runtime { get; init; }

    [JsonPropertyName("purpose")]
    public required string Purpose { get; init; }

    [JsonPropertyName("allowed")]
    public List<string> Allowed { get; init; } = [];

    [JsonPropertyName("denied")]
    public List<string> Denied { get; init; } = [];

    [JsonPropertyName("requiresApprovalFor")]
    public List<string> RequiresApprovalFor { get; init; } = [];

    [JsonPropertyName("secretReferences")]
    public List<string> SecretReferences { get; init; } = [];
}

public sealed class CustomConnectionRegistry
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("registryId")]
    public required string RegistryId { get; init; }

    [JsonPropertyName("defaultPolicy")]
    public required string DefaultPolicy { get; init; }

    [JsonPropertyName("owner")]
    public string? Owner { get; init; }

    [JsonPropertyName("connections")]
    public List<CustomConnectionDefinition> Connections { get; init; } = [];
}

public sealed class CustomConnectionDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("environment")]
    public List<string> Environment { get; init; } = [];

    [JsonPropertyName("auth")]
    public required ConnectionAuthentication Auth { get; init; }

    [JsonPropertyName("readTools")]
    public List<string> ReadTools { get; init; } = [];

    [JsonPropertyName("writeTools")]
    public List<string> WriteTools { get; init; } = [];

    [JsonPropertyName("approvalRequired")]
    public List<string> ApprovalRequired { get; init; } = [];

    [JsonPropertyName("healthCheck")]
    public required string HealthCheck { get; init; }
}

public sealed class ConnectionAuthentication
{
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("secretReferences")]
    public List<string> SecretReferences { get; init; } = [];

    [JsonPropertyName("recommendedStore")]
    public string? RecommendedStore { get; init; }
}

public sealed record AgentTaskEnvelope(
    Guid CorrelationId,
    string AgentId,
    string Operation,
    string Environment,
    string Repository,
    int? Issue,
    string? CommitSha,
    IReadOnlyDictionary<string, object?> Inputs,
    IReadOnlyList<string> RequiredApprovals,
    string? RollbackReference);

public sealed record AgentEvidence(
    string Kind,
    Uri Url,
    string? Sha256 = null);

public sealed record AgentTaskResult(
    Guid CorrelationId,
    string AgentId,
    string Operation,
    string Environment,
    string Status,
    IReadOnlyDictionary<string, object?> Outputs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<AgentEvidence> Evidence,
    string? RollbackReference,
    DateTimeOffset OccurredAt);
