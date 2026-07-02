namespace MonadoBlade.Tests.Performance;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Collections.Concurrent;
using System.Data;
using MonadoBlade.Core.ObjectPooling;
using MonadoBlade.Core.Optimization;
using MonadoBlade.Core.Concurrency;
using MonadoBlade.Core.Caching;
using MonadoBlade.Core.Data;

/// <summary>
/// Comprehensive performance benchmarks for MonadoBlade optimizations.
/// Measures: throughput, memory allocation, GC pressure, and latency.
/// </summary>

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class StringInterningBenchmarks
{
    private StringInterningPool _pool = null!;
    private const int IterationCount = 10000;
    private string[] _testStrings = null!;

    [GlobalSetup]
    public void Setup()
    {
        _pool = StringInterningPool.Instance;
        _testStrings = new[]
        {
            "hermes-opt-1-security",
            "hermes-opt-2-drivers",
            "hermes-opt-3-features",
            "hermes-opt-4-gpu",
            "common-event-type",
            "system-notification",
            "data-processing",
            "cache-invalidation",
            "lock-free-queue",
            "task-batch-operation"
        };
    }

    [Benchmark(Description = "String Interning - Repeated Lookups")]
    public string StringInternLookups()
    {
        string result = "";
        for (int i = 0; i < IterationCount; i++)
        {
            result = _pool.Intern(_testStrings[i % _testStrings.Length]);
        }
        return result;
    }

    [Benchmark(Description = "String Interning - New Strings")]
    public string StringInternNewStrings()
    {
        string result = "";
        for (int i = 0; i < 1000; i++)
        {
            result = _pool.Intern($"dynamic-string-{i}");
        }
        return result;
    }

    [Benchmark(Description = "String Comparison - Interned vs Regular")]
    public bool StringComparisonInterned()
    {
        var interned1 = _pool.Intern("hermes-opt-1-security");
        var interned2 = _pool.Intern("hermes-opt-1-security");
        bool result = false;
        for (int i = 0; i < IterationCount; i++)
        {
            result = ReferenceEquals(interned1, interned2);
        }
        return result;
    }

    [Benchmark(Description = "String Comparison - Regular")]
    public bool StringComparisonRegular()
    {
        var str1 = "hermes-opt-1-security";
        var str2 = "hermes-opt-1-security";
        bool result = false;
        for (int i = 0; i < IterationCount; i++)
        {
            result = str1 == str2;
        }
        return result;
    }
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class ObjectPoolingBenchmarks
{
    private ObjectPool<TestPoolableObject> _pool = null!;
    private const int IterationCount = 5000;
    private const int PoolSize = 500;

    public class TestPoolableObject
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

    [GlobalSetup]
    public void Setup()
    {
        _pool = new ObjectPool<TestPoolableObject>(
            () => new TestPoolableObject(),
            obj => obj.Reset(),
            PoolSize
        );
    }

    [Benchmark(Description = "Object Pool - Rent/Return Cycle")]
    public void ObjectPoolRentReturn()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var obj = _pool.Rent();
            obj.Id = i;
            obj.Data = $"Data-{i}";
            obj.Timestamp = DateTime.UtcNow;
            _pool.Return(obj);
        }
    }

    [Benchmark(Description = "Object Allocation - No Pooling")]
    public void ObjectAllocationWithoutPooling()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var obj = new TestPoolableObject
            {
                Id = i,
                Data = $"Data-{i}",
                Timestamp = DateTime.UtcNow
            };
            // Simulate usage
            _ = obj.Data;
        }
    }

    [Benchmark(Description = "Object Pool - Contention Under Load")]
    public void ObjectPoolHighContention()
    {
        var tasks = new System.Threading.Tasks.Task[Environment.ProcessorCount];
        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < IterationCount / Environment.ProcessorCount; i++)
                {
                    var obj = _pool.Rent();
                    obj.Id = i;
                    _pool.Return(obj);
                }
            });
        }
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _pool?.Dispose();
    }
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class TaskBatchingBenchmarks
{
    private TaskBatcher<int> _batcher = null!;
    private List<int> _batchedItems = null!;
    private volatile int _batchCount = 0;

    [GlobalSetup]
    public void Setup()
    {
        _batchedItems = new List<int>();
        _batcher = new TaskBatcher<int>(
            batch =>
            {
                lock (_batchedItems)
                {
                    _batchedItems.AddRange(batch);
                    Interlocked.Increment(ref _batchCount);
                }
            },
            batchSize: 100,
            flushInterval: 50
        );
    }

    [Benchmark(Description = "Task Batching - Sequential Add")]
    public void TaskBatchingSequential()
    {
        _batchCount = 0;
        _batchedItems.Clear();
        
        for (int i = 0; i < 5000; i++)
        {
            _batcher.Add(i);
        }
        
        // Wait for final batch
        System.Threading.Thread.Sleep(100);
    }

    [Benchmark(Description = "Task Batching - Concurrent Add")]
    public void TaskBatchingConcurrent()
    {
        _batchCount = 0;
        _batchedItems.Clear();
        
        var tasks = new System.Threading.Tasks.Task[Environment.ProcessorCount];
        int itemsPerTask = 5000 / Environment.ProcessorCount;
        
        for (int t = 0; t < tasks.Length; t++)
        {
            int taskId = t;
            tasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < itemsPerTask; i++)
                {
                    _batcher.Add(taskId * itemsPerTask + i);
                }
            });
        }
        
        System.Threading.Tasks.Task.WaitAll(tasks);
        System.Threading.Thread.Sleep(100);
    }

    [Benchmark(Description = "Direct Processing - No Batching")]
    public void DirectProcessingNoBatching()
    {
        var items = new List<int>();
        for (int i = 0; i < 5000; i++)
        {
            items.Add(i);
        }
        
        // Simulate processing
        int sum = 0;
        foreach (var item in items)
        {
            sum += item;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _batcher?.Dispose();
    }
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class ConnectionPoolingBenchmarks
{
    private ConnectionPool _pool = null!;
    private const int IterationCount = 1000;

    public class MockDbConnection : IDbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "MockDb";
        public ConnectionState State => _state;

        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) { }
        public void Close() => _state = ConnectionState.Closed;
        public IDbCommand CreateCommand() => new MockDbCommand();
        public void Dispose() { }
        public void Open() => _state = ConnectionState.Open;
    }

    public class MockDbCommand : IDbCommand
    {
        public string CommandText { get; set; } = "";
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }
        public IDbConnection Connection { get; set; } = null!;
        public IDataParameterCollection Parameters => throw new NotImplementedException();
        public IDbTransaction Transaction { get; set; } = null!;
        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public IDbDataParameter CreateParameter() => throw new NotImplementedException();
        public void Dispose() { }
        public int ExecuteNonQuery() => 0;
        public IDataReader ExecuteReader() => throw new NotImplementedException();
        public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotImplementedException();
        public object ExecuteScalar() => null!;
        public void Prepare() { }
    }

    [GlobalSetup]
    public void Setup()
    {
        _pool = new ConnectionPool(
            () => new MockDbConnection { ConnectionString = "mock://localhost" },
            minPoolSize: 5,
            maxPoolSize: 50
        );
    }

    [Benchmark(Description = "Connection Pool - Acquire/Release")]
    public void ConnectionPoolAcquireRelease()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var conn = _pool.AcquireConnection();
            // Simulate usage
            _ = conn.State;
            _pool.ReleaseConnection(conn);
        }
    }

    [Benchmark(Description = "Connection Creation - No Pooling")]
    public void ConnectionCreationNoDirect()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var conn = new MockDbConnection { ConnectionString = "mock://localhost" };
            conn.Open();
            _ = conn.State;
            conn.Close();
        }
    }

    [Benchmark(Description = "Connection Pool - High Concurrency")]
    public void ConnectionPoolHighConcurrency()
    {
        var tasks = new System.Threading.Tasks.Task[Environment.ProcessorCount * 2];
        int tasksPerThread = IterationCount / tasks.Length;
        
        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < tasksPerThread; i++)
                {
                    var conn = _pool.AcquireConnection();
                    _ = conn.State;
                    _pool.ReleaseConnection(conn);
                }
            });
        }
        
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _pool?.Dispose();
    }
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class IntelligentCachingBenchmarks
{
    private IntelligentCache<string, string> _cache = null!;
    private const int IterationCount = 10000;

    [GlobalSetup]
    public void Setup()
    {
        _cache = new IntelligentCache<string, string>(
            capacity: 1000,
            defaultTtl: TimeSpan.FromSeconds(60)
        );
        
        // Pre-populate cache
        for (int i = 0; i < 100; i++)
        {
            _cache.Set($"key-{i}", $"value-{i}", TimeSpan.FromSeconds(60));
        }
    }

    [Benchmark(Description = "Cache - Get Hit")]
    public string CacheGetHit()
    {
        string result = "";
        for (int i = 0; i < IterationCount; i++)
        {
            _cache.TryGet($"key-{i % 100}", out var value);
            result = value ?? "";
        }
        return result;
    }

    [Benchmark(Description = "Cache - Set")]
    public void CacheSet()
    {
        for (int i = 0; i < 1000; i++)
        {
            _cache.Set($"new-key-{i}", $"new-value-{i}", TimeSpan.FromSeconds(60));
        }
    }

    [Benchmark(Description = "Cache - Mixed Operations")]
    public void CacheMixedOperations()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            if (i % 3 == 0)
            {
                _cache.Set($"mixed-key-{i}", $"mixed-value-{i}");
            }
            else if (i % 3 == 1)
            {
                _cache.TryGet($"key-{i % 100}", out _);
            }
            else
            {
                _cache.Invalidate($"key-{i % 100}");
            }
        }
    }

    [Benchmark(Description = "Dictionary - Baseline")]
    public void DictionaryBaseline()
    {
        var dict = new ConcurrentDictionary<string, string>();
        
        for (int i = 0; i < 100; i++)
        {
            dict.TryAdd($"key-{i}", $"value-{i}");
        }
        
        for (int i = 0; i < IterationCount; i++)
        {
            dict.TryGetValue($"key-{i % 100}", out _);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cache?.Dispose();
    }
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class LockFreeConcurrencyBenchmarks
{
    private LockFreeQueue<int> _lockFreeQueue = null!;
    private ConcurrentQueue<int> _standardQueue = null!;
    private const int IterationCount = 50000;

    public class LockFreeQueue<T>
    {
        private class Node
        {
            public T Value { get; set; } = default!;
            public Node? Next { get; set; }
        }

        private Node _head;
        private Node _tail;

        public LockFreeQueue()
        {
            _head = _tail = new Node();
        }

        public void Enqueue(T value)
        {
            var newNode = new Node { Value = value };
            Node oldTail;
            
            while (true)
            {
                oldTail = _tail;
                var oldTailNext = oldTail.Next;
                
                if (oldTail == _tail)
                {
                    if (oldTailNext == null)
                    {
                        if (System.Threading.Interlocked.CompareExchange(ref oldTail.Next, newNode, null) == null)
                        {
                            System.Threading.Interlocked.CompareExchange(ref _tail, newNode, oldTail);
                            break;
                        }
                    }
                    else
                    {
                        System.Threading.Interlocked.CompareExchange(ref _tail, oldTailNext, oldTail);
                    }
                }
            }
        }

        public bool TryDequeue(out T value)
        {
            while (true)
            {
                var oldHead = _head;
                var oldHeadNext = oldHead.Next;
                
                if (oldHead == _head)
                {
                    if (oldHeadNext == null)
                    {
                        value = default!;
                        return false;
                    }
                    
                    if (System.Threading.Interlocked.CompareExchange(ref _head, oldHeadNext, oldHead) == oldHead)
                    {
                        value = oldHeadNext.Value;
                        return true;
                    }
                }
            }
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _lockFreeQueue = new LockFreeQueue<int>();
        _standardQueue = new ConcurrentQueue<int>();
    }

    [Benchmark(Description = "Lock-Free Queue - Sequential")]
    public void LockFreeQueueSequential()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            _lockFreeQueue.Enqueue(i);
        }
        
        for (int i = 0; i < IterationCount; i++)
        {
            _lockFreeQueue.TryDequeue(out _);
        }
    }

    [Benchmark(Description = "Lock-Free Queue - Concurrent")]
    public void LockFreeQueueConcurrent()
    {
        var tasks = new System.Threading.Tasks.Task[Environment.ProcessorCount];
        int itemsPerTask = IterationCount / Environment.ProcessorCount;
        
        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < itemsPerTask; i++)
                {
                    _lockFreeQueue.Enqueue(i);
                }
            });
        }
        
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    [Benchmark(Description = "ConcurrentQueue - Baseline")]
    public void ConcurrentQueueBaseline()
    {
        var tasks = new System.Threading.Tasks.Task[Environment.ProcessorCount];
        int itemsPerTask = IterationCount / Environment.ProcessorCount;
        
        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < itemsPerTask; i++)
                {
                    _standardQueue.Enqueue(i);
                }
            });
        }
        
        System.Threading.Tasks.Task.WaitAll(tasks);
    }
}

/// <summary>
/// Integration benchmark measuring combined effect of all optimizations
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 2, targetCount: 3)]
public class IntegratedOptimizationsBenchmark
{
    private StringInterningPool _stringPool = null!;
    private ObjectPool<TestMessage> _objectPool = null!;
    private TaskBatcher<TestMessage> _batcher = null!;
    private IntelligentCache<string, TestMessage> _cache = null!;
    private List<TestMessage> _processedMessages = null!;

    public class TestMessage
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Data { get; set; } = "";
        public DateTime Timestamp { get; set; }

        public void Reset()
        {
            Id = "";
            Type = "";
            Data = "";
            Timestamp = DateTime.MinValue;
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _stringPool = StringInterningPool.Instance;
        _objectPool = new ObjectPool<TestMessage>(
            () => new TestMessage(),
            m => m.Reset(),
            500
        );
        _processedMessages = new List<TestMessage>();
        _batcher = new TaskBatcher<TestMessage>(
            batch =>
            {
                lock (_processedMessages)
                {
                    _processedMessages.AddRange(batch);
                }
            },
            batchSize: 100,
            flushInterval: 50
        );
        _cache = new IntelligentCache<string, TestMessage>(1000, TimeSpan.FromSeconds(60));
    }

    [Benchmark(Description = "Integrated - Full Message Processing Pipeline")]
    public void IntegratedFullPipeline()
    {
        _processedMessages.Clear();

        for (int i = 0; i < 5000; i++)
        {
            // Allocate from pool
            var msg = _objectPool.Rent();
            
            // Use string interning for common strings
            msg.Id = _stringPool.Intern($"msg-{i % 10}");
            msg.Type = _stringPool.Intern("event");
            msg.Data = $"Data-{i}";
            msg.Timestamp = DateTime.UtcNow;
            
            // Cache it
            _cache.Set(msg.Id, msg, TimeSpan.FromSeconds(60));
            
            // Batch process
            _batcher.Add(msg);
        }
        
        System.Threading.Thread.Sleep(100);
    }

    [Benchmark(Description = "Baseline - No Optimizations")]
    public void BaselineNoop()
    {
        var messages = new List<TestMessage>();
        
        for (int i = 0; i < 5000; i++)
        {
            var msg = new TestMessage
            {
                Id = $"msg-{i}",
                Type = "event",
                Data = $"Data-{i}",
                Timestamp = DateTime.UtcNow
            };
            messages.Add(msg);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _objectPool?.Dispose();
        _batcher?.Dispose();
        _cache?.Dispose();
    }
}
