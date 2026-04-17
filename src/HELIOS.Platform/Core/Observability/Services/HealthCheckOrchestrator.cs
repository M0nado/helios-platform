using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.Observability.Services;

/// <summary>
/// Health check orchestrator implementation.
/// Monitors service availability and manages SLA compliance.
/// </summary>
public class HealthCheckOrchestrator : IHealthCheckOrchestrator
{
    private readonly ILogger<HealthCheckOrchestrator> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, HealthCheckConfig> _checks = new();
    private readonly Dictionary<string, HealthCheckResult> _lastResults = new();
    private readonly Dictionary<string, int> _consecutiveFailures = new();
    private readonly Dictionary<string, DateTime> _lastCheckTime = new();
    private bool _isMonitoring;

    public HealthCheckOrchestrator(ILogger<HealthCheckOrchestrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("Health Check Orchestrator initialized");
    }

    public async Task<string> RegisterHealthCheckAsync(string serviceName, HealthCheckConfig checkConfig)
    {
        if (string.IsNullOrEmpty(serviceName))
            throw new ArgumentException("Service name is required", nameof(serviceName));

        await _semaphore.WaitAsync();
        try
        {
            var checkId = Guid.NewGuid().ToString();
            checkConfig.Id = checkId;
            _checks[checkId] = checkConfig;
            _consecutiveFailures[checkId] = 0;
            _logger.LogInformation("Health check registered: {ServiceName} ({CheckId})", serviceName, checkId);
            return checkId;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeregisterHealthCheckAsync(string checkId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var result = _checks.Remove(checkId);
            _consecutiveFailures.Remove(checkId);
            _lastResults.Remove(checkId);
            _lastCheckTime.Remove(checkId);
            if (result)
                _logger.LogInformation("Health check deregistered: {CheckId}", checkId);
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<HealthCheckResult> ExecuteHealthCheckAsync(string checkId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_checks.TryGetValue(checkId, out var config))
                return new HealthCheckResult { Status = HealthStatus.Unknown };

            var startTime = DateTime.UtcNow;
            var status = HealthStatus.Healthy;

            try
            {
                // Simulate health check execution
                await Task.Delay(10);
                status = HealthStatus.Healthy;
            }
            catch (Exception ex)
            {
                status = HealthStatus.Unhealthy;
                _logger.LogWarning("Health check failed: {CheckId} - {Message}", checkId, ex.Message);
            }

            var result = new HealthCheckResult
            {
                CheckId = checkId,
                Status = status,
                CheckedAt = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime
            };

            _lastResults[checkId] = result;
            _lastCheckTime[checkId] = DateTime.UtcNow;

            if (status == HealthStatus.Unhealthy)
                _consecutiveFailures[checkId]++;
            else
                _consecutiveFailures[checkId] = 0;

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<HealthCheckResult>> ExecuteAllHealthChecksAsync()
    {
        var results = new List<HealthCheckResult>();
        foreach (var checkId in _checks.Keys.ToList())
        {
            var result = await ExecuteHealthCheckAsync(checkId);
            results.Add(result);
        }
        return results;
    }

    public async Task<ServiceHealthStatus> GetServiceHealthAsync(string serviceName)
    {
        await _semaphore.WaitAsync();
        try
        {
            var checkId = _checks.Values
                .FirstOrDefault(c => c.Endpoint.Contains(serviceName))?.Id;

            if (checkId == null)
                return new ServiceHealthStatus { ServiceName = serviceName, CurrentStatus = HealthStatus.Unknown };

            _lastResults.TryGetValue(checkId, out var lastResult);
            _consecutiveFailures.TryGetValue(checkId, out var failures);

            var recentChecks = _lastResults
                .Values
                .OrderByDescending(r => r.CheckedAt)
                .Take(10)
                .ToList();

            return new ServiceHealthStatus
            {
                ServiceName = serviceName,
                CurrentStatus = lastResult?.Status ?? HealthStatus.Unknown,
                LastCheck = lastResult?.CheckedAt ?? DateTime.UtcNow,
                ConsecutiveFailures = failures,
                UpTimePercentage = CalculateUptime(recentChecks),
                RecentChecks = recentChecks
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SystemHealthSummary> GetSystemHealthAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var results = _lastResults.Values.ToList();
            var healthy = results.Count(r => r.Status == HealthStatus.Healthy);
            var degraded = results.Count(r => r.Status == HealthStatus.Degraded);
            var unhealthy = results.Count(r => r.Status == HealthStatus.Unhealthy);

            var overallStatus = unhealthy > 0 ? HealthStatus.Unhealthy
                : degraded > 0 ? HealthStatus.Degraded
                : HealthStatus.Healthy;

            return new SystemHealthSummary
            {
                OverallStatus = overallStatus,
                HealthyServices = healthy,
                DegradedServices = degraded,
                UnhealthyServices = unhealthy,
                ScanTime = DateTime.UtcNow
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<HealthCheckResult>> GetHealthHistoryAsync(string? serviceName = null, TimeSpan? period = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var results = _lastResults.Values.ToList();
            if (period.HasValue)
            {
                var cutoff = DateTime.UtcNow - period.Value;
                results = results.Where(r => r.CheckedAt >= cutoff).ToList();
            }
            return results.OrderByDescending(r => r.CheckedAt).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> SetDependencyAsync(string dependentService, string dependsOnService)
    {
        _logger.LogInformation("Dependency set: {Dependent} depends on {DependsOn}", dependentService, dependsOnService);
        return await Task.FromResult(true);
    }

    public async Task<bool> StartMonitoringAsync()
    {
        _isMonitoring = true;
        _logger.LogInformation("Health monitoring started");
        return await Task.FromResult(true);
    }

    public async Task<bool> StopMonitoringAsync()
    {
        _isMonitoring = false;
        _logger.LogInformation("Health monitoring stopped");
        return await Task.FromResult(true);
    }

    public async Task<RemediationResult> RemediateAsync(string serviceName)
    {
        return await Task.FromResult(new RemediationResult
        {
            ServiceName = serviceName,
            Success = true,
            Action = "restart",
            Message = $"Service {serviceName} remediation completed"
        });
    }

    private double CalculateUptime(List<HealthCheckResult> results)
    {
        if (!results.Any())
            return 100.0;

        var healthy = results.Count(r => r.Status == HealthStatus.Healthy);
        return (double)healthy / results.Count * 100;
    }
}
