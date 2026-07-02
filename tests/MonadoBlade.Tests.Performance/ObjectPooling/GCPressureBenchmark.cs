using MonadoBlade.Core.ObjectPooling;
using System.Diagnostics;

namespace MonadoBlade.Tests.Performance.ObjectPooling;

/// <summary>
/// GC Pressure Benchmark comparing allocation patterns with and without object pooling.
/// </summary>
public class GCPressureBenchmark
{
    private const int BenchmarkIterations = 10000;

    /// <summary>
    /// Represents benchmark results for memory and GC pressure.
    /// </summary>
    public class BenchmarkResults
    {
        /// <summary>Gets or sets the total bytes allocated.</summary>
        public long BytesAllocated { get; set; }

        /// <summary>Gets or sets the number of GC collections.</summary>
        public int GCCollections { get; set; }

        /// <summary>Gets or sets the peak memory in bytes.</summary>
        public long PeakMemory { get; set; }

        /// <summary>Gets or sets elapsed milliseconds.</summary>
        public long ElapsedMs { get; set; }

        /// <summary>Gets or sets allocations per millisecond.</summary>
        public double AllocationsPerMs { get; set; }

        /// <summary>Gets or sets the benchmark scenario name.</summary>
        public string Scenario { get; set; } = string.Empty;
    }

    /// <summary>
    /// Runs the baseline benchmark (allocations without pooling).
    /// </summary>
    /// <returns>Benchmark results.</returns>
    public static BenchmarkResults RunBaselineBenchmark()
    {
        // Force GC collection to start clean
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memBefore = GC.GetTotalAllocatedBytes();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();

        var objects = new List<TestObject>();
        for (int i = 0; i < BenchmarkIterations; i++)
        {
            objects.Add(new TestObject { Value = i, Name = $"Object_{i}" });
        }

        // Prevent optimization - use the objects
        var sum = objects.Sum(o => o.Value);
        var lastObject = objects.Last();

        sw.Stop();

        GC.Collect();
        var memAfter = GC.GetTotalAllocatedBytes();
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);

        var bytesAllocated = memAfter - memBefore;
        var totalCollections = (gen0After - gen0Before) + (gen1After - gen1Before) + (gen2After - gen2Before);

        return new BenchmarkResults
        {
            BytesAllocated = bytesAllocated,
            GCCollections = totalCollections,
            PeakMemory = memAfter,
            ElapsedMs = sw.ElapsedMilliseconds,
            AllocationsPerMs = (double)BenchmarkIterations / sw.ElapsedMilliseconds,
            Scenario = "Baseline (Direct Allocation)"
        };
    }

    /// <summary>
    /// Runs the optimized benchmark (allocations with object pooling).
    /// </summary>
    /// <returns>Benchmark results.</returns>
    public static BenchmarkResults RunOptimizedBenchmark()
    {
        // Force GC collection to start clean
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memBefore = GC.GetTotalAllocatedBytes();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();

        var pool = new ObjectPool<TestObject>(
            () => new TestObject(),
            obj => { obj.Value = 0; obj.Name = string.Empty; },
            poolSize: 500
        );

        var objects = new List<TestObject>();
        for (int i = 0; i < BenchmarkIterations; i++)
        {
            var obj = pool.Rent();
            obj.Value = i;
            obj.Name = $"Object_{i}";
            objects.Add(obj);
        }

        // Prevent optimization - use the objects
        var sum = objects.Sum(o => o.Value);
        var lastObject = objects.Last();

        // Return objects to pool
        foreach (var obj in objects)
        {
            pool.Return(obj);
        }

        sw.Stop();

        GC.Collect();
        var memAfter = GC.GetTotalAllocatedBytes();
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);

        var bytesAllocated = memAfter - memBefore;
        var totalCollections = (gen0After - gen0Before) + (gen1After - gen1Before) + (gen2After - gen2Before);

        var metrics = pool.GetMetrics();
        pool.Dispose();

        return new BenchmarkResults
        {
            BytesAllocated = bytesAllocated,
            GCCollections = totalCollections,
            PeakMemory = memAfter,
            ElapsedMs = sw.ElapsedMilliseconds,
            AllocationsPerMs = (double)BenchmarkIterations / sw.ElapsedMilliseconds,
            Scenario = "Optimized (Object Pooling)"
        };
    }

    /// <summary>
    /// Calculates improvement metrics between baseline and optimized benchmarks.
    /// </summary>
    public class ImprovementMetrics
    {
        /// <summary>Gets or sets memory usage improvement percentage.</summary>
        public double MemoryImprovement { get; set; }

        /// <summary>Gets or sets GC pressure improvement percentage.</summary>
        public double GCPressureImprovement { get; set; }

        /// <summary>Gets or sets throughput improvement percentage.</summary>
        public double ThroughputImprovement { get; set; }

        /// <summary>Gets or sets elapsed time improvement percentage.</summary>
        public double ElapsedTimeImprovement { get; set; }

        /// <summary>Gets the summary report.</summary>
        public string Summary =>
            $"Memory: {MemoryImprovement:F2}% | GC: {GCPressureImprovement:F2}% | Throughput: {ThroughputImprovement:F2}% | Time: {ElapsedTimeImprovement:F2}%";
    }

    /// <summary>
    /// Calculates improvement metrics.
    /// </summary>
    public static ImprovementMetrics CalculateImprovements(BenchmarkResults baseline, BenchmarkResults optimized)
    {
        var memoryImprovement = ((double)(baseline.BytesAllocated - optimized.BytesAllocated) 
            / baseline.BytesAllocated) * 100;
        
        var gcImprovement = baseline.GCCollections > 0 
            ? ((double)(baseline.GCCollections - optimized.GCCollections) 
            / baseline.GCCollections) * 100 
            : 0;
        
        var throughputImprovement = ((double)(optimized.AllocationsPerMs - baseline.AllocationsPerMs) 
            / baseline.AllocationsPerMs) * 100;
        
        var timeImprovement = ((double)(baseline.ElapsedMs - optimized.ElapsedMs) 
            / baseline.ElapsedMs) * 100;

        return new ImprovementMetrics
        {
            MemoryImprovement = memoryImprovement,
            GCPressureImprovement = gcImprovement,
            ThroughputImprovement = throughputImprovement,
            ElapsedTimeImprovement = timeImprovement
        };
    }

    /// <summary>
    /// Runs the complete benchmark suite and returns results.
    /// </summary>
    public static (BenchmarkResults Baseline, BenchmarkResults Optimized, ImprovementMetrics Improvements) RunFullBenchmark()
    {
        Console.WriteLine("=== GC Pressure Benchmark ===");
        Console.WriteLine($"Iterations: {BenchmarkIterations:N0}");
        Console.WriteLine();

        Console.WriteLine("Running baseline benchmark (direct allocation)...");
        var baseline = RunBaselineBenchmark();
        Console.WriteLine($"  Bytes allocated: {baseline.BytesAllocated:N0}");
        Console.WriteLine($"  GC collections: {baseline.GCCollections}");
        Console.WriteLine($"  Elapsed: {baseline.ElapsedMs}ms");
        Console.WriteLine();

        Console.WriteLine("Running optimized benchmark (object pooling)...");
        var optimized = RunOptimizedBenchmark();
        Console.WriteLine($"  Bytes allocated: {optimized.BytesAllocated:N0}");
        Console.WriteLine($"  GC collections: {optimized.GCCollections}");
        Console.WriteLine($"  Elapsed: {optimized.ElapsedMs}ms");
        Console.WriteLine();

        var improvements = CalculateImprovements(baseline, optimized);
        Console.WriteLine("=== Improvements ===");
        Console.WriteLine($"  Memory usage: {improvements.MemoryImprovement:F2}% reduction");
        Console.WriteLine($"  GC pressure: {improvements.GCPressureImprovement:F2}% reduction");
        Console.WriteLine($"  Throughput: {improvements.ThroughputImprovement:F2}% improvement");
        Console.WriteLine($"  Execution time: {improvements.ElapsedTimeImprovement:F2}% reduction");
        Console.WriteLine();

        return (baseline, optimized, improvements);
    }

    private class TestObject
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
