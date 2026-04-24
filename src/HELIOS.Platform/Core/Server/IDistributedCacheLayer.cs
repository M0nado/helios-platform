using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Distributed cache layer interface with Redis-like operations, TTL support, and LRU eviction.
    /// </summary>
    public interface IDistributedCacheLayer
    {
        /// <summary>
        /// Sets a value with optional TTL in seconds. If TTL is null, the value persists.
        /// </summary>
        Task<bool> SetAsync(string key, string value, int? ttlSeconds = null);

        /// <summary>
        /// Gets a value by key. Returns null if key doesn't exist or has expired.
        /// </summary>
        Task<string?> GetAsync(string key);

        /// <summary>
        /// Gets multiple values by keys.
        /// </summary>
        Task<Dictionary<string, string?>> MGetAsync(params string[] keys);

        /// <summary>
        /// Deletes a key.
        /// </summary>
        Task<bool> DeleteAsync(string key);

        /// <summary>
        /// Deletes multiple keys.
        /// </summary>
        Task<int> DeleteAsync(params string[] keys);

        /// <summary>
        /// Checks if a key exists.
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Gets the TTL in seconds for a key. Returns -1 if no TTL, -2 if doesn't exist.
        /// </summary>
        Task<long> GetTtlAsync(string key);

        /// <summary>
        /// Sets an expiration (TTL) on an existing key.
        /// </summary>
        Task<bool> ExpireAsync(string key, int ttlSeconds);

        /// <summary>
        /// Increments a numeric value. Creates key with value 1 if it doesn't exist.
        /// </summary>
        Task<long> IncrementAsync(string key);

        /// <summary>
        /// Decrements a numeric value. Creates key with value -1 if it doesn't exist.
        /// </summary>
        Task<long> DecrementAsync(string key);

        /// <summary>
        /// Flushes all keys from the cache.
        /// </summary>
        Task FlushAllAsync();

        /// <summary>
        /// Gets cache statistics (hit rate, eviction count, etc.).
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// Gets the current cache size (number of keys).
        /// </summary>
        Task<int> GetSizeAsync();

        /// <summary>
        /// Gets the maximum cache capacity (in terms of number of keys).
        /// </summary>
        int MaxCapacity { get; }
    }

    /// <summary>
    /// Cache statistics.
    /// </summary>
    public class CacheStatistics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long Evictions { get; set; }
        public int CurrentSize { get; set; }
        public int MaxCapacity { get; set; }
        public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) : 0;
    }
}
