namespace HELIOS.Platform.Core.ActionFlow;

/// <summary>
/// Represents a workflow - a collection of actions that can be executed in sequence, parallel, or conditional patterns.
/// </summary>
public interface IWorkflow
{
    /// <summary>Gets the unique identifier for this workflow.</summary>
    Guid Id { get; }

    /// <summary>Gets the display name of the workflow.</summary>
    string Name { get; }

    /// <summary>Gets the description of what this workflow does.</summary>
    string Description { get; }

    /// <summary>Gets the version of this workflow.</summary>
    Version Version { get; }

    /// <summary>Gets the workflow category (e.g., "Deployment", "Maintenance", "Optimization").</summary>
    string Category { get; }

    /// <summary>Gets the list of actions that comprise this workflow.</summary>
    IReadOnlyList<IWorkflowAction> Actions { get; }

    /// <summary>Gets the current execution state of the workflow.</summary>
    WorkflowExecutionState ExecutionState { get; }

    /// <summary>Gets the progress of workflow execution (0-100).</summary>
    int Progress { get; }

    /// <summary>Executes the workflow with optional parameters.</summary>
    Task<WorkflowResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Pauses the currently running workflow.</summary>
    Task PauseAsync();

    /// <summary>Resumes a paused workflow.</summary>
    Task ResumeAsync();

    /// <summary>Cancels the currently running workflow.</summary>
    Task CancelAsync();

    /// <summary>Reverts the workflow to undo the last executed action.</summary>
    Task UndoAsync();

    /// <summary>Re-executes the last undone action.</summary>
    Task RedoAsync();

    /// <summary>Gets the history of executed actions.</summary>
    IReadOnlyList<WorkflowActionHistory> GetExecutionHistory();
}

/// <summary>Represents a single action within a workflow.</summary>
public interface IWorkflowAction
{
    /// <summary>Gets the unique identifier for this action.</summary>
    Guid Id { get; }

    /// <summary>Gets the display name of the action.</summary>
    string Name { get; }

    /// <summary>Gets the description of what this action does.</summary>
    string Description { get; }

    /// <summary>Gets the type of action (e.g., "PowerShell", "REST", "Database", "Custom").</summary>
    string ActionType { get; }

    /// <summary>Gets the input parameters required for this action.</summary>
    IReadOnlyDictionary<string, ActionParameter> Parameters { get; }

    /// <summary>Gets the conditions that must be met before this action executes.</summary>
    IReadOnlyList<IActionCondition> Conditions { get; }

    /// <summary>Gets the ID of the next action to execute (for sequential flows).</summary>
    Guid? NextActionId { get; }

    /// <summary>Gets the IDs of parallel actions to execute simultaneously.</summary>
    IReadOnlyList<Guid> ParallelActionIds { get; }

    /// <summary>Gets the retry policy for this action.</summary>
    RetryPolicy? RetryPolicy { get; }

    /// <summary>Executes this action within the given context.</summary>
    Task<ActionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Validates that all required parameters are provided.</summary>
    ValidationResult Validate();
}

/// <summary>Represents a condition that must be true for an action to execute.</summary>
public interface IActionCondition
{
    /// <summary>Gets the type of condition (e.g., "Equals", "NotEquals", "GreaterThan", "Contains", "Custom").</summary>
    string ConditionType { get; }

    /// <summary>Gets the left-hand side value or variable reference.</summary>
    string LeftOperand { get; }

    /// <summary>Gets the right-hand side value or variable reference.</summary>
    string RightOperand { get; }

    /// <summary>Evaluates the condition given the current workflow context.</summary>
    bool Evaluate(WorkflowExecutionContext context);
}

/// <summary>Represents the execution context for a workflow - includes variables, history, etc.</summary>
public class WorkflowExecutionContext
{
    public required Guid WorkflowId { get; init; }
    public required Guid ExecutionId { get; init; }
    public required DateTime StartTime { get; init; }
    public Dictionary<string, object?> Variables { get; } = new();
    public List<ActionExecutionRecord> ExecutionRecords { get; } = new();
    public List<ActionExecutionRecord> UndoStack { get; } = new();
    public List<ActionExecutionRecord> RedoStack { get; } = new();
    public CancellationToken CancellationToken { get; set; }
    public WorkflowExecutionState State { get; set; } = WorkflowExecutionState.Pending;

    public T? GetVariable<T>(string name, T? defaultValue = default)
    {
        if (Variables.TryGetValue(name, out var value))
            return (T?)value;
        return defaultValue;
    }

    public void SetVariable(string name, object? value)
    {
        Variables[name] = value;
    }
}

/// <summary>Represents an action parameter with validation and default values.</summary>
public class ActionParameter
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public object? DefaultValue { get; init; }
    public string? Description { get; init; }
    public List<object>? AllowedValues { get; init; }
}

/// <summary>Result of executing a workflow action.</summary>
public class ActionResult
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public object? Data { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
}

/// <summary>Result of executing a complete workflow.</summary>
public class WorkflowResult
{
    public required Guid ExecutionId { get; init; }
    public required Guid WorkflowId { get; init; }
    public required bool Success { get; init; }
    public required WorkflowExecutionState FinalState { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan TotalExecutionTime => EndTime - StartTime;
    public List<ActionExecutionRecord> CompletedActions { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public Dictionary<string, object?>? FinalVariables { get; init; }

    public int SuccessfulActionCount => CompletedActions.Count(a => a.Success);
    public int FailedActionCount => CompletedActions.Count(a => !a.Success);
}

/// <summary>Record of an action execution for history/audit purposes.</summary>
public class ActionExecutionRecord
{
    public required Guid ActionId { get; init; }
    public required string ActionName { get; init; }
    public required DateTime ExecutionTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object?>? InputParameters { get; init; }
    public object? Output { get; init; }
}

/// <summary>History entry for a workflow action execution.</summary>
public class WorkflowActionHistory
{
    public required Guid ActionId { get; init; }
    public required string ActionName { get; init; }
    public required DateTime ExecutedAt { get; init; }
    public required bool Successful { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public TimeSpan ExecutionDuration { get; init; }
}

/// <summary>Policy for retrying failed actions.</summary>
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public List<string> RetryableErrorPatterns { get; set; } = new();
}

/// <summary>Result of validating a workflow or action.</summary>
public class ValidationResult
{
    public required bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

/// <summary>State of a workflow execution.</summary>
public enum WorkflowExecutionState
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled,
    Unknown
}
