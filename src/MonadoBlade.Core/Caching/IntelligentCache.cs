using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MonadoBlade.Core.Caching
{
    /// <summary>
    /// A thread-safe in-memory cache implementation with intelligent dependency-based invalidation.
    /// Tracks cache hit rates and uses partial invalidation to improve performance.
    /// </summary>
    public class IntelligentCache
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly DependencyTracker _dependencyTracker = new DependencyTracker();
        private long _hits = 0;
        private long _misses = 0;
        private long _invalidations = 0;
        private long _invalidatedKeys = 0;

        /// <summary>
        /// Gets the number of cache hits.
        /// </summary>
        public long Hits
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _hits;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the number of cache misses.
        /// </summary>
        public long Misses
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _misses;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the cache hit rate as a percentage (0-100).
        /// </summary>
        public double HitRate
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    if (_hits + _misses == 0)
                        return 0;

                    return (_hits * 100.0) / (_hits + _misses);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the number of invalidation operations.
        /// </summary>
        public long InvalidationCount => _invalidations;

        /// <summary>
        /// Gets the total number of keys invalidated.
        /// </summary>
        public long InvalidatedKeysCount => _invalidatedKeys;

        /// <summary>
        /// Gets the current cache size.
        /// </summary>
        public int CacheSize
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _cache.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the dependency tracker used by this cache.
        /// </summary>
        public DependencyTracker DependencyTracker => _dependencyTracker;

        /// <summary>
        /// Gets a value from the cache if it exists and is valid.
        /// </summary>
        /// <typeparam name="T">The type of value to retrieve.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cached value, if found.</param>
        /// <returns>True if the value was found and is valid; otherwise, false.</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var cachedEntry))
                {
                    if (cachedEntry is CacheEntry<T> entry && entry.IsValid)
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            entry.RecordAccess();
                            _hits++;
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }

                        value = entry.Value;
                        return true;
                    }
                    else
                    {
                        // Entry is expired, remove it
                        _lock.EnterWriteLock();
                        try
                        {
                            _cache.Remove(key);
                            _dependencyTracker.RemoveKey(key);
                            _misses++;
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                }

                _lock.EnterWriteLock();
                try
                {
                    _misses++;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                value = default;
                return false;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Sets a value in the cache with the specified expiration and dependencies.
        /// </summary>
        /// <typeparam name="T">The type of value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiresAt">When the cache entry should expire.</param>
        /// <param name="dependencies">Optional cache keys this entry depends on.</param>
        public void Set<T>(string key, T value, DateTime expiresAt, IEnumerable<string> dependencies = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterWriteLock();
            try
            {
                var entry = new CacheEntry<T>(key, value, expiresAt, dependencies);
                _cache[key] = entry;

                // Register dependencies
                if (dependencies != null)
                {
                    foreach (var dep in dependencies)
                    {
                        try
                        {
                            _dependencyTracker.RegisterDependency(key, dep);
                        }
                        catch (InvalidOperationException)
                        {
                            // Circular dependency; skip this dependency
                        }
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Sets a value in the cache with a duration-based expiration.
        /// </summary>
        /// <typeparam name="T">The type of value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="duration">How long the entry should be valid.</param>
        /// <param name="dependencies">Optional cache keys this entry depends on.</param>
        public void Set<T>(string key, T value, TimeSpan duration, IEnumerable<string> dependencies = null)
        {
            Set(key, value, DateTime.UtcNow.Add(duration), dependencies);
        }

        /// <summary>
        /// Invalidates a cache key using intelligent dependency tracking.
        /// May perform either full or partial invalidation based on impact.
        /// </summary>
        /// <param name="key">The cache key to invalidate.</param>
        /// <returns>The number of keys that were invalidated.</returns>
        public int InvalidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterWriteLock();
            try
            {
                var result = _dependencyTracker.InvalidateKey(key, _cache.Count);
                _invalidations++;

                if (result.ShouldClearAll)
                {
                    // Full cache clear
                    _invalidatedKeys += _cache.Count;
                    _cache.Clear();
                }
                else
                {
                    // Partial invalidation
                    foreach (var keyToInvalidate in result.KeysToInvalidate)
                    {
                        if (_cache.Remove(keyToInvalidate))
                            _invalidatedKeys++;
                    }
                }

                return result.KeysToInvalidate.Count;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clears the entire cache.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Clear();
                _dependencyTracker.Clear();
                _hits = 0;
                _misses = 0;
                _invalidations = 0;
                _invalidatedKeys = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets a cache entry without recording a hit/miss.
        /// </summary>
        public CacheEntry<T> GetEntry<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _lock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var cachedEntry) && cachedEntry is CacheEntry<T> entry)
                    return entry;

                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the metrics for this cache.
        /// </summary>
        public CacheMetrics GetMetrics()
        {
            _lock.EnterReadLock();
            try
            {
                // Calculate hit rate without recursion
                double hitRate = 0;
                if (_hits + _misses > 0)
                    hitRate = (_hits * 100.0) / (_hits + _misses);

                return new CacheMetrics
                {
                    Hits = _hits,
                    Misses = _misses,
                    HitRate = hitRate,
                    CacheSize = _cache.Count,
                    InvalidationOperations = _invalidations,
                    TotalKeysInvalidated = _invalidatedKeys,
                    PartialInvalidations = _dependencyTracker.PartialInvalidationCount,
                    FullInvalidations = _dependencyTracker.FullInvalidationCount,
                    TrackedDependencies = _dependencyTracker.TrackingCount
                };
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Disposes the cache and releases managed resources.
        /// </summary>
        public void Dispose()
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Clear();
                _dependencyTracker.Dispose();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Represents cache metrics and statistics.
    /// </summary>
    public class CacheMetrics
    {
        /// <summary>
        /// Gets the number of cache hits.
        /// </summary>
        public long Hits { get; set; }

        /// <summary>
        /// Gets the number of cache misses.
        /// </summary>
        public long Misses { get; set; }

        /// <summary>
        /// Gets the cache hit rate as a percentage.
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// Gets the current cache size.
        /// </summary>
        public int CacheSize { get; set; }

        /// <summary>
        /// Gets the number of invalidation operations.
        /// </summary>
        public long InvalidationOperations { get; set; }

        /// <summary>
        /// Gets the total number of keys invalidated.
        /// </summary>
        public long TotalKeysInvalidated { get; set; }

        /// <summary>
        /// Gets the number of partial invalidations.
        /// </summary>
        public long PartialInvalidations { get; set; }

        /// <summary>
        /// Gets the number of full invalidations.
        /// </summary>
        public long FullInvalidations { get; set; }

        /// <summary>
        /// Gets the number of tracked dependencies.
        /// </summary>
        public long TrackedDependencies { get; set; }

        /// <summary>
        /// Gets a string representation of the metrics.
        /// </summary>
        public override string ToString()
        {
            return $"CacheMetrics [" +
                   $"HitRate: {HitRate:F2}%, " +
                   $"Hits: {Hits}, " +
                   $"Misses: {Misses}, " +
                   $"Size: {CacheSize}, " +
                   $"Invalidations: {InvalidationOperations} " +
                   $"(Partial: {PartialInvalidations}, Full: {FullInvalidations}), " +
                   $"KeysInvalidated: {TotalKeysInvalidated}, " +
                   $"Dependencies: {TrackedDependencies}" +
                   $"]";
        }
    }
}
