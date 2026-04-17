using System;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Phase 4 Tier 1.3: Advanced Profiling Services Tests
    /// </summary>
    public class Phase4AdvancedProfilingTests
    {
        private readonly Core.Logging.ILogger _logger;

        public Phase4AdvancedProfilingTests()
        {
            _logger = new Core.Logging.ConsoleLogger();
        }

        #region Performance Profiler Tests

        [Fact]
        public async Task PerformanceProfiler_ProfileApplication_ReturnsValidMetrics()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var metrics = await profiler.ProfileApplicationAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.NotNull(metrics.Memory);
            Assert.NotNull(metrics.CPU);
            Assert.True(metrics.MeasuredAt > DateTime.UtcNow.AddSeconds(-5));
        }

        [Fact]
        public async Task PerformanceProfiler_AnalyzeMemoryUsage_ReturnsValidMetrics()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var metrics = await profiler.AnalyzeMemoryUsageAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalMemoryMb > 0);
            Assert.True(metrics.UsedMemoryMb > 0);
            Assert.True(metrics.UsagePercent >= 0);
        }

        [Fact]
        public async Task PerformanceProfiler_AnalyzeCPUUsage_ReturnsValidMetrics()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var metrics = await profiler.AnalyzeCPUUsageAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.CoreCount > 0);
            Assert.True(metrics.ThreadCount > 0);
            Assert.True(metrics.AverageUsagePercent >= 0);
        }

        [Fact]
        public async Task PerformanceProfiler_AnalyzeDiskIO_ReturnsValidMetrics()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var metrics = await profiler.AnalyzeDiskIOAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ReadThroughputMbps >= 0);
            Assert.True(metrics.WriteThroughputMbps >= 0);
            Assert.True(metrics.DiskUtilizationPercent >= 0);
        }

        [Fact]
        public async Task PerformanceProfiler_AnalyzeNetwork_ReturnsValidMetrics()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var metrics = await profiler.AnalyzeNetworkAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.BytesSentPerSecond >= 0);
            Assert.True(metrics.BytesReceivedPerSecond >= 0);
            Assert.True(metrics.AverageLatencyMs >= 0);
        }

        [Fact]
        public async Task PerformanceProfiler_MeasureStartupTime_ReturnsValidMetrics()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var metrics = await profiler.MeasureStartupTimeAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalStartupTimeMs > 0);
            Assert.True(metrics.ApplicationLoadTimeMs > 0);
        }

        [Fact]
        public async Task PerformanceProfiler_MeasureResponseTimes_ReturnsValidMetrics()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var metrics = await profiler.MeasureResponseTimesAsync();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.AverageResponseTimeMs > 0);
            Assert.True(metrics.P95ResponseTimeMs >= metrics.AverageResponseTimeMs);
            Assert.True(metrics.P99ResponseTimeMs >= metrics.P95ResponseTimeMs);
        }

        [Fact]
        public async Task PerformanceProfiler_GetOptimizationRecommendations_ReturnsValidData()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var recommendations = await profiler.GetOptimizationRecommendationsAsync();

            // Assert
            Assert.NotNull(recommendations);
            Assert.NotNull(recommendations.MemoryOptimizations);
            Assert.NotNull(recommendations.CPUOptimizations);
            Assert.True(recommendations.EstimatedPerformanceGainPercent > 0);
        }

        [Fact]
        public async Task PerformanceProfiler_ApplyOptimizations_ReturnsTrue()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);
            var recommendations = await profiler.GetOptimizationRecommendationsAsync();

            // Act
            var result = await profiler.ApplyOptimizationsAsync(recommendations);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PerformanceProfiler_CachePrefetch_CompletesSuccessfully()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act & Assert
            await profiler.CachePrefetchAsync(); // Should not throw
        }

        #endregion

        #region Metric Validation Tests

        [Fact]
        public void PerformanceMetrics_AllFieldsPopulated_True()
        {
            // Arrange & Act
            var metrics = new PerformanceMetrics
            {
                Memory = new MemoryProfile { TotalMemoryMb = 1024, UsedMemoryMb = 512 },
                CPU = new CPUProfile { CoreCount = 8, AverageUsagePercent = 25.5 },
                Disk = new DiskProfile { ReadThroughputMbps = 100 },
                Network = new NetworkProfile { AverageLatencyMs = 15 }
            };

            // Assert
            Assert.NotNull(metrics.Memory);
            Assert.NotNull(metrics.CPU);
            Assert.NotNull(metrics.Disk);
            Assert.NotNull(metrics.Network);
            Assert.True(metrics.MeasuredAt > DateTime.UtcNow.AddSeconds(-5));
        }

        [Fact]
        public void MemoryProfile_UsagePercentageValid_True()
        {
            // Arrange & Act
            var profile = new MemoryProfile
            {
                TotalMemoryMb = 1000,
                UsedMemoryMb = 750,
                UsagePercent = 75.0
            };

            // Assert
            Assert.True(profile.UsagePercent >= 0 && profile.UsagePercent <= 100);
        }

        [Fact]
        public void CPUProfile_CoreCountValid_True()
        {
            // Arrange & Act
            var profile = new CPUProfile
            {
                CoreCount = Environment.ProcessorCount,
                AverageUsagePercent = 50.0,
                ThreadCount = 100
            };

            // Assert
            Assert.True(profile.CoreCount > 0);
            Assert.True(profile.ThreadCount > 0);
        }

        [Fact]
        public void StartupMetrics_Breakdown_IsValid()
        {
            // Arrange & Act
            var metrics = new StartupMetrics
            {
                TotalStartupTimeMs = 2000,
                ApplicationLoadTimeMs = 800,
                DependencyInitializationMs = 600,
                DatabaseInitializationMs = 400,
                UIRenderTimeMs = 200
            };

            // Assert
            Assert.Equal(2000, metrics.TotalStartupTimeMs);
            Assert.True(metrics.ApplicationLoadTimeMs > 0);
        }

        [Fact]
        public void ResponseTimeMetrics_PercentilesCorrect_True()
        {
            // Arrange & Act
            var metrics = new ResponseTimeMetrics
            {
                AverageResponseTimeMs = 100,
                P95ResponseTimeMs = 150,
                P99ResponseTimeMs = 200,
                MinResponseTimeMs = 10,
                MaxResponseTimeMs = 500
            };

            // Assert
            Assert.True(metrics.MinResponseTimeMs <= metrics.AverageResponseTimeMs);
            Assert.True(metrics.AverageResponseTimeMs <= metrics.P95ResponseTimeMs);
            Assert.True(metrics.P95ResponseTimeMs <= metrics.P99ResponseTimeMs);
            Assert.True(metrics.P99ResponseTimeMs <= metrics.MaxResponseTimeMs);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task ProfilerIntegration_FullPipeline_WorksEnd2End()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);

            // Act
            var profile = await profiler.ProfileApplicationAsync();
            var startupMetrics = await profiler.MeasureStartupTimeAsync();
            var responseMetrics = await profiler.MeasureResponseTimesAsync();
            var recommendations = await profiler.GetOptimizationRecommendationsAsync();

            // Assert
            Assert.NotNull(profile);
            Assert.NotNull(startupMetrics);
            Assert.NotNull(responseMetrics);
            Assert.NotNull(recommendations);
        }

        #endregion

        #region Performance Benchmarks

        [Fact]
        public async Task Benchmark_Profiling_OperationsCompleteFast()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var profile = await profiler.ProfileApplicationAsync();
            var startup = await profiler.MeasureStartupTimeAsync();
            var response = await profiler.MeasureResponseTimesAsync();
            stopwatch.Stop();

            // Assert - All profiling should complete within reasonable time
            Assert.True(stopwatch.ElapsedMilliseconds < 5000);
            _logger.Info($"Full profiling cycle: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Benchmark_RecommendationsGeneration_IsFast()
        {
            // Arrange
            var profiler = new PerformanceProfiler(_logger);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var recommendations = await profiler.GetOptimizationRecommendationsAsync();
            stopwatch.Stop();

            // Assert
            Assert.NotNull(recommendations);
            Assert.True(stopwatch.ElapsedMilliseconds < 2000);
            _logger.Info($"Recommendations generated in {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion
    }
}

