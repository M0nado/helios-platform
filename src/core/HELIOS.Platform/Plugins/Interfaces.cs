namespace HELIOS.Platform.Plugins;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Represents metadata about a plugin.
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Version Version { get; set; }
    public string Author { get; set; }
    public string License { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> Capabilities { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Core plugin interface that all plugins must implement.
/// </summary>
public interface IPlugin
{
    PluginMetadata Metadata { get; }
    Task InitializeAsync();
    Task ShutdownAsync();
    Task<bool> ValidateAsync();
}

/// <summary>
/// Plugin lifecycle hook interface.
/// </summary>
public interface IPluginLifecycle
{
    Task OnLoadAsync();
    Task OnUnloadAsync();
    Task OnEnableAsync();
    Task OnDisableAsync();
    Task<bool> CanUnloadAsync();
}

/// <summary>
/// Plugin configuration interface.
/// </summary>
public interface IPluginConfiguration
{
    Dictionary<string, object> GetConfiguration();
    Task UpdateConfigurationAsync(Dictionary<string, object> config);
    Task ResetConfigurationAsync();
}

/// <summary>
/// Plugin security context.
/// </summary>
public class PluginSecurityContext
{
    public string PluginId { get; set; }
    public List<string> AllowedPermissions { get; set; } = new();
    public List<string> DeniedPermissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsApproved { get; set; }
    public string ApprovedBy { get; set; }
}

/// <summary>
/// Plugin validation result.
/// </summary>
public class PluginValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Plugin event args.
/// </summary>
public class PluginEventArgs : EventArgs
{
    public string PluginId { get; set; }
    public PluginMetadata Metadata { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }
}

/// <summary>
/// Plugin registry interface.
/// </summary>
public interface IPluginRegistry
{
    Task<PluginMetadata> RegisterAsync(IPlugin plugin);
    Task UnregisterAsync(string pluginId);
    Task<IPlugin> GetPluginAsync(string pluginId);
    Task<List<PluginMetadata>> ListPluginsAsync();
    Task<List<PluginMetadata>> ListEnabledPluginsAsync();
    Task EnablePluginAsync(string pluginId);
    Task DisablePluginAsync(string pluginId);
    bool IsPluginRegistered(string pluginId);
}

/// <summary>
/// Plugin loader interface with sandboxing.
/// </summary>
public interface IPluginLoader
{
    Task<IPlugin> LoadPluginAsync(string pluginPath, PluginSecurityContext securityContext);
    Task UnloadPluginAsync(string pluginId);
    Task<PluginValidationResult> ValidatePluginAsync(string pluginPath);
    Task<List<string>> GetAvailablePluginsAsync();
}

/// <summary>
/// Plugin manager orchestrates all plugin operations.
/// </summary>
public interface IPluginManager : IPluginRegistry, IPluginLoader
{
    event EventHandler<PluginEventArgs> PluginLoaded;
    event EventHandler<PluginEventArgs> PluginUnloaded;
    event EventHandler<PluginEventArgs> PluginEnabled;
    event EventHandler<PluginEventArgs> PluginDisabled;
    event EventHandler<PluginEventArgs> PluginError;

    Task InitializeAsync();
    Task<PluginValidationResult> ValidateAllPluginsAsync();
    Task<Dictionary<string, PluginValidationResult>> GetPluginValidationStatusAsync();
}

/// <summary>
/// Plugin marketplace interface.
/// </summary>
public interface IPluginMarketplace
{
    Task<List<PluginMetadata>> SearchPluginsAsync(string query);
    Task<PluginMetadata> GetPluginDetailsAsync(string pluginId);
    Task<byte[]> DownloadPluginAsync(string pluginId);
    Task<bool> PublishPluginAsync(IPlugin plugin, string marketplaceKey);
    Task RatePluginAsync(string pluginId, int rating, string review);
    Task<List<(PluginMetadata Plugin, double Rating)>> GetTopPluginsAsync(int count);
}

/// <summary>
/// Plugin service communication interface.
/// </summary>
public interface IPluginServiceBroker
{
    Task<T> InvokeServiceAsync<T>(string pluginId, string serviceName, Dictionary<string, object> parameters);
    Task PublishEventAsync(string eventName, object data);
    Task<IDisposable> SubscribeAsync(string eventName, Func<object, Task> handler);
}

/// <summary>
/// Plugin execution context.
/// </summary>
public class PluginExecutionContext
{
    public string PluginId { get; set; }
    public PluginSecurityContext SecurityContext { get; set; }
    public Dictionary<string, object> Environment { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
/// Plugin execution result.
/// </summary>
public class PluginExecutionResult
{
    public bool Success { get; set; }
    public object Result { get; set; }
    public string Error { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Plugin capability request.
/// </summary>
public class PluginCapabilityRequest
{
    public string PluginId { get; set; }
    public string CapabilityName { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Plugin exception.
/// </summary>
public class PluginException : Exception
{
    public string PluginId { get; set; }
    public PluginException(string message) : base(message) { }
    public PluginException(string message, Exception innerException) : base(message, innerException) { }
    public PluginException(string pluginId, string message) : base(message) => PluginId = pluginId;
    public PluginException(string pluginId, string message, Exception innerException) 
        : base(message, innerException) => PluginId = pluginId;
}
