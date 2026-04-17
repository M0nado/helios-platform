using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// Alert and notification service for real-time alerting on metric thresholds and anomalies.
/// </summary>
public interface IAlertingService
{
    /// <summary>
    /// Create an alert rule.
    /// </summary>
    Task<string> CreateAlertRuleAsync(AlertRule rule);

    /// <summary>
    /// Update an alert rule.
    /// </summary>
    Task<bool> UpdateAlertRuleAsync(string ruleId, AlertRule rule);

    /// <summary>
    /// Delete an alert rule.
    /// </summary>
    Task<bool> DeleteAlertRuleAsync(string ruleId);

    /// <summary>
    /// Evaluate alert rules and trigger notifications.
    /// </summary>
    Task<List<AlertFiring>> EvaluateAlertsAsync();

    /// <summary>
    /// Get alert history.
    /// </summary>
    Task<List<AlertEvent>> GetAlertHistoryAsync(string? ruleId = null, TimeSpan? period = null);

    /// <summary>
    /// Acknowledge a firing alert.
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(string alertId, string acknowledgedBy);

    /// <summary>
    /// Silence an alert for a period.
    /// </summary>
    Task<bool> SilenceAlertAsync(string ruleId, TimeSpan duration);

    /// <summary>
    /// Get active alerts.
    /// </summary>
    Task<List<AlertFiring>> GetActiveAlertsAsync();

    /// <summary>
    /// Get alert statistics.
    /// </summary>
    Task<AlertStatistics> GetAlertStatisticsAsync(TimeSpan? period = null);

    /// <summary>
    /// Configure notification channel (email, Slack, PagerDuty, etc).
    /// </summary>
    Task<string> ConfigureNotificationChannelAsync(NotificationChannel channel);

    /// <summary>
    /// Delete notification channel.
    /// </summary>
    Task<bool> DeleteNotificationChannelAsync(string channelId);

    /// <summary>
    /// Test notification channel.
    /// </summary>
    Task<bool> TestNotificationChannelAsync(string channelId);
}

/// <summary>
/// Alert rule definition.
/// </summary>
public class AlertRule
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MetricQuery { get; set; } = string.Empty;
    public AlertCondition Condition { get; set; } = new();
    public TimeSpan EvaluationInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan ForDuration { get; set; } = TimeSpan.FromMinutes(5);
    public List<string> NotificationChannels { get; set; } = new();
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public bool IsEnabled { get; set; } = true;
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Alert condition (threshold evaluation).
/// </summary>
public class AlertCondition
{
    public AlertOperator Operator { get; set; }
    public double Threshold { get; set; }
    public string? AggregationMethod { get; set; }
}

/// <summary>
/// Alert comparison operators.
/// </summary>
public enum AlertOperator
{
    GreaterThan,
    LessThan,
    Equal,
    NotEqual,
    GreaterOrEqual,
    LessOrEqual
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Fired/active alert.
/// </summary>
public class AlertFiring
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
    public double Value { get; set; }
    public double Threshold { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
}

/// <summary>
/// Alert event in history.
/// </summary>
public class AlertEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string RuleId { get; set; } = string.Empty;
    public AlertEventType EventType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double? Value { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Alert event type.
/// </summary>
public enum AlertEventType
{
    Firing,
    Resolved,
    Acknowledged,
    Silenced
}

/// <summary>
/// Notification channel configuration.
/// </summary>
public class NotificationChannel
{
    public string? Id { get; set; }
    public NotificationChannelType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Config { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Notification channel types.
/// </summary>
public enum NotificationChannelType
{
    Email,
    Slack,
    MicrosoftTeams,
    PagerDuty,
    Webhook,
    SMS
}

/// <summary>
/// Alert statistics.
/// </summary>
public class AlertStatistics
{
    public int TotalRules { get; set; }
    public int ActiveAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int AcknowledgedAlerts { get; set; }
    public Dictionary<AlertSeverity, int> CountBySeverity { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public int FalsePositives { get; set; }
}
