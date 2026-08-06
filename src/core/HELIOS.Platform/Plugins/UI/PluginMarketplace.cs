// ═══════════════════════════════════════════════════════════════════════════
// Plugin Marketplace UI - Plugin Discovery, Installation & Management
// Provides user interface for plugin browsing, installation, and configuration
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Plugins.Interfaces;
using HELIOS.Platform.Plugins.Loader;
using HELIOS.Platform.Plugins.Registry;
using Microsoft.Extensions.Logging;
using PluginContract = HELIOS.Platform.Plugins.Interfaces.IPlugin;
using PluginLoaderContract = HELIOS.Platform.Plugins.Loader.IPluginLoader;
using PluginRegistryContract = HELIOS.Platform.Plugins.Registry.IPluginRegistry;
using RegistryPluginMetadata = HELIOS.Platform.Plugins.Registry.PluginMetadata;

namespace HELIOS.Platform.Plugins.UI
{
    /// <summary>
    /// Marketplace for discovering, installing, and managing plugins
    /// </summary>
    public class PluginMarketplace : IPluginMarketplace
    {
        private readonly PluginLoaderContract _pluginLoader;
        private readonly PluginRegistryContract _pluginRegistry;
        private readonly ILogger<PluginMarketplace> _logger;
        private readonly List<PluginListingViewModel> _availablePlugins;
        private readonly object _lockObject = new();

        public event EventHandler<PluginInstallationEventArgs> InstallationStarted;
        public event EventHandler<PluginInstallationEventArgs> InstallationCompleted;
        public event EventHandler<PluginInstallationEventArgs> InstallationFailed;
        public event EventHandler<PluginInstallationEventArgs> UninstallationStarted;

        public PluginMarketplace(
            PluginLoaderContract pluginLoader,
            PluginRegistryContract pluginRegistry,
            ILogger<PluginMarketplace> logger)
        {
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _availablePlugins = new();
        }

        public async Task<List<PluginListingViewModel>> BrowseAvailablePluginsAsync(
            string category = null,
            string searchTerm = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Browsing plugins: category={Category}, search={SearchTerm}, page={Page}",
                category,
                searchTerm,
                pageNumber);

            var discovered = await _pluginLoader.DiscoverPluginsAsync(cancellationToken);

            var filtered = discovered
                .Where(p => string.IsNullOrWhiteSpace(category) || p.Category.ToString() == category)
                .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var paginated = filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return paginated.Select(p => new PluginListingViewModel
            {
                PluginId = p.PluginId,
                Name = p.Name,
                Version = p.Version,
                Author = p.Author,
                Description = p.Description,
                Category = p.Category.ToString(),
                AverageRating = 4.5,
                DownloadCount = 0,
                IsInstalled = _pluginRegistry.GetPluginMetadata(p.PluginId) != null,
                DiscoveryResult = p
            }).ToList();
        }

        public async Task<bool> InstallPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Installing plugin {PluginId}", pluginId);

            try
            {
                InstallationStarted?.Invoke(this, new PluginInstallationEventArgs
                {
                    PluginId = pluginId,
                    EventTime = DateTime.UtcNow
                });

                var loadContext = new PluginLoadContext
                {
                    ContextName = $"plugin-{pluginId}",
                    IsolationEnabled = true
                };

                var plugin = await _pluginLoader.LoadPluginAsync(
                    pluginId,
                    loadContext,
                    cancellationToken);

                await plugin.InitializeAsync(
                    new DefaultPluginContext(plugin),
                    cancellationToken);

                var metadata = new RegistryPluginMetadata
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Version = plugin.Version,
                    Author = plugin.Author,
                    Description = plugin.Description,
                    Category = plugin.Category,
                    Dependencies = plugin.Dependencies.ToList(),
                    LoadedTime = DateTime.UtcNow,
                    IsEnabled = true
                };

                _pluginRegistry.RegisterPlugin(pluginId, plugin, metadata);

                await plugin.StartAsync(cancellationToken);

                _logger.LogInformation("Plugin {PluginId} installed successfully", pluginId);

                InstallationCompleted?.Invoke(this, new PluginInstallationEventArgs
                {
                    PluginId = pluginId,
                    EventTime = DateTime.UtcNow,
                    Success = true
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install plugin {PluginId}", pluginId);

                InstallationFailed?.Invoke(this, new PluginInstallationEventArgs
                {
                    PluginId = pluginId,
                    EventTime = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message
                });

                return false;
            }
        }

        public async Task<bool> UninstallPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uninstalling plugin {PluginId}", pluginId);

            try
            {
                UninstallationStarted?.Invoke(this, new PluginInstallationEventArgs
                {
                    PluginId = pluginId,
                    EventTime = DateTime.UtcNow
                });

                if (!_pluginRegistry.GetAllPlugins().TryGetValue(pluginId, out var plugin))
                {
                    _logger.LogWarning("Plugin {PluginId} not found in registry", pluginId);
                    return false;
                }

                await plugin.StopAsync(cancellationToken);
                _pluginRegistry.UnregisterPlugin(pluginId);
                await _pluginLoader.UnloadPluginAsync(pluginId, cancellationToken);

                _logger.LogInformation("Plugin {PluginId} uninstalled successfully", pluginId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to uninstall plugin {PluginId}", pluginId);
                return false;
            }
        }

        public async Task<bool> EnablePluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Enabling plugin {PluginId}", pluginId);

            try
            {
                if (!_pluginRegistry.GetAllPlugins().TryGetValue(pluginId, out var plugin))
                {
                    return false;
                }

                _pluginRegistry.EnablePlugin(pluginId);
                await plugin.StartAsync(cancellationToken);
                _pluginRegistry.SetPluginStatus(pluginId, PluginStatus.Running);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable plugin {PluginId}", pluginId);
                return false;
            }
        }

        public async Task<bool> DisablePluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Disabling plugin {PluginId}", pluginId);

            try
            {
                if (!_pluginRegistry.GetAllPlugins().TryGetValue(pluginId, out var plugin))
                {
                    return false;
                }

                _pluginRegistry.DisablePlugin(pluginId);
                await plugin.StopAsync(cancellationToken);
                _pluginRegistry.SetPluginStatus(pluginId, PluginStatus.Disabled);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable plugin {PluginId}", pluginId);
                return false;
            }
        }

        public async Task<PluginConfigurationViewModel> GetPluginConfigurationAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            if (!_pluginRegistry.GetAllPlugins().TryGetValue(pluginId, out var plugin))
            {
                return null;
            }

            var config = plugin.GetConfiguration();
            if (config == null)
            {
                return null;
            }

            await config.LoadAsync(cancellationToken);

            return new PluginConfigurationViewModel
            {
                PluginId = pluginId,
                ConfigurationSettings = config.GetAllKeys().ToDictionary(
                    k => k,
                    k => config.GetValue(k))
            };
        }

        public async Task<bool> SavePluginConfigurationAsync(
            string pluginId,
            Dictionary<string, object> settings,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_pluginRegistry.GetAllPlugins().TryGetValue(pluginId, out var plugin))
                {
                    return false;
                }

                var config = plugin.GetConfiguration();
                if (config == null)
                {
                    return false;
                }

                foreach (var kvp in settings)
                {
                    config.SetValue(kvp.Key, kvp.Value);
                }

                await config.SaveAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save plugin configuration for {PluginId}", pluginId);
                return false;
            }
        }

        public async Task<List<PluginListingViewModel>> GetInstalledPluginsAsync(
            CancellationToken cancellationToken = default)
        {
            var installedPlugins = _pluginRegistry.GetAllPlugins();

            return await Task.FromResult(installedPlugins
                .Select(p => new PluginListingViewModel
                {
                    PluginId = p.Key,
                    Name = p.Value.Name,
                    Version = p.Value.Version,
                    Author = p.Value.Author,
                    Description = p.Value.Description,
                    Category = p.Value.Category.ToString(),
                    IsInstalled = true
                })
                .ToList());
        }
    }

    /// <summary>
    /// Plugin marketplace interface
    /// </summary>
    public interface IPluginMarketplace
    {
        Task<List<PluginListingViewModel>> BrowseAvailablePluginsAsync(
            string category = null,
            string searchTerm = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<bool> InstallPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        Task<bool> UninstallPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        Task<bool> EnablePluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        Task<bool> DisablePluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        Task<PluginConfigurationViewModel> GetPluginConfigurationAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        Task<bool> SavePluginConfigurationAsync(
            string pluginId,
            Dictionary<string, object> settings,
            CancellationToken cancellationToken = default);

        Task<List<PluginListingViewModel>> GetInstalledPluginsAsync(
            CancellationToken cancellationToken = default);

        event EventHandler<PluginInstallationEventArgs> InstallationStarted;
        event EventHandler<PluginInstallationEventArgs> InstallationCompleted;
        event EventHandler<PluginInstallationEventArgs> InstallationFailed;
        event EventHandler<PluginInstallationEventArgs> UninstallationStarted;
    }

    // View Models
    public class PluginListingViewModel
    {
        public string PluginId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public double AverageRating { get; set; }
        public int DownloadCount { get; set; }
        public bool IsInstalled { get; set; }
        public PluginDiscoveryResult DiscoveryResult { get; set; }
    }

    public class PluginConfigurationViewModel
    {
        public string PluginId { get; set; }
        public Dictionary<string, object> ConfigurationSettings { get; set; } = new();
    }

    public class PluginInstallationEventArgs : EventArgs
    {
        public string PluginId { get; set; }
        public DateTime EventTime { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Default plugin context implementation
    /// </summary>
    public class DefaultPluginContext : IPluginContext
    {
        private readonly PluginContract _plugin;

        public DefaultPluginContext(PluginContract plugin)
        {
            _plugin = plugin;
        }

        public IServiceProvider HostServices { get; set; }
        public string ConfigurationPath { get; set; }
        public string DataPath { get; set; }
        public IPluginLogger Logger { get; set; } = new DefaultPluginLogger();
        public IPluginMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Default plugin logger implementation
    /// </summary>
    public class DefaultPluginLogger : IPluginLogger
    {
        public void LogInformation(string message, params object[] args)
            => Console.WriteLine($"[INFO] {string.Format(message, args)}");

        public void LogWarning(string message, params object[] args)
            => Console.WriteLine($"[WARN] {string.Format(message, args)}");

        public void LogError(string message, Exception ex = null, params object[] args)
            => Console.WriteLine($"[ERROR] {string.Format(message, args)}{(ex != null ? " " + ex : "")}");

        public void LogDebug(string message, params object[] args)
            => Console.WriteLine($"[DEBUG] {string.Format(message, args)}");

        public void LogTrace(string message, params object[] args)
            => Console.WriteLine($"[TRACE] {string.Format(message, args)}");
    }
}
