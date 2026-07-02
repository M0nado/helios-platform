namespace MonadoBlade.Core.Services;

using MonadoBlade.Core.Abstractions;
using MonadoBlade.Core.Caching;
using MonadoBlade.Core.Concurrency;
using System.Collections.Concurrent;

/// <summary>
/// High-performance plugin service with lock-free collections and optimizations.
/// </summary>
public class PluginService : ServiceBase, ILifecycleService
{
    private readonly LockFreeRegistry _registry;
    private readonly IntelligentCache _cache;
    private readonly ConcurrentDictionary<string, Plugin> _loadedPlugins;
    private bool _initialized;

    public bool IsInitialized => _initialized;

    public PluginService(ILogger logger) : base(logger)
    {
        _registry = new LockFreeRegistry();
        _cache = new IntelligentCache();
        _loadedPlugins = new ConcurrentDictionary<string, Plugin>();
    }

    public async Task InitializeAsync()
    {
        LogInfo("PluginService initializing");
        _initialized = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Loads a plugin from metadata.
    /// </summary>
    public async Task<bool> LoadPluginAsync(PluginMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        try
        {
            var plugin = new Plugin
            {
                Metadata = metadata,
                IsLoaded = true,
                Instance = null // Would be actual plugin instance in production
            };

            _loadedPlugins.AddOrUpdate(metadata.Id, plugin, (_, _) => plugin);
            _registry.Register(metadata.Id, plugin);

            LogInfo("Plugin loaded: {PluginId} v{Version}", metadata.Id, metadata.Version);
            return true;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to load plugin: {PluginId}", metadata.Id);
            return false;
        }
    }

    /// <summary>
    /// Unloads a plugin by ID.
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pluginId))
            throw new ArgumentNullException(nameof(pluginId));

        if (_loadedPlugins.TryRemove(pluginId, out var plugin))
        {
            _registry.Unregister(pluginId);
            (plugin.Instance as IDisposable)?.Dispose();
            LogInfo("Plugin unloaded: {PluginId}", pluginId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a loaded plugin by ID.
    /// </summary>
    public async Task<Plugin?> GetPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"plugin_{pluginId}";

        if (_cache.TryGetValue<Plugin>(cacheKey, out var cached))
            return cached;

        if (_loadedPlugins.TryGetValue(pluginId, out var plugin))
        {
            _cache.Set(cacheKey, plugin, TimeSpan.FromMinutes(10));
            return plugin;
        }

        return null;
    }

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public async Task<List<Plugin>> GetAllPluginsAsync(CancellationToken cancellationToken = default)
    {
        return new List<Plugin>(_loadedPlugins.Values);
    }

    /// <summary>
    /// Checks if a plugin with the specified ID is loaded.
    /// </summary>
    public async Task<bool> IsPluginLoadedAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        return _registry.IsRegistered(pluginId);
    }

    /// <summary>
    /// Disposes the service.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _cache?.Dispose();
        _loadedPlugins.Clear();
        _registry.Clear();
        await Task.CompletedTask;
    }
}
