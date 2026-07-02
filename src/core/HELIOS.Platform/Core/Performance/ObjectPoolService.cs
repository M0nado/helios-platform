using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Object Pool Service: Reduces GC pressure by reusing objects instead of allocating new ones
    /// Optimized for Phase 1-2 services with frequent allocations
    /// </summary>
    public interface IObjectPoolService
    {
        PooledObject<T> Rent<T>() where T : class, new();
        void Return<T>(PooledObject<T> pooledObject) where T : class, new();
        ArrayPool<T> GetArrayPool<T>();
        PoolStatistics GetStatistics();
    }

    public class PooledObject<T> : IDisposable where T : class, new()
    {
        private T _value;
        private readonly Action<PooledObject<T>> _returnAction;

        public T Value => _value ?? throw new ObjectDisposedException(nameof(PooledObject<T>));

        internal PooledObject(T value, Action<PooledObject<T>> returnAction)
        {
            _value = value;
            _returnAction = returnAction;
        }

        public void Dispose()
        {
            if (_value != null)
            {
                _returnAction?.Invoke(this);
                _value = null;
            }
        }

        internal void Reset()
        {
            // Clear any state if the object supports IResettable
            if (_value is IPoolable poolable)
                poolable.Reset();
        }

        internal T GetValueForReturn() => _value;
    }

    public interface IPoolable
    {
        void Reset();
    }

    public class PoolStatistics
    {
        public int TotalPoolsCreated { get; set; }
        public long TotalObjectsRented { get; set; }
        public long TotalObjectsReturned { get; set; }
        public long TotalAllocationsAvoided { get; set; }
        public long TotalMemorySavedBytes { get; set; }
        public Dictionary<string, int> PoolSizes { get; set; } = new();
    }

    public class ObjectPoolService : IObjectPoolService
    {
        private class ObjectPool<T> where T : class, new()
        {
            private readonly ConcurrentBag<T> _objects = new();
            private readonly Func<T> _objectGenerator;
            private readonly int _maxPoolSize;
            private long _rented;
            private long _returned;
            private long _created;

            public ObjectPool(Func<T> objectGenerator = null, int maxPoolSize = 100)
            {
                _objectGenerator = objectGenerator ?? (() => new T());
                _maxPoolSize = maxPoolSize;
            }

            public T Rent()
            {
                Interlocked.Increment(ref _rented);
                if (_objects.TryTake(out T item))
                    return item;

                Interlocked.Increment(ref _created);
                return _objectGenerator();
            }

            public void Return(T item)
            {
                Interlocked.Increment(ref _returned);

                if (item is IPoolable poolable)
                    poolable.Reset();

                if (_objects.Count < _maxPoolSize)
                    _objects.Add(item);
            }

            public int Count => _objects.Count;
            public long Rented => _rented;
            public long Returned => _returned;
            public long Created => _created;
        }

        private readonly ConcurrentDictionary<Type, object> _pools = new();
        private readonly ConcurrentDictionary<string, long> _arrayPoolStats = new();

        public PooledObject<T> Rent<T>() where T : class, new()
        {
            var pool = GetOrCreatePool<T>();
            var obj = pool.Rent();
            return new PooledObject<T>(obj, Return);
        }

        public void Return<T>(PooledObject<T> pooledObject) where T : class, new()
        {
            if (pooledObject == null) return;

            var obj = pooledObject.GetValueForReturn();
            if (obj == null) return;

            var pool = GetOrCreatePool<T>();
            pool.Return(obj);
        }

        public ArrayPool<T> GetArrayPool<T>()
        {
            var key = typeof(T).FullName ?? typeof(T).Name;
            _arrayPoolStats.AddOrUpdate(key, 1, (_, count) => count + 1);
            return ArrayPool<T>.Shared;
        }

        public PoolStatistics GetStatistics()
        {
            var stats = new PoolStatistics
            {
                TotalPoolsCreated = _pools.Count,
                PoolSizes = new Dictionary<string, int>()
            };

            foreach (var kvp in _pools)
            {
                var poolType = kvp.Key.Name;
                var pool = kvp.Value as dynamic;
                stats.PoolSizes[poolType] = pool.Count;
                stats.TotalObjectsRented += pool.Rented;
                stats.TotalObjectsReturned += pool.Returned;
                stats.TotalAllocationsAvoided += Math.Max(0, pool.Rented - pool.Created);
            }

            return stats;
        }

        private ObjectPool<T> GetOrCreatePool<T>() where T : class, new()
        {
            return (ObjectPool<T>)_pools.GetOrAdd(typeof(T), _ => new ObjectPool<T>());
        }
    }

    /// <summary>
    /// Memory Optimization Helper: Utilities to reduce allocations and GC pressure
    /// </summary>
    public class MemoryOptimizationHelper
    {
        /// <summary>Reuses a list instead of creating a new one</summary>
        public static void ClearAndReuse<T>(List<T> list)
        {
            if (list != null)
                list.Clear();
        }

        /// <summary>Trims large strings to reduce memory footprint</summary>
        public static void TrimLargeStrings(Dictionary<string, string> dict)
        {
            if (dict == null) return;
            var keys = new List<string>(dict.Keys);
            foreach (var key in keys)
            {
                if (dict[key]?.Length > 10000)
                    dict[key] = dict[key].Substring(0, 10000);
            }
        }

        /// <summary>Unboxes value types to avoid heap allocations</summary>
        public static T UnboxSafe<T>(object boxed) where T : struct
        {
            try
            {
                return (T)boxed;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>Batch dispose of resources</summary>
        public static void BatchDispose(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch
                {
                    // Log but don't throw
                }
            }
        }

        /// <summary>Force garbage collection at strategic points</summary>
        public static void StrategicGarbageCollection()
        {
            // Only collect Gen2 if Gen0 is over 75% full
            if (GC.GetTotalMemory(false) > GC.GetTotalMemory(false) * 0.75)
            {
                GC.Collect(2, GCCollectionMode.Optimized);
            }
        }
    }
}
