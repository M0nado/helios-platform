using System;
using System.Collections.Generic;
using System.Linq;
using HELIOS.Platform.Core.ActionFlow.Models;
using HELIOS.Platform.Core.ActionFlow.StateManagement;

namespace HELIOS.Platform.Core.ActionFlow.UndoRedo
{
    /// <summary>
    /// Represents an operation that can be undone and redone
    /// </summary>
    public interface IUndoableOperation
    {
        string OperationId { get; }
        string OperationName { get; }
        DateTime Timestamp { get; }

        void Execute();
        void Undo();
        void Redo();

        bool CanCombine(IUndoableOperation other);
        void Combine(IUndoableOperation other);
    }

    /// <summary>
    /// Base class for undoable operations
    /// </summary>
    public abstract class UndoableOperationBase : IUndoableOperation
    {
        public string OperationId { get; set; } = Guid.NewGuid().ToString();
        public abstract string OperationName { get; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public abstract void Execute();
        public abstract void Undo();
        public abstract void Redo();

        public virtual bool CanCombine(IUndoableOperation other) => false;
        public virtual void Combine(IUndoableOperation other) { }
    }

    /// <summary>
    /// Undo/redo stack manager
    /// </summary>
    public class UndoRedoStack
    {
        private readonly Stack<IUndoableOperation> _undoStack = new();
        private readonly Stack<IUndoableOperation> _redoStack = new();
        private readonly object _lockObject = new();

        private readonly int _maxStackSize;
        private readonly List<Action<UndoRedoState>> _listeners = new();

        public int UndoCount
        {
            get { lock (_lockObject) { return _undoStack.Count; } }
        }

        public int RedoCount
        {
            get { lock (_lockObject) { return _redoStack.Count; } }
        }

        public UndoRedoStack(int maxStackSize = 100)
        {
            _maxStackSize = maxStackSize;
        }

        /// <summary>
        /// Records an operation in the undo stack
        /// </summary>
        public void RecordOperation(IUndoableOperation operation)
        {
            lock (_lockObject)
            {
                // Try to combine with the last operation if possible
                if (_undoStack.TryPeek(out var lastOp) && lastOp.CanCombine(operation))
                {
                    lastOp.Combine(operation);
                }
                else
                {
                    operation.Execute();
                    _undoStack.Push(operation);

                    // Clear redo stack when new operation is executed
                    _redoStack.Clear();

                    // Maintain stack size
                    while (_undoStack.Count > _maxStackSize)
                    {
                        _undoStack.Pop();
                    }
                }

                NotifyListeners();
            }
        }

        /// <summary>
        /// Undoes the last operation
        /// </summary>
        public bool Undo()
        {
            lock (_lockObject)
            {
                if (_undoStack.Count == 0)
                    return false;

                var operation = _undoStack.Pop();
                operation.Undo();
                _redoStack.Push(operation);

                NotifyListeners();
                return true;
            }
        }

        /// <summary>
        /// Redoes the last undone operation
        /// </summary>
        public bool Redo()
        {
            lock (_lockObject)
            {
                if (_redoStack.Count == 0)
                    return false;

                var operation = _redoStack.Pop();
                operation.Redo();
                _undoStack.Push(operation);

                NotifyListeners();
                return true;
            }
        }

        /// <summary>
        /// Undoes multiple operations
        /// </summary>
        public int UndoMultiple(int count)
        {
            int undoneCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (Undo())
                    undoneCount++;
                else
                    break;
            }
            return undoneCount;
        }

        /// <summary>
        /// Redoes multiple operations
        /// </summary>
        public int RedoMultiple(int count)
        {
            int redoneCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (Redo())
                    redoneCount++;
                else
                    break;
            }
            return redoneCount;
        }

        /// <summary>
        /// Clears all history
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _undoStack.Clear();
                _redoStack.Clear();
                NotifyListeners();
            }
        }

        /// <summary>
        /// Gets undo history
        /// </summary>
        public List<IUndoableOperation> GetUndoHistory()
        {
            lock (_lockObject)
            {
                return _undoStack.ToList();
            }
        }

        /// <summary>
        /// Gets redo history
        /// </summary>
        public List<IUndoableOperation> GetRedoHistory()
        {
            lock (_lockObject)
            {
                return _redoStack.ToList();
            }
        }

        /// <summary>
        /// Subscribes to changes
        /// </summary>
        public void Subscribe(Action<UndoRedoState> listener)
        {
            lock (_lockObject)
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes from changes
        /// </summary>
        public void Unsubscribe(Action<UndoRedoState> listener)
        {
            lock (_lockObject)
            {
                _listeners.Remove(listener);
            }
        }

        private void NotifyListeners()
        {
            var state = new UndoRedoState
            {
                CanUndo = _undoStack.Count > 0,
                CanRedo = _redoStack.Count > 0,
                UndoCount = _undoStack.Count,
                RedoCount = _redoStack.Count,
                LastUndoOperation = _undoStack.FirstOrDefault(),
                LastRedoOperation = _redoStack.FirstOrDefault()
            };

            foreach (var listener in _listeners.ToList())
            {
                listener(state);
            }
        }
    }

    /// <summary>
    /// Represents the state of the undo/redo stacks
    /// </summary>
    public class UndoRedoState
    {
        public bool CanUndo { get; set; }
        public bool CanRedo { get; set; }
        public int UndoCount { get; set; }
        public int RedoCount { get; set; }
        public IUndoableOperation? LastUndoOperation { get; set; }
        public IUndoableOperation? LastRedoOperation { get; set; }
    }

    /// <summary>
    /// Operation for adding a workflow
    /// </summary>
    public class AddWorkflowOperation : UndoableOperationBase
    {
        private readonly StateStore _stateStore;
        private readonly WorkflowDefinition _workflow;

        public override string OperationName => $"Add Workflow: {_workflow.Name}";

        public AddWorkflowOperation(StateStore stateStore, WorkflowDefinition workflow)
        {
            _stateStore = stateStore;
            _workflow = workflow;
        }

        public override void Execute()
        {
            _stateStore.Dispatch(new AddWorkflowAction(_workflow));
        }

        public override void Undo()
        {
            _stateStore.Dispatch(new DeleteWorkflowAction(_workflow.Id));
        }

        public override void Redo()
        {
            Execute();
        }
    }

    /// <summary>
    /// Operation for updating a workflow
    /// </summary>
    public class UpdateWorkflowOperation : UndoableOperationBase
    {
        private readonly StateStore _stateStore;
        private readonly WorkflowDefinition _newWorkflow;
        private readonly WorkflowDefinition _previousWorkflow;

        public override string OperationName => $"Update Workflow: {_newWorkflow.Name}";

        public UpdateWorkflowOperation(StateStore stateStore, WorkflowDefinition newWorkflow, WorkflowDefinition previousWorkflow)
        {
            _stateStore = stateStore;
            _newWorkflow = newWorkflow;
            _previousWorkflow = previousWorkflow;
        }

        public override void Execute()
        {
            _stateStore.Dispatch(new UpdateWorkflowAction(_newWorkflow));
        }

        public override void Undo()
        {
            _stateStore.Dispatch(new UpdateWorkflowAction(_previousWorkflow));
        }

        public override void Redo()
        {
            Execute();
        }

        public override bool CanCombine(IUndoableOperation other) =>
            other is UpdateWorkflowOperation uwo && uwo._newWorkflow.Id == _newWorkflow.Id;

        public override void Combine(IUndoableOperation other)
        {
            if (other is UpdateWorkflowOperation uwo)
            {
                // Keep the new workflow, discard the intermediate one
            }
        }
    }

    /// <summary>
    /// Operation for deleting a workflow
    /// </summary>
    public class DeleteWorkflowOperation : UndoableOperationBase
    {
        private readonly StateStore _stateStore;
        private readonly WorkflowDefinition _workflow;

        public override string OperationName => $"Delete Workflow: {_workflow.Name}";

        public DeleteWorkflowOperation(StateStore stateStore, WorkflowDefinition workflow)
        {
            _stateStore = stateStore;
            _workflow = workflow;
        }

        public override void Execute()
        {
            _stateStore.Dispatch(new DeleteWorkflowAction(_workflow.Id));
        }

        public override void Undo()
        {
            _stateStore.Dispatch(new AddWorkflowAction(_workflow));
        }

        public override void Redo()
        {
            Execute();
        }
    }

    /// <summary>
    /// Operation for adding a page
    /// </summary>
    public class AddPageOperation : UndoableOperationBase
    {
        private readonly StateStore _stateStore;
        private readonly PageConfiguration _page;

        public override string OperationName => $"Add Page: {_page.Name}";

        public AddPageOperation(StateStore stateStore, PageConfiguration page)
        {
            _stateStore = stateStore;
            _page = page;
        }

        public override void Execute()
        {
            _stateStore.Dispatch(new AddPageAction(_page));
        }

        public override void Undo()
        {
            _stateStore.Dispatch(new DeletePageAction(_page.Id));
        }

        public override void Redo()
        {
            Execute();
        }
    }

    /// <summary>
    /// Operation for updating a page
    /// </summary>
    public class UpdatePageOperation : UndoableOperationBase
    {
        private readonly StateStore _stateStore;
        private readonly PageConfiguration _newPage;
        private readonly PageConfiguration _previousPage;

        public override string OperationName => $"Update Page: {_newPage.Name}";

        public UpdatePageOperation(StateStore stateStore, PageConfiguration newPage, PageConfiguration previousPage)
        {
            _stateStore = stateStore;
            _newPage = newPage;
            _previousPage = previousPage;
        }

        public override void Execute()
        {
            _stateStore.Dispatch(new UpdatePageAction(_newPage));
        }

        public override void Undo()
        {
            _stateStore.Dispatch(new UpdatePageAction(_previousPage));
        }

        public override void Redo()
        {
            Execute();
        }

        public override bool CanCombine(IUndoableOperation other) =>
            other is UpdatePageOperation upo && upo._newPage.Id == _newPage.Id;
    }

    #region Action Classes

    public class AddWorkflowAction : ActionFlowActionBase
    {
        private readonly WorkflowDefinition _workflow;

        public override string Type => WorkflowActions.ADD_WORKFLOW;
        public override Dictionary<string, object> Payload => new() { { "workflow", _workflow } };

        public AddWorkflowAction(WorkflowDefinition workflow)
        {
            _workflow = workflow;
        }
    }

    public class UpdateWorkflowAction : ActionFlowActionBase
    {
        private readonly WorkflowDefinition _workflow;

        public override string Type => WorkflowActions.UPDATE_WORKFLOW;
        public override Dictionary<string, object> Payload => new() { { "workflow", _workflow } };

        public UpdateWorkflowAction(WorkflowDefinition workflow)
        {
            _workflow = workflow;
        }
    }

    public class DeleteWorkflowAction : ActionFlowActionBase
    {
        private readonly ActionFlowId _workflowId;

        public override string Type => WorkflowActions.DELETE_WORKFLOW;
        public override Dictionary<string, object> Payload => new() { { "workflowId", _workflowId } };

        public DeleteWorkflowAction(ActionFlowId workflowId)
        {
            _workflowId = workflowId;
        }
    }

    public class AddPageAction : ActionFlowActionBase
    {
        private readonly PageConfiguration _page;

        public override string Type => PageActions.ADD_PAGE;
        public override Dictionary<string, object> Payload => new() { { "page", _page } };

        public AddPageAction(PageConfiguration page)
        {
            _page = page;
        }
    }

    public class UpdatePageAction : ActionFlowActionBase
    {
        private readonly PageConfiguration _page;

        public override string Type => PageActions.UPDATE_PAGE;
        public override Dictionary<string, object> Payload => new() { { "page", _page } };

        public UpdatePageAction(PageConfiguration page)
        {
            _page = page;
        }
    }

    public class DeletePageAction : ActionFlowActionBase
    {
        private readonly ActionFlowId _pageId;

        public override string Type => PageActions.DELETE_PAGE;
        public override Dictionary<string, object> Payload => new() { { "pageId", _pageId } };

        public DeletePageAction(ActionFlowId pageId)
        {
            _pageId = pageId;
        }
    }

    #endregion
}
