using System;
using System.Windows;
using System.Windows.Controls;
using MonadoBlade.GUI.StateManagement;

namespace MonadoBlade.GUI.Components.Base
{
    /// <summary>
    /// Base component class for all GUI components (150 LOC)
    /// Provides common functionality, state management binding, lifecycle hooks,
    /// and error handling to reduce duplication across 18+ components.
    /// </summary>
    public abstract class ComponentBase : UserControl
    {
        protected AppStateManagement StateManager { get; private set; }
        protected bool IsInitialized { get; private set; }
        protected Action<AppState> StateChangedCallback { get; set; }

        public event EventHandler ComponentInitialized;
        public event EventHandler ComponentDisposed;
        public event EventHandler<Exception> ComponentErrorOccurred;

        public ComponentBase()
        {
            Loaded += (s, e) => InitializeComponent();
            Unloaded += (s, e) => DisposeComponent();
        }

        /// <summary>
        /// Initialize component with state manager
        /// </summary>
        public virtual void SetStateManager(AppStateManagement stateManager)
        {
            StateManager = stateManager;
            if (StateManager != null)
            {
                StateManager.Subscribe(OnStateChanged);
                OnStateChanged(StateManager.GetState());
            }
        }

        /// <summary>
        /// Initialize component - override in derived classes
        /// </summary>
        protected virtual void InitializeComponent()
        {
            try
            {
                OnInitialize();
                IsInitialized = true;
                ComponentInitialized?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                HandleError(ex, "Initialization failed");
            }
        }

        /// <summary>
        /// Dispose component resources
        /// </summary>
        protected virtual void DisposeComponent()
        {
            try
            {
                if (StateManager != null && StateChangedCallback != null)
                {
                    StateManager.Unsubscribe(StateChangedCallback);
                }
                OnDispose();
                ComponentDisposed?.Invoke(this, EventArgs.Empty);
                IsInitialized = false;
            }
            catch (Exception ex)
            {
                HandleError(ex, "Disposal failed");
            }
        }

        /// <summary>
        /// Handle state changes from StateManager
        /// </summary>
        protected virtual void OnStateChanged(AppState newState)
        {
            StateChangedCallback?.Invoke(newState);
        }

        /// <summary>
        /// Get current state snapshot
        /// </summary>
        protected AppState GetCurrentState()
        {
            return StateManager?.GetState() ?? new AppState();
        }

        /// <summary>
        /// Dispatch action to state manager
        /// </summary>
        protected void DispatchAction(AppAction action)
        {
            StateManager?.Dispatch(action);
        }

        /// <summary>
        /// Dispatch async action (thunk)
        /// </summary>
        protected async void DispatchActionAsync(Func<AppStateManagement, System.Threading.Tasks.Task> thunk)
        {
            if (StateManager != null)
                await StateManager.DispatchAsync(thunk);
        }

        /// <summary>
        /// Error handling with logging
        /// </summary>
        protected virtual void HandleError(Exception ex, string context = "")
        {
            var message = string.IsNullOrEmpty(context) 
                ? ex.Message 
                : $"{context}: {ex.Message}";
            
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] ERROR: {message}");
            ComponentErrorOccurred?.Invoke(this, ex);
        }

        /// <summary>
        /// Override in derived classes for initialization logic
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// Override in derived classes for cleanup logic
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// Update UI on main thread
        /// </summary>
        protected void UpdateUI(Action updateAction)
        {
            try
            {
                Dispatcher.Invoke(updateAction);
            }
            catch (Exception ex)
            {
                HandleError(ex, "UI update failed");
            }
        }

        /// <summary>
        /// Validate required fields
        /// </summary>
        protected bool ValidateRequired(params (string fieldName, object value)[] fields)
        {
            foreach (var (name, value) in fields)
            {
                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    HandleError(new ArgumentNullException(name), $"Required field missing: {name}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Show error message to user
        /// </summary>
        protected void ShowError(string title, string message)
        {
            UpdateUI(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// Show info message to user
        /// </summary>
        protected void ShowInfo(string title, string message)
        {
            UpdateUI(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        /// <summary>
        /// Get component metadata
        /// </summary>
        public virtual ComponentMetadata GetMetadata()
        {
            return new ComponentMetadata
            {
                Name = GetType().Name,
                Version = "1.0",
                InitializedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Component metadata
    /// </summary>
    public class ComponentMetadata
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime InitializedAt { get; set; }
    }
}
