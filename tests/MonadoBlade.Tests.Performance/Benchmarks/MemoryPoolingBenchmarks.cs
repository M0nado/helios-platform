namespace MonadoBlade.Tests.Performance.Benchmarks;

using BenchmarkDotNet.Attributes;
using System.Buffers;

/// <summary>
/// Benchmarks for memory pooling optimization.
/// Measures the performance improvement from using ArrayPool vs allocating new arrays.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class MemoryPoolingBenchmarks
{
    private const int BufferSize = 4096;
    private const int Iterations = 1000;

    [Benchmark(Description = "Array allocation without pooling")]
    public long AllocateWithoutPooling()
    {
        long total = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var buffer = new byte[BufferSize];
            total += buffer.Length;
        }
        return total;
    }

    [Benchmark(Description = "Array allocation with ArrayPool")]
    public long AllocateWithPooling()
    {
        long total = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                total += buffer.Length;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        return total;
    }

    [Benchmark(Description = "Large buffer allocation without pooling")]
    public long AllocateLargeBufferWithoutPooling()
    {
        long total = 0;
        for (int i = 0; i < 100; i++)
        {
            var buffer = new byte[1024 * 1024]; // 1MB
            total += buffer.Length;
        }
        return total;
    }

    [Benchmark(Description = "Large buffer allocation with ArrayPool")]
    public long AllocateLargeBufferWithPooling()
    {
        long total = 0;
        for (int i = 0; i < 100; i++)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            try
            {
                total += buffer.Length;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        return total;
    }

    [Benchmark(Description = "Concurrent ArrayPool allocation")]
    public long ConcurrentPoolAllocation()
    {
        long total = 0;
        var tasks = new Task[Environment.ProcessorCount];

        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < Iterations / tasks.Length; i++)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                    Interlocked.Add(ref total, buffer.Length);
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            });
        }

        Task.WaitAll(tasks);
        return total;
    }

    [Benchmark(Description = "Multiple pool allocations with reuse")]
    public long PoolAllocationWithReuse()
    {
        long total = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            for (int i = 0; i < Iterations; i++)
            {
                total += buffer.Length;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        return total;
    }
}
