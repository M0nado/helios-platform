using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Extension framework for custom plugins and dashboard extensions
    /// </summary>
    public class ExtensionFramework
    {
        private readonly List<Extension> _extensions;
        private readonly List<ExtensionHook> _hooks;
        private readonly string _extensionDirectory;

        public event EventHandler<Extension> ExtensionLoaded;
        public event EventHandler<string> ExtensionUnloaded;
        public event EventHandler<ExtensionError> ExtensionError;

        public ExtensionFramework(string extensionDirectory = null)
        {
            _extensions = new List<Extension>();
            _hooks = new List<ExtensionHook>();
            _extensionDirectory = extensionDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HELIOS", "Studio", "Extensions"
            );

            EnsureExtensionDirectory();
        }

        /// <summary>
        /// Register an extension hook
        /// </summary>
        public void RegisterHook(string hookName, string description, Type callbackType)
        {
            var hook = new ExtensionHook
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = hookName,
                Description = description,
                CallbackType = callbackType,
                RegisteredAt = DateTime.UtcNow
            };

            _hooks.Add(hook);
        }

        /// <summary>
        /// Load extension from file
        /// </summary>
        public async Task<Extension> LoadExtensionAsync(string assemblyPath)
        {
            try
            {
                if (!File.Exists(assemblyPath))
                    throw new FileNotFoundException($"Extension file not found: {assemblyPath}");

                var assembly = Assembly.LoadFrom(assemblyPath);
                var extensionType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IStudioExtension).IsAssignableFrom(t) && !t.IsInterface);

                if (extensionType == null)
                    throw new InvalidOperationException("No IStudioExtension implementation found in assembly");

                var instance = (IStudioExtension)Activator.CreateInstance(extensionType);

                var extension = new Extension
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = instance.Name,
                    Version = instance.Version,
                    Author = instance.Author,
                    Description = instance.Description,
                    Assembly = assembly,
                    Instance = instance,
                    IsEnabled = true,
                    LoadedAt = DateTime.UtcNow
                };

                _extensions.Add(extension);
                
                await instance.InitializeAsync();
                
                ExtensionLoaded?.Invoke(this, extension);

                return extension;
            }
            catch (Exception ex)
            {
                OnExtensionError(assemblyPath, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Load all extensions from directory
        /// </summary>
        public async Task LoadAllExtensionsAsync()
        {
            if (!Directory.Exists(_extensionDirectory))
                return;

            var assemblyFiles = Directory.GetFiles(_extensionDirectory, "*.dll");

            foreach (var file in assemblyFiles)
            {
                try
                {
                    await LoadExtensionAsync(file);
                }
                catch (Exception ex)
                {
                    OnExtensionError(file, $"Failed to load: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unload extension
        /// </summary>
        public async Task UnloadExtensionAsync(string extensionId)
        {
            var extension = _extensions.FirstOrDefault(e => e.Id == extensionId);
            if (extension != null)
            {
                try
                {
                    await extension.Instance.ShutdownAsync();
                    _extensions.Remove(extension);
                    ExtensionUnloaded?.Invoke(this, extensionId);
                }
                catch (Exception ex)
                {
                    OnExtensionError(extensionId, $"Unload failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get extension
        /// </summary>
        public Extension GetExtension(string extensionId)
        {
            return _extensions.FirstOrDefault(e => e.Id == extensionId);
        }

        /// <summary>
        /// Get all extensions
        /// </summary>
        public IEnumerable<Extension> GetExtensions()
        {
            return _extensions;
        }

        /// <summary>
        /// Enable extension
        /// </summary>
        public void EnableExtension(string extensionId)
        {
            var extension = GetExtension(extensionId);
            if (extension != null)
                extension.IsEnabled = true;
        }

        /// <summary>
        /// Disable extension
        /// </summary>
        public void DisableExtension(string extensionId)
        {
            var extension = GetExtension(extensionId);
            if (extension != null)
                extension.IsEnabled = false;
        }

        /// <summary>
        /// Execute hook across all extensions
        /// </summary>
        public async Task ExecuteHookAsync(string hookName, params object[] parameters)
        {
            var hook = _hooks.FirstOrDefault(h => h.Name == hookName);
            if (hook == null)
                return;

            foreach (var extension in _extensions.Where(e => e.IsEnabled))
            {
                try
                {
                    var method = extension.Instance.GetType().GetMethod(hookName);
                    if (method != null)
                    {
                        var result = method.Invoke(extension.Instance, parameters);
                        if (result is Task task)
                            await task;
                    }
                }
                catch (Exception ex)
                {
                    OnExtensionError(extension.Id, $"Hook execution failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get available hooks
        /// </summary>
        public IEnumerable<ExtensionHook> GetAvailableHooks()
        {
            return _hooks;
        }

        /// <summary>
        /// Create extension manifest
        /// </summary>
        public ExtensionManifest GenerateManifest(Extension extension)
        {
            return new ExtensionManifest
            {
                Name = extension.Name,
                Version = extension.Version,
                Author = extension.Author,
                Description = extension.Description,
                Hooks = extension.Instance.GetSupportedHooks(),
                Permissions = extension.Instance.GetRequiredPermissions(),
                CreatedAt = extension.LoadedAt
            };
        }

        private void EnsureExtensionDirectory()
        {
            if (!Directory.Exists(_extensionDirectory))
                Directory.CreateDirectory(_extensionDirectory);
        }

        private void OnExtensionError(string extensionId, string message)
        {
            var error = new ExtensionError
            {
                ExtensionId = extensionId,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            ExtensionError?.Invoke(this, error);
        }
    }

    /// <summary>
    /// Interface for studio extensions
    /// </summary>
    public interface IStudioExtension
    {
        string Name { get; }
        string Version { get; }
        string Author { get; }
        string Description { get; }

        Task InitializeAsync();
        Task ShutdownAsync();
        List<string> GetSupportedHooks();
        List<string> GetRequiredPermissions();
    }

    /// <summary>
    /// Extension metadata
    /// </summary>
    public class Extension
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public Assembly Assembly { get; set; }
        public IStudioExtension Instance { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime LoadedAt { get; set; }
    }

    /// <summary>
    /// Extension hook definition
    /// </summary>
    public class ExtensionHook
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Type CallbackType { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    /// <summary>
    /// Extension error
    /// </summary>
    public class ExtensionError
    {
        public string ExtensionId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Extension manifest
    /// </summary>
    public class ExtensionManifest
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public List<string> Hooks { get; set; }
        public List<string> Permissions { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Sample extension implementation
    /// </summary>
    public class SampleExtension : IStudioExtension
    {
        public string Name => "Sample Dashboard Extension";
        public string Version => "1.0.0";
        public string Author => "HELIOS Team";
        public string Description => "A sample extension demonstrating the extension framework";

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync()
        {
            await Task.CompletedTask;
        }

        public List<string> GetSupportedHooks()
        {
            return new List<string>
            {
                "OnDashboardLoad",
                "OnMetricsUpdated",
                "OnAlertTriggered"
            };
        }

        public List<string> GetRequiredPermissions()
        {
            return new List<string>
            {
                "ReadMetrics",
                "ReadAlerts"
            };
        }
    }
}
