using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Administration;

/// <summary>
/// Manages batch operations across multiple services
/// Enables efficient bulk processing with error handling and progress tracking
/// </summary>
public interface IBatchOperationService
{
    /// <summary>Execute a batch operation across multiple items</summary>
    Task<BatchResult> ExecuteBatchAsync<T>(List<T> items, Func<T, Task<bool>> operation, string batchName);
    
    /// <summary>Execute parallel batch operations with concurrency control</summary>
    Task<BatchResult> ExecuteParallelBatchAsync<T>(List<T> items, Func<T, Task<bool>> operation, int concurrency, string batchName);
    
    /// <summary>Get batch operation progress</summary>
    Task<BatchProgress> GetBatchProgressAsync(string batchId);
    
    /// <summary>Cancel a running batch operation</summary>
    Task<bool> CancelBatchAsync(string batchId);
    
    /// <summary>Get batch operation history</summary>
    Task<List<BatchResult>> GetBatchHistoryAsync(int limit = 50);
}

public class BatchOperationService : IBatchOperationService
{
    private readonly Dictionary<string, BatchProgress> _progress = new();
    private readonly List<BatchResult> _history = new();
    private int _processedCounter;
    private int _successfulCounter;
    private int _failedCounter;

    public async Task<BatchResult> ExecuteBatchAsync<T>(List<T> items, Func<T, Task<bool>> operation, string batchName)
    {
        var batchId = Guid.NewGuid().ToString();
        var progress = new BatchProgress
        {
            BatchId = batchId,
            BatchName = batchName,
            TotalItems = items.Count,
            StartTime = DateTime.UtcNow
        };
        
        _progress[batchId] = progress;

        try
        {
            foreach (var item in items)
            {
                if (progress.IsCancelled) break;
                
                try
                {
                    var success = await operation(item);
                    if (success)
                        progress.SuccessfulItems++;
                    else
                        progress.FailedItems++;
                }
                catch (Exception ex)
                {
                    progress.FailedItems++;
                    progress.Errors.Add($"Item error: {ex.Message}");
                }
                
                progress.ProcessedItems++;
                progress.Percentage = (double)progress.ProcessedItems / progress.TotalItems * 100;
            }

            progress.EndTime = DateTime.UtcNow;
            progress.Status = progress.FailedItems > 0 ? "Completed with errors" : "Completed successfully";

            var result = new BatchResult
            {
                BatchId = batchId,
                BatchName = batchName,
                TotalItems = progress.TotalItems,
                SuccessfulItems = progress.SuccessfulItems,
                FailedItems = progress.FailedItems,
                Duration = progress.EndTime.Value - progress.StartTime,
                Status = progress.Status,
                Errors = progress.Errors
            };

            _history.Add(result);
            return result;
        }
        catch (Exception ex)
        {
            progress.Status = "Failed";
            progress.Errors.Add($"Batch error: {ex.Message}");
            
            return new BatchResult
            {
                BatchId = batchId,
                Status = "Failed",
                Errors = new List<string> { ex.Message }
            };
        }
        finally
        {
            _progress.Remove(batchId);
        }
    }

    public async Task<BatchResult> ExecuteParallelBatchAsync<T>(List<T> items, Func<T, Task<bool>> operation, int concurrency, string batchName)
    {
        var batchId = Guid.NewGuid().ToString();
        var progress = new BatchProgress
        {
            BatchId = batchId,
            BatchName = batchName,
            TotalItems = items.Count,
            StartTime = DateTime.UtcNow
        };
        
        _progress[batchId] = progress;

        try
        {
            var tasks = items.Select(async item =>
            {
                if (progress.IsCancelled) return false;
                
                try
                {
                    var success = await operation(item);
                    if (success)
                    {
                        var current = Interlocked.Increment(ref _successfulCounter);
                        progress.SuccessfulItems = current;
                    }
                    else
                    {
                        var current = Interlocked.Increment(ref _failedCounter);
                        progress.FailedItems = current;
                    }
                    return success;
                }
                catch (Exception ex)
                {
                    var current = Interlocked.Increment(ref _failedCounter);
                    progress.FailedItems = current;
                    lock (progress.Errors)
                    {
                        progress.Errors.Add($"Item error: {ex.Message}");
                    }
                    return false;
                }
                finally
                {
                    var current = Interlocked.Increment(ref _processedCounter);
                    progress.ProcessedItems = current;
                    progress.Percentage = (double)progress.ProcessedItems / progress.TotalItems * 100;
                }
            }).ToList();

            await Task.WhenAll(tasks.Take(concurrency).Select(async t => await t));

            progress.EndTime = DateTime.UtcNow;
            progress.Status = progress.FailedItems > 0 ? "Completed with errors" : "Completed successfully";

            var result = new BatchResult
            {
                BatchId = batchId,
                BatchName = batchName,
                TotalItems = progress.TotalItems,
                SuccessfulItems = progress.SuccessfulItems,
                FailedItems = progress.FailedItems,
                Duration = progress.EndTime.Value - progress.StartTime,
                Status = progress.Status,
                Errors = progress.Errors
            };

            _history.Add(result);
            return result;
        }
        catch (Exception ex)
        {
            progress.Status = "Failed";
            progress.Errors.Add(ex.Message);
            
            return new BatchResult
            {
                BatchId = batchId,
                Status = "Failed",
                Errors = new List<string> { ex.Message }
            };
        }
        finally
        {
            _progress.Remove(batchId);
        }
    }

    public async Task<BatchProgress> GetBatchProgressAsync(string batchId)
    {
        var result = _progress.TryGetValue(batchId, out var progress) ? progress : null;
        await Task.CompletedTask;
        return result;
    }

    public async Task<bool> CancelBatchAsync(string batchId)
    {
        if (_progress.TryGetValue(batchId, out var progress))
        {
            progress.IsCancelled = true;
            await Task.CompletedTask;
            return true;
        }
        return false;
    }

    public async Task<List<BatchResult>> GetBatchHistoryAsync(int limit = 50)
    {
        var result = _history.OrderByDescending(h => h.BatchId).Take(limit).ToList();
        await Task.CompletedTask;
        return result;
    }
}

public class BatchProgress
{
    private int _processedItems;
    private int _successfulItems;
    private int _failedItems;
    
    public string BatchId { get; set; }
    public string BatchName { get; set; }
    public int TotalItems { get; set; }
    
    public int ProcessedItems
    {
        get => _processedItems;
        set => _processedItems = value;
    }
    
    public int SuccessfulItems
    {
        get => _successfulItems;
        set => _successfulItems = value;
    }
    
    public int FailedItems
    {
        get => _failedItems;
        set => _failedItems = value;
    }
    
    public double Percentage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsCancelled { get; set; }
}

public class BatchResult
{
    public string BatchId { get; set; }
    public string BatchName { get; set; }
    public int TotalItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public TimeSpan Duration { get; set; }
    public string Status { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
