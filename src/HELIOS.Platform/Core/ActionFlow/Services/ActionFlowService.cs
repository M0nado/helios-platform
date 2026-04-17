using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.ActionFlow.Models;
using HELIOS.Platform.Core.ActionFlow.StateManagement;
using HELIOS.Platform.Core.ActionFlow.Engines;
using HELIOS.Platform.Core.ActionFlow.PageRouter;
using HELIOS.Platform.Core.ActionFlow.UndoRedo;
using HELIOS.Platform.Core.ActionFlow.AutoSave;
using HELIOS.Platform.Core.ActionFlow.Templates;
using HELIOS.Platform.Core.ActionFlow.DragDrop;
using HELIOS.Platform.Core.ActionFlow.Monitoring;

namespace HELIOS.Platform.Core.ActionFlow.Services
{
    /// <summary>
    /// Comprehensive service combining all ActionFlow components
    /// </summary>
    public class ActionFlowService : IDisposable
    {
        private readonly StateStore _stateStore;
        private readonly WorkflowStateMachine _stateMachine;
        private readonly ActionEngine _actionEngine;
        private readonly PageRouter _pageRouter;
        private readonly ProjectPageNavigationManager _navigationManager;
        private readonly UndoRedoStack _undoRedoStack;
        private readonly AutoSaveManager _autoSaveManager;
        private readonly TemplateManager _templateManager;
        private readonly WorkflowDragDropBuilder _workflowBuilder;
        private readonly PageDragDropBuilder _pageBuilder;
        private readonly MonitoringEngine _monitoringEngine;
        private readonly HealthCheckEngine _healthCheckEngine;

        public ActionFlowState CurrentState => _stateStore.CurrentState;
        public WorkflowState CurrentWorkflowState => _stateMachine.CurrentState;
        public ActionFlowId? CurrentPageId => _pageRouter.CurrentPageId;

        public ActionFlowService(IAutoSavePersistence? persistence = null)
        {
            // Initialize core components
            _stateStore = new StateStore();
            _stateMachine = new WorkflowStateMachine();
            _actionEngine = new ActionEngine(_stateMachine, _stateStore);

            // Initialize routing
            _pageRouter = new PageRouter();
            _navigationManager = new ProjectPageNavigationManager();

            // Initialize undo/redo
            _undoRedoStack = new UndoRedoStack(maxStackSize: 100);

            // Initialize auto-save
            var autosavePersistence = persistence ?? new InMemoryAutoSavePersistence();
            _autoSaveManager = new AutoSaveManager(_stateStore, autosavePersistence);

            // Initialize templates
            _templateManager = new TemplateManager();
            InitializeDefaultTemplates();

            // Initialize drag-drop builders
            _workflowBuilder = new WorkflowDragDropBuilder();
            _pageBuilder = new PageDragDropBuilder();

            // Initialize monitoring
            _monitoringEngine = new MonitoringEngine();
            _healthCheckEngine = new HealthCheckEngine(_monitoringEngine);

            // Setup state change logging
            _stateStore.Subscribe((newState, previousState, action) =>
            {
                _monitoringEngine.RecordEvent("StateChanged", $"State updated by action: {action.Type}", "Info");
            });
        }

        #region Project Management

        /// <summary>
        /// Creates a new project
        /// </summary>
        public async Task<ProjectDefinition> CreateProjectAsync(string name, string description)
        {
            var project = new ProjectDefinition
            {
                Name = name,
                Description = description
            };

            _stateStore.Dispatch(new AddProjectAction(project));
            _undoRedoStack.RecordOperation(new AddProjectOperation(_stateStore, project));
            _monitoringEngine.RecordEvent("ProjectCreated", $"Project created: {name}", "Info");

            await Task.CompletedTask;
            return project;
        }

        /// <summary>
        /// Gets a project by ID
        /// </summary>
        public ProjectDefinition? GetProject(ActionFlowId projectId)
        {
            var state = _stateStore.CurrentState;
            state.Projects.TryGetValue(projectId, out var project);
            return project;
        }

        /// <summary>
        /// Deletes a project
        /// </summary>
        public async Task<bool> DeleteProjectAsync(ActionFlowId projectId)
        {
            var project = GetProject(projectId);
            if (project == null)
                return false;

            _stateStore.Dispatch(new DeleteProjectAction(projectId));
            _monitoringEngine.RecordEvent("ProjectDeleted", $"Project deleted: {projectId}", "Info");

            await Task.CompletedTask;
            return true;
        }

        #endregion

        #region Workflow Management

        /// <summary>
        /// Creates a new workflow from template
        /// </summary>
        public WorkflowDefinition CreateWorkflowFromTemplate(ActionFlowId templateId)
        {
            var workflow = _templateManager.CreateWorkflowFromTemplate(templateId);
            _stateStore.Dispatch(new AddWorkflowAction(workflow));
            _undoRedoStack.RecordOperation(new AddWorkflowOperation(_stateStore, workflow));
            _monitoringEngine.RecordEvent("WorkflowCreated", $"Workflow created: {workflow.Name}", "Info");

            return workflow;
        }

        /// <summary>
        /// Gets a workflow by ID
        /// </summary>
        public WorkflowDefinition? GetWorkflow(ActionFlowId workflowId)
        {
            var state = _stateStore.CurrentState;
            state.Workflows.TryGetValue(workflowId, out var workflow);
            return workflow;
        }

        /// <summary>
        /// Updates a workflow
        /// </summary>
        public void UpdateWorkflow(WorkflowDefinition workflow)
        {
            var oldWorkflow = GetWorkflow(workflow.Id);
            if (oldWorkflow != null)
            {
                _stateStore.Dispatch(new UpdateWorkflowAction(workflow));
                _undoRedoStack.RecordOperation(new UpdateWorkflowOperation(_stateStore, workflow, oldWorkflow));
                _monitoringEngine.RecordEvent("WorkflowUpdated", $"Workflow updated: {workflow.Name}", "Info");
            }
        }

        /// <summary>
        /// Executes a workflow
        /// </summary>
        public async Task<WorkflowExecutionInstance> ExecuteWorkflowAsync(
            ActionFlowId workflowId,
            ActionFlowId projectId,
            Dictionary<string, object>? variables = null)
        {
            var workflow = GetWorkflow(workflowId);
            if (workflow == null)
                throw new InvalidOperationException($"Workflow not found: {workflowId}");

            _monitoringEngine.RecordWorkflowStart(workflowId);

            var startTime = DateTime.UtcNow;
            try
            {
                var instance = await _actionEngine.ExecuteWorkflowAsync(workflow, projectId, variables);
                var duration = DateTime.UtcNow - startTime;

                _monitoringEngine.RecordWorkflowCompleted(workflowId, duration, instance.CurrentState == WorkflowState.Completed);
                _stateStore.Dispatch(new SetExecutingWorkflowAction(instance));

                return instance;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _monitoringEngine.RecordWorkflowCompleted(workflowId, duration, false);
                throw;
            }
        }

        #endregion

        #region Page Management

        /// <summary>
        /// Creates a new page from template
        /// </summary>
        public PageConfiguration CreatePageFromTemplate(ActionFlowId templateId)
        {
            var page = _templateManager.CreatePageFromTemplate(templateId);
            _stateStore.Dispatch(new AddPageAction(page));
            _undoRedoStack.RecordOperation(new AddPageOperation(_stateStore, page));
            _monitoringEngine.RecordEvent("PageCreated", $"Page created: {page.Name}", "Info");

            return page;
        }

        /// <summary>
        /// Gets a page by ID
        /// </summary>
        public PageConfiguration? GetPage(ActionFlowId pageId)
        {
            var state = _stateStore.CurrentState;
            state.Pages.TryGetValue(pageId, out var page);
            return page;
        }

        /// <summary>
        /// Updates a page
        /// </summary>
        public void UpdatePage(PageConfiguration page)
        {
            var oldPage = GetPage(page.Id);
            if (oldPage != null)
            {
                _stateStore.Dispatch(new UpdatePageAction(page));
                _undoRedoStack.RecordOperation(new UpdatePageOperation(_stateStore, page, oldPage));
                _monitoringEngine.RecordEvent("PageUpdated", $"Page updated: {page.Name}", "Info");
            }
        }

        /// <summary>
        /// Navigates to a page
        /// </summary>
        public async Task<bool> NavigateToPageAsync(ActionFlowId pageId, string? path = null)
        {
            var success = await _pageRouter.NavigateToPageAsync(pageId, path);
            if (success)
            {
                _monitoringEngine.RecordPageNavigation(pageId);
            }
            return success;
        }

        #endregion

        #region Navigation Management

        /// <summary>
        /// Creates a project router with all pages
        /// </summary>
        public PageRouter CreateProjectRouter(ActionFlowId projectId)
        {
            var project = GetProject(projectId);
            if (project == null)
                throw new InvalidOperationException($"Project not found: {projectId}");

            return _navigationManager.CreateProjectRouter(projectId, project);
        }

        /// <summary>
        /// Gets available navigations from current page
        /// </summary>
        public List<PageNavigation> GetAvailableNavigations(ActionFlowId projectId, ActionFlowId pageId)
        {
            return _navigationManager.GetAvailableNavigations(projectId, pageId);
        }

        #endregion

        #region Undo/Redo

        /// <summary>
        /// Undoes the last operation
        /// </summary>
        public bool Undo()
        {
            var success = _undoRedoStack.Undo();
            if (success)
                _monitoringEngine.RecordEvent("Undo", "Operation undone", "Info");
            return success;
        }

        /// <summary>
        /// Redoes the last undone operation
        /// </summary>
        public bool Redo()
        {
            var success = _undoRedoStack.Redo();
            if (success)
                _monitoringEngine.RecordEvent("Redo", "Operation redone", "Info");
            return success;
        }

        /// <summary>
        /// Gets undo/redo state
        /// </summary>
        public UndoRedoState GetUndoRedoState()
        {
            return new UndoRedoState
            {
                CanUndo = _undoRedoStack.UndoCount > 0,
                CanRedo = _undoRedoStack.RedoCount > 0,
                UndoCount = _undoRedoStack.UndoCount,
                RedoCount = _undoRedoStack.RedoCount
            };
        }

        #endregion

        #region Auto-Save

        /// <summary>
        /// Starts auto-save
        /// </summary>
        public void StartAutoSave(int intervalSeconds = 30)
        {
            _autoSaveManager.StartAutoSave(intervalSeconds);
            _monitoringEngine.RecordEvent("AutoSaveStarted", $"Auto-save started with {intervalSeconds}s interval", "Info");
        }

        /// <summary>
        /// Stops auto-save
        /// </summary>
        public void StopAutoSave()
        {
            _autoSaveManager.StopAutoSave();
            _monitoringEngine.RecordEvent("AutoSaveStopped", "Auto-save stopped", "Info");
        }

        /// <summary>
        /// Manually saves current state
        /// </summary>
        public async Task<SaveCheckpoint> SaveAsync(string? savedBy = null)
        {
            return await _autoSaveManager.SaveAsync(detectConflicts: true, savedBy: savedBy);
        }

        #endregion

        #region Monitoring & Analytics

        /// <summary>
        /// Records a performance metric
        /// </summary>
        public void RecordMetric(string name, double value, string unit = "")
        {
            _monitoringEngine.RecordMetric(name, value, unit);
        }

        /// <summary>
        /// Gets workflow execution statistics
        /// </summary>
        public ExecutionStatistics GetWorkflowStatistics(ActionFlowId workflowId)
        {
            return _monitoringEngine.GetWorkflowStatistics(workflowId);
        }

        /// <summary>
        /// Gets recent events
        /// </summary>
        public List<ActionFlowEvent> GetRecentEvents(int count = 100)
        {
            return _monitoringEngine.GetRecentEvents(count);
        }

        /// <summary>
        /// Gets error events
        /// </summary>
        public List<ActionFlowEvent> GetErrorEvents(int count = 100)
        {
            return _monitoringEngine.GetErrorEvents(count);
        }

        /// <summary>
        /// Performs health checks
        /// </summary>
        public Dictionary<string, bool> CheckHealth()
        {
            return _healthCheckEngine.PerformHealthChecks();
        }

        #endregion

        #region Templates

        /// <summary>
        /// Gets all page templates
        /// </summary>
        public List<PageTemplate> GetPageTemplates()
        {
            return _templateManager.GetAllPageTemplates();
        }

        /// <summary>
        /// Gets all workflow templates
        /// </summary>
        public List<WorkflowTemplate> GetWorkflowTemplates()
        {
            return _templateManager.GetAllWorkflowTemplates();
        }

        #endregion

        #region Builders

        /// <summary>
        /// Gets the workflow builder
        /// </summary>
        public WorkflowDragDropBuilder GetWorkflowBuilder()
        {
            return _workflowBuilder;
        }

        /// <summary>
        /// Gets the page builder
        /// </summary>
        public PageDragDropBuilder GetPageBuilder()
        {
            return _pageBuilder;
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Subscribes to state changes
        /// </summary>
        public void SubscribeToStateChanges(StateChangeListener listener)
        {
            _stateStore.Subscribe(listener);
        }

        /// <summary>
        /// Subscribes to navigation
        /// </summary>
        public void SubscribeToNavigation(Func<ActionFlowId, ActionFlowId, Task> listener)
        {
            _pageRouter.SubscribeToNavigation(listener);
        }

        /// <summary>
        /// Subscribes to events
        /// </summary>
        public void SubscribeToEvents(Action<ActionFlowEvent> listener)
        {
            _monitoringEngine.SubscribeToEvents(listener);
        }

        #endregion

        #region Initialization

        private void InitializeDefaultTemplates()
        {
            _templateManager.RegisterPageTemplate(BuiltInPageTemplates.CreateBlankTemplate());
            _templateManager.RegisterPageTemplate(BuiltInPageTemplates.CreateFormTemplate());
            _templateManager.RegisterPageTemplate(BuiltInPageTemplates.CreateDashboardTemplate());
            _templateManager.RegisterPageTemplate(BuiltInPageTemplates.CreateListTemplate());

            _templateManager.RegisterWorkflowTemplate(BuiltInWorkflowTemplates.CreateSimpleWorkflow());
            _templateManager.RegisterWorkflowTemplate(BuiltInWorkflowTemplates.CreateApprovalWorkflow());
            _templateManager.RegisterWorkflowTemplate(BuiltInWorkflowTemplates.CreateParallelProcessing());
        }

        #endregion

        public void Dispose()
        {
            _autoSaveManager?.Dispose();
            _stateStore?.Dispose();
        }
    }

    /// <summary>
    /// In-memory auto-save persistence
    /// </summary>
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
            return Task.FromResult(
                _checkpoints.Values
                    .OrderByDescending(c => c.SavedAt)
                    .Take(limit)
                    .ToList());
        }

        public Task DeleteCheckpointAsync(ActionFlowId id)
        {
            _checkpoints.Remove(id);
            return Task.CompletedTask;
        }
    }
}
