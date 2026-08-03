namespace HELIOS.Platform.Contracts.AIHub;

/// <summary>
/// Automation scope for the branch absorption finish flow.
/// </summary>
public enum BranchAbsorptionAutomationLevel
{
    PlanOnly,
    AutoLocal,
    RepoLive,
    CloudLive,
    FullLive
}

/// <summary>
/// Represents a branch, commit, document, or code fragment that can be scored for absorption or pruning.
/// </summary>
public sealed record BranchAbsorptionCandidate(
    string Id,
    string SourceRef,
    string Path,
    string Language,
    int ChangedLines,
    int FailingChecks,
    double Novelty,
    double SecurityRisk,
    double PerformanceValue,
    double LearningValue);

/// <summary>
/// Finish settings selected from the GUI before applying scoped branch absorption or multicloud actions.
/// </summary>
public sealed record BranchAbsorptionFinishSettings(
    BranchAbsorptionAutomationLevel AutomationLevel,
    bool AllowBranchCreation,
    bool AllowPullRequestCreation,
    bool AllowPagesPublish,
    bool AllowAzureValidation,
    bool RequireRollbackPlan,
    string MulticloudMode);

/// <summary>
/// Result used by the GUI and branch nervous system to decide whether to absorb, test, rewrite, or prune.
/// </summary>
public sealed record BranchAbsorptionDecision(
    string CandidateId,
    string Action,
    double Score,
    AIHubEngineKind PrimaryEngine,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> NextCommands);

/// <summary>
/// C# contract for branch/code/document absorption ranking. Implementations may call F# scoring and C++ graph helpers.
/// </summary>
public interface IBranchAbsorptionRanker
{
    BranchAbsorptionDecision Rank(BranchAbsorptionCandidate candidate);
    IReadOnlyList<BranchAbsorptionDecision> RankMany(IEnumerable<BranchAbsorptionCandidate> candidates);
}

/// <summary>
/// GUI-safe default branch absorption policy used by prototypes before a concrete service is wired in.
/// </summary>
public static class BranchAbsorptionDefaults
{
    public static BranchAbsorptionFinishSettings SafePlanOnly { get; } = new(
        BranchAbsorptionAutomationLevel.PlanOnly,
        AllowBranchCreation: false,
        AllowPullRequestCreation: false,
        AllowPagesPublish: false,
        AllowAzureValidation: true,
        RequireRollbackPlan: true,
        MulticloudMode: "local");

    public static AIHubEngineKind ChoosePrimaryEngine(BranchAbsorptionCandidate candidate)
    {
        var language = candidate.Language.ToLowerInvariant();
        if (language.Contains("c++", StringComparison.Ordinal) || candidate.PerformanceValue >= 0.75)
        {
            return AIHubEngineKind.CppNativePerformance;
        }

        if (language.Contains("f#", StringComparison.Ordinal) || candidate.LearningValue >= 0.75)
        {
            return AIHubEngineKind.FSharpLearning;
        }

        if (language.Contains("python", StringComparison.Ordinal))
        {
            return AIHubEngineKind.PythonIntegration;
        }

        return AIHubEngineKind.CSharpOrchestrator;
    }

    public static string ActionFromScore(double score) => score switch
    {
        >= 0.82 => "absorb-and-test",
        >= 0.64 => "isolate-and-specialize",
        >= 0.42 => "record-abstract-idea",
        _ => "prune-or-rewrite"
    };
}
