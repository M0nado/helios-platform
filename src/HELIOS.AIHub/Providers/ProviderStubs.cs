using HELIOS.AIHub.Abstractions;

namespace HELIOS.AIHub.Providers;

public abstract class UnwiredProviderBase : IAiProvider
{
    protected UnwiredProviderBase(string id, string displayName, AiProviderKind kind, IReadOnlySet<string> capabilities, IReadOnlySet<AiDataClass> allowedDataClasses)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Capabilities = capabilities;
        AllowedDataClasses = allowedDataClasses;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public AiProviderKind Kind { get; }

    public IReadOnlySet<string> Capabilities { get; }

    public IReadOnlySet<AiDataClass> AllowedDataClasses { get; }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<AiTaskResponse> ExecuteAsync(AiTaskRequest request)
    {
        var metadata = new Dictionary<string, string>
        {
            ["status"] = "unwired",
            ["reason"] = "Provider skeleton is present, but Key Vault, endpoint, and policy wiring are not configured."
        };

        return Task.FromResult(new AiTaskResponse(
            request.TaskId,
            Id,
            Success: false,
            Summary: $"{DisplayName} is not configured.",
            Content: null,
            Metadata: metadata));
    }
}

public sealed class LocalLlmProvider : UnwiredProviderBase
{
    public LocalLlmProvider()
        : base(
            "local.llm",
            "Local LLM",
            AiProviderKind.LocalLlm,
            new HashSet<string> { "chat", "code", "summarize", "classify" },
            new HashSet<AiDataClass> { AiDataClass.Public, AiDataClass.Internal, AiDataClass.Sensitive })
    {
    }
}

public sealed class ChatGptProvider : UnwiredProviderBase
{
    public ChatGptProvider()
        : base(
            "cloud.chatgpt",
            "ChatGPT / OpenAI",
            AiProviderKind.CloudLlm,
            new HashSet<string> { "chat", "code", "reason", "review" },
            new HashSet<AiDataClass> { AiDataClass.Public, AiDataClass.Internal })
    {
    }
}

public sealed class AzureFoundryProvider : UnwiredProviderBase
{
    public AzureFoundryProvider()
        : base(
            "cloud.azure_foundry",
            "Azure AI Foundry",
            AiProviderKind.AzureFoundry,
            new HashSet<string> { "agent", "evaluation", "model-routing", "enterprise-ai" },
            new HashSet<AiDataClass> { AiDataClass.Public, AiDataClass.Internal, AiDataClass.Sensitive })
    {
    }
}

public sealed class CodexProvider : UnwiredProviderBase
{
    public CodexProvider()
        : base(
            "code.codex",
            "Codex Branch Executor",
            AiProviderKind.CodeAgent,
            new HashSet<string> { "branch-refactor", "test-generation", "pr-summary", "build-fix" },
            new HashSet<AiDataClass> { AiDataClass.Public, AiDataClass.Internal })
    {
    }
}

public sealed class GitHubCopilotProvider : UnwiredProviderBase
{
    public GitHubCopilotProvider()
        : base(
            "code.github_copilot",
            "GitHub Copilot Guidance",
            AiProviderKind.CopilotGuidance,
            new HashSet<string> { "completion-guidance", "workspace-context", "prompt-guidance" },
            new HashSet<AiDataClass> { AiDataClass.Public, AiDataClass.Internal })
    {
    }
}
