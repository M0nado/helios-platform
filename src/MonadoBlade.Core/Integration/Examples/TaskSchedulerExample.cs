using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MonadoBlade.Core.Concurrency;

namespace MonadoBlade.Core.Integration.Examples
{
    /// <summary>
    /// Example integration of TaskBatcher with task scheduler pattern.
    /// Demonstrates how to batch work items for efficient scheduled execution.
    /// </summary>
    public class TaskSchedulerExample
    {
        /// <summary>
        /// Represents a scheduled work item.
        /// </summary>
        public class WorkItem
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Description { get; set; } = "";
            public Func<Task>? Action { get; set; }
            public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
            public int Priority { get; set; } = 0;
        }

        /// <summary>
        /// Task scheduler using TaskBatcher for batched task execution.
        /// Efficiently executes work items in batches with optional prioritization.
        /// </summary>
        public class BatchedTaskScheduler : IDisposable
        {
            private readonly TaskBatcher<WorkItem> _workItemBatcher;
            private readonly int _maxConcurrentTasks;
            private readonly SemaphoreSlim _concurrencyLimiter;

            /// <summary>
            /// Initializes a new task scheduler with batched work item processing.
            /// </summary>
            /// <param name="batchSize">Number of work items to accumulate before dispatch. Default: 100.</param>
            /// <param name="flushInterval">Timeout in milliseconds for batch dispatch. Default: 50ms.</param>
            /// <param name="maxConcurrentTasks">Maximum concurrent tasks to execute. Default: 10.</param>
            public BatchedTaskScheduler(int batchSize = 100, int flushInterval = 50, int maxConcurrentTasks = 10)
            {
                if (maxConcurrentTasks <= 0)
                    throw new ArgumentException("Max concurrent tasks must be greater than 0.", nameof(maxConcurrentTasks));

                _maxConcurrentTasks = maxConcurrentTasks;
                _concurrencyLimiter = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);
                _workItemBatcher = new TaskBatcher<WorkItem>(ExecuteWorkItemBatch, batchSize, flushInterval);
            }

            /// <summary>
            /// Schedules a work item for batched execution.
            /// </summary>
            public void Schedule(WorkItem workItem)
            {
                if (workItem == null)
                    throw new ArgumentNullException(nameof(workItem));

                _workItemBatcher.Enqueue(workItem);
            }

            /// <summary>
            /// Schedules a work item with a delegate action.
            /// </summary>
            public void Schedule(string description, Func<Task> action)
            {
                if (string.IsNullOrWhiteSpace(description))
                    throw new ArgumentException("Description cannot be empty.", nameof(description));
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                var workItem = new WorkItem
                {
                    Description = description,
                    Action = action
                };

                Schedule(workItem);
            }

            /// <summary>
            /// Internally executes a batch of work items with concurrency control.
            /// </summary>
            private void ExecuteWorkItemBatch(List<WorkItem> workItems)
            {
                var tasks = new List<Task>();

                foreach (var workItem in workItems)
                {
                    tasks.Add(ExecuteWorkItemWithConcurrencyControl(workItem));
                }

                Task.WaitAll(tasks.ToArray());
            }

            /// <summary>
            /// Executes a single work item with concurrency limiting.
            /// </summary>
            private async Task ExecuteWorkItemWithConcurrencyControl(WorkItem workItem)
            {
                await _concurrencyLimiter.WaitAsync();
                try
                {
                    if (workItem.Action != null)
                    {
                        await workItem.Action.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing work item {workItem.Id}: {ex}");
                }
                finally
                {
                    _concurrencyLimiter.Release();
                }
            }

            /// <summary>
            /// Gets the number of queued work items.
            /// </summary>
            public int QueuedWorkItemCount => _workItemBatcher.QueuedItemCount;

            /// <summary>
            /// Flushes all queued work items and executes them immediately.
            /// </summary>
            public void Flush() => _workItemBatcher.Flush();

            public void Dispose()
            {
                _workItemBatcher?.Dispose();
                _concurrencyLimiter?.Dispose();
            }
        }

        /// <summary>
        /// Example usage of BatchedTaskScheduler.
        /// </summary>
        public static async Task ExampleUsage()
        {
            Console.WriteLine("=== TaskScheduler Batching Example ===\n");

            var executedCount = 0;
            var stopwatch = Stopwatch.StartNew();

            var scheduler = new BatchedTaskScheduler(batchSize: 20, flushInterval: 100, maxConcurrentTasks: 5);

            // Schedule 100 work items
            for (int i = 0; i < 100; i++)
            {
                int itemIndex = i;
                scheduler.Schedule(
                    $"Process item {itemIndex}",
                    async () =>
                    {
                        await Task.Delay(10); // Simulate work
                        Interlocked.Increment(ref executedCount);
                        Console.WriteLine($"  Executed work item {itemIndex}");
                    });
            }

            // Flush and wait for all to complete
            scheduler.Flush();
            Thread.Sleep(2000); // Give async operations time to complete

            stopwatch.Stop();

            Console.WriteLine($"\nTotal work items executed: {executedCount}");
            Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average per item: {(double)stopwatch.ElapsedMilliseconds / executedCount:F2}ms");

            scheduler.Dispose();
        }
    }
}
