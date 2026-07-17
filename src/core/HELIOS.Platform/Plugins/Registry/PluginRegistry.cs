// ═══════════════════════════════════════════════════════════════════════════
// Plugin Registry - Plugin Lifecycle Management & Metadata Storage
// Manages plugin state, dependencies, events, and configuration
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Plugins.Interfaces;
using Microsoft.Extensions.Logging;
using PluginContract = HELIOS.Platform.Plugins.Interfaces.IPlugin;

namespace HELIOS.Platform.Plugins.Registry
{
    /// <summary>
    /// Registry for managing plugin lifecycle and metadata
    /// </summary>
    public class PluginRegistry : IPluginRegistry
    {
        private readonly ILogger<PluginRegistry> _logger;
        private readonly Dictionary<string, PluginRegistryEntry> _registry;
        private readonly Dictionary<string, List<string>> _dependencyGraph;
        private readonly object _lockObject = new();

        public event EventHandler<PluginRegistryEventArgs> PluginRegistered;
        public event EventHandler<PluginRegistryEventArgs> PluginUnregistered;
        public event EventHandler<PluginStatusEventArgs> PluginStatusChanged;
        public event EventHandler<PluginConfigurationEventArgs> PluginConfigurationChanged;

        public PluginRegistry(ILogger<PluginRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = new();
            _dependencyGraph = new();
        }

        public void RegisterPlugin(
            string pluginId,
            PluginContract plugin,
            PluginMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                throw new ArgumentNullException(nameof(pluginId));
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            lock (_lockObject)
            {
                if (_registry.ContainsKey(pluginId))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} is already registered");
                }

                var entry = new PluginRegistryEntry
                {
                    PluginId = pluginId,
                    Plugin = plugin,
                    Metadata = metadata,
                    Status = PluginStatus.Loaded,
                    IsEnabled = true,
                    RegisteredTime = DateTime.UtcNow,
                    Configuration = new PluginConfigurationData()
                };

                _registry[pluginId] = entry;

                // Update dependency graph
                foreach (var dependency in metadata.Dependencies)
                {
                    if (!_dependencyGraph.ContainsKey(dependency))
                    {
                        _dependencyGraph[dependency] = new();
                    }
                    _dependencyGraph[dependency].Add(pluginId);
                }

                _logger.LogInformation(
                    "Plugin {PluginId} registered: {PluginName} v{Version}",
                    pluginId,
                    metadata.Name,
                    metadata.Version);

                PluginRegistered?.Invoke(this, new PluginRegistryEventArgs
                {
                    PluginId = pluginId,
                    EventTime = DateTime.UtcNow
                });
            }
        }

        public void UnregisterPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                throw new ArgumentNullException(nameof(pluginId));

            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} not found");
                }

                _registry.Remove(pluginId);

                foreach (var dependency in entry.Metadata.Dependencies)
                {
                    if (_dependencyGraph.TryGetValue(dependency, out var dependents))
                    {
                        dependents.Remove(pluginId);
                        if (dependents.Count == 0)
                        {
                            _dependencyGraph.Remove(dependency);
                        }
                    }
                }

                _dependencyGraph.Remove(pluginId);

                _logger.LogInformation("Plugin {PluginId} unregistered", pluginId);

                PluginUnregistered?.Invoke(this, new PluginRegistryEventArgs
                {
                    PluginId = pluginId,
                    EventTime = DateTime.UtcNow
                });
            }
        }

        public void SetPluginStatus(
            string pluginId,
            PluginStatus newStatus,
            string message = null)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} not found");
                }

                var oldStatus = entry.Status;
                entry.Status = newStatus;
                entry.LastStatusChangeTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Plugin {PluginId} status changed: {OldStatus} -> {NewStatus}",
                    pluginId,
                    oldStatus,
                    newStatus);

                PluginStatusChanged?.Invoke(this, new PluginStatusEventArgs
                {
                    PluginId = pluginId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    Message = message,
                    EventTime = DateTime.UtcNow
                });
            }
        }

        public void EnablePlugin(string pluginId)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} not found");
                }

                if (entry.IsEnabled)
                {
                    _logger.LogWarning("Plugin {PluginId} is already enabled", pluginId);
                    return;
                }

                entry.IsEnabled = true;
                _logger.LogInformation("Plugin {PluginId} enabled", pluginId);
            }
        }

        public void DisablePlugin(string pluginId)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} not found");
                }

                if (!entry.IsEnabled)
                {
                    _logger.LogWarning("Plugin {PluginId} is already disabled", pluginId);
                    return;
                }

                entry.IsEnabled = false;
                _logger.LogInformation("Plugin {PluginId} disabled", pluginId);
            }
        }

        public IPluginMetadata GetPluginMetadata(string pluginId)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    return null;
                }

                return new PluginMetadataView(entry.Metadata);
            }
        }

        public IReadOnlyList<string> GetPluginDependencies(string pluginId)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    return new List<string>();
                }

                return entry.Metadata.Dependencies.ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<string> GetDependentPlugins(string pluginId)
        {
            lock (_lockObject)
            {
                if (!_dependencyGraph.TryGetValue(pluginId, out var dependents))
                {
                    return new List<string>();
                }

                return dependents.AsReadOnly();
            }
        }

        public bool CanLoadPlugin(string pluginId)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    return false;
                }

                // Check if all dependencies are loaded
                foreach (var dependency in entry.Metadata.Dependencies)
                {
                    if (!_registry.ContainsKey(dependency))
                    {
                        return false;
                    }

                    var depEntry = _registry[dependency];
                    if (!depEntry.IsEnabled || depEntry.Status != PluginStatus.Running)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public IReadOnlyDictionary<string, PluginContract> GetAllPlugins()
        {
            lock (_lockObject)
            {
                return _registry.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Plugin);
            }
        }

        public IReadOnlyDictionary<string, PluginRegistryEntry> GetRegistrySnapshot()
        {
            lock (_lockObject)
            {
                return _registry.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        public void SetPluginConfiguration(string pluginId, string key, object value)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} not found");
                }

                entry.Configuration.Settings[key] = value;
                entry.Configuration.LastModified = DateTime.UtcNow;

                PluginConfigurationChanged?.Invoke(this, new PluginConfigurationEventArgs
                {
                    PluginId = pluginId,
                    Key = key,
                    Value = value,
                    EventTime = DateTime.UtcNow
                });
            }
        }

        public object GetPluginConfiguration(string pluginId, string key)
        {
            lock (_lockObject)
            {
                if (!_registry.TryGetValue(pluginId, out var entry))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} not found");
                }

                entry.Configuration.Settings.TryGetValue(key, out var value);
                return value;
            }
        }

        public void ClearRegistry()
        {
            lock (_lockObject)
            {
                _registry.Clear();
                _dependencyGraph.Clear();
                _logger.LogInformation("Registry cleared");
            }
        }
    }

    /// <summary>
    /// Plugin registry interface
    /// </summary>
    public interface IPluginRegistry
    {
        void RegisterPlugin(string pluginId, PluginContract plugin, PluginMetadata metadata);
        void UnregisterPlugin(string pluginId);
        void SetPluginStatus(string pluginId, PluginStatus newStatus, string message = null);
        void EnablePlugin(string pluginId);
        void DisablePlugin(string pluginId);
        IPluginMetadata GetPluginMetadata(string pluginId);
        IReadOnlyList<string> GetPluginDependencies(string pluginId);
        IReadOnlyList<string> GetDependentPlugins(string pluginId);
        bool CanLoadPlugin(string pluginId);
        IReadOnlyDictionary<string, PluginContract> GetAllPlugins();
        IReadOnlyDictionary<string, PluginRegistryEntry> GetRegistrySnapshot();
        void SetPluginConfiguration(string pluginId, string key, object value);
        object GetPluginConfiguration(string pluginId, string key);
        void ClearRegistry();

        event EventHandler<PluginRegistryEventArgs> PluginRegistered;
        event EventHandler<PluginRegistryEventArgs> PluginUnregistered;
        event EventHandler<PluginStatusEventArgs> PluginStatusChanged;
        event EventHandler<PluginConfigurationEventArgs> PluginConfigurationChanged;
    }

    /// <summary>
    /// Plugin registry entry
    /// </summary>
    public class PluginRegistryEntry
    {
        public string PluginId { get; set; }
        public PluginContract Plugin { get; set; }
        public PluginMetadata Metadata { get; set; }
        public PluginStatus Status { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime RegisteredTime { get; set; }
        public DateTime LastStatusChangeTime { get; set; }
        public PluginConfigurationData Configuration { get; set; }
    }

    /// <summary>
    /// Plugin metadata implementation
    /// </summary>
    public class PluginMetadata : IPluginMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public PluginCategory Category { get; set; }
        public IReadOnlyList<string> Dependencies { get; set; } = Array.Empty<string>();
        public DateTime LoadedTime { get; set; }
        public string AssemblyPath { get; set; }
        public bool IsEnabled { get; set; }
        public string Signature { get; set; }
    }

    /// <summary>
    /// Read-only view of plugin metadata
    /// </summary>
    public class PluginMetadataView : IPluginMetadata
    {
        private readonly PluginMetadata _metadata;

        public PluginMetadataView(PluginMetadata metadata)
        {
            _metadata = metadata;
        }

        public string Id => _metadata.Id;
        public string Name => _metadata.Name;
        public string Version => _metadata.Version;
        public string Author => _metadata.Author;
        public string Description => _metadata.Description;
        public PluginCategory Category => _metadata.Category;
        public IReadOnlyList<string> Dependencies => _metadata.Dependencies;
        public DateTime LoadedTime => _metadata.LoadedTime;
        public string AssemblyPath => _metadata.AssemblyPath;
        public bool IsEnabled => _metadata.IsEnabled;
        public string Signature => _metadata.Signature;
    }

    /// <summary>
    /// Plugin configuration data
    /// </summary>
    public class PluginConfigurationData
    {
        public Dictionary<string, object> Settings { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    // Event args
    public class PluginRegistryEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public DateTime EventTime { get; set; }
    }

    public class PluginStatusEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public PluginStatus OldStatus { get; set; }
        public PluginStatus NewStatus { get; set; }
        public string Message { get; set; }
        public DateTime EventTime { get; set; }
    }

    public class PluginConfigurationEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public string Key { get; set; }
        public object Value { get; set; }
        public DateTime EventTime { get; set; }
    }
}
