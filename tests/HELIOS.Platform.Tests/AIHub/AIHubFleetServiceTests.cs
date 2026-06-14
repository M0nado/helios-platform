using System;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.AIHub;
using Xunit;

namespace HELIOS.Platform.Tests.AIHub;

public sealed class AIHubFleetServiceTests
{
    [Fact]
    public async Task CalculateBulkCostAsync_AppliesBulkDiscounts()
    {
        var service = new AIHubFleetService();

        var single = await service.CalculateBulkCostAsync(AIHubUnitType.Hermes, 1);
        var fivePack = await service.CalculateBulkCostAsync(AIHubUnitType.Hermes, 5);
        var tenPack = await service.CalculateBulkCostAsync(AIHubUnitType.Hermes, 10);

        Assert.Equal(120, single);
        Assert.Equal(552, fivePack);
        Assert.Equal(1020, tenPack);
    }

    [Fact]
    public async Task CreateUnitsAsync_DebitsPointsAndAddsFleetUnits()
    {
        var service = new AIHubFleetService(startingPoints: 1_000);
        var before = await service.GetSnapshotAsync();

        var created = await service.CreateUnitsAsync(AIHubUnitType.XCore, 2);
        var after = await service.GetSnapshotAsync();

        Assert.Equal(2, created.Count);
        Assert.Equal(before.Units.Count + 2, after.Units.Count);
        Assert.Equal(400, after.Economy.AvailablePoints);
        Assert.All(created, unit => Assert.Equal(AIHubUnitStatus.Idle, unit.Status));
    }

    [Fact]
    public async Task CreateUnitsAsync_RejectsInsufficientPoints()
    {
        var service = new AIHubFleetService(startingPoints: 100);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUnitsAsync(AIHubUnitType.Specialist, 1));
    }

    [Fact]
    public async Task TrainUnitsAsync_AddsExperienceAndLevelsUnits()
    {
        var service = new AIHubFleetService();
        var unit = (await service.GetSnapshotAsync()).Units.First();

        var results = await service.TrainUnitsAsync(new[] { unit.Id }, 500);
        var trained = (await service.GetSnapshotAsync()).Units.Single(u => u.Id == unit.Id);

        Assert.Single(results);
        Assert.True(trained.Level > 4);
        Assert.Equal(AIHubUnitStatus.Idle, trained.Status);
        Assert.Equal("Training complete", trained.CurrentWork);
    }

    [Fact]
    public async Task DeployFleetAsync_MarksSelectedUnitsAsDeployed()
    {
        var service = new AIHubFleetService();
        var selected = (await service.GetSnapshotAsync()).Units.Take(2).Select(unit => unit.Id).ToArray();

        var deployment = await service.DeployFleetAsync(selected, "Azure Docker GitHub readiness sweep");
        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal("Active", deployment.Status);
        Assert.Equal(2, snapshot.DeployedCount);
        Assert.All(snapshot.Units.Where(unit => selected.Contains(unit.Id)), unit =>
        {
            Assert.Equal(AIHubUnitStatus.Deployed, unit.Status);
            Assert.Equal("Azure Docker GitHub readiness sweep", unit.CurrentWork);
        });
    }
}
