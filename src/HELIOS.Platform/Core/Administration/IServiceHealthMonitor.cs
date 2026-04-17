using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Administration;

public class ServiceDependency
{
    public string ServiceName { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public bool IsHealthy { get; set; }
    public DateTime LastStatusCheck { get; set; }
}

public class ServiceHealthData
{
    public string ServiceName { get; set; }
    public ServiceHealthStatus Status { get; set; }
    public int ResponseTimeMs { get; set; }
    public double CPUUsagePercent { get; set; }
    public int MemoryUsageMB { get; set; }
    public int ThreadCount { get; set; }
    public DateTime CheckedAt { get; set; }
}

public enum ServiceHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Offline,
    Unknown
}

public interface IServiceHealthMonitor
{
    Task<ServiceHealthData> CheckServiceHealthAsync(string serviceName);
    Task<List<ServiceHealthData>> CheckAllServicesAsync();
    Task<bool> RegisterServiceAsync(string serviceName, List<string> dependencies);
    Task<bool> UnregisterServiceAsync(string serviceName);
    Task<List<ServiceDependency>> GetServiceDependenciesAsync();
    Task<bool> RestartUnhealthyServicesAsync();
    Task<Dictionary<string, ServiceHealthStatus>> GetServiceStatusSummaryAsync();
    Task<List<ServiceHealthData>> GetHistoryAsync(string serviceName, int limit = 100);
}

public class AnomalyResult
{
    public string AnomalyId { get; set; }
    public string MetricName { get; set; }
    public double DetectedValue { get; set; }
    public double ExpectedValue { get; set; }
    public double Deviation { get; set; }
    public string Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
}

public class RemediationAction
{
    public string ActionId { get; set; }
    public string ActionType { get; set; }
    public string AnomalyId { get; set; }
    public string Status { get; set; }
    public DateTime ExecutedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
}

public interface IAnomalyDetectionService
{
    Task<AnomalyResult> DetectAnomalyAsync(string metricName, double value, double baseline);
    Task<List<AnomalyResult>> GetActiveAnomaliesAsync();
    Task<bool> ResolveAnomalyAsync(string anomalyId);
    Task<RemediationAction> ExecuteRemediationAsync(string anomalyId, string actionType);
    Task<List<RemediationAction>> GetRemediationHistoryAsync(int limit = 100);
    Task<double> CalculateBaselineAsync(string metricName, int hoursBack = 24);
    Task<Dictionary<string, int>> GetAnomalySummaryAsync();
}

public class ServiceDependencyMonitor : IServiceHealthMonitor
{
    private readonly Dictionary<string, ServiceDependency> _services = new();
    private readonly List<ServiceHealthData> _healthHistory = new();

    public async Task<ServiceHealthData> CheckServiceHealthAsync(string serviceName)
    {
        var service = _services.ContainsKey(serviceName) ? _services[serviceName] : null;
        
        var healthData = new ServiceHealthData
        {
            ServiceName = serviceName,
            Status = service?.IsHealthy == true ? ServiceHealthStatus.Healthy : ServiceHealthStatus.Unhealthy,
            ResponseTimeMs = Random.Shared.Next(50, 200),
            CPUUsagePercent = Random.Shared.Next(5, 40),
            MemoryUsageMB = Random.Shared.Next(50, 300),
            ThreadCount = Random.Shared.Next(5, 50),
            CheckedAt = DateTime.UtcNow
        };

        _healthHistory.Add(healthData);
        return await Task.FromResult(healthData);
    }

    public async Task<List<ServiceHealthData>> CheckAllServicesAsync()
    {
        var results = new List<ServiceHealthData>();
        
        foreach (var serviceName in _services.Keys)
        {
            var health = await CheckServiceHealthAsync(serviceName);
            results.Add(health);
        }

        return await Task.FromResult(results);
    }

    public async Task<bool> RegisterServiceAsync(string serviceName, List<string> dependencies)
    {
        _services[serviceName] = new ServiceDependency
        {
            ServiceName = serviceName,
            Dependencies = dependencies,
            IsHealthy = true,
            LastStatusCheck = DateTime.UtcNow
        };

        return await Task.FromResult(true);
    }

    public async Task<bool> UnregisterServiceAsync(string serviceName)
    {
        if (!_services.ContainsKey(serviceName))
            return await Task.FromResult(false);

        _services.Remove(serviceName);
        return await Task.FromResult(true);
    }

    public async Task<List<ServiceDependency>> GetServiceDependenciesAsync()
    {
        return await Task.FromResult(_services.Values.ToList());
    }

    public async Task<bool> RestartUnhealthyServicesAsync()
    {
        var unhealthyCount = 0;
        
        foreach (var service in _services.Values)
        {
            if (!service.IsHealthy)
            {
                service.IsHealthy = true;
                unhealthyCount++;
            }
        }

        return await Task.FromResult(unhealthyCount > 0);
    }

    public async Task<Dictionary<string, ServiceHealthStatus>> GetServiceStatusSummaryAsync()
    {
        var summary = new Dictionary<string, ServiceHealthStatus>();
        
        foreach (var service in _services)
        {
            var status = service.Value.IsHealthy ? ServiceHealthStatus.Healthy : ServiceHealthStatus.Unhealthy;
            summary[service.Key] = status;
        }

        return await Task.FromResult(summary);
    }

    public async Task<List<ServiceHealthData>> GetHistoryAsync(string serviceName, int limit = 100)
    {
        var history = _healthHistory
            .Where(h => h.ServiceName == serviceName)
            .OrderByDescending(h => h.CheckedAt)
            .Take(limit)
            .ToList();

        return await Task.FromResult(history);
    }
}

public class AnomalyDetectionEngine : IAnomalyDetectionService
{
    private readonly List<AnomalyResult> _anomalies = new();
    private readonly List<RemediationAction> _remediationHistory = new();
    private readonly Dictionary<string, List<double>> _metricValues = new();

    public async Task<AnomalyResult> DetectAnomalyAsync(string metricName, double value, double baseline)
    {
        if (!_metricValues.ContainsKey(metricName))
            _metricValues[metricName] = new List<double>();

        _metricValues[metricName].Add(value);

        var deviation = Math.Abs(value - baseline) / baseline * 100;
        var isAnomaly = deviation > 20.0;

        if (!isAnomaly)
            return await Task.FromResult<AnomalyResult>(null);

        var anomaly = new AnomalyResult
        {
            AnomalyId = Guid.NewGuid().ToString(),
            MetricName = metricName,
            DetectedValue = value,
            ExpectedValue = baseline,
            Deviation = deviation,
            Severity = deviation > 50 ? "Critical" : deviation > 30 ? "High" : "Medium",
            DetectedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _anomalies.Add(anomaly);
        return await Task.FromResult(anomaly);
    }

    public async Task<List<AnomalyResult>> GetActiveAnomaliesAsync()
    {
        var active = _anomalies.Where(a => !a.IsResolved).ToList();
        return await Task.FromResult(active);
    }

    public async Task<bool> ResolveAnomalyAsync(string anomalyId)
    {
        var anomaly = _anomalies.FirstOrDefault(a => a.AnomalyId == anomalyId);
        if (anomaly == null)
            return await Task.FromResult(false);

        anomaly.IsResolved = true;
        return await Task.FromResult(true);
    }

    public async Task<RemediationAction> ExecuteRemediationAsync(string anomalyId, string actionType)
    {
        var anomaly = _anomalies.FirstOrDefault(a => a.AnomalyId == anomalyId);
        if (anomaly == null)
            return await Task.FromResult<RemediationAction>(null);

        var action = new RemediationAction
        {
            ActionId = Guid.NewGuid().ToString(),
            ActionType = actionType,
            AnomalyId = anomalyId,
            Status = "Executing",
            ExecutedAt = DateTime.UtcNow,
            IsSuccessful = Random.Shared.Next(100) > 20
        };

        _remediationHistory.Add(action);
        
        if (action.IsSuccessful)
        {
            anomaly.IsResolved = true;
            action.Status = "Completed";
        }
        else
        {
            action.Status = "Failed";
            action.ErrorMessage = "Remediation action encountered an error";
        }

        return await Task.FromResult(action);
    }

    public async Task<List<RemediationAction>> GetRemediationHistoryAsync(int limit = 100)
    {
        var history = _remediationHistory.OrderByDescending(r => r.ExecutedAt).Take(limit).ToList();
        return await Task.FromResult(history);
    }

    public async Task<double> CalculateBaselineAsync(string metricName, int hoursBack = 24)
    {
        if (!_metricValues.ContainsKey(metricName) || _metricValues[metricName].Count == 0)
            return await Task.FromResult(100.0);

        var values = _metricValues[metricName];
        var baseline = values.Count > 0 ? values.Average() : 100.0;

        return await Task.FromResult(baseline);
    }

    public async Task<Dictionary<string, int>> GetAnomalySummaryAsync()
    {
        var summary = new Dictionary<string, int>
        {
            ["Total"] = _anomalies.Count,
            ["Active"] = _anomalies.Count(a => !a.IsResolved),
            ["Resolved"] = _anomalies.Count(a => a.IsResolved),
            ["Critical"] = _anomalies.Count(a => a.Severity == "Critical"),
            ["High"] = _anomalies.Count(a => a.Severity == "High"),
            ["Medium"] = _anomalies.Count(a => a.Severity == "Medium")
        };

        return await Task.FromResult(summary);
    }
}
