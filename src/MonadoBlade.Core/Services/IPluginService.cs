namespace MonadoBlade.Core.Services;

/// <summary>
/// Metadata about a plugin.
/// </summary>
public class PluginMetadata
{
    /// <summary>Gets or sets the unique plugin identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin version.</summary>
    public Version Version { get; set; } = new();

    /// <summary>Gets or sets the plugin description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin author.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin entry point (type name).</summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>Gets or sets required permissions.</summary>
    public List<string> RequiredPermissions { get; set; } = new();

    /// <summary>Gets or sets dependency plugins.</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>Gets or sets whether the plugin is system-critical.</summary>
    public bool IsSystemCritical { get; set; }
}

/// <summary>
/// Represents a loaded plugin instance.
/// </summary>
public class Plugin
{
    /// <summary>Gets or sets the plugin metadata.</summary>
    public PluginMetadata Metadata { get; set; } = new();

    /// <summary>Gets or sets the plugin instance.</summary>
    public object? Instance { get; set; }

    /// <summary>Gets or sets whether the plugin is currently loaded.</summary>
    public bool IsLoaded { get; set; }

    /// <summary>Gets or sets the last error if plugin failed to load.</summary>
    public string? LastError { get; set; }
}

/// <summary>
/// Result of a plugin execution.
/// </summary>
public class PluginExecutionResult
{
    /// <summary>Gets or sets whether execution succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the execution output.</summary>
    public object? Output { get; set; }

    /// <summary>Gets or sets any error message.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the execution duration.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Gets or sets execution metrics.</summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Plugin service managing plugin discovery, loading, and execution.
/// Responsible for plugin lifecycle, dependency resolution, and sandboxing.
/// </summary>
public interface IPluginService : IService
{
    /// <summary>
    /// Discovers all available plugins in the plugin directory.
    /// </summary>
    /// <returns>A collection of discovered plugin metadata.</returns>
    /// <exception cref="OperationFailedException">Thrown when discovery fails.</exception>
    Task<ICollection<PluginMetadata>> DiscoverPluginsAsync();

    /// <summary>
    /// Loads a plugin by ID, resolving its dependencies.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <returns>The loaded plugin.</returns>
    /// <exception cref="OperationFailedException">Thrown when loading or dependency resolution fails.</exception>
    Task<Plugin> LoadPluginAsync(string pluginId);

    /// <summary>
    /// Unloads a plugin and releases its resources.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <returns>A task representing the unload operation.</returns>
    Task UnloadPluginAsync(string pluginId);

    /// <summary>
    /// Executes a method on a loaded plugin.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <param name="methodName">The method to execute.</param>
    /// <param name="parameters">Method parameters.</param>
    /// <returns>The execution result.</returns>
    /// <exception cref="OperationFailedException">Thrown when execution fails.</exception>
    Task<PluginExecutionResult> ExecutePluginAsync(string pluginId, string methodName, params object?[] parameters);

    /// <summary>
    /// Registers a new plugin in the system.
    /// </summary>
    /// <param name="metadata">The plugin metadata.</param>
    /// <returns>A task representing the registration operation.</returns>
    /// <exception cref="ValidationFailedException">Thrown when metadata is invalid.</exception>
    Task RegisterPluginAsync(PluginMetadata metadata);

    /// <summary>
    /// Unregisters a plugin from the system.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <returns>A task representing the unregistration operation.</returns>
    Task UnregisterPluginAsync(string pluginId);

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    /// <returns>A collection of loaded plugins.</returns>
    Task<ICollection<Plugin>> GetLoadedPluginsAsync();

    /// <summary>
    /// Gets a plugin by ID.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <returns>The plugin if found; otherwise null.</returns>
    Task<Plugin?> GetPluginAsync(string pluginId);

    /// <summary>
    /// Checks if a plugin has a specific permission.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if plugin has permission; otherwise false.</returns>
    Task<bool> HasPermissionAsync(string pluginId, string permission);

    /// <summary>
    /// Grants a permission to a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <param name="permission">The permission to grant.</param>
    /// <returns>A task representing the operation.</returns>
    Task GrantPermissionAsync(string pluginId, string permission);

    /// <summary>
    /// Revokes a permission from a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <param name="permission">The permission to revoke.</param>
    /// <returns>A task representing the operation.</returns>
    Task RevokePermissionAsync(string pluginId, string permission);
}
