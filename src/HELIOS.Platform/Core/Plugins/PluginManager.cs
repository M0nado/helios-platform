using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Plugins
{
    /// <summary>
    /// Plugin interface that all plugins must implement.
    /// </summary>
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Author { get; }
        string Description { get; }
        Task InitializeAsync();
        Task ExecuteAsync();
        Task UnloadAsync();
    }

    /// <summary>
    /// Plugin management system for extensible functionality.
    /// </summary>
    public interface IPluginManager
    {
        Task<bool> LoadPluginAsync(string pluginPath);
        Task<bool> UnloadPluginAsync(string pluginName);
        Task<List<PluginInfo>> GetLoadedPluginsAsync();
        Task<List<PluginInfo>> DiscoverPluginsAsync(string searchPath);
        Task<bool> EnablePluginAsync(string pluginName);
        Task<bool> DisablePluginAsync(string pluginName);
        Task<PluginExecutionResult> ExecutePluginAsync(string pluginName, Dictionary<string, object>? parameters = null);
        Task<List<PluginMarketplaceItem>> SearchMarketplaceAsync(string query);
    }

    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsLoaded { get; set; }
        public DateTime LoadedAt { get; set; }
        public List<string> Dependencies { get; set; } = new();
    }

    public class PluginExecutionResult
    {
        public string PluginName { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    public class PluginMarketplaceItem
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Downloads { get; set; }
        public double Rating { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Plugin manager for loading, executing, and managing plugins.
    /// </summary>
    public class PluginManager : IPluginManager
    {
        private readonly Core.Logging.ILogger? _logger;
        private readonly Dictionary<string, PluginInfo> _plugins = new();
        private readonly Dictionary<string, IPlugin> _loadedInstances = new();
        private readonly List<PluginMarketplaceItem> _marketplaceCache = new();

        public PluginManager(Core.Logging.ILogger? logger = null)
        {
            _logger = logger;
            InitializeMarketplaceCache();
        }

        public async Task<bool> LoadPluginAsync(string pluginPath)
        {
            try
            {
                if (!File.Exists(pluginPath))
                {
                    _logger?.Warning($"Plugin file not found: {pluginPath}");
                    return false;
                }

                var assembly = Assembly.LoadFrom(pluginPath);
                var pluginTypes = assembly.GetTypes();

                foreach (var type in pluginTypes)
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface)
                    {
                        var instance = (IPlugin?)Activator.CreateInstance(type);
                        if (instance != null)
                        {
                            await instance.InitializeAsync();

                            var info = new PluginInfo
                            {
                                Name = instance.Name,
                                Version = instance.Version,
                                Author = instance.Author,
                                Description = instance.Description,
                                FilePath = pluginPath,
                                IsEnabled = true,
                                IsLoaded = true,
                                LoadedAt = DateTime.UtcNow
                            };

                            _plugins[instance.Name] = info;
                            _loadedInstances[instance.Name] = instance;

                            _logger?.Info($"Plugin loaded: {instance.Name} v{instance.Version}");
                            return true;
                        }
                    }
                }

                _logger?.Warning($"No IPlugin implementations found in {pluginPath}");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to load plugin: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnloadPluginAsync(string pluginName)
        {
            try
            {
                if (!_loadedInstances.TryGetValue(pluginName, out var instance))
                {
                    _logger?.Warning($"Plugin not loaded: {pluginName}");
                    return false;
                }

                await instance.UnloadAsync();
                _loadedInstances.Remove(pluginName);

                if (_plugins.TryGetValue(pluginName, out var info))
                    info.IsLoaded = false;

                _logger?.Info($"Plugin unloaded: {pluginName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to unload plugin: {ex.Message}");
                return false;
            }
        }

        public async Task<List<PluginInfo>> GetLoadedPluginsAsync()
        {
            return new List<PluginInfo>(_plugins.Values);
        }

        public async Task<List<PluginInfo>> DiscoverPluginsAsync(string searchPath)
        {
            var discovered = new List<PluginInfo>();

            try
            {
                if (!Directory.Exists(searchPath))
                {
                    _logger?.Warning($"Search path not found: {searchPath}");
                    return discovered;
                }

                var dllFiles = Directory.GetFiles(searchPath, "*.dll");

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(dllFile);
                        var pluginTypes = assembly.GetTypes();

                        foreach (var type in pluginTypes)
                        {
                            if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface)
                            {
                                var instance = (IPlugin?)Activator.CreateInstance(type);
                                if (instance != null)
                                {
                                    var info = new PluginInfo
                                    {
                                        Name = instance.Name,
                                        Version = instance.Version,
                                        Author = instance.Author,
                                        Description = instance.Description,
                                        FilePath = dllFile,
                                        IsEnabled = false,
                                        IsLoaded = false,
                                        LoadedAt = DateTime.MinValue
                                    };

                                    discovered.Add(info);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warning($"Failed to inspect {dllFile}: {ex.Message}");
                    }
                }

                _logger?.Info($"Discovered {discovered.Count} plugins in {searchPath}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Plugin discovery failed: {ex.Message}");
            }

            return discovered;
        }

        public async Task<bool> EnablePluginAsync(string pluginName)
        {
            try
            {
                if (_plugins.TryGetValue(pluginName, out var info))
                {
                    info.IsEnabled = true;
                    _logger?.Info($"Plugin enabled: {pluginName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to enable plugin: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisablePluginAsync(string pluginName)
        {
            try
            {
                if (_plugins.TryGetValue(pluginName, out var info))
                {
                    info.IsEnabled = false;
                    _logger?.Info($"Plugin disabled: {pluginName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to disable plugin: {ex.Message}");
                return false;
            }
        }

        public async Task<PluginExecutionResult> ExecutePluginAsync(string pluginName, Dictionary<string, object>? parameters = null)
        {
            var result = new PluginExecutionResult
            {
                PluginName = pluginName,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                if (!_loadedInstances.TryGetValue(pluginName, out var instance))
                {
                    result.ErrorMessage = $"Plugin not loaded: {pluginName}";
                    result.Success = false;
                    return result;
                }

                if (!_plugins[pluginName].IsEnabled)
                {
                    result.ErrorMessage = $"Plugin is disabled: {pluginName}";
                    result.Success = false;
                    return result;
                }

                var startTime = DateTime.UtcNow;
                await instance.ExecuteAsync();
                result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                result.Success = true;

                _logger?.Info($"Plugin executed: {pluginName} ({result.ExecutionTimeMs}ms)");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.Success = false;
                _logger?.Error($"Plugin execution failed: {ex.Message}");
            }

            return result;
        }

        public async Task<List<PluginMarketplaceItem>> SearchMarketplaceAsync(string query)
        {
            try
            {
                var results = new List<PluginMarketplaceItem>();

                foreach (var item in _marketplaceCache)
                {
                    if (item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        item.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(item);
                    }
                }

                _logger?.Info($"Marketplace search for '{query}': {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Marketplace search failed: {ex.Message}");
                return new List<PluginMarketplaceItem>();
            }
        }

        private void InitializeMarketplaceCache()
        {
            _marketplaceCache.AddRange(new[]
            {
                new PluginMarketplaceItem
                {
                    Name = "GPU Accelerator",
                    Version = "1.0.0",
                    Author = "HELIOS Team",
                    Description = "GPU acceleration for computation tasks",
                    Category = "Performance",
                    Downloads = 1250,
                    Rating = 4.8,
                    DownloadUrl = "https://marketplace.helios.io/gpu-accelerator"
                },
                new PluginMarketplaceItem
                {
                    Name = "Cloud Backup",
                    Version = "2.1.0",
                    Author = "HELIOS Team",
                    Description = "Automated cloud backup and sync",
                    Category = "Storage",
                    Downloads = 3400,
                    Rating = 4.6,
                    DownloadUrl = "https://marketplace.helios.io/cloud-backup"
                },
                new PluginMarketplaceItem
                {
                    Name = "Network Monitor",
                    Version = "1.5.0",
                    Author = "HELIOS Team",
                    Description = "Real-time network monitoring and analytics",
                    Category = "Monitoring",
                    Downloads = 2100,
                    Rating = 4.7,
                    DownloadUrl = "https://marketplace.helios.io/network-monitor"
                }
            });
        }
    }
}
