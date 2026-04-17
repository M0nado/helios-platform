using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Diagnostics
{
    /// <summary>
    /// Core health diagnostics engine
    /// </summary>
    public class HealthDiagnosticsEngine
    {
        private static readonly ILogger _logger = Log.ForContext<HealthDiagnosticsEngine>();
        private readonly Dictionary<string, IHealthCheck> _healthChecks = new();
        private readonly Dictionary<string, HealthStatus> _statusCache = new();
        private DateTime _lastCheckTime = DateTime.MinValue;

        /// <summary>
        /// Register a health check
        /// </summary>
        public void RegisterHealthCheck(string name, IHealthCheck check)
        {
            _healthChecks[name] = check;
            _logger.Information("Registered health check: {CheckName}", name);
        }

        /// <summary>
        /// Unregister a health check
        /// </summary>
        public void UnregisterHealthCheck(string name)
        {
            _healthChecks.Remove(name);
            _statusCache.Remove(name);
            _logger.Information("Unregistered health check: {CheckName}", name);
        }

        /// <summary>
        /// Run a specific health check
        /// </summary>
        public async Task<HealthStatus> RunHealthCheckAsync(string name)
        {
            if (!_healthChecks.TryGetValue(name, out var check))
            {
                return new HealthStatus 
                { 
                    CheckName = name, 
                    Status = HealthStatusCode.Unknown,
                    Message = "Health check not found"
                };
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var status = await check.CheckAsync();
                sw.Stop();
                
                status.CheckName = name;
                status.CheckDurationMs = sw.ElapsedMilliseconds;
                status.CheckedAt = DateTime.UtcNow;
                
                _statusCache[name] = status;
                _logger.Debug("Health check {CheckName} completed in {Duration}ms with status {Status}",
                    name, sw.ElapsedMilliseconds, status.Status);

                return status;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running health check: {CheckName}", name);
                return new HealthStatus
                {
                    CheckName = name,
                    Status = HealthStatusCode.Failed,
                    Message = $"Exception: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Run all health checks
        /// </summary>
        public async Task<OverallHealthStatus> RunAllHealthChecksAsync()
        {
            var sw = Stopwatch.StartNew();
            var results = new List<HealthStatus>();

            foreach (var checkName in _healthChecks.Keys.ToList())
            {
                var status = await RunHealthCheckAsync(checkName);
                results.Add(status);
            }

            sw.Stop();
            _lastCheckTime = DateTime.UtcNow;

            var overallStatus = DetermineOverallStatus(results);
            overallStatus.CheckedAt = DateTime.UtcNow;
            overallStatus.TotalCheckDurationMs = sw.ElapsedMilliseconds;

            _logger.Information("All health checks completed. Overall status: {Status}", overallStatus.Status);

            return overallStatus;
        }

        /// <summary>
        /// Get the cached status for a health check
        /// </summary>
        public HealthStatus GetCachedStatus(string name)
        {
            return _statusCache.TryGetValue(name, out var status) ? status : null;
        }

        /// <summary>
        /// Get all cached statuses
        /// </summary>
        public IEnumerable<HealthStatus> GetAllCachedStatuses()
        {
            return _statusCache.Values.ToList();
        }

        /// <summary>
        /// Determine overall health status from individual checks
        /// </summary>
        private OverallHealthStatus DetermineOverallStatus(List<HealthStatus> statuses)
        {
            var overall = new OverallHealthStatus
            {
                IndividualStatuses = statuses
            };

            if (statuses.Any(s => s.Status == HealthStatusCode.Critical))
            {
                overall.Status = HealthStatusCode.Critical;
                overall.Message = "One or more critical health checks failed";
            }
            else if (statuses.Any(s => s.Status == HealthStatusCode.Failed))
            {
                overall.Status = HealthStatusCode.Failed;
                overall.Message = "One or more health checks failed";
            }
            else if (statuses.Any(s => s.Status == HealthStatusCode.Degraded))
            {
                overall.Status = HealthStatusCode.Degraded;
                overall.Message = "System health is degraded";
            }
            else if (statuses.All(s => s.Status == HealthStatusCode.Healthy))
            {
                overall.Status = HealthStatusCode.Healthy;
                overall.Message = "All health checks passed";
            }
            else
            {
                overall.Status = HealthStatusCode.Unknown;
                overall.Message = "Unable to determine overall health status";
            }

            return overall;
        }
    }

    /// <summary>
    /// Interface for health checks
    /// </summary>
    public interface IHealthCheck
    {
        /// <summary>
        /// Perform the health check
        /// </summary>
        Task<HealthStatus> CheckAsync();
    }

    /// <summary>
    /// Health status codes
    /// </summary>
    public enum HealthStatusCode
    {
        Healthy = 0,
        Degraded = 1,
        Failed = 2,
        Critical = 3,
        Unknown = 99
    }

    /// <summary>
    /// Individual health check status
    /// </summary>
    public class HealthStatus
    {
        public string CheckName { get; set; }
        public HealthStatusCode Status { get; set; }
        public string Message { get; set; }
        public DateTime CheckedAt { get; set; }
        public long CheckDurationMs { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Overall health status combining all checks
    /// </summary>
    public class OverallHealthStatus
    {
        public HealthStatusCode Status { get; set; }
        public string Message { get; set; }
        public DateTime CheckedAt { get; set; }
        public long TotalCheckDurationMs { get; set; }
        public List<HealthStatus> IndividualStatuses { get; set; } = new();

        public int HealthyCount => IndividualStatuses.Count(s => s.Status == HealthStatusCode.Healthy);
        public int DegradedCount => IndividualStatuses.Count(s => s.Status == HealthStatusCode.Degraded);
        public int FailedCount => IndividualStatuses.Count(s => s.Status == HealthStatusCode.Failed);
        public int CriticalCount => IndividualStatuses.Count(s => s.Status == HealthStatusCode.Critical);
    }
}
