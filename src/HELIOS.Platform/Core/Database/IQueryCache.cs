using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Database
{
    /// <summary>
    /// Query cache interface for reducing repeated database queries.
    /// Provides TTL-based caching for frequently executed queries.
    /// Expected impact: -60% repeated query latency
    /// </summary>
    public interface IQueryCache
    {
        /// <summary>
        /// Gets or retrieves a cached query result asynchronously.
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="cacheKey">Unique cache key (query hash + parameters)</param>
        /// <param name="queryFunc">Function to execute if cache miss</param>
        /// <param name="ttl">Time-to-live for cache entry</param>
        Task<T> GetOrExecuteAsync<T>(
            string cacheKey,
            Func<Task<T>> queryFunc,
            TimeSpan ttl) where T : class;

        /// <summary>
        /// Gets or retrieves a cached query result synchronously.
        /// Use for non-async operations.
        /// </summary>
        T GetOrExecute<T>(
            string cacheKey,
            Func<T> queryFunc,
            TimeSpan ttl) where T : class;

        /// <summary>
        /// Invalidates a specific cache entry by key.
        /// </summary>
        void Invalidate(string cacheKey);

        /// <summary>
        /// Invalidates all cache entries matching a pattern.
        /// </summary>
        void InvalidatePattern(string pattern);

        /// <summary>
        /// Gets cache statistics including hit rate and memory usage.
        /// </summary>
        CacheStatistics GetStatistics();

        /// <summary>
        /// Clears all cache entries and resets statistics.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Query cache statistics for monitoring and diagnostics.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Total number of cache hits
        /// </summary>
        public long TotalHits { get; set; }

        /// <summary>
        /// Total number of cache misses
        /// </summary>
        public long TotalMisses { get; set; }

        /// <summary>
        /// Cache hit rate (0.0 to 1.0)
        /// </summary>
        public double HitRate =>
            TotalHits + TotalMisses > 0
                ? (double)TotalHits / (TotalHits + TotalMisses)
                : 0;

        /// <summary>
        /// Current number of entries in cache
        /// </summary>
        public int EntriesCount { get; set; }

        /// <summary>
        /// Estimated memory usage in bytes
        /// </summary>
        public long EstimatedSizeBytes { get; set; }

        /// <summary>
        /// Timestamp of last cache clear or initialization
        /// </summary>
        public DateTime LastClearedAt { get; set; }
    }
}
