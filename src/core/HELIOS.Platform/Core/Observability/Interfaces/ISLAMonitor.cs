using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// SLA (Service Level Agreement) monitoring and compliance tracking.
/// </summary>
public interface ISLAMonitor
{
    /// <summary>
    /// Define an SLA for a service.
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="sla">SLA definition</param>
    Task<string> DefineSLAAsync(string serviceName, ServiceLevelAgreement sla);

    /// <summary>
    /// Update SLA definition.
    /// </summary>
    Task<bool> UpdateSLAAsync(string slaId, ServiceLevelAgreement sla);

    /// <summary>
    /// Get current SLA compliance.
    /// </summary>
    Task<SLACompliance> GetSLAComplianceAsync(string slaId, TimeSpan? period = null);

    /// <summary>
    /// Check if SLA is at risk of breach.
    /// </summary>
    Task<SLARiskAssessment> AssessSLARiskAsync(string slaId);

    /// <summary>
    /// Get SLA history and trends.
    /// </summary>
    Task<List<SLACompliance>> GetSLAHistoryAsync(string slaId, TimeSpan period);

    /// <summary>
    /// Generate SLA compliance report.
    /// </summary>
    Task<string> GenerateSLAReportAsync(string slaId, TimeSpan period, string format = "pdf");

    /// <summary>
    /// Record SLA violation event.
    /// </summary>
    Task<bool> RecordViolationAsync(string slaId, SLAViolation violation);

    /// <summary>
    /// Get all SLAs.
    /// </summary>
    Task<List<ServiceLevelAgreement>> GetAllSLAsAsync();

    /// <summary>
    /// Get SLA alerts.
    /// </summary>
    Task<List<SLAAlert>> GetSLAAlerts(string? slaId = null);

    /// <summary>
    /// Calculate SLA cost impact (for billing).
    /// </summary>
    Task<decimal> CalculateSLACostAsync(string slaId, TimeSpan period);
}

/// <summary>
/// Service Level Agreement definition.
/// </summary>
public class ServiceLevelAgreement
{
    public string? Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double TargetAvailability { get; set; } // 99.9, 99.99, etc
    public TimeSpan MaxDowntime { get; set; } // For reporting period
    public int MaxIncidents { get; set; }
    public double MaxResponseTime { get; set; } // ms
    public double MaxErrorRate { get; set; } // percentage
    public TimeSpan ReportingPeriod { get; set; } = TimeSpan.FromDays(30);
    public decimal? PenaltyPercentPerHour { get; set; } // For credits
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// SLA compliance status.
/// </summary>
public class SLACompliance
{
    public string SLAId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double ActualAvailability { get; set; }
    public bool IsCompliant { get; set; }
    public int IncidentCount { get; set; }
    public TimeSpan ActualDowntime { get; set; }
    public double ActualErrorRate { get; set; }
    public double ActualResponseTime { get; set; }
    public decimal? CreditOwed { get; set; }
    public List<SLAViolation> Violations { get; set; } = new();
}

/// <summary>
/// SLA violation event.
/// </summary>
public class SLAViolation
{
    public string ViolationId { get; set; } = Guid.NewGuid().ToString();
    public string SLAId { get; set; } = string.Empty;
    public DateTime ViolationTime { get; set; } = DateTime.UtcNow;
    public SLAViolationType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public double Severity { get; set; } // 0-100
    public string? AffectedService { get; set; }
    public string? RootCause { get; set; }
}

/// <summary>
/// Types of SLA violations.
/// </summary>
public enum SLAViolationType
{
    Availability,
    ResponseTime,
    ErrorRate,
    Incident,
    Custom
}

/// <summary>
/// SLA risk assessment.
/// </summary>
public class SLARiskAssessment
{
    public string SLAId { get; set; } = string.Empty;
    public SLARiskLevel RiskLevel { get; set; }
    public double RiskScore { get; set; } // 0-100
    public string Assessment { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public TimeSpan TimeToViolation { get; set; }
}

/// <summary>
/// Risk level enumeration.
/// </summary>
public enum SLARiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// SLA alert.
/// </summary>
public class SLAAlert
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public string SLAId { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public DateTime AlertTime { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// SLA alert type.
/// </summary>
public enum AlertType
{
    RiskWarning,
    Violation,
    Approaching
}
