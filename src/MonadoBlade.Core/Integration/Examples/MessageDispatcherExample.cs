using System;
using System.Collections.Generic;
using MonadoBlade.Core.Concurrency;

namespace MonadoBlade.Core.Integration.Examples
{
    /// <summary>
    /// Example integration of TaskBatcher with message dispatcher pattern.
    /// Demonstrates how to batch messages before dispatching to handlers.
    /// </summary>
    public class MessageDispatcherExample
    {
        /// <summary>
        /// Represents a message to be dispatched.
        /// </summary>
        public class Message
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Type { get; set; } = "";
            public object? Payload { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Message dispatcher using TaskBatcher for batched message handling.
        /// </summary>
        public class BatchedMessageDispatcher : IDisposable
        {
            private readonly TaskBatcher<Message> _messageBatcher;
            private readonly Action<List<Message>> _messageHandler;

            /// <summary>
            /// Initializes a new message dispatcher with batched message handling.
            /// </summary>
            /// <param name="messageHandler">Callback to handle batches of messages.</param>
            /// <param name="batchSize">Number of messages to accumulate before dispatch. Default: 100.</param>
            /// <param name="flushInterval">Timeout in milliseconds for dispatch. Default: 50ms.</param>
            public BatchedMessageDispatcher(
                Action<List<Message>> messageHandler,
                int batchSize = 100,
                int flushInterval = 50)
            {
                _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
                _messageBatcher = new TaskBatcher<Message>(HandleMessageBatch, batchSize, flushInterval);
            }

            /// <summary>
            /// Dispatches a message for processing.
            /// The message will be batched and processed with others.
            /// </summary>
            public void Dispatch(Message message)
            {
                if (message == null)
                    throw new ArgumentNullException(nameof(message));

                _messageBatcher.Enqueue(message);
            }

            /// <summary>
            /// Internally handles a batch of messages.
            /// </summary>
            private void HandleMessageBatch(List<Message> messages)
            {
                try
                {
                    _messageHandler.Invoke(messages);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling message batch: {ex}");
                }
            }

            /// <summary>
            /// Gets the number of queued messages awaiting dispatch.
            /// </summary>
            public int QueuedMessageCount => _messageBatcher.QueuedItemCount;

            /// <summary>
            /// Immediately flushes all queued messages.
            /// </summary>
            public void Flush() => _messageBatcher.Flush();

            public void Dispose()
            {
                _messageBatcher?.Dispose();
            }
        }

        /// <summary>
        /// Example usage of BatchedMessageDispatcher.
        /// </summary>
        public static void ExampleUsage()
        {
            Console.WriteLine("=== MessageDispatcher Batching Example ===\n");

            var processedBatches = new List<List<Message>>();

            // Create dispatcher with batch size of 50 and 100ms flush interval
            var dispatcher = new BatchedMessageDispatcher(
                batch =>
                {
                    processedBatches.Add(new List<Message>(batch));
                    Console.WriteLine($"Processing batch of {batch.Count} messages");
                    foreach (var msg in batch)
                    {
                        Console.WriteLine($"  - Message ID: {msg.Id}, Type: {msg.Type}");
                    }
                },
                batchSize: 50,
                flushInterval: 100);

            // Dispatch 150 messages
            for (int i = 0; i < 150; i++)
            {
                dispatcher.Dispatch(new Message
                {
                    Type = $"Order.Created.v{i % 3 + 1}",
                    Payload = new { OrderId = i, Amount = 100.0 * i }
                });
            }

            // Manual flush to ensure all messages are processed
            dispatcher.Flush();

            // Wait for async processing to complete
            System.Threading.Thread.Sleep(500);

            Console.WriteLine($"\nTotal batches processed: {processedBatches.Count}");
            Console.WriteLine($"Total messages processed: {processedBatches.Sum(b => b.Count)}");

            dispatcher.Dispose();
        }
    }
}
