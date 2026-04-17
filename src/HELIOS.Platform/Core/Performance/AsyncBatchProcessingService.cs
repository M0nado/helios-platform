using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Async Batch Processing Service: Groups I/O operations to reduce context switches
    /// Optimized for Phase 1-2 services to improve throughput on bulk operations
    /// </summary>
    public interface IAsyncBatchProcessingService
    {
        Task<T[]> BatchProcessAsync<T>(IEnumerable<Func<Task<T>>> operations, int batchSize = 10);
        Task BatchExecuteAsync(IEnumerable<Func<Task>> operations, int batchSize = 10);
        Task<IEnumerable<TResult>> MapAsync<TSource, TResult>(
            IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> mapper,
            int degreeOfParallelism = 5);
    }

    public class AsyncBatchProcessingService : IAsyncBatchProcessingService
    {
        public async Task<T[]> BatchProcessAsync<T>(
            IEnumerable<Func<Task<T>>> operations,
            int batchSize = 10)
        {
            var operationsList = operations.ToList();
            var results = new T[operationsList.Count];
            var semaphore = new SemaphoreSlim(batchSize);

            var tasks = operationsList
                .Select((op, index) => ProcessWithSemaphoreAsync(op, index, results, semaphore))
                .ToList();

            await Task.WhenAll(tasks);
            return results;
        }

        public async Task BatchExecuteAsync(
            IEnumerable<Func<Task>> operations,
            int batchSize = 10)
        {
            var operationsList = operations.ToList();
            var semaphore = new SemaphoreSlim(batchSize);

            var tasks = operationsList
                .Select(op => ExecuteWithSemaphoreAsync(op, semaphore))
                .ToList();

            await Task.WhenAll(tasks);
        }

        public async Task<IEnumerable<TResult>> MapAsync<TSource, TResult>(
            IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> mapper,
            int degreeOfParallelism = 5)
        {
            var semaphore = new SemaphoreSlim(degreeOfParallelism);
            var sourceList = source.ToList();
            var results = new TResult[sourceList.Count];

            var tasks = sourceList
                .Select((item, index) => MapWithSemaphoreAsync(item, index, mapper, results, semaphore))
                .ToList();

            await Task.WhenAll(tasks);
            return results;
        }

        private async Task ProcessWithSemaphoreAsync<T>(
            Func<Task<T>> operation,
            int index,
            T[] results,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                results[index] = await operation();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ExecuteWithSemaphoreAsync(
            Func<Task> operation,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                await operation();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task MapWithSemaphoreAsync<TSource, TResult>(
            TSource item,
            int index,
            Func<TSource, Task<TResult>> mapper,
            TResult[] results,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                results[index] = await mapper(item);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
