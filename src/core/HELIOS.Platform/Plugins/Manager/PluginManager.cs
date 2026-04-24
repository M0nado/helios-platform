namespace HELIOS.Platform.Plugins;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// Central plugin management system with lifecycle and security.
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly Dictionary<string, IPlugin> _loadedPlugins = new();
    private readonly Dictionary<string, PluginMetadata> _pluginMetadata = new();
    private readonly Dictionary<string, PluginSecurityContext> _securityContexts = new();
    private readonly HashSet<string> _disabledPlugins = new();
    private readonly string _pluginsDirectory;

    public event EventHandler<PluginEventArgs> PluginLoaded;
    public event EventHandler<PluginEventArgs> PluginUnloaded;
    public event EventHandler<PluginEventArgs> PluginEnabled;
    public event EventHandler<PluginEventArgs> PluginDisabled;
    public event EventHandler<PluginEventArgs> PluginError;

    public PluginManager(string pluginsDirectory)
    {
        _pluginsDirectory = pluginsDirectory ?? throw new ArgumentNullException(nameof(pluginsDirectory));
        Directory.CreateDirectory(_pluginsDirectory);
    }

    public async Task InitializeAsync()
    {
        var pluginFiles = Directory.GetFiles(_pluginsDirectory, "*.dll");
        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(pluginFile);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                    await RegisterAsync(plugin);
                }
            }
            catch (Exception ex)
            {
                OnPluginError(new PluginEventArgs 
                { 
                    Message = $"Failed to load plugin: {ex.Message}",
                    Timestamp = DateTime.UtcNow 
                });
            }
        }
    }

    public async Task<PluginMetadata> RegisterAsync(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        
        var metadata = plugin.Metadata;
        if (_loadedPlugins.ContainsKey(metadata.Id))
            throw new PluginException(metadata.Id, "Plugin already registered");

        // Validate plugin
        if (!await plugin.ValidateAsync())
            throw new PluginException(metadata.Id, "Plugin validation failed");

        // Initialize plugin
        await plugin.InitializeAsync();

        _loadedPlugins[metadata.Id] = plugin;
        _pluginMetadata[metadata.Id] = metadata;
        _securityContexts[metadata.Id] = new PluginSecurityContext 
        { 
            PluginId = metadata.Id,
            CreatedAt = DateTime.UtcNow,
            IsApproved = false
        };

        OnPluginLoaded(metadata);
        return metadata;
    }

    public async Task UnregisterAsync(string pluginId)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            throw new PluginException(pluginId, "Plugin not registered");

        await plugin.ShutdownAsync();
        _loadedPlugins.Remove(pluginId);
        _pluginMetadata.Remove(pluginId);
        _securityContexts.Remove(pluginId);
        _disabledPlugins.Remove(pluginId);

        OnPluginUnloaded(_pluginMetadata.Values.FirstOrDefault(p => p.Id == pluginId));
    }

    public Task<IPlugin> GetPluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            throw new PluginException(pluginId, "Plugin not found");

        return Task.FromResult(plugin);
    }

    public Task<List<PluginMetadata>> ListPluginsAsync()
    {
        return Task.FromResult(_pluginMetadata.Values.ToList());
    }

    public Task<List<PluginMetadata>> ListEnabledPluginsAsync()
    {
        var enabled = _pluginMetadata.Values
            .Where(p => !_disabledPlugins.Contains(p.Id))
            .ToList();

        return Task.FromResult(enabled);
    }

    public async Task EnablePluginAsync(string pluginId)
    {
        if (!_loadedPlugins.ContainsKey(pluginId))
            throw new PluginException(pluginId, "Plugin not found");

        if (_disabledPlugins.Remove(pluginId))
        {
            var plugin = _loadedPlugins[pluginId];
            if (plugin is IPluginLifecycle lifecycle)
                await lifecycle.OnEnableAsync();

            OnPluginEnabled(_pluginMetadata[pluginId]);
        }
    }

    public async Task DisablePluginAsync(string pluginId)
    {
        if (!_loadedPlugins.ContainsKey(pluginId))
            throw new PluginException(pluginId, "Plugin not found");

        var plugin = _loadedPlugins[pluginId];
        if (plugin is IPluginLifecycle lifecycle && !await lifecycle.CanUnloadAsync())
            throw new PluginException(pluginId, "Plugin cannot be disabled");

        if (_disabledPlugins.Add(pluginId))
        {
            if (plugin is IPluginLifecycle lc)
                await lc.OnDisableAsync();

            OnPluginDisabled(_pluginMetadata[pluginId]);
        }
    }

    public bool IsPluginRegistered(string pluginId) => _loadedPlugins.ContainsKey(pluginId);

    public async Task<IPlugin> LoadPluginAsync(string pluginPath, PluginSecurityContext securityContext)
    {
        if (!File.Exists(pluginPath))
            throw new FileNotFoundException($"Plugin file not found: {pluginPath}");

        var assembly = Assembly.LoadFrom(pluginPath);
        var pluginType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

        if (pluginType == null)
            throw new PluginException("No IPlugin implementation found in assembly");

        var plugin = (IPlugin)Activator.CreateInstance(pluginType);
        await RegisterAsync(plugin);

        if (securityContext != null)
            _securityContexts[plugin.Metadata.Id] = securityContext;

        return plugin;
    }

    public async Task UnloadPluginAsync(string pluginId)
    {
        await UnregisterAsync(pluginId);
    }

    public async Task<PluginValidationResult> ValidatePluginAsync(string pluginPath)
    {
        var result = new PluginValidationResult { CheckedAt = DateTime.UtcNow };

        try
        {
            if (!File.Exists(pluginPath))
            {
                result.IsValid = false;
                result.Errors.Add($"File not found: {pluginPath}");
                return result;
            }

            var assembly = Assembly.LoadFrom(pluginPath);
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

            if (pluginType == null)
            {
                result.IsValid = false;
                result.Errors.Add("No IPlugin implementation found");
                return result;
            }

            var plugin = (IPlugin)Activator.CreateInstance(pluginType);
            var isValid = await plugin.ValidateAsync();

            result.IsValid = isValid;
            if (!isValid)
                result.Errors.Add("Plugin validation failed");
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public Task<List<string>> GetAvailablePluginsAsync()
    {
        var plugins = Directory.GetFiles(_pluginsDirectory, "*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        return Task.FromResult(plugins);
    }

    public async Task<PluginValidationResult> ValidateAllPluginsAsync()
    {
        var result = new PluginValidationResult { CheckedAt = DateTime.UtcNow, IsValid = true };

        foreach (var pluginId in _loadedPlugins.Keys)
        {
            try
            {
                var plugin = _loadedPlugins[pluginId];
                if (!await plugin.ValidateAsync())
                {
                    result.IsValid = false;
                    result.Warnings.Add($"Plugin {pluginId} validation failed");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Plugin {pluginId}: {ex.Message}");
            }
        }

        return result;
    }

    public Task<Dictionary<string, PluginValidationResult>> GetPluginValidationStatusAsync()
    {
        var status = new Dictionary<string, PluginValidationResult>();
        foreach (var pluginId in _loadedPlugins.Keys)
        {
            status[pluginId] = new PluginValidationResult
            {
                IsValid = _loadedPlugins[pluginId] != null,
                CheckedAt = DateTime.UtcNow
            };
        }
        return Task.FromResult(status);
    }

    private void OnPluginLoaded(PluginMetadata metadata)
        => PluginLoaded?.Invoke(this, new PluginEventArgs { Metadata = metadata, Timestamp = DateTime.UtcNow });

    private void OnPluginUnloaded(PluginMetadata metadata)
        => PluginUnloaded?.Invoke(this, new PluginEventArgs { Metadata = metadata, Timestamp = DateTime.UtcNow });

    private void OnPluginEnabled(PluginMetadata metadata)
        => PluginEnabled?.Invoke(this, new PluginEventArgs { Metadata = metadata, Timestamp = DateTime.UtcNow });

    private void OnPluginDisabled(PluginMetadata metadata)
        => PluginDisabled?.Invoke(this, new PluginEventArgs { Metadata = metadata, Timestamp = DateTime.UtcNow });

    private void OnPluginError(PluginEventArgs args)
        => PluginError?.Invoke(this, args);
}
