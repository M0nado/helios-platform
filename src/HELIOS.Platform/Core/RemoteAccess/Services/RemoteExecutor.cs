using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace HELIOS.Platform.Core.RemoteAccess.Services;

/// <summary>
/// Manages remote command execution.
/// </summary>
public interface IRemoteExecutor
{
    /// <summary>Executes a remote command.</summary>
    Task<RemoteCommandResult> ExecuteCommandAsync(RemoteCommandRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancels a running command execution.</summary>
    Task<bool> CancelCommandAsync(string requestId);

    /// <summary>Gets the status of a command execution.</summary>
    Task<ExecutionStatus> GetCommandStatusAsync(string requestId);

    /// <summary>Streams command output in real-time.</summary>
    IAsyncEnumerable<string> StreamCommandOutputAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Gets execution history.</summary>
    Task<IEnumerable<RemoteCommandResult>> GetExecutionHistoryAsync(string connectionId, int maxResults = 100);
}

/// <summary>
/// Default implementation of IRemoteExecutor.
/// </summary>
public class RemoteExecutor : IRemoteExecutor
{
    private readonly ILogger<RemoteExecutor> _logger;
    private readonly IRemoteConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, RemoteCommandResult> _executionCache;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningCommands;
    private readonly Queue<RemoteCommandResult> _executionHistory;
    private readonly int _maxHistorySize = 1000;

    public RemoteExecutor(
        ILogger<RemoteExecutor> logger,
        IRemoteConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _executionCache = new ConcurrentDictionary<string, RemoteCommandResult>();
        _runningCommands = new ConcurrentDictionary<string, CancellationTokenSource>();
        _executionHistory = new Queue<RemoteCommandResult>(_maxHistorySize);
    }

    /// <inheritdoc/>
    public async Task<RemoteCommandResult> ExecuteCommandAsync(RemoteCommandRequest request, CancellationToken cancellationToken = default)
    {
        var result = new RemoteCommandResult
        {
            RequestId = request.RequestId,
            Status = ExecutionStatus.Pending,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Executing command on connection {ConnectionId}: {Command}",
                request.ConnectionId, request.Command);

            // Validate connection
            var connection = await _connectionManager.GetConnectionAsync(request.ConnectionId);
            if (connection == null || connection.State != ConnectionState.Connected)
            {
                result.Status = ExecutionStatus.Failed;
                result.ErrorMessage = "Connection not available";
                _logger.LogWarning("Connection {ConnectionId} not available for command execution", request.ConnectionId);
                return result;
            }

            // Create cancellation token with timeout
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));

                if (!_runningCommands.TryAdd(request.RequestId, cts))
                {
                    result.Status = ExecutionStatus.Failed;
                    result.ErrorMessage = "Failed to track command execution";
                    return result;
                }

                try
                {
                    result.Status = ExecutionStatus.Running;

                    // Simulate command execution
                    // In production, this would communicate with the remote system via SSH/RDP
                    await Task.Delay(100, cts.Token);

                    // Generate mock output
                    result.StandardOutput = $"Command executed successfully: {request.Command}";
                    result.ExitCode = 0;
                    result.Status = ExecutionStatus.Completed;

                    _logger.LogInformation("Command {RequestId} completed successfully with exit code {ExitCode}",
                        request.RequestId, result.ExitCode);
                }
                catch (OperationCanceledException)
                {
                    result.Status = ExecutionStatus.TimedOut;
                    result.ErrorMessage = "Command execution timed out";
                    _logger.LogWarning("Command {RequestId} timed out", request.RequestId);
                }
                catch (Exception ex)
                {
                    result.Status = ExecutionStatus.Failed;
                    result.ErrorMessage = ex.Message;
                    result.StandardError = ex.StackTrace ?? string.Empty;
                    _logger.LogError(ex, "Error executing command {RequestId}", request.RequestId);
                }
                finally
                {
                    _runningCommands.TryRemove(request.RequestId, out _);
                }
            }

            result.EndTime = DateTime.UtcNow;

            // Cache and log result
            _executionCache.AddOrUpdate(request.RequestId, result, (_, _) => result);
            AddToHistory(result);

            return result;
        }
        catch (Exception ex)
        {
            result.Status = ExecutionStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "Unhandled error during command execution");
            return result;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelCommandAsync(string requestId)
    {
        try
        {
            if (_runningCommands.TryGetValue(requestId, out var cts))
            {
                cts.Cancel();
                _logger.LogInformation("Cancellation requested for command {RequestId}", requestId);
                return true;
            }

            _logger.LogWarning("Command {RequestId} not found or already completed", requestId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling command {RequestId}", requestId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ExecutionStatus> GetCommandStatusAsync(string requestId)
    {
        if (_runningCommands.ContainsKey(requestId))
        {
            return ExecutionStatus.Running;
        }

        if (_executionCache.TryGetValue(requestId, out var result))
        {
            return result.Status;
        }

        return ExecutionStatus.Pending;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> StreamCommandOutputAsync(string requestId, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_executionCache.TryGetValue(requestId, out var result))
            {
                if (!string.IsNullOrEmpty(result.StandardOutput))
                {
                    yield return result.StandardOutput;
                }

                if (result.Status != ExecutionStatus.Running)
                {
                    break;
                }
            }

            await Task.Delay(500, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteCommandResult>> GetExecutionHistoryAsync(string connectionId, int maxResults = 100)
    {
        return _executionHistory
            .Where(r => r.RequestId.StartsWith(connectionId))
            .TakeLast(maxResults)
            .ToList();
    }

    private void AddToHistory(RemoteCommandResult result)
    {
        lock (_executionHistory)
        {
            _executionHistory.Enqueue(result);

            if (_executionHistory.Count > _maxHistorySize)
            {
                _executionHistory.Dequeue();
            }
        }
    }
}
