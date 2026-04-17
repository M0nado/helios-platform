using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Plugins.Abstractions;

namespace HELIOS.Platform.Plugins.Samples
{
    /// <summary>
    /// Sample metrics plugin demonstrating monitoring capabilities
    /// </summary>
    public class MetricsPlugin : PluginBase
    {
        private Dictionary<string, double> _metrics = new();
        private DateTime _startTime;

        public override string Id => "com.helios.plugins.metrics";
        public override string Name => "HELIOS Metrics";
        public override string Version => "1.0.0";
        public override string Author => "HELIOS Team";
        public override string Description => "System metrics and monitoring plugin";

        public override IReadOnlyList<string> GetCapabilities()
        {
            return new[] { "metrics", "monitoring", "stats" };
        }

        public override async Task InitializeAsync(IPluginContext context)
        {
            await base.InitializeAsync(context);
            _startTime = DateTime.UtcNow;
            LogInfo("MetricsPlugin initialized");
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();
            LogInfo("MetricsPlugin started");

            // Start periodic metrics collection
            _ = Task.Run(async () => await CollectMetricsAsync());
        }

        public override async Task<PluginCommandResult> ExecuteCommandAsync(
            string commandName,
            Dictionary<string, object> parameters)
        {
            return commandName switch
            {
                "record_metric" => ExecuteRecordMetric(parameters),
                "get_metric" => ExecuteGetMetric(parameters),
                "get_all_metrics" => ExecuteGetAllMetrics(),
                "reset_metrics" => ExecuteResetMetrics(),
                "get_system_info" => ExecuteGetSystemInfo(),
                _ => await base.ExecuteCommandAsync(commandName, parameters)
            };
        }

        private PluginCommandResult ExecuteRecordMetric(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("name", out var name) || !parameters.TryGetValue("value", out var value))
            {
                return PluginCommandResult.Error("'name' and 'value' parameters are required");
            }

            try
            {
                _metrics[name.ToString()] = Convert.ToDouble(value);
                return PluginCommandResult.Ok($"Metric '{name}' recorded");
            }
            catch
            {
                return PluginCommandResult.Error("Invalid metric value");
            }
        }

        private PluginCommandResult ExecuteGetMetric(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("name", out var name))
            {
                return PluginCommandResult.Error("'name' parameter is required");
            }

            var metricName = name.ToString();
            if (_metrics.TryGetValue(metricName, out var value))
            {
                return PluginCommandResult.Ok(new { name = metricName, value });
            }

            return PluginCommandResult.Error($"Metric '{metricName}' not found");
        }

        private PluginCommandResult ExecuteGetAllMetrics()
        {
            return PluginCommandResult.Ok(_metrics);
        }

        private PluginCommandResult ExecuteResetMetrics()
        {
            var count = _metrics.Count;
            _metrics.Clear();
            return PluginCommandResult.Ok($"Reset {count} metrics");
        }

        private PluginCommandResult ExecuteGetSystemInfo()
        {
            var uptime = DateTime.UtcNow - _startTime;
            var info = new
            {
                plugin_name = Name,
                plugin_version = Version,
                uptime_seconds = uptime.TotalSeconds,
                total_metrics_tracked = _metrics.Count,
                timestamp = DateTime.UtcNow,
                state = State
            };

            return PluginCommandResult.Ok(info);
        }

        private async Task CollectMetricsAsync()
        {
            while (State == PluginState.Running)
            {
                try
                {
                    var uptime = (DateTime.UtcNow - _startTime).TotalSeconds;
                    _metrics["uptime_seconds"] = uptime;
                    _metrics["metric_count"] = _metrics.Count;
                    _metrics["timestamp"] = DateTime.UtcNow.Ticks;

                    await Task.Delay(5000); // Collect every 5 seconds
                }
                catch (Exception ex)
                {
                    LogError("Error collecting metrics", ex);
                }
            }
        }

        public override async Task<PluginHealth> GetHealthAsync()
        {
            var health = await base.GetHealthAsync();
            health.Metrics["metrics_count"] = _metrics.Count;
            health.Metrics["uptime_seconds"] = (DateTime.UtcNow - _startTime).TotalSeconds;
            return health;
        }
    }
}
