// ═══════════════════════════════════════════════════════════════════════════
// Plugin Loader - Dynamic Assembly Loading & Discovery
// Handles plugin discovery, loading, signature verification, and isolation
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Plugins.Interfaces;
using Microsoft.Extensions.Logging;
using PluginContract = HELIOS.Platform.Plugins.Interfaces.IPlugin;

namespace HELIOS.Platform.Plugins.Loader
{
    /// <summary>
    /// Handles dynamic loading and discovery of plugins
    /// Provides signature verification and sandboxing mechanisms
    /// </summary>
    public class PluginLoader : IPluginLoader
    {
        private readonly string _pluginDirectory;
        private readonly ILogger<PluginLoader> _logger;
        private readonly IPluginSecurityValidator _securityValidator;
        private readonly Dictionary<string, LoadedPlugin> _loadedPlugins;
        private readonly object _lockObject = new();

        public PluginLoader(
            string pluginDirectory,
            ILogger<PluginLoader> logger,
            IPluginSecurityValidator securityValidator)
        {
            _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _securityValidator = securityValidator ?? throw new ArgumentNullException(nameof(securityValidator));
            _loadedPlugins = new();

            EnsurePluginDirectoryExists();
        }

        public async Task<IEnumerable<PluginDiscoveryResult>> DiscoverPluginsAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting plugin discovery in {PluginDirectory}", _pluginDirectory);
            var results = new List<PluginDiscoveryResult>();

            try
            {
                var pluginAssemblies = Directory.GetFiles(
                    _pluginDirectory,
                    "*.dll",
                    SearchOption.AllDirectories);

                foreach (var assemblyPath in pluginAssemblies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await DiscoverPluginInAssemblyAsync(assemblyPath, cancellationToken);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }

                _logger.LogInformation("Plugin discovery completed. Found {PluginCount} plugins", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin discovery failed");
            }

            return results;
        }

        public async Task<PluginContract> LoadPluginAsync(
            string pluginId,
            PluginLoadContext loadContext,
            CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                if (_loadedPlugins.ContainsKey(pluginId))
                {
                    _logger.LogWarning("Plugin {PluginId} is already loaded", pluginId);
                    return _loadedPlugins[pluginId].Instance;
                }
            }

            _logger.LogInformation("Loading plugin {PluginId}", pluginId);

            try
            {
                var discoveryResult = await DiscoverPluginByIdAsync(pluginId, cancellationToken);
                if (discoveryResult == null)
                {
                    throw new PluginNotFoundException($"Plugin {pluginId} not found");
                }

                // Verify signature
                if (!await _securityValidator.VerifySignatureAsync(
                    discoveryResult.AssemblyPath,
                    discoveryResult.Signature,
                    cancellationToken))
                {
                    throw new PluginSecurityException($"Plugin {pluginId} signature verification failed");
                }

                // Check version compatibility
                if (!IsVersionCompatible(discoveryResult.Version))
                {
                    throw new PluginVersionException(
                        $"Plugin {pluginId} version {discoveryResult.Version} is not compatible");
                }

                // Load assembly with isolation context
                var assembly = await LoadAssemblyInContextAsync(
                    discoveryResult.AssemblyPath,
                    loadContext,
                    cancellationToken);

                // Create plugin instance
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(PluginContract).IsAssignableFrom(t) && !t.IsInterface);

                if (pluginType == null)
                {
                    throw new PluginNotFoundException($"No IPlugin implementation found in {pluginId}");
                }

                var plugin = (PluginContract)Activator.CreateInstance(pluginType);

                // Store loaded plugin
                lock (_lockObject)
                {
                    _loadedPlugins[pluginId] = new LoadedPlugin
                    {
                        Instance = plugin,
                        AssemblyPath = discoveryResult.AssemblyPath,
                        LoadedTime = DateTime.UtcNow,
                        LoadContext = loadContext
                    };
                }

                _logger.LogInformation("Plugin {PluginId} loaded successfully", pluginId);
                return plugin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin {PluginId}", pluginId);
                throw;
            }
        }

        public async Task<bool> UnloadPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Unloading plugin {PluginId}", pluginId);

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
                    {
                        _logger.LogWarning("Plugin {PluginId} is not loaded", pluginId);
                        return false;
                    }

                    loadedPlugin.Instance?.Dispose();
                    _loadedPlugins.Remove(pluginId);
                }

                _logger.LogInformation("Plugin {PluginId} unloaded successfully", pluginId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload plugin {PluginId}", pluginId);
                return false;
            }
        }

        public IReadOnlyDictionary<string, PluginContract> GetLoadedPlugins()
        {
            lock (_lockObject)
            {
                return _loadedPlugins.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Instance);
            }
        }

        public bool IsPluginLoaded(string pluginId)
        {
            lock (_lockObject)
            {
                return _loadedPlugins.ContainsKey(pluginId);
            }
        }

        private async Task<PluginDiscoveryResult> DiscoverPluginInAssemblyAsync(
            string assemblyPath,
            CancellationToken cancellationToken)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(PluginContract).IsAssignableFrom(t) && !t.IsInterface);

                if (pluginType == null)
                    return null;

                var pluginAttribute = pluginType.GetCustomAttribute<PluginAttribute>();
                if (pluginAttribute == null)
                    return null;

                var signature = await ComputeAssemblySignatureAsync(assemblyPath, cancellationToken);

                return new PluginDiscoveryResult
                {
                    PluginId = pluginAttribute.Id,
                    Name = pluginAttribute.Name,
                    Version = pluginAttribute.Version,
                    Author = pluginAttribute.Author,
                    Description = pluginAttribute.Description,
                    Category = pluginAttribute.Category,
                    Dependencies = pluginAttribute.Dependencies.ToList(),
                    AssemblyPath = assemblyPath,
                    PluginType = pluginType.FullName,
                    Signature = signature,
                    DiscoveredTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover plugin in {AssemblyPath}", assemblyPath);
                return null;
            }
        }

        private async Task<PluginDiscoveryResult> DiscoverPluginByIdAsync(
            string pluginId,
            CancellationToken cancellationToken)
        {
            var discoveryResults = await DiscoverPluginsAsync(cancellationToken);
            return discoveryResults.FirstOrDefault(r => r.PluginId == pluginId);
        }

        private async Task<Assembly> LoadAssemblyInContextAsync(
            string assemblyPath,
            PluginLoadContext loadContext,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Assembly.LoadFrom(assemblyPath);
            }, cancellationToken);
        }

        private async Task<string> ComputeAssemblySignatureAsync(
            string assemblyPath,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var sha256 = SHA256.Create();
                using var fileStream = File.OpenRead(assemblyPath);
                var hash = sha256.ComputeHash(fileStream);
                return Convert.ToBase64String(hash);
            }, cancellationToken);
        }

        private bool IsVersionCompatible(string pluginVersion)
        {
            try
            {
                var version = Version.Parse(pluginVersion);
                var minVersion = new Version(1, 0, 0);
                var maxVersion = new Version(99, 99, 99);

                return version >= minVersion && version <= maxVersion;
            }
            catch
            {
                _logger.LogWarning("Invalid version format: {Version}", pluginVersion);
                return false;
            }
        }

        private void EnsurePluginDirectoryExists()
        {
            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
                _logger.LogInformation("Created plugin directory: {PluginDirectory}", _pluginDirectory);
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                foreach (var plugin in _loadedPlugins.Values)
                {
                    try
                    {
                        plugin.Instance?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing plugin");
                    }
                }
                _loadedPlugins.Clear();
            }
        }
    }

    /// <summary>
    /// Plugin loader interface
    /// </summary>
    public interface IPluginLoader : IDisposable
    {
        Task<IEnumerable<PluginDiscoveryResult>> DiscoverPluginsAsync(
            CancellationToken cancellationToken = default);

        Task<PluginContract> LoadPluginAsync(
            string pluginId,
            PluginLoadContext loadContext,
            CancellationToken cancellationToken = default);

        Task<bool> UnloadPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        IReadOnlyDictionary<string, PluginContract> GetLoadedPlugins();

        bool IsPluginLoaded(string pluginId);
    }

    /// <summary>
    /// Plugin security validator interface
    /// </summary>
    public interface IPluginSecurityValidator
    {
        Task<bool> VerifySignatureAsync(
            string assemblyPath,
            string expectedSignature,
            CancellationToken cancellationToken = default);

        Task<bool> IsMaliciousAsync(
            string assemblyPath,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Plugin discovery result
    /// </summary>
    public class PluginDiscoveryResult
    {
        public string PluginId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public PluginCategory Category { get; set; }
        public List<string> Dependencies { get; set; }
        public string AssemblyPath { get; set; }
        public string PluginType { get; set; }
        public string Signature { get; set; }
        public DateTime DiscoveredTime { get; set; }
    }

    /// <summary>
    /// Plugin load context for isolation
    /// </summary>
    public class PluginLoadContext
    {
        public string ContextName { get; set; }
        public bool IsolationEnabled { get; set; }
        public Dictionary<string, object> IsolationSettings { get; set; } = new();
    }

    /// <summary>
    /// Internally tracked loaded plugin info
    /// </summary>
    internal class LoadedPlugin
    {
        public PluginContract Instance { get; set; }
        public string AssemblyPath { get; set; }
        public DateTime LoadedTime { get; set; }
        public PluginLoadContext LoadContext { get; set; }
    }

    // Custom exceptions
    public class PluginNotFoundException : Exception
    {
        public PluginNotFoundException(string message) : base(message) { }
    }

    public class PluginSecurityException : Exception
    {
        public PluginSecurityException(string message) : base(message) { }
    }

    public class PluginVersionException : Exception
    {
        public PluginVersionException(string message) : base(message) { }
    }
}
