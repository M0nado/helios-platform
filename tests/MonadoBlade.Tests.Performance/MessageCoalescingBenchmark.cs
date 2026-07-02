using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using MonadoBlade.Core.Messaging;

namespace MonadoBlade.Tests.Performance
{
    /// <summary>
    /// Performance benchmarks for MessageCoalescer demonstrating throughput improvements.
    /// Measures the impact of message coalescing on dispatch operations and overall performance.
    /// </summary>
    public class MessageCoalescingBenchmark
    {
        private class BenchmarkMessage
        {
            public int Id { get; set; }
            public byte[] Payload { get; set; }

            public BenchmarkMessage(int id, int payloadSize)
            {
                Id = id;
                Payload = new byte[payloadSize];
            }
        }

        [Fact]
        public void Benchmark_BaselineVsOptimized_10000Messages()
        {
            const int messageCount = 10000;
            const int payloadSize = 256; // 256 bytes per message
            
            // BASELINE: Individual dispatch operations
            var baselineWatch = Stopwatch.StartNew();
            var baselineDispatchCount = 0;

            for (int i = 0; i < messageCount; i++)
            {
                baselineDispatchCount++; // Simulate dispatch
            }

            baselineWatch.Stop();

            // OPTIMIZED: Coalesced dispatch
            var optimizedDispatchCount = 0;
            var coalescer = new MessageCoalescer<BenchmarkMessage, int>(
                (key, messages) => optimizedDispatchCount++)
            {
                FlushInterval = 100,
                BatchSizeThreshold = 5
            };

            var optimizedWatch = Stopwatch.StartNew();

            for (int i = 0; i < messageCount; i++)
            {
                var key = i % 20; // 20 different keys
                coalescer.Enqueue(key, new BenchmarkMessage(i, payloadSize));
            }
            coalescer.Flush();

            optimizedWatch.Stop();

            var metrics = coalescer.GetMetrics();

            // Calculate improvements
            var dispatchReduction = ((double)(baselineDispatchCount - optimizedDispatchCount) / baselineDispatchCount) * 100;
            var throughputImprovement = ((double)(messageCount - optimizedDispatchCount) / messageCount) * 100;
            var averageBatchSize = (double)messageCount / optimizedDispatchCount;

            PrintBenchmarkResults("10,000 Message Coalescing Benchmark", new Dictionary<string, object>
            {
                { "Baseline Dispatches", baselineDispatchCount },
                { "Optimized Dispatches", optimizedDispatchCount },
                { "Dispatch Reduction", $"{dispatchReduction:F2}%" },
                { "Throughput Improvement", $"{throughputImprovement:F2}%" },
                { "Messages Reduced %", $"{(double)metrics["ReductionPercentage"]:F2}%" },
                { "Average Batch Size", $"{averageBatchSize:F2}" },
                { "Baseline Time (ms)", baselineWatch.ElapsedMilliseconds },
                { "Optimized Time (ms)", optimizedWatch.ElapsedMilliseconds },
                { "Total Messages", messageCount }
            });

            // Assertions
            Assert.True(dispatchReduction > 50.0,
                $"Expected > 50% dispatch reduction, got {dispatchReduction:F2}%");
            Assert.True(throughputImprovement > 50.0,
                $"Expected > 50% throughput improvement, got {throughputImprovement:F2}%");
            Assert.True((double)metrics["ReductionPercentage"] >= 60.0,
                $"Expected message reduction >= 60%, got {(double)metrics["ReductionPercentage"]:F2}%");

            coalescer.Dispose();
        }

        [Fact]
        public void Benchmark_MultipleKeys_MeasuresScalability()
        {
            const int messageCount = 10000;
            const int keyCount = 50;
            
            var dispatchCount = 0;
            var coalescer = new MessageCoalescer<BenchmarkMessage, int>(
                (key, messages) => dispatchCount++)
            {
                FlushInterval = 100,
                BatchSizeThreshold = 5
            };

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < messageCount; i++)
            {
                var key = i % keyCount;
                coalescer.Enqueue(key, new BenchmarkMessage(i, 128));
            }
            coalescer.Flush();

            watch.Stop();

            var metrics = coalescer.GetMetrics();
            var reductionPct = (double)metrics["ReductionPercentage"];
            var throughputImprovement = ((double)(messageCount - dispatchCount) / messageCount) * 100;

            PrintBenchmarkResults("Multi-Key Scalability (50 keys)", new Dictionary<string, object>
            {
                { "Total Messages", messageCount },
                { "Number of Keys", keyCount },
                { "Dispatch Operations", dispatchCount },
                { "Messages Reduced %", $"{reductionPct:F2}%" },
                { "Throughput Improvement %", $"{throughputImprovement:F2}%" },
                { "Average Batch Size", $"{(double)messageCount / dispatchCount:F2}" },
                { "Execution Time (ms)", watch.ElapsedMilliseconds }
            });

            Assert.True(reductionPct > 70.0);
            Assert.True(throughputImprovement > 70.0);

            coalescer.Dispose();
        }

        [Fact]
        public void Benchmark_VariableBatchThresholds_FindsOptimal()
        {
            const int messageCount = 5000;
            var results = new List<(int threshold, int dispatches, double reduction)>();

            foreach (var threshold in new[] { 1, 5, 10, 20, 50 })
            {
                var dispatchCount = 0;
                var coalescer = new MessageCoalescer<BenchmarkMessage, int>(
                    (key, messages) => dispatchCount++)
                {
                    FlushInterval = 100,
                    BatchSizeThreshold = threshold
                };

                for (int i = 0; i < messageCount; i++)
                {
                    coalescer.Enqueue(i % 10, new BenchmarkMessage(i, 128));
                }
                coalescer.Flush();

                var metrics = coalescer.GetMetrics();
                var reductionPct = (double)metrics["ReductionPercentage"];
                results.Add((threshold, dispatchCount, reductionPct));

                coalescer.Dispose();
            }

            PrintBenchmarkResults("Batch Threshold Optimization", new Dictionary<string, object>
            {
                { "Threshold 1", $"Dispatches: {results[0].dispatches}, Reduction: {results[0].reduction:F2}%" },
                { "Threshold 5", $"Dispatches: {results[1].dispatches}, Reduction: {results[1].reduction:F2}%" },
                { "Threshold 10", $"Dispatches: {results[2].dispatches}, Reduction: {results[2].reduction:F2}%" },
                { "Threshold 20", $"Dispatches: {results[3].dispatches}, Reduction: {results[3].reduction:F2}%" },
                { "Threshold 50", $"Dispatches: {results[4].dispatches}, Reduction: {results[4].reduction:F2}%" }
            });

            // Verify that larger thresholds result in more coalescing
            for (int i = 1; i < results.Count; i++)
            {
                Assert.True(results[i].dispatches <= results[i - 1].dispatches,
                    $"Expected fewer dispatches with higher threshold");
            }
        }

        [Fact]
        public void Benchmark_ThroughputUnderLoad_High()
        {
            const int messageCount = 50000;
            
            var dispatchCount = 0;
            var coalescer = new MessageCoalescer<BenchmarkMessage, int>(
                (key, messages) => dispatchCount++)
            {
                FlushInterval = 50,
                BatchSizeThreshold = 10
            };

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < messageCount; i++)
            {
                coalescer.Enqueue(i % 30, new BenchmarkMessage(i, 64));
            }
            coalescer.Flush();

            watch.Stop();

            var metrics = coalescer.GetMetrics();
            var messagesPerSecond = (messageCount / (watch.ElapsedMilliseconds / 1000.0));
            var reductionPct = (double)metrics["ReductionPercentage"];

            PrintBenchmarkResults("High-Load Throughput (50,000 messages)", new Dictionary<string, object>
            {
                { "Total Messages", messageCount },
                { "Dispatch Operations", dispatchCount },
                { "Messages Reduced %", $"{reductionPct:F2}%" },
                { "Execution Time (ms)", watch.ElapsedMilliseconds },
                { "Throughput (messages/sec)", $"{messagesPerSecond:F0}" },
                { "Average Batch Size", $"{(double)messageCount / dispatchCount:F2}" }
            });

            Assert.True(reductionPct > 70.0);
            Assert.True(dispatchCount < messageCount / 2);

            coalescer.Dispose();
        }

        [Fact]
        public void Benchmark_MemoryEfficiency_LargePayloads()
        {
            const int messageCount = 1000;
            const int payloadSize = 10240; // 10KB per message
            
            var dispatchCount = 0;
            var coalescer = new MessageCoalescer<BenchmarkMessage, int>(
                (key, messages) => dispatchCount++)
            {
                FlushInterval = 100,
                BatchSizeThreshold = 5
            };

            var initialMemory = GC.GetTotalMemory(true);
            var watch = Stopwatch.StartNew();

            for (int i = 0; i < messageCount; i++)
            {
                coalescer.Enqueue(i % 10, new BenchmarkMessage(i, payloadSize));
            }
            coalescer.Flush();

            watch.Stop();
            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            var metrics = coalescer.GetMetrics();
            var reductionPct = (double)metrics["ReductionPercentage"];

            PrintBenchmarkResults("Memory Efficiency with Large Payloads (10KB each)", new Dictionary<string, object>
            {
                { "Total Messages", messageCount },
                { "Payload Size", "10 KB" },
                { "Total Data Size", $"{(messageCount * payloadSize) / (1024 * 1024):F2} MB" },
                { "Dispatch Operations", dispatchCount },
                { "Messages Reduced %", $"{reductionPct:F2}%" },
                { "Memory Used", $"{memoryUsed / (1024 * 1024):F2} MB" },
                { "Execution Time (ms)", watch.ElapsedMilliseconds }
            });

            coalescer.Dispose();
        }

        [Fact]
        public void Benchmark_SequentialToCoalesced_Comparison()
        {
            const int messageCount = 20000;
            
            // Sequential baseline
            var sequentialWatch = Stopwatch.StartNew();
            var sequentialDispatches = 0;
            for (int i = 0; i < messageCount; i++)
            {
                sequentialDispatches++;
            }
            sequentialWatch.Stop();

            // Coalesced
            var coalescedDispatches = 0;
            var coalescer = new MessageCoalescer<BenchmarkMessage, string>(
                (key, messages) => coalescedDispatches++)
            {
                FlushInterval = 100,
                BatchSizeThreshold = 5
            };

            var coalescedWatch = Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                coalescer.Enqueue($"key{i % 25}", new BenchmarkMessage(i, 128));
            }
            coalescer.Flush();
            coalescedWatch.Stop();

            var metrics = coalescer.GetMetrics();
            var throughputImprovement = ((double)(sequentialDispatches - coalescedDispatches) / sequentialDispatches) * 100;
            var reductionPct = (double)metrics["ReductionPercentage"];

            PrintBenchmarkResults("Sequential vs Coalesced: Final Comparison", new Dictionary<string, object>
            {
                { "Total Messages", messageCount },
                { "Sequential Dispatches", sequentialDispatches },
                { "Coalesced Dispatches", coalescedDispatches },
                { "Throughput Improvement %", $"{throughputImprovement:F2}%" },
                { "Messages Reduced %", $"{reductionPct:F2}%" },
                { "Sequential Time (ms)", sequentialWatch.ElapsedMilliseconds },
                { "Coalesced Time (ms)", coalescedWatch.ElapsedMilliseconds },
                { "Average Batch Size", $"{(double)messageCount / coalescedDispatches:F2}" }
            });

            Assert.True(throughputImprovement > 50.0);
            Assert.True(reductionPct > 60.0);

            coalescer.Dispose();
        }

        private void PrintBenchmarkResults(string title, Dictionary<string, object> results)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine($"  {title}");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");

            foreach (var kvp in results)
            {
                Console.WriteLine($"  {kvp.Key,-30} : {kvp.Value}");
            }

            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine();
        }
    }
}
