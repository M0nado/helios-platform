using MonadoBlade.Core.ObjectPooling;
using Xunit;

namespace MonadoBlade.Tests.Unit.ObjectPooling;

public class ObjectPoolTests
{
    [Fact]
    public void Constructor_CreatesPoolWithCorrectSize()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 50);
        
        Assert.Equal(50, pool.PoolSize);
        Assert.Equal(50, pool.AvailableCount);
        
        pool.Dispose();
    }

    [Fact]
    public void Constructor_DefaultPoolSizeIs500()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        
        Assert.Equal(500, pool.PoolSize);
        
        pool.Dispose();
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidPoolSize()
    {
        Assert.Throws<ArgumentException>(() => new ObjectPool<TestObject>(() => new TestObject(), poolSize: 0));
        Assert.Throws<ArgumentException>(() => new ObjectPool<TestObject>(() => new TestObject(), poolSize: -1));
    }

    [Fact]
    public void Constructor_ThrowsOnNullFactory()
    {
        Assert.Throws<ArgumentNullException>(() => new ObjectPool<TestObject>(null!));
    }

    [Fact]
    public void Rent_ReturnsObjectFromPool()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 10);
        
        var obj = pool.Rent();
        
        Assert.NotNull(obj);
        Assert.Equal(9, pool.AvailableCount);
        
        pool.Dispose();
    }

    [Fact]
    public void Rent_CreatesNewObjectWhenPoolEmpty()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 1);
        
        var obj1 = pool.Rent();
        Assert.Equal(0, pool.AvailableCount);
        
        var obj2 = pool.Rent();
        Assert.NotNull(obj2);
        Assert.NotEqual(obj1, obj2);
        
        pool.Dispose();
    }

    [Fact]
    public void Return_PutsObjectBackInPool()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 10);
        
        var obj = pool.Rent();
        Assert.Equal(9, pool.AvailableCount);
        
        pool.Return(obj);
        Assert.Equal(10, pool.AvailableCount);
        
        pool.Dispose();
    }

    [Fact]
    public void Return_CallsResetAction()
    {
        var resetCalled = false;
        var pool = new ObjectPool<TestObject>(
            () => new TestObject(),
            obj => { obj.Value = 0; resetCalled = true; },
            poolSize: 10
        );

        var obj = pool.Rent();
        obj.Value = 42;
        
        pool.Return(obj);
        
        Assert.True(resetCalled);
        Assert.Equal(0, obj.Value);
        
        pool.Dispose();
    }

    [Fact]
    public void Return_IgnoresNullObjects()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 10);
        var initialCount = pool.AvailableCount;
        
        pool.Return(null!);
        
        Assert.Equal(initialCount, pool.AvailableCount);
        
        pool.Dispose();
    }

    [Fact]
    public void Return_DoesNotExceedPoolSize()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 5);
        var maxPoolSize = pool.PoolSize;
        
        var objs = new List<TestObject>();
        for (int i = 0; i < 10; i++)
        {
            objs.Add(pool.Rent());
        }
        
        foreach (var obj in objs)
        {
            pool.Return(obj);
        }
        
        Assert.True(pool.AvailableCount <= maxPoolSize);
        
        pool.Dispose();
    }

    [Fact]
    public void GetMetrics_ReturnsAccurateHits()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 10);
        
        var obj = pool.Rent();
        pool.Return(obj);
        pool.Rent();
        
        var metrics = pool.GetMetrics();
        
        Assert.Equal(2, metrics.TotalAllocations);
        Assert.Equal(1, metrics.PoolHits);
        Assert.Equal(1, metrics.PoolMisses);
        
        pool.Dispose();
    }

    [Fact]
    public void GetMetrics_CalculatesHitRate()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 10);
        
        var obj = pool.Rent();
        pool.Return(obj);
        pool.Rent();
        pool.Return(obj);
        pool.Rent();
        
        var metrics = pool.GetMetrics();
        
        // 2 hits out of 3 allocations = 66.67%
        Assert.True(metrics.HitRate > 60.0 && metrics.HitRate < 70.0);
        
        pool.Dispose();
    }

    [Fact]
    public void GetMetrics_TracksReturns()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 10);
        
        var obj1 = pool.Rent();
        var obj2 = pool.Rent();
        
        pool.Return(obj1);
        pool.Return(obj2);
        
        var metrics = pool.GetMetrics();
        
        Assert.Equal(2, metrics.TotalReturns);
        
        pool.Dispose();
    }

    [Fact]
    public void ConcurrentRentReturn_ThreadSafe()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 50);
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        
        for (int t = 0; t < 20; t++)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < 100; i++)
                    {
                        var obj = pool.Rent();
                        obj.Value = i;
                        pool.Return(obj);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
            thread.Start();
        }
        
        foreach (var thread in threads)
        {
            thread.Join();
        }
        
        Assert.Empty(exceptions);
        
        var metrics = pool.GetMetrics();
        Assert.Equal(2000, metrics.TotalAllocations);
        
        pool.Dispose();
    }

    [Fact]
    public void Dispose_ThrowsOnSubsequentOperations()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        pool.Dispose();
        
        Assert.Throws<ObjectDisposedException>(() => pool.Rent());
        Assert.Throws<ObjectDisposedException>(() => pool.GetMetrics());
    }

    [Fact]
    public void PoolExhaustion_GracefullyCreatesNew()
    {
        var allocationCount = 0;
        var pool = new ObjectPool<TestObject>(
            () => { allocationCount++; return new TestObject(); },
            poolSize: 5
        );
        
        var initialAllocations = allocationCount;
        
        var rented = new List<TestObject>();
        for (int i = 0; i < 10; i++)
        {
            rented.Add(pool.Rent());
        }
        
        // We should have created 5 from initialization + 5 additional
        Assert.True(allocationCount >= initialAllocations + 5);
        
        pool.Dispose();
    }

    [Fact]
    public void AllocationReductionRatio_CalculatesCorrectly()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 10);
        
        for (int i = 0; i < 10; i++)
        {
            var obj = pool.Rent();
            pool.Return(obj);
        }
        
        var metrics = pool.GetMetrics();
        
        // All 10 should be hits since pool was pre-filled
        Assert.Equal(1.0, metrics.AllocationReductionRatio);
        
        pool.Dispose();
    }

    [Fact]
    public void GCPressureReduction_PoolBetterThanAllocation()
    {
        // Test allocation without pool
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var beforeNoPool = GC.GetTotalAllocatedBytes();
        var noPoolCollection0 = GC.CollectionCount(0);
        
        for (int i = 0; i < 1000; i++)
        {
            var obj = new TestObject();
            obj.Value = i;
        }
        
        GC.Collect();
        var afterNoPool = GC.GetTotalAllocatedBytes();
        var noPoolCollection1 = GC.CollectionCount(0);
        var noPoolCollections = noPoolCollection1 - noPoolCollection0;
        
        // Test allocation with pool
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var beforePool = GC.GetTotalAllocatedBytes();
        var poolCollection0 = GC.CollectionCount(0);
        
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 100);
        for (int i = 0; i < 1000; i++)
        {
            var obj = pool.Rent();
            obj.Value = i;
            pool.Return(obj);
        }
        
        GC.Collect();
        var afterPool = GC.GetTotalAllocatedBytes();
        var poolCollection1 = GC.CollectionCount(0);
        var poolCollections = poolCollection1 - poolCollection0;
        
        var noPoolAllocation = afterNoPool - beforeNoPool;
        var poolAllocation = afterPool - beforePool;
        
        // Pool should allocate less overall
        Assert.True(poolAllocation <= noPoolAllocation);
        
        pool.Dispose();
    }

    [Fact]
    public void ConcurrentMetricsAccuracy_AllCountsMatch()
    {
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 50);
        var threads = new List<Thread>();
        var threadCount = 10;
        var operationsPerThread = 100;
        
        for (int t = 0; t < threadCount; t++)
        {
            var thread = new Thread(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var obj = pool.Rent();
                    pool.Return(obj);
                }
            });
            threads.Add(thread);
            thread.Start();
        }
        
        foreach (var thread in threads)
        {
            thread.Join();
        }
        
        var metrics = pool.GetMetrics();
        
        // Each thread does 100 operations, 10 threads = 1000 total allocations
        Assert.Equal(1000, metrics.TotalAllocations);
        Assert.Equal(1000, metrics.TotalReturns);
        
        // Almost all should be hits since pool is pre-filled with 50
        Assert.True(metrics.PoolHits > 900);
        
        pool.Dispose();
    }

    [Fact]
    public void SequentialVsPooledComparison_ShowsAllocationImprovement()
    {
        const int iterations = 10000;
        
        // Sequential allocation
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memBefore = GC.GetTotalAllocatedBytes();
        var colBefore = GC.CollectionCount(0);
        
        for (int i = 0; i < iterations; i++)
        {
            var obj = new TestObject { Value = i };
        }
        
        GC.Collect();
        var memAfterSeq = GC.GetTotalAllocatedBytes();
        var colAfterSeq = GC.CollectionCount(0);
        
        var sequentialMem = memAfterSeq - memBefore;
        var sequentialCollections = colAfterSeq - colBefore;
        
        // Pooled allocation
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        memBefore = GC.GetTotalAllocatedBytes();
        colBefore = GC.CollectionCount(0);
        
        var pool = new ObjectPool<TestObject>(() => new TestObject(), poolSize: 500);
        
        for (int i = 0; i < iterations; i++)
        {
            var obj = pool.Rent();
            obj.Value = i;
            pool.Return(obj);
        }
        
        GC.Collect();
        var memAfterPool = GC.GetTotalAllocatedBytes();
        var colAfterPool = GC.CollectionCount(0);
        
        var pooledMem = memAfterPool - memBefore;
        var pooledCollections = colAfterPool - colBefore;
        
        var memImprovement = ((double)(sequentialMem - pooledMem) / sequentialMem) * 100;
        var gcImprovement = ((double)(sequentialCollections - pooledCollections) / Math.Max(sequentialCollections, 1)) * 100;
        
        // Pooling should reduce allocations significantly
        Assert.True(memImprovement > 0, $"Memory improvement: {memImprovement:F2}%");
        
        pool.Dispose();
    }

    private class TestObject
    {
        public int Value { get; set; }
    }
}
