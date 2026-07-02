using System.Collections.Concurrent;

namespace MonadoBlade.Core.ObjectPooling;

/// <summary>
/// A thread-safe, generic object pool for reusing objects and reducing GC pressure.
/// </summary>
/// <typeparam name="T">The type of object to pool.</typeparam>
public class ObjectPool<T> : IDisposable
{
    private readonly ConcurrentBag<T> _items;
    private readonly Func<T> _factory;
    private readonly Action<T>? _resetAction;
    private readonly int _maxPoolSize;
    
    private long _rentCount;
    private long _returnCount;
    private long _poolMisses;
    private long _poolHits;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the ObjectPool class.
    /// </summary>
    /// <param name="factory">Factory function to create new objects when pool is empty.</param>
    /// <param name="resetAction">Optional action to reset object state when returned to pool.</param>
    /// <param name="poolSize">Maximum number of objects to pool (default 500).</param>
    public ObjectPool(Func<T> factory, Action<T>? resetAction = null, int poolSize = 500)
    {
        if (poolSize < 1)
            throw new ArgumentException("Pool size must be at least 1.", nameof(poolSize));

        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _resetAction = resetAction;
        _maxPoolSize = poolSize;
        _items = new ConcurrentBag<T>();
        
        // Pre-allocate the pool
        for (int i = 0; i < poolSize; i++)
        {
            _items.Add(_factory());
        }
    }

    /// <summary>
    /// Gets the current size of the pool.
    /// </summary>
    public int PoolSize => _maxPoolSize;

    /// <summary>
    /// Gets the number of objects currently available in the pool.
    /// </summary>
    public int AvailableCount => _items.Count;

    /// <summary>
    /// Rents an object from the pool. If the pool is empty, creates a new object.
    /// </summary>
    /// <returns>An object of type T.</returns>
    public T Rent()
    {
        ThrowIfDisposed();

        if (_items.TryTake(out var item))
        {
            Interlocked.Increment(ref _poolHits);
            Interlocked.Increment(ref _rentCount);
            return item;
        }

        Interlocked.Increment(ref _poolMisses);
        Interlocked.Increment(ref _rentCount);
        return _factory();
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="item">The object to return.</param>
    public void Return(T item)
    {
        ThrowIfDisposed();

        if (item == null)
            return;

        Interlocked.Increment(ref _returnCount);

        // Only return to pool if we're under capacity
        if (_items.Count < _maxPoolSize)
        {
            _resetAction?.Invoke(item);
            _items.Add(item);
        }
    }

    /// <summary>
    /// Gets the current pool metrics.
    /// </summary>
    /// <returns>A PoolMetrics object with allocation statistics.</returns>
    public PoolMetrics GetMetrics()
    {
        ThrowIfDisposed();

        return new PoolMetrics
        {
            PoolHits = Interlocked.Read(ref _poolHits),
            PoolMisses = Interlocked.Read(ref _poolMisses),
            TotalAllocations = Interlocked.Read(ref _rentCount),
            TotalReturns = Interlocked.Read(ref _returnCount),
            AvailableCount = _items.Count,
            PoolSize = _maxPoolSize,
            HitRate = Interlocked.Read(ref _rentCount) > 0 
                ? (double)Interlocked.Read(ref _poolHits) / Interlocked.Read(ref _rentCount) * 100
                : 0.0
        };
    }

    /// <summary>
    /// Disposes the pool and clears all objects.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Clear the pool
        while (_items.TryTake(out var item))
        {
            (item as IDisposable)?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }
}

/// <summary>
/// Metrics for an ObjectPool instance.
/// </summary>
public class PoolMetrics
{
    /// <summary>
    /// Gets or sets the number of times an object was successfully retrieved from the pool.
    /// </summary>
    public long PoolHits { get; set; }

    /// <summary>
    /// Gets or sets the number of times a new object was created because the pool was empty.
    /// </summary>
    public long PoolMisses { get; set; }

    /// <summary>
    /// Gets or sets the total number of Rent() calls.
    /// </summary>
    public long TotalAllocations { get; set; }

    /// <summary>
    /// Gets or sets the total number of Return() calls.
    /// </summary>
    public long TotalReturns { get; set; }

    /// <summary>
    /// Gets or sets the number of objects currently available in the pool.
    /// </summary>
    public int AvailableCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the pool.
    /// </summary>
    public int PoolSize { get; set; }

    /// <summary>
    /// Gets or sets the hit rate percentage.
    /// </summary>
    public double HitRate { get; set; }

    /// <summary>
    /// Gets the allocation reduction ratio (hits / total allocations).
    /// </summary>
    public double AllocationReductionRatio => TotalAllocations > 0 
        ? (double)PoolHits / TotalAllocations 
        : 0.0;
}
