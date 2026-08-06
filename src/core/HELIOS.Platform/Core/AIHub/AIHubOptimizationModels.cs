using System;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Core.AIHub;

public sealed record AIHubCapabilityScore(
    int Speed,
    int Reasoning,
    int Compute,
    int Security,
    int Cloud,
    int GitHub,
    int Analytics,
    int Prediction,
    int Learning,
    int Coordination)
{
    public int WeightedTotal =>
        Speed + Reasoning + Compute + Security + Cloud + GitHub + Analytics + Prediction + Learning + Coordination;
}

public sealed record AIHubLoadout(
    string Name,
    IReadOnlyList<AIHubSubagentType> Subagents,
    IReadOnlyList<AIHubSpecialization> Specializations,
    IReadOnlyList<AIHubImplementationLanguage> Languages,
    string BalanceTip);

public sealed record AIHubUnitRanking(
    int Rank,
    string CatalogKey,
    string DisplayName,
    AIHubModelFamily Family,
    string Variant,
    AIHubCapabilityScore Score,
    string Grade,
    AIHubLoadout RecommendedLoadout);

public sealed record AIHubDeepLearningReference(string Path, string Kind, string Grade, string IntegrationTip);

public static class AIHubOptimizationCatalog
{
    private static readonly IReadOnlyDictionary<string, AIHubCapabilityScore> Scores = new Dictionary<string, AIHubCapabilityScore>(StringComparer.OrdinalIgnoreCase)
    {
        ["hermes-swift"] = new(10, 7, 4, 4, 5, 9, 6, 5, 6, 8),
        ["hermes-prime"] = new(8, 8, 6, 6, 6, 7, 7, 6, 7, 9),
        ["hermes-oracle"] = new(6, 9, 5, 5, 5, 6, 9, 9, 8, 7),
        ["hermes-relay"] = new(7, 8, 5, 5, 7, 8, 7, 6, 8, 10),
        ["xcore-alpha"] = new(7, 6, 9, 6, 7, 5, 6, 5, 5, 5),
        ["xcore-titan"] = new(5, 6, 10, 8, 10, 6, 6, 5, 5, 6),
        ["xcore-flux"] = new(10, 5, 9, 4, 6, 5, 7, 6, 5, 4),
        ["xcore-quantum"] = new(6, 8, 8, 5, 6, 5, 10, 10, 8, 5),
        ["xcore-sentinel"] = new(5, 7, 8, 10, 8, 6, 6, 6, 6, 6),
        ["atlas-memory"] = new(5, 8, 5, 7, 6, 7, 8, 7, 10, 8),
        ["atlas-planner"] = new(5, 9, 6, 6, 8, 6, 9, 9, 9, 8),
        ["atlas-ledger"] = new(5, 7, 5, 8, 7, 10, 8, 6, 10, 7),
        ["atlas-cartographer"] = new(7, 8, 5, 6, 6, 10, 8, 7, 9, 7),
        ["aegis-prime"] = new(7, 10, 9, 10, 9, 8, 9, 9, 9, 10),
        ["aegis-trinity"] = new(8, 10, 10, 10, 10, 9, 10, 10, 10, 10)
    };

    public static IReadOnlyList<AIHubUnitRanking> RankCatalog(IEnumerable<AIHubCatalogEntry> catalog, int take = 10)
    {
        return catalog
            .Select(entry => new AIHubUnitRanking(
                Rank: 0,
                CatalogKey: entry.CatalogKey,
                DisplayName: $"{entry.Family} {entry.Variant}",
                Family: entry.Family,
                Variant: entry.Variant,
                Score: Scores.TryGetValue(entry.CatalogKey, out var score) ? score : new AIHubCapabilityScore(5, 5, 5, 5, 5, 5, 5, 5, 5, 5),
                Grade: "Pending",
                RecommendedLoadout: BuildLoadout(entry)))
            .OrderByDescending(ranking => ranking.Score.WeightedTotal)
            .ThenBy(ranking => ranking.CatalogKey)
            .Take(take)
            .Select((ranking, index) => ranking with
            {
                Rank = index + 1,
                Grade = ToGrade(ranking.Score.WeightedTotal)
            })
            .ToArray();
    }

    public static IReadOnlyList<AIHubDeepLearningReference> GetDeepLearningReferences() => new[]
    {
        new AIHubDeepLearningReference("src/core/HELIOS.Platform/Core/AdvancedML/DeepLearningPredictor.cs", "C# deep learning predictor", "A-", "Use as the C# prediction core behind Hermes Oracle, XCore Quantum, and Atlas Planner."),
        new AIHubDeepLearningReference("src/core/HELIOS.Platform/Core/AdvancedML/Interfaces/IDeepLearningPredictor.cs", "C# predictor contract", "A", "Bind AIHub learning missions to this interface before wiring concrete predictors."),
        new AIHubDeepLearningReference("scripts/learning/ComprehensiveLearningSystem.psm1", "PowerShell learning orchestrator", "B+", "Use as orchestration inspiration and expose through a safe adapter with dry-run output."),
        new AIHubDeepLearningReference("scripts/learning/LearningSystem.psm1", "PowerShell learning module", "B", "Connect to Atlas Memory after structured JSON output is added."),
        new AIHubDeepLearningReference("scripts/learning/BidirectionalLearning.psm1", "PowerShell bidirectional learning", "B", "Useful for self-learning feedback loops between AIHub units and scripts."),
        new AIHubDeepLearningReference("installer/analytics/machine-learning/model-training.ps1", "Installer ML training script", "B", "Promote into a Python-first training adapter or Azure Function trigger."),
        new AIHubDeepLearningReference("installer/analytics/machine-learning/prediction-engine.ps1", "Installer prediction script", "B-", "Use only after parameterizing inputs and returning structured output."),
        new AIHubDeepLearningReference("installer/analytics/machine-learning/adaptive-optimizer.ps1", "Adaptive optimizer", "B-", "Map optimizer recommendations into AIHub loadout balance tips."),
        new AIHubDeepLearningReference("installer/analytics/learning-engine/performance-profiler.ps1", "Performance profiler", "B", "Feed XCore and Atlas scoring variables."),
        new AIHubDeepLearningReference("installer/analytics/learning-database/learned-patterns.schema", "Learning data schema", "B+", "Use as the persistence shape for AIHubLearningRecord storage.")
    };

    private static AIHubLoadout BuildLoadout(AIHubCatalogEntry entry)
    {
        var subagents = entry.Family switch
        {
            AIHubModelFamily.Hermes => new[] { AIHubSubagentType.Scout, AIHubSubagentType.Commander },
            AIHubModelFamily.XCore => new[] { AIHubSubagentType.Builder, AIHubSubagentType.Cloud },
            AIHubModelFamily.Atlas => new[] { AIHubSubagentType.Scribe, AIHubSubagentType.Analyst },
            AIHubModelFamily.Aegis => new[] { AIHubSubagentType.Commander, AIHubSubagentType.Security, AIHubSubagentType.FSharpModeler },
            _ => new[] { AIHubSubagentType.Analyst }
        };

        var languages = entry.PrimarySpecialization switch
        {
            AIHubSpecialization.FSharpPrediction => new[] { AIHubImplementationLanguage.FSharp, AIHubImplementationLanguage.Python },
            AIHubSpecialization.PythonAIHubIntegration => new[] { AIHubImplementationLanguage.Python, AIHubImplementationLanguage.CSharp },
            AIHubSpecialization.CPlusPlusBackend => new[] { AIHubImplementationLanguage.CPlusPlus, AIHubImplementationLanguage.CSharp },
            _ => new[] { entry.PreferredLanguage }
        };

        var specialties = new List<AIHubSpecialization> { entry.PrimarySpecialization };
        if (entry.SecondarySpecialization is { } secondary)
        {
            specialties.Add(secondary);
        }

        return new AIHubLoadout(
            Name: $"{entry.Family}-{entry.Variant} optimized loadout",
            Subagents: subagents,
            Specializations: specialties,
            Languages: languages,
            BalanceTip: entry.BalanceTip);
    }

    private static string ToGrade(int total) => total switch
    {
        >= 92 => "S",
        >= 85 => "A+",
        >= 78 => "A",
        >= 70 => "B+",
        >= 62 => "B",
        >= 54 => "C+",
        _ => "C"
    };
}
