using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace MonadoBlade.GUI.Performance
{
    /// <summary>
    /// Lazy loading manager for on-demand component loading (200 LOC)
    /// Improves startup performance and memory efficiency by deferring component initialization.
    /// </summary>
    public class LazyLoadingManager
    {
        private readonly Dictionary<string, Lazy<Task<object>>> _lazyComponents = new();
        private readonly Dictionary<string, object> _loadedComponents = new();
        private readonly object _lockObject = new();

        public event Action<string> ComponentLoadStarted;
        public event Action<string> ComponentLoadCompleted;
        public event Action<string, Exception> ComponentLoadFailed;

        /// <summary>
        /// Register a lazy-loadable component
        /// </summary>
        public void RegisterLazyComponent(string componentName, 
            Func<Task<object>> componentFactory)
        {
            lock (_lockObject)
            {
                if (_lazyComponents.ContainsKey(componentName))
                    throw new InvalidOperationException($"Component {componentName} already registered");

                _lazyComponents[componentName] = new Lazy<Task<object>>(componentFactory);
            }
        }

        /// <summary>
        /// Load a component asynchronously
        /// </summary>
        public async Task<object> LoadComponentAsync(string componentName)
        {
            if (string.IsNullOrWhiteSpace(componentName))
                throw new ArgumentNullException(nameof(componentName));

            lock (_lockObject)
            {
                if (!_lazyComponents.ContainsKey(componentName))
                    throw new KeyNotFoundException($"Component {componentName} not found");

                // Check if already loaded
                if (_loadedComponents.ContainsKey(componentName))
                    return _loadedComponents[componentName];
            }

            try
            {
                ComponentLoadStarted?.Invoke(componentName);

                var lazyLoader = _lazyComponents[componentName];
                var component = await lazyLoader.Value;

                lock (_lockObject)
                {
                    _loadedComponents[componentName] = component;
                }

                ComponentLoadCompleted?.Invoke(componentName);
                return component;
            }
            catch (Exception ex)
            {
                ComponentLoadFailed?.Invoke(componentName, ex);
                throw;
            }
        }

        /// <summary>
        /// Load multiple components in parallel
        /// </summary>
        public async Task<Dictionary<string, object>> LoadComponentsAsync(
            params string[] componentNames)
        {
            var tasks = new List<Task<(string Name, object Component)>>();

            foreach (var name in componentNames)
            {
                tasks.Add(LoadComponentAsyncInternal(name));
            }

            var results = await Task.WhenAll(tasks);
            var dictionary = new Dictionary<string, object>();

            foreach (var (name, component) in results)
            {
                dictionary[name] = component;
            }

            return dictionary;
        }

        /// <summary>
        /// Internal helper for parallel loading
        /// </summary>
        private async Task<(string Name, object Component)> LoadComponentAsyncInternal(string name)
        {
            var component = await LoadComponentAsync(name);
            return (name, component);
        }

        /// <summary>
        /// Check if component is already loaded
        /// </summary>
        public bool IsComponentLoaded(string componentName)
        {
            lock (_lockObject)
            {
                return _loadedComponents.ContainsKey(componentName);
            }
        }

        /// <summary>
        /// Get loaded component without async loading
        /// </summary>
        public object GetLoadedComponent(string componentName)
        {
            lock (_lockObject)
            {
                return _loadedComponents.ContainsKey(componentName) 
                    ? _loadedComponents[componentName] 
                    : null;
            }
        }

        /// <summary>
        /// Get all loaded components
        /// </summary>
        public Dictionary<string, object> GetAllLoadedComponents()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, object>(_loadedComponents);
            }
        }

        /// <summary>
        /// Unload component and free resources
        /// </summary>
        public void UnloadComponent(string componentName)
        {
            lock (_lockObject)
            {
                if (_loadedComponents.ContainsKey(componentName))
                {
                    var component = _loadedComponents[componentName];
                    
                    // Dispose if implements IDisposable
                    if (component is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    _loadedComponents.Remove(componentName);
                }
            }
        }

        /// <summary>
        /// Clear all loaded components
        /// </summary>
        public void UnloadAll()
        {
            lock (_lockObject)
            {
                foreach (var component in _loadedComponents.Values)
                {
                    if (component is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _loadedComponents.Clear();
            }
        }

        /// <summary>
        /// Get memory usage of loaded components
        /// </summary>
        public long GetMemoryUsage()
        {
            lock (_lockObject)
            {
                long totalBytes = 0;
                foreach (var component in _loadedComponents.Values)
                {
                    if (component != null)
                    {
                        try
                        {
                            totalBytes += 
                                System.Runtime.InteropServices.Marshal.SizeOf(component);
                        }
                        catch
                        {
                            // Some objects don't support SizeOf, skip them
                        }
                    }
                }
                return totalBytes;
            }
        }

        /// <summary>
        /// Get loading statistics
        /// </summary>
        public LoadingStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                return new LoadingStatistics
                {
                    TotalRegistered = _lazyComponents.Count,
                    TotalLoaded = _loadedComponents.Count,
                    MemoryUsageBytes = GetMemoryUsage()
                };
            }
        }
    }

    /// <summary>
    /// Statistics about component loading
    /// </summary>
    public class LoadingStatistics
    {
        public int TotalRegistered { get; set; }
        public int TotalLoaded { get; set; }
        public long MemoryUsageBytes { get; set; }

        public double MemoryUsageMB => MemoryUsageBytes / (1024.0 * 1024.0);
    }
}
