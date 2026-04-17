using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.ActionFlow.Models;
using HELIOS.Platform.Core.ActionFlow.StateManagement;

namespace HELIOS.Platform.Core.ActionFlow.Engines
{
    /// <summary>
    /// Represents an action handler
    /// </summary>
    public interface IActionHandler
    {
        ActionFlowId HandlerId { get; }
        string ActionTypeName { get; }
        Task<ActionExecutionResult> HandleAsync(ActionStep step, ActionContext context);
    }

    /// <summary>
    /// Base class for action handlers
    /// </summary>
    public abstract class ActionHandlerBase : IActionHandler
    {
        public ActionFlowId HandlerId { get; } = ActionFlowId.New();
        public abstract string ActionTypeName { get; }

        public abstract Task<ActionExecutionResult> HandleAsync(ActionStep step, ActionContext context);

        protected async Task<ActionExecutionResult> ExecuteWithRetry(
            Func<Task<ActionExecutionResult>> action,
            int maxRetries,
            TimeSpan timeout)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var cts = new CancellationTokenSource(timeout))
                    {
                        var task = action();
                        var result = await task;

                        if (result.Success)
                            return result;

                        if (attempt < maxRetries)
                            await Task.Delay(1000 * (int)Math.Pow(2, attempt)); // Exponential backoff
                    }
                }
                catch (OperationCanceledException)
                {
                    return new ActionExecutionResult
                    {
                        State = WorkflowState.Failed,
                        Error = new TimeoutException("Action execution timed out"),
                        RetryCount = attempt
                    };
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                        return new ActionExecutionResult
                        {
                            State = WorkflowState.Failed,
                            Error = ex,
                            RetryCount = attempt
                        };
                }
            }

            return new ActionExecutionResult
            {
                State = WorkflowState.Failed,
                Error = new Exception("All retry attempts failed"),
                RetryCount = maxRetries
            };
        }
    }

    /// <summary>
    /// Simple action handler
    /// </summary>
    public class SimpleActionHandler : ActionHandlerBase
    {
        public override string ActionTypeName => "Simple";

        public override async Task<ActionExecutionResult> HandleAsync(ActionStep step, ActionContext context)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // Simulate action execution
                await Task.Delay(100);

                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Completed,
                    Output = new() { { "result", "success" } },
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Failed,
                    Error = ex,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Conditional action handler
    /// </summary>
    public class ConditionalActionHandler : ActionHandlerBase
    {
        public override string ActionTypeName => "Conditional";

        public override async Task<ActionExecutionResult> HandleAsync(ActionStep step, ActionContext context)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // Evaluate condition from configuration
                var condition = step.Configuration.TryGetValue("condition", out var cond) ? cond?.ToString() : "true";
                var result = condition == "true";

                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Completed,
                    Output = new() { { "condition_result", result } },
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Failed,
                    Error = ex,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Loop action handler
    /// </summary>
    public class LoopActionHandler : ActionHandlerBase
    {
        public override string ActionTypeName => "Loop";

        public override async Task<ActionExecutionResult> HandleAsync(ActionStep step, ActionContext context)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var iterations = step.Configuration.TryGetValue("iterations", out var iter) && iter is int i ? i : 1;
                var results = new List<object>();

                for (int i = 0; i < iterations; i++)
                {
                    results.Add(new { iteration = i, timestamp = DateTime.UtcNow });
                }

                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Completed,
                    Output = new() { { "iterations_completed", iterations }, { "results", results } },
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Failed,
                    Error = ex,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Parallel action handler
    /// </summary>
    public class ParallelActionHandler : ActionHandlerBase
    {
        public override string ActionTypeName => "Parallel";

        public override async Task<ActionExecutionResult> HandleAsync(ActionStep step, ActionContext context)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var parallelTasks = step.Configuration.TryGetValue("tasks", out var tasksObj) && tasksObj is List<object> tasks
                    ? tasks.Count
                    : 1;

                var tasks2 = Enumerable.Range(0, parallelTasks)
                    .Select(_ => Task.Delay(100))
                    .ToArray();

                await Task.WhenAll(tasks2);

                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Completed,
                    Output = new() { { "parallel_tasks_completed", parallelTasks } },
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ActionExecutionResult
                {
                    ActionId = step.Id,
                    State = WorkflowState.Failed,
                    Error = ex,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Core action execution engine
    /// </summary>
    public class ActionEngine
    {
        private readonly Dictionary<ActionType, IActionHandler> _handlers = new();
        private readonly WorkflowStateMachine _stateMachine;
        private readonly StateStore _stateStore;
        private readonly object _lockObject = new();

        public ActionEngine(WorkflowStateMachine stateMachine, StateStore stateStore)
        {
            _stateMachine = stateMachine;
            _stateStore = stateStore;
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// Registers default action handlers
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            RegisterHandler(ActionType.Simple, new SimpleActionHandler());
            RegisterHandler(ActionType.Conditional, new ConditionalActionHandler());
            RegisterHandler(ActionType.Loop, new LoopActionHandler());
            RegisterHandler(ActionType.Parallel, new ParallelActionHandler());
        }

        /// <summary>
        /// Registers a custom action handler
        /// </summary>
        public void RegisterHandler(ActionType actionType, IActionHandler handler)
        {
            lock (_lockObject)
            {
                _handlers[actionType] = handler;
            }
        }

        /// <summary>
        /// Executes an action step
        /// </summary>
        public async Task<ActionExecutionResult> ExecuteActionAsync(ActionStep action, ActionContext context)
        {
            IActionHandler? handler;
            lock (_lockObject)
            {
                _handlers.TryGetValue(action.ActionType, out handler);
            }

            if (handler == null)
                return new ActionExecutionResult
                {
                    ActionId = action.Id,
                    State = WorkflowState.Failed,
                    Error = new InvalidOperationException($"No handler registered for action type: {action.ActionType}")
                };

            var result = await ExecuteWithRetry(
                () => handler.HandleAsync(action, context),
                action.MaxRetries,
                action.Timeout);

            result.ActionId = action.Id;
            context.Output[action.Id.ToString()] = result.Output;

            return result;
        }

        /// <summary>
        /// Executes a complete workflow
        /// </summary>
        public async Task<WorkflowExecutionInstance> ExecuteWorkflowAsync(
            WorkflowDefinition workflow,
            ActionFlowId projectId,
            Dictionary<string, object>? initialVariables = null)
        {
            var instance = new WorkflowExecutionInstance
            {
                WorkflowDefinitionId = workflow.Id,
                ProjectId = projectId,
                ExecutionContext = new ActionContext
                {
                    WorkflowId = workflow.Id,
                    ProjectId = projectId
                }
            };

            // Initialize variables
            if (initialVariables != null)
            {
                foreach (var kvp in initialVariables)
                {
                    instance.ExecutionContext.SetVariable(kvp.Key, kvp.Value);
                }
            }

            // Add global variables
            foreach (var kvp in workflow.GlobalVariables)
            {
                instance.ExecutionContext.SetVariable(kvp.Key, kvp.Value);
            }

            try
            {
                // Transition to Running state
                await _stateMachine.TransitionAsync(WorkflowState.Running, instance.ExecutionContext);
                instance.CurrentState = WorkflowState.Running;

                // Execute steps
                if (workflow.InitialStepId != null)
                {
                    var stepId = workflow.InitialStepId.Value;
                    var executedStepIds = new HashSet<ActionFlowId>();

                    while (stepId != null && !workflow.TerminalStepIds.Contains(stepId))
                    {
                        if (executedStepIds.Contains(stepId))
                            break; // Prevent infinite loops

                        executedStepIds.Add(stepId);

                        var step = workflow.Steps.FirstOrDefault(s => s.Id == stepId);
                        if (step == null)
                            break;

                        instance.ExecutionHistory.Add($"Executing step: {step.Name}");
                        instance.CurrentStepId = stepId;

                        var result = await ExecuteActionAsync(step, instance.ExecutionContext);
                        instance.StepResults[step.Id] = result;

                        if (!result.Success)
                        {
                            instance.Error = result.Error;
                            await _stateMachine.TransitionAsync(WorkflowState.Failed, instance.ExecutionContext);
                            instance.CurrentState = WorkflowState.Failed;
                            break;
                        }

                        // Find next step
                        var transition = workflow.Transitions.FirstOrDefault(t => t.FromStepId == stepId);
                        stepId = transition?.ToStepId;
                    }

                    if (instance.CurrentState == WorkflowState.Running)
                    {
                        await _stateMachine.TransitionAsync(WorkflowState.Completed, instance.ExecutionContext);
                        instance.CurrentState = WorkflowState.Completed;
                    }
                }
            }
            catch (Exception ex)
            {
                instance.Error = ex;
                instance.CurrentState = WorkflowState.Failed;
                await _stateMachine.TransitionAsync(WorkflowState.Failed, instance.ExecutionContext);
            }

            instance.CompletedAt = DateTime.UtcNow;
            return instance;
        }

        /// <summary>
        /// Pauses workflow execution
        /// </summary>
        public async Task<bool> PauseWorkflowAsync(WorkflowExecutionInstance instance)
        {
            return await _stateMachine.TransitionAsync(WorkflowState.Paused, instance.ExecutionContext);
        }

        /// <summary>
        /// Resumes workflow execution
        /// </summary>
        public async Task<bool> ResumeWorkflowAsync(WorkflowExecutionInstance instance)
        {
            return await _stateMachine.TransitionAsync(WorkflowState.Running, instance.ExecutionContext);
        }

        /// <summary>
        /// Cancels workflow execution
        /// </summary>
        public async Task<bool> CancelWorkflowAsync(WorkflowExecutionInstance instance)
        {
            return await _stateMachine.TransitionAsync(WorkflowState.Cancelled, instance.ExecutionContext);
        }

        private async Task<ActionExecutionResult> ExecuteWithRetry(
            Func<Task<ActionExecutionResult>> action,
            int maxRetries,
            TimeSpan timeout)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var cts = new CancellationTokenSource(timeout))
                    {
                        var result = await action();
                        if (result.Success)
                            return result;

                        if (attempt < maxRetries)
                            await Task.Delay(1000 * (int)Math.Pow(2, attempt));
                    }
                }
                catch (OperationCanceledException)
                {
                    return new ActionExecutionResult
                    {
                        State = WorkflowState.Failed,
                        Error = new TimeoutException("Action execution timed out"),
                        RetryCount = attempt
                    };
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                        return new ActionExecutionResult
                        {
                            State = WorkflowState.Failed,
                            Error = ex,
                            RetryCount = attempt
                        };
                }
            }

            return new ActionExecutionResult
            {
                State = WorkflowState.Failed,
                Error = new Exception("All retry attempts failed"),
                RetryCount = maxRetries
            };
        }
    }
}
