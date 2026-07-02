using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Messaging
{
    /// <summary>
    /// Generic message coalescer that accumulates messages by key and dispatches them in batches.
    /// Reduces message throughput by combining similar messages and dispatching them together.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being coalesced</typeparam>
    /// <typeparam name="TKey">The key type for grouping messages</typeparam>
    public class MessageCoalescer<TMessage, TKey> : IDisposable
    {
        private readonly ConcurrentDictionary<TKey, List<TMessage>> _batches;
        private readonly Action<TKey, List<TMessage>> _onBatchReady;
        private readonly object _lockObject = new object();
        private Timer _flushTimer;
        private volatile bool _disposed = false;
        private long _totalMessagesEnqueued = 0;
        private long _totalBatchesDispatched = 0;
        private long _totalMessagesReduced = 0;

        /// <summary>
        /// Gets or sets the interval (in milliseconds) after which batches are automatically flushed.
        /// Default: 100ms
        /// </summary>
        public int FlushInterval { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum number of messages to accumulate before forcing a flush.
        /// Default: 50 messages
        /// </summary>
        public int BatchSizeThreshold { get; set; } = 50;

        /// <summary>
        /// Gets the number of distinct keys with coalesced messages waiting to be dispatched.
        /// </summary>
        public int CoalescedCount => _batches.Count;

        /// <summary>
        /// Gets the total number of messages that have been enqueued.
        /// </summary>
        public long TotalMessagesEnqueued => Interlocked.Read(ref _totalMessagesEnqueued);

        /// <summary>
        /// Gets the total number of batches that have been dispatched.
        /// </summary>
        public long TotalBatchesDispatched => Interlocked.Read(ref _totalBatchesDispatched);

        /// <summary>
        /// Gets the total number of messages that were reduced through coalescing.
        /// </summary>
        public long TotalMessagesReduced => Interlocked.Read(ref _totalMessagesReduced);

        /// <summary>
        /// Initializes a new instance of the MessageCoalescer class.
        /// </summary>
        /// <param name="onBatchReady">Callback invoked when a batch of messages is ready for dispatch.
        /// The callback receives the key and the list of coalesced messages for that key.</param>
        /// <exception cref="ArgumentNullException">Thrown when onBatchReady is null</exception>
        public MessageCoalescer(Action<TKey, List<TMessage>> onBatchReady)
        {
            _onBatchReady = onBatchReady ?? throw new ArgumentNullException(nameof(onBatchReady));
            _batches = new ConcurrentDictionary<TKey, List<TMessage>>();
            _flushTimer = new Timer(FlushCallback, null, FlushInterval, Timeout.Infinite);
        }

        /// <summary>
        /// Enqueues a message with the specified key. Messages with the same key are accumulated
        /// and dispatched together when either the batch size threshold is reached or the flush
        /// interval expires.
        /// </summary>
        /// <param name="key">The key to group the message by</param>
        /// <param name="message">The message to enqueue</param>
        /// <exception cref="ObjectDisposedException">Thrown when the coalescer is disposed</exception>
        public void Enqueue(TKey key, TMessage message)
        {
            ThrowIfDisposed();

            Interlocked.Increment(ref _totalMessagesEnqueued);

            _batches.AddOrUpdate(
                key,
                new List<TMessage> { message },
                (k, existingList) =>
                {
                    lock (existingList)
                    {
                        existingList.Add(message);
                        return existingList;
                    }
                });

            // Check if batch threshold is exceeded
            if (_batches.TryGetValue(key, out var batch))
            {
                lock (batch)
                {
                    if (batch.Count >= BatchSizeThreshold)
                    {
                        DispatchBatch(key, batch);
                    }
                }
            }
        }

        /// <summary>
        /// Manually flushes all accumulated batches, invoking the batch ready callback for each.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the coalescer is disposed</exception>
        public void Flush()
        {
            // Allow flush during dispose, but not after
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lockObject)
            {
                var keys = _batches.Keys.ToList();
                foreach (var key in keys)
                {
                    if (_batches.TryRemove(key, out var batch))
                    {
                        lock (batch)
                        {
                            if (batch.Count > 0)
                            {
                                DispatchBatchDirect(key, batch);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets comprehensive metrics about the coalescing efficiency.
        /// </summary>
        /// <returns>A dictionary containing metrics such as total messages, reduction count/percentage, 
        /// batches dispatched, and reduction percentage.</returns>
        public Dictionary<string, object> GetMetrics()
        {
            var totalEnqueued = TotalMessagesEnqueued;
            var totalDispatched = TotalBatchesDispatched;
            var totalReduced = TotalMessagesReduced;
            var reductionPercentage = totalEnqueued > 0 
                ? (totalReduced * 100.0) / totalEnqueued 
                : 0.0;

            return new Dictionary<string, object>
            {
                { "TotalMessagesEnqueued", totalEnqueued },
                { "TotalBatchesDispatched", totalDispatched },
                { "TotalMessagesReduced", totalReduced },
                { "ReductionPercentage", Math.Round(reductionPercentage, 2) },
                { "AverageBatchSize", totalDispatched > 0 ? (double)totalEnqueued / totalDispatched : 0.0 },
                { "CurrentCoalescedKeys", CoalescedCount }
            };
        }

        /// <summary>
        /// Resets all metrics to zero.
        /// </summary>
        public void ResetMetrics()
        {
            Interlocked.Exchange(ref _totalMessagesEnqueued, 0);
            Interlocked.Exchange(ref _totalBatchesDispatched, 0);
            Interlocked.Exchange(ref _totalMessagesReduced, 0);
        }

        private void FlushCallback(object state)
        {
            try
            {
                Flush();
            }
            finally
            {
                // Restart timer if not disposed
                if (!_disposed && _flushTimer != null)
                {
                    _flushTimer.Change(FlushInterval, Timeout.Infinite);
                }
            }
        }

        private void DispatchBatch(TKey key, List<TMessage> batch)
        {
            if (_batches.TryRemove(key, out _))
            {
                DispatchBatchDirect(key, batch);
            }
        }

        private void DispatchBatchDirect(TKey key, List<TMessage> batch)
        {
            if (batch.Count == 0)
                return;

            var batchCount = batch.Count;
            Interlocked.Increment(ref _totalBatchesDispatched);
            
            // Calculate reduction: if we're dispatching N messages as 1 batch, we reduced (N-1) messages
            if (batchCount > 1)
            {
                Interlocked.Add(ref _totalMessagesReduced, batchCount - 1);
            }

            try
            {
                _onBatchReady(key, new List<TMessage>(batch));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error dispatching batch for key {key}: {ex.Message}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Releases all resources used by the MessageCoalescer.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _flushTimer?.Dispose();
            _disposed = true;  // Set disposed AFTER stopping the timer but BEFORE flushing
            
            // Flush remaining messages without the disposed check
            lock (_lockObject)
            {
                var keys = _batches.Keys.ToList();
                foreach (var key in keys)
                {
                    if (_batches.TryRemove(key, out var batch))
                    {
                        lock (batch)
                        {
                            if (batch.Count > 0)
                            {
                                DispatchBatchDirect(key, batch);
                            }
                        }
                    }
                }
            }
            
            _batches.Clear();
        }
    }
}
