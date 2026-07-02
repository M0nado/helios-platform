using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.UI
{
    /// <summary>
    /// STREAM C: AppStateManagement - Redux-like centralized state management
    /// Reduces component coupling and consolidates UI logic
    /// Expected: Additional 10-15% code reduction
    /// </summary>
    public interface IAppState
    {
        T GetState<T>(string key) where T : class;
        void SetState<T>(string key, T value) where T : class;
        void Subscribe<T>(string key, Action<T> callback) where T : class;
        void Unsubscribe<T>(string key, Action<T> callback) where T : class;
    }

    public interface IReducer
    {
        Type StateType { get; }
        object Reduce(object state, IAction action);
    }

    public interface IAction
    {
        string Type { get; }
        Dictionary<string, object> Payload { get; }
    }

    public class AppStateManager : IAppState
    {
        private readonly Dictionary<string, object> _states = new();
        private readonly Dictionary<string, List<Delegate>> _subscribers = new();
        private readonly Dictionary<string, object> _reducers = new();
        private readonly ReaderWriterLockSlim _stateLock = new();
        private readonly object _subscriberLock = new();

        public AppStateManager()
        {
            // Initialize default state
            _states["root"] = new Dictionary<string, object>();
        }

        public void RegisterReducer<TState, TReducer>(string key, TReducer reducer) 
            where TState : class 
            where TReducer : IReducer
        {
            lock (_subscriberLock)
            {
                _reducers[key] = reducer;
            }
        }

        public T GetState<T>(string key) where T : class
        {
            _stateLock.EnterReadLock();
            try
            {
                if (_states.TryGetValue(key, out var state))
                    return state as T;
                return null;
            }
            finally { _stateLock.ExitReadLock(); }
        }

        public void SetState<T>(string key, T value) where T : class
        {
            _stateLock.EnterWriteLock();
            try
            {
                _states[key] = value;
            }
            finally { _stateLock.ExitWriteLock(); }

            NotifySubscribers(key, value);
        }

        public void Subscribe<T>(string key, Action<T> callback) where T : class
        {
            lock (_subscriberLock)
            {
                if (!_subscribers.ContainsKey(key))
                    _subscribers[key] = new();
                _subscribers[key].Add(callback);
            }
        }

        public void Unsubscribe<T>(string key, Action<T> callback) where T : class
        {
            lock (_subscriberLock)
            {
                if (_subscribers.TryGetValue(key, out var callbacks))
                {
                    callbacks.Remove(callback);
                }
            }
        }

        public async Task DispatchAsync(IAction action)
        {
            // Find applicable reducer
            var reducerKey = action.Type.Split(':')[0];
            
            if (_reducers.TryGetValue(reducerKey, out var reducerObj))
            {
                var reducer = reducerObj as IReducer;
                _stateLock.EnterReadLock();
                object currentState;
                try
                {
                    _states.TryGetValue(reducerKey, out currentState);
                }
                finally { _stateLock.ExitReadLock(); }

                var newState = reducer.Reduce(currentState, action);
                SetState(reducerKey, newState);
            }

            await Task.CompletedTask;
        }

        private void NotifySubscribers<T>(string key, T value) where T : class
        {
            List<Delegate> callbacks;
            lock (_subscriberLock)
            {
                if (!_subscribers.TryGetValue(key, out callbacks))
                    return;
                callbacks = new(callbacks);
            }

            foreach (var callback in callbacks)
            {
                var action = callback as Action<T>;
                action?.Invoke(value);
            }
        }
    }

    public class AppAction : IAction
    {
        public string Type { get; set; }
        public Dictionary<string, object> Payload { get; set; } = new();

        public static AppAction Create(string type, Dictionary<string, object> payload = null)
        {
            return new AppAction 
            { 
                Type = type, 
                Payload = payload ?? new() 
            };
        }
    }

    public abstract class BaseReducer<TState> : IReducer where TState : class, new()
    {
        public Type StateType => typeof(TState);

        public object Reduce(object state, IAction action)
        {
            var typedState = (state as TState) ?? new TState();
            return ReduceInternal(typedState, action);
        }

        protected abstract TState ReduceInternal(TState state, IAction action);
    }

    public class UIComponentState
    {
        public string ComponentId { get; set; }
        public Dictionary<string, object> Props { get; set; } = new();
        public bool IsVisible { get; set; } = true;
        public Dictionary<string, object> LocalState { get; set; } = new();
    }

    public class ComponentReducer : BaseReducer<UIComponentState>
    {
        protected override UIComponentState ReduceInternal(UIComponentState state, IAction action)
        {
            return action.Type switch
            {
                "ui:set_prop" => SetProp(state, action),
                "ui:set_local_state" => SetLocalState(state, action),
                "ui:toggle_visibility" => ToggleVisibility(state),
                _ => state
            };
        }

        private UIComponentState SetProp(UIComponentState state, IAction action)
        {
            if (action.Payload.TryGetValue("key", out var keyObj) && 
                action.Payload.TryGetValue("value", out var value))
            {
                state.Props[keyObj.ToString()] = value;
            }
            return state;
        }

        private UIComponentState SetLocalState(UIComponentState state, IAction action)
        {
            if (action.Payload.TryGetValue("key", out var keyObj) && 
                action.Payload.TryGetValue("value", out var value))
            {
                state.LocalState[keyObj.ToString()] = value;
            }
            return state;
        }

        private UIComponentState ToggleVisibility(UIComponentState state)
        {
            state.IsVisible = !state.IsVisible;
            return state;
        }
    }

    public class StateSelector<T> where T : class
    {
        private readonly IAppState _state;
        private readonly string _key;
        private readonly Func<T, T> _selector;

        public StateSelector(IAppState state, string key, Func<T, T> selector = null)
        {
            _state = state;
            _key = key;
            _selector = selector ?? (x => x);
        }

        public T Select()
        {
            var state = _state.GetState<T>(_key);
            return _selector(state);
        }

        public void Subscribe(Action<T> callback)
        {
            _state.Subscribe<T>(_key, state =>
            {
                callback(_selector(state));
            });
        }
    }

    public class AppStateContext
    {
        private readonly AppStateManager _manager = new();
        private readonly Dictionary<string, StateSelector<dynamic>> _selectors = new();
        private readonly object _selectorLock = new();

        public IAppState State => _manager;

        public void RegisterComponent(string componentId)
        {
            var componentState = new UIComponentState { ComponentId = componentId };
            _manager.SetState($"component:{componentId}", componentState);
        }

        public UIComponentState GetComponentState(string componentId)
        {
            return _manager.GetState<UIComponentState>($"component:{componentId}");
        }

        public async Task DispatchAsync(IAction action)
        {
            await _manager.DispatchAsync(action);
        }

        public StateSelector<T> CreateSelector<T>(string key, Func<T, T> selector = null) where T : class
        {
            return new StateSelector<T>(_manager, key, selector);
        }
    }

    public class MemoizedSelector<TState, TResult> where TState : class
    {
        private readonly Func<TState, TResult> _selector;
        private TResult _cachedResult;
        private TState _previousState;
        private readonly object _cacheLock = new();

        public MemoizedSelector(Func<TState, TResult> selector)
        {
            _selector = selector;
        }

        public TResult Select(TState state)
        {
            lock (_cacheLock)
            {
                if (state != _previousState)
                {
                    _cachedResult = _selector(state);
                    _previousState = state;
                }
                return _cachedResult;
            }
        }
    }
}
