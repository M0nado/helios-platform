using System;
using System.Collections.Generic;
using MonadoBlade.Core.Concurrency;

namespace MonadoBlade.Core.Integration.Examples
{
    /// <summary>
    /// Example integration of TaskBatcher with event handler pattern.
    /// Demonstrates how to batch events before invoking handlers.
    /// </summary>
    public class EventHandlerExample
    {
        /// <summary>
        /// Represents a domain event.
        /// </summary>
        public abstract class DomainEvent
        {
            public string EventId { get; set; } = Guid.NewGuid().ToString();
            public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
            public string EventType { get; set; } = "";
        }

        /// <summary>
        /// Example domain event.
        /// </summary>
        public class UserCreatedEvent : DomainEvent
        {
            public string UserId { get; set; } = "";
            public string Email { get; set; } = "";
            public string FullName { get; set; } = "";

            public UserCreatedEvent()
            {
                EventType = "UserCreated";
            }
        }

        /// <summary>
        /// Event handler using TaskBatcher for batched event processing.
        /// Useful for high-volume event scenarios where batch processing is more efficient.
        /// </summary>
        public class BatchedEventHandler : IDisposable
        {
            private readonly TaskBatcher<DomainEvent> _eventBatcher;
            private readonly Action<List<DomainEvent>> _eventProcessor;

            /// <summary>
            /// Initializes a new event handler with batched event processing.
            /// </summary>
            /// <param name="eventProcessor">Callback to process batches of events.</param>
            /// <param name="batchSize">Number of events to accumulate before processing. Default: 100.</param>
            /// <param name="flushInterval">Timeout in milliseconds for batch dispatch. Default: 50ms.</param>
            public BatchedEventHandler(
                Action<List<DomainEvent>> eventProcessor,
                int batchSize = 100,
                int flushInterval = 50)
            {
                _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
                _eventBatcher = new TaskBatcher<DomainEvent>(ProcessEventBatch, batchSize, flushInterval);
            }

            /// <summary>
            /// Handles a domain event by enqueuing it for batch processing.
            /// </summary>
            public void Handle(DomainEvent @event)
            {
                if (@event == null)
                    throw new ArgumentNullException(nameof(@event));

                _eventBatcher.Enqueue(@event);
            }

            /// <summary>
            /// Internally processes a batch of events.
            /// </summary>
            private void ProcessEventBatch(List<DomainEvent> events)
            {
                try
                {
                    _eventProcessor.Invoke(events);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing event batch: {ex}");
                }
            }

            /// <summary>
            /// Gets the number of queued events awaiting processing.
            /// </summary>
            public int QueuedEventCount => _eventBatcher.QueuedItemCount;

            /// <summary>
            /// Flushes all queued events immediately.
            /// </summary>
            public void Flush() => _eventBatcher.Flush();

            public void Dispose()
            {
                _eventBatcher?.Dispose();
            }
        }

        /// <summary>
        /// Example usage of BatchedEventHandler.
        /// </summary>
        public static void ExampleUsage()
        {
            Console.WriteLine("=== EventHandler Batching Example ===\n");

            var processedBatches = new List<List<DomainEvent>>();
            var eventHandler = new BatchedEventHandler(
                batch =>
                {
                    processedBatches.Add(new List<DomainEvent>(batch));
                    Console.WriteLine($"Processing event batch with {batch.Count} events:");
                    foreach (var @event in batch)
                    {
                        if (@event is UserCreatedEvent uce)
                        {
                            Console.WriteLine($"  - {uce.EventType}: User {uce.UserId} ({uce.Email})");
                        }
                    }
                },
                batchSize: 25,
                flushInterval: 100);

            // Publish 100 user created events
            for (int i = 0; i < 100; i++)
            {
                var @event = new UserCreatedEvent
                {
                    UserId = $"USER_{i:D4}",
                    Email = $"user{i}@example.com",
                    FullName = $"User {i}"
                };
                eventHandler.Handle(@event);
            }

            // Flush remaining events
            eventHandler.Flush();

            // Wait for async processing
            System.Threading.Thread.Sleep(500);

            Console.WriteLine($"\nTotal batches processed: {processedBatches.Count}");
            Console.WriteLine($"Total events processed: {processedBatches.Sum(b => b.Count)}");

            eventHandler.Dispose();
        }
    }
}
