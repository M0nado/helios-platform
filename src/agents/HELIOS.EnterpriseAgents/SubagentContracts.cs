using System.Text.Json.Serialization;

namespace HELIOS.EnterpriseAgents;

public sealed class EnterpriseSubagentRegistry
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("registryId")]
    public required string RegistryId { get; init; }

    [JsonPropertyName("parentFleet")]
    public required string ParentFleet { get; init; }

    [JsonPropertyName("parentIssue")]
    public int ParentIssue { get; init; }

    [JsonPropertyName("productionBlockerIssue")]
    public int ProductionBlockerIssue { get; init; }

    [JsonPropertyName("defaultMode")]
    public required string DefaultMode { get; init; }

    [JsonPropertyName("subagents")]
    public List<EnterpriseSubagentDefinition> Subagents { get; init; } = [];
}

public sealed class EnterpriseSubagentDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("parentAgent")]
    public required string ParentAgent { get; init; }

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

    [JsonPropertyName("healthChecks")]
    public List<string> HealthChecks { get; init; } = [];

    [JsonPropertyName("evidence")]
    public List<string> Evidence { get; init; } = [];
}

public sealed class ToolchainManifest
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("toolchainId")]
    public required string ToolchainId { get; init; }

    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    [JsonPropertyName("installationMode")]
    public required string InstallationMode { get; init; }

    [JsonPropertyName("versionPolicy")]
    public required string VersionPolicy { get; init; }

    [JsonPropertyName("productionDeployment")]
    public bool ProductionDeployment { get; init; }

    [JsonPropertyName("pathRepairFirst")]
    public bool PathRepairFirst { get; init; }

    [JsonPropertyName("packageSources")]
    public List<string> PackageSources { get; init; } = [];

    [JsonPropertyName("tools")]
    public List<ToolchainToolDefinition> Tools { get; init; } = [];

    [JsonPropertyName("requiredEnvironmentReferences")]
    public List<string> RequiredEnvironmentReferences { get; init; } = [];

    [JsonPropertyName("postInstallChecks")]
    public List<string> PostInstallChecks { get; init; } = [];
}

public sealed class ToolchainToolDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("install")]
    public Dictionary<string, object?> Install { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("upgrade")]
    public string? Upgrade { get; init; }

    [JsonPropertyName("verify")]
    public List<string> Verify { get; init; } = [];

    [JsonPropertyName("authRequiredFor")]
    public List<string> AuthRequiredFor { get; init; } = [];

    [JsonPropertyName("manualValidation")]
    public List<string> ManualValidation { get; init; } = [];

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed class SupplementalConnectionRegistry
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("registryId")]
    public required string RegistryId { get; init; }

    [JsonPropertyName("defaultPolicy")]
    public required string DefaultPolicy { get; init; }

    [JsonPropertyName("parentRegistry")]
    public required string ParentRegistry { get; init; }

    [JsonPropertyName("connections")]
    public List<CustomConnectionDefinition> Connections { get; init; } = [];
}
