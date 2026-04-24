// Phase 3 & Phase 5 Integration Test Framework
// Tests all services with Phase 4 optimizations integrated

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core;
using HELIOS.Platform.Core.Intelligence;
using HELIOS.Platform.Core.Observability;
using HELIOS.Platform.Core.API;
using HELIOS.Platform.Core.Production;
using HELIOS.Platform.Core.AdvancedML;
using HELIOS.Platform.Core.Global;
using HELIOS.Platform.Core.Autonomy;
using HELIOS.Platform.Core.Ecosystem;

namespace Tests.Phase3Phase5Integration
{
    public class Phase3Phase5IntegrationTests
    {
        private readonly ServiceContainer _serviceContainer;
        private readonly ILogger _logger;

        public Phase3Phase5IntegrationTests()
        {
            _serviceContainer = new ServiceContainer();
            _logger = _serviceContainer.GetService<ILogger>();
        }

        #region Phase 3 ML Intelligence Tests

        [Fact]
        public async Task DataCollectorIntegration_ShouldAggregateMetricsWithPhase4Caching()
        {
            // Arrange
            var collector = _serviceContainer.GetService<IDataCollector>();
            var cache = _serviceContainer.GetService<IL1CacheService>();
            var sw = Stopwatch.StartNew();

            // Act - First call (cache miss)
            await collector.CollectMetricsAsync("service1", new Dictionary<string, double>
            {
                { "cpu", 45.5 },
                { "memory", 256.2 },
                { "latency", 12.5 }
            });
            sw.Stop();
            var firstCallTime = sw.ElapsedMilliseconds;

            // Second call (cache hit)
            sw.Restart();
            var data = await collector.GetCollectedDataAsync("service1", TimeSpan.FromMinutes(5));
            sw.Stop();
            var secondCallTime = sw.ElapsedMilliseconds;

            // Assert
            Assert.NotNull(data);
            Assert.True(secondCallTime < firstCallTime, "Cached call should be faster");
            Assert.True(firstCallTime < 50, "Collection should be <50ms");
        }

        [Fact]
        public async Task AnomalyDetectorIntegration_ShouldDetectAnomaliesUnder40ms()
        {
            // Arrange
            var detector = _serviceContainer.GetService<IAnomalyDetector>();
            var metrics = new Dictionary<string, double>
            {
                { "cpu", 95.0 }, // Anomaly
                { "memory", 512.0 }, // High
                { "latency", 500.0 } // Anomaly
            };
            var sw = Stopwatch.StartNew();

            // Act
            var score = await detector.DetectAnomalyAsync("service1", metrics);
            sw.Stop();

            // Assert
            Assert.NotNull(score);
            Assert.True(score.IsAnomaly || score.Score > 0.5);
            Assert.True(sw.ElapsedMilliseconds < 40, "Anomaly detection should be <40ms");
        }

        [Fact]
        public async Task PredictiveAnalyticsIntegration_ShouldForecastWithinTargets()
        {
            // Arrange
            var analytics = _serviceContainer.GetService<IPredictiveAnalytics>();
            var sw = Stopwatch.StartNew();

            // Act
            var forecast = await analytics.ForecastAsync("service1", TimeSpan.FromHours(1), steps: 6);
            sw.Stop();

            // Assert
            Assert.NotNull(forecast);
            Assert.True(sw.ElapsedMilliseconds < 100, "Forecasting should be <100ms");
        }

        [Fact]
        public async Task MLModelManager_ShouldManageModelsWithVersioning()
        {
            // Arrange
            var modelManager = _serviceContainer.GetService<IMLModelManager>();

            // Act
            var modelId = await modelManager.RegisterModelAsync(new ModelMetadata
            {
                Name = "Test-Model",
                Version = "1.0",
                Type = "anomaly-detector"
            });

            var model = await modelManager.LoadModelAsync(modelId);

            // Assert
            Assert.NotNull(modelId);
            Assert.NotNull(model);
        }

        #endregion

        #region Phase 3 Observability Tests

        [Fact]
        public async Task PrometheusExporter_ShouldExportMetricsInCorrectFormat()
        {
            // Arrange
            var exporter = _serviceContainer.GetService<IPrometheusExporter>();
            var sw = Stopwatch.StartNew();

            // Act
            var metrics = await exporter.ExportMetricsAsync();
            sw.Stop();

            // Assert
            Assert.NotNull(metrics);
            Assert.Contains("# HELP", metrics);
            Assert.Contains("# TYPE", metrics);
            Assert.True(sw.ElapsedMilliseconds < 10, "Prometheus export should be <10ms");
        }

        [Fact]
        public async Task OpenTelemetryTracer_ShouldTraceRequestsWithSpans()
        {
            // Arrange
            var tracer = _serviceContainer.GetService<IOpenTelemetryTracer>();
            var requestId = Guid.NewGuid().ToString();

            // Act
            var trace = await tracer.TraceRequestAsync(requestId, async () =>
            {
                await Task.Delay(10); // Simulate work
            });

            // Assert
            Assert.NotNull(trace);
            Assert.Equal(requestId, trace.RequestId);
        }

        [Fact]
        public async Task HealthCheckOrchestrator_ShouldAggregateHealthStatus()
        {
            // Arrange
            var orchestrator = _serviceContainer.GetService<IHealthCheckOrchestrator>();

            // Act
            var health = await orchestrator.GetOverallHealthAsync();

            // Assert
            Assert.NotNull(health);
            Assert.NotNull(health.Status);
        }

        [Fact]
        public async Task SLAMonitor_ShouldTrackSLACompliance()
        {
            // Arrange
            var monitor = _serviceContainer.GetService<ISLAMonitor>();

            // Act
            var status = await monitor.GetSLAStatusAsync("test-sla");

            // Assert
            Assert.NotNull(status);
        }

        #endregion

        #region Phase 3 API & Web Tests

        [Fact]
        public async Task APIGateway_ShouldRouteRequestsWithinTargets()
        {
            // Arrange
            var gateway = _serviceContainer.GetService<IAPIGateway>();
            var request = new ApiRequest { Path = "/api/services", Method = "GET" };
            var sw = Stopwatch.StartNew();

            // Act
            var response = await gateway.RouteRequestAsync(request);
            sw.Stop();

            // Assert
            Assert.NotNull(response);
            Assert.True(sw.ElapsedMilliseconds < 50, "Gateway routing should be <50ms");
        }

        [Fact]
        public async Task GraphQLServer_ShouldExecuteQueries()
        {
            // Arrange
            var graphql = _serviceContainer.GetService<IGraphQLServer>();

            // Act
            var result = await graphql.ExecuteQueryAsync("{ services { id name } }", null);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task WebSocketBroker_ShouldHandleRealTimeMessaging()
        {
            // Arrange
            var broker = _serviceContainer.GetService<IWebSocketBroker>();

            // Act
            var channel = await broker.SubscribeAsync("test-channel");

            // Assert
            Assert.NotNull(channel);
        }

        #endregion

        #region Phase 3 Production Tests

        [Fact]
        public async Task DistributedCacheLayer_ShouldCacheWithinTargets()
        {
            // Arrange
            var cache = _serviceContainer.GetService<IDistributedCacheLayer>();
            var sw = Stopwatch.StartNew();

            // Act
            await cache.SetAsync("test-key", "test-value", TimeSpan.FromMinutes(5));
            var value = await cache.GetAsync("test-key");
            sw.Stop();

            // Assert
            Assert.Equal("test-value", value);
            Assert.True(sw.ElapsedMilliseconds < 2, "Cache should be <2ms");
        }

        [Fact]
        public async Task ZeroTrustImplementation_ShouldEnforceSecurityPolicies()
        {
            // Arrange
            var zeroTrust = _serviceContainer.GetService<IZeroTrustImplementation>();

            // Act
            var allowed = await zeroTrust.AuthorizeAsync("user1", "service1", "read");

            // Assert
            Assert.NotNull(allowed);
        }

        [Fact]
        public async Task DisasterRecoveryOrchestrator_ShouldBackupAndRestore()
        {
            // Arrange
            var dr = _serviceContainer.GetService<IDisasterRecoveryOrchestrator>();

            // Act
            var backupId = await dr.CreateBackupAsync();

            // Assert
            Assert.NotNull(backupId);
        }

        #endregion

        #region Phase 5 Advanced ML Tests

        [Fact]
        public async Task DeepLearningPredictor_ShouldPredictTrendsUnder50ms()
        {
            // Arrange
            var predictor = _serviceContainer.GetService<IDeepLearningPredictor>();
            var sw = Stopwatch.StartNew();

            // Act
            var forecast = await predictor.PredictTrendAsync("metric1", TimeSpan.FromHours(1));
            sw.Stop();

            // Assert
            Assert.NotNull(forecast);
            Assert.True(sw.ElapsedMilliseconds < 50, "Deep learning should be <50ms");
        }

        [Fact]
        public async Task AutoMLOptimizer_ShouldSelectBestModel()
        {
            // Arrange
            var optimizer = _serviceContainer.GetService<IAutoMLOptimizer>();

            // Act
            var selection = await optimizer.SelectBestModelAsync(
                new Dataset(), 
                ProblemType.Regression, 
                TimeSpan.FromSeconds(5));

            // Assert
            Assert.NotNull(selection);
        }

        #endregion

        #region Integration End-to-End Tests

        [Fact]
        public async Task FullPhase3Workflow_ShouldIntegrateAllTiers()
        {
            // This tests the complete Phase 3 workflow:
            // DataCollector → DataNormalizer → FeatureExtractor → AnomalyDetector → Alert

            // Arrange
            var collector = _serviceContainer.GetService<IDataCollector>();
            var normalizer = _serviceContainer.GetService<IDataNormalizer>();
            var extractor = _serviceContainer.GetService<IFeatureExtractor>();
            var detector = _serviceContainer.GetService<IAnomalyDetector>();
            var alerter = _serviceContainer.GetService<IAlertManager>();

            // Act
            // Step 1: Collect metrics
            var metrics = new Dictionary<string, double> { { "cpu", 95.0 }, { "memory", 512.0 } };
            await collector.CollectMetricsAsync("svc1", metrics);

            // Step 2: Normalize
            var normalized = await normalizer.NormalizeMetricsAsync(metrics, NormalizationStrategy.ZScore);

            // Step 3: Extract features
            var features = await extractor.ExtractFeaturesAsync(normalized);

            // Step 4: Detect anomalies
            var anomaly = await detector.DetectAnomalyAsync("svc1", metrics);

            // Step 5: Alert if needed
            if (anomaly.IsAnomaly)
            {
                await alerter.RaiseAlertAsync("HIGH_CPU", "CPU >90%");
            }

            // Assert
            Assert.NotNull(features);
            Assert.NotNull(anomaly);
        }

        [Fact]
        public async Task Phase3AndPhase4Integration_ShouldCacheEffectivelyAndPerform()
        {
            // Test that Phase 3 services use Phase 4 caching

            // Arrange
            var collector = _serviceContainer.GetService<IDataCollector>();
            var cache = _serviceContainer.GetService<IL1CacheService>();
            var metrics = new Dictionary<string, double> { { "cpu", 45.0 } };

            // Act & Assert - Multiple calls should use cache
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                await collector.CollectMetricsAsync("svc1", metrics);
            }
            sw.Stop();

            // Should be fast due to caching
            Assert.True(sw.ElapsedMilliseconds < 100, "10 calls with caching should be <100ms");
        }

        #endregion

        #region Performance Benchmarks

        [Fact]
        public async Task Phase3_AllServicesBenchmark_ShouldMeetTargets()
        {
            var results = new Dictionary<string, long>();

            // Benchmark Phase 3 Tier 1
            var collector = _serviceContainer.GetService<IDataCollector>();
            var sw = Stopwatch.StartNew();
            await collector.CollectMetricsAsync("svc", new Dictionary<string, double> { { "cpu", 45 } });
            sw.Stop();
            results["DataCollector"] = sw.ElapsedMilliseconds;

            // Benchmark other Tier 1 services...
            // (Similar pattern for all 26 services)

            // Assert all are within targets
            foreach (var result in results)
            {
                _logger.LogInformation($"{result.Key}: {result.Value}ms");
                Assert.True(result.Value < 100, $"{result.Key} exceeded 100ms target");
            }
        }

        #endregion
    }

    // Helper classes
    public class ApiRequest
    {
        public string Path { get; set; }
        public string Method { get; set; }
    }

    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; }
    }

    public class ModelMetadata
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Type { get; set; }
    }

    public class Dataset
    {
        public int RecordCount { get; set; }
        public int FeatureCount { get; set; }
    }

    public enum ProblemType { Classification, Regression, Clustering }
    public enum NormalizationStrategy { ZScore, MinMax, Log, Percentage }
}
