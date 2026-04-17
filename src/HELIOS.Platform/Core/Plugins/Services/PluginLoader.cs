using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Plugins.Services
{
    /// <summary>
    /// Loads plugins from assemblies
    /// </summary>
    public class PluginLoader
    {
        private readonly string _pluginDirectoryPath;
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

        public PluginLoader(string pluginDirectoryPath)
        {
            _pluginDirectoryPath = pluginDirectoryPath;
            if (!Directory.Exists(_pluginDirectoryPath))
            {
                Directory.CreateDirectory(_pluginDirectoryPath);
            }
        }

        /// <summary>
        /// Load a plugin from a DLL file
        /// </summary>
        public async Task<PluginLoadResult> LoadPluginAsync(string pluginPath)
        {
            try
            {
                if (!File.Exists(pluginPath))
                {
                    return PluginLoadResult.Error($"Plugin file not found: {pluginPath}");
                }

                // Load the assembly
                var assembly = Assembly.LoadFrom(pluginPath);
                _loadedAssemblies[Path.GetFileNameWithoutExtension(pluginPath)] = assembly;

                // Find all IPlugin implementations
                var pluginTypes = FindPluginTypes(assembly);

                if (pluginTypes.Count == 0)
                {
                    return PluginLoadResult.Error("No IPlugin implementations found in assembly");
                }

                return await Task.FromResult(PluginLoadResult.Success(pluginTypes));
            }
            catch (Exception ex)
            {
                return PluginLoadResult.Error($"Failed to load plugin: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load a plugin manifest from JSON
        /// </summary>
        public async Task<PluginManifestResult> LoadManifestAsync(string manifestPath)
        {
            try
            {
                if (!File.Exists(manifestPath))
                {
                    return PluginManifestResult.Error($"Manifest not found: {manifestPath}");
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<PluginManifestData>(json);

                if (manifest == null)
                {
                    return PluginManifestResult.Error("Failed to deserialize manifest");
                }

                return PluginManifestResult.Success(manifest);
            }
            catch (Exception ex)
            {
                return PluginManifestResult.Error($"Failed to load manifest: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create an instance of a plugin
        /// </summary>
        public IPlugin CreateInstance(Type pluginType)
        {
            if (!typeof(Abstractions.IPlugin).IsAssignableFrom(pluginType))
            {
                throw new ArgumentException($"{pluginType.Name} does not implement IPlugin");
            }

            try
            {
                return (IPlugin)Activator.CreateInstance(pluginType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of {pluginType.Name}", ex);
            }
        }

        /// <summary>
        /// Discover all plugins in a directory
        /// </summary>
        public async Task<List<PluginDiscoveryResult>> DiscoverPluginsAsync(string searchPath = null)
        {
            var results = new List<PluginDiscoveryResult>();
            var basePath = searchPath ?? _pluginDirectoryPath;

            if (!Directory.Exists(basePath))
            {
                return results;
            }

            foreach (var manifestFile in Directory.EnumerateFiles(basePath, "plugin.json", SearchOption.AllDirectories))
            {
                try
                {
                    var manifestDir = Path.GetDirectoryName(manifestFile);
                    var manifestResult = await LoadManifestAsync(manifestFile);

                    if (manifestResult.IsSuccess)
                    {
                        var dllPath = Path.Combine(manifestDir, manifestResult.Data.MainAssembly);
                        var loadResult = await LoadPluginAsync(dllPath);

                        results.Add(new PluginDiscoveryResult
                        {
                            Manifest = manifestResult.Data,
                            PluginTypes = loadResult.PluginTypes,
                            Path = dllPath,
                            IsValid = loadResult.IsSuccess
                        });
                    }
                }
                catch
                {
                    // Continue discovering other plugins
                }
            }

            return await Task.FromResult(results);
        }

        private List<Type> FindPluginTypes(Assembly assembly)
        {
            var pluginTypes = new List<Type>();
            var pluginInterface = typeof(Abstractions.IPlugin);

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (pluginInterface.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        pluginTypes.Add(type);
                    }
                }
            }
            catch
            {
                // Handle types that cannot be reflected
            }

            return pluginTypes;
        }
    }

    /// <summary>
    /// Result of plugin loading operation
    /// </summary>
    public class PluginLoadResult
    {
        public bool IsSuccess { get; set; }
        public List<Type> PluginTypes { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

        public static PluginLoadResult Success(List<Type> types) =>
            new PluginLoadResult { IsSuccess = true, PluginTypes = types ?? new() };

        public static PluginLoadResult Error(string message, Exception ex = null) =>
            new PluginLoadResult { IsSuccess = false, ErrorMessage = message, Exception = ex, PluginTypes = new() };
    }

    /// <summary>
    /// Result of manifest loading operation
    /// </summary>
    public class PluginManifestResult
    {
        public bool IsSuccess { get; set; }
        public PluginManifestData Data { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

        public static PluginManifestResult Success(PluginManifestData data) =>
            new PluginManifestResult { IsSuccess = true, Data = data };

        public static PluginManifestResult Error(string message, Exception ex = null) =>
            new PluginManifestResult { IsSuccess = false, ErrorMessage = message, Exception = ex };
    }

    /// <summary>
    /// Plugin manifest data (JSON structure)
    /// </summary>
    public class PluginManifestData
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string Version { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("author")]
        public string Author { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("license")]
        public string License { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("homepage")]
        public string Homepage { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("repository")]
        public string RepositoryUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("main")]
        public string MainAssembly { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("entryPoint")]
        public string EntryPoint { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("dependencies")]
        public List<DependencyEntry> Dependencies { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("capabilities")]
        public List<string> Capabilities { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("priority")]
        public int Priority { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("autoStart")]
        public bool AutoStart { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Dependency entry in manifest
    /// </summary>
    public class DependencyEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string Version { get; set; } = "*";

        [System.Text.Json.Serialization.JsonPropertyName("optional")]
        public bool Optional { get; set; }
    }

    /// <summary>
    /// Result of plugin discovery
    /// </summary>
    public class PluginDiscoveryResult
    {
        public PluginManifestData Manifest { get; set; }
        public List<Type> PluginTypes { get; set; }
        public string Path { get; set; }
        public bool IsValid { get; set; }
    }
}
