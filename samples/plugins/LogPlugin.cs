using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Plugins.Abstractions;

namespace HELIOS.Platform.Plugins.Samples
{
    /// <summary>
    /// Sample logging plugin that demonstrates plugin development
    /// </summary>
    public class LogPlugin : PluginBase
    {
        private List<LogEntry> _logs = new();
        private bool _isRunning = false;

        public override string Id => "com.helios.plugins.log";
        public override string Name => "HELIOS Logger";
        public override string Version => "1.0.0";
        public override string Author => "HELIOS Team";
        public override string Description => "Centralized logging plugin for HELIOS platform";

        public override IReadOnlyList<string> GetCapabilities()
        {
            return new[] { "logging", "log-query", "log-export" };
        }

        public override async Task InitializeAsync(IPluginContext context)
        {
            await base.InitializeAsync(context);
            LogInfo("LogPlugin initialized");

            // Load configuration
            var maxLogs = context.Configuration.Get("max_logs", 1000);
            LogInfo($"Max logs setting: {maxLogs}");
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();
            _isRunning = true;
            LogInfo("LogPlugin started");

            // Subscribe to log events from other plugins
            _context.SubscribeToEvent("plugin.log", OnLogEventReceived);
        }

        public override async Task StopAsync()
        {
            await base.StopAsync();
            _isRunning = false;
            LogInfo("LogPlugin stopped");
        }

        public override async Task<PluginCommandResult> ExecuteCommandAsync(
            string commandName,
            Dictionary<string, object> parameters)
        {
            return commandName switch
            {
                "write_log" => ExecuteWriteLog(parameters),
                "get_logs" => ExecuteGetLogs(parameters),
                "clear_logs" => ExecuteClearLogs(),
                "get_stats" => ExecuteGetStats(),
                _ => await base.ExecuteCommandAsync(commandName, parameters)
            };
        }

        private PluginCommandResult ExecuteWriteLog(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("message", out var message))
            {
                return PluginCommandResult.Error("'message' parameter is required");
            }

            var level = parameters.ContainsKey("level") ? parameters["level"].ToString() : "INFO";
            var source = parameters.ContainsKey("source") ? parameters["source"].ToString() : "Unknown";

            _logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message.ToString(),
                Source = source
            });

            return PluginCommandResult.Ok("Log entry written");
        }

        private PluginCommandResult ExecuteGetLogs(Dictionary<string, object> parameters)
        {
            var level = parameters.ContainsKey("level") ? parameters["level"].ToString() : null;
            var limit = parameters.ContainsKey("limit") ? (int)parameters["limit"] : 100;

            var filtered = level == null
                ? _logs
                : _logs.FindAll(l => l.Level == level);

            var result = filtered.GetRange(
                Math.Max(0, filtered.Count - limit),
                Math.Min(limit, filtered.Count));

            return PluginCommandResult.Ok(result);
        }

        private PluginCommandResult ExecuteClearLogs()
        {
            var count = _logs.Count;
            _logs.Clear();
            return PluginCommandResult.Ok($"Cleared {count} log entries");
        }

        private PluginCommandResult ExecuteGetStats()
        {
            var stats = new Dictionary<string, object>
            {
                { "total_logs", _logs.Count },
                { "is_running", _isRunning },
                { "uptime_seconds", (DateTime.UtcNow - (_context?.Configuration.Get("start_time", DateTime.UtcNow))).TotalSeconds }
            };

            return PluginCommandResult.Ok(stats);
        }

        private async Task OnLogEventReceived(object eventData)
        {
            if (eventData is Dictionary<string, object> logData)
            {
                var entry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = logData.ContainsKey("level") ? logData["level"].ToString() : "INFO",
                    Message = logData.ContainsKey("message") ? logData["message"].ToString() : "",
                    Source = logData.ContainsKey("source") ? logData["source"].ToString() : "Unknown"
                };

                _logs.Add(entry);
            }

            await Task.CompletedTask;
        }

        public override async Task<PluginHealth> GetHealthAsync()
        {
            var health = await base.GetHealthAsync();
            health.Metrics["log_count"] = _logs.Count;
            health.Metrics["is_accepting_logs"] = _isRunning;
            return health;
        }
    }

    /// <summary>
    /// Log entry model
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
    }
}
