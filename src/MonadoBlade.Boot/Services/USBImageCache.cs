namespace MonadoBlade.Boot.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

/// <summary>
/// LRU (Least Recently Used) cache implementation for USB images.
/// Maintains a maximum size limit of 50GB with automatic eviction.
/// </summary>
public class USBImageCache : IUSBImageCache
{
    private readonly ILogger<USBImageCache> _logger;
    private readonly Dictionary<string, CacheEntry> _cache;
    private readonly LinkedList<string> _lruList;
    private readonly object _lockObject = new();

    private const long MaxCacheSizeBytes = 50L * 1024L * 1024L * 1024L; // 50GB
    private long _currentSizeBytes;

    /// <summary>
    /// Represents a cache entry with metadata.
    /// </summary>
    private class CacheEntry
    {
        /// <summary>
        /// Gets or sets the cached image data.
        /// </summary>
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets when the entry was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the entry was last accessed.
        /// </summary>
        public DateTime LastAccessedAt { get; set; }

        /// <summary>
        /// Gets or sets the access count.
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Gets the size in bytes.
        /// </summary>
        public long SizeBytes => ImageData.Length;
    }

    /// <summary>
    /// Initializes a new instance of the USBImageCache class.
    /// </summary>
    public USBImageCache(ILogger<USBImageCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = new Dictionary<string, CacheEntry>();
        _lruList = new LinkedList<string>();
        _currentSizeBytes = 0;

        _logger.LogInformation("USBImageCache initialized with max size: {MaxSize}GB", MaxCacheSizeBytes / 1024 / 1024 / 1024);
    }

    /// <summary>
    /// Tries to get a cached USB image.
    /// Updates LRU order and access metadata.
    /// </summary>
    public bool TryGetImage(string key, out byte[] image)
    {
        if (string.IsNullOrEmpty(key))
        {
            image = Array.Empty<byte>();
            return false;
        }

        lock (_lockObject)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccessedAt = DateTime.UtcNow;
                entry.AccessCount++;

                // Move to end of LRU list (most recently used)
                _lruList.Remove(key);
                _lruList.AddLast(key);

                image = entry.ImageData;
                _logger.LogInformation("Cache hit for key '{Key}' (access #{AccessCount})", key, entry.AccessCount);
                return true;
            }

            image = Array.Empty<byte>();
            _logger.LogInformation("Cache miss for key '{Key}'", key);
            return false;
        }
    }

    /// <summary>
    /// Stores a USB image in the cache.
    /// Evicts LRU entries if necessary to maintain size limit.
    /// </summary>
    public void StoreImage(string key, byte[] image)
    {
        if (string.IsNullOrEmpty(key) || image == null)
        {
            throw new ArgumentException("Key and image cannot be null");
        }

        lock (_lockObject)
        {
            // Remove existing entry if present
            if (_cache.TryGetValue(key, out var existing))
            {
                _currentSizeBytes -= existing.SizeBytes;
                _lruList.Remove(key);
                _logger.LogInformation("Replaced existing cache entry for key '{Key}'", key);
            }

            var imageSize = image.Length;

            // Evict entries if necessary
            while (_currentSizeBytes + imageSize > MaxCacheSizeBytes && _cache.Count > 0)
            {
                EvictLRUEntry();
            }

            // Handle case where single image exceeds max size
            if (imageSize > MaxCacheSizeBytes)
            {
                _logger.LogWarning(
                    "Image size ({Size}GB) exceeds max cache size ({MaxSize}GB). Storing anyway but limiting cache.",
                    imageSize / 1024 / 1024 / 1024, MaxCacheSizeBytes / 1024 / 1024 / 1024);

                // Clear cache to make room
                _cache.Clear();
                _lruList.Clear();
                _currentSizeBytes = 0;
            }

            var entry = new CacheEntry
            {
                ImageData = image,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 0
            };

            _cache[key] = entry;
            _lruList.AddLast(key);
            _currentSizeBytes += imageSize;

            _logger.LogInformation(
                "Cached image for key '{Key}' ({Size}MB). Cache utilization: {Percent:F2}%",
                key, imageSize / 1024 / 1024,
                (_currentSizeBytes * 100.0) / MaxCacheSizeBytes);
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            return new CacheStatistics
            {
                ItemCount = _cache.Count,
                TotalSizeBytes = _currentSizeBytes,
                MaxSizeBytes = MaxCacheSizeBytes,
                UtilizationPercent = (_currentSizeBytes * 100.0) / MaxCacheSizeBytes
            };
        }
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            var itemCount = _cache.Count;
            _cache.Clear();
            _lruList.Clear();
            _currentSizeBytes = 0;

            _logger.LogInformation("Cache cleared. Removed {ItemCount} items", itemCount);
        }
    }

    /// <summary>
    /// Gets detailed cache contents (for diagnostics).
    /// </summary>
    public IReadOnlyList<CacheEntryInfo> GetCacheContents()
    {
        lock (_lockObject)
        {
            var contents = _cache.Select(kvp => new CacheEntryInfo
            {
                Key = kvp.Key,
                SizeBytes = kvp.Value.SizeBytes,
                CreatedAt = kvp.Value.CreatedAt,
                LastAccessedAt = kvp.Value.LastAccessedAt,
                AccessCount = kvp.Value.AccessCount
            }).ToList();

            return contents.AsReadOnly();
        }
    }

    /// <summary>
    /// Evicts the least recently used entry.
    /// </summary>
    private void EvictLRUEntry()
    {
        if (_lruList.Count == 0)
        {
            return;
        }

        var lruKey = _lruList.First!.Value;

        if (_cache.TryGetValue(lruKey, out var entry))
        {
            _currentSizeBytes -= entry.SizeBytes;
            _cache.Remove(lruKey);
            _lruList.RemoveFirst();

            _logger.LogInformation(
                "Evicted LRU entry '{Key}' ({Size}MB). Cache now at {Percent:F2}% utilization",
                lruKey, entry.SizeBytes / 1024 / 1024,
                (_currentSizeBytes * 100.0) / MaxCacheSizeBytes);
        }
    }
}

/// <summary>
/// Information about a cache entry.
/// </summary>
public class CacheEntryInfo
{
    /// <summary>
    /// Gets or sets the cache key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last access time.
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the access count.
    /// </summary>
    public int AccessCount { get; set; }
}
