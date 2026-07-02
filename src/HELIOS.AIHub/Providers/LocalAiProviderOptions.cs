using HELIOS.AIHub.Abstractions;

namespace HELIOS.AIHub.Providers;

/// <summary>Configuration for a local AI provider used by AIHub. Keeps local model or endpoint settings explicit.
/// </summary>
public sealed record LocalAiProviderOptions(
    string ProviderId,
    string DisplayName,
    IReadOnlySet<string> Capabilities,
    IReadOnlySet<AiDataClass> AllowedDataClasses,
    string? ModelPath = null,
    Uri? Endpoint = null,
    int MaxPromptCharacters = 24_000)
{
    /// <summary>Default offline Hermes XCore provider profile for local-only planning and drafting.</summary>
    public static LocalAiProviderOptions HermesXCoreDefault { get; } = new(
        ProviderId: "local-hermes-xcore",
        DisplayName: "Hermes XCore Local AI",
        Capabilities: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "local-ai",
            "code-analysis",
            "security-review",
            "optimization",
            "aihub-integration",
            "offline-draft"
        },
        AllowedDataClasses: new HashSet<AiDataClass>
        {
            AiDataClass.Public,
            AiDataClass.Internal,
            AiDataClass.Sensitive
        });
}
