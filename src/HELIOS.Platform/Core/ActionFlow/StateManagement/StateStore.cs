using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.ActionFlow.Models;

namespace HELIOS.Platform.Core.ActionFlow.StateManagement
{
    /// <summary>
    /// Represents an action that can be dispatched to the state store
    /// </summary>
    public interface IActionFlowAction
    {
        string Type { get; }
        DateTime Timestamp { get; }
        Dictionary<string, object> Payload { get; }
    }

    /// <summary>
    /// Base class for action flow actions
    /// </summary>
    public abstract class ActionFlowActionBase : IActionFlowAction
    {
        public abstract string Type { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public virtual Dictionary<string, object> Payload { get; } = new();
    }

    /// <summary>
    /// Represents the complete application state
    /// </summary>
    public sealed class ActionFlowState
    {
        public ActionFlowState()
        {
            Workflows = ImmutableDictionary<ActionFlowId, WorkflowDefinition>.Empty;
            Projects = ImmutableDictionary<ActionFlowId, ProjectDefinition>.Empty;
            Pages = ImmutableDictionary<ActionFlowId, PageConfiguration>.Empty;
            ExecutingWorkflows = ImmutableDictionary<ActionFlowId, WorkflowExecutionInstance>.Empty;
            UiState = new UIState();
            UserPreferences = new UserPreferences();
        }

        public ImmutableDictionary<ActionFlowId, WorkflowDefinition> Workflows { get; private set; }
        public ImmutableDictionary<ActionFlowId, ProjectDefinition> Projects { get; private set; }
        public ImmutableDictionary<ActionFlowId, PageConfiguration> Pages { get; private set; }
        public ImmutableDictionary<ActionFlowId, WorkflowExecutionInstance> ExecutingWorkflows { get; private set; }

        public UIState UiState { get; private set; }
        public UserPreferences UserPreferences { get; private set; }

        public DateTime LastModified { get; private set; } = DateTime.UtcNow;

        public ActionFlowState WithWorkflows(ImmutableDictionary<ActionFlowId, WorkflowDefinition> workflows) =>
            new() { Workflows = workflows, Projects = Projects, Pages = Pages, ExecutingWorkflows = ExecutingWorkflows, UiState = UiState, UserPreferences = UserPreferences, LastModified = DateTime.UtcNow };

        public ActionFlowState WithProjects(ImmutableDictionary<ActionFlowId, ProjectDefinition> projects) =>
            new() { Workflows = Workflows, Projects = projects, Pages = Pages, ExecutingWorkflows = ExecutingWorkflows, UiState = UiState, UserPreferences = UserPreferences, LastModified = DateTime.UtcNow };

        public ActionFlowState WithPages(ImmutableDictionary<ActionFlowId, PageConfiguration> pages) =>
            new() { Workflows = Workflows, Projects = Projects, Pages = pages, ExecutingWorkflows = ExecutingWorkflows, UiState = UiState, UserPreferences = UserPreferences, LastModified = DateTime.UtcNow };

        public ActionFlowState WithExecutingWorkflows(ImmutableDictionary<ActionFlowId, WorkflowExecutionInstance> executing) =>
            new() { Workflows = Workflows, Projects = Projects, Pages = Pages, ExecutingWorkflows = executing, UiState = UiState, UserPreferences = UserPreferences, LastModified = DateTime.UtcNow };

        public ActionFlowState WithUIState(UIState uiState) =>
            new() { Workflows = Workflows, Projects = Projects, Pages = Pages, ExecutingWorkflows = ExecutingWorkflows, UiState = uiState, UserPreferences = UserPreferences, LastModified = DateTime.UtcNow };

        public ActionFlowState WithUserPreferences(UserPreferences prefs) =>
            new() { Workflows = Workflows, Projects = Projects, Pages = Pages, ExecutingWorkflows = ExecutingWorkflows, UiState = UiState, UserPreferences = prefs, LastModified = DateTime.UtcNow };
    }

    /// <summary>
    /// Represents the UI state
    /// </summary>
    public sealed class UIState
    {
        public ActionFlowId? SelectedProjectId { get; set; }
        public ActionFlowId? SelectedPageId { get; set; }
        public ActionFlowId? SelectedWorkflowId { get; set; }
        public ActionFlowId? SelectedElementId { get; set; }

        public string CurrentView { get; set; } = "ProjectList";
        public bool IsLoading { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public Dictionary<string, object> ViewState { get; set; } = new();
        public Dictionary<string, bool> ExpandedNodes { get; set; } = new();
    }

    /// <summary>
    /// Represents user preferences
    /// </summary>
    public sealed class UserPreferences
    {
        public string Theme { get; set; } = "light";
        public bool AutoSaveEnabled { get; set; } = true;
        public int AutoSaveIntervalSeconds { get; set; } = 30;
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    /// <summary>
    /// Reducer function type
    /// </summary>
    public delegate ActionFlowState StateReducer(ActionFlowState currentState, IActionFlowAction action);

    /// <summary>
    /// Represents subscribers to state changes
    /// </summary>
    public delegate void StateChangeListener(ActionFlowState newState, ActionFlowState previousState, IActionFlowAction action);

    /// <summary>
    /// Redux-like state management store
    /// </summary>
    public class StateStore : IDisposable
    {
        private ActionFlowState _currentState;
        private readonly List<StateChangeListener> _listeners = new();
        private readonly List<Func<ActionFlowState, IActionFlowAction, Task>> _middlewares = new();
        private readonly object _lockObject = new();

        public ActionFlowState CurrentState
        {
            get
            {
                lock (_lockObject)
                {
                    return _currentState;
                }
            }
        }

        public StateStore()
        {
            _currentState = new ActionFlowState();
        }

        /// <summary>
        /// Dispatches an action to update state
        /// </summary>
        public async Task DispatchAsync(IActionFlowAction action)
        {
            ActionFlowState newState;
            
            lock (_lockObject)
            {
                var previousState = _currentState;
                
                // Apply middlewares
                foreach (var middleware in _middlewares)
                {
                    middleware(previousState, action).GetAwaiter().GetResult();
                }

                newState = ReduceState(_currentState, action);
                _currentState = newState;

                // Notify listeners
                foreach (var listener in _listeners.ToList())
                {
                    listener(newState, previousState, action);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Synchronous dispatch for simple state updates
        /// </summary>
        public void Dispatch(IActionFlowAction action)
        {
            DispatchAsync(action).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Registers a state change listener
        /// </summary>
        public void Subscribe(StateChangeListener listener)
        {
            lock (_lockObject)
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregisters a state change listener
        /// </summary>
        public void Unsubscribe(StateChangeListener listener)
        {
            lock (_lockObject)
            {
                _listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Adds middleware
        /// </summary>
        public void Use(Func<ActionFlowState, IActionFlowAction, Task> middleware)
        {
            lock (_lockObject)
            {
                _middlewares.Add(middleware);
            }
        }

        /// <summary>
        /// Core state reducer
        /// </summary>
        private static ActionFlowState ReduceState(ActionFlowState state, IActionFlowAction action) =>
            action.Type switch
            {
                WorkflowActions.ADD_WORKFLOW => HandleAddWorkflow(state, action),
                WorkflowActions.UPDATE_WORKFLOW => HandleUpdateWorkflow(state, action),
                WorkflowActions.DELETE_WORKFLOW => HandleDeleteWorkflow(state, action),
                WorkflowActions.SET_EXECUTING_WORKFLOW => HandleSetExecutingWorkflow(state, action),
                WorkflowActions.REMOVE_EXECUTING_WORKFLOW => HandleRemoveExecutingWorkflow(state, action),

                ProjectActions.ADD_PROJECT => HandleAddProject(state, action),
                ProjectActions.UPDATE_PROJECT => HandleUpdateProject(state, action),
                ProjectActions.DELETE_PROJECT => HandleDeleteProject(state, action),

                PageActions.ADD_PAGE => HandleAddPage(state, action),
                PageActions.UPDATE_PAGE => HandleUpdatePage(state, action),
                PageActions.DELETE_PAGE => HandleDeletePage(state, action),

                UIActions.SET_SELECTED_PROJECT => HandleSetSelectedProject(state, action),
                UIActions.SET_SELECTED_PAGE => HandleSetSelectedPage(state, action),
                UIActions.SET_LOADING => HandleSetLoading(state, action),
                UIActions.SET_ERROR => HandleSetError(state, action),

                _ => state
            };

        #region Workflow Handlers
        private static ActionFlowState HandleAddWorkflow(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("workflow", out var wfObj) && wfObj is WorkflowDefinition wf)
            {
                var workflows = state.Workflows.Add(wf.Id, wf);
                return state.WithWorkflows(workflows);
            }
            return state;
        }

        private static ActionFlowState HandleUpdateWorkflow(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("workflow", out var wfObj) && wfObj is WorkflowDefinition wf)
            {
                var workflows = state.Workflows.SetItem(wf.Id, wf);
                return state.WithWorkflows(workflows);
            }
            return state;
        }

        private static ActionFlowState HandleDeleteWorkflow(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("workflowId", out var idObj) && idObj is ActionFlowId id)
            {
                var workflows = state.Workflows.Remove(id);
                return state.WithWorkflows(workflows);
            }
            return state;
        }

        private static ActionFlowState HandleSetExecutingWorkflow(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("instance", out var instObj) && instObj is WorkflowExecutionInstance inst)
            {
                var executing = state.ExecutingWorkflows.SetItem(inst.Id, inst);
                return state.WithExecutingWorkflows(executing);
            }
            return state;
        }

        private static ActionFlowState HandleRemoveExecutingWorkflow(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("instanceId", out var idObj) && idObj is ActionFlowId id)
            {
                var executing = state.ExecutingWorkflows.Remove(id);
                return state.WithExecutingWorkflows(executing);
            }
            return state;
        }
        #endregion

        #region Project Handlers
        private static ActionFlowState HandleAddProject(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("project", out var projObj) && projObj is ProjectDefinition proj)
            {
                var projects = state.Projects.Add(proj.Id, proj);
                return state.WithProjects(projects);
            }
            return state;
        }

        private static ActionFlowState HandleUpdateProject(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("project", out var projObj) && projObj is ProjectDefinition proj)
            {
                var projects = state.Projects.SetItem(proj.Id, proj);
                return state.WithProjects(projects);
            }
            return state;
        }

        private static ActionFlowState HandleDeleteProject(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("projectId", out var idObj) && idObj is ActionFlowId id)
            {
                var projects = state.Projects.Remove(id);
                return state.WithProjects(projects);
            }
            return state;
        }
        #endregion

        #region Page Handlers
        private static ActionFlowState HandleAddPage(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("page", out var pageObj) && pageObj is PageConfiguration page)
            {
                var pages = state.Pages.Add(page.Id, page);
                return state.WithPages(pages);
            }
            return state;
        }

        private static ActionFlowState HandleUpdatePage(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("page", out var pageObj) && pageObj is PageConfiguration page)
            {
                var pages = state.Pages.SetItem(page.Id, page);
                return state.WithPages(pages);
            }
            return state;
        }

        private static ActionFlowState HandleDeletePage(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("pageId", out var idObj) && idObj is ActionFlowId id)
            {
                var pages = state.Pages.Remove(id);
                return state.WithPages(pages);
            }
            return state;
        }
        #endregion

        #region UI Handlers
        private static ActionFlowState HandleSetSelectedProject(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("projectId", out var idObj) && idObj is ActionFlowId id)
            {
                var uiState = state.UiState;
                uiState.SelectedProjectId = id;
                return state.WithUIState(uiState);
            }
            return state;
        }

        private static ActionFlowState HandleSetSelectedPage(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("pageId", out var idObj) && idObj is ActionFlowId id)
            {
                var uiState = state.UiState;
                uiState.SelectedPageId = id;
                return state.WithUIState(uiState);
            }
            return state;
        }

        private static ActionFlowState HandleSetLoading(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("isLoading", out var isLoadingObj) && isLoadingObj is bool isLoading)
            {
                var uiState = state.UiState;
                uiState.IsLoading = isLoading;
                return state.WithUIState(uiState);
            }
            return state;
        }

        private static ActionFlowState HandleSetError(ActionFlowState state, IActionFlowAction action)
        {
            if (action.Payload.TryGetValue("error", out var errorObj) && errorObj is string error)
            {
                var uiState = state.UiState;
                uiState.ErrorMessage = error;
                return state.WithUIState(uiState);
            }
            return state;
        }
        #endregion

        public void Dispose()
        {
            lock (_lockObject)
            {
                _listeners.Clear();
                _middlewares.Clear();
            }
        }
    }

    /// <summary>
    /// Predefined workflow action types
    /// </summary>
    public static class WorkflowActions
    {
        public const string ADD_WORKFLOW = "workflow/add";
        public const string UPDATE_WORKFLOW = "workflow/update";
        public const string DELETE_WORKFLOW = "workflow/delete";
        public const string SET_EXECUTING_WORKFLOW = "workflow/setExecuting";
        public const string REMOVE_EXECUTING_WORKFLOW = "workflow/removeExecuting";
    }

    /// <summary>
    /// Predefined project action types
    /// </summary>
    public static class ProjectActions
    {
        public const string ADD_PROJECT = "project/add";
        public const string UPDATE_PROJECT = "project/update";
        public const string DELETE_PROJECT = "project/delete";
    }

    /// <summary>
    /// Predefined page action types
    /// </summary>
    public static class PageActions
    {
        public const string ADD_PAGE = "page/add";
        public const string UPDATE_PAGE = "page/update";
        public const string DELETE_PAGE = "page/delete";
    }

    /// <summary>
    /// Predefined UI action types
    /// </summary>
    public static class UIActions
    {
        public const string SET_SELECTED_PROJECT = "ui/setSelectedProject";
        public const string SET_SELECTED_PAGE = "ui/setSelectedPage";
        public const string SET_SELECTED_WORKFLOW = "ui/setSelectedWorkflow";
        public const string SET_LOADING = "ui/setLoading";
        public const string SET_ERROR = "ui/setError";
        public const string SET_SUCCESS = "ui/setSuccess";
    }
}
