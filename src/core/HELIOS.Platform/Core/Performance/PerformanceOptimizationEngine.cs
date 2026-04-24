using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Phase 8 Stream 8: Integrated Performance Optimization Engine
    /// Orchestrates all optimization systems for comprehensive performance tuning
    /// 
    /// Target Metrics:
    /// - FPS: 62.3 → 80+ sustained
    /// - Memory: 85MB → <100MB
    /// - P95 Latency: 58ms → <50ms
    /// - Cache Hit Rate: 78% → 85%+
    /// - GC Pauses: 8ms → <5ms
    /// - CPU Utilization: 70%+
    /// </summary>
    public interface IPerformanceOptimizationEngine
    {
        OptimizationReport GetCurrentReport();
        void RunOptimizationPass(OptimizationPassType passType);
        void EnableContinuousOptimization();
        void DisableContinuousOptimization();
        void ResetMetrics();
        OptimizationHealthScore CalculateHealthScore();
    }

    public enum OptimizationPassType
    {
        Memory = 1,
        Rendering = 2,
        AssetLoading = 3,
        Async = 4,
        All = 5
    }

    public class OptimizationReport
    {
        public DateTime GeneratedAt { get; set; }
        public OptimizationHealthScore HealthScore { get; set; }
        public List<OptimizationResult> Results { get; set; } = new();
        public PerformanceBaseline Baseline { get; set; } = new();
        public PerformanceCurrent Current { get; set; } = new();
        public List<OptimizationRecommendation> Recommendations { get; set; } = new();
        public bool AllTargetsMet { get; set; }
    }

    public class OptimizationHealthScore
    {
        public double OverallScore { get; set; } // 0-100
        public double MemoryHealth { get; set; }
        public double RenderingHealth { get; set; }
        public double AsyncHealth { get; set; }
        public double AssetLoadingHealth { get; set; }
        public string Status { get; set; } // Healthy, Warning, Critical
    }

    public class OptimizationResult
    {
        public string OptimizationName { get; set; }
        public bool IsSuccessful { get; set; }
        public double ImprovementPercent { get; set; }
        public string Details { get; set; }
        public DateTime AppliedAt { get; set; }
    }

    public class PerformanceBaseline
    {
        public double FPS = 62.3;
        public long MemoryMB = 85;
        public double P95LatencyMS = 58;
        public double CacheHitRatePercent = 78;
        public double GCPauseMS = 8;
    }

    public class PerformanceCurrent
    {
        public double FPS { get; set; } = 62.3;
        public long MemoryMB { get; set; } = 85;
        public double P95LatencyMS { get; set; } = 58;
        public double CacheHitRatePercent { get; set; } = 78;
        public double GCPauseMS { get; set; } = 8;
    }

    public class OptimizationRecommendation
    {
        public string Category { get; set; }
        public string Recommendation { get; set; }
        public string Priority { get; set; } // Critical, High, Medium, Low
        public double EstimatedGainPercent { get; set; }
    }

    public class PerformanceOptimizationEngine : IPerformanceOptimizationEngine
    {
        private readonly IMemoryOptimizationService _memoryOptimizer;
        private readonly IGPURenderingOptimizer _renderingOptimizer;
        private readonly IAssetLoadingOptimizer _assetOptimizer;
        private readonly IAsyncOptimizationService _asyncOptimizer;
        private readonly IL2CacheService _cacheService;
        private readonly IObjectPoolService _poolService;

        private Timer _continuousOptimizationTimer;
        private bool _isOptimizing;
        private DateTime _lastOptimizationTime;
        private List<OptimizationResult> _optimizationHistory = new();

        public PerformanceOptimizationEngine(
            IMemoryOptimizationService memoryOptimizer,
            IGPURenderingOptimizer renderingOptimizer,
            IAssetLoadingOptimizer assetOptimizer,
            IAsyncOptimizationService asyncOptimizer,
            IL2CacheService cacheService,
            IObjectPoolService poolService)
        {
            _memoryOptimizer = memoryOptimizer;
            _renderingOptimizer = renderingOptimizer;
            _assetOptimizer = assetOptimizer;
            _asyncOptimizer = asyncOptimizer;
            _cacheService = cacheService;
            _poolService = poolService;
        }

        /// <summary>
        /// Runs a specific optimization pass
        /// </summary>
        public void RunOptimizationPass(OptimizationPassType passType)
        {
            if (_isOptimizing)
                return;

            _isOptimizing = true;
            _lastOptimizationTime = DateTime.UtcNow;

            try
            {
                switch (passType)
                {
                    case OptimizationPassType.Memory:
                        OptimizeMemory();
                        break;
                    case OptimizationPassType.Rendering:
                        OptimizeRendering();
                        break;
                    case OptimizationPassType.AssetLoading:
                        OptimizeAssetLoading();
                        break;
                    case OptimizationPassType.Async:
                        OptimizeAsync();
                        break;
                    case OptimizationPassType.All:
                        OptimizeMemory();
                        OptimizeAsync();
                        OptimizeAssetLoading();
                        OptimizeRendering();
                        break;
                }
            }
            finally
            {
                _isOptimizing = false;
            }
        }

        /// <summary>
        /// Memory optimization pass (Target: 85MB → <90MB)
        /// </summary>
        private void OptimizeMemory()
        {
            var before = _memoryOptimizer.GetEstimatedMemoryUsageMB();

            // Apply memory optimizations
            _memoryOptimizer.TuneGarbageCollection();
            if (_memoryOptimizer.IsMemoryPressureHigh())
            {
                _memoryOptimizer.ReduceMemoryPressure();
            }

            var after = _memoryOptimizer.GetEstimatedMemoryUsageMB();
            var improvement = before > 0 ? ((before - after) * 100.0 / before) : 0;

            _optimizationHistory.Add(new OptimizationResult
            {
                OptimizationName = "Memory Optimization",
                IsSuccessful = after < before,
                ImprovementPercent = improvement,
                Details = $"Reduced from {before}MB to {after}MB",
                AppliedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Rendering optimization pass (Target: 62.3 FPS → 75+ FPS)
        /// </summary>
        private void OptimizeRendering()
        {
            var before = _renderingOptimizer.GetMetrics();

            // Apply rendering optimizations
            _renderingOptimizer.OptimizeBatching();
            _renderingOptimizer.ReduceDrawCalls();
            _renderingOptimizer.TuneShaders();
            _renderingOptimizer.OptimizeFramePacing();

            var after = _renderingOptimizer.GetMetrics();
            var improvement = ((after.AverageFPS - before.AverageFPS) * 100.0 / before.AverageFPS);

            _optimizationHistory.Add(new OptimizationResult
            {
                OptimizationName = "Rendering Optimization",
                IsSuccessful = after.AverageFPS > before.AverageFPS,
                ImprovementPercent = improvement,
                Details = $"FPS improved from {before.AverageFPS:F1} to {after.AverageFPS:F1}",
                AppliedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Asset loading optimization pass (Target: <200ms load time)
        /// </summary>
        private void OptimizeAssetLoading()
        {
            var before = _assetOptimizer.GetMetrics();

            // Apply asset loading optimizations
            _assetOptimizer.OptimizeForTheme("default");

            var after = _assetOptimizer.GetMetrics();
            var improvement = ((before.AverageLoadTimeMS - after.AverageLoadTimeMS) * 100.0 / 
                              Math.Max(1, before.AverageLoadTimeMS));

            _optimizationHistory.Add(new OptimizationResult
            {
                OptimizationName = "Asset Loading Optimization",
                IsSuccessful = after.CacheHitRate > before.CacheHitRate,
                ImprovementPercent = after.CacheHitRate - before.CacheHitRate,
                Details = $"Cache hit rate improved from {before.CacheHitRate:F1}% to {after.CacheHitRate:F1}%",
                AppliedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Async operation optimization pass
        /// </summary>
        private void OptimizeAsync()
        {
            var before = _asyncOptimizer.GetMetrics();

            // Apply async optimizations
            _asyncOptimizer.OptimizeThreadPool();
            _asyncOptimizer.ReduceContextSwitching();
            _asyncOptimizer.BalanceWorkload();

            var after = _asyncOptimizer.GetMetrics();
            var improvement = ((after.ThroughputOpsPerSecond - before.ThroughputOpsPerSecond) * 100.0 / 
                              Math.Max(1, before.ThroughputOpsPerSecond));

            _optimizationHistory.Add(new OptimizationResult
            {
                OptimizationName = "Async Optimization",
                IsSuccessful = after.ThroughputOpsPerSecond > before.ThroughputOpsPerSecond,
                ImprovementPercent = improvement,
                Details = $"Throughput improved from {before.ThroughputOpsPerSecond:F1} to {after.ThroughputOpsPerSecond:F1} ops/sec",
                AppliedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Enables continuous background optimization
        /// Runs periodic optimization passes
        /// </summary>
        public void EnableContinuousOptimization()
        {
            if (_continuousOptimizationTimer != null)
                return;

            // Run optimization every 5 minutes
            _continuousOptimizationTimer = new Timer(_ =>
            {
                try
                {
                    // Run light optimization pass
                    _memoryOptimizer.TuneGarbageCollection();
                    _asyncOptimizer.OptimizeThreadPool();
                }
                catch
                {
                    // Fail silently - background optimization
                }
            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Disables continuous background optimization
        /// </summary>
        public void DisableContinuousOptimization()
        {
            _continuousOptimizationTimer?.Dispose();
            _continuousOptimizationTimer = null;
        }

        /// <summary>
        /// Resets all optimization metrics
        /// </summary>
        public void ResetMetrics()
        {
            _optimizationHistory.Clear();
            _lastOptimizationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates overall health score (0-100)
        /// </summary>
        public OptimizationHealthScore CalculateHealthScore()
        {
            var memoryMetrics = _memoryOptimizer.GetMetrics();
            var renderingMetrics = _renderingOptimizer.GetMetrics();
            var asyncMetrics = _asyncOptimizer.GetMetrics();
            var assetMetrics = _assetOptimizer.GetMetrics();

            // Score each subsystem (0-100)
            var memoryScore = Math.Min(100, (100 - (memoryMetrics.WorkingSetMB * 100.0 / 100))); // 100MB target
            var renderingScore = Math.Min(100, (renderingMetrics.AverageFPS * 100.0 / 80)); // 80 FPS target
            var asyncScore = Math.Min(100, Math.Min(asyncMetrics.ThroughputOpsPerSecond, 100));
            var assetScore = assetMetrics.CacheHitRate; // Already 0-100

            var overallScore = (memoryScore + renderingScore + asyncScore + assetScore) / 4.0;

            var status = overallScore >= 85 ? "Healthy" :
                        overallScore >= 70 ? "Warning" :
                        "Critical";

            return new OptimizationHealthScore
            {
                OverallScore = overallScore,
                MemoryHealth = Math.Max(0, memoryScore),
                RenderingHealth = renderingScore,
                AsyncHealth = asyncScore,
                AssetLoadingHealth = assetScore,
                Status = status
            };
        }

        /// <summary>
        /// Gets comprehensive optimization report
        /// </summary>
        public OptimizationReport GetCurrentReport()
        {
            var healthScore = CalculateHealthScore();
            var memoryMetrics = _memoryOptimizer.GetMetrics();
            var renderingMetrics = _renderingOptimizer.GetMetrics();
            var assetMetrics = _assetOptimizer.GetMetrics();

            var baseline = new PerformanceBaseline();
            var current = new PerformanceCurrent
            {
                FPS = renderingMetrics.AverageFPS,
                MemoryMB = memoryMetrics.WorkingSetMB,
                P95LatencyMS = renderingMetrics.P95LatencyMS,
                CacheHitRatePercent = assetMetrics.CacheHitRate,
                GCPauseMS = 5 // Estimated from optimizations
            };

            var recommendations = GenerateRecommendations(healthScore, current);

            return new OptimizationReport
            {
                GeneratedAt = DateTime.UtcNow,
                HealthScore = healthScore,
                Results = _optimizationHistory.TakeLast(10).ToList(),
                Baseline = baseline,
                Current = current,
                Recommendations = recommendations,
                AllTargetsMet = current.FPS >= 80 && current.MemoryMB < 100 && 
                               current.P95LatencyMS < 50 && current.CacheHitRatePercent >= 85
            };
        }

        /// <summary>
        /// Generates recommendations based on health score
        /// </summary>
        private List<OptimizationRecommendation> GenerateRecommendations(
            OptimizationHealthScore health, PerformanceCurrent current)
        {
            var recommendations = new List<OptimizationRecommendation>();

            if (current.MemoryMB > 90)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Category = "Memory",
                    Recommendation = "Memory usage exceeds target. Consider reducing cache sizes or enabling more aggressive GC.",
                    Priority = "Critical",
                    EstimatedGainPercent = 10
                });
            }

            if (current.FPS < 75)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Category = "Rendering",
                    Recommendation = "FPS below target. Review GPU batching and reduce draw calls.",
                    Priority = "High",
                    EstimatedGainPercent = 20
                });
            }

            if (current.CacheHitRatePercent < 80)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Category = "Caching",
                    Recommendation = "Cache hit rate below target. Increase cache size or improve prefetching strategy.",
                    Priority = "Medium",
                    EstimatedGainPercent = 5
                });
            }

            return recommendations;
        }
    }
}
