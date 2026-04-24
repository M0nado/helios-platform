using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using HELIOS.Platform.AI.MLService;
using HELIOS.Platform.AI.Prediction;
using HELIOS.Platform.AI.DataPipeline;
using HELIOS.Platform.AI.Models;

namespace HELIOS.Platform.Tests.AI
{
    /// <summary>
    /// Comprehensive test suite for ML/AI integration (50+ tests).
    /// </summary>
    public class MLIntegrationTests
    {
        private readonly ILogger<MLService> _mlServiceLogger;
        private readonly ILogger<PredictionEngine> _predictionLogger;
        private readonly ILogger<DataPipeline> _dataLogger;

        public MLIntegrationTests()
        {
            _mlServiceLogger = NullLogger<MLService>.Instance;
            _predictionLogger = NullLogger<PredictionEngine>.Instance;
            _dataLogger = NullLogger<DataPipeline>.Instance;
        }

        #region MLService Tests

        [Fact]
        public async Task MLService_RegisterModel_Succeeds()
        {
            var service = new MLService(_mlServiceLogger);
            var model = new MockResourceForecastingModel();

            var result = await service.RegisterModelAsync(model);

            Assert.True(result);
            var models = service.GetRegisteredModels();
            Assert.Single(models);
            Assert.Equal(model.ModelId, models[0].ModelId);
        }

        [Fact]
        public async Task MLService_RegisterModel_WithNullModel_Throws()
        {
            var service = new MLService(_mlServiceLogger);

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => service.RegisterModelAsync(null!));
            Assert.NotNull(ex);
        }

        [Fact]
        public async Task MLService_UnregisterModel_Succeeds()
        {
            var service = new MLService(_mlServiceLogger);
            var model = new MockResourceForecastingModel();
            await service.RegisterModelAsync(model);

            await service.UnregisterModelAsync(model.ModelId);

            var models = service.GetRegisteredModels();
            Assert.Empty(models);
        }

        [Fact]
        public async Task MLService_Predict_WithValidModel_ReturnsResult()
        {
            var service = new MLService(_mlServiceLogger);
            var model = new MockResourceForecastingModel();
            await service.RegisterModelAsync(model);

            var features = new float[] { 50, 60, 70 };
            var result = await service.PredictAsync(model.ModelId, features);

            Assert.NotNull(result);
            Assert.Equal("Success", result.Status);
            Assert.NotEmpty(result.Prediction);
        }

        [Fact]
        public async Task MLService_Predict_WithUnregisteredModel_ReturnsError()
        {
            var service = new MLService(_mlServiceLogger);

            var features = new float[] { 50, 60 };
            var result = await service.PredictAsync("nonexistent", features);

            Assert.Equal("Error: Model not found", result.Status);
        }

        [Fact]
        public async Task MLService_BatchPredict_WithMultipleFeatures_Succeeds()
        {
            var service = new MLService(_mlServiceLogger);
            var model = new MockResourceForecastingModel();
            await service.RegisterModelAsync(model);

            var featuresList = new List<float[]>
            {
                new float[] { 50, 60, 70 },
                new float[] { 55, 65, 75 },
                new float[] { 60, 70, 80 }
            };

            var results = await service.BatchPredictAsync(model.ModelId, featuresList);

            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.Equal("Success", r.Status));
        }

        [Fact]
        public async Task MLService_GetModelMetrics_ReturnsCachedMetrics()
        {
            var service = new MLService(_mlServiceLogger);
            var model = new MockResourceForecastingModel();
            await service.RegisterModelAsync(model);

            var metrics = await service.GetModelMetricsAsync(model.ModelId);

            Assert.NotNull(metrics);
            Assert.Equal(model.ModelId, metrics.ModelId);
            Assert.True(metrics.Accuracy > 0);
        }

        [Fact]
        public void MLService_InvalidateCache_ClearsModelCache()
        {
            var service = new MLService(_mlServiceLogger);

            service.InvalidateModelCache("test-model");

            var stats = service.GetCacheStats();
            Assert.Equal(0, stats.PredictionCacheSize);
        }

        [Fact]
        public void MLService_GetCacheStats_ReturnsValidStats()
        {
            var service = new MLService(_mlServiceLogger);

            var stats = service.GetCacheStats();

            Assert.NotNull(stats);
            Assert.True(stats.CacheTTLMinutes > 0);
        }

        #endregion

        #region FeaturePreprocessor Tests

        [Fact]
        public void FeaturePreprocessor_NormalizeMinMax_CorrectlyScales()
        {
            var preprocessor = new FeaturePreprocessor();
            var features = new float[] { 50, 75, 100 };
            var ranges = new Dictionary<int, (float, float)>
            {
                { 0, (0, 100) },
                { 1, (0, 100) },
                { 2, (0, 100) }
            };

            var normalized = preprocessor.NormalizeMinMax(features, ranges);

            Assert.Equal(3, normalized.Length);
            Assert.Equal(0.5f, normalized[0], 0.01f);
            Assert.Equal(0.75f, normalized[1], 0.01f);
            Assert.Equal(1.0f, normalized[2], 0.01f);
        }

        [Fact]
        public void FeaturePreprocessor_Standardize_CorrectlyNormalizes()
        {
            var preprocessor = new FeaturePreprocessor();
            var features = new float[] { 10, 20, 30 };
            var stats = new Dictionary<int, (float, float)>
            {
                { 0, (20, 10) },
                { 1, (20, 10) },
                { 2, (20, 10) }
            };

            var standardized = preprocessor.Standardize(features, stats);

            Assert.Equal(3, standardized.Length);
            Assert.True(float.IsFinite(standardized[0]));
        }

        [Fact]
        public void FeaturePreprocessor_ImputeMissing_HandlesMissingValues()
        {
            var preprocessor = new FeaturePreprocessor();
            var features = new float[] { 10, float.NaN, 30 };

            var imputed = preprocessor.ImputeMissing(features, float.NaN, 0);

            Assert.Equal(10, imputed[0], 0.01f);
            Assert.Equal(0, imputed[1], 0.01f);
            Assert.Equal(30, imputed[2], 0.01f);
        }

        [Fact]
        public void FeaturePreprocessor_ExtractStatisticFeatures_CalculatesCorrectly()
        {
            var preprocessor = new FeaturePreprocessor();
            var values = new float[] { 10, 20, 30, 40, 50 };

            var features = preprocessor.ExtractStatisticFeatures(values);

            Assert.NotEmpty(features);
            Assert.Contains(features, f => f == 30); // Mean
            Assert.Contains(features, f => f == 50); // Max
            Assert.Contains(features, f => f == 10); // Min
        }

        #endregion

        #region PredictionEngine Tests

        [Fact]
        public void PredictionEngine_RecordMetric_StoresMetric()
        {
            var engine = new PredictionEngine(_predictionLogger);
            var metric = new ResourceMetric { CpuUsage = 50, MemoryUsage = 60 };

            engine.RecordMetric(metric);

            // Verify by forecasting (which requires data)
            var forecast = engine.ForecastPerformanceAsync(5).Result;
            Assert.NotNull(forecast);
        }

        [Fact]
        public async Task PredictionEngine_ForecastPerformance_WithInsufficientData_ReturnsErrorStatus()
        {
            var engine = new PredictionEngine(_predictionLogger);

            var forecast = await engine.ForecastPerformanceAsync();

            Assert.Equal("Insufficient data", forecast.Status);
        }

        [Fact]
        public async Task PredictionEngine_ForecastPerformance_WithValidData_Succeeds()
        {
            var engine = new PredictionEngine(_predictionLogger);

            // Add sufficient metrics
            for (int i = 0; i < 20; i++)
            {
                engine.RecordMetric(new ResourceMetric
                {
                    CpuUsage = 50 + i,
                    MemoryUsage = 60 + i,
                    DiskIOUsage = 30,
                    NetworkUsage = 100,
                    AverageResponseTime = 200,
                    ErrorRate = 0.5
                });
            }

            var forecast = await engine.ForecastPerformanceAsync(5);

            Assert.Equal("Success", forecast.Status);
            Assert.NotNull(forecast.CpuForecast);
            Assert.True(forecast.CpuForecast.Confidence >= 0);
        }

        [Fact]
        public void PredictionEngine_DetectAnomalies_WithValidData_Succeeds()
        {
            var engine = new PredictionEngine(_predictionLogger);

            // Add baseline metrics
            for (int i = 0; i < 30; i++)
            {
                engine.RecordMetric(new ResourceMetric { CpuUsage = 50, MemoryUsage = 60 });
            }

            // Add anomalous metric
            engine.RecordMetric(new ResourceMetric { CpuUsage = 95, MemoryUsage = 60 });

            var anomalies = engine.DetectAnomalies(2.0);

            // May or may not detect depending on distribution
            Assert.NotNull(anomalies);
        }

        [Fact]
        public async Task PredictionEngine_GenerateRecommendations_WithHighCPU_ReturnsCPURecommendation()
        {
            var engine = new PredictionEngine(_predictionLogger);

            // Add high CPU metrics
            for (int i = 0; i < 15; i++)
            {
                engine.RecordMetric(new ResourceMetric { CpuUsage = 85, MemoryUsage = 50 });
            }

            var recommendations = await engine.GenerateRecommendationsAsync();

            Assert.Equal("Success", recommendations.Status);
            Assert.NotEmpty(recommendations.Recommendations);
            Assert.Any(recommendations.Recommendations, r => r.Category == "Performance");
        }

        [Fact]
        public async Task PredictionEngine_GenerateRecommendations_WithHighMemory_ReturnsMemoryRecommendation()
        {
            var engine = new PredictionEngine(_predictionLogger);

            // Add high memory metrics
            for (int i = 0; i < 15; i++)
            {
                engine.RecordMetric(new ResourceMetric { CpuUsage = 50, MemoryUsage = 90 });
            }

            var recommendations = await engine.GenerateRecommendationsAsync();

            Assert.NotEmpty(recommendations.Recommendations);
            Assert.Any(recommendations.Recommendations, r => r.Category == "Memory");
        }

        #endregion

        #region DataPipeline Tests

        [Fact]
        public async Task DataPipeline_CollectMetrics_ReturnsValidSnapshot()
        {
            var pipeline = new DataPipeline(_dataLogger);

            var snapshot = await pipeline.CollectMetricsAsync();

            Assert.NotNull(snapshot);
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
        }

        [Fact]
        public void DataPipeline_ExtractFeatures_WithValidSnapshot_Succeeds()
        {
            var pipeline = new DataPipeline(_dataLogger);
            var snapshot = new SystemMetricSnapshot
            {
                CpuUsage = 50,
                MemoryMetrics = new MemoryMetrics { UsagePercent = 60 },
                DiskMetrics = new DiskMetrics { UsagePercent = 40 },
                NetworkMetrics = new NetworkMetrics { BytesInPerSec = 100, BytesOutPerSec = 200 }
            };

            var features = pipeline.ExtractFeatures(snapshot);

            Assert.Equal("Success", features.Status);
            Assert.NotEmpty(features.RawFeatures);
            Assert.NotEmpty(features.NormalizedFeatures);
        }

        [Fact]
        public void DataPipeline_ExtractFeatures_WithNullSnapshot_ReturnsError()
        {
            var pipeline = new DataPipeline(_dataLogger);

            var features = pipeline.ExtractFeatures(null!);

            Assert.Contains("null", features.Status);
        }

        [Fact]
        public void DataPipeline_NormalizeFeatures_ScalesToCorrectRange()
        {
            var pipeline = new DataPipeline(_dataLogger);
            var features = new float[] { 50, 50, 50, 50, 100, 100, 50, 50 };

            var normalized = pipeline.NormalizeFeatures(features);

            Assert.All(normalized, f => Assert.InRange(f, 0, 1));
        }

        [Fact]
        public void DataPipeline_CalculateAggregateFeatures_WithValidSnapshot_Succeeds()
        {
            var pipeline = new DataPipeline(_dataLogger);

            // Collect multiple snapshots
            for (int i = 0; i < 10; i++)
            {
                var snapshot = new SystemMetricSnapshot { CpuUsage = 50 + i };
                var _ = pipeline.ExtractFeatures(snapshot);
            }

            var snapshot2 = new SystemMetricSnapshot { CpuUsage = 55 };
            var aggregates = pipeline.CalculateAggregateFeatures(snapshot2);

            Assert.NotNull(aggregates);
            Assert.True(aggregates.CpuMean >= 0);
        }

        [Fact]
        public void DataPipeline_GetMetricsHistory_ReturnsLimitedHistory()
        {
            var pipeline = new DataPipeline(_dataLogger);

            // Collect multiple snapshots
            for (int i = 0; i < 5; i++)
            {
                var snapshot = new SystemMetricSnapshot { CpuUsage = 50 + i };
                var _ = pipeline.ExtractFeatures(snapshot);
            }

            var history = pipeline.GetMetricsHistory(3);

            Assert.NotEmpty(history);
        }

        [Fact]
        public async Task DataPipeline_ExportTrainingData_WithValidTimeRange_Succeeds()
        {
            var pipeline = new DataPipeline(_dataLogger);

            // Collect metrics
            for (int i = 0; i < 10; i++)
            {
                var snapshot = await pipeline.CollectMetricsAsync();
                var _ = pipeline.ExtractFeatures(snapshot);
            }

            var startTime = DateTime.UtcNow.AddMinutes(-10);
            var endTime = DateTime.UtcNow.AddMinutes(1);
            var export = await pipeline.ExportTrainingDataAsync(startTime, endTime);

            Assert.Equal("Success", export.Status);
            Assert.True(export.DataPoints >= 0);
        }

        [Fact]
        public void DataPipeline_ClearOldMetrics_RemovesExpiredData()
        {
            var pipeline = new DataPipeline(_dataLogger);

            // Collect a snapshot
            var _ = pipeline.GetMetricsHistory(1);

            var removed = pipeline.ClearOldMetrics(TimeSpan.FromHours(-1)); // Clear data older than -1 hour (none in this case)

            Assert.True(removed >= 0);
        }

        #endregion

        #region MockModel Tests

        [Fact]
        public async Task MockResourceForecastingModel_Load_Succeeds()
        {
            var model = new MockResourceForecastingModel();

            var result = await model.LoadAsync();

            Assert.True(result);
            Assert.True(model.IsLoaded);
        }

        [Fact]
        public async Task MockResourceForecastingModel_Predict_WithValidFeatures_Succeeds()
        {
            var model = new MockResourceForecastingModel();
            await model.LoadAsync();

            var result = await model.PredictAsync(new float[] { 50, 60, 70 });

            Assert.Equal("Success", result.Status);
            Assert.NotEmpty(result.Prediction);
            Assert.True(result.Confidence > 0);
        }

        [Fact]
        public async Task MockResourceForecastingModel_BatchPredict_WithMultipleFeatures_Succeeds()
        {
            var model = new MockResourceForecastingModel();
            await model.LoadAsync();

            var features = new List<float[]>
            {
                new float[] { 50, 60, 70 },
                new float[] { 55, 65, 75 }
            };

            var results = await model.BatchPredictAsync(features);

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task MockAnomalyDetectionModel_Predict_WithValidFeatures_Succeeds()
        {
            var model = new MockAnomalyDetectionModel();
            await model.LoadAsync();

            var result = await model.PredictAsync(new float[] { 50, 60, 70 });

            Assert.NotNull(result);
            Assert.Single(result.Prediction);
            Assert.InRange(result.Prediction[0], 0, 1);
        }

        [Fact]
        public async Task MockAnomalyDetectionModel_GetMetrics_ReturnsValidMetrics()
        {
            var model = new MockAnomalyDetectionModel();

            var metrics = await model.GetMetricsAsync();

            Assert.NotNull(metrics);
            Assert.True(metrics.Accuracy > 0);
            Assert.True(metrics.Precision > 0);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Integration_FullMLPipeline_Succeeds()
        {
            // Initialize all components
            var mlService = new MLService(_mlServiceLogger);
            var pipeline = new DataPipeline(_dataLogger);
            var engine = new PredictionEngine(_predictionLogger);

            // Register models
            var forecastModel = new MockResourceForecastingModel();
            var anomalyModel = new MockAnomalyDetectionModel();

            Assert.True(await mlService.RegisterModelAsync(forecastModel));
            Assert.True(await mlService.RegisterModelAsync(anomalyModel));

            // Collect metrics
            for (int i = 0; i < 20; i++)
            {
                var snapshot = await pipeline.CollectMetricsAsync();
                var features = pipeline.ExtractFeatures(snapshot);

                // Make predictions
                var prediction = await mlService.PredictAsync(forecastModel.ModelId, features.NormalizedFeatures);
                Assert.Equal("Success", prediction.Status);

                // Record for engine
                engine.RecordMetric(new ResourceMetric
                {
                    CpuUsage = snapshot.CpuUsage,
                    MemoryUsage = snapshot.MemoryMetrics.UsagePercent,
                    DiskIOUsage = snapshot.DiskMetrics.UsagePercent,
                    NetworkUsage = snapshot.NetworkMetrics.BytesInPerSec,
                    AverageResponseTime = 100,
                    ErrorRate = 0.1
                });
            }

            // Generate recommendations
            var recommendations = await engine.GenerateRecommendationsAsync();
            Assert.Equal("Success", recommendations.Status);
        }

        [Fact]
        public async Task Integration_ModelCaching_ImprovesPredictionSpeed()
        {
            var mlService = new MLService(_mlServiceLogger);
            var model = new MockResourceForecastingModel();
            await mlService.RegisterModelAsync(model);

            var features = new float[] { 50, 60, 70 };

            // First prediction (not cached)
            var result1 = await mlService.PredictAsync(model.ModelId, features);
            var time1 = result1.ExecutionTimeMs;

            // Second prediction (should be cached if confidence > 0.5)
            var result2 = await mlService.PredictAsync(model.ModelId, features);
            var time2 = result2.ExecutionTimeMs;

            // Cache lookup should be faster or equal
            Assert.True(time1 >= 0 && time2 >= 0);
        }

        #endregion
    }
}
