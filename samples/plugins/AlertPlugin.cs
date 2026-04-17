using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Plugins.Abstractions;

namespace HELIOS.Platform.Plugins.Samples
{
    /// <summary>
    /// Sample alert plugin demonstrating event publishing and subscription
    /// </summary>
    public class AlertPlugin : PluginBase
    {
        private List<Alert> _alerts = new();
        private Dictionary<string, AlertRule> _rules = new();

        public override string Id => "com.helios.plugins.alerts";
        public override string Name => "HELIOS Alerts";
        public override string Version => "1.0.0";
        public override string Author => "HELIOS Team";
        public override string Description => "Alert management and notification plugin";

        public override IReadOnlyList<string> GetCapabilities()
        {
            return new[] { "alerts", "notifications", "alerting" };
        }

        public override async Task InitializeAsync(IPluginContext context)
        {
            await base.InitializeAsync(context);
            LogInfo("AlertPlugin initialized");

            // Load alert rules from configuration
            var rulesJson = context.Configuration.Get("alert_rules", "{}");
            LogInfo($"Loaded alert configuration");
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();
            LogInfo("AlertPlugin started");

            // Subscribe to events from other plugins
            _context.SubscribeToEvent("system.error", OnSystemError);
            _context.SubscribeToEvent("system.warning", OnSystemWarning);
        }

        public override async Task<PluginCommandResult> ExecuteCommandAsync(
            string commandName,
            Dictionary<string, object> parameters)
        {
            return commandName switch
            {
                "create_alert" => ExecuteCreateAlert(parameters),
                "get_alerts" => ExecuteGetAlerts(parameters),
                "acknowledge_alert" => ExecuteAcknowledgeAlert(parameters),
                "create_rule" => ExecuteCreateRule(parameters),
                "list_rules" => ExecuteListRules(),
                "trigger_alert" => ExecuteTriggerAlert(parameters),
                _ => await base.ExecuteCommandAsync(commandName, parameters)
            };
        }

        private PluginCommandResult ExecuteCreateAlert(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("title", out var title) || !parameters.TryGetValue("message", out var message))
            {
                return PluginCommandResult.Error("'title' and 'message' parameters are required");
            }

            var severity = parameters.ContainsKey("severity") ? parameters["severity"].ToString() : "INFO";
            var source = parameters.ContainsKey("source") ? parameters["source"].ToString() : "Unknown";

            var alert = new Alert
            {
                Id = Guid.NewGuid().ToString(),
                Title = title.ToString(),
                Message = message.ToString(),
                Severity = severity,
                Source = source,
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false
            };

            _alerts.Add(alert);
            LogInfo($"Alert created: {alert.Title}");

            return PluginCommandResult.Ok(alert);
        }

        private PluginCommandResult ExecuteGetAlerts(Dictionary<string, object> parameters)
        {
            var severity = parameters.ContainsKey("severity") ? parameters["severity"].ToString() : null;
            var unacknowledgedOnly = parameters.ContainsKey("unacknowledged_only") 
                ? (bool)parameters["unacknowledged_only"] 
                : false;

            var filtered = _alerts;

            if (!string.IsNullOrEmpty(severity))
            {
                filtered = filtered.FindAll(a => a.Severity == severity);
            }

            if (unacknowledgedOnly)
            {
                filtered = filtered.FindAll(a => !a.IsAcknowledged);
            }

            return PluginCommandResult.Ok(filtered);
        }

        private PluginCommandResult ExecuteAcknowledgeAlert(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("alert_id", out var alertId))
            {
                return PluginCommandResult.Error("'alert_id' parameter is required");
            }

            var alert = _alerts.Find(a => a.Id == alertId.ToString());
            if (alert == null)
            {
                return PluginCommandResult.Error($"Alert '{alertId}' not found");
            }

            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = parameters.ContainsKey("acknowledged_by") ? parameters["acknowledged_by"].ToString() : "System";

            return PluginCommandResult.Ok("Alert acknowledged");
        }

        private PluginCommandResult ExecuteCreateRule(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("name", out var name) || !parameters.TryGetValue("condition", out var condition))
            {
                return PluginCommandResult.Error("'name' and 'condition' parameters are required");
            }

            var rule = new AlertRule
            {
                Id = Guid.NewGuid().ToString(),
                Name = name.ToString(),
                Condition = condition.ToString(),
                CreatedAt = DateTime.UtcNow,
                IsEnabled = true
            };

            _rules[rule.Id] = rule;
            LogInfo($"Alert rule created: {rule.Name}");

            return PluginCommandResult.Ok(rule);
        }

        private PluginCommandResult ExecuteListRules()
        {
            return PluginCommandResult.Ok(new List<AlertRule>(_rules.Values));
        }

        private PluginCommandResult ExecuteTriggerAlert(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("rule_id", out var ruleId))
            {
                return PluginCommandResult.Error("'rule_id' parameter is required");
            }

            if (!_rules.TryGetValue(ruleId.ToString(), out var rule))
            {
                return PluginCommandResult.Error($"Rule '{ruleId}' not found");
            }

            if (!rule.IsEnabled)
            {
                return PluginCommandResult.Error("Rule is disabled");
            }

            var alert = new Alert
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Alert from rule: {rule.Name}",
                Message = $"Rule condition matched: {rule.Condition}",
                Severity = "WARNING",
                Source = "AlertRule",
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false
            };

            _alerts.Add(alert);
            LogInfo($"Alert triggered from rule: {rule.Name}");

            return PluginCommandResult.Ok(alert);
        }

        private async Task OnSystemError(object eventData)
        {
            var errorAlert = new Alert
            {
                Id = Guid.NewGuid().ToString(),
                Title = "System Error",
                Message = eventData?.ToString() ?? "Unknown error",
                Severity = "ERROR",
                Source = "SystemEvent",
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false
            };

            _alerts.Add(errorAlert);
            await Task.CompletedTask;
        }

        private async Task OnSystemWarning(object eventData)
        {
            var warningAlert = new Alert
            {
                Id = Guid.NewGuid().ToString(),
                Title = "System Warning",
                Message = eventData?.ToString() ?? "Unknown warning",
                Severity = "WARNING",
                Source = "SystemEvent",
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false
            };

            _alerts.Add(warningAlert);
            await Task.CompletedTask;
        }

        public override async Task<PluginHealth> GetHealthAsync()
        {
            var health = await base.GetHealthAsync();
            health.Metrics["total_alerts"] = _alerts.Count;
            health.Metrics["unacknowledged_alerts"] = _alerts.FindAll(a => !a.IsAcknowledged).Count;
            health.Metrics["active_rules"] = _rules.Count;
            return health;
        }
    }

    /// <summary>
    /// Alert model
    /// </summary>
    public class Alert
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedBy { get; set; }
    }

    /// <summary>
    /// Alert rule model
    /// </summary>
    public class AlertRule
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Condition { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEnabled { get; set; }
    }
}
