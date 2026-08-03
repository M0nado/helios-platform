using System.Collections.Concurrent;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Performance;

public interface IL1CacheService
{
    Task<T> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
    bool TryGet<T>(string key, out T value);
    void Set<T>(string key, T value, TimeSpan ttl);
    bool Remove(string key)
    {
        Set<object?>(key, null, TimeSpan.Zero);
        return true;
    }
    void Clear();
}

public sealed class L1CacheService : IL1CacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly ILogger _logger;

    private sealed class CacheItem
    {
        public object? Value { get; init; }
        public DateTime ExpiresAtUtc { get; init; }
    }

    public L1CacheService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        if (TryGet(key, out T existing))
        {
            return existing;
        }

        var created = await factory();
        Set(key, created, ttl);
        return created;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (_cache.TryGetValue(key, out var item) && item.ExpiresAtUtc > DateTime.UtcNow && item.Value is T typed)
        {
            value = typed;
            return true;
        }

        _cache.TryRemove(key, out _);
        value = default!;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan ttl)
    {
        _cache[key] = new CacheItem
        {
            Value = value,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
        };
    }

    public bool Remove(string key)
    {
        return _cache.TryRemove(key, out _);
    }

    public void Clear()
    {
        _cache.Clear();
        _logger.Info("L1 cache cleared");
    }
}

public sealed class InMemoryL2Cache : L2CacheService
{
}

public interface IAdvancedCacheService
{
}

public sealed class AdvancedCacheService : IAdvancedCacheService
{
    public AdvancedCacheService(IL1CacheService l1CacheService, IL2CacheService l2CacheService, ILogger logger)
    {
        L1CacheService = l1CacheService;
        L2CacheService = l2CacheService;
        Logger = logger;
    }

    public IL1CacheService L1CacheService { get; }
    public IL2CacheService L2CacheService { get; }
    public ILogger Logger { get; }
}
