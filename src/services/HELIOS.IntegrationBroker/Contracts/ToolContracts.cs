using System.Text.Json;

namespace HELIOS.IntegrationBroker.Contracts;

public sealed record ToolCatalogDocument(string SchemaVersion, IReadOnlyList<ToolDescriptor> Tools);

public sealed record ToolDescriptor(
    string Name,
    string Title,
    string Description,
    string Namespace,
    bool ReadOnly,
    bool OpenWorld,
    bool Destructive,
    string Approval,
    IReadOnlyList<string> AllowedEnvironments,
    JsonElement InputSchema);

public sealed record ServiceCatalogDocument(string SchemaVersion, IReadOnlyList<ServiceDescriptor> Services);

public sealed record ServiceDescriptor(
    string Name,
    string Owner,
    string Kind,
    string Status,
    string Authentication,
    string SecretSource,
    string? HealthPath);

public sealed record ToolActionRequest(
    string ToolName,
    string CorrelationId,
    string Environment,
    JsonElement Arguments,
    string? RequestedBy,
    string? Reason);

public sealed record ToolActionPreview(
    string ToolName,
    bool Allowed,
    string Approval,
    string ExecutionMode,
    bool ReadOnly,
    bool OpenWorld,
    bool Destructive,
    IReadOnlyList<string> PolicyMessages);

public sealed record PendingAction(
    string RequestId,
    string ToolName,
    string CorrelationId,
    string Environment,
    JsonElement Arguments,
    string? RequestedBy,
    string? Reason,
    string Status,
    string Approval,
    DateTimeOffset CreatedAt);
