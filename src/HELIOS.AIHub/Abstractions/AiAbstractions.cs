namespace HELIOS.AIHub.Abstractions;

public enum AiProviderKind
{
    LocalLlm,
    CloudLlm,
    AzureFoundry,
    AzureOpenAI,
    CodeAgent,
    CopilotGuidance,
    ToolRouter
}

public enum AiDataClass
{
    Public,
    Internal,
    Sensitive,
    Secret,
    Dangerous
}

public enum AiExecutionMode
{
    Suggest,
    Draft,
    DryRun,
    Execute,
    ElevatedExecute
}

public sealed record AiTaskRequest(
    string TaskId,
    string Intent,
    string Prompt,
    AiDataClass DataClass,
    AiExecutionMode RequestedMode,
    IReadOnlyDictionary<string, string> Context,
    CancellationToken CancellationToken = default);

public sealed record AiTaskResponse(
    string TaskId,
    string ProviderId,
    bool Success,
    string Summary,
    string? Content,
    IReadOnlyDictionary<string, string> Metadata);

public interface IAiProvider
{
    string Id { get; }

    string DisplayName { get; }

    AiProviderKind Kind { get; }

    IReadOnlySet<string> Capabilities { get; }

    IReadOnlySet<AiDataClass> AllowedDataClasses { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<AiTaskResponse> ExecuteAsync(AiTaskRequest request);
}

public interface IHermesAgent
{
    string Id { get; }

    string Name { get; }

    IReadOnlySet<AiExecutionMode> AllowedModes { get; }

    Task<AiTaskResponse> RunAsync(AiTaskRequest request);
}

public interface IAgentRouter
{
    Task<IAiProvider?> SelectProviderAsync(AiTaskRequest request, IEnumerable<IAiProvider> providers);

    Task<IHermesAgent?> SelectAgentAsync(AiTaskRequest request, IEnumerable<IHermesAgent> agents);
}
