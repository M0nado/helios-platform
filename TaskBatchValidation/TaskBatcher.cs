using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Concurrency
{
    /// <summary>
    /// Generic task batcher that accumulates items and processes them in batches.
    /// Uses timeout-based flushing and parallel dispatch for high-throughput scenarios.
    /// Thread-safe implementation using ReaderWriterLockSlim for synchronization.
    /// </summary>
    /// <typeparam name="T">The type of items to batch process.</typeparam>
    public class TaskBatcher<T> : IDisposable
    {
        private readonly List<T> _queue;
        private readonly ReaderWriterLockSlim _queueLock;
        private readonly Timer _flushTimer;
        private readonly Action<List<T>> _batchCallback;
        private volatile bool _disposed;
        private int _batchSize;
        private int _flushInterval;

        /// <summary>
        /// Gets or sets the maximum number of items to accumulate before automatic flush.
        /// </summary>
        public int BatchSize
        {
            get => _batchSize;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Batch size must be greater than 0.", nameof(value));
                _batchSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the flush interval in milliseconds for timeout-based flushing.
        /// </summary>
        public int FlushInterval
        {
            get => _flushInterval;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Flush interval must be greater than 0.", nameof(value));
                _flushInterval = value;
            }
        }

        /// <summary>
        /// Gets the current number of items queued for batch processing.
        /// </summary>
        public int QueuedItemCount
        {
            get
            {
                _queueLock.EnterReadLock();
                try
                {
                    return _queue.Count;
                }
                finally
                {
                    _queueLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the TaskBatcher class.
        /// </summary>
        /// <param name="batchCallback">The callback action to invoke with accumulated items.</param>
        /// <param name="batchSize">The maximum number of items before automatic flush. Default is 100.</param>
        /// <param name="flushInterval">The timeout interval in milliseconds for automatic flush. Default is 50.</param>
        /// <exception cref="ArgumentNullException">Thrown when batchCallback is null.</exception>
        /// <exception cref="ArgumentException">Thrown when batchSize or flushInterval is invalid.</exception>
        public TaskBatcher(Action<List<T>> batchCallback, int batchSize = 100, int flushInterval = 50)
        {
            if (batchCallback == null)
                throw new ArgumentNullException(nameof(batchCallback));
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than 0.", nameof(batchSize));
            if (flushInterval <= 0)
                throw new ArgumentException("Flush interval must be greater than 0.", nameof(flushInterval));

            _batchCallback = batchCallback;
            _batchSize = batchSize;
            _flushInterval = flushInterval;
            _queue = new List<T>(batchSize);
            _queueLock = new ReaderWriterLockSlim();
            _flushTimer = new Timer(FlushTimerCallback, null, flushInterval, flushInterval);
            _disposed = false;
        }

        /// <summary>
        /// Enqueues an item for batch processing.
        /// Automatically flushes if batch size is reached.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
        public void Enqueue(T item)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TaskBatcher<T>));

            _queueLock.EnterWriteLock();
            try
            {
                _queue.Add(item);

                if (_queue.Count >= _batchSize)
                {
                    FlushInternal();
                }
            }
            finally
            {
                _queueLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Manually flushes all accumulated items in the queue.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
        public void Flush()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TaskBatcher<T>));

            _queueLock.EnterWriteLock();
            try
            {
                FlushInternal();
            }
            finally
            {
                _queueLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Disposes the task batcher and releases all resources.
        /// Automatically flushes any remaining items before disposal.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _flushTimer?.Dispose();

            _queueLock.EnterWriteLock();
            try
            {
                FlushInternal();
                _queue.Clear();
            }
            finally
            {
                _queueLock.ExitWriteLock();
            }

            _queueLock?.Dispose();
        }

        /// <summary>
        /// Internal method to perform the actual flush operation.
        /// Must be called with the write lock acquired.
        /// </summary>
        private void FlushInternal()
        {
            if (_queue.Count == 0)
                return;

            var batch = new List<T>(_queue);
            _queue.Clear();

            Task.Run(() =>
            {
                try
                {
                    _batchCallback?.Invoke(batch);
                }
                catch
                {
                    // Exceptions in the callback should not crash the batcher
                    // Callers should handle exceptions within their callback
                }
            });
        }

        /// <summary>
        /// Timer callback for automatic timeout-based flushing.
        /// </summary>
        private void FlushTimerCallback(object? state)
        {
            if (_disposed)
                return;

            _queueLock.EnterWriteLock();
            try
            {
                FlushInternal();
            }
            finally
            {
                _queueLock.ExitWriteLock();
            }
        }
    }
}
