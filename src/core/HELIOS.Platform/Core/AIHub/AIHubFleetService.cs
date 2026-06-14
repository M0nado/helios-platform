using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AIHub;

public interface IAIHubFleetService
{
    Task<AIHubFleetSnapshot> GetSnapshotAsync();
    Task<int> CalculateBulkCostAsync(AIHubUnitType type, int quantity);
    Task<IReadOnlyList<AIHubUnit>> CreateUnitsAsync(AIHubUnitType type, int quantity);
    Task<IReadOnlyList<AIHubTrainingResult>> TrainUnitsAsync(IEnumerable<Guid> unitIds, int experience);
    Task<AIHubFleetDeployment> DeployFleetAsync(IEnumerable<Guid> unitIds, string mission);
}

public sealed class AIHubFleetService : IAIHubFleetService
{
    private readonly List<AIHubUnit> _units = new();
    private readonly List<AIHubFleetDeployment> _deployments = new();
    private readonly ReadOnlyCollection<AIHubUnitCost> _costs = new(new[]
    {
        new AIHubUnitCost(AIHubUnitType.Hermes, 120, 1, "Rapid local reasoning"),
        new AIHubUnitCost(AIHubUnitType.XCore, 300, 3, "Parallel performance core"),
        new AIHubUnitCost(AIHubUnitType.Specialist, 550, 5, "Security, math, analytics, or integration expert")
    });

    public AIHubFleetService(int startingPoints = 2_500)
    {
        Economy = new AIHubEconomyState { AvailablePoints = startingPoints, Costs = _costs };
        SeedStarterFleet();
    }

    public AIHubEconomyState Economy { get; }

    public Task<AIHubFleetSnapshot> GetSnapshotAsync() => Task.FromResult(new AIHubFleetSnapshot
    {
        Economy = Economy,
        Units = _units.ToArray(),
        Deployments = _deployments.ToArray()
    });

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

    public async Task<IReadOnlyList<AIHubUnit>> CreateUnitsAsync(AIHubUnitType type, int quantity)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least one.");
        }

        var totalCost = await CalculateBulkCostAsync(type, quantity).ConfigureAwait(false);
        if (totalCost > Economy.AvailablePoints)
        {
            throw new InvalidOperationException($"Insufficient AIHub points. Required {totalCost}, available {Economy.AvailablePoints}.");
        }

        var cost = _costs.Single(c => c.Type == type);
        var created = Enumerable.Range(1, quantity)
            .Select(index => new AIHubUnit
            {
                Name = $"{type}-{_units.Count(u => u.Type == type) + index:000}",
                Type = type,
                Role = cost.Role,
                Level = cost.StartingLevel
            })
            .ToArray();

        _units.AddRange(created);
        Economy.AvailablePoints -= totalCost;
        return created;
    }

    public Task<IReadOnlyList<AIHubTrainingResult>> TrainUnitsAsync(IEnumerable<Guid> unitIds, int experience)
    {
        if (experience < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(experience), "Experience must be positive.");
        }

        var selected = ResolveUnits(unitIds, AIHubUnitStatus.Idle).ToArray();
        var results = selected.Select(unit =>
        {
            unit.Status = AIHubUnitStatus.Training;
            unit.CurrentWork = $"Training +{experience} XP";
            unit.Experience += experience;
            while (unit.Experience >= unit.Level * 100)
            {
                unit.Level++;
            }
            unit.Status = AIHubUnitStatus.Idle;
            unit.CurrentWork = "Training complete";
            unit.UpdatedAt = DateTimeOffset.UtcNow;
            return new AIHubTrainingResult(unit.Id, experience, unit.Level, "Complete");
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
            new AIHubUnit { Name = "Hermes-Prime", Type = AIHubUnitType.Hermes, Role = "Local routing", Level = 4, Experience = 320 },
            new AIHubUnit { Name = "XCore-Alpha", Type = AIHubUnitType.XCore, Role = "Parallel optimization", Level = 5, Experience = 510 },
            new AIHubUnit { Name = "Specialist-Sec", Type = AIHubUnitType.Specialist, Role = "Security hardening", Level = 6, Experience = 740 }
        });
    }
}
