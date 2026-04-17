using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HELIOS.Platform.Core.ActionFlow.Models
{
    /// <summary>
    /// Represents a unique identifier for actions, workflows, pages, and projects
    /// </summary>
    public readonly struct ActionFlowId : IEquatable<ActionFlowId>
    {
        private readonly string _value;

        public ActionFlowId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ActionFlowId cannot be null or empty", nameof(value));
            _value = value;
        }

        public static ActionFlowId New() => new(Guid.NewGuid().ToString());
        public override string ToString() => _value;
        public override bool Equals(object? obj) => obj is ActionFlowId id && Equals(id);
        public bool Equals(ActionFlowId other) => _value == other._value;
        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(ActionFlowId left, ActionFlowId right) => left.Equals(right);
        public static bool operator !=(ActionFlowId left, ActionFlowId right) => !left.Equals(right);
    }

    /// <summary>
    /// Represents the state of a workflow or action
    /// </summary>
    public enum WorkflowState
    {
        Idle,
        Created,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled,
        WaitingForInput,
        AwaitingDependency
    }

    /// <summary>
    /// Represents the status of a page within a project
    /// </summary>
    public enum PageStatus
    {
        Draft,
        Active,
        Archived,
        Published,
        Locked
    }

    /// <summary>
    /// Represents the type of action or step in a workflow
    /// </summary>
    public enum ActionType
    {
        Simple,
        Conditional,
        Loop,
        Parallel,
        Sequence,
        Custom,
        Template,
        Composite
    }

    /// <summary>
    /// Represents a constraint or condition on data flow
    /// </summary>
    public interface IActionConstraint
    {
        ActionFlowId Id { get; }
        string Name { get; }
        bool Validate(ActionContext context);
        string GetErrorMessage();
    }

    /// <summary>
    /// Represents the execution context for an action
    /// </summary>
    public class ActionContext
    {
        public ActionFlowId ActionId { get; set; }
        public ActionFlowId WorkflowId { get; set; }
        public ActionFlowId PageId { get; set; }
        public ActionFlowId ProjectId { get; set; }

        public Dictionary<string, object> Variables { get; set; } = new();
        public Dictionary<string, object> Output { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public WorkflowState CurrentState { get; set; } = WorkflowState.Idle;
        public Exception? Error { get; set; }

        public Dictionary<string, object> GetAllData() =>
            new(Variables.Concat(Output).Concat(Metadata).ToDictionary(x => x.Key, x => x.Value));

        public void SetVariable(string key, object value) => Variables[key] = value;
        public object? GetVariable(string key) => Variables.TryGetValue(key, out var value) ? value : null;
        public T? GetVariable<T>(string key) => GetVariable(key) as T;
    }

    /// <summary>
    /// Represents a data transformation rule
    /// </summary>
    public interface IDataTransformer
    {
        ActionFlowId Id { get; }
        string Name { get; }
        object Transform(object? input);
        Type GetInputType();
        Type GetOutputType();
    }

    /// <summary>
    /// Represents a page configuration with layout and navigation
    /// </summary>
    public class PageConfiguration
    {
        public ActionFlowId Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PageStatus Status { get; set; } = PageStatus.Draft;

        public Dictionary<string, object> LayoutData { get; set; } = new();
        public List<PageElement> Elements { get; set; } = new();
        public Dictionary<string, PageNavigation> NavigationRules { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        public string Serialize() => System.Text.Json.JsonSerializer.Serialize(this);
        public static PageConfiguration? Deserialize(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<PageConfiguration>(json);
    }

    /// <summary>
    /// Represents an element on a page
    /// </summary>
    public class PageElement
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string ElementType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, object> BindingRules { get; set; } = new();
        public List<PageElement> Children { get; set; } = new();
        public int ZIndex { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Represents navigation rules between pages
    /// </summary>
    public class PageNavigation
    {
        public ActionFlowId TargetPageId { get; set; }
        public string TriggerEvent { get; set; } = string.Empty;
        public Dictionary<string, object> Conditions { get; set; } = new();
        public Dictionary<string, object> DataMapping { get; set; } = new();
        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// Represents an action step in a workflow
    /// </summary>
    public class ActionStep
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ActionType ActionType { get; set; } = ActionType.Simple;

        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> InputParameters { get; set; } = new();
        public List<string> OutputParameters { get; set; } = new();

        public List<ActionFlowId> DependsOnActionIds { get; set; } = new();
        public List<ActionConstraint> Constraints { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Represents a constraint on an action
    /// </summary>
    public class ActionConstraint
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string ConstraintType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string ErrorMessage { get; set; } = "Constraint violation";
        public bool IsCritical { get; set; } = true;
    }

    /// <summary>
    /// Represents a complete workflow definition
    /// </summary>
    public class WorkflowDefinition
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Version { get; set; } = 1;

        public List<ActionStep> Steps { get; set; } = new();
        public List<WorkflowTransition> Transitions { get; set; } = new();
        public Dictionary<string, object> GlobalVariables { get; set; } = new();

        public ActionFlowId? InitialStepId { get; set; }
        public List<ActionFlowId> TerminalStepIds { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        public string Serialize() => System.Text.Json.JsonSerializer.Serialize(this);
        public static WorkflowDefinition? Deserialize(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<WorkflowDefinition>(json);
    }

    /// <summary>
    /// Represents a transition between workflow states
    /// </summary>
    public class WorkflowTransition
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public ActionFlowId FromStepId { get; set; }
        public ActionFlowId ToStepId { get; set; }
        public string TransitionCondition { get; set; } = string.Empty;
        public List<ActionConstraint> Constraints { get; set; } = new();
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Represents a project containing multiple pages and workflows
    /// </summary>
    public class ProjectDefinition
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<PageConfiguration> Pages { get; set; } = new();
        public List<WorkflowDefinition> Workflows { get; set; } = new();
        public Dictionary<string, object> GlobalConfig { get; set; } = new();

        public ActionFlowId? InitialPageId { get; set; }
        public Dictionary<string, ActionFlowId> PageLookup { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        public string Serialize() => System.Text.Json.JsonSerializer.Serialize(this);
        public static ProjectDefinition? Deserialize(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<ProjectDefinition>(json);
    }

    /// <summary>
    /// Represents an execution instance of a workflow
    /// </summary>
    public class WorkflowExecutionInstance
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public ActionFlowId WorkflowDefinitionId { get; set; }
        public ActionFlowId ProjectId { get; set; }

        public WorkflowState CurrentState { get; set; } = WorkflowState.Created;
        public ActionFlowId? CurrentStepId { get; set; }

        public Dictionary<ActionFlowId, ActionExecutionResult> StepResults { get; set; } = new();
        public ActionContext ExecutionContext { get; set; } = new();

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }

        public List<string> ExecutionHistory { get; set; } = new();
        public Exception? Error { get; set; }
    }

    /// <summary>
    /// Represents the result of executing an action
    /// </summary>
    public class ActionExecutionResult
    {
        public ActionFlowId ActionId { get; set; }
        public WorkflowState State { get; set; } = WorkflowState.Idle;
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Output { get; set; } = new();
        public Exception? Error { get; set; }
        public int RetryCount { get; set; } = 0;
        public bool Success => Error == null && State == WorkflowState.Completed;
    }
}
