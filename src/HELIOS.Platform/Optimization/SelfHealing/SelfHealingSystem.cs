namespace HELIOS.Platform.Optimization.SelfHealing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Critical
}

public class HealthCheckResult
{
    public string ComponentName { get; set; }
    public HealthStatus Status { get; set; }
    public string Message { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public double ResponseTimeMs { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public interface IHealthChecker
{
    Task<HealthCheckResult> CheckHealthAsync(string componentName);
    Task<Dictionary<string, HealthCheckResult>> CheckAllComponentsAsync();
    Task RegisterComponentAsync(string name, Func<Task<HealthCheckResult>> checker);
    Task<HealthCheckResult> GetLastResultAsync(string componentName);
}

public interface IAutoRepair
{
    Task<bool> AttemptRepairAsync(HealthCheckResult healthResult);
    Task RegisterRepairStrategyAsync(string componentName, Func<Task<bool>> repairStrategy);
}

public class HealthChecker : IHealthChecker
{
    private readonly Dictionary<string, Func<Task<HealthCheckResult>>> _checkers = new();
    private readonly Dictionary<string, HealthCheckResult> _lastResults = new();
    private readonly object _lockObj = new();

    public async Task<HealthCheckResult> CheckHealthAsync(string componentName)
    {
        if (!_checkers.TryGetValue(componentName, out var checker))
            throw new ArgumentException($"No health checker registered for {componentName}");

        var startTime = DateTime.UtcNow;
        var result = await checker();
        result.ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

        lock (_lockObj)
        {
            _lastResults[componentName] = result;
        }

        return result;
    }

    public async Task<Dictionary<string, HealthCheckResult>> CheckAllComponentsAsync()
    {
        var results = new Dictionary<string, HealthCheckResult>();
        var tasks = _checkers.Keys.Select(name => CheckHealthAsync(name));
        var allResults = await Task.WhenAll(tasks);

        int index = 0;
        foreach (var name in _checkers.Keys)
        {
            results[name] = allResults[index++];
        }

        return results;
    }

    public async Task RegisterComponentAsync(string name, Func<Task<HealthCheckResult>> checker)
    {
        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                _checkers[name] = checker;
            }
        });
    }

    public async Task<HealthCheckResult> GetLastResultAsync(string componentName)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                return _lastResults.TryGetValue(componentName, out var result)
                    ? result
                    : null;
            }
        });
    }
}

public class AutoRepair : IAutoRepair
{
    private readonly IHealthChecker _healthChecker;
    private readonly Dictionary<string, Func<Task<bool>>> _repairStrategies = new();
    private readonly object _lockObj = new();

    public AutoRepair(IHealthChecker healthChecker)
    {
        _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));
    }

    public async Task<bool> AttemptRepairAsync(HealthCheckResult healthResult)
    {
        if (healthResult.Status == HealthStatus.Healthy)
            return true;

        if (!_repairStrategies.TryGetValue(healthResult.ComponentName, out var strategy))
            return false;

        try
        {
            return await strategy();
        }
        catch (Exception ex)
        {
            healthResult.Message = $"Repair failed: {ex.Message}";
            return false;
        }
    }

    public async Task RegisterRepairStrategyAsync(string componentName, Func<Task<bool>> repairStrategy)
    {
        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                _repairStrategies[componentName] = repairStrategy;
            }
        });
    }
}

public class CircuitBreaker
{
    private enum State { Closed, Open, HalfOpen }

    private State _state = State.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private int _successCount = 0;
    private readonly int _failureThreshold = 5;
    private readonly int _successThreshold = 2;
    private readonly TimeSpan _openDuration = TimeSpan.FromSeconds(30);
    private readonly object _lockObj = new();

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        lock (_lockObj)
        {
            switch (_state)
            {
                case State.Open:
                    if (DateTime.UtcNow - _lastFailureTime > _openDuration)
                    {
                        _state = State.HalfOpen;
                        _successCount = 0;
                    }
                    else
                    {
                        throw new InvalidOperationException("Circuit breaker is open. Service temporarily unavailable.");
                    }
                    break;
            }
        }

        try
        {
            var result = await operation();
            RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure();
            throw;
        }
    }

    private void RecordSuccess()
    {
        lock (_lockObj)
        {
            if (_state == State.HalfOpen)
            {
                _successCount++;
                if (_successCount >= _successThreshold)
                {
                    _state = State.Closed;
                    _failureCount = 0;
                }
            }
        }
    }

    private void RecordFailure()
    {
        lock (_lockObj)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == State.HalfOpen || _failureCount >= _failureThreshold)
            {
                _state = State.Open;
            }
        }
    }

    public string GetState()
    {
        lock (_lockObj)
        {
            return _state.ToString();
        }
    }
}

public class SelfDiagnostics
{
    private readonly List<DiagnosticReport> _reports = new();
    private readonly object _lockObj = new();

    public class DiagnosticReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ComponentName { get; set; }
        public HealthStatus Status { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public async Task<DiagnosticReport> GenerateDiagnosticsAsync(string componentName, HealthCheckResult healthResult)
    {
        return await Task.Run(() =>
        {
            var report = new DiagnosticReport
            {
                ComponentName = componentName,
                Status = healthResult.Status
            };

            // Identify issues
            if (healthResult.Status != HealthStatus.Healthy)
            {
                report.Issues.Add(healthResult.Message);

                if (healthResult.ResponseTimeMs > 1000)
                    report.Issues.Add("Slow response time detected");
            }

            // Generate recommendations
            if (report.Issues.Any())
            {
                if (healthResult.ResponseTimeMs > 1000)
                    report.Recommendations.Add("Optimize database queries or add caching");
                if (healthResult.Status == HealthStatus.Critical)
                    report.Recommendations.Add("Escalate to on-call engineer immediately");
            }

            lock (_lockObj)
            {
                _reports.Add(report);
            }

            return report;
        });
    }

    public async Task<List<DiagnosticReport>> GetRecentReportsAsync(int count = 10)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                return _reports.OrderByDescending(r => r.GeneratedAt).Take(count).ToList();
            }
        });
    }
}
