using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Comprehensive performance baseline and benchmarking service for Phase 1-2 optimization
    /// Captures CPU, memory, latency, and GC metrics before and after optimizations
    /// </summary>
    public class PerformanceBenchmarkService
    {
        private readonly Dictionary<string, ServiceBenchmark> _benchmarks = new();
        private readonly object _lockObj = new();
        private long _initialMemory;
        private long _initialGCCount;

        public class ServiceBenchmark
        {
            public string ServiceName { get; set; }
            public long OperationCount { get; set; }
            public long TotalDurationMs { get; set; }
            public long MinLatencyMs { get; set; } = long.MaxValue;
            public long MaxLatencyMs { get; set; } = long.MinValue;
            public long AvgLatencyMs => OperationCount > 0 ? TotalDurationMs / OperationCount : 0;
            public long MedianLatencyMs { get; set; }
            public long P95LatencyMs { get; set; }
            public long P99LatencyMs { get; set; }
            public long MemoryDeltaMB { get; set; }
            public long GCGen0Collections { get; set; }
            public long GCGen1Collections { get; set; }
            public long GCGen2Collections { get; set; }
            public double ThroughputOpsPerSec => OperationCount > 0 ? (OperationCount * 1000.0) / TotalDurationMs : 0;
            public List<long> LatencySamples { get; set; } = new();
        }

        public class OptimizationResult
        {
            public string ServiceName { get; set; }
            public ServiceBenchmark BaselineMetrics { get; set; }
            public ServiceBenchmark OptimizedMetrics { get; set; }
            public double ThroughputImprovement => BaselineMetrics.ThroughputOpsPerSec > 0
                ? ((OptimizedMetrics.ThroughputOpsPerSec - BaselineMetrics.ThroughputOpsPerSec) / BaselineMetrics.ThroughputOpsPerSec) * 100
                : 0;
            public double LatencyImprovement => BaselineMetrics.AvgLatencyMs > 0
                ? ((BaselineMetrics.AvgLatencyMs - OptimizedMetrics.AvgLatencyMs) / BaselineMetrics.AvgLatencyMs) * 100
                : 0;
            public double MemoryImprovement => BaselineMetrics.MemoryDeltaMB > 0
                ? ((BaselineMetrics.MemoryDeltaMB - OptimizedMetrics.MemoryDeltaMB) / BaselineMetrics.MemoryDeltaMB) * 100
                : 0;
            public double GCImprovement => BaselineMetrics.GCGen2Collections > 0
                ? ((BaselineMetrics.GCGen2Collections - OptimizedMetrics.GCGen2Collections) / (double)BaselineMetrics.GCGen2Collections) * 100
                : 0;
        }

        public void StartBaseline()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _initialMemory = GC.GetTotalMemory(false);
            _initialGCCount = GC.CollectionCount(2);
        }

        public async Task<ServiceBenchmark> BenchmarkServiceAsync(
            string serviceName,
            Func<Task> operation,
            int iterations = 1000,
            int warmupIterations = 100)
        {
            // Warmup
            for (int i = 0; i < warmupIterations; i++)
            {
                try { await operation(); }
                catch { }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var benchmark = new ServiceBenchmark { ServiceName = serviceName };
            var sw = Stopwatch.StartNew();
            var latencies = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                var opSw = Stopwatch.StartNew();
                try
                {
                    await operation();
                }
                catch { }
                opSw.Stop();

                latencies.Add(opSw.ElapsedMilliseconds);
                benchmark.MinLatencyMs = Math.Min(benchmark.MinLatencyMs, opSw.ElapsedMilliseconds);
                benchmark.MaxLatencyMs = Math.Max(benchmark.MaxLatencyMs, opSw.ElapsedMilliseconds);
            }

            sw.Stop();
            benchmark.OperationCount = iterations;
            benchmark.TotalDurationMs = sw.ElapsedMilliseconds;
            benchmark.LatencySamples = latencies;

            // Calculate percentiles
            var sorted = latencies.OrderBy(x => x).ToList();
            benchmark.MedianLatencyMs = sorted[iterations / 2];
            benchmark.P95LatencyMs = sorted[(int)(iterations * 0.95)];
            benchmark.P99LatencyMs = sorted[(int)(iterations * 0.99)];

            // Memory and GC metrics
            benchmark.MemoryDeltaMB = (GC.GetTotalMemory(false) - _initialMemory) / (1024 * 1024);
            benchmark.GCGen0Collections = GC.CollectionCount(0);
            benchmark.GCGen1Collections = GC.CollectionCount(1);
            benchmark.GCGen2Collections = GC.CollectionCount(2) - (int)_initialGCCount;

            lock (_lockObj)
            {
                _benchmarks[serviceName] = benchmark;
            }

            return benchmark;
        }

        public OptimizationResult CalculateOptimization(
            string serviceName,
            ServiceBenchmark baseline,
            ServiceBenchmark optimized)
        {
            return new OptimizationResult
            {
                ServiceName = serviceName,
                BaselineMetrics = baseline,
                OptimizedMetrics = optimized
            };
        }

        public Dictionary<string, ServiceBenchmark> GetAllBenchmarks()
        {
            lock (_lockObj)
            {
                return new Dictionary<string, ServiceBenchmark>(_benchmarks);
            }
        }

        public string GenerateReport(List<OptimizationResult> results)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("═══════════════════════════════════════════════════════════════════");
            report.AppendLine("PHASE 6 OPTIMIZATION BENCHMARK REPORT");
            report.AppendLine("═══════════════════════════════════════════════════════════════════");
            report.AppendLine($"\nGenerated: {DateTime.UtcNow:O}");
            report.AppendLine($"Services Optimized: {results.Count}");
            report.AppendLine();

            var totalThroughputImprovement = 0.0;
            var totalLatencyImprovement = 0.0;
            var totalMemoryImprovement = 0.0;
            var totalGCImprovement = 0.0;

            report.AppendLine("OPTIMIZATION RESULTS");
            report.AppendLine("─────────────────────────────────────────────────────────────────");
            report.AppendLine($"{"Service Name",-40} {"Throughput",15} {"Latency",15} {"Memory",15} {"GC",15}");
            report.AppendLine("─────────────────────────────────────────────────────────────────");

            foreach (var result in results)
            {
                var tput = $"{result.ThroughputImprovement:+0.0;-0.0;0.0}%";
                var lat = $"{result.LatencyImprovement:+0.0;-0.0;0.0}%";
                var mem = $"{result.MemoryImprovement:+0.0;-0.0;0.0}%";
                var gc = $"{result.GCImprovement:+0.0;-0.0;0.0}%";

                report.AppendLine($"{result.ServiceName,-40} {tput,15} {lat,15} {mem,15} {gc,15}");

                totalThroughputImprovement += result.ThroughputImprovement;
                totalLatencyImprovement += result.LatencyImprovement;
                totalMemoryImprovement += result.MemoryImprovement;
                totalGCImprovement += result.GCImprovement;
            }

            report.AppendLine("─────────────────────────────────────────────────────────────────");

            var avgCount = results.Count > 0 ? results.Count : 1;
            report.AppendLine($"{"AVERAGE IMPROVEMENTS",-40} " +
                $"{totalThroughputImprovement / avgCount:+0.0;-0.0;0.0}% " +
                $"{totalLatencyImprovement / avgCount:+0.0;-0.0;0.0}% " +
                $"{totalMemoryImprovement / avgCount:+0.0;-0.0;0.0}% " +
                $"{totalGCImprovement / avgCount:+0.0;-0.0;0.0}%");

            return report.ToString();
        }

        public string GenerateJson(List<OptimizationResult> results)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                timestamp = DateTime.UtcNow,
                phase = "PHASE 6",
                summary = new
                {
                    servicesOptimized = results.Count,
                    averageThroughputImprovement = results.Any() ? results.Average(r => r.ThroughputImprovement) : 0,
                    averageLatencyImprovement = results.Any() ? results.Average(r => r.LatencyImprovement) : 0,
                    averageMemoryImprovement = results.Any() ? results.Average(r => r.MemoryImprovement) : 0,
                    averageGCImprovement = results.Any() ? results.Average(r => r.GCImprovement) : 0
                },
                results = results.Select(r => new
                {
                    serviceName = r.ServiceName,
                    baseline = new
                    {
                        throughputOpsPerSec = r.BaselineMetrics.ThroughputOpsPerSec,
                        avgLatencyMs = r.BaselineMetrics.AvgLatencyMs,
                        p95LatencyMs = r.BaselineMetrics.P95LatencyMs,
                        memoryMB = r.BaselineMetrics.MemoryDeltaMB,
                        gcGen2Collections = r.BaselineMetrics.GCGen2Collections
                    },
                    optimized = new
                    {
                        throughputOpsPerSec = r.OptimizedMetrics.ThroughputOpsPerSec,
                        avgLatencyMs = r.OptimizedMetrics.AvgLatencyMs,
                        p95LatencyMs = r.OptimizedMetrics.P95LatencyMs,
                        memoryMB = r.OptimizedMetrics.MemoryDeltaMB,
                        gcGen2Collections = r.OptimizedMetrics.GCGen2Collections
                    },
                    improvements = new
                    {
                        throughputPercent = r.ThroughputImprovement,
                        latencyPercent = r.LatencyImprovement,
                        memoryPercent = r.MemoryImprovement,
                        gcPercent = r.GCImprovement
                    }
                }).ToList()
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            return json;
        }
    }
}
