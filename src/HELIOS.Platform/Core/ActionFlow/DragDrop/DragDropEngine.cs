using System;
using System.Collections.Generic;
using HELIOS.Platform.Core.ActionFlow.Models;

namespace HELIOS.Platform.Core.ActionFlow.DragDrop
{
    /// <summary>
    /// Represents a draggable item
    /// </summary>
    public class DragItem
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string ItemType { get; set; } = string.Empty;
        public object? Data { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a drop target
    /// </summary>
    public class DropTarget
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string TargetType { get; set; } = string.Empty;
        public Dictionary<string, object> Constraints { get; set; } = new();
        public List<string> AcceptedItemTypes { get; set; } = new();
    }

    /// <summary>
    /// Represents a drag operation
    /// </summary>
    public class DragOperation
    {
        public ActionFlowId OperationId { get; set; } = ActionFlowId.New();
        public DragItem Item { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        public ActionFlowId? SourceContainerId { get; set; }
        public ActionFlowId? TargetContainerId { get; set; }
        
        public bool IsValid { get; set; } = true;
        public string? ValidationMessage { get; set; }
    }

    /// <summary>
    /// Drag and drop engine
    /// </summary>
    public class DragDropEngine
    {
        private readonly Dictionary<ActionFlowId, DropTarget> _dropTargets = new();
        private readonly Dictionary<ActionFlowId, List<Func<DragItem, DropTarget, bool>>> _dropValidators = new();
        private readonly List<Func<DragOperation, System.Threading.Tasks.Task>> _dropListeners = new();
        
        private readonly object _lockObject = new();

        /// <summary>
        /// Registers a drop target
        /// </summary>
        public void RegisterDropTarget(DropTarget target)
        {
            lock (_lockObject)
            {
                _dropTargets[target.Id] = target;
            }
        }

        /// <summary>
        /// Registers a drop validator
        /// </summary>
        public void RegisterDropValidator(ActionFlowId targetId, Func<DragItem, DropTarget, bool> validator)
        {
            lock (_lockObject)
            {
                if (!_dropValidators.ContainsKey(targetId))
                    _dropValidators[targetId] = new();
                    
                _dropValidators[targetId].Add(validator);
            }
        }

        /// <summary>
        /// Validates if an item can be dropped on a target
        /// </summary>
        public bool CanDrop(DragItem item, DropTarget target)
        {
            lock (_lockObject)
            {
                if (!target.AcceptedItemTypes.Contains(item.ItemType))
                    return false;

                if (_dropValidators.TryGetValue(target.Id, out var validators))
                {
                    foreach (var validator in validators)
                    {
                        if (!validator(item, target))
                            return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Starts a drag operation
        /// </summary>
        public DragOperation StartDrag(DragItem item, ActionFlowId? sourceContainerId = null)
        {
            return new DragOperation
            {
                Item = item,
                SourceContainerId = sourceContainerId
            };
        }

        /// <summary>
        /// Validates drop on target
        /// </summary>
        public DragOperation ValidateDrop(DragOperation operation, ActionFlowId targetId)
        {
            DropTarget? target;
            lock (_lockObject)
            {
                _dropTargets.TryGetValue(targetId, out target);
            }

            if (target == null)
            {
                operation.IsValid = false;
                operation.ValidationMessage = "Target not found";
                return operation;
            }

            operation.TargetContainerId = targetId;
            operation.IsValid = CanDrop(operation.Item, target);
            operation.ValidationMessage = operation.IsValid ? "Valid drop target" : "Invalid drop target";

            return operation;
        }

        /// <summary>
        /// Completes a drag operation
        /// </summary>
        public async System.Threading.Tasks.Task CompleteDragAsync(DragOperation operation)
        {
            foreach (var listener in _dropListeners.ToList())
            {
                await listener(operation);
            }
        }

        /// <summary>
        /// Subscribes to drop events
        /// </summary>
        public void SubscribeToDrops(Func<DragOperation, System.Threading.Tasks.Task> listener)
        {
            lock (_lockObject)
            {
                _dropListeners.Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes from drop events
        /// </summary>
        public void UnsubscribeFromDrops(Func<DragOperation, System.Threading.Tasks.Task> listener)
        {
            lock (_lockObject)
            {
                _dropListeners.Remove(listener);
            }
        }
    }

    /// <summary>
    /// Workflow builder using drag and drop
    /// </summary>
    public class WorkflowDragDropBuilder
    {
        private readonly DragDropEngine _dragDropEngine;
        private readonly List<ActionStep> _steps = new();
        private readonly List<WorkflowTransition> _transitions = new();
        
        private readonly object _lockObject = new();

        public WorkflowDragDropBuilder()
        {
            _dragDropEngine = new DragDropEngine();
            InitializeWorkflowTargets();
        }

        /// <summary>
        /// Initializes drop targets for workflow building
        /// </summary>
        private void InitializeWorkflowTargets()
        {
            _dragDropEngine.RegisterDropTarget(new DropTarget
            {
                Id = ActionFlowId.New(),
                TargetType = "WorkflowCanvas",
                AcceptedItemTypes = new() { "ActionStep", "Connector" }
            });

            _dragDropEngine.RegisterDropTarget(new DropTarget
            {
                Id = ActionFlowId.New(),
                TargetType = "StepConnection",
                AcceptedItemTypes = new() { "Connector" }
            });
        }

        /// <summary>
        /// Adds a step to the workflow
        /// </summary>
        public void AddStep(ActionStep step)
        {
            lock (_lockObject)
            {
                _steps.Add(step);
            }
        }

        /// <summary>
        /// Connects two steps
        /// </summary>
        public void ConnectSteps(ActionFlowId fromStepId, ActionFlowId toStepId, string? condition = null)
        {
            lock (_lockObject)
            {
                _transitions.Add(new WorkflowTransition
                {
                    FromStepId = fromStepId,
                    ToStepId = toStepId,
                    TransitionCondition = condition ?? ""
                });
            }
        }

        /// <summary>
        /// Removes a step from the workflow
        /// </summary>
        public void RemoveStep(ActionFlowId stepId)
        {
            lock (_lockObject)
            {
                _steps.RemoveAll(s => s.Id == stepId);
                _transitions.RemoveAll(t => t.FromStepId == stepId || t.ToStepId == stepId);
            }
        }

        /// <summary>
        /// Builds the workflow definition
        /// </summary>
        public WorkflowDefinition Build(string name, string description)
        {
            lock (_lockObject)
            {
                var workflow = new WorkflowDefinition
                {
                    Name = name,
                    Description = description,
                    Steps = new List<ActionStep>(_steps),
                    Transitions = new List<WorkflowTransition>(_transitions)
                };

                if (_steps.Count > 0)
                    workflow.InitialStepId = _steps[0].Id;

                return workflow;
            }
        }

        /// <summary>
        /// Gets all steps in the workflow
        /// </summary>
        public List<ActionStep> GetSteps()
        {
            lock (_lockObject)
            {
                return new List<ActionStep>(_steps);
            }
        }

        /// <summary>
        /// Gets all transitions in the workflow
        /// </summary>
        public List<WorkflowTransition> GetTransitions()
        {
            lock (_lockObject)
            {
                return new List<WorkflowTransition>(_transitions);
            }
        }

        /// <summary>
        /// Clears the workflow
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _steps.Clear();
                _transitions.Clear();
            }
        }
    }

    /// <summary>
    /// Page builder using drag and drop
    /// </summary>
    public class PageDragDropBuilder
    {
        private readonly DragDropEngine _dragDropEngine;
        private readonly List<PageElement> _elements = new();
        
        private readonly object _lockObject = new();

        public PageDragDropBuilder()
        {
            _dragDropEngine = new DragDropEngine();
            InitializePageTargets();
        }

        /// <summary>
        /// Initializes drop targets for page building
        /// </summary>
        private void InitializePageTargets()
        {
            _dragDropEngine.RegisterDropTarget(new DropTarget
            {
                Id = ActionFlowId.New(),
                TargetType = "PageCanvas",
                AcceptedItemTypes = new() { "PageElement", "Widget", "Component" }
            });
        }

        /// <summary>
        /// Adds an element to the page
        /// </summary>
        public void AddElement(PageElement element)
        {
            lock (_lockObject)
            {
                _elements.Add(element);
            }
        }

        /// <summary>
        /// Removes an element from the page
        /// </summary>
        public void RemoveElement(ActionFlowId elementId)
        {
            lock (_lockObject)
            {
                _elements.RemoveAll(e => e.Id == elementId);
            }
        }

        /// <summary>
        /// Reorders elements
        /// </summary>
        public void ReorderElements(ActionFlowId elementId, int newIndex)
        {
            lock (_lockObject)
            {
                var element = _elements.FirstOrDefault(e => e.Id == elementId);
                if (element != null)
                {
                    _elements.Remove(element);
                    if (newIndex >= _elements.Count)
                        _elements.Add(element);
                    else
                        _elements.Insert(newIndex, element);
                }
            }
        }

        /// <summary>
        /// Builds the page configuration
        /// </summary>
        public PageConfiguration Build(string name, string description)
        {
            lock (_lockObject)
            {
                return new PageConfiguration
                {
                    Name = name,
                    Description = description,
                    Elements = new List<PageElement>(_elements)
                };
            }
        }

        /// <summary>
        /// Gets all elements in the page
        /// </summary>
        public List<PageElement> GetElements()
        {
            lock (_lockObject)
            {
                return new List<PageElement>(_elements);
            }
        }

        /// <summary>
        /// Clears the page
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _elements.Clear();
            }
        }
    }
}
