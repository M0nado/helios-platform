using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Plugins.Abstractions
{
    /// <summary>
    /// Core plugin interface that all plugins must implement
    /// </summary>
    public interface IPlugin : IDisposable
    {
        /// <summary>
        /// Unique identifier for the plugin
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Human-readable name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Current version of the plugin
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Plugin author
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Detailed description of plugin functionality
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Plugin lifecycle state
        /// </summary>
        PluginState State { get; }

        /// <summary>
        /// List of plugin IDs that this plugin depends on
        /// </summary>
        IReadOnlyList<PluginDependency> Dependencies { get; }

        /// <summary>
        /// Initialize the plugin with configuration
        /// </summary>
        Task InitializeAsync(IPluginContext context);

        /// <summary>
        /// Start the plugin (called after initialization)
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stop the plugin gracefully
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Reload the plugin configuration
        /// </summary>
        Task ReloadConfigAsync();

        /// <summary>
        /// Get plugin metadata
        /// </summary>
        IPluginMetadata GetMetadata();

        /// <summary>
        /// Execute a plugin command
        /// </summary>
        Task<PluginCommandResult> ExecuteCommandAsync(string commandName, Dictionary<string, object> parameters);

        /// <summary>
        /// Get the capabilities that this plugin provides
        /// </summary>
        IReadOnlyList<string> GetCapabilities();

        /// <summary>
        /// Health check for the plugin
        /// </summary>
        Task<PluginHealth> GetHealthAsync();

        /// <summary>
        /// Event raised when plugin state changes
        /// </summary>
        event EventHandler<PluginStateChangedEventArgs> StateChanged;
    }

    /// <summary>
    /// Plugin lifecycle states
    /// </summary>
    public enum PluginState
    {
        Created = 0,
        Initialized = 1,
        Running = 2,
        Stopped = 3,
        Failed = 4,
        Unloaded = 5
    }

    /// <summary>
    /// Represents a plugin dependency with version constraints
    /// </summary>
    public class PluginDependency
    {
        public string PluginId { get; set; }
        public string VersionConstraint { get; set; }
        public bool IsOptional { get; set; }

        public PluginDependency(string pluginId, string versionConstraint = "*", bool isOptional = false)
        {
            PluginId = pluginId;
            VersionConstraint = versionConstraint;
            IsOptional = isOptional;
        }
    }

    /// <summary>
    /// Plugin context providing access to platform services
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Plugin configuration
        /// </summary>
        IPluginConfiguration Configuration { get; }

        /// <summary>
        /// Service provider for dependency injection
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Plugin logger
        /// </summary>
        IPluginLogger Logger { get; }

        /// <summary>
        /// Get another plugin instance by ID
        /// </summary>
        IPlugin GetPlugin(string pluginId);

        /// <summary>
        /// Register a service exported by this plugin
        /// </summary>
        void RegisterService(string serviceName, object serviceInstance);

        /// <summary>
        /// Get a service provided by another plugin
        /// </summary>
        object GetService(string serviceName);

        /// <summary>
        /// Publish an event that other plugins can subscribe to
        /// </summary>
        Task PublishEventAsync(string eventName, object eventData);

        /// <summary>
        /// Subscribe to an event from another plugin
        /// </summary>
        void SubscribeToEvent(string eventName, Func<object, Task> handler);
    }

    /// <summary>
    /// Plugin logging interface
    /// </summary>
    public interface IPluginLogger
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception ex = null);
        void Critical(string message, Exception ex = null);
    }

    /// <summary>
    /// Plugin configuration interface
    /// </summary>
    public interface IPluginConfiguration
    {
        /// <summary>
        /// Get a configuration value by key
        /// </summary>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// Set a configuration value
        /// </summary>
        void Set<T>(string key, T value);

        /// <summary>
        /// Get all configuration as dictionary
        /// </summary>
        Dictionary<string, object> GetAll();

        /// <summary>
        /// Reload configuration from source
        /// </summary>
        Task ReloadAsync();
    }

    /// <summary>
    /// Plugin metadata
    /// </summary>
    public interface IPluginMetadata
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        string Author { get; }
        string Description { get; }
        string License { get; }
        string Homepage { get; }
        Dictionary<string, object> Tags { get; }
    }

    /// <summary>
    /// Result of a plugin command execution
    /// </summary>
    public class PluginCommandResult
    {
        public bool Success { get; set; }
        public object Data { get; set; }
        public string ErrorMessage { get; set; }

        public static PluginCommandResult Ok(object data = null) =>
            new PluginCommandResult { Success = true, Data = data };

        public static PluginCommandResult Error(string message) =>
            new PluginCommandResult { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Plugin health status
    /// </summary>
    public class PluginHealth
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime LastCheck { get; set; }
    }

    /// <summary>
    /// Event args for plugin state changes
    /// </summary>
    public class PluginStateChangedEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public PluginState OldState { get; set; }
        public PluginState NewState { get; set; }
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; }
    }
}
