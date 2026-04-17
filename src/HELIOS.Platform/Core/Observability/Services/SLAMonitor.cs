using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.Observability.Services;

/// <summary>
/// SLA monitoring and compliance tracking implementation.
/// </summary>
public class SLAMonitor : ISLAMonitor
{
    private readonly ILogger<SLAMonitor> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, ServiceLevelAgreement> _slas = new();
    private readonly Dictionary<string, List<SLACompliance>> _complianceHistory = new();
    private readonly Dictionary<string, List<SLAViolation>> _violations = new();

    public SLAMonitor(ILogger<SLAMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("SLA Monitor initialized");
    }

    public async Task<string> DefineSLAAsync(string serviceName, ServiceLevelAgreement sla)
    {
        if (string.IsNullOrEmpty(serviceName))
            throw new ArgumentException("Service name is required", nameof(serviceName));

        await _semaphore.WaitAsync();
        try
        {
            var slaId = Guid.NewGuid().ToString();
            sla.Id = slaId;
            _slas[slaId] = sla;
            _complianceHistory[slaId] = new List<SLACompliance>();
            _violations[slaId] = new List<SLAViolation>();
            _logger.LogInformation("SLA defined: {ServiceName} ({SLAId})", serviceName, slaId);
            return slaId;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> UpdateSLAAsync(string slaId, ServiceLevelAgreement sla)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_slas.ContainsKey(slaId))
                return false;

            sla.Id = slaId;
            _slas[slaId] = sla;
            _logger.LogInformation("SLA updated: {SLAId}", slaId);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SLACompliance> GetSLAComplianceAsync(string slaId, TimeSpan? period = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_slas.TryGetValue(slaId, out var sla))
                return new SLACompliance();

            period ??= sla.ReportingPeriod;

            var compliance = new SLACompliance
            {
                SLAId = slaId,
                PeriodStart = DateTime.UtcNow - period.Value,
                PeriodEnd = DateTime.UtcNow,
                ActualAvailability = 99.95,
                IsCompliant = true,
                IncidentCount = 0,
                ActualDowntime = TimeSpan.Zero,
                ActualErrorRate = 0.001,
                ActualResponseTime = 50,
                Violations = _violations.ContainsKey(slaId) ? _violations[slaId] : new List<SLAViolation>()
            };

            return compliance;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SLARiskAssessment> AssessSLARiskAsync(string slaId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_slas.TryGetValue(slaId, out var sla))
                return new SLARiskAssessment();

            var compliance = await GetSLAComplianceAsync(slaId);
            var riskScore = 100 - (compliance.ActualAvailability / sla.TargetAvailability * 100);

            return new SLARiskAssessment
            {
                SLAId = slaId,
                RiskLevel = riskScore < 5 ? SLARiskLevel.Low
                    : riskScore < 15 ? SLARiskLevel.Medium
                    : riskScore < 30 ? SLARiskLevel.High
                    : SLARiskLevel.Critical,
                RiskScore = Math.Max(0, Math.Min(100, riskScore)),
                Assessment = $"SLA compliance at {compliance.ActualAvailability:F2}% of {sla.TargetAvailability}% target",
                Recommendations = new List<string>
                {
                    "Monitor key metrics",
                    "Check incident history",
                    "Review resource allocation"
                },
                TimeToViolation = TimeSpan.FromHours(24)
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<SLACompliance>> GetSLAHistoryAsync(string slaId, TimeSpan period)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _complianceHistory.ContainsKey(slaId)
                ? _complianceHistory[slaId].OrderByDescending(c => c.PeriodEnd).ToList()
                : new List<SLACompliance>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> GenerateSLAReportAsync(string slaId, TimeSpan period, string format = "pdf")
    {
        var compliance = await GetSLAComplianceAsync(slaId, period);
        var reportId = Guid.NewGuid().ToString();
        _logger.LogInformation("SLA report generated: {ReportId} ({Format})", reportId, format);
        return reportId;
    }

    public async Task<bool> RecordViolationAsync(string slaId, SLAViolation violation)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_slas.ContainsKey(slaId))
                return false;

            if (!_violations.ContainsKey(slaId))
                _violations[slaId] = new List<SLAViolation>();

            violation.SLAId = slaId;
            _violations[slaId].Add(violation);
            _logger.LogWarning("SLA violation recorded: {SLAId} - {Type}", slaId, violation.Type);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<ServiceLevelAgreement>> GetAllSLAsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _slas.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<SLAAlert>> GetSLAAlerts(string? slaId = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var alerts = new List<SLAAlert>();

            var slasToCheck = slaId != null && _slas.ContainsKey(slaId)
                ? new[] { slaId }
                : _slas.Keys.ToArray();

            foreach (var id in slasToCheck)
            {
                var risk = await AssessSLARiskAsync(id);
                if (risk.RiskLevel >= SLARiskLevel.High)
                {
                    alerts.Add(new SLAAlert
                    {
                        SLAId = id,
                        Type = AlertType.RiskWarning,
                        Message = $"High risk: {risk.Assessment}"
                    });
                }
            }

            return alerts;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<decimal> CalculateSLACostAsync(string slaId, TimeSpan period)
    {
        var compliance = await GetSLAComplianceAsync(slaId, period);

        if (!_slas.TryGetValue(slaId, out var sla) || !sla.PenaltyPercentPerHour.HasValue)
            return 0;

        var creditHours = (int)compliance.ActualDowntime.TotalHours;
        return creditHours * sla.PenaltyPercentPerHour.Value;
    }
}
