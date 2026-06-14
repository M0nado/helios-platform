using System;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Core.AIHub;

public enum AIHubUnitType
{
    Hermes,
    XCore,
    Specialist
}

public enum AIHubUnitStatus
{
    Idle,
    Training,
    Deployed,
    Working,
    Offline
}

public sealed record AIHubUnitCost(AIHubUnitType Type, int Points, int StartingLevel, string Role);

public sealed class AIHubUnit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public AIHubUnitType Type { get; init; }
    public string Role { get; set; } = "Generalist";
    public AIHubUnitStatus Status { get; set; } = AIHubUnitStatus.Idle;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public string CurrentWork { get; set; } = "Awaiting assignment";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int ExperienceToNextLevel => Math.Max(0, (Level * 100) - Experience);
}

public sealed class AIHubEconomyState
{
    public int AvailablePoints { get; set; }
    public IReadOnlyList<AIHubUnitCost> Costs { get; init; } = Array.Empty<AIHubUnitCost>();
}

public sealed record AIHubFleetDeployment(Guid Id, string Mission, IReadOnlyList<Guid> UnitIds, string Status, DateTimeOffset CreatedAt);

public sealed record AIHubTrainingResult(Guid UnitId, int ExperienceAdded, int NewLevel, string Status);

public sealed class AIHubFleetSnapshot
{
    public required AIHubEconomyState Economy { get; init; }
    public required IReadOnlyList<AIHubUnit> Units { get; init; }
    public required IReadOnlyList<AIHubFleetDeployment> Deployments { get; init; }

    public int IdleCount => Units.Count(u => u.Status == AIHubUnitStatus.Idle);
    public int TrainingCount => Units.Count(u => u.Status == AIHubUnitStatus.Training);
    public int DeployedCount => Units.Count(u => u.Status == AIHubUnitStatus.Deployed || u.Status == AIHubUnitStatus.Working);
}
