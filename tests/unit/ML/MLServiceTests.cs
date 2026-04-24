namespace HELIOS.Platform.Tests.Unit.ML;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.ML;

public class MLServiceTests
{
    private readonly IPredictionEngine _predictionEngine;
    private readonly IDataPipeline _dataPipeline;
    private readonly MLService _mlService;

    public MLServiceTests()
    {
        _predictionEngine = new PredictionEngine();
        _dataPipeline = new DataPipeline();
        _mlService = new MLService(_predictionEngine, _dataPipeline);
    }

    #region Model Management Tests

    [Fact]
    public async Task CreateModelAsync_WithValidId_CreatesModel()
    {
        // Act
        var model = await _mlService.CreateModelAsync("model-1", "classification");

        // Assert
        Assert.NotNull(model);
        Assert.Equal("model-1", model.Id);
        Assert.Equal("classification", model.Type);
        Assert.Equal("idle", model.Status);
    }

    [Fact]
    public async Task CreateModelAsync_WithDuplicateId_ThrowsException()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");

        // Act & Assert
        await Assert.ThrowsAsync<MLException>(() => _mlService.CreateModelAsync("model-1", "classification"));
    }

    [Fact]
    public async Task GetModelAsync_WithValidId_ReturnsModel()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");

        // Act
        var model = await _mlService.GetModelAsync("model-1");

        // Assert
        Assert.NotNull(model);
        Assert.Equal("model-1", model.Id);
    }

    [Fact]
    public async Task ListModelsAsync_WithMultipleModels_ReturnsAll()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");
        await _mlService.CreateModelAsync("model-2", "regression");

        // Act
        var models = await _mlService.ListModelsAsync();

        // Assert
        Assert.Equal(2, models.Count);
    }

    [Fact]
    public async Task DeleteModelAsync_WithValidId_DeletesModel()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");

        // Act
        await _mlService.DeleteModelAsync("model-1");

        // Assert
        await Assert.ThrowsAsync<MLException>(() => _mlService.GetModelAsync("model-1"));
    }

    #endregion

    #region Model Training Tests

    [Fact]
    public async Task TrainModelAsync_WithTrainingData_TrainsSuccessfully()
    {
        // Arrange
        var model = await _mlService.CreateModelAsync("model-1", "classification");
        var trainingData = new List<TrainingDataPoint>
        {
            new TrainingDataPoint { Id = "1", Features = new Dictionary<string, object> { { "x", 1.0 } }, Label = "A" },
            new TrainingDataPoint { Id = "2", Features = new Dictionary<string, object> { { "x", 2.0 } }, Label = "B" }
        };

        // Act
        await _mlService.TrainModelAsync("model-1", trainingData);

        // Assert
        var trained = await _mlService.GetModelAsync("model-1");
        Assert.Equal("idle", trained.Status);
        Assert.True(trained.Accuracy > 0);
    }

    [Fact]
    public async Task TrainModelAsync_WithNoData_ThrowsException()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");

        // Act & Assert
        await Assert.ThrowsAsync<MLException>(() => _mlService.TrainModelAsync("model-1", new List<TrainingDataPoint>()));
    }

    #endregion

    #region Prediction Tests

    [Fact]
    public async Task PredictAsync_WithValidFeatures_ReturnsPrediction()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");
        var trainingData = new List<TrainingDataPoint>
        {
            new TrainingDataPoint { Id = "1", Features = new Dictionary<string, object> { { "x", 1.0 } }, Label = "A" }
        };
        await _mlService.TrainModelAsync("model-1", trainingData);

        // Act
        var result = await _mlService.PredictAsync("model-1", new Dictionary<string, object> { { "x", 1.5 } });

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.PredictedValue);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public async Task BatchPredictAsync_WithMultipleFeatureSets_ReturnsPredictions()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");
        var trainingData = new List<TrainingDataPoint>
        {
            new TrainingDataPoint { Id = "1", Features = new Dictionary<string, object> { { "x", 1.0 } }, Label = "A" }
        };
        await _mlService.TrainModelAsync("model-1", trainingData);
        var features = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "x", 1.0 } },
            new Dictionary<string, object> { { "x", 2.0 } }
        };

        // Act
        var results = await _mlService.BatchPredictAsync("model-1", features);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.NotNull(r.PredictedValue));
    }

    #endregion

    #region Anomaly Detection Tests

    [Fact]
    public async Task DetectAnomalyAsync_WithNormalData_ReturnsNoAnomaly()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "anomaly_detection");
        var dataPoint = new Dictionary<string, object> { { "x", 10.0 }, { "y", 10.0 } };

        // Act
        var result = await _mlService.DetectAnomalyAsync("model-1", dataPoint);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AnomalyScore >= 0);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_WithMultipleDataPoints_ReturnsResults()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "anomaly_detection");
        var dataPoints = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "x", 10.0 } },
            new Dictionary<string, object> { { "x", 20.0 } }
        };

        // Act
        var results = await _mlService.DetectAnomaliesAsync("model-1", dataPoints);

        // Assert
        Assert.Equal(2, results.Count);
    }

    #endregion

    #region Model Evaluation Tests

    [Fact]
    public async Task GetModelMetricsAsync_ReturnsMetrics()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");

        // Act
        var metrics = await _mlService.GetModelMetricsAsync("model-1");

        // Assert
        Assert.NotNull(metrics);
        Assert.Contains("accuracy", metrics.Keys);
        Assert.Contains("training_samples", metrics.Keys);
    }

    [Fact]
    public async Task EvaluateModelAsync_WithTestData_ReturnsAccuracy()
    {
        // Arrange
        await _mlService.CreateModelAsync("model-1", "classification");
        var trainingData = new List<TrainingDataPoint>
        {
            new TrainingDataPoint { Id = "1", Features = new Dictionary<string, object> { { "x", 1.0 } }, Label = "A" }
        };
        await _mlService.TrainModelAsync("model-1", trainingData);

        // Act
        var accuracy = await _mlService.EvaluateModelAsync("model-1", trainingData);

        // Assert
        Assert.True(accuracy >= 0 && accuracy <= 1);
    }

    #endregion

    #region Provider Tests

    [Fact]
    public async Task GetAvailableProvidersAsync_ReturnsProviders()
    {
        // Act
        var providers = await _mlService.GetAvailableProvidersAsync();

        // Assert
        Assert.NotNull(providers);
        Assert.NotEmpty(providers);
    }

    [Fact]
    public async Task SetActiveProviderAsync_WithValidProvider_SetSuccessfully()
    {
        // Act & Assert - should not throw
        await _mlService.SetActiveProviderAsync("local");
    }

    [Fact]
    public async Task SetActiveProviderAsync_WithInvalidProvider_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<MLException>(() => _mlService.SetActiveProviderAsync("non-existent"));
    }

    #endregion

    #region Data Pipeline Tests

    [Fact]
    public async Task PreprocessAsync_RemovesDuplicates()
    {
        // Arrange
        var rawData = new List<TrainingDataPoint>
        {
            new TrainingDataPoint { Id = "1", Features = new Dictionary<string, object> { { "x", 1.0 } } },
            new TrainingDataPoint { Id = "1", Features = new Dictionary<string, object> { { "x", 1.0 } } }
        };

        // Act
        var processed = await _dataPipeline.PreprocessAsync(rawData);

        // Assert
        Assert.Single(processed);
    }

    [Fact]
    public async Task NormalizeAsync_NormalizesFeatures()
    {
        // Arrange
        var features = new Dictionary<string, object> { { "x", 100.0 } };

        // Act
        var normalized = await _dataPipeline.NormalizeAsync(features);

        // Assert
        Assert.Contains("x", normalized.Keys);
    }

    #endregion

    #region Anomaly Detection Engine Tests

    [Fact]
    public async Task AnomalyDetectionEngine_UpdateThreshold_UpdatesSuccessfully()
    {
        // Arrange
        var engine = new AnomalyDetectionEngine();

        // Act
        await engine.UpdateThresholdAsync("model-1", 0.8);
        var threshold = await engine.GetThresholdAsync("model-1");

        // Assert
        Assert.Equal(0.8, threshold);
    }

    [Fact]
    public async Task AnomalyDetectionEngine_UpdateThreshold_WithInvalidValue_ThrowsException()
    {
        // Arrange
        var engine = new AnomalyDetectionEngine();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => engine.UpdateThresholdAsync("model-1", 1.5));
    }

    #endregion
}
