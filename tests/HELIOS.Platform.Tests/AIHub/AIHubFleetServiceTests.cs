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

public sealed class AIHubExpandedTaxonomyTests
{
    [Fact]
    public async Task GetCatalogAsync_IncludesHermesXCoreAtlasAegisWithPythonAndFSharpSpecialties()
    {
        var service = new AIHubFleetService();
        var catalog = await service.GetCatalogAsync();

        Assert.Equal(4, catalog.Count(entry => entry.Family == AIHubModelFamily.Hermes));
        Assert.Contains(catalog, entry => entry.Family == AIHubModelFamily.Atlas && entry.PreferredLanguage == AIHubImplementationLanguage.Python);
        Assert.Contains(catalog, entry => entry.CatalogKey == "xcore-quantum" && entry.PrimarySpecialization == AIHubSpecialization.FSharpPrediction);
        Assert.Contains(catalog, entry => entry.Family == AIHubModelFamily.Aegis && entry.SubagentSlots >= 4);
    }

    [Fact]
    public async Task CreateUnitsAsync_WithCatalogKey_AssignsPortraitBalanceAndSubagentSlots()
    {
        var service = new AIHubFleetService(startingPoints: 2_000);

        var created = await service.CreateUnitsAsync("hermes-relay", 1);
        var unit = Assert.Single(created);

        Assert.Equal(AIHubModelFamily.Hermes, unit.Family);
        Assert.Equal("Relay", unit.Variant);
        Assert.Equal(AIHubImplementationLanguage.Python, unit.PreferredLanguage);
        Assert.Equal(3, unit.SubagentSlots);
        Assert.Equal("hermes-relay", unit.PortraitKey);
        Assert.Contains("subagent", unit.BalanceTip, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TrainUnitsAsync_WithSpecialty_RecordsSelfLearnedPythonAndFSharpTopics()
    {
        var service = new AIHubFleetService();
        var snapshot = await service.GetSnapshotAsync();
        var unit = snapshot.Units.First(u => u.Family == AIHubModelFamily.Atlas);

        var results = await service.TrainUnitsAsync(
            new[] { unit.Id },
            150,
            AIHubSpecialization.PythonAIHubIntegration,
            AIHubImplementationLanguage.Python,
            "Python AIHub adapter automation");
        var trained = (await service.GetSnapshotAsync()).Units.Single(u => u.Id == unit.Id);

        Assert.Single(results);
        Assert.Contains("Python AIHub adapter automation", trained.LearnedSummary);
        Assert.Contains(trained.LearningRecords, record => record.Language == AIHubImplementationLanguage.Python);
        Assert.Equal(AIHubSpecialization.PythonAIHubIntegration, trained.PrimarySpecialization);
    }
}

public sealed class AIHubOptimizationRankingTests
{
    [Fact]
    public async Task GetTopRankingsAsync_ReturnsTenRankedUnitsWithCapabilityScoresAndLoadouts()
    {
        var service = new AIHubFleetService();

        var rankings = await service.GetTopRankingsAsync();

        Assert.Equal(10, rankings.Count);
        Assert.Equal(Enumerable.Range(1, 10), rankings.Select(ranking => ranking.Rank));
        Assert.All(rankings, ranking =>
        {
            Assert.True(ranking.Score.WeightedTotal > 0);
            Assert.Equal(10, new[]
            {
                ranking.Score.Speed,
                ranking.Score.Reasoning,
                ranking.Score.Compute,
                ranking.Score.Security,
                ranking.Score.Cloud,
                ranking.Score.GitHub,
                ranking.Score.Analytics,
                ranking.Score.Prediction,
                ranking.Score.Learning,
                ranking.Score.Coordination
            }.Length);
            Assert.NotEmpty(ranking.RecommendedLoadout.Subagents);
            Assert.NotEmpty(ranking.RecommendedLoadout.Specializations);
            Assert.NotEmpty(ranking.RecommendedLoadout.Languages);
        });
    }

    [Fact]
    public async Task GetDeepLearningReferencesAsync_ConnectsDeepLearningAndLearningScripts()
    {
        var service = new AIHubFleetService();

        var references = await service.GetDeepLearningReferencesAsync();

        Assert.Contains(references, reference => reference.Path.EndsWith("DeepLearningPredictor.cs", StringComparison.Ordinal));
        Assert.Contains(references, reference => reference.Path.Contains("scripts/learning/ComprehensiveLearningSystem.psm1", StringComparison.Ordinal));
        Assert.Contains(references, reference => reference.Path.Contains("model-training.ps1", StringComparison.Ordinal));
        Assert.All(references, reference => Assert.False(string.IsNullOrWhiteSpace(reference.IntegrationTip)));
    }
}
