using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Caching
{
    /// <summary>
    /// Unified cache service consolidating L2Cache, QueryCache, and ObjectPool functionality.
    /// Provides multiple caching strategies optimized for different workloads:
    /// - L2Cache: General-purpose caching with TTL and pattern invalidation
    /// - QueryCache: Database query result caching with LRU eviction
    /// - ObjectPool: Object reuse to reduce GC pressure
    /// </summary>
    public interface ICacheService
    {
        // L2 Cache operations
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        Task RemoveAsync(string key);
        Task InvalidatePatternAsync(string pattern);
        void ClearCache();

        // Query cache operations
        Task<T> GetOrExecuteQueryAsync<T>(string cacheKey, Func<Task<T>> queryFunc, TimeSpan ttl) where T : class;
        T GetOrExecuteQuery<T>(string cacheKey, Func<T> queryFunc, TimeSpan ttl) where T : class;
        void InvalidateQuery(string cacheKey);
        void InvalidateQueryPattern(string pattern);

        // Object pool operations
        PooledObject<T> RentObject<T>() where T : class, new();
        void ReturnObject<T>(PooledObject<T> pooledObject) where T : class, new();
        ArrayPool<T> GetArrayPool<T>();

        // Statistics
        CacheStatistics GetCacheStatistics();
        QueryCacheStatistics GetQueryCacheStatistics();
        PoolStatistics GetPoolStatistics();
    }

    /// <summary>
    /// L2 Cache statistics.
    /// </summary>
    public class CacheStatistics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long Evictions { get; set; }
        public long InvalidatedKeys { get; set; }
        public int CurrentItemCount { get; set; }
        public long MemoryUsageBytes { get; set; }
        public double HitRate => (Hits + Misses) > 0 ? (Hits * 100.0) / (Hits + Misses) : 0;
    }

    /// <summary>
    /// Query cache statistics.
    /// </summary>
    public class QueryCacheStatistics
    {
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public int EntriesCount { get; set; }
        public long EstimatedSizeBytes { get; set; }
        public double HitRate => (TotalHits + TotalMisses) > 0 ? (double)TotalHits / (TotalHits + TotalMisses) : 0;
        public DateTime LastClearedAt { get; set; }
    }

    /// <summary>
    /// Object pool statistics.
    /// </summary>
    public class PoolStatistics
    {
        public int TotalPoolsCreated { get; set; }
        public long TotalObjectsRented { get; set; }
        public long TotalObjectsReturned { get; set; }
        public long TotalAllocationsAvoided { get; set; }
        public long TotalMemorySavedBytes { get; set; }
        public Dictionary<string, int> PoolSizes { get; set; } = new();
    }

    /// <summary>
    /// Pooled object wrapper for automatic return to pool on disposal.
    /// </summary>
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
            if (_value is IPoolable poolable)
                poolable.Reset();
        }

        internal T GetValueForReturn() => _value;
    }

    /// <summary>
    /// Interface for objects that can be pooled and reset.
    /// </summary>
    public interface IPoolable
    {
        void Reset();
    }

    /// <summary>
    /// Unified cache service implementation consolidating L2Cache, QueryCache, and ObjectPool.
    /// </summary>
    public class CacheService : ICacheService
    {
        // L2 Cache implementation
        private class L2CacheEntry<T>
        {
            public T Value { get; set; }
            public DateTime ExpirationTime { get; set; }
            public long Size { get; set; }
        }

        // Query Cache implementation
        private class QueryCacheEntry<T> where T : class
        {
            public T Value { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public long LastAccessTicks { get; set; }
            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        }

        // Object Pool implementation
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

            public int CurrentSize => _objects.Count;
            public long TotalRented => _rented;
            public long TotalReturned => _returned;
            public long TotalCreated => _created;
        }

        // L2 Cache state
        private readonly ConcurrentDictionary<string, object> _l2cache = new();
        private readonly ConcurrentDictionary<string, Timer> _l2timers = new();
        private readonly ReaderWriterLockSlim _l2lock = new();
        private readonly TimeSpan _l2defaultTtl;
        private readonly long _l2maxMemoryBytes;
        private long _l2currentMemoryBytes;
        private long _l2hits;
        private long _l2misses;
        private long _l2evictions;
        private long _l2invalidations;

        // Query Cache state
        private readonly ConcurrentDictionary<string, object> _queryCache = new();
        private readonly SortedSet<(long Ticks, string Key)> _queryLruIndex = new();
        private readonly int _queryMaxEntries;
        private readonly object _queryLruLock = new();
        private long _queryHits = 0;
        private long _queryMisses = 0;

        // Object Pool state
        private readonly ConcurrentDictionary<string, object> _objectPools = new();

        public CacheService(TimeSpan? defaultTtl = null, long maxMemoryMB = 500, int queryMaxEntries = 1000)
        {
            _l2defaultTtl = defaultTtl ?? TimeSpan.FromHours(1);
            _l2maxMemoryBytes = maxMemoryMB * 1024 * 1024;
            _queryMaxEntries = queryMaxEntries;
        }

        // ========== L2 Cache Implementation ==========

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
        {
            if (TryGetCachedL2<T>(key, out T cached))
            {
                Interlocked.Increment(ref _l2hits);
                return cached;
            }

            Interlocked.Increment(ref _l2misses);
            var value = await factory();
            await SetAsync(key, value, ttl);
            return value;
        }

        public Task<T> GetAsync<T>(string key)
        {
            if (TryGetCachedL2<T>(key, out T value))
            {
                Interlocked.Increment(ref _l2hits);
                return Task.FromResult(value);
            }

            Interlocked.Increment(ref _l2misses);
            return Task.FromResult<T>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            if (value == null)
                return Task.CompletedTask;

            _l2lock.EnterWriteLock();
            try
            {
                var effectiveTtl = ttl ?? _l2defaultTtl;
                var size = EstimateSize(value);

                while (_l2currentMemoryBytes + size > _l2maxMemoryBytes && _l2cache.Count > 0)
                {
                    EvictL2Lru();
                }

                var entry = new L2CacheEntry<T>
                {
                    Value = value,
                    ExpirationTime = DateTime.UtcNow.Add(effectiveTtl),
                    Size = size
                };

                if (_l2cache.TryGetValue(key, out var existing) && existing is L2CacheEntry<T> existingEntry)
                {
                    _l2currentMemoryBytes -= existingEntry.Size;
                    if (_l2timers.TryRemove(key, out var oldTimer))
                        oldTimer?.Dispose();
                }

                _l2cache[key] = entry;
                _l2currentMemoryBytes += size;

                if (_l2timers.TryGetValue(key, out var existingTimer))
                    existingTimer.Dispose();

                var timer = new Timer(_ => RemoveAsync(key), null, effectiveTtl, Timeout.InfiniteTimeSpan);
                _l2timers[key] = timer;
            }
            finally
            {
                _l2lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _l2lock.EnterWriteLock();
            try
            {
                if (_l2cache.TryRemove(key, out var obj) && obj is not null)
                {
                    var type = obj.GetType();
                    var sizeProperty = type.GetProperty("Size");
                    if (sizeProperty?.GetValue(obj) is long size)
                    {
                        _l2currentMemoryBytes -= size;
                    }
                }

                if (_l2timers.TryRemove(key, out var timer))
                    timer?.Dispose();
            }
            finally
            {
                _l2lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public Task InvalidatePatternAsync(string pattern)
        {
            _l2lock.EnterWriteLock();
            try
            {
                var keysToRemove = new List<string>();
                var regexPattern = WildcardToRegex(pattern);
                var regex = new System.Text.RegularExpressions.Regex(regexPattern);

                foreach (var key in _l2cache.Keys)
                {
                    if (regex.IsMatch(key))
                        keysToRemove.Add(key);
                }

                foreach (var key in keysToRemove)
                {
                    _l2cache.TryRemove(key, out _);
                    if (_l2timers.TryRemove(key, out var timer))
                        timer?.Dispose();

                    Interlocked.Increment(ref _l2invalidations);
                }
            }
            finally
            {
                _l2lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public void ClearCache()
        {
            _l2lock.EnterWriteLock();
            try
            {
                foreach (var timer in _l2timers.Values)
                    timer?.Dispose();

                _l2timers.Clear();
                _l2cache.Clear();
                _l2currentMemoryBytes = 0;
            }
            finally
            {
                _l2lock.ExitWriteLock();
            }

            lock (_queryLruLock)
            {
                _queryCache.Clear();
                _queryLruIndex.Clear();
            }
        }

        // ========== Query Cache Implementation ==========

        public async Task<T> GetOrExecuteQueryAsync<T>(string cacheKey, Func<Task<T>> queryFunc, TimeSpan ttl) where T : class
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));

            if (_queryCache.TryGetValue(cacheKey, out var cachedEntry))
            {
                var entry = (QueryCacheEntry<T>)cachedEntry;
                if (!entry.IsExpired)
                {
                    lock (_queryLruLock)
                    {
                        _queryLruIndex.Remove((entry.LastAccessTicks, cacheKey));
                        entry.LastAccessTicks = DateTime.UtcNow.Ticks;
                        _queryLruIndex.Add((entry.LastAccessTicks, cacheKey));
                    }

                    Interlocked.Increment(ref _queryHits);
                    return entry.Value;
                }

                _queryCache.TryRemove(cacheKey, out _);
            }

            Interlocked.Increment(ref _queryMisses);
            var result = await queryFunc();

            var now = DateTime.UtcNow;
            var newEntry = new QueryCacheEntry<T>
            {
                Value = result,
                CreatedAt = now,
                ExpiresAt = now.Add(ttl),
                LastAccessTicks = now.Ticks
            };

            lock (_queryLruLock)
            {
                if (_queryCache.Count >= _queryMaxEntries)
                {
                    var oldestKey = _queryLruIndex.Min.Key;
                    _queryCache.TryRemove(oldestKey, out _);
                    _queryLruIndex.Remove(_queryLruIndex.Min);
                }

                _queryCache[cacheKey] = newEntry;
                _queryLruIndex.Add((newEntry.LastAccessTicks, cacheKey));
            }

            return result;
        }

        public T GetOrExecuteQuery<T>(string cacheKey, Func<T> queryFunc, TimeSpan ttl) where T : class
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));

            if (_queryCache.TryGetValue(cacheKey, out var cachedEntry))
            {
                var entry = (QueryCacheEntry<T>)cachedEntry;
                if (!entry.IsExpired)
                {
                    lock (_queryLruLock)
                    {
                        _queryLruIndex.Remove((entry.LastAccessTicks, cacheKey));
                        entry.LastAccessTicks = DateTime.UtcNow.Ticks;
                        _queryLruIndex.Add((entry.LastAccessTicks, cacheKey));
                    }

                    Interlocked.Increment(ref _queryHits);
                    return entry.Value;
                }

                _queryCache.TryRemove(cacheKey, out _);
            }

            Interlocked.Increment(ref _queryMisses);
            var result = queryFunc();

            var now = DateTime.UtcNow;
            var newEntry = new QueryCacheEntry<T>
            {
                Value = result,
                CreatedAt = now,
                ExpiresAt = now.Add(ttl),
                LastAccessTicks = now.Ticks
            };

            lock (_queryLruLock)
            {
                if (_queryCache.Count >= _queryMaxEntries)
                {
                    var oldestKey = _queryLruIndex.Min.Key;
                    _queryCache.TryRemove(oldestKey, out _);
                    _queryLruIndex.Remove(_queryLruIndex.Min);
                }

                _queryCache[cacheKey] = newEntry;
                _queryLruIndex.Add((newEntry.LastAccessTicks, cacheKey));
            }

            return result;
        }

        public void InvalidateQuery(string cacheKey)
        {
            _queryCache.TryRemove(cacheKey, out _);
        }

        public void InvalidateQueryPattern(string pattern)
        {
            var keysToRemove = new List<string>();
            foreach (var key in _queryCache.Keys)
            {
                if (key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
            {
                _queryCache.TryRemove(key, out _);
            }
        }

        // ========== Object Pool Implementation ==========

        public PooledObject<T> RentObject<T>() where T : class, new()
        {
            var poolName = typeof(T).FullName;
            var pool = (ObjectPool<T>)_objectPools.GetOrAdd(poolName, _ => new ObjectPool<T>());

            var obj = pool.Rent();
            return new PooledObject<T>(obj, po => ReturnObjectInternal(po, poolName, pool));
        }

        public void ReturnObject<T>(PooledObject<T> pooledObject) where T : class, new()
        {
            var poolName = typeof(T).FullName;
            if (_objectPools.TryGetValue(poolName, out var poolObj) && poolObj is ObjectPool<T> pool)
            {
                pooledObject.Reset();
                pool.Return(pooledObject.GetValueForReturn());
            }
        }

        private void ReturnObjectInternal<T>(PooledObject<T> pooledObject, string poolName, ObjectPool<T> pool) where T : class, new()
        {
            pooledObject.Reset();
            pool.Return(pooledObject.GetValueForReturn());
        }

        public ArrayPool<T> GetArrayPool<T>()
        {
            return ArrayPool<T>.Shared;
        }

        // ========== Statistics ==========

        public CacheStatistics GetCacheStatistics()
        {
            return new CacheStatistics
            {
                Hits = _l2hits,
                Misses = _l2misses,
                Evictions = _l2evictions,
                InvalidatedKeys = _l2invalidations,
                CurrentItemCount = _l2cache.Count,
                MemoryUsageBytes = _l2currentMemoryBytes
            };
        }

        public QueryCacheStatistics GetQueryCacheStatistics()
        {
            return new QueryCacheStatistics
            {
                TotalHits = _queryHits,
                TotalMisses = _queryMisses,
                EntriesCount = _queryCache.Count,
                LastClearedAt = DateTime.UtcNow
            };
        }

        public PoolStatistics GetPoolStatistics()
        {
            var stats = new PoolStatistics
            {
                TotalPoolsCreated = _objectPools.Count
            };

            foreach (var kvp in _objectPools)
            {
                var poolType = kvp.Value.GetType();
                var totalRentedProp = poolType.GetProperty("TotalRented");
                var totalReturnedProp = poolType.GetProperty("TotalReturned");
                var currentSizeProp = poolType.GetProperty("CurrentSize");

                if (totalRentedProp?.GetValue(kvp.Value) is long rented)
                    stats.TotalObjectsRented += rented;
                if (totalReturnedProp?.GetValue(kvp.Value) is long returned)
                    stats.TotalObjectsReturned += returned;
                if (currentSizeProp?.GetValue(kvp.Value) is int currentSize)
                    stats.PoolSizes[kvp.Key] = currentSize;
            }

            stats.TotalAllocationsAvoided = stats.TotalObjectsReturned;
            return stats;
        }

        // ========== Helpers ==========

        private bool TryGetCachedL2<T>(string key, out T value)
        {
            _l2lock.EnterReadLock();
            try
            {
                if (_l2cache.TryGetValue(key, out var obj))
                {
                    if (obj is L2CacheEntry<T> entry)
                    {
                        if (entry.ExpirationTime > DateTime.UtcNow)
                        {
                            value = entry.Value;
                            return true;
                        }
                    }
                }

                value = default;
                return false;
            }
            finally
            {
                _l2lock.ExitReadLock();
            }
        }

        private void EvictL2Lru()
        {
            var oldestKey = string.Empty;
            DateTime oldestTime = DateTime.MaxValue;

            foreach (var kvp in _l2cache)
            {
                if (kvp.Value is not null)
                {
                    var type = kvp.Value.GetType();
                    var expProperty = type.GetProperty("ExpirationTime");
                    if (expProperty?.GetValue(kvp.Value) is DateTime expTime)
                    {
                        if (expTime < oldestTime)
                        {
                            oldestTime = expTime;
                            oldestKey = kvp.Key;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(oldestKey))
            {
                _l2cache.TryRemove(oldestKey, out var removed);
                if (_l2timers.TryRemove(oldestKey, out var timer))
                    timer?.Dispose();

                Interlocked.Increment(ref _l2evictions);
            }
        }

        private long EstimateSize(object obj)
        {
            if (obj == null) return 0;
            return 100 + System.Runtime.InteropServices.Marshal.SizeOf(obj);
        }

        private string WildcardToRegex(string pattern)
        {
            var regex = System.Text.RegularExpressions.Regex.Escape(pattern);
            regex = regex.Replace("\\*", ".*").Replace("\\?", ".");
            return $"^{regex}$";
        }
    }
}
