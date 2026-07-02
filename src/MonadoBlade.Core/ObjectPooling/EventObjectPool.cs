namespace MonadoBlade.Core.ObjectPooling;

/// <summary>
/// Represents an event object that can be pooled and reused.
/// </summary>
public class EventObject
{
    /// <summary>Gets or sets the event type.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the event timestamp.</summary>
    public long Timestamp { get; set; }

    /// <summary>Gets or sets the event data.</summary>
    public object? Data { get; set; }

    /// <summary>Gets or sets the event source.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Resets the event object to its initial state.
    /// </summary>
    public void Reset()
    {
        EventType = string.Empty;
        Timestamp = 0;
        Data = null;
        Source = string.Empty;
    }
}

/// <summary>
/// Specialized object pool for event objects.
/// </summary>
public class EventObjectPool : IDisposable
{
    private readonly ObjectPool<EventObject> _pool;
    private const int DefaultPoolSize = 128;
    
    private long _beforeAllocationCount;
    private long _afterAllocationCount;

    /// <summary>
    /// Initializes a new instance of the EventObjectPool.
    /// </summary>
    /// <param name="poolSize">Maximum number of event objects to pool.</param>
    public EventObjectPool(int poolSize = DefaultPoolSize)
    {
        _pool = new ObjectPool<EventObject>(
            factory: () => new EventObject(),
            resetAction: obj => obj.Reset(),
            poolSize: poolSize
        );

        _beforeAllocationCount = GC.GetTotalAllocatedBytes();
    }

    /// <summary>
    /// Rents an event object from the pool.
    /// </summary>
    /// <returns>An EventObject instance.</returns>
    public EventObject Rent()
    {
        return _pool.Rent();
    }

    /// <summary>
    /// Returns an event object to the pool.
    /// </summary>
    /// <param name="eventObj">The event object to return.</param>
    public void Return(EventObject eventObj)
    {
        _pool.Return(eventObj);
    }

    /// <summary>
    /// Gets the current metrics for the event object pool.
    /// </summary>
    /// <returns>Pool metrics.</returns>
    public EventObjectPoolMetrics GetMetrics()
    {
        var poolMetrics = _pool.GetMetrics();
        _afterAllocationCount = GC.GetTotalAllocatedBytes();

        var allocationReduction = _beforeAllocationCount < _afterAllocationCount
            ? ((double)(_afterAllocationCount - _beforeAllocationCount) / _beforeAllocationCount * 100)
            : 0;

        return new EventObjectPoolMetrics
        {
            PoolHits = poolMetrics.PoolHits,
            PoolMisses = poolMetrics.PoolMisses,
            TotalAllocations = poolMetrics.TotalAllocations,
            AvailableCount = poolMetrics.AvailableCount,
            HitRate = poolMetrics.HitRate,
            AllocationReductionPercentage = allocationReduction
        };
    }

    /// <summary>
    /// Disposes the event object pool.
    /// </summary>
    public void Dispose()
    {
        _pool?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Metrics for EventObjectPool.
/// </summary>
public class EventObjectPoolMetrics
{
    /// <summary>Gets or sets pool hits.</summary>
    public long PoolHits { get; set; }

    /// <summary>Gets or sets pool misses.</summary>
    public long PoolMisses { get; set; }

    /// <summary>Gets or sets total allocations.</summary>
    public long TotalAllocations { get; set; }

    /// <summary>Gets or sets available count in pool.</summary>
    public int AvailableCount { get; set; }

    /// <summary>Gets or sets pool hit rate percentage.</summary>
    public double HitRate { get; set; }

    /// <summary>Gets or sets the allocation reduction percentage.</summary>
    public double AllocationReductionPercentage { get; set; }
}
