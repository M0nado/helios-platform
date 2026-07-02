using System;
using System.Collections.Generic;
using MonadoBlade.Core.Messaging;

namespace MonadoBlade.Core.Integrations
{
    /// <summary>
    /// Example integration demonstrating how to use MessageCoalescer with an EventPublisher pattern.
    /// This coalesces similar events by type and source, reducing the number of event publications.
    /// </summary>
    public class EventPublisherCoalescingExample
    {
        /// <summary>
        /// Represents an event with type and source information.
        /// </summary>
        public class Event
        {
            public string Type { get; set; }
            public string Source { get; set; }
            public string Data { get; set; }
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Key for grouping events by type and source.
        /// </summary>
        public class EventKey
        {
            public string Type { get; set; }
            public string Source { get; set; }

            public override bool Equals(object obj)
            {
                return obj is EventKey key && key.Type == Type && key.Source == Source;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Type, Source);
            }

            public override string ToString()
            {
                return $"{Type}::{Source}";
            }
        }

        private readonly MessageCoalescer<Event, EventKey> _coalescer;

        /// <summary>
        /// Initializes a new EventPublisher with event coalescing.
        /// Similar events (same type and source) are batched and published together.
        /// </summary>
        public EventPublisherCoalescingExample()
        {
            _coalescer = new MessageCoalescer<Event, EventKey>(OnEventBatchReady);
            _coalescer.FlushInterval = 50;    // Flush every 50ms
            _coalescer.BatchSizeThreshold = 25; // Or when 25 events of same type/source accumulate
        }

        /// <summary>
        /// Publishes an event. The event is accumulated with similar events and
        /// dispatched when the batch is ready.
        /// </summary>
        public void PublishEvent(string type, string source, string data)
        {
            var key = new EventKey { Type = type, Source = source };
            var evt = new Event
            {
                Type = type,
                Source = source,
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            _coalescer.Enqueue(key, evt);
        }

        /// <summary>
        /// Callback invoked when a batch of events is ready for publication.
        /// </summary>
        private void OnEventBatchReady(EventKey key, List<Event> events)
        {
            // In a real implementation, this would publish events to subscribers
            Console.WriteLine($"Publishing {events.Count} events of type '{key.Type}' from source '{key.Source}'");
            foreach (var evt in events)
            {
                Console.WriteLine($"  - {evt.Data} at {evt.Timestamp:O}");
            }
        }

        /// <summary>
        /// Flushes any pending events immediately.
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
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _coalescer?.Dispose();
        }
    }
}
