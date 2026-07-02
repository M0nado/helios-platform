namespace MonadoBlade.Core.ObjectPooling;

/// <summary>
/// Represents a task object that can be pooled and reused.
/// </summary>
public class TaskObject
{
    /// <summary>Gets or sets the task ID.</summary>
    public Guid TaskId { get; set; }

    /// <summary>Gets or sets the task name.</summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>Gets or sets the task status.</summary>
    public TaskStatus Status { get; set; }

    /// <summary>Gets or sets the task creation timestamp.</summary>
    public long CreatedAt { get; set; }

    /// <summary>Gets or sets the task execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>Gets or sets the task payload.</summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Resets the task object to its initial state.
    /// </summary>
    public void Reset()
    {
        TaskId = Guid.Empty;
        TaskName = string.Empty;
        Status = TaskStatus.Pending;
        CreatedAt = 0;
        ExecutionTimeMs = 0;
        Payload = null;
    }
}

/// <summary>
/// Enumeration for task status.
/// </summary>
public enum TaskStatus
{
    /// <summary>Task is pending.</summary>
    Pending = 0,
    /// <summary>Task is running.</summary>
    Running = 1,
    /// <summary>Task completed successfully.</summary>
    Completed = 2,
    /// <summary>Task failed.</summary>
    Failed = 3,
    /// <summary>Task was cancelled.</summary>
    Cancelled = 4
}

/// <summary>
/// Specialized object pool for task objects.
/// </summary>
public class TaskObjectPool : IDisposable
{
    private readonly ObjectPool<TaskObject> _pool;
    private const int DefaultPoolSize = 256;
    
    private long _beforeAllocationCount;
    private long _afterAllocationCount;

    /// <summary>
    /// Initializes a new instance of the TaskObjectPool.
    /// </summary>
    /// <param name="poolSize">Maximum number of task objects to pool.</param>
    public TaskObjectPool(int poolSize = DefaultPoolSize)
    {
        _pool = new ObjectPool<TaskObject>(
            factory: () => new TaskObject(),
            resetAction: obj => obj.Reset(),
            poolSize: poolSize
        );

        _beforeAllocationCount = GC.GetTotalAllocatedBytes();
    }

    /// <summary>
    /// Rents a task object from the pool.
    /// </summary>
    /// <returns>A TaskObject instance.</returns>
    public TaskObject Rent()
    {
        return _pool.Rent();
    }

    /// <summary>
    /// Returns a task object to the pool.
    /// </summary>
    /// <param name="taskObj">The task object to return.</param>
    public void Return(TaskObject taskObj)
    {
        _pool.Return(taskObj);
    }

    /// <summary>
    /// Gets the current metrics for the task object pool.
    /// </summary>
    /// <returns>Pool metrics.</returns>
    public TaskObjectPoolMetrics GetMetrics()
    {
        var poolMetrics = _pool.GetMetrics();
        _afterAllocationCount = GC.GetTotalAllocatedBytes();

        var allocationReduction = _beforeAllocationCount < _afterAllocationCount
            ? ((double)(_afterAllocationCount - _beforeAllocationCount) / _beforeAllocationCount * 100)
            : 0;

        return new TaskObjectPoolMetrics
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
    /// Disposes the task object pool.
    /// </summary>
    public void Dispose()
    {
        _pool?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Metrics for TaskObjectPool.
/// </summary>
public class TaskObjectPoolMetrics
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
