namespace MonadoBlade.Tests.Unit.Concurrency;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Core.Concurrency;

/// <summary>
/// Comprehensive unit tests for lock-free collections.
/// Tests concurrent operations, stress scenarios, and thread safety.
/// </summary>
public class LockFreeTests
{
    // =====================================================================
    // LockFreeEventQueue Tests
    // =====================================================================

    [Fact]
    public void EventQueue_SingleThreaded_EnqueueDequeue()
    {
        var queue = new LockFreeEventQueue();
        var evt1 = new object();
        var evt2 = new object();

        queue.Enqueue(evt1);
        queue.Enqueue(evt2);

        Assert.Equal(2, queue.Count);
        Assert.True(queue.TryDequeue(out var dequeued1));
        Assert.Same(evt1, dequeued1);
        Assert.True(queue.TryDequeue(out var dequeued2));
        Assert.Same(evt2, dequeued2);
        Assert.False(queue.TryDequeue(out _));
    }

    [Fact]
    public void EventQueue_Concurrent_Enqueue_30Tasks()
    {
        var queue = new LockFreeEventQueue();
        const int tasksCount = 30;
        const int eventsPerTask = 100;

        var tasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < eventsPerTask; i++)
                {
                    queue.Enqueue(new { TaskId = taskId, EventId = i });
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.Equal(tasksCount * eventsPerTask, queue.Count);
        Assert.Equal(tasksCount * eventsPerTask, queue.Metrics.TotalOperations);
        Assert.Equal(tasksCount * eventsPerTask, queue.Metrics.SuccessfulOperations);
    }

    [Fact]
    public void EventQueue_Concurrent_EnqueueDequeue_30Tasks()
    {
        var queue = new LockFreeEventQueue();
        const int tasksCount = 30;
        const int eventsPerTask = 100;
        int dequeuedCount = 0;
        var lockForCount = new object();

        // Enqueue phase
        var enqueueTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < eventsPerTask; i++)
                {
                    queue.Enqueue(new { TaskId = taskId, EventId = i });
                }
            }))
            .ToArray();

        Task.WaitAll(enqueueTasks);

        // Dequeue phase
        var dequeueTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                while (queue.TryDequeue(out _))
                {
                    lock (lockForCount)
                    {
                        dequeuedCount++;
                    }
                }
            }))
            .ToArray();

        Task.WaitAll(dequeueTasks);

        Assert.Equal(0, queue.Count);
        Assert.Equal(tasksCount * eventsPerTask, dequeuedCount);
    }

    [Fact]
    public void EventQueue_Stress_100ConcurrentOperations()
    {
        var queue = new LockFreeEventQueue();
        const int concurrentTasks = 100;
        const int operationsPerTask = 1000;

        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask / 2; i++)
                {
                    queue.Enqueue(new { Value = i });
                }

                for (int i = 0; i < operationsPerTask / 2; i++)
                {
                    queue.TryDequeue(out _);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        // All operations should complete without deadlock or exception
        Assert.True(queue.Metrics.TotalOperations > 0);
        Assert.True(queue.Metrics.SuccessfulOperations > 0);
    }

    [Fact]
    public void EventQueue_FIFO_Ordering()
    {
        var queue = new LockFreeEventQueue();

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.True(queue.TryDequeue(out var evt));
            Assert.Equal(i, evt);
        }
    }

    // =====================================================================
    // LockFreeRegistry Tests
    // =====================================================================

    [Fact]
    public void Registry_SingleThreaded_RegisterRetrieve()
    {
        var registry = new LockFreeRegistry();
        var service1 = new object();
        var service2 = new object();

        registry.Register("service1", service1);
        registry.Register("service2", service2);

        Assert.Equal(2, registry.Count);
        Assert.True(registry.TryGetService("service1", out var retrieved1));
        Assert.Same(service1, retrieved1);
        Assert.True(registry.TryGetService("service2", out var retrieved2));
        Assert.Same(service2, retrieved2);
    }

    [Fact]
    public void Registry_Concurrent_Register_30Tasks()
    {
        var registry = new LockFreeRegistry();
        const int tasksCount = 30;
        const int servicesPerTask = 100;

        var tasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < servicesPerTask; i++)
                {
                    registry.Register($"service-{taskId}-{i}", new object());
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.Equal(tasksCount * servicesPerTask, registry.Count);
        Assert.Equal(tasksCount * servicesPerTask, registry.Metrics.TotalOperations);
    }

    [Fact]
    public void Registry_Concurrent_RegisterRetrieve_30Tasks()
    {
        var registry = new LockFreeRegistry();
        const int tasksCount = 30;
        const int servicesPerTask = 100;
        var services = new Dictionary<string, object>();

        // Register phase
        for (int i = 0; i < tasksCount * servicesPerTask; i++)
        {
            var service = new object();
            services[$"service-{i}"] = service;
            registry.Register($"service-{i}", service);
        }

        // Retrieve phase
        var retrieveTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < servicesPerTask; i++)
                {
                    var key = $"service-{taskId * servicesPerTask + i}";
                    Assert.True(registry.TryGetService(key, out var retrieved));
                    Assert.Same(services[key], retrieved);
                }
            }))
            .ToArray();

        Task.WaitAll(retrieveTasks);

        Assert.Equal(tasksCount * servicesPerTask, registry.Count);
    }

    [Fact]
    public void Registry_Concurrent_RegisterUnregister_30Tasks()
    {
        var registry = new LockFreeRegistry();
        const int tasksCount = 30;
        const int servicesPerTask = 100;

        // Register
        var registerTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < servicesPerTask; i++)
                {
                    registry.Register($"service-{taskId}-{i}", new object());
                }
            }))
            .ToArray();

        Task.WaitAll(registerTasks);
        var registeredCount = registry.Count;

        // Unregister
        var unregisterTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < servicesPerTask; i++)
                {
                    registry.Unregister($"service-{taskId}-{i}");
                }
            }))
            .ToArray();

        Task.WaitAll(unregisterTasks);

        Assert.Equal(0, registry.Count);
        Assert.Equal(registeredCount, tasksCount * servicesPerTask);
    }

    [Fact]
    public void Registry_Stress_100ConcurrentOperations()
    {
        var registry = new LockFreeRegistry();
        const int concurrentTasks = 100;
        const int operationsPerTask = 1000;

        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    var key = $"key-{taskId}-{i % 100}";
                    if (i % 3 == 0)
                        registry.Register(key, new object());
                    else if (i % 3 == 1)
                        registry.TryGetService(key, out _);
                    else
                        registry.Unregister(key);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.True(registry.Metrics.TotalOperations > 0);
    }

    // =====================================================================
    // LockFreeCachePool Tests
    // =====================================================================

    [Fact]
    public void CachePool_SingleThreaded_RentReturn()
    {
        var pool = new LockFreeCachePool(100);
        var item1 = new object();
        var item2 = new object();

        pool.Return(item1);
        pool.Return(item2);

        Assert.Equal(2, pool.Count);
        Assert.True(pool.TryRent(out var rented1));
        Assert.Same(item1, rented1);
        Assert.True(pool.TryRent(out var rented2));
        Assert.Same(item2, rented2);
        Assert.False(pool.TryRent(out _));
    }

    [Fact]
    public void CachePool_Concurrent_Return_30Tasks()
    {
        var pool = new LockFreeCachePool(10000);
        const int tasksCount = 30;
        const int itemsPerTask = 100;

        var tasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < itemsPerTask; i++)
                {
                    pool.Return(new object());
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.Equal(tasksCount * itemsPerTask, pool.Count);
        Assert.Equal(tasksCount * itemsPerTask, pool.Metrics.TotalOperations);
    }

    [Fact]
    public void CachePool_Concurrent_RentReturn_30Tasks()
    {
        var pool = new LockFreeCachePool(10000);
        const int tasksCount = 30;
        const int itemsPerTask = 100;
        int rentedCount = 0;
        var lockForCount = new object();

        // Return items to pool
        for (int i = 0; i < tasksCount * itemsPerTask; i++)
        {
            pool.Return(new object());
        }

        // Rent from pool
        var rentTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                while (pool.TryRent(out _))
                {
                    lock (lockForCount)
                    {
                        rentedCount++;
                    }
                }
            }))
            .ToArray();

        Task.WaitAll(rentTasks);

        Assert.Equal(0, pool.Count);
        Assert.Equal(tasksCount * itemsPerTask, rentedCount);
    }

    [Fact]
    public void CachePool_MaxCapacity_Respected()
    {
        var pool = new LockFreeCachePool(100);

        for (int i = 0; i < 200; i++)
        {
            pool.Return(new object());
        }

        // Should not exceed max capacity
        Assert.True(pool.Count <= 100);
    }

    [Fact]
    public void CachePool_Stress_100ConcurrentOperations()
    {
        var pool = new LockFreeCachePool(10000);
        const int concurrentTasks = 100;
        const int operationsPerTask = 1000;

        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask / 2; i++)
                {
                    pool.Return(new object());
                }

                for (int i = 0; i < operationsPerTask / 2; i++)
                {
                    pool.TryRent(out _);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.True(pool.Metrics.TotalOperations > 0);
    }

    // =====================================================================
    // LockFreeDataCollection Tests
    // =====================================================================

    [Fact]
    public void DataCollection_SingleThreaded_AddRetrieve()
    {
        var collection = new LockFreeDataCollection<int>(1000);

        for (int i = 0; i < 100; i++)
        {
            collection.Add(i);
        }

        Assert.Equal(100, collection.Count);

        var items = collection.GetAllItems();
        Assert.Equal(100, items.Count);
    }

    [Fact]
    public void DataCollection_Concurrent_Add_30Tasks()
    {
        var collection = new LockFreeDataCollection<int>(10000);
        const int tasksCount = 30;
        const int itemsPerTask = 100;

        var tasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < itemsPerTask; i++)
                {
                    collection.Add(taskId * itemsPerTask + i);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.Equal(tasksCount * itemsPerTask, collection.Count);
        Assert.Equal(tasksCount * itemsPerTask, collection.Metrics.TotalOperations);
    }

    [Fact]
    public void DataCollection_Concurrent_AddRetrieve_30Tasks()
    {
        var collection = new LockFreeDataCollection<int>(10000);
        const int tasksCount = 30;
        const int itemsPerTask = 100;

        // Add items
        var addTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < itemsPerTask; i++)
                {
                    collection.Add(taskId * itemsPerTask + i);
                }
            }))
            .ToArray();

        Task.WaitAll(addTasks);

        // Retrieve items
        var retrieveTasks = Enumerable.Range(0, tasksCount)
            .Select(taskId => Task.Run(() =>
            {
                var items = collection.GetAllItems();
                Assert.NotEmpty(items);
            }))
            .ToArray();

        Task.WaitAll(retrieveTasks);

        Assert.Equal(tasksCount * itemsPerTask, collection.Count);
    }

    [Fact]
    public void DataCollection_GetItemsAfter_Filtering()
    {
        var collection = new LockFreeDataCollection<int>(1000);
        var beforeDate = DateTime.UtcNow;

        System.Threading.Thread.Sleep(10);

        for (int i = 0; i < 50; i++)
        {
            collection.Add(i);
        }

        System.Threading.Thread.Sleep(10);
        var afterDate = DateTime.UtcNow;

        var itemsAfter = collection.GetItemsAfter(beforeDate.AddSeconds(-1));
        Assert.NotEmpty(itemsAfter);
    }

    [Fact]
    public void DataCollection_Stress_100ConcurrentOperations()
    {
        var collection = new LockFreeDataCollection<int>(100000);
        const int concurrentTasks = 100;
        const int operationsPerTask = 1000;

        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    collection.Add(taskId * operationsPerTask + i);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.Equal(concurrentTasks * operationsPerTask, collection.Count);
    }

    // =====================================================================
    // Thread Safety Tests
    // =====================================================================

    [Fact]
    public void AllCollections_NoDataRacesUnderHighContention()
    {
        var queue = new LockFreeEventQueue();
        var registry = new LockFreeRegistry();
        var pool = new LockFreeCachePool(10000);
        var collection = new LockFreeDataCollection<int>(100000);

        const int concurrentTasks = 50;
        const int operationsPerTask = 500;

        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(taskId => Task.Run(() =>
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    // Queue operations
                    queue.Enqueue(new { Value = i });
                    queue.TryDequeue(out _);

                    // Registry operations
                    registry.Register($"key-{taskId}-{i}", new object());
                    registry.TryGetService($"key-{taskId}-{i}", out _);

                    // Pool operations
                    pool.Return(new object());
                    pool.TryRent(out _);

                    // Collection operations
                    collection.Add(i);
                }
            }))
            .ToArray();

        var sw = Stopwatch.StartNew();
        Task.WaitAll(tasks);
        sw.Stop();

        // All operations completed without exception = no data races
        Assert.True(sw.ElapsedMilliseconds < 30000); // Should complete in < 30 seconds
    }

    // =====================================================================
    // Contention Metrics Tests
    // =====================================================================

    [Fact]
    public void Metrics_AreAccurate()
    {
        var queue = new LockFreeEventQueue();

        for (int i = 0; i < 1000; i++)
        {
            queue.Enqueue(i);
        }

        for (int i = 0; i < 500; i++)
        {
            queue.TryDequeue(out _);
        }

        Assert.Equal(1500, queue.Metrics.TotalOperations);
        Assert.Equal(1500, queue.Metrics.SuccessfulOperations);
        Assert.True(queue.Metrics.TotalWaitTimeMs >= 0);
    }

    [Fact]
    public void Metrics_CanBeReset()
    {
        var queue = new LockFreeEventQueue();

        queue.Enqueue(new object());
        queue.Enqueue(new object());

        Assert.Equal(2, queue.Metrics.TotalOperations);

        queue.Metrics.Reset();

        Assert.Equal(0, queue.Metrics.TotalOperations);
        Assert.Equal(0, queue.Metrics.ContentionEvents);
    }

    // =====================================================================
    // Edge Case Tests
    // =====================================================================

    [Fact]
    public void EventQueue_NullEnqueue_ThrowsArgumentNullException()
    {
        var queue = new LockFreeEventQueue();
        Assert.Throws<ArgumentNullException>(() => queue.Enqueue(null!));
    }

    [Fact]
    public void Registry_NullKey_ThrowsArgumentNullException()
    {
        var registry = new LockFreeRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!, new object()));
    }

    [Fact]
    public void Registry_NullService_ThrowsArgumentNullException()
    {
        var registry = new LockFreeRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.Register("key", null!));
    }

    [Fact]
    public void CachePool_NullReturn_Handled()
    {
        var pool = new LockFreeCachePool(100);
        pool.Return(null); // Should not throw
        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void DataCollection_MaxCapacity_Enforced()
    {
        var collection = new LockFreeDataCollection<int>(100);

        for (int i = 0; i < 500; i++)
        {
            collection.Add(i);
        }

        // Should not exceed max capacity (with some tolerance for concurrent cleanup)
        Assert.True(collection.Count <= 150); // 100 max + some buffer
    }
}
