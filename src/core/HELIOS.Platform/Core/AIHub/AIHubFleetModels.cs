using System;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Core.AIHub;

public enum AIHubUnitType
{
    Hermes,
    XCore,
    Specialist,
    Atlas,
    Aegis
}

public enum AIHubModelFamily
{
    Hermes,
    XCore,
    Atlas,
    Aegis
}

public enum AIHubUnitStatus
{
    Idle,
    Training,
    Deployed,
    Working,
    Offline
}

public enum AIHubUnitClass
{
    Scout,
    Builder,
    Defender,
    Analyst,
    Specialist,
    Commander,
    Archivist
}

public enum AIHubSpecialization
{
    Azure,
    Docker,
    GitHub,
    Security,
    CSharpFrontend,
    CPlusPlusBackend,
    FSharpPrediction,
    PythonAIHubIntegration,
    Analytics,
    Math,
    UXTheme,
    LearningMemory,
    SubagentCoordination
}

public enum AIHubImplementationLanguage
{
    CSharp,
    CPlusPlus,
    FSharp,
    Python,
    PowerShell
}

public enum AIHubSubagentType
{
    Scout,
    Builder,
    Security,
    Analyst,
    Cloud,
    Scribe,
    Commander,
    PythonToolsmith,
    FSharpModeler
}

public sealed record AIHubUnitCost(AIHubUnitType Type, int Points, int StartingLevel, string Role);

public sealed record AIHubCatalogEntry(
    string CatalogKey,
    AIHubUnitType Type,
    AIHubModelFamily Family,
    string Variant,
    AIHubUnitClass Class,
    AIHubSpecialization PrimarySpecialization,
    AIHubSpecialization? SecondarySpecialization,
    AIHubImplementationLanguage PreferredLanguage,
    int Points,
    int StartingLevel,
    int SubagentSlots,
    string Role,
    string PortraitKey,
    string AccentColor,
    string BalanceTip);

public sealed class AIHubSubagent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public AIHubSubagentType Type { get; init; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public string LearnedSummary { get; set; } = "Ready to learn from assigned work";
}

public sealed class AIHubLearningRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UnitId { get; init; }
    public required string Topic { get; init; }
    public AIHubSpecialization Specialization { get; init; }
    public AIHubImplementationLanguage Language { get; init; }
    public int ExperienceAdded { get; init; }
    public DateTimeOffset LearnedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class AIHubUnit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public AIHubUnitType Type { get; init; }
    public AIHubModelFamily Family { get; init; }
    public string Variant { get; init; } = "Prime";
    public AIHubUnitClass Class { get; init; } = AIHubUnitClass.Specialist;
    public AIHubSpecialization PrimarySpecialization { get; set; } = AIHubSpecialization.Analytics;
    public AIHubSpecialization? SecondarySpecialization { get; set; }
    public AIHubImplementationLanguage PreferredLanguage { get; set; } = AIHubImplementationLanguage.CSharp;
    public string Role { get; set; } = "Generalist";
    public AIHubUnitStatus Status { get; set; } = AIHubUnitStatus.Idle;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int SubagentSlots { get; init; } = 1;
    public List<AIHubSubagent> Subagents { get; } = new();
    public List<AIHubLearningRecord> LearningRecords { get; } = new();
    public string CurrentWork { get; set; } = "Awaiting assignment";
    public string LearnedSummary { get; set; } = "No learning captured yet";
    public string PortraitKey { get; set; } = "aihub-default";
    public string AccentColor { get; set; } = "#00D9FF";
    public string BalanceTip { get; set; } = "Pair with a complementary unit before deployment";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int ExperienceToNextLevel => Math.Max(0, (Level * 100) - Experience);
}

public sealed class AIHubEconomyState
{
    public int AvailablePoints { get; set; }
    public IReadOnlyList<AIHubUnitCost> Costs { get; init; } = Array.Empty<AIHubUnitCost>();
    public IReadOnlyList<AIHubCatalogEntry> Catalog { get; init; } = Array.Empty<AIHubCatalogEntry>();
}

public sealed record AIHubFleetDeployment(Guid Id, string Mission, IReadOnlyList<Guid> UnitIds, string Status, DateTimeOffset CreatedAt);

public sealed record AIHubTrainingResult(Guid UnitId, int ExperienceAdded, int NewLevel, string Status, string LearnedSummary);

public sealed class AIHubFleetSnapshot
{
    public required AIHubEconomyState Economy { get; init; }
    public required IReadOnlyList<AIHubUnit> Units { get; init; }
    public required IReadOnlyList<AIHubFleetDeployment> Deployments { get; init; }
    public IReadOnlyList<AIHubLearningRecord> LearningRecords => Units.SelectMany(unit => unit.LearningRecords).ToArray();

    public int IdleCount => Units.Count(u => u.Status == AIHubUnitStatus.Idle);
    public int TrainingCount => Units.Count(u => u.Status == AIHubUnitStatus.Training);
    public int DeployedCount => Units.Count(u => u.Status == AIHubUnitStatus.Deployed || u.Status == AIHubUnitStatus.Working);
}
