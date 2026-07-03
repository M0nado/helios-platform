namespace HELIOS.Platform.Contracts.AIHub;

/// <summary>
/// 30-variable complex code grading signals routed through C# orchestration into F# scoring and C++ hot-path helpers.
/// </summary>
public sealed record ComplexCodeGradeSignals(
    string Path,
    string Module,
    double UniqueTokenRatio,
    double DuplicateLineRatio,
    double ComplexityScore,
    double MaintainabilityScore,
    double ReuseScore,
    double SecuritySignal,
    double PerformanceSignal,
    double LearningSignal,
    double ProviderGlueSignal);

/// <summary>
/// Engine placement for a complex code grading decision.
/// </summary>
public sealed record ComplexCodeGradeDecision(
    string Path,
    string Action,
    double KeepScore,
    AIHubEngineKind PrimaryScoringEngine,
    IReadOnlyList<AIHubEngineKind> EnginePipeline,
    IReadOnlyList<string> Reasons);

/// <summary>
/// C# owns the safe orchestration boundary while delegating analytics-heavy grading to F#, hot-path comparison to C++, and provider glue to Python only when useful.
/// </summary>
public interface IComplexCodeGradingOrchestrator
{
    ComplexCodeGradeDecision Decide(ComplexCodeGradeSignals signals);
    IReadOnlyList<ComplexCodeGradeDecision> DecideMany(IEnumerable<ComplexCodeGradeSignals> signals);
}

public static class ComplexCodeGradeEngineRouter
{
    public static IReadOnlyList<AIHubEngineKind> BuildPipeline(ComplexCodeGradeSignals signals)
    {
        var pipeline = new List<AIHubEngineKind>
        {
            AIHubEngineKind.CSharpOrchestrator,
            AIHubEngineKind.FSharpLearning
        };

        if (signals.PerformanceSignal >= 0.50 || signals.ComplexityScore >= 0.70)
        {
            pipeline.Add(AIHubEngineKind.CppNativePerformance);
        }

        if (signals.ProviderGlueSignal >= 0.55)
        {
            pipeline.Add(AIHubEngineKind.PythonIntegration);
        }

        return pipeline;
    }

    public static string ActionFromKeepScore(double keepScore, double duplicateLineRatio) => (keepScore, duplicateLineRatio) switch
    {
        (>= 0.74, _) => "keep-unique-good-part",
        (>= 0.58, < 0.40) => "absorb-small-idea-through-fsharp-score",
        (< 0.42, >= 0.42) => "prune-or-rewrite-trash-after-csharp-review",
        _ => "review-with-fsharp-and-csharp"
    };
}
