namespace MonadoBlade.Core.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonadoBlade.Core.Async;

/// <summary>
/// Base class for service implementations providing common functionality.
/// </summary>
public abstract class ServiceBase : IService
{
    protected readonly ILogger _logger;
    protected readonly ConcurrentDictionary<string, object> _cache;
    protected readonly ConcurrentDictionary<string, object> _asyncLazyCache;
    private readonly AsyncThrottler _asyncThrottler;

    protected ServiceBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = new ConcurrentDictionary<string, object>();
        _asyncLazyCache = new ConcurrentDictionary<string, object>();
        _asyncThrottler = new AsyncThrottler(Environment.ProcessorCount * 2);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    protected void LogInfo(string message, params object?[] args)
    {
        _logger.LogInformation(message, args);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    protected void LogWarning(string message, params object?[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    protected void LogError(Exception ex, string message, params object?[] args)
    {
        _logger.LogError(ex, message, args);
    }

    /// <summary>
    /// Gets a cached value or computes it if not cached.
    /// </summary>
    protected async Task<T?> GetCachedOrComputeAsync<T>(
        string cacheKey,
        Func<Task<T?>> computeAsync,
        TimeSpan? cacheDuration = null) where T : class
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
            return (T?)cached;

        var value = await computeAsync();
        if (value != null)
            _cache.AddOrUpdate(cacheKey, value, (_, _) => value);

        return value;
    }

    /// <summary>
    /// Invalidates a cache entry.
    /// </summary>
    protected void InvalidateCache(string cacheKey)
    {
        _cache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Invalidates all cache entries matching a pattern.
    /// </summary>
    protected void InvalidateCachePattern(string pattern)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
            _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    protected void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets or creates an async lazy value using deferred initialization.
    /// </summary>
    protected AsyncLazy<T> GetOrCreateAsyncLazy<T>(
        string cacheKey,
        Func<Task<T>> valueFactory) where T : class
    {
        return (AsyncLazy<T>)_asyncLazyCache.GetOrAdd(cacheKey, _ => new AsyncLazy<T>(valueFactory));
    }

    /// <summary>
    /// Executes multiple async operations in parallel with automatic throttling.
    /// </summary>
    protected async Task<Dictionary<string, T>> ExecuteBatchAsync<T>(
        Dictionary<string, Func<CancellationToken, Task<T>>> operations,
        CancellationToken cancellationToken = default) where T : class
    {
        var results = new ConcurrentDictionary<string, T>();
        var tasks = new List<Task>();

        foreach (var (opKey, opFunc) in operations)
        {
            var task = _asyncThrottler.ExecuteAsync(async ct =>
            {
                var result = await opFunc(ct);
                results.TryAdd(opKey, result);
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        return results.ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Gets a cached async value or computes it with throttling if not cached.
    /// </summary>
    protected async Task<T?> GetCachedOrComputeAsyncThrottled<T>(
        string cacheKey,
        Func<Task<T?>> computeAsync,
        CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
            return (T?)cached;

        var value = await _asyncThrottler.ExecuteAsync(
            async ct => await computeAsync(),
            cancellationToken);

        if (value != null)
            _cache.AddOrUpdate(cacheKey, value, (_, _) => value);

        return value;
    }

    /// <summary>
    /// Creates a batch processor for handling items in batches.
    /// </summary>
    protected AsyncBatchProcessor<TIn, TOut> CreateBatchProcessor<TIn, TOut>(
        Func<TIn[], CancellationToken, Task<TOut[]>> batchProcessor,
        int batchSize = 100,
        TimeSpan? batchTimeout = null) where TOut : class
    {
        return new AsyncBatchProcessor<TIn, TOut>(batchProcessor, batchSize, batchTimeout);
    }

    /// <summary>
    /// Creates an async throttler for limiting concurrent operations.
    /// </summary>
    protected AsyncThrottler CreateThrottler(int maxConcurrency = 0)
    {
        return new AsyncThrottler(maxConcurrency == 0 ? Environment.ProcessorCount : maxConcurrency);
    }

    /// <summary>
    /// Gets current throttler statistics.
    /// </summary>
    protected Dictionary<string, int> GetThrottlerStats()
    {
        return new Dictionary<string, int>
        {
            { "AvailableSlots", _asyncThrottler.AvailableSlots },
            { "MaxConcurrency", Environment.ProcessorCount * 2 }
        };
    }
}

/// <summary>
/// Logger interface for dependency injection.
/// </summary>
public interface ILogger
{
    void LogInformation(string message, params object?[] args);
    void LogWarning(string message, params object?[] args);
    void LogError(Exception ex, string message, params object?[] args);
}
