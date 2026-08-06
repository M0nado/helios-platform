using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AIHub;

public interface IAIHubFleetService
{
    Task<AIHubFleetSnapshot> GetSnapshotAsync();
    Task<IReadOnlyList<AIHubCatalogEntry>> GetCatalogAsync();
    Task<IReadOnlyList<AIHubUnitRanking>> GetTopRankingsAsync(int take = 10);
    Task<IReadOnlyList<AIHubDeepLearningReference>> GetDeepLearningReferencesAsync();
    Task<int> CalculateBulkCostAsync(AIHubUnitType type, int quantity);
    Task<int> CalculateBulkCostAsync(string catalogKey, int quantity);
    Task<IReadOnlyList<AIHubUnit>> CreateUnitsAsync(AIHubUnitType type, int quantity);
    Task<IReadOnlyList<AIHubUnit>> CreateUnitsAsync(string catalogKey, int quantity);
    Task<IReadOnlyList<AIHubTrainingResult>> TrainUnitsAsync(IEnumerable<Guid> unitIds, int experience);
    Task<IReadOnlyList<AIHubTrainingResult>> TrainUnitsAsync(IEnumerable<Guid> unitIds, int experience, AIHubSpecialization specialization, AIHubImplementationLanguage language, string topic);
    Task<AIHubFleetDeployment> DeployFleetAsync(IEnumerable<Guid> unitIds, string mission);
}

public sealed class AIHubFleetService : IAIHubFleetService
{
    private readonly List<AIHubUnit> _units = new();
    private readonly List<AIHubFleetDeployment> _deployments = new();
    private readonly ReadOnlyCollection<AIHubCatalogEntry> _catalog = new(new[]
    {
        Entry("hermes-swift", AIHubUnitType.Hermes, AIHubModelFamily.Hermes, "Swift", AIHubUnitClass.Scout, AIHubSpecialization.GitHub, AIHubSpecialization.SubagentCoordination, AIHubImplementationLanguage.CSharp, 90, 1, 1, "Fast routing and branch reconnaissance", "hermes-swift", "#27E0FF", "Use for cheap scout work; pair with Atlas Cartographer for repo sweeps"),
        Entry("hermes-prime", AIHubUnitType.Hermes, AIHubModelFamily.Hermes, "Prime", AIHubUnitClass.Commander, AIHubSpecialization.SubagentCoordination, AIHubSpecialization.CSharpFrontend, AIHubImplementationLanguage.CSharp, 120, 1, 2, "Balanced local reasoning and coordination", "hermes-prime", "#00D9FF", "Best starter commander for balanced parties"),
        Entry("hermes-oracle", AIHubUnitType.Hermes, AIHubModelFamily.Hermes, "Oracle", AIHubUnitClass.Analyst, AIHubSpecialization.Analytics, AIHubSpecialization.FSharpPrediction, AIHubImplementationLanguage.FSharp, 180, 2, 2, "Prediction, summaries, and learned-report synthesis", "hermes-oracle", "#70F0FF", "Pair with XCore Quantum for F# forecasting"),
        Entry("hermes-relay", AIHubUnitType.Hermes, AIHubModelFamily.Hermes, "Relay", AIHubUnitClass.Commander, AIHubSpecialization.SubagentCoordination, AIHubSpecialization.PythonAIHubIntegration, AIHubImplementationLanguage.Python, 220, 2, 3, "Subagent command bus and AIHub integration bridge", "hermes-relay", "#35B8FF", "Use when a mission needs multiple subagents"),

        Entry("xcore-alpha", AIHubUnitType.XCore, AIHubModelFamily.XCore, "Alpha", AIHubUnitClass.Builder, AIHubSpecialization.CPlusPlusBackend, AIHubSpecialization.Docker, AIHubImplementationLanguage.CPlusPlus, 300, 3, 1, "General performance backend core", "xcore-alpha", "#6D5CFF", "Stable C++ backend unit for most build/deploy tasks"),
        Entry("xcore-titan", AIHubUnitType.XCore, AIHubModelFamily.XCore, "Titan", AIHubUnitClass.Builder, AIHubSpecialization.Azure, AIHubSpecialization.Docker, AIHubImplementationLanguage.CPlusPlus, 450, 4, 2, "Heavy Azure and Docker deployment core", "xcore-titan", "#855CFF", "Best deployment tank; slower but reliable"),
        Entry("xcore-flux", AIHubUnitType.XCore, AIHubModelFamily.XCore, "Flux", AIHubUnitClass.Builder, AIHubSpecialization.CPlusPlusBackend, AIHubSpecialization.Analytics, AIHubImplementationLanguage.CPlusPlus, 260, 3, 1, "Burst parallel optimization core", "xcore-flux", "#9C7BFF", "Fast but needs Hermes Relay to avoid overuse"),
        Entry("xcore-quantum", AIHubUnitType.XCore, AIHubModelFamily.XCore, "Quantum", AIHubUnitClass.Analyst, AIHubSpecialization.FSharpPrediction, AIHubSpecialization.Math, AIHubImplementationLanguage.FSharp, 520, 5, 2, "F# math, prediction, and analytics core", "xcore-quantum", "#B48CFF", "Best F# predictive modeler; expensive but high insight"),
        Entry("xcore-sentinel", AIHubUnitType.XCore, AIHubModelFamily.XCore, "Sentinel", AIHubUnitClass.Defender, AIHubSpecialization.Security, AIHubSpecialization.Docker, AIHubImplementationLanguage.CPlusPlus, 480, 5, 2, "Security hardening and quarantine core", "xcore-sentinel", "#5F8CFF", "Best security defender for risky deployments"),

        Entry("atlas-memory", AIHubUnitType.Atlas, AIHubModelFamily.Atlas, "Memory", AIHubUnitClass.Archivist, AIHubSpecialization.LearningMemory, AIHubSpecialization.PythonAIHubIntegration, AIHubImplementationLanguage.Python, 250, 2, 2, "Persistent learned-state and memory keeper", "atlas-memory", "#72E06A", "Attach Scribe subagents to preserve what everyone learns"),
        Entry("atlas-planner", AIHubUnitType.Atlas, AIHubModelFamily.Atlas, "Planner", AIHubUnitClass.Analyst, AIHubSpecialization.Analytics, AIHubSpecialization.Azure, AIHubImplementationLanguage.FSharp, 340, 3, 2, "F# planning, scoring, and balance forecasts", "atlas-planner", "#B6E66F", "Use before bulk deployment for better team balance"),
        Entry("atlas-ledger", AIHubUnitType.Atlas, AIHubModelFamily.Atlas, "Ledger", AIHubUnitClass.Archivist, AIHubSpecialization.GitHub, AIHubSpecialization.LearningMemory, AIHubImplementationLanguage.Python, 280, 2, 2, "Points, audit, GitHub, and history ledger", "atlas-ledger", "#E4D96F", "Use to grade scripts and preserve decisions"),
        Entry("atlas-cartographer", AIHubUnitType.Atlas, AIHubModelFamily.Atlas, "Cartographer", AIHubUnitClass.Scout, AIHubSpecialization.GitHub, AIHubSpecialization.PythonAIHubIntegration, AIHubImplementationLanguage.Python, 320, 3, 2, "Repository, branch, script, and dependency mapper", "atlas-cartographer", "#8CE6A8", "Best codebase explorer; pair with Hermes Swift"),

        Entry("aegis-prime", AIHubUnitType.Aegis, AIHubModelFamily.Aegis, "Prime", AIHubUnitClass.Commander, AIHubSpecialization.SubagentCoordination, AIHubSpecialization.Security, AIHubImplementationLanguage.CSharp, 1500, 10, 4, "Ultimate synchronized command core", "aegis-prime", "#FFF2A6", "Endgame commander; do not spam because cost scales high"),
        Entry("aegis-trinity", AIHubUnitType.Aegis, AIHubModelFamily.Aegis, "Trinity", AIHubUnitClass.Commander, AIHubSpecialization.Azure, AIHubSpecialization.FSharpPrediction, AIHubImplementationLanguage.FSharp, 3000, 12, 5, "Ultimate Hermes/XCore/Atlas fusion model", "aegis-trinity", "#FFFFFF", "Use for final cloud fleet orchestration only")
    });

    private readonly ReadOnlyCollection<AIHubUnitCost> _costs;

    public AIHubFleetService(int startingPoints = 2_500)
    {
        _costs = new ReadOnlyCollection<AIHubUnitCost>(new[]
        {
            new AIHubUnitCost(AIHubUnitType.Hermes, 120, 1, ResolveCatalog("hermes-prime").Role),
            new AIHubUnitCost(AIHubUnitType.XCore, 300, 3, ResolveCatalog("xcore-alpha").Role),
            new AIHubUnitCost(AIHubUnitType.Specialist, 550, 5, "Security, math, analytics, or integration expert"),
            new AIHubUnitCost(AIHubUnitType.Atlas, 250, 2, ResolveCatalog("atlas-memory").Role),
            new AIHubUnitCost(AIHubUnitType.Aegis, 1500, 10, ResolveCatalog("aegis-prime").Role)
        });
        Economy = new AIHubEconomyState { AvailablePoints = startingPoints, Costs = _costs, Catalog = _catalog };
        SeedStarterFleet();
    }

    public AIHubEconomyState Economy { get; }

    public Task<AIHubFleetSnapshot> GetSnapshotAsync() => Task.FromResult(new AIHubFleetSnapshot
    {
        Economy = Economy,
        Units = _units.ToArray(),
        Deployments = _deployments.ToArray()
    });

    public Task<IReadOnlyList<AIHubCatalogEntry>> GetCatalogAsync() => Task.FromResult<IReadOnlyList<AIHubCatalogEntry>>(_catalog);

    public Task<IReadOnlyList<AIHubUnitRanking>> GetTopRankingsAsync(int take = 10) =>
        Task.FromResult(AIHubOptimizationCatalog.RankCatalog(_catalog, take));

    public Task<IReadOnlyList<AIHubDeepLearningReference>> GetDeepLearningReferencesAsync() =>
        Task.FromResult(AIHubOptimizationCatalog.GetDeepLearningReferences());

    public Task<int> CalculateBulkCostAsync(AIHubUnitType type, int quantity)
    {
        if (quantity < 1)
        {
            return Task.FromResult(0);
        }

        var unitCost = _costs.Single(c => c.Type == type).Points;
        var subtotal = unitCost * quantity;
        var discount = quantity >= 10 ? 0.15 : quantity >= 5 ? 0.08 : 0.0;
        return Task.FromResult((int)Math.Round(subtotal * (1 - discount), MidpointRounding.AwayFromZero));
    }

    public Task<int> CalculateBulkCostAsync(string catalogKey, int quantity)
    {
        if (quantity < 1)
        {
            return Task.FromResult(0);
        }

        var unitCost = ResolveCatalog(catalogKey).Points;
        var subtotal = unitCost * quantity;
        var discount = quantity >= 10 ? 0.15 : quantity >= 5 ? 0.08 : 0.0;
        return Task.FromResult((int)Math.Round(subtotal * (1 - discount), MidpointRounding.AwayFromZero));
    }

    public Task<IReadOnlyList<AIHubUnit>> CreateUnitsAsync(AIHubUnitType type, int quantity)
    {
        var catalogKey = type switch
        {
            AIHubUnitType.Hermes => "hermes-prime",
            AIHubUnitType.XCore => "xcore-alpha",
            AIHubUnitType.Atlas => "atlas-memory",
            AIHubUnitType.Aegis => "aegis-prime",
            _ => "xcore-sentinel"
        };

        return CreateUnitsAsync(catalogKey, quantity);
    }

    public async Task<IReadOnlyList<AIHubUnit>> CreateUnitsAsync(string catalogKey, int quantity)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least one.");
        }

        var entry = ResolveCatalog(catalogKey);
        var totalCost = await CalculateBulkCostAsync(catalogKey, quantity).ConfigureAwait(false);
        if (totalCost > Economy.AvailablePoints)
        {
            throw new InvalidOperationException($"Insufficient AIHub points. Required {totalCost}, available {Economy.AvailablePoints}.");
        }

        var created = Enumerable.Range(1, quantity)
            .Select(index => CreateUnitFromCatalog(entry, _units.Count(u => u.Family == entry.Family && u.Variant == entry.Variant) + index))
            .ToArray();

        _units.AddRange(created);
        Economy.AvailablePoints -= totalCost;
        return created;
    }

    public Task<IReadOnlyList<AIHubTrainingResult>> TrainUnitsAsync(IEnumerable<Guid> unitIds, int experience) =>
        TrainUnitsAsync(unitIds, experience, AIHubSpecialization.Analytics, AIHubImplementationLanguage.Python, "General AIHub self-learning");

    public Task<IReadOnlyList<AIHubTrainingResult>> TrainUnitsAsync(
        IEnumerable<Guid> unitIds,
        int experience,
        AIHubSpecialization specialization,
        AIHubImplementationLanguage language,
        string topic)
    {
        if (experience < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(experience), "Experience must be positive.");
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Training topic is required.", nameof(topic));
        }

        var selected = ResolveUnits(unitIds, AIHubUnitStatus.Idle).ToArray();
        var results = selected.Select(unit =>
        {
            unit.Status = AIHubUnitStatus.Training;
            unit.CurrentWork = $"Learning {topic} with {language} +{experience} XP";
            unit.Experience += experience;
            unit.PrimarySpecialization = specialization;
            unit.PreferredLanguage = language;
            while (unit.Experience >= unit.Level * 100)
            {
                unit.Level++;
            }

            var record = new AIHubLearningRecord
            {
                UnitId = unit.Id,
                Topic = topic,
                Specialization = specialization,
                Language = language,
                ExperienceAdded = experience
            };
            unit.LearningRecords.Add(record);
            unit.LearnedSummary = $"Learned {topic} as {specialization} using {language}";
            unit.Status = AIHubUnitStatus.Idle;
            unit.CurrentWork = "Training complete";
            unit.UpdatedAt = DateTimeOffset.UtcNow;
            return new AIHubTrainingResult(unit.Id, experience, unit.Level, "Complete", unit.LearnedSummary);
        }).ToArray();

        return Task.FromResult<IReadOnlyList<AIHubTrainingResult>>(results);
    }

    public Task<AIHubFleetDeployment> DeployFleetAsync(IEnumerable<Guid> unitIds, string mission)
    {
        if (string.IsNullOrWhiteSpace(mission))
        {
            throw new ArgumentException("Mission is required.", nameof(mission));
        }

        var selected = ResolveUnits(unitIds, AIHubUnitStatus.Idle).ToArray();
        foreach (var unit in selected)
        {
            unit.Status = AIHubUnitStatus.Deployed;
            unit.CurrentWork = mission;
            unit.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var deployment = new AIHubFleetDeployment(Guid.NewGuid(), mission, selected.Select(u => u.Id).ToArray(), "Active", DateTimeOffset.UtcNow);
        _deployments.Add(deployment);
        return Task.FromResult(deployment);
    }

    private static AIHubCatalogEntry Entry(
        string catalogKey,
        AIHubUnitType type,
        AIHubModelFamily family,
        string variant,
        AIHubUnitClass unitClass,
        AIHubSpecialization primary,
        AIHubSpecialization? secondary,
        AIHubImplementationLanguage language,
        int points,
        int startingLevel,
        int subagentSlots,
        string role,
        string portraitKey,
        string accentColor,
        string balanceTip) =>
        new(catalogKey, type, family, variant, unitClass, primary, secondary, language, points, startingLevel, subagentSlots, role, portraitKey, accentColor, balanceTip);

    private AIHubCatalogEntry ResolveCatalog(string catalogKey) =>
        _catalog.SingleOrDefault(entry => string.Equals(entry.CatalogKey, catalogKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Unknown AIHub catalog entry: {catalogKey}.");

    private static AIHubUnit CreateUnitFromCatalog(AIHubCatalogEntry entry, int index) => new()
    {
        Name = $"{entry.Family}-{entry.Variant}-{index:000}",
        Type = entry.Type,
        Family = entry.Family,
        Variant = entry.Variant,
        Class = entry.Class,
        PrimarySpecialization = entry.PrimarySpecialization,
        SecondarySpecialization = entry.SecondarySpecialization,
        PreferredLanguage = entry.PreferredLanguage,
        Role = entry.Role,
        Level = entry.StartingLevel,
        SubagentSlots = entry.SubagentSlots,
        PortraitKey = entry.PortraitKey,
        AccentColor = entry.AccentColor,
        BalanceTip = entry.BalanceTip
    };

    private IEnumerable<AIHubUnit> ResolveUnits(IEnumerable<Guid> unitIds, AIHubUnitStatus requiredStatus)
    {
        var ids = unitIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (ids.Length == 0)
        {
            throw new ArgumentException("At least one unit must be selected.", nameof(unitIds));
        }

        var selected = _units.Where(unit => ids.Contains(unit.Id)).ToArray();
        if (selected.Length != ids.Length)
        {
            throw new InvalidOperationException("One or more selected units do not exist.");
        }

        if (selected.Any(unit => unit.Status != requiredStatus))
        {
            throw new InvalidOperationException($"All selected units must be {requiredStatus}.");
        }

        return selected;
    }

    private void SeedStarterFleet()
    {
        _units.AddRange(new[]
        {
            Starter("hermes-prime", 1, 4, 320, "Local routing"),
            Starter("xcore-alpha", 1, 5, 510, "Parallel C++ optimization"),
            Starter("atlas-memory", 1, 3, 280, "Python learning memory"),
            Starter("xcore-quantum", 1, 6, 740, "F# prediction analytics")
        });

        AIHubUnit Starter(string catalogKey, int index, int level, int experience, string learned)
        {
            var unit = CreateUnitFromCatalog(ResolveCatalog(catalogKey), index);
            unit.Level = level;
            unit.Experience = experience;
            unit.LearnedSummary = learned;
            return unit;
        }
    }
}
