# HELIOS Platform Action Flow System

A production-ready, comprehensive system for building, managing, executing, and monitoring complex workflows and page-based applications within the HELIOS Platform.

## Features

### ✅ Complete Implementation

- **12 Core Components** - Fully implemented and production-ready
- **State Management** - Redux-like immutable state with full serialization
- **Workflow Execution** - Complete workflow engine with state machine
- **Page Routing** - Advanced navigation and routing system
- **Undo/Redo** - Full history management with operation combining
- **Auto-Save** - Conflict-aware auto-save with resolution strategies
- **Templates** - Reusable templates for pages, workflows, and projects
- **Drag-and-Drop** - Visual builders for workflows and pages
- **Monitoring** - Comprehensive metrics, events, and health checks
- **Thread-Safety** - All components are fully thread-safe
- **Extensibility** - Plugin architecture for custom handlers

## Quick Start

### Installation

The ActionFlow system is located in:
```
src/HELIOS.Platform/Core/ActionFlow/
```

### Basic Usage

```csharp
using HELIOS.Platform.Core.ActionFlow.Services;

// Create service
var actionFlow = new ActionFlowService();

// Create a project
var project = await actionFlow.CreateProjectAsync("My Project", "Description");

// Create a workflow from template
var workflow = actionFlow.CreateWorkflowFromTemplate(
    actionFlow.GetWorkflowTemplates().First().Id);

// Add to project
project.Workflows.Add(workflow);
actionFlow.UpdateProject(project);

// Execute workflow
var execution = await actionFlow.ExecuteWorkflowAsync(
    workflow.Id,
    project.Id,
    new() { { "userId", "user123" } });

// Get statistics
var stats = actionFlow.GetWorkflowStatistics(workflow.Id);
Console.WriteLine($"Success Rate: {stats.SuccessRate}%");

// Auto-save
actionFlow.StartAutoSave(intervalSeconds: 30);
var checkpoint = await actionFlow.SaveAsync("user123");
```

## Architecture

### Component Structure

```
ActionFlow/
├── Models/                    # Core types and data models
├── StateManagement/           # Redux-like state store + state machine
├── Engines/                   # Action execution engine
├── PageRouter/                # Page routing and navigation
├── UndoRedo/                  # Undo/redo stack and operations
├── AutoSave/                  # Auto-save with conflict resolution
├── Templates/                 # Template system
├── DragDrop/                  # Drag-and-drop builders
├── Monitoring/                # Metrics and event tracking
├── Services/                  # Integration service
└── IMPLEMENTATION_GUIDE.md    # Detailed documentation
```

### State Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    ActionFlowService                         │
│                  (Orchestration Layer)                       │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  StateStore  │  │ Action Engine│  │ Page Router  │      │
│  │              │  │              │  │              │      │
│  │ - Immutable  │  │ - Execute    │  │ - Navigate   │      │
│  │ - Reducers   │  │ - Retry      │  │ - History    │      │
│  │ - Listeners  │  │ - Handlers   │  │ - Guards     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ WorkflowState│  │  Undo/Redo   │  │ Auto-Save    │      │
│  │  Machine     │  │              │  │              │      │
│  │              │  │ - Operations │  │ - Conflict   │      │
│  │ - States     │  │ - History    │  │ - Strategies │      │
│  │ - Transitions│  │ - Combining  │  │ - Checkpoint │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Templates   │  │ Drag-Drop    │  │ Monitoring   │      │
│  │              │  │              │  │              │      │
│  │ - Pages      │  │ - Workflows  │  │ - Metrics    │      │
│  │ - Workflows  │  │ - Pages      │  │ - Events     │      │
│  │ - Projects   │  │ - Builders   │  │ - Health     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

## Detailed Examples

### Creating a Project with Workflows

```csharp
var actionFlow = new ActionFlowService();

// Create project
var project = await actionFlow.CreateProjectAsync(
    "Invoice Processing",
    "Automated invoice approval workflow");

// Create pages from templates
var dashboardPage = actionFlow.CreatePageFromTemplate(
    actionFlow.GetPageTemplates()
        .First(p => p.Name == "Dashboard").Id);

var formPage = actionFlow.CreatePageFromTemplate(
    actionFlow.GetPageTemplates()
        .First(p => p.Name == "Form Page").Id);

project.Pages.Add(dashboardPage);
project.Pages.Add(formPage);

// Create approval workflow
var approvalWorkflow = actionFlow.CreateWorkflowFromTemplate(
    actionFlow.GetWorkflowTemplates()
        .First(w => w.Name == "Approval Workflow").Id);

project.Workflows.Add(approvalWorkflow);

// Setup navigation between pages
dashboardPage.NavigationRules["viewInvoice"] = new PageNavigation
{
    TargetPageId = formPage.Id,
    TriggerEvent = "invoiceSelected"
};

actionFlow.UpdatePage(dashboardPage);

// Execute the workflow
var execution = await actionFlow.ExecuteWorkflowAsync(
    approvalWorkflow.Id,
    project.Id,
    new()
    {
        { "invoiceAmount", 15000 },
        { "requestedBy", "user@example.com" },
        { "department", "accounting" }
    });

Console.WriteLine($"Workflow Status: {execution.CurrentState}");
```

### Building a Workflow Visually

```csharp
var builder = actionFlow.GetWorkflowBuilder();

// Add workflow steps
var step1 = new ActionStep 
{ 
    Name = "Validate Invoice",
    ActionType = ActionType.Simple
};

var step2 = new ActionStep 
{ 
    Name = "Check Budget",
    ActionType = ActionType.Conditional
};

var step3 = new ActionStep 
{ 
    Name = "Send for Approval",
    ActionType = ActionType.Simple
};

var step4 = new ActionStep 
{ 
    Name = "Process Payment",
    ActionType = ActionType.Simple
};

builder.AddStep(step1);
builder.AddStep(step2);
builder.AddStep(step3);
builder.AddStep(step4);

// Connect steps
builder.ConnectSteps(step1.Id, step2.Id);
builder.ConnectSteps(step2.Id, step3.Id, "amount > 10000");
builder.ConnectSteps(step3.Id, step4.Id);

// Build and save
var workflow = builder.Build("Invoice Approval", "Automated approval");
actionFlow.UpdateWorkflow(workflow);
```

### Building a Page Visually

```csharp
var pageBuilder = actionFlow.GetPageBuilder();

// Create elements
var titleElement = new PageElement
{
    ElementType = "text",
    Name = "Title",
    Properties = new() { { "content", "Invoice Dashboard" } }
};

var tableElement = new PageElement
{
    ElementType = "table",
    Name = "InvoiceList",
    Properties = new() { { "columns", 5 } }
};

var buttonElement = new PageElement
{
    ElementType = "button",
    Name = "ProcessButton",
    Properties = new() { { "label", "Process Selected" } },
    BindingRules = new() { { "onClick", "processInvoices" } }
};

pageBuilder.AddElement(titleElement);
pageBuilder.AddElement(tableElement);
pageBuilder.AddElement(buttonElement);

// Build page
var page = pageBuilder.Build("Invoice Dashboard", "Main dashboard");
actionFlow.UpdatePage(page);
```

### Handling Undo/Redo

```csharp
// Check if undo/redo is available
var undoState = actionFlow.GetUndoRedoState();
if (undoState.CanUndo)
{
    actionFlow.Undo();
}

if (undoState.CanRedo)
{
    actionFlow.Redo();
}

// Subscribe to undo/redo state changes
actionFlow.SubscribeToStateChanges((newState, previousState, action) =>
{
    var undoRedoState = actionFlow.GetUndoRedoState();
    Console.WriteLine($"Undo available: {undoRedoState.CanUndo}");
    Console.WriteLine($"Redo available: {undoRedoState.CanRedo}");
});
```

### Auto-Save with Conflict Resolution

```csharp
var persistence = new InMemoryAutoSavePersistence();
var actionFlow = new ActionFlowService(persistence);

// Configure conflict resolution
actionFlow._autoSaveManager.RegisterConflictResolver(
    async conflicts =>
    {
        Console.WriteLine($"Conflicts detected: {conflicts.Count}");
        foreach (var conflict in conflicts)
        {
            Console.WriteLine($"  - {conflict.ResourceType}: {conflict.ResourceId}");
        }
        
        // Auto-resolve by merging
        return ConflictResolutionStrategy.Merge;
    });

// Start auto-save every 30 seconds
actionFlow.StartAutoSave(30);

// Subscribe to save events
actionFlow._autoSaveManager.SaveCompleted += (s, checkpoint) =>
{
    Console.WriteLine($"Saved {checkpoint.SerializedWorkflows.Count} workflows");
};

// Manual save when needed
var result = await actionFlow.SaveAsync("currentUser");
```

### Monitoring and Analytics

```csharp
// Subscribe to events
actionFlow.SubscribeToEvents(@event =>
{
    if (@event.EventLevel == "Error")
    {
        Console.WriteLine($"ERROR: {@event.Message}");
        Console.WriteLine($"Exception: {@event.Exception?.Message}");
    }
});

// Record custom metrics
actionFlow.RecordMetric("user.action.count", 1, "count");
actionFlow.RecordMetric("api.response.time", 125.5, "ms");

// Get execution statistics
var workflowStats = actionFlow.GetWorkflowStatistics(workflowId);
Console.WriteLine($"Total Executions: {workflowStats.TotalExecutions}");
Console.WriteLine($"Success Rate: {workflowStats.SuccessRate}%");
Console.WriteLine($"Avg Duration: {workflowStats.AverageDuration}s");

// Get recent events
var recentErrors = actionFlow.GetErrorEvents(limit: 10);
foreach (var error in recentErrors)
{
    Console.WriteLine($"{error.Timestamp}: {error.Message}");
}

// Health checks
var health = actionFlow.CheckHealth();
foreach (var check in health)
{
    Console.WriteLine($"Health Check '{check.Key}': {(check.Value ? "✓" : "✗")}");
}
```

## API Reference

### ActionFlowService

Main orchestration service that combines all components.

**Projects**
- `CreateProjectAsync(name, description)` - Create new project
- `GetProject(projectId)` - Retrieve project
- `DeleteProjectAsync(projectId)` - Delete project
- `UpdateProject(project)` - Update project

**Workflows**
- `CreateWorkflowFromTemplate(templateId)` - Create from template
- `GetWorkflow(workflowId)` - Retrieve workflow
- `UpdateWorkflow(workflow)` - Update workflow
- `ExecuteWorkflowAsync(workflowId, projectId, variables)` - Execute

**Pages**
- `CreatePageFromTemplate(templateId)` - Create from template
- `GetPage(pageId)` - Retrieve page
- `UpdatePage(page)` - Update page
- `NavigateToPageAsync(pageId, path)` - Navigate

**Undo/Redo**
- `Undo()` - Undo last operation
- `Redo()` - Redo last undone operation
- `GetUndoRedoState()` - Get undo/redo state

**Auto-Save**
- `StartAutoSave(intervalSeconds)` - Start auto-save
- `StopAutoSave()` - Stop auto-save
- `SaveAsync(savedBy)` - Manual save

**Monitoring**
- `RecordMetric(name, value, unit)` - Record metric
- `GetWorkflowStatistics(workflowId)` - Get stats
- `GetRecentEvents(count)` - Get events
- `CheckHealth()` - Health checks

## Performance

- **State Updates**: O(1) for most operations
- **Workflow Execution**: O(n) where n = number of steps
- **Undo/Redo**: O(1) operations, O(n) history traversal
- **Event Storage**: 10,000 events default limit
- **Metric Storage**: 50,000 metrics default limit

## Thread Safety

All components are fully thread-safe:
- Immutable state with locked updates
- Concurrent event subscriptions
- Atomic operation recording
- Lock-free reading where possible

## Integration Points

### Custom Action Handlers

```csharp
public class CustomHandler : ActionHandlerBase
{
    public override string ActionTypeName => "Custom";

    public override async Task<ActionExecutionResult> HandleAsync(
        ActionStep step, 
        ActionContext context)
    {
        // Your custom logic here
        return new ActionExecutionResult
        {
            State = WorkflowState.Completed,
            Output = new() { { "result", "success" } }
        };
    }
}

// Register
actionFlow._actionEngine.RegisterHandler(ActionType.Custom, new CustomHandler());
```

### Custom Persistence

```csharp
public class DatabasePersistence : IAutoSavePersistence
{
    public async Task SaveCheckpointAsync(SaveCheckpoint checkpoint)
    {
        // Save to your database
    }

    // Implement other methods...
}

var persistence = new DatabasePersistence();
var actionFlow = new ActionFlowService(persistence);
```

## Best Practices

1. **Always use the ActionFlowService** for orchestration
2. **Subscribe to events** for monitoring and debugging
3. **Use templates** for consistency and reusability
4. **Enable auto-save** to prevent data loss
5. **Monitor execution statistics** for performance tuning
6. **Handle errors gracefully** with proper exception handling
7. **Test workflows** before production deployment

## Troubleshooting

### Workflows not executing
- Check workflow definition has at least one step
- Verify initial step ID is set
- Check action handlers are registered

### Conflicts during auto-save
- Implement conflict resolver
- Check persistence implementation
- Review merge strategy

### Memory usage
- Adjust event/metric history limits
- Clear old events periodically
- Use checkpoint cleanup

## Future Roadmap

- [ ] Distributed workflow execution
- [ ] Workflow versioning and rollback
- [ ] Advanced analytics dashboard
- [ ] Custom action marketplace
- [ ] Workflow simulation/testing
- [ ] Event streaming integration
- [ ] Multi-project workflows

## Support

For detailed implementation examples, see `IMPLEMENTATION_GUIDE.md`.

For API documentation, see XML comments in source files.

## License

Part of HELIOS Platform - All rights reserved.

## Version

**v1.0.0** - Production Ready

---

**Created**: 2024
**Status**: ✅ Complete & Production-Ready
**Components**: 12/12 Implemented
**Test Coverage**: Comprehensive
**Documentation**: Complete
