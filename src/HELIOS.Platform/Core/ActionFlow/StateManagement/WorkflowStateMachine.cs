using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.ActionFlow.Models;

namespace HELIOS.Platform.Core.ActionFlow.StateManagement
{
    /// <summary>
    /// Represents a state in the workflow state machine
    /// </summary>
    public interface IWorkflowMachineState
    {
        WorkflowState StateType { get; }
        bool CanTransitionTo(WorkflowState targetState);
        Task<bool> OnEnterAsync(ActionContext context);
        Task<bool> OnExitAsync(ActionContext context);
    }

    /// <summary>
    /// Represents a transition rule between states
    /// </summary>
    public class StateTransitionRule
    {
        public WorkflowState FromState { get; set; }
        public WorkflowState ToState { get; set; }
        public Func<ActionContext, bool>? Condition { get; set; }
        public Func<ActionContext, Task>? OnTransition { get; set; }
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Workflow state machine implementation
    /// </summary>
    public class WorkflowStateMachine
    {
        private WorkflowState _currentState = WorkflowState.Idle;
        private readonly Dictionary<WorkflowState, IWorkflowMachineState> _states = new();
        private readonly List<StateTransitionRule> _transitionRules = new();
        private readonly Dictionary<WorkflowState, List<Func<ActionContext, Task>>> _stateHandlers = new();
        private readonly List<Func<WorkflowState, WorkflowState, ActionContext, Task>> _stateChangeListeners = new();

        private readonly object _lockObject = new();

        public WorkflowState CurrentState
        {
            get { lock (_lockObject) { return _currentState; } }
        }

        public WorkflowStateMachine()
        {
            InitializeDefaultStates();
        }

        /// <summary>
        /// Initializes default workflow states
        /// </summary>
        private void InitializeDefaultStates()
        {
            RegisterState(WorkflowState.Idle, new IdleState());
            RegisterState(WorkflowState.Created, new CreatedState());
            RegisterState(WorkflowState.Running, new RunningState());
            RegisterState(WorkflowState.Paused, new PausedState());
            RegisterState(WorkflowState.Completed, new CompletedState());
            RegisterState(WorkflowState.Failed, new FailedState());
            RegisterState(WorkflowState.Cancelled, new CancelledState());
            RegisterState(WorkflowState.WaitingForInput, new WaitingForInputState());
            RegisterState(WorkflowState.AwaitingDependency, new AwaitingDependencyState());
        }

        /// <summary>
        /// Registers a state
        /// </summary>
        public void RegisterState(WorkflowState stateType, IWorkflowMachineState state)
        {
            lock (_lockObject)
            {
                _states[stateType] = state;
            }
        }

        /// <summary>
        /// Adds a transition rule
        /// </summary>
        public void AddTransitionRule(StateTransitionRule rule)
        {
            lock (_lockObject)
            {
                _transitionRules.Add(rule);
                _transitionRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        /// <summary>
        /// Attempts to transition to a new state
        /// </summary>
        public async Task<bool> TransitionAsync(WorkflowState targetState, ActionContext context)
        {
            lock (_lockObject)
            {
                if (_currentState == targetState)
                    return true;

                // Check if transition is valid
                if (!CanTransition(targetState))
                    return false;

                // Check transition rules
                var applicableRules = _transitionRules
                    .Where(r => r.FromState == _currentState && r.ToState == targetState)
                    .ToList();

                if (applicableRules.Any())
                {
                    var rule = applicableRules.First();
                    if (rule.Condition != null && !rule.Condition(context))
                        return false;
                }
            }

            // Exit current state
            if (_states.TryGetValue(_currentState, out var currentStateHandler))
            {
                await currentStateHandler.OnExitAsync(context);
            }

            // Execute transition callbacks
            var transitionRules = _transitionRules
                .Where(r => r.FromState == _currentState && r.ToState == targetState)
                .ToList();

            foreach (var rule in transitionRules)
            {
                if (rule.OnTransition != null)
                    await rule.OnTransition(context);
            }

            // Enter new state
            if (_states.TryGetValue(targetState, out var newStateHandler))
            {
                var enterSuccess = await newStateHandler.OnEnterAsync(context);
                if (!enterSuccess)
                    return false;
            }

            WorkflowState oldState;
            lock (_lockObject)
            {
                oldState = _currentState;
                _currentState = targetState;
            }

            // Notify listeners
            foreach (var listener in _stateChangeListeners.ToList())
            {
                await listener(oldState, targetState, context);
            }

            return true;
        }

        /// <summary>
        /// Checks if a transition is allowed
        /// </summary>
        public bool CanTransition(WorkflowState targetState)
        {
            lock (_lockObject)
            {
                if (!_states.TryGetValue(_currentState, out var currentState))
                    return false;
                return currentState.CanTransitionTo(targetState);
            }
        }

        /// <summary>
        /// Registers a handler for a specific state
        /// </summary>
        public void RegisterStateHandler(WorkflowState state, Func<ActionContext, Task> handler)
        {
            lock (_lockObject)
            {
                if (!_stateHandlers.ContainsKey(state))
                    _stateHandlers[state] = new List<Func<ActionContext, Task>>();
                _stateHandlers[state].Add(handler);
            }
        }

        /// <summary>
        /// Subscribes to state changes
        /// </summary>
        public void Subscribe(Func<WorkflowState, WorkflowState, ActionContext, Task> listener)
        {
            lock (_lockObject)
            {
                _stateChangeListeners.Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes from state changes
        /// </summary>
        public void Unsubscribe(Func<WorkflowState, WorkflowState, ActionContext, Task> listener)
        {
            lock (_lockObject)
            {
                _stateChangeListeners.Remove(listener);
            }
        }

        /// <summary>
        /// Gets available transitions from current state
        /// </summary>
        public List<WorkflowState> GetAvailableTransitions()
        {
            lock (_lockObject)
            {
                if (!_states.TryGetValue(_currentState, out var state))
                    return new List<WorkflowState>();

                var allStates = _states.Keys.ToList();
                return allStates.Where(s => state.CanTransitionTo(s)).ToList();
            }
        }

        /// <summary>
        /// Resets the state machine
        /// </summary>
        public async Task ResetAsync(ActionContext context)
        {
            if (_states.TryGetValue(_currentState, out var currentState))
                await currentState.OnExitAsync(context);

            lock (_lockObject)
            {
                _currentState = WorkflowState.Idle;
            }

            if (_states.TryGetValue(WorkflowState.Idle, out var idleState))
                await idleState.OnEnterAsync(context);
        }
    }

    #region Default State Implementations

    /// <summary>
    /// Idle state - no workflow is running
    /// </summary>
    public class IdleState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.Idle;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState == WorkflowState.Created;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.Idle;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// Created state - workflow has been created
    /// </summary>
    public class CreatedState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.Created;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState is WorkflowState.Running or WorkflowState.Cancelled;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.Created;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// Running state - workflow is executing
    /// </summary>
    public class RunningState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.Running;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState is WorkflowState.Paused or WorkflowState.Completed or WorkflowState.Failed or WorkflowState.Cancelled or WorkflowState.WaitingForInput;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.Running;
            context.StartedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// Paused state - workflow is temporarily stopped
    /// </summary>
    public class PausedState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.Paused;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState is WorkflowState.Running or WorkflowState.Cancelled;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.Paused;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// Completed state - workflow finished successfully
    /// </summary>
    public class CompletedState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.Completed;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState == WorkflowState.Idle;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.Completed;
            context.CompletedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// Failed state - workflow encountered an error
    /// </summary>
    public class FailedState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.Failed;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState is WorkflowState.Idle or WorkflowState.Running;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.Failed;
            context.CompletedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// Cancelled state - workflow was cancelled
    /// </summary>
    public class CancelledState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.Cancelled;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState == WorkflowState.Idle;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.Cancelled;
            context.CompletedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// WaitingForInput state - workflow is waiting for user input
    /// </summary>
    public class WaitingForInputState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.WaitingForInput;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState is WorkflowState.Running or WorkflowState.Cancelled or WorkflowState.Failed;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.WaitingForInput;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    /// <summary>
    /// AwaitingDependency state - workflow is waiting for dependencies
    /// </summary>
    public class AwaitingDependencyState : IWorkflowMachineState
    {
        public WorkflowState StateType => WorkflowState.AwaitingDependency;

        public bool CanTransitionTo(WorkflowState targetState) =>
            targetState is WorkflowState.Running or WorkflowState.Cancelled or WorkflowState.Failed;

        public Task<bool> OnEnterAsync(ActionContext context)
        {
            context.CurrentState = WorkflowState.AwaitingDependency;
            return Task.FromResult(true);
        }

        public Task<bool> OnExitAsync(ActionContext context) => Task.FromResult(true);
    }

    #endregion
}
