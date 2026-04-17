# HELIOS Platform Action Flow System - Complete Implementation Guide

## Overview

The Action Flow System is a comprehensive framework for building, managing, and executing complex workflows and page navigation systems within the HELIOS Platform. It provides production-ready components for state management, action execution, undo/redo, auto-save, templates, and monitoring.

## Architecture

The system consists of 12 core components:

### 1. Core Types & Models (`Models/ActionFlowTypes.cs`)
- **ActionFlowId**: Unique identifier for all entities
- **WorkflowState**: State machine states (Idle, Created, Running, Paused, Completed, Failed, Cancelled, WaitingForInput, AwaitingDependency)
- **ActionContext**: Execution context containing variables, output, and metadata
- **PageConfiguration**: Page definition with layout, elements, and navigation rules
- **WorkflowDefinition**: Complete workflow with steps and transitions
- **ProjectDefinition**: Project containing pages and workflows
- **WorkflowExecutionInstance**: Runtime instance of executing workflow

### 2. State Management (`StateManagement/StateStore.cs`)
Redux-like state management with immutable state updates.

**Key Features:**
- Centralized state store
- Action dispatching
- Subscription-based notifications
- Middleware support
- Thread-safe operations

**Usage:**
```csharp
var store = new StateStore();

// Subscribe to changes
store.Subscribe((newState, previousState, action) =>
{
    Console.WriteLine($"State changed by action: {action.Type}");
});

// Dispatch actions
store.Dispatch(new AddWorkflowAction(workflow));

// Get current state
var state = store.CurrentState;
var workflows = state.Workflows;
```

### 3. Workflow State Machine (`StateManagement/WorkflowStateMachine.cs`)
Implements state machine pattern for workflow execution.

**Key Features:**
- 9 predefined states with transitions
- Custom state handlers
- Transition rules with conditions
- State change listeners
- Thread-safe state transitions

**Usage:**
```csharp
var stateMachine = new WorkflowStateMachine();

// Add transition rule
stateMachine.AddTransitionRule(new StateTransitionRule
{
    FromState = WorkflowState.Created,
    ToState = WorkflowState.Running,
    Condition = ctx => ctx.Variables.Count > 0,
    OnTransition = async ctx => await UpdateContextAsync(ctx)
});

// Transition state
await stateMachine.TransitionAsync(WorkflowState.Running, context);

// Get available transitions
var available = stateMachine.GetAvailableTransitions();
```

### 4. Action Engine (`Engines/ActionEngine.cs`)
Executes actions and workflows with retry logic and error handling.

**Key Features:**
- Pluggable action handlers (Simple, Conditional, Loop, Parallel)
- Retry logic with exponential backoff
- Timeout management
- Workflow execution orchestration
- Step dependency resolution

**Usage:**
```csharp
var engine = new ActionEngine(stateMachine, stateStore);

// Execute single action
var result = await engine.ExecuteActionAsync(step, context);

// Execute complete workflow
var instance = await engine.ExecuteWorkflowAsync(
    workflow, 
    projectId,
    new Dictionary<string, object> { { "key", "value" } });

// Control workflow
await engine.PauseWorkflowAsync(instance);
await engine.ResumeWorkflowAsync(instance);
await engine.CancelWorkflowAsync(instance);
```

### 5. Page Router (`PageRouter/PageRouter.cs`)
Manages page navigation and routing within projects.

**Key Features:**
- Route registration and lookup
- Navigation history tracking
- Navigation guards
- History back/forward navigation
- Project-based routing

**Usage:**
```csharp
var router = new PageRouter();

// Register routes
router.RegisterRoute(new PageRoute
{
    Path = "/dashboard",
    PageId = pageId,
    PageName = "Dashboard"
});

// Navigate
await router.NavigateToAsync("/dashboard", new { filter = "active" });

// Go back
await router.GoBackAsync();

// Subscribe to navigation
router.SubscribeToNavigation((from, to) =>
{
    Console.WriteLine($"Navigated from {from} to {to}");
    return Task.CompletedTask;
});
```

### 6. State Management with Undo/Redo (`UndoRedo/UndoRedoStack.cs`)
Complete undo/redo functionality with operation history.

**Key Features:**
- Unlimited undo/redo stacks (configurable)
- Operation combining for related changes
- History browsing
- Thread-safe operations
- Full serialization support

**Usage:**
```csharp
var undoStack = new UndoRedoStack(maxStackSize: 100);

// Record operation
undoStack.RecordOperation(new AddWorkflowOperation(stateStore, workflow));

// Undo/Redo
if (undoStack.Undo())
{
    Console.WriteLine("Undone!");
}

if (undoStack.Redo())
{
    Console.WriteLine("Redone!");
}

// Get history
var history = undoStack.GetUndoHistory();

// Subscribe to changes
undoStack.Subscribe(state =>
{
    Console.WriteLine($"Can undo: {state.CanUndo}, Can redo: {state.CanRedo}");
});
```

### 7. Auto-Save with Conflict Resolution (`AutoSave/AutoSaveManager.cs`)
Automatic saving with intelligent conflict detection and resolution.

**Key Features:**
- Configurable auto-save intervals
- Conflict detection and resolution
- Multiple resolution strategies (Local, Remote, Merge, Manual)
- Checkpoint-based persistence
- Event notifications

**Usage:**
```csharp
var persistence = new InMemoryAutoSavePersistence(); // Implement IAutoSavePersistence
var autoSave = new AutoSaveManager(stateStore, persistence);

// Configure conflict resolver
autoSave.RegisterConflictResolver(async conflicts =>
{
    return ConflictResolutionStrategy.Merge;
});

// Start auto-save (every 30 seconds)
autoSave.StartAutoSave(intervalSeconds: 30);

// Manual save
var checkpoint = await autoSave.SaveAsync(detectConflicts: true, savedBy: "user1");

// Subscribe to events
autoSave.SaveCompleted += (s, checkpoint) =>
{
    Console.WriteLine($"Saved at {checkpoint.SavedAt}");
};
```

### 8. Template System (`Templates/TemplateManager.cs`)
Create reusable templates for pages, workflows, and projects.

**Key Features:**
- Page templates (blank, form, dashboard, list)
- Workflow templates (simple, approval, parallel)
- Project templates
- Template categorization
- Deep cloning for instances

**Usage:**
```csharp
var templateManager = new TemplateManager();

// Register built-in templates
templateManager.RegisterPageTemplate(BuiltInPageTemplates.CreateFormTemplate());
templateManager.RegisterWorkflowTemplate(BuiltInWorkflowTemplates.CreateApprovalWorkflow());

// Create from template
var page = templateManager.CreatePageFromTemplate(templateId);
var workflow = templateManager.CreateWorkflowFromTemplate(templateId);

// Save as template
var template = templateManager.SavePageAsTemplate(page, "Dashboard", "user1");

// Get templates by category
var dashboards = templateManager.GetPageTemplatesByCategory("Dashboard");
```

### 9. Drag-and-Drop (`DragDrop/DragDropEngine.cs`)
Visual drag-and-drop support for workflow and page building.

**Key Features:**
- Drop target registration
- Drop validators
- Drag operation tracking
- Workflow builder with drag-drop
- Page builder with drag-drop

**Usage:**
```csharp
// Workflow builder
var workflowBuilder = new WorkflowDragDropBuilder();

workflowBuilder.AddStep(step1);
workflowBuilder.AddStep(step2);
workflowBuilder.ConnectSteps(step1.Id, step2.Id, "condition");

var workflow = workflowBuilder.Build("My Workflow", "Description");

// Page builder
var pageBuilder = new PageDragDropBuilder();

pageBuilder.AddElement(element1);
pageBuilder.AddElement(element2);
pageBuilder.ReorderElements(element1.Id, 1);

var page = pageBuilder.Build("My Page", "Description");
```

### 10. Monitoring and Analytics (`Monitoring/MonitoringEngine.cs`)
Comprehensive monitoring, metrics, and health checks.

**Key Features:**
- Performance metric recording
- Event logging and tracking
- Execution statistics
- Health check engine
- Real-time event/metric listeners

**Usage:**
```csharp
var monitoring = new MonitoringEngine();

// Record metrics
monitoring.RecordMetric("api.response.time", 125.5, "ms");

// Record events
monitoring.RecordEvent("WorkflowStarted", "Workflow started", "Info");

// Workflow execution tracking
monitoring.RecordWorkflowStart(workflowId);
monitoring.RecordWorkflowCompleted(workflowId, duration, success: true);

// Get statistics
var stats = monitoring.GetWorkflowStatistics(workflowId);
Console.WriteLine($"Success rate: {stats.SuccessRate}%");

// Get events/metrics
var errors = monitoring.GetErrorEvents();
var avgDuration = monitoring.GetAverageMetric("action.execution.duration");

// Health checks
var healthEngine = new HealthCheckEngine(monitoring);
healthEngine.RegisterHealthCheck("database", () => CheckDatabase());

var health = healthEngine.PerformHealthChecks();
```

## Complete Example: Building a Project

```csharp
using HELIOS.Platform.Core.ActionFlow.Models;
using HELIOS.Platform.Core.ActionFlow.StateManagement;
using HELIOS.Platform.Core.ActionFlow.Engines;
using HELIOS.Platform.Core.ActionFlow.Templates;
using HELIOS.Platform.Core.ActionFlow.UndoRedo;
using HELIOS.Platform.Core.ActionFlow.AutoSave;

public class ActionFlowApplicationService
{
    private readonly StateStore _stateStore;
    private readonly ActionEngine _actionEngine;
    private readonly WorkflowStateMachine _stateMachine;
    private readonly UndoRedoStack _undoStack;
    private readonly AutoSaveManager _autoSaveManager;
    private readonly TemplateManager _templateManager;

    public ActionFlowApplicationService()
    {
        _stateStore = new StateStore();
        _stateMachine = new WorkflowStateMachine();
        _actionEngine = new ActionEngine(_stateMachine, _stateStore);
        _undoStack = new UndoRedoStack();
        
        var persistence = new InMemoryAutoSavePersistence();
        _autoSaveManager = new AutoSaveManager(_stateStore, persistence);
        _templateManager = new TemplateManager();
        
        InitializeTemplates();
    }

    private void InitializeTemplates()
    {
        _templateManager.RegisterPageTemplate(BuiltInPageTemplates.CreateDashboardTemplate());
        _templateManager.RegisterWorkflowTemplate(BuiltInWorkflowTemplates.CreateApprovalWorkflow());
    }

    public async Task CreateAndExecuteProjectAsync()
    {
        // Create project from template
        var pageTemplate = _templateManager.GetAllPageTemplates().First();
        var projectPage = _templateManager.CreatePageFromTemplate(pageTemplate.Id);
        
        var workflowTemplate = _templateManager.GetAllWorkflowTemplates().First();
        var projectWorkflow = _templateManager.CreateWorkflowFromTemplate(workflowTemplate.Id);

        var project = new ProjectDefinition
        {
            Name = "My Project",
            Pages = new List<PageConfiguration> { projectPage },
            Workflows = new List<WorkflowDefinition> { projectWorkflow }
        };

        // Add to state
        _stateStore.Dispatch(new AddProjectAction(project));

        // Record undo operation
        var addProjectOp = new AddProjectOperation(_stateStore, project);
        _undoStack.RecordOperation(addProjectOp);

        // Start auto-save
        _autoSaveManager.StartAutoSave(intervalSeconds: 30);

        // Execute workflow
        var instance = await _actionEngine.ExecuteWorkflowAsync(
            projectWorkflow,
            project.Id,
            new() { { "initiator", "system" } });

        Console.WriteLine($"Workflow completed: {instance.CurrentState}");

        // Get statistics
        var stats = _actionEngine.GetExecutionStats(projectWorkflow.Id);
        Console.WriteLine($"Executions: {stats.TotalExecutions}, Success Rate: {stats.SuccessRate}%");

        // Cleanup
        _autoSaveManager.Dispose();
    }
}

// Auto-save persistence implementation
public class InMemoryAutoSavePersistence : IAutoSavePersistence
{
    private readonly Dictionary<ActionFlowId, SaveCheckpoint> _checkpoints = new();

    public Task SaveCheckpointAsync(SaveCheckpoint checkpoint)
    {
        _checkpoints[checkpoint.Id] = checkpoint;
        return Task.CompletedTask;
    }

    public Task<SaveCheckpoint?> LoadCheckpointAsync(ActionFlowId id)
    {
        _checkpoints.TryGetValue(id, out var checkpoint);
        return Task.FromResult(checkpoint);
    }

    public Task<List<SaveCheckpoint>> GetCheckpointsAsync(int limit = 10)
    {
        return Task.FromResult(_checkpoints.Values.TakeLast(limit).ToList());
    }

    public Task DeleteCheckpointAsync(ActionFlowId id)
    {
        _checkpoints.Remove(id);
        return Task.CompletedTask;
    }
}

// Add these missing action classes to StateManagement/StateStore.cs

public class AddProjectAction : ActionFlowActionBase
{
    private readonly ProjectDefinition _project;

    public override string Type => ProjectActions.ADD_PROJECT;
    public override Dictionary<string, object> Payload => new() { { "project", _project } };

    public AddProjectAction(ProjectDefinition project)
    {
        _project = project;
    }
}

public class AddProjectOperation : UndoableOperationBase
{
    private readonly StateStore _stateStore;
    private readonly ProjectDefinition _project;

    public override string OperationName => $"Add Project: {_project.Name}";

    public AddProjectOperation(StateStore stateStore, ProjectDefinition project)
    {
        _stateStore = stateStore;
        _project = project;
    }

    public override void Execute()
    {
        _stateStore.Dispatch(new AddProjectAction(_project));
    }

    public override void Undo()
    {
        _stateStore.Dispatch(new DeleteProjectAction(_project.Id));
    }

    public override void Redo()
    {
        Execute();
    }
}

public class DeleteProjectAction : ActionFlowActionBase
{
    private readonly ActionFlowId _projectId;

    public override string Type => ProjectActions.DELETE_PROJECT;
    public override Dictionary<string, object> Payload => new() { { "projectId", _projectId } };

    public DeleteProjectAction(ActionFlowId projectId)
    {
        _projectId = projectId;
    }
}
```

## Best Practices

1. **State Management**
   - Always use the StateStore for state changes
   - Never modify state directly
   - Use actions for all state mutations

2. **Error Handling**
   - Always handle ActionFlowEvent errors
   - Use monitoring for error tracking
   - Implement proper retry logic

3. **Performance**
   - Use auto-save to prevent data loss
   - Implement auto-save persistence
   - Monitor execution metrics

4. **Workflows**
   - Define clear step dependencies
   - Use templates for reusable patterns
   - Test workflows before production

5. **Pages**
   - Keep element hierarchies shallow
   - Use templates for consistency
   - Implement proper navigation guards

## Thread Safety

All components are fully thread-safe with internal locking mechanisms. Multiple threads can safely:
- Dispatch actions to the state store
- Modify undo/redo stacks
- Navigate pages
- Execute workflows
- Record metrics

## Integration with Existing Systems

The Action Flow System integrates seamlessly with existing HELIOS services:
- Use dependency injection to provide implementations
- Implement IAutoSavePersistence for your storage backend
- Connect to your event system through listeners
- Extend action handlers for custom logic

## Performance Characteristics

- **State Store Dispatch**: O(1) for most actions
- **Workflow Execution**: O(n) where n is number of steps
- **Undo/Redo**: O(1) pop/push, O(n) for history traversal
- **History Size**: Configurable, default 10,000 events, 50,000 metrics

## Future Enhancements

1. Distributed workflow execution
2. Workflow versioning and rollback
3. Advanced analytics and reporting
4. Custom action handler marketplace
5. Workflow performance optimization
6. Multi-project workflows
7. Workflow simulation and testing
8. Event streaming integration

## Support and Documentation

For detailed API documentation, see individual class files. Each class includes:
- XML documentation
- Usage examples
- Error handling patterns
- Performance notes

