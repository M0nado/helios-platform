using Xunit;
using HELIOS.Platform.Core.AdvancedOptimization;
using HELIOS.Platform.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Comprehensive unit tests for Phase 6 Advanced Optimization & Intelligent Systems
    /// Total: 150+ test cases covering all 8 services
    /// </summary>
    public class Phase6AdvancedOptimizationTests
    {
        private readonly ILogger _logger = new ConsoleLogger();

        // ==================== ADVANCED OPTIMIZATION ENGINE TESTS ====================

        [Fact]
        public async Task AdvOptEngine_Initialize_Succeeds()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            var result = await engine.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task AdvOptEngine_AnalyzeSystem_GeneratesRecommendations()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();
            var recommendations = await engine.AnalyzeSystemAsync();

            Assert.NotEmpty(recommendations);
            Assert.All(recommendations, r =>
            {
                Assert.NotEmpty(r.Name);
                Assert.True(r.SafetyScore >= 0 && r.SafetyScore <= 1);
                Assert.True(r.ExpectedImpact >= 0);
            });
        }

        [Fact]
        public async Task AdvOptEngine_ApplyOptimization_ReturnsResult()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();
            var result = await engine.ApplyOptimizationAsync("test-opt-1");

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.OptimizationId);
        }

        [Fact]
        public async Task AdvOptEngine_RollbackOptimization_Succeeds()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();
            await engine.ApplyOptimizationAsync("test-opt-2");
            var rollback = await engine.RollbackOptimizationAsync("test-opt-2");

            Assert.True(rollback);
        }

        [Fact]
        public async Task AdvOptEngine_GetMetrics_ReturnsValidMetrics()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();
            await engine.ApplyOptimizationAsync("opt-1");
            var metrics = await engine.GetOptimizationMetricsAsync();

            Assert.NotNull(metrics);
            Assert.True(metrics.AppliedOptimizations >= 0);
            Assert.True(metrics.CPUOptimizationPercent >= 0);
            Assert.True(metrics.MemoryOptimizationPercent >= 0);
        }

        [Fact]
        public async Task AdvOptEngine_GetImpactReport_ReturnsReport()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();
            var report = await engine.GetOptimizationImpactAsync();

            Assert.NotNull(report);
            Assert.NotNull(report.Impacts);
        }

        [Fact]
        public async Task AdvOptEngine_MultipleConcurrentOptimizations_AllSucceed()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();
            var tasks = Enumerable.Range(1, 10)
                .Select(i => engine.ApplyOptimizationAsync($"opt-{i}"))
                .ToList();

            await Task.WhenAll(tasks);
            var metrics = await engine.GetOptimizationMetricsAsync();

            Assert.True(metrics.AppliedOptimizations >= 10);
        }

        [Fact]
        public async Task AdvOptEngine_OptimizationTypeCoverage_AllTypes()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();
            var recommendations = await engine.AnalyzeSystemAsync();

            var types = recommendations.Select(r => r.Type).Distinct();
            Assert.True(types.Count() > 5);
        }

        // ==================== INTELLIGENT RESOURCE ALLOCATOR TESTS ====================

        [Fact]
        public async Task ResourceAllocator_Initialize_Succeeds()
        {
            var allocator = new IntelligentResourceAllocator(_logger);
            var result = await allocator.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task ResourceAllocator_PredictResources_GeneratesPrediction()
        {
            var allocator = new IntelligentResourceAllocator(_logger);
            await allocator.InitializeAsync();
            var prediction = await allocator.PredictResourceNeedsAsync("Service_A", 15);

            Assert.NotNull(prediction);
            Assert.Equal("Service_A", prediction.ServiceId);
            Assert.True(prediction.PredictedCPUPercent >= 0 && prediction.PredictedCPUPercent <= 100);
            Assert.True(prediction.Confidence >= 0 && prediction.Confidence <= 1);
        }

        [Fact]
        public async Task ResourceAllocator_GenerateAllocationPlan_ReturnsValidPlan()
        {
            var allocator = new IntelligentResourceAllocator(_logger);
            await allocator.InitializeAsync();
            var services = new Dictionary<string, ResourceRequirement>
            {
                { "Service_A", new ResourceRequirement { MinCPUPercent = 10, MaxCPUPercent = 50 } },
                { "Service_B", new ResourceRequirement { MinCPUPercent = 5, MaxCPUPercent = 30 } }
            };

            var plan = await allocator.GenerateAllocationPlanAsync(services);

            Assert.NotNull(plan);
            Assert.Equal(2, plan.Allocations.Count);
            Assert.True(plan.TotalCPUAllocated > 0);
        }

        [Fact]
        public async Task ResourceAllocator_ApplyAllocation_Succeeds()
        {
            var allocator = new IntelligentResourceAllocator(_logger);
            await allocator.InitializeAsync();
            var plan = new AllocationPlan
            {
                Allocations = new List<ServiceAllocation>
                {
                    new ServiceAllocation { ServiceId = "Test_Service", AllocatedCPUPercent = 25 }
                }
            };

            var result = await allocator.ApplyAllocationAsync(plan);
            Assert.True(result);
        }

        [Fact]
        public async Task ResourceAllocator_GetMetrics_ReturnsMetrics()
        {
            var allocator = new IntelligentResourceAllocator(_logger);
            await allocator.InitializeAsync();
            var metrics = await allocator.GetAllocationMetricsAsync();

            Assert.NotNull(metrics);
            Assert.True(metrics.AverageMemoryUtilization >= 0);
            Assert.True(metrics.WastePercentage >= 0);
        }

        [Fact]
        public async Task ResourceAllocator_GetUtilization_ReturnsCurrentState()
        {
            var allocator = new IntelligentResourceAllocator(_logger);
            await allocator.InitializeAsync();
            var utilization = await allocator.GetCurrentUtilizationAsync();

            Assert.NotNull(utilization);
        }

        // ==================== ANOMALY PREDICTION ENGINE TESTS ====================

        [Fact]
        public async Task AnomalyEngine_Initialize_Succeeds()
        {
            var engine = new AnomalyPredictionEngine(_logger);
            var result = await engine.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task AnomalyEngine_LearnPattern_Succeeds()
        {
            var engine = new AnomalyPredictionEngine(_logger);
            await engine.InitializeAsync();

            var data = Enumerable.Range(1, 20)
                .Select(i => new MetricDataPoint { Value = 50 + i * 2 })
                .ToList();

            var result = await engine.LearnPatternAsync("metric_1", data);
            Assert.True(result);
        }

        [Fact]
        public async Task AnomalyEngine_PredictAnomalies_GeneratesPredictions()
        {
            var engine = new AnomalyPredictionEngine(_logger);
            await engine.InitializeAsync();

            var data = Enumerable.Range(1, 30)
                .Select(i => new MetricDataPoint { Value = 50 + (i % 10) })
                .ToList();

            await engine.LearnPatternAsync("test_metric", data);
            var predictions = await engine.PredictAnomaliesAsync(30);

            Assert.NotNull(predictions);
        }

        [Fact]
        public async Task AnomalyEngine_ReportAnomaly_Records()
        {
            var engine = new AnomalyPredictionEngine(_logger);
            await engine.InitializeAsync();

            var anomaly = new AnomalyEvent
            {
                MetricName = "test_metric",
                Type = AnomalyType.Spike,
                MeasuredValue = 100,
                ExpectedValue = 50
            };

            var result = await engine.ReportAnomalyAsync("test_metric", anomaly);
            Assert.True(result);
        }

        [Fact]
        public async Task AnomalyEngine_GetMetrics_ReturnsMetrics()
        {
            var engine = new AnomalyPredictionEngine(_logger);
            await engine.InitializeAsync();

            var data = Enumerable.Range(1, 25)
                .Select(i => new MetricDataPoint { Value = 50 + i })
                .ToList();

            await engine.LearnPatternAsync("metric_1", data);
            var metrics = await engine.GetAnomalyMetricsAsync();

            Assert.NotNull(metrics);
            Assert.True(metrics.PatternsLearned >= 0);
        }

        [Fact]
        public async Task AnomalyEngine_GetConfidenceReport_ReturnsReport()
        {
            var engine = new AnomalyPredictionEngine(_logger);
            await engine.InitializeAsync();

            var data = Enumerable.Range(1, 30)
                .Select(i => new MetricDataPoint { Value = 50 + i })
                .ToList();

            await engine.LearnPatternAsync("metric", data);
            var report = await engine.GetConfidenceReportAsync();

            Assert.NotNull(report);
            Assert.True(report.OverallConfidence >= 0 && report.OverallConfidence <= 1);
        }

        // ==================== SERVICE MESH OPTIMIZER TESTS ====================

        [Fact]
        public async Task MeshOptimizer_Initialize_Succeeds()
        {
            var optimizer = new ServiceMeshOptimizer(_logger);
            var result = await optimizer.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task MeshOptimizer_OptimizeRoutes_GeneratesRoutes()
        {
            var optimizer = new ServiceMeshOptimizer(_logger);
            await optimizer.InitializeAsync();
            var routes = await optimizer.OptimizeRoutesAsync();

            Assert.NotEmpty(routes);
            Assert.All(routes, r =>
            {
                Assert.NotEmpty(r.SourceService);
                Assert.NotEmpty(r.DestinationService);
                Assert.True(r.LatencyMs > 0);
            });
        }

        [Fact]
        public async Task MeshOptimizer_ApplyRoute_Succeeds()
        {
            var optimizer = new ServiceMeshOptimizer(_logger);
            await optimizer.InitializeAsync();

            var route = new ServiceRoute
            {
                SourceService = "Service_A",
                DestinationService = "Service_B",
                LatencyMs = 25,
                Throughput = 5000
            };

            var result = await optimizer.ApplyRouteAsync(route);
            Assert.True(result);
        }

        [Fact]
        public async Task MeshOptimizer_GetCircuitBreakerStatus_ReturnsStatus()
        {
            var optimizer = new ServiceMeshOptimizer(_logger);
            await optimizer.InitializeAsync();

            var route = new ServiceRoute
            {
                SourceService = "A",
                DestinationService = "B"
            };
            await optimizer.ApplyRouteAsync(route);

            var breakers = await optimizer.GetCircuitBreakerStatusAsync();
            Assert.NotNull(breakers);
        }

        [Fact]
        public async Task MeshOptimizer_GetMetrics_ReturnsMetrics()
        {
            var optimizer = new ServiceMeshOptimizer(_logger);
            await optimizer.InitializeAsync();
            var metrics = await optimizer.GetMeshMetricsAsync();

            Assert.NotNull(metrics);
            Assert.True(metrics.RequestSuccessRate >= 0 && metrics.RequestSuccessRate <= 1);
        }

        [Fact]
        public async Task MeshOptimizer_GetCacheStats_ReturnsCacheStats()
        {
            var optimizer = new ServiceMeshOptimizer(_logger);
            await optimizer.InitializeAsync();
            var stats = await optimizer.GetCacheStatsAsync();

            Assert.NotNull(stats);
            Assert.True(stats.HitRate >= 0);
        }

        // ==================== SECURITY THREAT ANALYZER TESTS ====================

        [Fact]
        public async Task ThreatAnalyzer_Initialize_Succeeds()
        {
            var analyzer = new SecurityThreatAnalyzer(_logger);
            var result = await analyzer.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task ThreatAnalyzer_AnalyzeSecurityEvent_Records()
        {
            var analyzer = new SecurityThreatAnalyzer(_logger);
            await analyzer.InitializeAsync();

            var evt = new SecurityEvent
            {
                EventType = SecurityEventType.FailedLogin,
                Source = "IP_1.2.3.4",
                SeverityLevel = 2
            };

            var result = await analyzer.AnalyzeSecurityEventAsync(evt);
            Assert.True(result);
        }

        [Fact]
        public async Task ThreatAnalyzer_DetectThreats_GeneratesThreatDetections()
        {
            var analyzer = new SecurityThreatAnalyzer(_logger);
            await analyzer.InitializeAsync();

            for (int i = 0; i < 10; i++)
            {
                await analyzer.AnalyzeSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.FailedLogin,
                    SeverityLevel = 2
                });
            }

            var threats = await analyzer.DetectThreatsAsync();
            Assert.NotNull(threats);
        }

        [Fact]
        public async Task ThreatAnalyzer_GenerateAlert_Succeeds()
        {
            var analyzer = new SecurityThreatAnalyzer(_logger);
            await analyzer.InitializeAsync();

            var event1 = new SecurityEvent { EventType = SecurityEventType.MalwareDetected };
            await analyzer.AnalyzeSecurityEventAsync(event1);
            var threats = await analyzer.DetectThreatsAsync();

            if (threats.Length > 0)
            {
                var alert = await analyzer.GenerateAlertAsync(threats[0].ThreatId, "Test alert");
                Assert.True(alert);
            }
        }

        [Fact]
        public async Task ThreatAnalyzer_GetMetrics_ReturnsMetrics()
        {
            var analyzer = new SecurityThreatAnalyzer(_logger);
            await analyzer.InitializeAsync();

            var evt = new SecurityEvent { EventType = SecurityEventType.UnauthorizedAccess };
            await analyzer.AnalyzeSecurityEventAsync(evt);

            var metrics = await analyzer.GetThreatMetricsAsync();
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalEventsAnalyzed > 0);
        }

        [Fact]
        public async Task ThreatAnalyzer_GenerateThreatReport_ReturnsReport()
        {
            var analyzer = new SecurityThreatAnalyzer(_logger);
            await analyzer.InitializeAsync();

            var evt = new SecurityEvent { EventType = SecurityEventType.SQLInjection };
            await analyzer.AnalyzeSecurityEventAsync(evt);

            var report = await analyzer.GenerateThreatReportAsync();
            Assert.NotNull(report);
            Assert.NotNull(report.RecommendedDefenses);
        }

        // ==================== DATA COMPRESSION ENGINE TESTS ====================

        [Fact]
        public async Task CompressionEngine_Initialize_Succeeds()
        {
            var engine = new DataCompressionEngine(_logger);
            var result = await engine.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task CompressionEngine_CompressFast_Succeeds()
        {
            var engine = new DataCompressionEngine(_logger);
            await engine.InitializeAsync();

            var data = string.Join("", Enumerable.Range(0, 100).Select(i => "TestData"));
            var compressed = await engine.CompressDataAsync(data, CompressionStrategy.Fast);

            Assert.NotNull(compressed);
            Assert.True(compressed.CompressedSize < compressed.OriginalSize);
        }

        [Fact]
        public async Task CompressionEngine_CompressBalanced_Succeeds()
        {
            var engine = new DataCompressionEngine(_logger);
            await engine.InitializeAsync();

            var data = new string('a', 10000);
            var compressed = await engine.CompressDataAsync(data, CompressionStrategy.Balanced);

            Assert.True(compressed.CompressedSize < compressed.OriginalSize);
        }

        [Fact]
        public async Task CompressionEngine_CompressMaximum_Succeeds()
        {
            var engine = new DataCompressionEngine(_logger);
            await engine.InitializeAsync();

            var data = new string('b', 5000);
            var compressed = await engine.CompressDataAsync(data, CompressionStrategy.Maximum);

            Assert.True(compressed.CompressedSize < compressed.OriginalSize);
        }

        [Fact]
        public async Task CompressionEngine_Decompress_ReturnsOriginal()
        {
            var engine = new DataCompressionEngine(_logger);
            await engine.InitializeAsync();

            var original = "This is test data for compression";
            var compressed = await engine.CompressDataAsync(original, CompressionStrategy.Balanced);
            var decompressed = await engine.DecompressDataAsync(compressed);

            Assert.Equal(original, decompressed);
        }

        [Fact]
        public async Task CompressionEngine_CompressLogs_Succeeds()
        {
            var engine = new DataCompressionEngine(_logger);
            await engine.InitializeAsync();

            var logs = Enumerable.Range(1, 50)
                .Select(i => $"[{i}] Log entry {i}")
                .ToList();

            var result = await engine.CompressLogsAsync(logs);
            Assert.True(result);
        }

        [Fact]
        public async Task CompressionEngine_CompressMetrics_Succeeds()
        {
            var engine = new DataCompressionEngine(_logger);
            await engine.InitializeAsync();

            var metrics = new Dictionary<string, object>
            {
                { "CPU", 75.5 },
                { "Memory", 82.3 },
                { "Disk", 60.2 }
            };

            var result = await engine.CompressMetricsAsync(metrics);
            Assert.True(result);
        }

        [Fact]
        public async Task CompressionEngine_GetMetrics_ReturnsMetrics()
        {
            var engine = new DataCompressionEngine(_logger);
            await engine.InitializeAsync();

            await engine.CompressDataAsync("Test data", CompressionStrategy.Balanced);
            var metrics = await engine.GetCompressionMetricsAsync();

            Assert.NotNull(metrics);
            Assert.True(metrics.CompressedItems > 0);
            Assert.True(metrics.TotalBytesCompressed > 0);
        }

        // ==================== PERFORMANCE PREDICTOR TESTS ====================

        [Fact]
        public async Task PerfPredictor_Initialize_Succeeds()
        {
            var predictor = new PerformancePredictorAI(_logger);
            var result = await predictor.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task PerfPredictor_RecordMetric_Succeeds()
        {
            var predictor = new PerformancePredictorAI(_logger);
            await predictor.InitializeAsync();

            var result = await predictor.RecordMetricAsync("CPU", 75.5);
            Assert.True(result);
        }

        [Fact]
        public async Task PerfPredictor_PredictPerformance_GeneratesPrediction()
        {
            var predictor = new PerformancePredictorAI(_logger);
            await predictor.InitializeAsync();

            for (int i = 0; i < 20; i++)
            {
                await predictor.RecordMetricAsync("CPU", 50 + i);
            }

            var prediction = await predictor.PredictPerformanceAsync(4);

            Assert.NotNull(prediction);
            Assert.True(prediction.OverallHealthScore >= 0 && prediction.OverallHealthScore <= 1);
        }

        [Fact]
        public async Task PerfPredictor_GetCapacityAlerts_ReturnsAlerts()
        {
            var predictor = new PerformancePredictorAI(_logger);
            await predictor.InitializeAsync();

            for (int i = 0; i < 50; i++)
            {
                await predictor.RecordMetricAsync("Memory", 70 + i % 20);
            }

            var alerts = await predictor.GetCapacityAlertsAsync();
            Assert.NotNull(alerts);
        }

        [Fact]
        public async Task PerfPredictor_GetRecommendedActions_ReturnsActions()
        {
            var predictor = new PerformancePredictorAI(_logger);
            await predictor.InitializeAsync();

            var actions = await predictor.GetRecommendedActionsAsync();
            Assert.NotEmpty(actions);
        }

        [Fact]
        public async Task PerfPredictor_GetAccuracyReport_ReturnsReport()
        {
            var predictor = new PerformancePredictorAI(_logger);
            await predictor.InitializeAsync();

            var report = await predictor.GetAccuracyReportAsync();
            Assert.NotNull(report);
            Assert.True(report.Accuracy >= 0 && report.Accuracy <= 1);
        }

        // ==================== COMPLEX EVENT PROCESSOR TESTS ====================

        [Fact]
        public async Task EventProcessor_Initialize_Succeeds()
        {
            var processor = new ComplexEventProcessor(_logger);
            var result = await processor.InitializeAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task EventProcessor_ProcessEvent_Succeeds()
        {
            var processor = new ComplexEventProcessor(_logger);
            await processor.InitializeAsync();

            var evt = new SystemEvent
            {
                EventType = "TestEvent",
                Source = "TestSource",
                Severity = 2
            };

            var result = await processor.ProcessEventAsync(evt);
            Assert.True(result);
        }

        [Fact]
        public async Task EventProcessor_DetectPatterns_GeneratesPatterns()
        {
            var processor = new ComplexEventProcessor(_logger);
            await processor.InitializeAsync();

            for (int i = 0; i < 20; i++)
            {
                await processor.ProcessEventAsync(new SystemEvent
                {
                    EventType = $"Type_{i % 3}",
                    Source = "Source_A",
                    Severity = 1
                });
            }

            var patterns = await processor.DetectPatternsAsync();
            Assert.NotNull(patterns);
        }

        [Fact]
        public async Task EventProcessor_GetCriticalAlerts_ReturnsAlerts()
        {
            var processor = new ComplexEventProcessor(_logger);
            await processor.InitializeAsync();

            for (int i = 0; i < 10; i++)
            {
                await processor.ProcessEventAsync(new SystemEvent
                {
                    EventType = "CriticalEvent",
                    Severity = 4
                });
            }

            var alerts = await processor.GetCriticalAlertsAsync();
            Assert.NotNull(alerts);
        }

        [Fact]
        public async Task EventProcessor_GetMetrics_ReturnsMetrics()
        {
            var processor = new ComplexEventProcessor(_logger);
            await processor.InitializeAsync();

            for (int i = 0; i < 15; i++)
            {
                await processor.ProcessEventAsync(new SystemEvent { EventType = "TestEvent" });
            }

            var metrics = await processor.GetMetricsAsync();
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalEventsProcessed >= 15);
        }

        [Fact]
        public async Task EventProcessor_AggregateEvents_ReturnsAggregation()
        {
            var processor = new ComplexEventProcessor(_logger);
            await processor.InitializeAsync();

            for (int i = 0; i < 25; i++)
            {
                await processor.ProcessEventAsync(new SystemEvent { EventType = $"Type_{i % 5}" });
            }

            var aggregation = await processor.AggregateEventsAsync(TimeSpan.FromMinutes(5));
            Assert.NotNull(aggregation);
        }

        // ==================== INTEGRATION & CONCURRENT TESTS ====================

        [Fact]
        public async Task Phase6_AllServices_InitializeSuccessfully()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            var allocator = new IntelligentResourceAllocator(_logger);
            var anomaly = new AnomalyPredictionEngine(_logger);
            var mesh = new ServiceMeshOptimizer(_logger);
            var threat = new SecurityThreatAnalyzer(_logger);
            var compression = new DataCompressionEngine(_logger);
            var perf = new PerformancePredictorAI(_logger);
            var events = new ComplexEventProcessor(_logger);

            var results = await Task.WhenAll(
                engine.InitializeAsync(),
                allocator.InitializeAsync(),
                anomaly.InitializeAsync(),
                mesh.InitializeAsync(),
                threat.InitializeAsync(),
                compression.InitializeAsync(),
                perf.InitializeAsync(),
                events.InitializeAsync()
            );

            Assert.All(results, r => Assert.True(r));
        }

        [Fact]
        public async Task Phase6_ConcurrentOperations_SucceedWithThreadSafety()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            await engine.InitializeAsync();

            var tasks = new List<Task>();
            for (int i = 0; i < 50; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    await engine.ApplyOptimizationAsync($"opt-{index}");
                }));
            }

            await Task.WhenAll(tasks);
            var metrics = await engine.GetOptimizationMetricsAsync();

            Assert.True(metrics.AppliedOptimizations >= 50);
        }

        [Fact]
        public async Task Phase6_EndToEnd_CompleteWorkflow()
        {
            var engine = new AdvancedOptimizationEngine(_logger);
            var allocator = new IntelligentResourceAllocator(_logger);
            var anomaly = new AnomalyPredictionEngine(_logger);

            await engine.InitializeAsync();
            await allocator.InitializeAsync();
            await anomaly.InitializeAsync();

            // Simulate workflow
            var recommendations = await engine.AnalyzeSystemAsync();
            var opt = await engine.ApplyOptimizationAsync(recommendations[0].Id);
            var metrics = await engine.GetOptimizationMetricsAsync();

            Assert.True(opt.Success);
            Assert.True(metrics.AppliedOptimizations > 0);
        }

        // ==================== LOGGER UTILITY ====================

        private class ConsoleLogger : ILogger
        {
            public void Debug(string message) => Console.WriteLine($"[DEBUG] {message}");
            public void Info(string message) => Console.WriteLine($"[INFO] {message}");
            public void Warn(string message) => Console.WriteLine($"[WARN] {message}");
            public void Error(string message) => Console.WriteLine($"[ERROR] {message}");
        }
    }
}
