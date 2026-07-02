using System;
using System.Collections.Generic;
using MonadoBlade.Core.Messaging;

namespace MonadoBlade.Core.Integrations
{
    /// <summary>
    /// Example integration demonstrating how to use MessageCoalescer with a notification queue.
    /// This coalesces notifications by category, reducing the number of notification dispatches.
    /// </summary>
    public class NotificationQueueCoalescingExample
    {
        /// <summary>
        /// Represents a notification with category, priority, and content.
        /// </summary>
        public class Notification
        {
            public string Id { get; set; }
            public string Category { get; set; }
            public int Priority { get; set; }
            public string Message { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private readonly MessageCoalescer<Notification, string> _coalescer;
        private int _dispatchCount = 0;

        /// <summary>
        /// Initializes a new NotificationQueue with notification coalescing.
        /// Notifications are grouped by category and dispatched in batches.
        /// </summary>
        public NotificationQueueCoalescingExample()
        {
            _coalescer = new MessageCoalescer<Notification, string>(OnNotificationBatchReady);
            _coalescer.FlushInterval = 100;    // Flush every 100ms
            _coalescer.BatchSizeThreshold = 30; // Or when 30 notifications of same category accumulate
        }

        /// <summary>
        /// Enqueues a notification. Notifications are accumulated by category.
        /// </summary>
        public void Enqueue(string id, string category, int priority, string message)
        {
            var notification = new Notification
            {
                Id = id,
                Category = category,
                Priority = priority,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _coalescer.Enqueue(category, notification);
        }

        /// <summary>
        /// Callback invoked when a batch of notifications is ready for dispatch.
        /// </summary>
        private void OnNotificationBatchReady(string category, List<Notification> notifications)
        {
            _dispatchCount++;
            
            // In a real implementation, this would send notifications to subscribers
            var maxPriority = 0;
            foreach (var notif in notifications)
            {
                if (notif.Priority > maxPriority)
                    maxPriority = notif.Priority;
            }

            Console.WriteLine($"Dispatching notification batch #{_dispatchCount}: {notifications.Count} " +
                            $"notifications in category '{category}' (max priority: {maxPriority})");
        }

        /// <summary>
        /// Flushes any pending notifications immediately.
        /// </summary>
        public void Flush()
        {
            _coalescer.Flush();
        }

        /// <summary>
        /// Gets coalescing metrics.
        /// </summary>
        public Dictionary<string, object> GetMetrics()
        {
            return _coalescer.GetMetrics();
        }

        /// <summary>
        /// Gets the number of dispatch operations performed.
        /// </summary>
        public int DispatchCount => _dispatchCount;

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _coalescer?.Dispose();
        }
    }
}
