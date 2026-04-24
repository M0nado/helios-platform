// ═══════════════════════════════════════════════════════════════════════════
// Plugin System - Core Interface Definitions
// Provides base contracts for extensible plugin architecture
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Plugins.Interfaces
{
    /// <summary>
    /// Metadata attributes for plugin discovery and validation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PluginAttribute : Attribute
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; }
        public string Description { get; }
        public string[] Dependencies { get; }
        public PluginCategory Category { get; }

        public PluginAttribute(
            string id,
            string name,
            string version,
            string author,
            string description,
            PluginCategory category = PluginCategory.General,
            string[] dependencies = null)
        {
            Id = id;
            Name = name;
            Version = version;
            Author = author;
            Description = description;
            Category = category;
            Dependencies = dependencies ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Plugin category enumeration for classification
    /// </summary>
    public enum PluginCategory
    {
        General = 0,
        UI = 1,
        Analyzer = 2,
        Optimizer = 3,
        Utility = 4,
        Diagnostic = 5,
        Integration = 6,
        Security = 7
    }

    /// <summary>
    /// Plugin execution status
    /// </summary>
    public enum PluginStatus
    {
        NotLoaded = 0,
        Loading = 1,
        Loaded = 2,
        Running = 3,
        Error = 4,
        Disabled = 5,
        Unloading = 6,
        Unloaded = 7
    }

    /// <summary>
    /// Base interface for all plugins
    /// All plugins must implement this contract
    /// </summary>
    public interface IPlugin : IDisposable
    {
        /// <summary>
        /// Gets the unique plugin identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the plugin display name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the plugin version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the plugin author/maintainer
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Gets the plugin description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the plugin category
        /// </summary>
        PluginCategory Category { get; }

        /// <summary>
        /// Gets the list of plugin dependencies
        /// </summary>
        IReadOnlyList<string> Dependencies { get; }

        /// <summary>
        /// Gets the current execution status
        /// </summary>
        PluginStatus Status { get; }

        /// <summary>
        /// Initializes the plugin with context
        /// Called once during plugin load
        /// </summary>
        Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the plugin execution
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the plugin execution
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a configuration interface if supported
        /// </summary>
        IPluginConfiguration GetConfiguration();

        /// <summary>
        /// Validates plugin integrity and security
        /// </summary>
        Task<bool> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when plugin status changes
        /// </summary>
        event EventHandler<PluginStatusChangedEventArgs> StatusChanged;
    }

    /// <summary>
    /// Plugin context passed during initialization
    /// Provides access to host services and logging
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Gets the host service provider
        /// </summary>
        IServiceProvider HostServices { get; }

        /// <summary>
        /// Gets the plugin configuration directory
        /// </summary>
        string ConfigurationPath { get; }

        /// <summary>
        /// Gets the plugin data directory
        /// </summary>
        string DataPath { get; }

        /// <summary>
        /// Gets the logger instance
        /// </summary>
        IPluginLogger Logger { get; }

        /// <summary>
        /// Gets plugin metadata
        /// </summary>
        IPluginMetadata Metadata { get; }
    }

    /// <summary>
    /// Plugin metadata for discovery and management
    /// </summary>
    public interface IPluginMetadata
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        string Author { get; }
        string Description { get; }
        PluginCategory Category { get; }
        IReadOnlyList<string> Dependencies { get; }
        DateTime LoadedTime { get; }
        string AssemblyPath { get; }
        bool IsEnabled { get; }
        string Signature { get; }
    }

    /// <summary>
    /// Plugin configuration interface
    /// Allows plugins to manage their settings
    /// </summary>
    public interface IPluginConfiguration
    {
        /// <summary>
        /// Gets a configuration value
        /// </summary>
        object GetValue(string key);

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        void SetValue(string key, object value);

        /// <summary>
        /// Saves configuration to persistent storage
        /// </summary>
        Task SaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads configuration from persistent storage
        /// </summary>
        Task LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all configuration keys
        /// </summary>
        IReadOnlyList<string> GetAllKeys();

        /// <summary>
        /// Resets to default configuration
        /// </summary>
        void ResetToDefaults();
    }

    /// <summary>
    /// Plugin logging interface
    /// </summary>
    public interface IPluginLogger
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, Exception ex = null, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogTrace(string message, params object[] args);
    }

    /// <summary>
    /// Event args for plugin status changes
    /// </summary>
    public class PluginStatusChangedEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public PluginStatus OldStatus { get; set; }
        public PluginStatus NewStatus { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
