using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

/// <summary>
/// Standalone performance benchmark runner for MonadoBlade optimizations
/// Generates comprehensive performance metrics without external dependencies
/// </summary>
public class PerformanceBenchmarkRunner
{
    private readonly List<BenchmarkResult> _results = new();
    private const int WarmupIterations = 100;

    public class BenchmarkResult
    {
        public string Name { get; set; } = "";
        public long AverageTicks { get; set; }
        public long MinTicks { get; set; }
        public long MaxTicks { get; set; }
        public double AverageMs => AverageTicks / 10000.0;
        public long AllocatedBytes { get; set; }
        public int IterationCount { get; set; }
        public double ThroughputPerSecond { get; set; }
    }

    public static void Main(string[] args)
    {
        var runner = new PerformanceBenchmarkRunner();
        runner.RunAllBenchmarks();
        runner.PrintResults();
    }

    private void RunAllBenchmarks()
    {
        Console.WriteLine("=== MonadoBlade Performance Benchmarks ===");
        Console.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        // String Interning Benchmarks
        Console.WriteLine("Running String Interning benchmarks...");
        RunStringInterningBenchmarks();

        // Object Pooling Benchmarks
        Console.WriteLine("Running Object Pooling benchmarks...");
        RunObjectPoolingBenchmarks();

        // Task Batching Benchmarks
        Console.WriteLine("Running Task Batching benchmarks...");
        RunTaskBatchingBenchmarks();

        // Lock-Free Collections Benchmarks
        Console.WriteLine("Running Lock-Free Collections benchmarks...");
        RunLockFreeBenchmarks();

        Console.WriteLine();
        Console.WriteLine($"Completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
    }

    private void RunStringInterningBenchmarks()
    {
        // Warmup
        var strings = new[] { "test1", "test2", "test3", "test4", "test5" };
        var dict = new Dictionary<string, string>();
        for (int i = 0; i < WarmupIterations; i++)
        {
            foreach (var s in strings)
            {
                if (!dict.ContainsKey(s))
                    dict[s] = s;
                _ = dict[s];
            }
        }

        // Benchmark: Dictionary lookup (simulating string interning)
        var sw = Stopwatch.StartNew();
        var results = new List<long>();
        const int iterations = 100000;

        for (int iteration = 0; iteration < 5; iteration++)
        {
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                _ = dict[strings[i % strings.Length]];
            }
            sw.Stop();
            results.Add(sw.ElapsedTicks);
        }

        var avg = new List<long>(results);
        avg.Sort();
        avg.RemoveAt(0);
        avg.RemoveAt(avg.Count - 1);

        _results.Add(new BenchmarkResult
        {
            Name = "String Interning - Dictionary Lookup",
            AverageTicks = (long)avg.Average(),
            MinTicks = avg[0],
            MaxTicks = avg[avg.Count - 1],
            IterationCount = iterations,
            ThroughputPerSecond = (iterations * 10000000.0) / avg[(int)(avg.Count * 0.5)]
        });
    }

    private void RunObjectPoolingBenchmarks()
    {
        const int poolSize = 500;
        const int iterations = 10000;

        // Pool Implementation
        var pool = new SimpleObjectPool<TestObject>(() => new TestObject(), obj => obj.Reset(), poolSize);

        // Warmup
        for (int i = 0; i < WarmupIterations; i++)
        {
            var obj = pool.Rent();
            pool.Return(obj);
        }

        // Benchmark: Rent/Return cycle
        var results = new List<long>();
        for (int iteration = 0; iteration < 5; iteration++)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var obj = pool.Rent();
                obj.Id = i;
                pool.Return(obj);
            }
            sw.Stop();
            results.Add(sw.ElapsedTicks);
        }

        var avg = new List<long>(results);
        avg.Sort();
        avg.RemoveAt(0);
        avg.RemoveAt(avg.Count - 1);

        _results.Add(new BenchmarkResult
        {
            Name = "Object Pooling - Rent/Return Cycle",
            AverageTicks = (long)avg.Average(),
            MinTicks = avg[0],
            MaxTicks = avg[avg.Count - 1],
            IterationCount = iterations,
            ThroughputPerSecond = (iterations * 10000000.0) / avg[(int)(avg.Count * 0.5)]
        });

        // Benchmark: Direct allocation (baseline)
        results.Clear();
        for (int iteration = 0; iteration < 5; iteration++)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var obj = new TestObject { Id = i };
                _ = obj.Id;
            }
            sw.Stop();
            results.Add(sw.ElapsedTicks);
        }

        avg = new List<long>(results);
        avg.Sort();
        avg.RemoveAt(0);
        avg.RemoveAt(avg.Count - 1);

        _results.Add(new BenchmarkResult
        {
            Name = "Object Allocation - Direct (Baseline)",
            AverageTicks = (long)avg.Average(),
            MinTicks = avg[0],
            MaxTicks = avg[avg.Count - 1],
            IterationCount = iterations,
            ThroughputPerSecond = (iterations * 10000000.0) / avg[(int)(avg.Count * 0.5)]
        });
    }

    private void RunTaskBatchingBenchmarks()
    {
        const int iterations = 5000;

        // Benchmark: Sequential processing
        var results = new List<long>();
        for (int iteration = 0; iteration < 5; iteration++)
        {
            var sw = Stopwatch.StartNew();
            var items = new List<int>();
            for (int i = 0; i < iterations; i++)
            {
                items.Add(i);
            }
            var sum = 0;
            foreach (var item in items)
                sum += item;
            sw.Stop();
            results.Add(sw.ElapsedTicks);
        }

        var avg = new List<long>(results);
        avg.Sort();
        avg.RemoveAt(0);
        avg.RemoveAt(avg.Count - 1);

        _results.Add(new BenchmarkResult
        {
            Name = "Task Batching - Sequential Processing",
            AverageTicks = (long)avg.Average(),
            MinTicks = avg[0],
            MaxTicks = avg[avg.Count - 1],
            IterationCount = iterations,
            ThroughputPerSecond = (iterations * 10000000.0) / avg[(int)(avg.Count * 0.5)]
        });

        // Benchmark: Concurrent processing
        results.Clear();
        for (int iteration = 0; iteration < 5; iteration++)
        {
            var sw = Stopwatch.StartNew();
            var tasks = new Task[Environment.ProcessorCount];
            int itemsPerTask = iterations / Environment.ProcessorCount;

            for (int t = 0; t < tasks.Length; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    var sum = 0;
                    for (int i = 0; i < itemsPerTask; i++)
                        sum += i;
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            results.Add(sw.ElapsedTicks);
        }

        avg = new List<long>(results);
        avg.Sort();
        avg.RemoveAt(0);
        avg.RemoveAt(avg.Count - 1);

        _results.Add(new BenchmarkResult
        {
            Name = "Task Batching - Concurrent Processing",
            AverageTicks = (long)avg.Average(),
            MinTicks = avg[0],
            MaxTicks = avg[avg.Count - 1],
            IterationCount = iterations,
            ThroughputPerSecond = (iterations * 10000000.0) / avg[(int)(avg.Count * 0.5)]
        });
    }

    private void RunLockFreeBenchmarks()
    {
        const int iterations = 50000;

        // Benchmark: ConcurrentQueue (baseline)
        var queue = new ConcurrentQueue<int>();
        var results = new List<long>();

        for (int iteration = 0; iteration < 5; iteration++)
        {
            var sw = Stopwatch.StartNew();
            var tasks = new Task[Environment.ProcessorCount];
            int itemsPerTask = iterations / Environment.ProcessorCount;

            for (int t = 0; t < tasks.Length; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < itemsPerTask; i++)
                        queue.Enqueue(i);
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            results.Add(sw.ElapsedTicks);
        }

        var avg = new List<long>(results);
        avg.Sort();
        avg.RemoveAt(0);
        avg.RemoveAt(avg.Count - 1);

        _results.Add(new BenchmarkResult
        {
            Name = "ConcurrentQueue - Concurrent Enqueue",
            AverageTicks = (long)avg.Average(),
            MinTicks = avg[0],
            MaxTicks = avg[avg.Count - 1],
            IterationCount = iterations,
            ThroughputPerSecond = (iterations * 10000000.0) / avg[(int)(avg.Count * 0.5)]
        });
    }

    private void PrintResults()
    {
        Console.WriteLine();
        Console.WriteLine("=== Benchmark Results ===");
        Console.WriteLine();
        Console.WriteLine("{0,-60} | {1,12} | {2,12} | {3,12} | {4,15}",
            "Benchmark Name", "Avg (ms)", "Min (ms)", "Max (ms)", "Throughput/sec");
        Console.WriteLine(new string('-', 130));

        foreach (var result in _results)
        {
            Console.WriteLine("{0,-60} | {1,12:F3} | {2,12:F3} | {3,12:F3} | {4,15:F0}",
                result.Name,
                result.AverageMs,
                result.MinTicks / 10000.0,
                result.MaxTicks / 10000.0,
                result.ThroughputPerSecond
            );
        }

        Console.WriteLine();
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Total benchmarks run: {_results.Count}");
        Console.WriteLine($"Average execution time: {_results.Average(r => r.AverageMs):F2}ms");
        Console.WriteLine();
    }

    public class TestObject
    {
        public int Id { get; set; }
        public string Data { get; set; } = "";
        public DateTime Timestamp { get; set; }

        public void Reset()
        {
            Id = 0;
            Data = "";
            Timestamp = DateTime.MinValue;
        }
    }

    public class SimpleObjectPool<T>
    {
        private readonly ConcurrentBag<T> _items;
        private readonly Func<T> _factory;
        private readonly Action<T>? _resetAction;

        public SimpleObjectPool(Func<T> factory, Action<T>? resetAction = null, int poolSize = 500)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _resetAction = resetAction;
            _items = new ConcurrentBag<T>();

            for (int i = 0; i < poolSize; i++)
                _items.Add(_factory());
        }

        public T Rent()
        {
            return _items.TryTake(out var item) ? item : _factory();
        }

        public void Return(T item)
        {
            _resetAction?.Invoke(item);
            _items.Add(item);
        }
    }
}
