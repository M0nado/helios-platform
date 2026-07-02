using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonadoBlade.GUI.StateManagement
{
    /// <summary>
    /// Redux-like centralized state management for the entire application.
    /// Provides actions, reducers, selectors, and state persistence.
    /// </summary>
    public class AppStateManagement
    {
        private AppState _currentState;
        private readonly List<Action<AppState>> _subscribers = new();
        private readonly Dictionary<string, Func<object, AppState, AppState>> _reducers = new();
        private readonly Dictionary<string, object> _selectorCache = new();
        private bool _isDispatching;

        public event Action<AppState> StateChanged;

        public AppStateManagement()
        {
            _currentState = new AppState();
            RegisterDefaultReducers();
        }

        /// <summary>
        /// Get the current application state (read-only snapshot)
        /// </summary>
        public AppState GetState() => _currentState.Clone();

        /// <summary>
        /// Dispatch an action to update state through reducers
        /// </summary>
        public void Dispatch(AppAction action)
        {
            if (_isDispatching)
                throw new InvalidOperationException("Cannot dispatch while dispatch is in progress");

            try
            {
                _isDispatching = true;
                var newState = _currentState.Clone();

                // Apply all matching reducers
                foreach (var reducerKey in _reducers.Keys.Where(k => action.Type.StartsWith(k)))
                {
                    var reducer = _reducers[reducerKey];
                    newState = reducer(action.Payload, newState);
                }

                // Only update if state actually changed (shallow comparison of key properties)
                if (!StateEquals(newState, _currentState))
                {
                    _currentState = newState;
                    _selectorCache.Clear();
                    OnStateChanged();
                }
            }
            finally
            {
                _isDispatching = false;
            }
        }

        /// <summary>
        /// Async dispatch for thunks
        /// </summary>
        public async Task DispatchAsync(Func<AppStateManagement, Task> thunk)
        {
            await thunk(this);
        }

        /// <summary>
        /// Subscribe to state changes
        /// </summary>
        public void Subscribe(Action<AppState> listener)
        {
            if (listener != null)
                _subscribers.Add(listener);
        }

        /// <summary>
        /// Unsubscribe from state changes
        /// </summary>
        public void Unsubscribe(Action<AppState> listener)
        {
            _subscribers.Remove(listener);
        }

        /// <summary>
        /// Register a reducer for a specific action type
        /// </summary>
        public void RegisterReducer(string actionType, Func<object, AppState, AppState> reducer)
        {
            _reducers[actionType] = reducer;
        }

        /// <summary>
        /// Selector with memoization for performance
        /// </summary>
        public T Select<T>(string key, Func<AppState, T> selector)
        {
            if (!_selectorCache.ContainsKey(key))
            {
                _selectorCache[key] = selector(_currentState);
            }
            return (T)_selectorCache[key];
        }

        /// <summary>
        /// Get state for a specific feature
        /// </summary>
        public DashboardState GetDashboardState() => _currentState.Dashboard;
        public SettingsState GetSettingsState() => _currentState.Settings;
        public PluginState GetPluginState() => _currentState.Plugins;
        public CloudSyncState GetCloudSyncState() => _currentState.CloudSync;

        /// <summary>
        /// Save state to persistent storage
        /// </summary>
        public async Task PersistState()
        {
            try
            {
                var serialized = SerializeState(_currentState);
                await Task.Run(() =>
                {
                    var statePath = GetStateFilePath();
                    System.IO.File.WriteAllText(statePath, serialized);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to persist state: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore state from persistent storage
        /// </summary>
        public async Task RestoreState()
        {
            try
            {
                var statePath = GetStateFilePath();
                if (System.IO.File.Exists(statePath))
                {
                    var serialized = await Task.Run(() => System.IO.File.ReadAllText(statePath));
                    _currentState = DeserializeState(serialized);
                    _selectorCache.Clear();
                    OnStateChanged();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore state: {ex.Message}");
            }
        }

        private void RegisterDefaultReducers()
        {
            // Dashboard reducer will be registered by DashboardReducer
            // Settings reducer will be registered by SettingsReducer
            // Plugin reducer will be registered by PluginReducer
            // CloudSync reducer will be registered by CloudSyncReducer
        }

        private bool StateEquals(AppState s1, AppState s2)
        {
            return s1.Dashboard.Equals(s2.Dashboard) &&
                   s1.Settings.Equals(s2.Settings) &&
                   s1.Plugins.Equals(s2.Plugins) &&
                   s1.CloudSync.Equals(s2.CloudSync);
        }

        private void OnStateChanged()
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber(_currentState);
            }
            StateChanged?.Invoke(_currentState);
        }

        private string SerializeState(AppState state)
        {
            // Simple JSON serialization (would use Json.NET in production)
            return System.Text.Json.JsonSerializer.Serialize(state);
        }

        private AppState DeserializeState(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
        }

        private string GetStateFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var monadoPath = System.IO.Path.Combine(appDataPath, "MonadoBlade");
            if (!System.IO.Directory.Exists(monadoPath))
                System.IO.Directory.CreateDirectory(monadoPath);
            return System.IO.Path.Combine(monadoPath, "appstate.json");
        }
    }

    /// <summary>
    /// Root application state
    /// </summary>
    public class AppState
    {
        public DashboardState Dashboard { get; set; } = new();
        public SettingsState Settings { get; set; } = new();
        public PluginState Plugins { get; set; } = new();
        public CloudSyncState CloudSync { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public AppState Clone()
        {
            return new AppState
            {
                Dashboard = Dashboard?.Clone() ?? new(),
                Settings = Settings?.Clone() ?? new(),
                Plugins = Plugins?.Clone() ?? new(),
                CloudSync = CloudSync?.Clone() ?? new(),
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Action dispatch payload
    /// </summary>
    public class AppAction
    {
        public string Type { get; set; }
        public object Payload { get; set; }

        public AppAction(string type, object payload = null)
        {
            Type = type;
            Payload = payload;
        }
    }

    /// <summary>
    /// Dashboard feature state
    /// </summary>
    public class DashboardState
    {
        public bool IsLoading { get; set; }
        public string CurrentView { get; set; } = "overview";
        public List<WidgetState> Widgets { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime LastRefresh { get; set; } = DateTime.UtcNow;

        public DashboardState Clone()
        {
            return new DashboardState
            {
                IsLoading = IsLoading,
                CurrentView = CurrentView,
                Widgets = Widgets.ConvertAll(w => w.Clone()),
                Metrics = new Dictionary<string, object>(Metrics),
                LastRefresh = LastRefresh
            };
        }

        public override bool Equals(object obj)
        {
            return obj is DashboardState other &&
                   IsLoading == other.IsLoading &&
                   CurrentView == other.CurrentView &&
                   Widgets.Count == other.Widgets.Count &&
                   LastRefresh == other.LastRefresh;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsLoading, CurrentView, Widgets.Count, LastRefresh);
        }
    }

    /// <summary>
    /// Settings feature state
    /// </summary>
    public class SettingsState
    {
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "en";
        public bool AutoSave { get; set; } = true;
        public bool NotificationsEnabled { get; set; } = true;
        public Dictionary<string, object> UserPreferences { get; set; } = new();

        public SettingsState Clone()
        {
            return new SettingsState
            {
                Theme = Theme,
                Language = Language,
                AutoSave = AutoSave,
                NotificationsEnabled = NotificationsEnabled,
                UserPreferences = new Dictionary<string, object>(UserPreferences)
            };
        }

        public override bool Equals(object obj)
        {
            return obj is SettingsState other &&
                   Theme == other.Theme &&
                   Language == other.Language &&
                   AutoSave == other.AutoSave &&
                   NotificationsEnabled == other.NotificationsEnabled;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Theme, Language, AutoSave, NotificationsEnabled);
        }
    }

    /// <summary>
    /// Plugin system state
    /// </summary>
    public class PluginState
    {
        public List<PluginInfo> InstalledPlugins { get; set; } = new();
        public bool IsLoadingPlugins { get; set; }
        public string CurrentPlugin { get; set; }
        public Dictionary<string, object> PluginData { get; set; } = new();

        public PluginState Clone()
        {
            return new PluginState
            {
                InstalledPlugins = InstalledPlugins.ConvertAll(p => p.Clone()),
                IsLoadingPlugins = IsLoadingPlugins,
                CurrentPlugin = CurrentPlugin,
                PluginData = new Dictionary<string, object>(PluginData)
            };
        }

        public override bool Equals(object obj)
        {
            return obj is PluginState other &&
                   InstalledPlugins.Count == other.InstalledPlugins.Count &&
                   IsLoadingPlugins == other.IsLoadingPlugins &&
                   CurrentPlugin == other.CurrentPlugin;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(InstalledPlugins.Count, IsLoadingPlugins, CurrentPlugin);
        }
    }

    /// <summary>
    /// Cloud synchronization state
    /// </summary>
    public class CloudSyncState
    {
        public bool IsSyncing { get; set; }
        public string SyncStatus { get; set; } = "idle";
        public int SyncProgress { get; set; }
        public DateTime LastSyncTime { get; set; }
        public List<string> PendingChanges { get; set; } = new();
        public bool IsConnected { get; set; }

        public CloudSyncState Clone()
        {
            return new CloudSyncState
            {
                IsSyncing = IsSyncing,
                SyncStatus = SyncStatus,
                SyncProgress = SyncProgress,
                LastSyncTime = LastSyncTime,
                PendingChanges = new List<string>(PendingChanges),
                IsConnected = IsConnected
            };
        }

        public override bool Equals(object obj)
        {
            return obj is CloudSyncState other &&
                   IsSyncing == other.IsSyncing &&
                   SyncStatus == other.SyncStatus &&
                   SyncProgress == other.SyncProgress &&
                   IsConnected == other.IsConnected;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsSyncing, SyncStatus, SyncProgress, IsConnected);
        }
    }

    /// <summary>
    /// Widget state within dashboard
    /// </summary>
    public class WidgetState
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public bool IsVisible { get; set; } = true;
        public Dictionary<string, object> Config { get; set; } = new();

        public WidgetState Clone()
        {
            return new WidgetState
            {
                Id = Id,
                Type = Type,
                IsVisible = IsVisible,
                Config = new Dictionary<string, object>(Config)
            };
        }
    }

    /// <summary>
    /// Plugin information
    /// </summary>
    public class PluginInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime InstalledDate { get; set; }

        public PluginInfo Clone()
        {
            return new PluginInfo
            {
                Id = Id,
                Name = Name,
                Version = Version,
                IsEnabled = IsEnabled,
                InstalledDate = InstalledDate
            };
        }
    }

    /// <summary>
    /// Action type constants
    /// </summary>
    public static class ActionTypes
    {
        // Dashboard Actions
        public const string DASHBOARD_LOADING = "DASHBOARD/LOADING";
        public const string DASHBOARD_SET_VIEW = "DASHBOARD/SET_VIEW";
        public const string DASHBOARD_ADD_WIDGET = "DASHBOARD/ADD_WIDGET";
        public const string DASHBOARD_REMOVE_WIDGET = "DASHBOARD/REMOVE_WIDGET";
        public const string DASHBOARD_UPDATE_METRICS = "DASHBOARD/UPDATE_METRICS";

        // Settings Actions
        public const string SETTINGS_SET_THEME = "SETTINGS/SET_THEME";
        public const string SETTINGS_SET_LANGUAGE = "SETTINGS/SET_LANGUAGE";
        public const string SETTINGS_TOGGLE_AUTOSAVE = "SETTINGS/TOGGLE_AUTOSAVE";
        public const string SETTINGS_TOGGLE_NOTIFICATIONS = "SETTINGS/TOGGLE_NOTIFICATIONS";
        public const string SETTINGS_UPDATE_PREFERENCES = "SETTINGS/UPDATE_PREFERENCES";

        // Plugin Actions
        public const string PLUGIN_LOAD = "PLUGIN/LOAD";
        public const string PLUGIN_INSTALL = "PLUGIN/INSTALL";
        public const string PLUGIN_UNINSTALL = "PLUGIN/UNINSTALL";
        public const string PLUGIN_ENABLE = "PLUGIN/ENABLE";
        public const string PLUGIN_DISABLE = "PLUGIN/DISABLE";

        // CloudSync Actions
        public const string CLOUDSYNC_START = "CLOUDSYNC/START";
        public const string CLOUDSYNC_PROGRESS = "CLOUDSYNC/PROGRESS";
        public const string CLOUDSYNC_COMPLETE = "CLOUDSYNC/COMPLETE";
        public const string CLOUDSYNC_ERROR = "CLOUDSYNC/ERROR";
        public const string CLOUDSYNC_CONNECTION_CHANGED = "CLOUDSYNC/CONNECTION_CHANGED";
    }
}
