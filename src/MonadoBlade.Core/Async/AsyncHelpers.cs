namespace MonadoBlade.Core.Async;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Async-friendly lazy initialization for deferred async operations.
/// Ensures the async value factory is called only once and result is cached.
/// </summary>
public class AsyncLazy<T> where T : class
{
    private readonly Lazy<Task<T>> _instance;
    private readonly Func<Task<T>> _valueFactory;

    public AsyncLazy(Func<Task<T>> valueFactory)
    {
        _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        _instance = new Lazy<Task<T>>(valueFactory);
    }

    /// <summary>
    /// Gets the value asynchronously, initializing if needed.
    /// </summary>
    public Task<T> Value => _instance.Value;

    /// <summary>
    /// Gets whether the value has been computed.
    /// </summary>
    public bool IsValueCreated => _instance.IsValueCreated;
}

/// <summary>
/// Batch processor for async operations with configurable batch size and timeout.
/// </summary>
public class AsyncBatchProcessor<TIn, TOut> where TOut : class
{
    private readonly Func<TIn[], CancellationToken, Task<TOut[]>> _batchProcessor;
    private readonly int _batchSize;
    private readonly TimeSpan _batchTimeout;
    private readonly object _lockObj = new object();
    private TIn[] _currentBatch = Array.Empty<TIn>();
    private int _currentBatchIndex = 0;
    private TaskCompletionSource<TOut[]>? _pendingBatchTcs;
    private CancellationTokenSource? _batchTimeoutCts;

    public AsyncBatchProcessor(
        Func<TIn[], CancellationToken, Task<TOut[]>> batchProcessor,
        int batchSize = 100,
        TimeSpan? batchTimeout = null)
    {
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        _batchSize = batchSize > 0 ? batchSize : throw new ArgumentException("Batch size must be positive", nameof(batchSize));
        _batchTimeout = batchTimeout ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Adds an item to the batch. Returns a task that completes when the batch is processed.
    /// </summary>
    public async Task<TOut> AddAsync(TIn item, CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            if (_currentBatchIndex >= _currentBatch.Length)
            {
                Array.Resize(ref _currentBatch, _batchSize);
            }

            _currentBatch[_currentBatchIndex] = item;
            _currentBatchIndex++;

            if (_pendingBatchTcs == null)
            {
                _pendingBatchTcs = new TaskCompletionSource<TOut[]>();
                _batchTimeoutCts = new CancellationTokenSource(_batchTimeout);

                // Start timeout timer
                _ = Task.Delay(_batchTimeout).ContinueWith(_ =>
                {
                    lock (_lockObj)
                    {
                        if (_currentBatchIndex > 0 && _pendingBatchTcs != null && !_pendingBatchTcs.Task.IsCompleted)
                        {
                            _ = FlushBatchAsync(cancellationToken);
                        }
                    }
                });
            }

            if (_currentBatchIndex >= _batchSize)
            {
                var batchResults = FlushBatchAsync(cancellationToken).Result;
                // Return the result for this specific item
                return batchResults[_currentBatchIndex - 1];
            }

            var itemIndex = _currentBatchIndex - 1;
            return _pendingBatchTcs.Task.Result[itemIndex];
        }
    }

    /// <summary>
    /// Flushes any pending items in the batch.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            if (_currentBatchIndex > 0)
            {
                _ = FlushBatchAsync(cancellationToken);
            }
        }
    }

    private async Task<TOut[]> FlushBatchAsync(CancellationToken cancellationToken)
    {
        if (_currentBatchIndex == 0)
            return Array.Empty<TOut>();

        var batchToProcess = new TIn[_currentBatchIndex];
        Array.Copy(_currentBatch, batchToProcess, _currentBatchIndex);

        try
        {
            var results = await _batchProcessor(batchToProcess, cancellationToken);
            _pendingBatchTcs?.TrySetResult(results);
            return results;
        }
        catch (Exception ex)
        {
            _pendingBatchTcs?.TrySetException(ex);
            throw;
        }
        finally
        {
            _currentBatchIndex = 0;
            _batchTimeoutCts?.Dispose();
            _pendingBatchTcs = null;
        }
    }
}

/// <summary>
/// Throttles concurrent async operations to a maximum concurrency level.
/// </summary>
public class AsyncThrottler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConcurrency;

    public AsyncThrottler(int? maxConcurrency = null)
    {
        var concurrency = maxConcurrency ?? Environment.ProcessorCount;
        _maxConcurrency = concurrency > 0 ? concurrency : throw new ArgumentException("Max concurrency must be positive");
        _semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
    }

    /// <summary>
    /// Executes an async operation with throttling.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Executes an async operation without returning a value.
    /// </summary>
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the number of currently available slots.
    /// </summary>
    public int AvailableSlots => _semaphore.CurrentCount;
}
