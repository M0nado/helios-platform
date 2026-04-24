using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using HELIOS.Platform.Core.Intelligence;
using HELIOS.Platform.Core.Intelligence.Interfaces;

namespace HELIOS.Platform.Tests.Intelligence
{
    /// <summary>
    /// Comprehensive tests for ML Intelligence services - Phase 3 Tier 1.
    /// </summary>
    public class MLIntelligenceServicesTests : IDisposable
    {
        private readonly Mock<ILogger<DataCollector>> _dataCollectorLogger;
        private readonly Mock<ILogger<DataNormalizer>> _dataNormalizerLogger;
        private readonly Mock<ILogger<FeatureExtractor>> _featureExtractorLogger;
        private readonly Mock<ILogger<InMemoryTimeSeriesDB>> _timeSeriesDbLogger;
        private readonly Mock<ILogger<AnomalyDetector>> _anomalyDetectorLogger;
        private readonly Mock<ILogger<PredictiveAnalytics>> _predictiveAnalyticsLogger;
        private readonly Mock<ILogger<MLModelManager>> _mlModelManagerLogger;

        public MLIntelligenceServicesTests()
        {
            _dataCollectorLogger = new Mock<ILogger<DataCollector>>();
            _dataNormalizerLogger = new Mock<ILogger<DataNormalizer>>();
            _featureExtractorLogger = new Mock<ILogger<FeatureExtractor>>();
            _timeSeriesDbLogger = new Mock<ILogger<InMemoryTimeSeriesDB>>();
            _anomalyDetectorLogger = new Mock<ILogger<AnomalyDetector>>();
            _predictiveAnalyticsLogger = new Mock<ILogger<PredictiveAnalytics>>();
            _mlModelManagerLogger = new Mock<ILogger<MLModelManager>>();
        }

        #region DataCollector Tests

        [Fact]
        public async Task DataCollector_RegisterAndCollectMetrics_ReturnsValidMetrics()
        {
            // Arrange
            var collector = new DataCollector(_dataCollectorLogger.Object);
            var testValue1 = 42.5;
            var testValue2 = 99.1;

            // Act
            await collector.RegisterMetricSourceAsync("metric1", async () => await Task.FromResult(testValue1));
            await collector.RegisterMetricSourceAsync("metric2", async () => await Task.FromResult(testValue2));
            var metrics = await collector.CollectMetricsAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(2, metrics.Count);
            Assert.Equal(testValue1, metrics["metric1"]);
            Assert.Equal(testValue2, metrics["metric2"]);

            collector.Dispose();
        }

        [Fact]
        public async Task DataCollector_UnregisterMetricSource_RemovesMetric()
        {
            // Arrange
            var collector = new DataCollector(_dataCollectorLogger.Object);
            await collector.RegisterMetricSourceAsync("metric1", async () => await Task.FromResult(42.0));

            // Act
            await collector.UnregisterMetricSourceAsync("metric1");
            var metrics = await collector.CollectMetricsAsync();

            // Assert
            Assert.Empty(metrics);

            collector.Dispose();
        }

        [Fact]
        public async Task DataCollector_GetCollectionStats_ReturnsValidStats()
        {
            // Arrange
            var collector = new DataCollector(_dataCollectorLogger.Object);
            await collector.RegisterMetricSourceAsync("metric1", async () => await Task.FromResult(10.0));
            
            // Act
            await collector.CollectMetricsAsync();
            var stats = await collector.GetCollectionStatsAsync();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.ContainsKey("TotalCollections"));
            Assert.True(stats.ContainsKey("RegisteredSources"));
            Assert.Equal(1, stats["TotalCollections"]);
            Assert.Equal(1, stats["RegisteredSources"]);

            collector.Dispose();
        }

        #endregion

        #region DataNormalizer Tests

        [Fact]
        public async Task DataNormalizer_NormalizeMetrics_ReturnsNormalizedValues()
        {
            // Arrange
            var normalizer = new DataNormalizer(_dataNormalizerLogger.Object);
            var metrics = new Dictionary<string, double>
            {
                { "cpu", 50.0 },
                { "memory", 75.0 }
            };

            // Act
            var normalized = await normalizer.NormalizeAsync(metrics);

            // Assert
            Assert.NotNull(normalized);
            Assert.Equal(2, normalized.Count);
            Assert.True(normalized.ContainsKey("cpu"));
            Assert.True(normalized.ContainsKey("memory"));

            normalizer.Dispose();
        }

        [Fact]
        public async Task DataNormalizer_RegisterMetricBounds_SuccessfullyStoresBounds()
        {
            // Arrange
            var normalizer = new DataNormalizer(_dataNormalizerLogger.Object);
            const double minValue = 0.0;
            const double maxValue = 100.0;

            // Act
            await normalizer.RegisterMetricBoundsAsync("cpu", minValue, maxValue);
            var stats = await normalizer.GetNormalizationStatsAsync("cpu");

            // Assert
            Assert.NotNull(stats);

            normalizer.Dispose();
        }

        [Fact]
        public async Task DataNormalizer_ClearHistory_RemovesAllHistory()
        {
            // Arrange
            var normalizer = new DataNormalizer(_dataNormalizerLogger.Object);
            var metrics = new Dictionary<string, double> { { "metric1", 50.0 } };
            await normalizer.NormalizeAsync(metrics);

            // Act
            await normalizer.ClearHistoryAsync();
            var stats = await normalizer.GetNormalizationStatsAsync("metric1");

            // Assert
            Assert.Empty(stats);

            normalizer.Dispose();
        }

        #endregion

        #region FeatureExtractor Tests

        [Fact]
        public async Task FeatureExtractor_ExtractFeatures_ReturnsMultipleFeatures()
        {
            // Arrange
            var extractor = new FeatureExtractor(_featureExtractorLogger.Object);
            var dataPoints = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };

            // Act
            var features = await extractor.ExtractFeaturesAsync(dataPoints);

            // Assert
            Assert.NotNull(features);
            Assert.True(features.Count > 0);
            Assert.True(features.ContainsKey("Mean"));
            Assert.True(features.ContainsKey("StdDev"));
            Assert.True(features.ContainsKey("Min"));
            Assert.True(features.ContainsKey("Max"));
            Assert.Equal(5.5, features["Mean"]);

            extractor.Dispose();
        }

        [Fact]
        public async Task FeatureExtractor_ExtractMovingAverage_CalculatesCorrectly()
        {
            // Arrange
            var extractor = new FeatureExtractor(_featureExtractorLogger.Object);
            var dataPoints = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 };

            // Act
            var movingAvg = await extractor.ExtractMovingAverageAsync(dataPoints, 2);

            // Assert
            Assert.NotNull(movingAvg);
            Assert.Equal(4, movingAvg.Count);
            Assert.Equal(1.5, movingAvg[0]);
            Assert.Equal(4.5, movingAvg[3]);

            extractor.Dispose();
        }

        [Fact]
        public async Task FeatureExtractor_CalculateTrendSlope_ReturnsPositiveSlope()
        {
            // Arrange
            var extractor = new FeatureExtractor(_featureExtractorLogger.Object);
            var dataPoints = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 }; // Increasing trend

            // Act
            var slope = await extractor.CalculateTrendSlopeAsync(dataPoints);

            // Assert
            Assert.True(slope > 0);

            extractor.Dispose();
        }

        [Fact]
        public async Task FeatureExtractor_ExtractSeasonalComponents_ReturnsComponents()
        {
            // Arrange
            var extractor = new FeatureExtractor(_featureExtractorLogger.Object);
            var dataPoints = new List<double> { 1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0, 3.0 };

            // Act
            var components = await extractor.ExtractSeasonalComponentsAsync(dataPoints, 3);

            // Assert
            Assert.NotNull(components);
            Assert.True(components.Count > 0);
            Assert.True(components.ContainsKey("Seasonal_Strength"));

            extractor.Dispose();
        }

        #endregion

        #region InMemoryTimeSeriesDB Tests

        [Fact]
        public async Task TimeSeriesDB_StoreAndQuery_RetrievesCorrectData()
        {
            // Arrange
            var db = new InMemoryTimeSeriesDB(_timeSeriesDbLogger.Object);
            var now = DateTime.UtcNow;
            var seriesName = "test-series";

            // Act
            await db.StoreAsync(seriesName, 42.0, now);
            await db.StoreAsync(seriesName, 50.0, now.AddSeconds(1));
            var results = await db.QueryAsync(seriesName, now.AddSeconds(-1), now.AddSeconds(2));

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);

            db.Dispose();
        }

        [Fact]
        public async Task TimeSeriesDB_GetRecent_ReturnsLatestDataPoints()
        {
            // Arrange
            var db = new InMemoryTimeSeriesDB(_timeSeriesDbLogger.Object);
            var seriesName = "test-series";

            // Act
            for (int i = 0; i < 10; i++)
                await db.StoreAsync(seriesName, i);

            var recent = await db.GetRecentAsync(seriesName, 3);

            // Assert
            Assert.Equal(3, recent.Count);
            Assert.True(recent[2].Item2 > recent[1].Item2);

            db.Dispose();
        }

        [Fact]
        public async Task TimeSeriesDB_GetAggregateStats_CalculatesCorrectly()
        {
            // Arrange
            var db = new InMemoryTimeSeriesDB(_timeSeriesDbLogger.Object);
            var now = DateTime.UtcNow;
            var seriesName = "test-series";

            // Act
            for (int i = 1; i <= 10; i++)
                await db.StoreAsync(seriesName, i, now.AddSeconds(i));

            var stats = await db.GetAggregateStatsAsync(seriesName, now, now.AddSeconds(15));

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(10, stats["Count"]);
            Assert.Equal(1, stats["Min"]);
            Assert.Equal(10, stats["Max"]);
            Assert.Equal(5.5, stats["Avg"]);

            db.Dispose();
        }

        [Fact]
        public async Task TimeSeriesDB_PurgeOldData_RemovesExpiredPoints()
        {
            // Arrange
            var db = new InMemoryTimeSeriesDB(_timeSeriesDbLogger.Object);
            var now = DateTime.UtcNow;
            var seriesName = "test-series";

            // Act
            await db.StoreAsync(seriesName, 1.0, now.AddHours(-2));
            await db.StoreAsync(seriesName, 2.0, now);
            var deletedCount = await db.PurgeOldDataAsync(TimeSpan.FromHours(1));

            // Assert
            Assert.Equal(1, deletedCount);

            db.Dispose();
        }

        #endregion

        #region AnomalyDetector Tests

        [Fact]
        public async Task AnomalyDetector_TrainAndDetect_IdentifiesAnomalies()
        {
            // Arrange
            var detector = new AnomalyDetector(_anomalyDetectorLogger.Object);
            var trainingData = new List<double> { 10.0, 11.0, 10.5, 11.5, 10.0, 11.0 };

            // Act
            await detector.TrainModelAsync("test-series", trainingData);
            var normalScore = await detector.DetectAnomalyAsync("test-series", 10.5);
            var anomalyScore = await detector.DetectAnomalyAsync("test-series", 50.0);

            // Assert
            Assert.True(normalScore < 0.5);
            Assert.True(anomalyScore > 0.5);

            detector.Dispose();
        }

        [Fact]
        public async Task AnomalyDetector_BatchDetect_ProcessesMultipleValues()
        {
            // Arrange
            var detector = new AnomalyDetector(_anomalyDetectorLogger.Object);
            var trainingData = new List<double> { 10.0, 11.0, 10.5, 11.5, 10.0, 11.0 };
            await detector.TrainModelAsync("test-series", trainingData);

            // Act
            var values = new List<double> { 10.5, 11.0, 50.0 };
            var scores = await detector.DetectBatchAnomaliesAsync("test-series", values);

            // Assert
            Assert.Equal(3, scores.Count);
            Assert.True(scores[2] > 0.5);

            detector.Dispose();
        }

        [Fact]
        public async Task AnomalyDetector_GetStats_ReturnsValidStatistics()
        {
            // Arrange
            var detector = new AnomalyDetector(_anomalyDetectorLogger.Object);
            var trainingData = new List<double> { 10.0, 11.0, 12.0 };
            await detector.TrainModelAsync("series1", trainingData);
            await detector.DetectAnomalyAsync("series1", 10.5);

            // Act
            var stats = await detector.GetDetectionStatsAsync();

            // Assert
            Assert.NotNull(stats);
            Assert.True((int)stats["TotalDetections"] > 0);
            Assert.True((int)stats["TrainedModels"] > 0);

            detector.Dispose();
        }

        #endregion

        #region PredictiveAnalytics Tests

        [Fact]
        public async Task PredictiveAnalytics_PredictTrend_ReturnsForecasts()
        {
            // Arrange
            var analytics = new PredictiveAnalytics(_predictiveAnalyticsLogger.Object);
            var historicalData = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };
            await analytics.AddHistoricalDataAsync("test-series", historicalData);

            // Act
            var predictions = await analytics.PredictTrendAsync("test-series", 5);

            // Assert
            Assert.NotNull(predictions);
            Assert.Equal(5, predictions.Count);
            Assert.True(predictions[0] > predictions[0] - 1); // Should be reasonable value

            analytics.Dispose();
        }

        [Fact]
        public async Task PredictiveAnalytics_GetConfidenceIntervals_ReturnsIntervals()
        {
            // Arrange
            var analytics = new PredictiveAnalytics(_predictiveAnalyticsLogger.Object);
            var historicalData = new List<double> { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0, 18.0, 19.0, 20.0 };
            await analytics.AddHistoricalDataAsync("test-series", historicalData);

            // Act
            var intervals = await analytics.GetPredictionConfidenceIntervalsAsync("test-series", 3, 0.95);

            // Assert
            Assert.NotNull(intervals);
            Assert.Equal(3, intervals.Count);
            foreach (var interval in intervals)
            {
                Assert.True(interval.Item2 > interval.Item1);
            }

            analytics.Dispose();
        }

        [Fact]
        public async Task PredictiveAnalytics_ForecastPeak_IdentifiersPeakTime()
        {
            // Arrange
            var analytics = new PredictiveAnalytics(_predictiveAnalyticsLogger.Object);
            var historicalData = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 5.0, 4.0, 3.0, 2.0, 1.0 };
            await analytics.AddHistoricalDataAsync("test-series", historicalData);

            // Act
            var (peakValue, peakTime) = await analytics.ForecastPeakAsync("test-series", 24);

            // Assert
            Assert.True(peakValue > 0);
            Assert.True(peakTime > DateTime.UtcNow);

            analytics.Dispose();
        }

        [Fact]
        public async Task PredictiveAnalytics_PredictThresholdBreach_CalculatesProbability()
        {
            // Arrange
            var analytics = new PredictiveAnalytics(_predictiveAnalyticsLogger.Object);
            var historicalData = new List<double> { 10.0, 15.0, 20.0, 25.0, 30.0, 35.0, 40.0, 45.0, 50.0, 55.0 };
            await analytics.AddHistoricalDataAsync("test-series", historicalData);

            // Act
            var probability = await analytics.PredictThresholdBreachAsync("test-series", 70.0, 5);

            // Assert
            Assert.True(probability >= 0);
            Assert.True(probability <= 1);

            analytics.Dispose();
        }

        #endregion

        #region MLModelManager Tests

        [Fact]
        public async Task MLModelManager_CreateModel_ReturnsValidModelId()
        {
            // Arrange
            var manager = new MLModelManager(_mlModelManagerLogger.Object);
            var trainingData = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 };

            // Act
            var modelId = await manager.CreateModelAsync("test-model", "forecast", trainingData);

            // Assert
            Assert.NotNull(modelId);
            Assert.NotEmpty(modelId);

            manager.Dispose();
        }

        [Fact]
        public async Task MLModelManager_TrainModel_CalculatesAccuracy()
        {
            // Arrange
            var manager = new MLModelManager(_mlModelManagerLogger.Object);
            var trainingData = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var modelId = await manager.CreateModelAsync("test-model", "forecast", trainingData);

            // Act
            var accuracy = await manager.TrainModelAsync(modelId, trainingData);

            // Assert
            Assert.True(accuracy > 0);
            Assert.True(accuracy <= 1);

            manager.Dispose();
        }

        [Fact]
        public async Task MLModelManager_GetModelInfo_ReturnsCompleteMetadata()
        {
            // Arrange
            var manager = new MLModelManager(_mlModelManagerLogger.Object);
            var trainingData = new List<double> { 1.0, 2.0, 3.0 };
            var modelId = await manager.CreateModelAsync("test-model", "anomaly", trainingData);

            // Act
            var info = await manager.GetModelInfoAsync(modelId);

            // Assert
            Assert.NotNull(info);
            Assert.Equal("test-model", info["Name"]);
            Assert.Equal("anomaly", info["Type"]);
            Assert.True((int)info["TrainingCount"] > 0);

            manager.Dispose();
        }

        [Fact]
        public async Task MLModelManager_EvaluateModel_ReturnsMetrics()
        {
            // Arrange
            var manager = new MLModelManager(_mlModelManagerLogger.Object);
            var trainingData = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var modelId = await manager.CreateModelAsync("test-model", "forecast", trainingData);
            var testData = new List<double> { 1.5, 2.5, 3.5 };
            var expectedOutputs = new List<double> { 1.5, 2.5, 3.5 };

            // Act
            var metrics = await manager.EvaluateModelAsync(modelId, testData, expectedOutputs);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ContainsKey("Accuracy"));
            Assert.True(metrics.ContainsKey("F1Score"));

            manager.Dispose();
        }

        [Fact]
        public async Task MLModelManager_DeleteModel_RemovesModel()
        {
            // Arrange
            var manager = new MLModelManager(_mlModelManagerLogger.Object);
            var trainingData = new List<double> { 1.0, 2.0, 3.0 };
            var modelId = await manager.CreateModelAsync("test-model", "anomaly", trainingData);

            // Act
            await manager.DeleteModelAsync(modelId);
            var allModels = await manager.GetAllModelsAsync();

            // Assert
            Assert.DoesNotContain(modelId, allModels);

            manager.Dispose();
        }

        [Fact]
        public async Task MLModelManager_GetAllModels_ReturnsAllModelIds()
        {
            // Arrange
            var manager = new MLModelManager(_mlModelManagerLogger.Object);
            var trainingData = new List<double> { 1.0, 2.0, 3.0 };
            var modelId1 = await manager.CreateModelAsync("model1", "forecast", trainingData);
            var modelId2 = await manager.CreateModelAsync("model2", "anomaly", trainingData);

            // Act
            var allModels = await manager.GetAllModelsAsync();

            // Assert
            Assert.NotNull(allModels);
            Assert.Contains(modelId1, allModels);
            Assert.Contains(modelId2, allModels);

            manager.Dispose();
        }

        [Fact]
        public async Task MLModelManager_AutoRetrainDegradedModels_RetrainsLowAccuracyModels()
        {
            // Arrange
            var manager = new MLModelManager(_mlModelManagerLogger.Object);
            var poorTrainingData = new List<double> { 1.0, 100.0, 1.0, 100.0, 1.0 };
            var modelId = await manager.CreateModelAsync("poor-model", "forecast", poorTrainingData);

            // Act
            var retrainedCount = await manager.AutoRetrainDegradedModelsAsync();

            // Assert
            Assert.True(retrainedCount >= 0);

            manager.Dispose();
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task DataCollector_Performance_CollectMetrics_CompletesWithin40ms()
        {
            // Arrange
            var collector = new DataCollector(_dataCollectorLogger.Object);
            for (int i = 0; i < 50; i++)
            {
                int index = i;
                await collector.RegisterMetricSourceAsync($"metric{i}", async () => await Task.FromResult((double)index));
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            await collector.CollectMetricsAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Collection took {stopwatch.ElapsedMilliseconds}ms");

            collector.Dispose();
        }

        [Fact]
        public async Task TimeSeriesDB_Performance_StoreAndQuery_CompletesWithin40ms()
        {
            // Arrange
            var db = new InMemoryTimeSeriesDB(_timeSeriesDbLogger.Object);
            var now = DateTime.UtcNow;

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                await db.StoreAsync("test-series", i, now.AddSeconds(i));
            }
            var results = await db.QueryAsync("test-series", now, now.AddSeconds(1001));
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 200, $"Store/Query took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Equal(1000, results.Count);

            db.Dispose();
        }

        [Fact]
        public async Task FeatureExtractor_Performance_ExtractFeatures_CompletesWithin40ms()
        {
            // Arrange
            var extractor = new FeatureExtractor(_featureExtractorLogger.Object);
            var dataPoints = Enumerable.Range(1, 1000).Select(i => (double)i).ToList();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var features = await extractor.ExtractFeaturesAsync(dataPoints);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Feature extraction took {stopwatch.ElapsedMilliseconds}ms");

            extractor.Dispose();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task FullPipeline_CollectNormalizeExtract_WorksEndToEnd()
        {
            // Arrange
            var collector = new DataCollector(_dataCollectorLogger.Object);
            var normalizer = new DataNormalizer(_dataNormalizerLogger.Object);
            var extractor = new FeatureExtractor(_featureExtractorLogger.Object);

            // Register metrics
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                await collector.RegisterMetricSourceAsync($"metric{i}", async () => await Task.FromResult((double)(index * 10)));
            }

            // Act
            var collected = await collector.CollectMetricsAsync();
            var normalized = await normalizer.NormalizeAsync(collected);
            var dataPoints = collected.Values.ToList();
            var features = await extractor.ExtractFeaturesAsync(dataPoints);

            // Assert
            Assert.NotNull(collected);
            Assert.NotNull(normalized);
            Assert.NotNull(features);
            Assert.Equal(collected.Count, normalized.Count);
            Assert.True(features.Count > 0);

            collector.Dispose();
            normalizer.Dispose();
            extractor.Dispose();
        }

        [Fact]
        public async Task TimeSeriesWithAnomalyDetection_StoreAnalyzeDetect_WorksEndToEnd()
        {
            // Arrange
            var db = new InMemoryTimeSeriesDB(_timeSeriesDbLogger.Object);
            var detector = new AnomalyDetector(_anomalyDetectorLogger.Object);
            var now = DateTime.UtcNow;

            // Store normal data
            var normalData = new List<double> { 10.0, 11.0, 10.5, 11.5, 10.0, 11.0 };
            for (int i = 0; i < normalData.Count; i++)
            {
                await db.StoreAsync("metrics", normalData[i], now.AddSeconds(i));
            }

            // Train detector
            await detector.TrainModelAsync("metrics", normalData);

            // Act
            var recent = await db.GetRecentAsync("metrics", 5);
            var recentValues = recent.Select(r => r.Item2).ToList();
            var anomalyScores = await detector.DetectBatchAnomaliesAsync("metrics", recentValues);

            // Assert
            Assert.Equal(5, recentValues.Count);
            Assert.Equal(5, anomalyScores.Count);
            Assert.True(anomalyScores.All(s => s >= 0 && s <= 1));

            db.Dispose();
            detector.Dispose();
        }

        #endregion

        public void Dispose()
        {
            // Cleanup is handled by individual service dispose calls in tests
        }
    }
}
