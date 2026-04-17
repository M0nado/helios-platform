namespace HELIOS.Platform.Core.ActionFlow;

/// <summary>
/// Service for managing and executing workflows.
/// </summary>
public interface IWorkflowService
{
    /// <summary>Creates a new workflow.</summary>
    IWorkflow CreateWorkflow(string name, string description, string category);

    /// <summary>Gets a workflow by ID.</summary>
    IWorkflow? GetWorkflow(Guid workflowId);

    /// <summary>Lists all available workflows.</summary>
    IEnumerable<IWorkflow> GetAllWorkflows();

    /// <summary>Lists workflows by category.</summary>
    IEnumerable<IWorkflow> GetWorkflowsByCategory(string category);

    /// <summary>Saves a workflow for later use.</summary>
    Task SaveWorkflowAsync(IWorkflow workflow);

    /// <summary>Loads a workflow from storage.</summary>
    Task<IWorkflow?> LoadWorkflowAsync(Guid workflowId);

    /// <summary>Executes a workflow and returns the result.</summary>
    Task<WorkflowResult> ExecuteWorkflowAsync(Guid workflowId, WorkflowExecutionContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>Gets the execution history for a workflow.</summary>
    IEnumerable<WorkflowExecutionHistory> GetExecutionHistory(Guid workflowId);

    /// <summary>Gets all currently running workflows.</summary>
    IEnumerable<(Guid WorkflowId, WorkflowExecutionState State, int Progress)> GetRunningWorkflows();

    /// <summary>Deletes a workflow.</summary>
    Task DeleteWorkflowAsync(Guid workflowId);
}

/// <summary>
/// Default implementation of IWorkflowService.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly Dictionary<Guid, IWorkflow> _workflows = new();
    private readonly Dictionary<Guid, List<WorkflowExecutionHistory>> _executionHistory = new();
    private readonly Dictionary<Guid, (IWorkflow Workflow, WorkflowExecutionState State, int Progress)> _runningWorkflows = new();

    public IWorkflow CreateWorkflow(string name, string description, string category)
    {
        var workflow = new Workflow(name, description, category);
        _workflows[workflow.Id] = workflow;
        _executionHistory[workflow.Id] = new List<WorkflowExecutionHistory>();
        return workflow;
    }

    public IWorkflow? GetWorkflow(Guid workflowId)
    {
        _workflows.TryGetValue(workflowId, out var workflow);
        return workflow;
    }

    public IEnumerable<IWorkflow> GetAllWorkflows() => _workflows.Values;

    public IEnumerable<IWorkflow> GetWorkflowsByCategory(string category)
    {
        return _workflows.Values.Where(w => w.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveWorkflowAsync(IWorkflow workflow)
    {
        _workflows[workflow.Id] = workflow;
        await Task.CompletedTask;
    }

    public async Task<IWorkflow?> LoadWorkflowAsync(Guid workflowId)
    {
        return await Task.FromResult(_workflows.TryGetValue(workflowId, out var workflow) ? workflow : null);
    }

    public async Task<WorkflowResult> ExecuteWorkflowAsync(Guid workflowId, WorkflowExecutionContext? context = null, CancellationToken cancellationToken = default)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new KeyNotFoundException($"Workflow {workflowId} not found");

        var execContext = context ?? new WorkflowExecutionContext
        {
            WorkflowId = workflowId,
            ExecutionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        try
        {
            _runningWorkflows[workflowId] = (workflow, WorkflowExecutionState.Running, 0);
            var result = await workflow.ExecuteAsync(execContext, cancellationToken);

            // Store execution history
            if (_executionHistory.TryGetValue(workflowId, out var history))
            {
                history.Add(new WorkflowExecutionHistory
                {
                    ExecutionId = result.ExecutionId,
                    ExecutedAt = DateTime.UtcNow,
                    Duration = result.TotalExecutionTime,
                    Successful = result.Success,
                    ActionsCompleted = result.CompletedActions.Count,
                    ErrorCount = result.Errors.Count,
                    Errors = string.Join("; ", result.Errors)
                });
            }

            return result;
        }
        finally
        {
            _runningWorkflows.Remove(workflowId);
        }
    }

    public IEnumerable<WorkflowExecutionHistory> GetExecutionHistory(Guid workflowId)
    {
        return _executionHistory.TryGetValue(workflowId, out var history)
            ? history
            : Enumerable.Empty<WorkflowExecutionHistory>();
    }

    public IEnumerable<(Guid WorkflowId, WorkflowExecutionState State, int Progress)> GetRunningWorkflows()
    {
        return _runningWorkflows.Select(x => (x.Key, x.Value.State, x.Value.Progress));
    }

    public async Task DeleteWorkflowAsync(Guid workflowId)
    {
        _workflows.Remove(workflowId);
        _executionHistory.Remove(workflowId);
        _runningWorkflows.Remove(workflowId);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Represents the history of a workflow execution.
/// </summary>
public class WorkflowExecutionHistory
{
    public required Guid ExecutionId { get; init; }
    public required DateTime ExecutedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool Successful { get; init; }
    public int ActionsCompleted { get; init; }
    public int ErrorCount { get; init; }
    public string? Errors { get; init; }
}

/// <summary>
/// Simple condition implementation for comparing values.
/// </summary>
public class SimpleCondition : IActionCondition
{
    public string ConditionType { get; }
    public string LeftOperand { get; }
    public string RightOperand { get; }

    public SimpleCondition(string conditionType, string leftOperand, string rightOperand)
    {
        ConditionType = conditionType;
        LeftOperand = leftOperand;
        RightOperand = rightOperand;
    }

    public bool Evaluate(WorkflowExecutionContext context)
    {
        var left = ResolveValue(LeftOperand, context);
        var right = ResolveValue(RightOperand, context);

        return ConditionType.ToLowerInvariant() switch
        {
            "equals" => Equals(left, right),
            "notequals" => !Equals(left, right),
            "greaterthan" => Compare(left, right) > 0,
            "lessthan" => Compare(left, right) < 0,
            "contains" => left?.ToString()?.Contains(right?.ToString() ?? "") ?? false,
            "notcontains" => !(left?.ToString()?.Contains(right?.ToString() ?? "") ?? false),
            "isempty" => string.IsNullOrEmpty(left?.ToString()),
            "isnotempty" => !string.IsNullOrEmpty(left?.ToString()),
            _ => false
        };
    }

    private object? ResolveValue(string operand, WorkflowExecutionContext context)
    {
        // If operand starts with $, it's a variable reference
        if (operand.StartsWith("$"))
        {
            var variableName = operand.Substring(1);
            return context.Variables.TryGetValue(variableName, out var value) ? value : null;
        }

        // Check if it's a number
        if (int.TryParse(operand, out var intValue))
            return intValue;
        if (double.TryParse(operand, out var doubleValue))
            return doubleValue;

        // Otherwise it's a literal string
        return operand;
    }

    private int Compare(object? left, object? right)
    {
        if (left is IComparable leftComparable && right is IComparable rightComparable)
            return leftComparable.CompareTo(rightComparable);

        var leftStr = left?.ToString();
        var rightStr = right?.ToString();
        return string.Compare(leftStr, rightStr, StringComparison.Ordinal);
    }
}

/// <summary>
/// Predefined workflow templates for common use cases.
/// </summary>
public class WorkflowTemplates
{
    /// <summary>
    /// Creates a deployment workflow template.
    /// </summary>
    public static Workflow CreateDeploymentWorkflow()
    {
        var workflow = new Workflow("Deployment", "Deploy application to environment", "Deployment", new Version(1, 0));

        // Pre-deployment checks
        workflow.AddAction(new PowerShellAction(
            "Check Prerequisites",
            "Verify all prerequisites are installed",
            "# Check .NET, Docker, etc."));

        // Build application
        workflow.AddAction(new PowerShellAction(
            "Build Application",
            "Build the application",
            "dotnet build --configuration Release"));

        // Run tests
        workflow.AddAction(new PowerShellAction(
            "Run Tests",
            "Run unit tests",
            "dotnet test --configuration Release"));

        // Deploy
        workflow.AddAction(new PowerShellAction(
            "Deploy",
            "Deploy to target environment",
            "dotnet publish --configuration Release"));

        return workflow;
    }

    /// <summary>
    /// Creates a maintenance workflow template.
    /// </summary>
    public static Workflow CreateMaintenanceWorkflow()
    {
        var workflow = new Workflow("Maintenance", "Perform system maintenance", "Maintenance", new Version(1, 0));

        // Cleanup
        workflow.AddAction(new PowerShellAction(
            "Clean Temporary Files",
            "Remove temporary files",
            "Remove-Item -Path $env:TEMP\\* -Recurse -Force -ErrorAction SilentlyContinue"));

        // Optimize
        workflow.AddAction(new PowerShellAction(
            "Optimize Disk",
            "Optimize disk space",
            "Optimize-Volume -DriveLetter C -Defrag"));

        // Update
        workflow.AddAction(new PowerShellAction(
            "Check Updates",
            "Check for system updates",
            "Get-WmiObject Win32_QuickFixEngineering"));

        return workflow;
    }

    /// <summary>
    /// Creates a backup workflow template.
    /// </summary>
    public static Workflow CreateBackupWorkflow()
    {
        var workflow = new Workflow("Backup", "Backup critical data", "Backup", new Version(1, 0));

        workflow.AddAction(new PowerShellAction(
            "Create Backup",
            "Create system backup",
            "$date = Get-Date -Format 'yyyyMMdd-HHmmss';\n" +
            "$backupPath = \"F:\\Backup-$date\";\n" +
            "New-Item -ItemType Directory -Path $backupPath -Force;\n" +
            "Copy-Item -Path 'C:\\Users\\*\\Documents' -Destination $backupPath -Recurse -Force"));

        workflow.AddAction(new PowerShellAction(
            "Verify Backup",
            "Verify backup integrity",
            "Get-ChildItem -Path 'F:\\Backup-*' -Recurse | Measure-Object"));

        return workflow;
    }
}
