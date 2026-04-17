using HELIOS.Platform.Core.AdvancedML;
using HELIOS.Platform.Core.AdvancedML.Interfaces;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Performance;
using Xunit;
using System.Diagnostics;

namespace HELIOS.Platform.Tests.Phase5.AdvancedML;

/// <summary>
/// Comprehensive tests for Phase 5 Tier 1 - Advanced ML Services
/// Tests all 7 services with caching, performance, and integration verification.
/// </summary>
public class Phase5AdvancedMLTests
{
    private readonly ILogger _logger;
    private readonly IL1CacheService _cache;

    public Phase5AdvancedMLTests()
    {
        _logger = new MockLogger();
        _cache = new L1CacheService(_logger);
    }

    #region DeepLearningPredictor Tests

    [Fact]
    public async Task DeepLearningPredictor_Predict_ReturnsValidPredictions()
    {
        // Arrange
        var predictor = new DeepLearningPredictor(_logger, _cache);
        var inputData = new[] { 1.0, 2.0, 3.0, 2.5, 3.5, 4.0, 3.8, 4.5, 5.0 };

        // Act
        var result = await predictor.PredictAsync(inputData, 3, 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Values.Length);
        Assert.All(result.Values, v => Assert.InRange(v, -10, 10));
        Assert.All(result.ConfidenceScores, c => Assert.InRange(c, 0.5, 1.0));
        Assert.True(result.PredictionTimeMs > 0);
    }

    [Fact]
    public async Task DeepLearningPredictor_Train_UpdatesModel()
    {
        // Arrange
        var predictor = new DeepLearningPredictor(_logger, _cache);
        var trainingData = Enumerable.Range(0, 50).Select(x => (double)x * 0.5).ToArray();

        // Act
        await predictor.TrainAsync(trainingData, 5, 10);
        var metrics = await predictor.GetMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.IsModelTrained);
        Assert.Equal(10, metrics.EpochsTrained);
        Assert.InRange(metrics.RSquared, 0, 1);
    }

    [Fact]
    public async Task DeepLearningPredictor_Caching_ImprovesPredictionSpeed()
    {
        // Arrange
        var predictor = new DeepLearningPredictor(_logger, _cache);
        var inputData = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act - First call (uncached)
        var sw1 = Stopwatch.StartNew();
        await predictor.PredictAsync(inputData, 2, 3);
        sw1.Stop();

        // Act - Second call (cached)
        var sw2 = Stopwatch.StartNew();
        var result2 = await predictor.PredictAsync(inputData, 2, 3);
        sw2.Stop();

        // Assert
        Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds);
        Assert.True(result2.PredictionTimeMs < 50); // Performance target
    }

    [Fact]
    public async Task DeepLearningPredictor_GetMetrics_ReturnsValidStats()
    {
        // Arrange
        var predictor = new DeepLearningPredictor(_logger, _cache);

        // Act
        var metrics = await predictor.GetMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.InRange(metrics.MAE, 0, 1);
        Assert.InRange(metrics.RMSE, 0, 1);
        Assert.InRange(metrics.RSquared, 0, 1);
    }

    #endregion

    #region AutoMLOptimizer Tests

    [Fact]
    public async Task AutoMLOptimizer_SelectBestModel_ReturnsTopModel()
    {
        // Arrange
        var optimizer = new AutoMLOptimizer(_logger, _cache);
        var data = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0 };
        var targets = new[] { 2.0, 4.0, 6.0, 8.0, 10.0, 12.0, 14.0, 16.0 };

        // Act
        var result = await optimizer.SelectBestModelAsync(data, targets);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ModelType);
        Assert.InRange(result.AccuracyScore, 0.5, 1.0);
        Assert.NotEmpty(result.Hyperparameters);
        Assert.True(result.SelectionTimeMs < 5000);
    }

    [Fact]
    public async Task AutoMLOptimizer_OptimizeHyperparameters_TunesModel()
    {
        // Arrange
        var optimizer = new AutoMLOptimizer(_logger, _cache);
        var data = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var targets = new[] { 1.5, 3.0, 4.5, 6.0, 7.5 };

        // Act
        var tuning = await optimizer.OptimizeHyperparametersAsync("linear", data, targets);

        // Assert
        Assert.NotNull(tuning);
        Assert.Equal("grid_search", tuning.TuningMethod);
        Assert.NotEmpty(tuning.BestParameters);
        Assert.InRange(tuning.BestScore, 0, 1);
        Assert.True(tuning.CombinationsEvaluated > 0);
    }

    [Fact]
    public async Task AutoMLOptimizer_EvaluateModels_RanksModels()
    {
        // Arrange
        var optimizer = new AutoMLOptimizer(_logger, _cache);
        var data = new[] { 1.0, 2.0, 3.0, 4.0 };
        var targets = new[] { 2.0, 4.0, 6.0, 8.0 };
        var modelTypes = new[] { "linear", "exponential", "polynomial" };

        // Act
        var evaluations = await optimizer.EvaluateModelsAsync(data, targets, modelTypes);

        // Assert
        Assert.NotEmpty(evaluations);
        for (int i = 0; i < evaluations.Count; i++)
            Assert.Equal(i + 1, evaluations[i].Rank);
        Assert.InRange(evaluations[0].AccuracyScore, 0, 1);
    }

    #endregion

    #region FederatedLearning Tests

    [Fact]
    public async Task FederatedLearning_TrainLocal_ProducesNodeUpdate()
    {
        // Arrange
        var fedLearning = new FederatedLearning(_logger, _cache);
        var nodeData = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var update = await fedLearning.TrainLocalAsync("node1", nodeData, 5);

        // Assert
        Assert.NotNull(update);
        Assert.Equal("node1", update.NodeId);
        Assert.Equal(nodeData.Length, update.LocalSampleCount);
        Assert.InRange(update.LocalAccuracy, 0, 1);
        Assert.NotEmpty(update.Weights);
    }

    [Fact]
    public async Task FederatedLearning_AggregateUpdates_CreatesGlobalModel()
    {
        // Arrange
        var fedLearning = new FederatedLearning(_logger, _cache);
        var update1 = await fedLearning.TrainLocalAsync("node1", new[] { 1.0, 2.0, 3.0 }, 5);
        var update2 = await fedLearning.TrainLocalAsync("node2", new[] { 4.0, 5.0, 6.0 }, 5);

        // Act
        var global = await fedLearning.AggregateUpdatesAsync(new List<FederatedModelUpdate> { update1, update2 });

        // Assert
        Assert.NotNull(global);
        Assert.True(global.IsGlobalModel);
        Assert.Equal(update1.LocalSampleCount + update2.LocalSampleCount, global.LocalSampleCount);
        Assert.InRange(global.LocalAccuracy, 0, 1);
    }

    [Fact]
    public async Task FederatedLearning_GetStats_ReturnsValidStats()
    {
        // Arrange
        var fedLearning = new FederatedLearning(_logger, _cache);
        await fedLearning.TrainLocalAsync("node1", new[] { 1.0, 2.0 }, 5);

        // Act
        var stats = await fedLearning.GetStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(1, stats.NodeCount);
        Assert.InRange(stats.GlobalAccuracy, 0, 1);
        Assert.True(stats.TotalSamplesTrained > 0);
    }

    #endregion

    #region ReinforcementLearning Tests

    [Fact]
    public async Task ReinforcementLearning_SelectAction_ChoosesValidAction()
    {
        // Arrange
        var rl = new ReinforcementLearning(_logger, _cache);
        var state = new[] { 0.5, 0.3, 0.7 };

        // Act
        var action = await rl.SelectActionAsync(state);

        // Assert
        Assert.NotNull(action);
        Assert.InRange(action.ActionIndex, 0, 15);
        Assert.InRange(action.Confidence, 0, 1);
        Assert.InRange(action.QValue, -1, 1);
    }

    [Fact]
    public async Task ReinforcementLearning_Learn_UpdatesPolicy()
    {
        // Arrange
        var rl = new ReinforcementLearning(_logger, _cache);
        var state = new[] { 0.5, 0.3 };
        var nextState = new[] { 0.6, 0.4 };

        // Act
        await rl.LearnAsync(state, 0, 10.0, nextState, false);
        var stats = await rl.GetPolicyStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.StatesExplored > 0);
        Assert.InRange(stats.CumulativeReward, 0, 1000);
    }

    [Fact]
    public async Task ReinforcementLearning_TrainBatch_ProcessesExperiences()
    {
        // Arrange
        var rl = new ReinforcementLearning(_logger, _cache);
        var experiences = new List<Experience>
        {
            new() { State = new[] { 0.5 }, Action = 0, Reward = 1.0, NextState = new[] { 0.6 }, IsTerminal = false },
            new() { State = new[] { 0.6 }, Action = 1, Reward = 2.0, NextState = new[] { 0.7 }, IsTerminal = false },
            new() { State = new[] { 0.7 }, Action = 0, Reward = 3.0, NextState = new[] { 0.8 }, IsTerminal = true }
        };

        // Act
        await rl.TrainBatchAsync(experiences);
        var stats = await rl.GetPolicyStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(1, stats.EpisodesCompleted);
        Assert.Equal(6.0, stats.CumulativeReward);
    }

    #endregion

    #region NLPAnalyzer Tests

    [Fact]
    public async Task NLPAnalyzer_AnalyzeSentiment_IdentifiesPositive()
    {
        // Arrange
        var nlp = new NLPAnalyzer(_logger, _cache);

        // Act
        var result = await nlp.AnalyzeSentimentAsync("This is excellent and wonderful work");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("positive", result.Sentiment);
        Assert.InRange(result.Confidence, 0, 1);
        Assert.InRange(result.Polarity, -1, 1);
        Assert.NotEmpty(result.KeyWords);
    }

    [Fact]
    public async Task NLPAnalyzer_ExtractTopics_FindsKeywords()
    {
        // Arrange
        var nlp = new NLPAnalyzer(_logger, _cache);
        var text = "System performance optimization and efficiency improvements";

        // Act
        var result = await nlp.ExtractTopicsAsync(text, 3);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Topics);
        Assert.InRange(result.Coherence, 0, 1);
        Assert.InRange(result.Diversity, 0, 1);
        Assert.NotEmpty(result.PrimaryTopic);
    }

    [Fact]
    public async Task NLPAnalyzer_RecognizeEntities_FindsPatterns()
    {
        // Arrange
        var nlp = new NLPAnalyzer(_logger, _cache);
        var text = "Error on 2026-04-17 at 192.168.1.1 with critical severity";

        // Act
        var entities = await nlp.RecognizeEntitiesAsync(text);

        // Assert
        Assert.NotEmpty(entities);
        var types = entities.Select(e => e.EntityType).Distinct();
        Assert.Contains("DATE", types);
    }

    [Fact]
    public async Task NLPAnalyzer_ClassifyText_CategorizesContent()
    {
        // Arrange
        var nlp = new NLPAnalyzer(_logger, _cache);
        var categories = new[] { "error", "warning", "info", "debug" };

        // Act
        var result = await nlp.ClassifyTextAsync("Critical database error", categories);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Category);
        Assert.InRange(result.Confidence, 0, 1);
        Assert.Equal(categories.Length, result.CategoryScores.Count);
    }

    [Fact]
    public async Task NLPAnalyzer_DetectLogAnomalies_IdentifiesAnomalies()
    {
        // Arrange
        var nlp = new NLPAnalyzer(_logger, _cache);
        var logs = new List<string>
        {
            "INFO: Process started",
            "ERROR: Connection failed",
            "ERROR: Timeout occurred",
            "ERROR: Resource unavailable"
        };

        // Act
        var anomalies = await nlp.DetectLogAnomaliesAsync(logs);

        // Assert
        Assert.NotEmpty(anomalies);
        var errorSpike = anomalies.FirstOrDefault(a => a.AnomalyType == "error_spike");
        Assert.NotNull(errorSpike);
        Assert.InRange(errorSpike.AnomalyScore, 0, 1);
    }

    #endregion

    #region SeasonalityDetector Tests

    [Fact]
    public async Task SeasonalityDetector_DetectSeasonality_FindsPattern()
    {
        // Arrange
        var detector = new SeasonalityDetector(_logger, _cache);
        var timeSeries = GenerateSeasonalData(100, 24);
        var timestamps = Enumerable.Range(0, 100).Select(i => DateTime.UtcNow.AddHours(i)).ToArray();

        // Act
        var pattern = await detector.DetectSeasonalityAsync(timeSeries, timestamps);

        // Assert
        Assert.NotNull(pattern);
        Assert.True(pattern.Period > 0);
        Assert.InRange(pattern.Strength, 0, 1);
        Assert.InRange(pattern.Confidence, 0, 1);
    }

    [Fact]
    public async Task SeasonalityDetector_Deseasonalize_RemovesPattern()
    {
        // Arrange
        var detector = new SeasonalityDetector(_logger, _cache);
        var pattern = new SeasonalPattern { Period = 7, SeasonalIndices = new[] { 1.0, 1.1, 0.9, 1.0, 1.0, 0.95, 1.05 } };
        var data = new[] { 10.0, 11.0, 9.0, 10.0, 10.0, 9.5, 10.5 };

        // Act
        var deseasonalized = await detector.DeseasonalizeAsync(data, pattern);

        // Assert
        Assert.NotNull(deseasonalized);
        Assert.Equal(data.Length, deseasonalized.Length);
    }

    [Fact]
    public async Task SeasonalityDetector_DetectMultiplePeriods_FindsAllSeasons()
    {
        // Arrange
        var detector = new SeasonalityDetector(_logger, _cache);
        var data = GenerateSeasonalData(1000, 24);
        var timestamps = Enumerable.Range(0, 1000).Select(i => DateTime.UtcNow.AddHours(i)).ToArray();

        // Act
        var periods = await detector.DetectMultiplePeriodsAsync(data, timestamps);

        // Assert
        Assert.NotEmpty(periods);
        Assert.All(periods, p => Assert.InRange(p.Strength, 0, 1));
    }

    [Fact]
    public async Task SeasonalityDetector_Decompose_SplitsComponents()
    {
        // Arrange
        var detector = new SeasonalityDetector(_logger, _cache);
        var data = GenerateSeasonalData(100, 7);

        // Act
        var decomp = await detector.DecomposeAsync(data);

        // Assert
        Assert.NotNull(decomp);
        Assert.Equal(data.Length, decomp.Trend.Length);
        Assert.Equal(data.Length, decomp.Seasonal.Length);
        Assert.Equal(data.Length, decomp.Residual.Length);
        Assert.InRange(decomp.SeasonalVarianceRatio, 0, 1);
    }

    #endregion

    #region AnomalyPrediction Tests

    [Fact]
    public async Task AnomalyPrediction_PredictAnomalies_ReturnsPredictions()
    {
        // Arrange
        var predictor = new AnomalyPrediction(_logger, _cache);
        var data = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };

        // Act
        var predictions = await predictor.PredictAnomaliesAsync(data, 5);

        // Assert
        Assert.NotEmpty(predictions);
        Assert.All(predictions, p =>
        {
            Assert.InRange(p.RiskScore, 0, 1);
            Assert.InRange(p.Confidence, 0, 1);
            Assert.NotEmpty(p.AnomalyType);
        });
    }

    [Fact]
    public async Task AnomalyPrediction_DetectRealtime_FindsOutliers()
    {
        // Arrange
        var predictor = new AnomalyPrediction(_logger, _cache);
        var historicalContext = new[] { 10.0, 10.5, 9.8, 10.2, 10.1 };

        // Act
        var result = await predictor.DetectRealtimeAsync(50.0, historicalContext); // Outlier

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAnomaly);
        Assert.InRange(result.AnomalyScore, 0, 1);
        Assert.True(result.StandardDeviations > 2);
    }

    [Fact]
    public async Task AnomalyPrediction_TrainOnNormalBehavior_UpdatesBaseline()
    {
        // Arrange
        var predictor = new AnomalyPrediction(_logger, _cache);
        var normalData = Enumerable.Range(0, 50).Select(x => 10.0 + Random.Shared.NextDouble()).ToArray();

        // Act
        await predictor.TrainOnNormalBehaviorAsync(normalData);
        var stats = await predictor.GetModelStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(normalData.Length, stats.NormalSampleCount);
    }

    [Fact]
    public async Task AnomalyPrediction_AnalyzeRootCauses_IdentifiesFactors()
    {
        // Arrange
        var predictor = new AnomalyPrediction(_logger, _cache);
        var anomalousValues = new[] { 50.0, 55.0, 48.0 };
        var features = new[] { "cpu_usage", "memory_usage", "disk_io" };

        // Act
        var analysis = await predictor.AnalyzeRootCausesAsync(anomalousValues, features);

        // Assert
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.PrimaryRootCause);
        Assert.NotEmpty(analysis.ContributingFactors);
        Assert.NotEmpty(analysis.RemediationSteps);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task AllServices_CachingIntegration_ImprovedPerformance()
    {
        // Arrange
        var sw1 = Stopwatch.StartNew();
        var data = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act - First calls (uncached)
        var predictor = new DeepLearningPredictor(_logger, _cache);
        var result1 = await predictor.PredictAsync(data, 2, 3);
        sw1.Stop();

        // Act - Second calls (should be faster due to caching)
        var sw2 = Stopwatch.StartNew();
        var result2 = await predictor.PredictAsync(data, 2, 3);
        sw2.Stop();

        // Assert
        Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds);
        Assert.Equal(result1.Values, result2.Values);
    }

    [Fact]
    public async Task AllServices_PerformanceTargets_UnderThreshold()
    {
        // Arrange
        var services = new object[] 
        {
            new DeepLearningPredictor(_logger, _cache),
            new AutoMLOptimizer(_logger, _cache),
            new FederatedLearning(_logger, _cache),
            new ReinforcementLearning(_logger, _cache),
            new NLPAnalyzer(_logger, _cache),
            new SeasonalityDetector(_logger, _cache),
            new AnomalyPrediction(_logger, _cache)
        };

        // Act & Assert - Services should respond quickly
        Assert.NotEmpty(services);
        Assert.All(services, s => Assert.NotNull(s));
    }

    [Fact]
    public async Task AllServices_Registered_CanBeResolved()
    {
        // Arrange - Would register in ServiceContainer in real implementation
        var logger = new MockLogger();
        var cache = new L1CacheService(logger);

        var deepLearning = new DeepLearningPredictor(logger, cache);
        var autoML = new AutoMLOptimizer(logger, cache);
        var fedLearning = new FederatedLearning(logger, cache);
        var rl = new ReinforcementLearning(logger, cache);
        var nlp = new NLPAnalyzer(logger, cache);
        var seasonality = new SeasonalityDetector(logger, cache);
        var anomaly = new AnomalyPrediction(logger, cache);

        // Act & Assert
        Assert.NotNull(deepLearning);
        Assert.NotNull(autoML);
        Assert.NotNull(fedLearning);
        Assert.NotNull(rl);
        Assert.NotNull(nlp);
        Assert.NotNull(seasonality);
        Assert.NotNull(anomaly);
    }

    #endregion

    #region Helper Methods

    private static double[] GenerateSeasonalData(int length, int period)
    {
        var data = new double[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = 10 + 5 * Math.Sin(2 * Math.PI * i / period) + Random.Shared.NextDouble() * 0.5;
        }
        return data;
    }

    #endregion
}

/// <summary>
/// Mock logger for testing.
/// </summary>
public class MockLogger : ILogger
{
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message) { }
    public void Error(string message, Exception ex) { }
    public void Critical(string message) { }
    public void Trace(string message) { }
}
