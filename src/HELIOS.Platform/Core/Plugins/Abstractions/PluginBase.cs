using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Plugins.Abstractions
{
    /// <summary>
    /// Base class for implementing plugins
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        protected PluginState _state = PluginState.Created;
        protected IPluginContext _context;
        protected PluginLogger _logger;

        public virtual string Id { get; protected set; }
        public virtual string Name { get; protected set; }
        public virtual string Version { get; protected set; } = "1.0.0";
        public virtual string Author { get; protected set; }
        public virtual string Description { get; protected set; }

        public PluginState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    var oldState = _state;
                    _state = value;
                    StateChanged?.Invoke(this, new PluginStateChangedEventArgs
                    {
                        PluginId = Id,
                        OldState = oldState,
                        NewState = value,
                        Timestamp = DateTime.UtcNow,
                        Reason = "State transition"
                    });
                }
            }
        }

        public virtual IReadOnlyList<PluginDependency> Dependencies
        {
            get => new List<PluginDependency>();
        }

        public event EventHandler<PluginStateChangedEventArgs> StateChanged;

        protected PluginBase()
        {
            _logger = new PluginLogger(GetType().Name);
        }

        public virtual async Task InitializeAsync(IPluginContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            State = PluginState.Initialized;
            await Task.CompletedTask;
        }

        public virtual async Task StartAsync()
        {
            if (State != PluginState.Initialized)
                throw new InvalidOperationException($"Cannot start plugin in {State} state");
            State = PluginState.Running;
            await Task.CompletedTask;
        }

        public virtual async Task StopAsync()
        {
            if (State != PluginState.Running)
                throw new InvalidOperationException($"Cannot stop plugin in {State} state");
            State = PluginState.Stopped;
            await Task.CompletedTask;
        }

        public virtual async Task ReloadConfigAsync()
        {
            await _context?.Configuration?.ReloadAsync();
        }

        public virtual IPluginMetadata GetMetadata()
        {
            return new PluginMetadata
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Author = Author,
                Description = Description
            };
        }

        public virtual async Task<PluginCommandResult> ExecuteCommandAsync(
            string commandName, 
            Dictionary<string, object> parameters)
        {
            return await Task.FromResult(
                PluginCommandResult.Error($"Command '{commandName}' not supported"));
        }

        public virtual IReadOnlyList<string> GetCapabilities()
        {
            return new List<string>();
        }

        public virtual async Task<PluginHealth> GetHealthAsync()
        {
            return await Task.FromResult(new PluginHealth
            {
                IsHealthy = State == PluginState.Running,
                Status = State.ToString(),
                LastCheck = DateTime.UtcNow
            });
        }

        public virtual void Dispose()
        {
            if (State == PluginState.Running)
            {
                StopAsync().Wait();
            }
            State = PluginState.Unloaded;
        }

        protected void LogDebug(string message) => _logger.Debug(message);
        protected void LogInfo(string message) => _logger.Info(message);
        protected void LogWarning(string message) => _logger.Warning(message);
        protected void LogError(string message, Exception ex = null) => _logger.Error(message, ex);
    }

    /// <summary>
    /// Default plugin logger implementation
    /// </summary>
    public class PluginLogger : IPluginLogger
    {
        private readonly string _pluginName;

        public PluginLogger(string pluginName)
        {
            _pluginName = pluginName;
        }

        public void Debug(string message) => Console.WriteLine($"[DEBUG][{_pluginName}] {message}");
        public void Info(string message) => Console.WriteLine($"[INFO][{_pluginName}] {message}");
        public void Warning(string message) => Console.WriteLine($"[WARN][{_pluginName}] {message}");
        public void Error(string message, Exception ex = null)
        {
            Console.WriteLine($"[ERROR][{_pluginName}] {message}");
            if (ex != null) Console.WriteLine($"Exception: {ex}");
        }
        public void Critical(string message, Exception ex = null)
        {
            Console.WriteLine($"[CRITICAL][{_pluginName}] {message}");
            if (ex != null) Console.WriteLine($"Exception: {ex}");
        }
    }

    /// <summary>
    /// Default plugin metadata implementation
    /// </summary>
    public class PluginMetadata : IPluginMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string License { get; set; } = "MIT";
        public string Homepage { get; set; }
        public Dictionary<string, object> Tags { get; set; } = new();
    }

    /// <summary>
    /// Default plugin configuration implementation
    /// </summary>
    public class PluginConfiguration : IPluginConfiguration
    {
        private readonly Dictionary<string, object> _config = new();

        public PluginConfiguration(Dictionary<string, object> initialConfig = null)
        {
            if (initialConfig != null)
            {
                foreach (var item in initialConfig)
                {
                    _config[item.Key] = item.Value;
                }
            }
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_config.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            _config[key] = value;
        }

        public Dictionary<string, object> GetAll()
        {
            return new Dictionary<string, object>(_config);
        }

        public async Task ReloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
