namespace MonadoBlade.Core.ObjectPooling;

/// <summary>
/// Specialized object pool for message buffer objects (512 byte buffers).
/// </summary>
public class MessageBufferPool : IDisposable
{
    private readonly ObjectPool<byte[]> _pool;
    private const int BufferSize = 512;
    private const int DefaultPoolSize = 256;
    
    private long _beforeAllocationCount;
    private long _afterAllocationCount;

    /// <summary>
    /// Initializes a new instance of the MessageBufferPool.
    /// </summary>
    /// <param name="poolSize">Maximum number of buffers to pool.</param>
    public MessageBufferPool(int poolSize = DefaultPoolSize)
    {
        _pool = new ObjectPool<byte[]>(
            factory: () => new byte[BufferSize],
            resetAction: buffer => Array.Clear(buffer, 0, buffer.Length),
            poolSize: poolSize
        );

        _beforeAllocationCount = GC.GetTotalAllocatedBytes();
    }

    /// <summary>
    /// Rents a message buffer from the pool.
    /// </summary>
    /// <returns>A 512-byte buffer.</returns>
    public byte[] Rent()
    {
        return _pool.Rent();
    }

    /// <summary>
    /// Returns a message buffer to the pool.
    /// </summary>
    /// <param name="buffer">The buffer to return.</param>
    public void Return(byte[] buffer)
    {
        _pool.Return(buffer);
    }

    /// <summary>
    /// Gets the current metrics for the message buffer pool.
    /// </summary>
    /// <returns>Pool metrics.</returns>
    public MessageBufferPoolMetrics GetMetrics()
    {
        var poolMetrics = _pool.GetMetrics();
        _afterAllocationCount = GC.GetTotalAllocatedBytes();

        var allocationReduction = _beforeAllocationCount < _afterAllocationCount
            ? ((double)(_afterAllocationCount - _beforeAllocationCount) / _beforeAllocationCount * 100)
            : 0;

        return new MessageBufferPoolMetrics
        {
            PoolHits = poolMetrics.PoolHits,
            PoolMisses = poolMetrics.PoolMisses,
            TotalAllocations = poolMetrics.TotalAllocations,
            AvailableCount = poolMetrics.AvailableCount,
            HitRate = poolMetrics.HitRate,
            BufferSize = BufferSize,
            AllocationReductionPercentage = allocationReduction
        };
    }

    /// <summary>
    /// Disposes the message buffer pool.
    /// </summary>
    public void Dispose()
    {
        _pool?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Metrics for MessageBufferPool.
/// </summary>
public class MessageBufferPoolMetrics
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

    /// <summary>Gets or sets the buffer size in bytes.</summary>
    public int BufferSize { get; set; }

    /// <summary>Gets or sets the allocation reduction percentage.</summary>
    public double AllocationReductionPercentage { get; set; }
}
