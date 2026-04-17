namespace HELIOS.Platform.Core.ActionFlow;

/// <summary>
/// Default implementation of IWorkflow - represents a complete, executable workflow.
/// </summary>
public class Workflow : IWorkflow
{
    private readonly List<IWorkflowAction> _actions = new();
    private readonly Stack<WorkflowActionHistory> _undoStack = new();
    private readonly Stack<WorkflowActionHistory> _redoStack = new();
    private TaskCompletionSource<bool>? _pauseTcs;
    private CancellationTokenSource? _cancellationSource;

    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Version Version { get; }
    public string Category { get; }
    public IReadOnlyList<IWorkflowAction> Actions => _actions.AsReadOnly();
    public WorkflowExecutionState ExecutionState { get; private set; } = WorkflowExecutionState.Pending;
    public int Progress { get; private set; } = 0;

    /// <summary>
    /// Creates a new workflow with the specified metadata.
    /// </summary>
    public Workflow(string name, string description, string category, Version? version = null)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Version = version ?? new Version(1, 0, 0, 0);
    }

    /// <summary>
    /// Adds an action to the workflow.
    /// </summary>
    public void AddAction(IWorkflowAction action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _actions.Add(action);
    }

    /// <summary>
    /// Executes the workflow step by step, handling conditions, retries, and undo/redo.
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        ExecutionState = WorkflowExecutionState.Running;
        Progress = 0;

        var executionId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var completedActions = new List<ActionExecutionRecord>();
        var errors = new List<string>();
        var actionLookup = _actions.ToDictionary(a => a.Id);

        _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Execute actions sequentially, handling dependencies and parallelization
            Guid? currentActionId = _actions.FirstOrDefault()?.Id;
            var executedActionIds = new HashSet<Guid>();

            while (currentActionId.HasValue && !_cancellationSource.Token.IsCancellationRequested)
            {
                // Handle pause requests
                if (_pauseTcs != null)
                {
                    ExecutionState = WorkflowExecutionState.Paused;
                    await _pauseTcs.Task;
                    ExecutionState = WorkflowExecutionState.Running;
                    _pauseTcs = null;
                }

                if (!actionLookup.TryGetValue(currentActionId.Value, out var action))
                    break;

                // Check if all prerequisites are satisfied
                var prerequisites = _actions.Where(a => a.NextActionId == currentActionId).ToList();
                if (prerequisites.Any(p => !executedActionIds.Contains(p.Id)))
                {
                    currentActionId = _actions.FirstOrDefault(a => !executedActionIds.Contains(a.Id))?.Id;
                    continue;
                }

                // Evaluate conditions
                if (action.Conditions.Any() && !action.Conditions.All(c => c.Evaluate(context)))
                {
                    executedActionIds.Add(currentActionId.Value);
                    currentActionId = action.NextActionId;
                    continue;
                }

                // Execute action with retry policy
                var result = await ExecuteActionWithRetryAsync(action, context, _cancellationSource.Token);
                
                var record = new ActionExecutionRecord
                {
                    ActionId = action.Id,
                    ActionName = action.Name,
                    ExecutionTime = DateTime.UtcNow,
                    Duration = result.ExecutionTime,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage,
                    Output = result.Data
                };

                completedActions.Add(record);
                context.ExecutionRecords.Add(record);

                if (!result.Success)
                {
                    errors.Add($"Action '{action.Name}' failed: {result.ErrorMessage}");
                }
                else
                {
                    _undoStack.Push(new WorkflowActionHistory
                    {
                        ActionId = action.Id,
                        ActionName = action.Name,
                        ExecutedAt = DateTime.UtcNow,
                        Successful = true,
                        Output = result.Data?.ToString(),
                        ExecutionDuration = result.ExecutionTime
                    });
                }

                executedActionIds.Add(currentActionId.Value);
                Progress = (int)((executedActionIds.Count / (double)_actions.Count) * 100);

                // Handle parallel actions
                if (action.ParallelActionIds.Count > 0)
                {
                    var parallelTasks = action.ParallelActionIds
                        .Where(id => actionLookup.ContainsKey(id) && !executedActionIds.Contains(id))
                        .Select(id => ExecuteActionWithRetryAsync(actionLookup[id], context, _cancellationSource.Token));

                    var parallelResults = await Task.WhenAll(parallelTasks);

                    foreach (var parallelResult in parallelResults)
                    {
                        completedActions.Add(new ActionExecutionRecord
                        {
                            ActionId = Guid.NewGuid(),
                            ActionName = "Parallel Action",
                            ExecutionTime = DateTime.UtcNow,
                            Duration = parallelResult.ExecutionTime,
                            Success = parallelResult.Success,
                            ErrorMessage = parallelResult.ErrorMessage
                        });
                    }
                }

                currentActionId = action.NextActionId;
            }

            var endTime = DateTime.UtcNow;
            var success = !errors.Any() && ExecutionState != WorkflowExecutionState.Cancelled;
            ExecutionState = success ? WorkflowExecutionState.Completed : WorkflowExecutionState.Failed;
            Progress = 100;

            return new WorkflowResult
            {
                ExecutionId = executionId,
                WorkflowId = Id,
                Success = success,
                FinalState = ExecutionState,
                StartTime = startTime,
                EndTime = endTime,
                CompletedActions = completedActions,
                Errors = errors,
                FinalVariables = new Dictionary<string, object?>(context.Variables)
            };
        }
        catch (OperationCanceledException)
        {
            ExecutionState = WorkflowExecutionState.Cancelled;
            return new WorkflowResult
            {
                ExecutionId = executionId,
                WorkflowId = Id,
                Success = false,
                FinalState = WorkflowExecutionState.Cancelled,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                CompletedActions = completedActions,
                Errors = new List<string> { "Workflow was cancelled." }
            };
        }
        catch (Exception ex)
        {
            ExecutionState = WorkflowExecutionState.Failed;
            errors.Add($"Workflow execution failed: {ex.Message}");
            return new WorkflowResult
            {
                ExecutionId = executionId,
                WorkflowId = Id,
                Success = false,
                FinalState = WorkflowExecutionState.Failed,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                CompletedActions = completedActions,
                Errors = errors
            };
        }
        finally
        {
            _cancellationSource?.Dispose();
        }
    }

    private async Task<ActionResult> ExecuteActionWithRetryAsync(IWorkflowAction action, WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        var retryPolicy = action.RetryPolicy;
        var maxRetries = retryPolicy?.MaxRetries ?? 1;
        var currentDelay = retryPolicy?.InitialDelay ?? TimeSpan.Zero;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await action.ExecuteAsync(context, cancellationToken);
                if (result.Success) return result;

                // Check if error is retryable
                if (retryPolicy?.RetryableErrorPatterns.Count > 0)
                {
                    var isRetryable = retryPolicy.RetryableErrorPatterns.Any(pattern =>
                        result.ErrorMessage?.Contains(pattern, StringComparison.OrdinalIgnoreCase) ?? false);

                    if (!isRetryable && attempt < maxRetries)
                        return result;
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(currentDelay, cancellationToken);
                    currentDelay = TimeSpan.FromMilliseconds(
                        Math.Min(
                            currentDelay.TotalMilliseconds * (retryPolicy?.BackoffMultiplier ?? 2),
                            retryPolicy?.MaxDelay.TotalMilliseconds ?? 30000
                        )
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    return new ActionResult
                    {
                        Success = false,
                        ErrorMessage = $"Action failed after {maxRetries} attempts",
                        Exception = ex
                    };
                }

                await Task.Delay(currentDelay, cancellationToken);
            }
        }

        return new ActionResult { Success = false, ErrorMessage = "Max retries exceeded" };
    }

    public async Task PauseAsync()
    {
        if (ExecutionState == WorkflowExecutionState.Running)
        {
            _pauseTcs = new TaskCompletionSource<bool>();
            await _pauseTcs.Task;
        }
    }

    public Task ResumeAsync()
    {
        _pauseTcs?.TrySetResult(true);
        return Task.CompletedTask;
    }

    public async Task CancelAsync()
    {
        ExecutionState = WorkflowExecutionState.Cancelled;
        _cancellationSource?.Cancel();
        await Task.Delay(100); // Allow cancellation to propagate
    }

    public async Task UndoAsync()
    {
        if (_undoStack.Count > 0)
        {
            var history = _undoStack.Pop();
            _redoStack.Push(history);
            // Actual undo logic would revert action effects
            await Task.CompletedTask;
        }
    }

    public async Task RedoAsync()
    {
        if (_redoStack.Count > 0)
        {
            var history = _redoStack.Pop();
            _undoStack.Push(history);
            // Actual redo logic would re-execute action
            await Task.CompletedTask;
        }
    }

    public IReadOnlyList<WorkflowActionHistory> GetExecutionHistory()
    {
        return _undoStack.ToList().AsReadOnly();
    }
}
