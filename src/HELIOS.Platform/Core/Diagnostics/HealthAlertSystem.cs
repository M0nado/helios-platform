using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// Health alerts and notifications system
    /// </summary>
    public class HealthAlertSystem
    {
        private static readonly ILogger _logger = Log.ForContext<HealthAlertSystem>();
        private readonly List<AlertRule> _alertRules = new();
        private readonly List<Alert> _alerts = new();
        private readonly List<IAlertHandler> _handlers = new();
        private readonly object _lock = new();

        /// <summary>
        /// Register an alert handler
        /// </summary>
        public void RegisterHandler(IAlertHandler handler)
        {
            lock (_lock)
            {
                _handlers.Add(handler);
                _logger.Information("Registered alert handler: {HandlerType}", handler.GetType().Name);
            }
        }

        /// <summary>
        /// Add an alert rule
        /// </summary>
        public void AddAlertRule(AlertRule rule)
        {
            lock (_lock)
            {
                _alertRules.Add(rule);
                _logger.Information("Added alert rule: {RuleName}", rule.Name);
            }
        }

        /// <summary>
        /// Remove an alert rule
        /// </summary>
        public void RemoveAlertRule(string ruleName)
        {
            lock (_lock)
            {
                _alertRules.RemoveAll(r => r.Name == ruleName);
                _logger.Information("Removed alert rule: {RuleName}", ruleName);
            }
        }

        /// <summary>
        /// Check health status and trigger alerts if necessary
        /// </summary>
        public void CheckHealthStatus(OverallHealthStatus healthStatus)
        {
            lock (_lock)
            {
                foreach (var rule in _alertRules)
                {
                    if (rule.IsTriggered(healthStatus))
                    {
                        CreateAndDispatchAlert(rule, healthStatus);
                    }
                }
            }
        }

        /// <summary>
        /// Create and dispatch an alert
        /// </summary>
        private void CreateAndDispatchAlert(AlertRule rule, OverallHealthStatus healthStatus)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid().ToString(),
                RuleName = rule.Name,
                Severity = rule.Severity,
                Title = rule.Title,
                Description = rule.Description,
                CreatedAt = DateTime.UtcNow,
                Status = AlertStatus.Active,
                HealthStatus = healthStatus
            };

            _alerts.Add(alert);
            _logger.Warning("Alert triggered: {AlertTitle}", alert.Title);

            // Dispatch to handlers
            foreach (var handler in _handlers)
            {
                try
                {
                    handler.HandleAlert(alert);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error handling alert with handler {HandlerType}", handler.GetType().Name);
                }
            }
        }

        /// <summary>
        /// Get all active alerts
        /// </summary>
        public List<Alert> GetActiveAlerts()
        {
            lock (_lock)
            {
                return _alerts.Where(a => a.Status == AlertStatus.Active).ToList();
            }
        }

        /// <summary>
        /// Get alerts by severity
        /// </summary>
        public List<Alert> GetAlertsBySeverity(AlertSeverity severity)
        {
            lock (_lock)
            {
                return _alerts.Where(a => a.Severity == severity && a.Status == AlertStatus.Active).ToList();
            }
        }

        /// <summary>
        /// Acknowledge an alert
        /// </summary>
        public void AcknowledgeAlert(string alertId, string acknowledgedBy = null)
        {
            lock (_lock)
            {
                var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
                if (alert != null)
                {
                    alert.Status = AlertStatus.Acknowledged;
                    alert.AcknowledgedAt = DateTime.UtcNow;
                    alert.AcknowledgedBy = acknowledgedBy;
                    _logger.Information("Alert acknowledged: {AlertId}", alertId);
                }
            }
        }

        /// <summary>
        /// Resolve an alert
        /// </summary>
        public void ResolveAlert(string alertId, string resolvedBy = null)
        {
            lock (_lock)
            {
                var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
                if (alert != null)
                {
                    alert.Status = AlertStatus.Resolved;
                    alert.ResolvedAt = DateTime.UtcNow;
                    alert.ResolvedBy = resolvedBy;
                    _logger.Information("Alert resolved: {AlertId}", alertId);
                }
            }
        }

        /// <summary>
        /// Get all alerts
        /// </summary>
        public List<Alert> GetAllAlerts()
        {
            lock (_lock)
            {
                return new List<Alert>(_alerts);
            }
        }

        /// <summary>
        /// Clear old resolved alerts
        /// </summary>
        public void ClearOldAlerts(TimeSpan retentionPeriod)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow.Subtract(retentionPeriod);
                var removed = _alerts.RemoveAll(a => a.Status == AlertStatus.Resolved && a.ResolvedAt < cutoff);
                _logger.Information("Cleared {RemovedCount} old alerts", removed);
            }
        }

        /// <summary>
        /// Get alert statistics
        /// </summary>
        public AlertStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new AlertStatistics
                {
                    TotalAlerts = _alerts.Count,
                    ActiveAlerts = _alerts.Count(a => a.Status == AlertStatus.Active),
                    AcknowledgedAlerts = _alerts.Count(a => a.Status == AlertStatus.Acknowledged),
                    ResolvedAlerts = _alerts.Count(a => a.Status == AlertStatus.Resolved),
                    CriticalAlerts = _alerts.Count(a => a.Status == AlertStatus.Active && a.Severity == AlertSeverity.Critical),
                    WarningAlerts = _alerts.Count(a => a.Status == AlertStatus.Active && a.Severity == AlertSeverity.Warning),
                    InfoAlerts = _alerts.Count(a => a.Status == AlertStatus.Active && a.Severity == AlertSeverity.Information)
                };
            }
        }
    }

    /// <summary>
    /// Interface for alert handlers
    /// </summary>
    public interface IAlertHandler
    {
        /// <summary>
        /// Handle an alert
        /// </summary>
        void HandleAlert(Alert alert);
    }

    /// <summary>
    /// Alert rule definition
    /// </summary>
    public class AlertRule
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public AlertSeverity Severity { get; set; }
        public Func<OverallHealthStatus, bool> Condition { get; set; }

        /// <summary>
        /// Check if this rule is triggered
        /// </summary>
        public bool IsTriggered(OverallHealthStatus healthStatus)
        {
            return Condition?.Invoke(healthStatus) ?? false;
        }
    }

    /// <summary>
    /// Alert severity
    /// </summary>
    public enum AlertSeverity
    {
        Information,
        Warning,
        Critical
    }

    /// <summary>
    /// Alert status
    /// </summary>
    public enum AlertStatus
    {
        Active,
        Acknowledged,
        Resolved
    }

    /// <summary>
    /// Alert notification
    /// </summary>
    public class Alert
    {
        public string Id { get; set; }
        public string RuleName { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public AlertStatus Status { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
        public OverallHealthStatus HealthStatus { get; set; }
    }

    /// <summary>
    /// Alert statistics
    /// </summary>
    public class AlertStatistics
    {
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public int AcknowledgedAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public int CriticalAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public int InfoAlerts { get; set; }
    }

    /// <summary>
    /// Console alert handler (for logging)
    /// </summary>
    public class ConsoleAlertHandler : IAlertHandler
    {
        private static readonly ILogger _logger = Log.ForContext<ConsoleAlertHandler>();

        public void HandleAlert(Alert alert)
        {
            _logger.Warning("ALERT [{Severity}] {Title}: {Description}", 
                alert.Severity, alert.Title, alert.Description);
        }
    }
}
