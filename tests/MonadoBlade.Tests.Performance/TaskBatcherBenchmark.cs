using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MonadoBlade.Core.Concurrency;

namespace MonadoBlade.Tests.Performance
{
    /// <summary>
    /// Performance benchmark for TaskBatcher comparing sequential vs batched processing.
    /// Measures throughput improvement and provides detailed metrics.
    /// </summary>
    public class TaskBatcherBenchmark
    {
        private const int WarmupIterations = 100;
        private const int MeasuredIterations = 5;

        public static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         TaskBatcher Performance Benchmark Results               ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            var benchmark = new TaskBatcherBenchmark();
            
            // Run benchmarks
            var sequentialResults = benchmark.RunSequentialBenchmark();
            var batchedSmallResults = benchmark.RunBatchedBenchmark(50, 100);
            var batchedLargeResults = benchmark.RunBatchedBenchmark(100, 100);

            // Display results
            benchmark.DisplayResults("Sequential Processing (Baseline)", sequentialResults);
            Console.WriteLine();
            benchmark.DisplayResults("Batched Processing (Batch Size: 50, Interval: 100ms)", batchedSmallResults);
            Console.WriteLine();
            benchmark.DisplayResults("Batched Processing (Batch Size: 100, Interval: 100ms)", batchedLargeResults);

            // Calculate and display improvements
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              Performance Improvement Analysis                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            double improvementSmall = benchmark.CalculateImprovement(
                sequentialResults.AverageThroughputItemsPerMs,
                batchedSmallResults.AverageThroughputItemsPerMs);
            
            double improvementLarge = benchmark.CalculateImprovement(
                sequentialResults.AverageThroughputItemsPerMs,
                batchedLargeResults.AverageThroughputItemsPerMs);

            Console.WriteLine($"Sequential Baseline Throughput:      {sequentialResults.AverageThroughputItemsPerMs:F2} items/ms");
            Console.WriteLine();
            Console.WriteLine($"Batched (Size 50) Throughput:        {batchedSmallResults.AverageThroughputItemsPerMs:F2} items/ms");
            Console.WriteLine($"  → Improvement vs Sequential:       {improvementSmall:+0.00;-0.00;0.00}%");
            Console.WriteLine();
            Console.WriteLine($"Batched (Size 100) Throughput:       {batchedLargeResults.AverageThroughputItemsPerMs:F2} items/ms");
            Console.WriteLine($"  → Improvement vs Sequential:       {improvementLarge:+0.00;-0.00;0.00}%");
            Console.WriteLine();

            // Callback efficiency analysis
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              Callback Efficiency Analysis                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            var callbackAnalysis = benchmark.AnalyzeCallbackReduction();
            Console.WriteLine($"Items Processed:                     {callbackAnalysis.ItemCount}");
            Console.WriteLine($"Sequential Callbacks:                {callbackAnalysis.ItemCount} (1 per item)");
            Console.WriteLine($"Batched Callbacks (Size 50):         {callbackAnalysis.BatchedCallbacks50}");
            Console.WriteLine($"Batched Callbacks (Size 100):        {callbackAnalysis.BatchedCallbacks100}");
            Console.WriteLine();
            Console.WriteLine($"Callback Reduction (Size 50):        {callbackAnalysis.ReductionPercent50:F1}%");
            Console.WriteLine($"Callback Reduction (Size 100):       {callbackAnalysis.ReductionPercent100:F1}%");
            Console.WriteLine();

            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    Benchmark Complete                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Runs sequential processing benchmark.
        /// </summary>
        private BenchmarkResult RunSequentialBenchmark()
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                var dummy = 0;
                for (int j = 0; j < 1000; j++)
                {
                    dummy++;
                }
            }

            var times = new List<long>();

            // Measured iterations
            for (int iter = 0; iter < MeasuredIterations; iter++)
            {
                var stopwatch = Stopwatch.StartNew();

                int count = 0;
                for (int i = 0; i < 1000; i++)
                {
                    count++;
                }

                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            return AnalyzeResults(1000, times);
        }

        /// <summary>
        /// Runs batched processing benchmark.
        /// </summary>
        private BenchmarkResult RunBatchedBenchmark(int batchSize, int flushInterval)
        {
            var times = new List<long>();

            // Warmup
            var warmupBatcher = new TaskBatcher<int>(items => { }, batchSize, flushInterval);
            for (int i = 0; i < WarmupIterations; i++)
            {
                warmupBatcher.Enqueue(i % 1000);
            }
            warmupBatcher.Dispose();
            Thread.Sleep(100);

            // Measured iterations
            for (int iter = 0; iter < MeasuredIterations; iter++)
            {
                int processedCount = 0;
                var batcher = new TaskBatcher<int>(items =>
                {
                    Interlocked.Add(ref processedCount, items.Count);
                }, batchSize, flushInterval);

                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < 1000; i++)
                {
                    batcher.Enqueue(i);
                }

                batcher.Flush();
                Thread.Sleep(200); // Wait for async callbacks

                stopwatch.Stop();
                batcher.Dispose();

                times.Add(stopwatch.ElapsedMilliseconds);
            }

            return AnalyzeResults(1000, times);
        }

        /// <summary>
        /// Analyzes benchmark results and calculates metrics.
        /// </summary>
        private BenchmarkResult AnalyzeResults(int itemCount, List<long> elapsedMilliseconds)
        {
            double minTime = elapsedMilliseconds[0];
            double maxTime = elapsedMilliseconds[elapsedMilliseconds.Count - 1];
            double avgTime = 0;

            foreach (var time in elapsedMilliseconds)
            {
                if (time < minTime) minTime = time;
                if (time > maxTime) maxTime = time;
                avgTime += time;
            }

            avgTime /= elapsedMilliseconds.Count;

            double minThroughput = itemCount / Math.Max(minTime, 1);
            double maxThroughput = itemCount / Math.Max(maxTime, 1);
            double avgThroughput = itemCount / Math.Max(avgTime, 1);

            return new BenchmarkResult
            {
                ItemCount = itemCount,
                MinTimeMs = (long)minTime,
                MaxTimeMs = (long)maxTime,
                AverageTimeMs = (long)avgTime,
                MinThroughputItemsPerMs = minThroughput,
                MaxThroughputItemsPerMs = maxThroughput,
                AverageThroughputItemsPerMs = avgThroughput
            };
        }

        /// <summary>
        /// Displays benchmark results in a formatted table.
        /// </summary>
        private void DisplayResults(string title, BenchmarkResult result)
        {
            Console.WriteLine($"║ {title,-62} ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ Items Processed:                    {result.ItemCount,-21} ║");
            Console.WriteLine($"║ Min Time:                           {result.MinTimeMs,19} ms ║");
            Console.WriteLine($"║ Max Time:                           {result.MaxTimeMs,19} ms ║");
            Console.WriteLine($"║ Average Time:                       {result.AverageTimeMs,19} ms ║");
            Console.WriteLine("╟────────────────────────────────────────────────────────────────╢");
            Console.WriteLine($"║ Min Throughput:                     {result.MinThroughputItemsPerMs,16:F2} items/ms ║");
            Console.WriteLine($"║ Max Throughput:                     {result.MaxThroughputItemsPerMs,16:F2} items/ms ║");
            Console.WriteLine($"║ Avg Throughput:                     {result.AverageThroughputItemsPerMs,16:F2} items/ms ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Calculates percentage improvement.
        /// </summary>
        private double CalculateImprovement(double baseline, double current)
        {
            if (baseline <= 0 || current <= 0)
                return 0;

            // Improvement % = (current / baseline - 1) * 100
            return ((current / baseline) - 1) * 100;
        }

        /// <summary>
        /// Analyzes callback invocation reduction.
        /// </summary>
        private CallbackAnalysis AnalyzeCallbackReduction()
        {
            const int itemCount = 1000;

            // Count callbacks for batch size 50
            int callbacks50 = 0;
            var batcher50 = new TaskBatcher<int>(items =>
            {
                Interlocked.Increment(ref callbacks50);
            }, 50, 1000);

            for (int i = 0; i < itemCount; i++)
            {
                batcher50.Enqueue(i);
            }
            batcher50.Flush();
            Thread.Sleep(150);
            batcher50.Dispose();

            // Count callbacks for batch size 100
            int callbacks100 = 0;
            var batcher100 = new TaskBatcher<int>(items =>
            {
                Interlocked.Increment(ref callbacks100);
            }, 100, 1000);

            for (int i = 0; i < itemCount; i++)
            {
                batcher100.Enqueue(i);
            }
            batcher100.Flush();
            Thread.Sleep(150);
            batcher100.Dispose();

            double reductionPercent50 = (1 - (double)callbacks50 / itemCount) * 100;
            double reductionPercent100 = (1 - (double)callbacks100 / itemCount) * 100;

            return new CallbackAnalysis
            {
                ItemCount = itemCount,
                BatchedCallbacks50 = callbacks50,
                BatchedCallbacks100 = callbacks100,
                ReductionPercent50 = reductionPercent50,
                ReductionPercent100 = reductionPercent100
            };
        }

        /// <summary>
        /// Represents benchmark results.
        /// </summary>
        private class BenchmarkResult
        {
            public int ItemCount { get; set; }
            public long MinTimeMs { get; set; }
            public long MaxTimeMs { get; set; }
            public long AverageTimeMs { get; set; }
            public double MinThroughputItemsPerMs { get; set; }
            public double MaxThroughputItemsPerMs { get; set; }
            public double AverageThroughputItemsPerMs { get; set; }
        }

        /// <summary>
        /// Represents callback analysis results.
        /// </summary>
        private class CallbackAnalysis
        {
            public int ItemCount { get; set; }
            public int BatchedCallbacks50 { get; set; }
            public int BatchedCallbacks100 { get; set; }
            public double ReductionPercent50 { get; set; }
            public double ReductionPercent100 { get; set; }
        }
    }
}
