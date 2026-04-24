using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Presentation.Studio.Models;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Manages dashboard alerts with configuration, history, and notifications
    /// </summary>
    public class AlertManagementService
    {
        private readonly List<AlertConfiguration> _alertConfigs;
        private readonly List<AlertHistory> _alertHistory;
        private readonly AlertNotifier _notifier;
        private CancellationTokenSource _evaluationToken;

        public event EventHandler<AlertConfiguration> AlertConfigCreated;
        public event EventHandler<string> AlertConfigDeleted;
        public event EventHandler<AlertHistory> AlertHistoryRecorded;

        public AlertManagementService()
        {
            _alertConfigs = new List<AlertConfiguration>();
            _alertHistory = new List<AlertHistory>();
            _notifier = new AlertNotifier();
            InitializeDefaultConfigurations();
        }

        /// <summary>
        /// Create a new alert configuration
        /// </summary>
        public AlertConfiguration CreateAlertConfig(string name, AlertCondition condition, AlertAction action)
        {
            var config = new AlertConfiguration
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Condition = condition,
                Action = action,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _alertConfigs.Add(config);
            AlertConfigCreated?.Invoke(this, config);

            return config;
        }

        /// <summary>
        /// Update an alert configuration
        /// </summary>
        public void UpdateAlertConfig(string configId, AlertConfiguration updated)
        {
            var existing = _alertConfigs.FirstOrDefault(c => c.Id == configId);
            if (existing != null)
            {
                existing.Name = updated.Name;
                existing.Condition = updated.Condition;
                existing.Action = updated.Action;
                existing.IsEnabled = updated.IsEnabled;
            }
        }

        /// <summary>
        /// Delete an alert configuration
        /// </summary>
        public bool DeleteAlertConfig(string configId)
        {
            var removed = _alertConfigs.RemoveAll(c => c.Id == configId) > 0;
            if (removed)
                AlertConfigDeleted?.Invoke(this, configId);
            return removed;
        }

        /// <summary>
        /// Get all alert configurations
        /// </summary>
        public IEnumerable<AlertConfiguration> GetAlertConfigurations(bool enabledOnly = false)
        {
            if (enabledOnly)
                return _alertConfigs.Where(c => c.IsEnabled);
            return _alertConfigs;
        }

        /// <summary>
        /// Evaluate an alert configuration against current metrics
        /// </summary>
        public bool EvaluateAlert(AlertConfiguration config, SystemMetrics metrics)
        {
            if (!config.IsEnabled)
                return false;

            var condition = config.Condition;

            switch (condition.MetricType)
            {
                case "CPU":
                    return EvaluateCondition(metrics.CpuUsagePercent, condition);
                case "Memory":
                    return EvaluateCondition(metrics.MemoryUsagePercent, condition);
                case "Disk":
                    return EvaluateCondition(metrics.DiskUsagePercent, condition);
                case "Network":
                    return EvaluateCondition(metrics.NetworkBytesPerSecond, condition);
                case "GPU":
                    return EvaluateCondition(metrics.GpuUsagePercent, condition);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Execute alert action
        /// </summary>
        public async Task ExecuteAlertActionAsync(AlertConfiguration config)
        {
            var action = config.Action;

            switch (action.ActionType)
            {
                case "Notification":
                    await _notifier.SendNotificationAsync(
                        action.Title ?? config.Name,
                        action.Message ?? "Alert triggered",
                        config.Condition.MetricType);
                    break;

                case "Email":
                    if (!string.IsNullOrEmpty(action.Email))
                        await _notifier.SendEmailAsync(action.Email, config.Name, action.Message);
                    break;

                case "Script":
                    if (!string.IsNullOrEmpty(action.ScriptPath))
                        ExecuteScript(action.ScriptPath);
                    break;

                case "Log":
                    await _notifier.LogAlertAsync(config.Name, action.Message);
                    break;
            }

            RecordAlertHistory(config, AlertHistoryEventType.Triggered);
        }

        /// <summary>
        /// Get alert history
        /// </summary>
        public IEnumerable<AlertHistory> GetAlertHistory(string configId = null, int days = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            var query = _alertHistory.Where(h => h.Timestamp >= cutoff);

            if (!string.IsNullOrEmpty(configId))
                query = query.Where(h => h.ConfigId == configId);

            return query.OrderByDescending(h => h.Timestamp);
        }

        /// <summary>
        /// Clear alert history older than specified days
        /// </summary>
        public void ClearOldHistory(int daysToKeep = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
            _alertHistory.RemoveAll(h => h.Timestamp < cutoff);
        }

        /// <summary>
        /// Start automatic alert evaluation
        /// </summary>
        public void StartEvaluation(SystemMetrics currentMetrics, int intervalSeconds = 5)
        {
            _evaluationToken = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_evaluationToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(intervalSeconds * 1000, _evaluationToken.Token);
                        // Metrics would be passed in from the dashboard service
                        // This is a placeholder for the evaluation loop
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });
        }

        /// <summary>
        /// Stop automatic alert evaluation
        /// </summary>
        public void StopEvaluation()
        {
            _evaluationToken?.Cancel();
        }

        private bool EvaluateCondition(double value, AlertCondition condition)
        {
            switch (condition.Operator)
            {
                case "GreaterThan":
                    return value > condition.Threshold;
                case "LessThan":
                    return value < condition.Threshold;
                case "GreaterOrEqual":
                    return value >= condition.Threshold;
                case "LessOrEqual":
                    return value <= condition.Threshold;
                case "Equal":
                    return Math.Abs(value - condition.Threshold) < 0.01;
                default:
                    return false;
            }
        }

        private void RecordAlertHistory(AlertConfiguration config, AlertHistoryEventType eventType)
        {
            var history = new AlertHistory
            {
                Id = Guid.NewGuid().ToString("N"),
                ConfigId = config.Id,
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                AlertName = config.Name
            };

            _alertHistory.Add(history);
            AlertHistoryRecorded?.Invoke(this, history);

            // Maintain history size
            if (_alertHistory.Count > 10000)
                _alertHistory.RemoveRange(0, _alertHistory.Count - 10000);
        }

        private void ExecuteScript(string scriptPath)
        {
            try
            {
                if (scriptPath.EndsWith(".ps1"))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    var process = System.Diagnostics.Process.Start(startInfo);
                    process?.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to execute script: {ex.Message}");
            }
        }

        private void InitializeDefaultConfigurations()
        {
            // Create default alert configurations
            var highCpuAlert = new AlertConfiguration
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "High CPU Usage",
                Condition = new AlertCondition
                {
                    MetricType = "CPU",
                    Operator = "GreaterThan",
                    Threshold = 80
                },
                Action = new AlertAction
                {
                    ActionType = "Notification",
                    Title = "High CPU Alert",
                    Message = "CPU usage exceeded 80%"
                },
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            var highMemoryAlert = new AlertConfiguration
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "High Memory Usage",
                Condition = new AlertCondition
                {
                    MetricType = "Memory",
                    Operator = "GreaterThan",
                    Threshold = 85
                },
                Action = new AlertAction
                {
                    ActionType = "Notification",
                    Title = "High Memory Alert",
                    Message = "Memory usage exceeded 85%"
                },
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _alertConfigs.Add(highCpuAlert);
            _alertConfigs.Add(highMemoryAlert);
        }
    }

    /// <summary>
    /// Alert configuration for threshold-based alerts
    /// </summary>
    public class AlertConfiguration
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public AlertCondition Condition { get; set; }
        public AlertAction Action { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Alert trigger condition
    /// </summary>
    public class AlertCondition
    {
        public string MetricType { get; set; } // CPU, Memory, Disk, Network, GPU
        public string Operator { get; set; } // GreaterThan, LessThan, Equal, etc.
        public double Threshold { get; set; }
        public int DurationSeconds { get; set; } = 0; // How long condition must be true
    }

    /// <summary>
    /// Alert action when condition is met
    /// </summary>
    public class AlertAction
    {
        public string ActionType { get; set; } // Notification, Email, Script, Log
        public string Title { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public string ScriptPath { get; set; }
    }

    /// <summary>
    /// Alert history entry
    /// </summary>
    public class AlertHistory
    {
        public string Id { get; set; }
        public string ConfigId { get; set; }
        public string AlertName { get; set; }
        public AlertHistoryEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum AlertHistoryEventType
    {
        Triggered,
        Resolved,
        ConfigCreated,
        ConfigDeleted,
        ConfigModified
    }

    /// <summary>
    /// Alert notification handler
    /// </summary>
    internal class AlertNotifier
    {
        public async Task SendNotificationAsync(string title, string message, string source)
        {
            // Implementation would send system notification
            await Task.CompletedTask;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Implementation would send email
            await Task.CompletedTask;
        }

        public async Task LogAlertAsync(string alertName, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[ALERT] {alertName}: {message}");
            await Task.CompletedTask;
        }
    }
}
