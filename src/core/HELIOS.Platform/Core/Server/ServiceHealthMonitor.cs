using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Server.Models;
using HealthStatus = HELIOS.Platform.Core.Server.Models.HealthStatus;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Monitors service health with periodic checks, auto-restart, and alerting.
    /// </summary>
    public class ServiceHealthMonitor : IDisposable
    {
        private readonly ServerServiceManager _serviceManager;
        private readonly ProcessManager _processManager;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _monitoringTask;
        private bool _isRunning;
        private readonly object _lock = new();
        private readonly int _healthCheckIntervalSeconds;
        private readonly Dictionary<string, HealthCheckResult> _lastResults = new();

        public event EventHandler<HealthCheckFailedEventArgs>? HealthCheckFailed;
        public event EventHandler<ServiceRestartedEventArgs>? ServiceRestarted;
        public event EventHandler<HealthAlertEventArgs>? HealthAlert;

        public ServiceHealthMonitor(ServerServiceManager serviceManager, ProcessManager processManager, int healthCheckIntervalSeconds = 30)
        {
            _serviceManager = serviceManager;
            _processManager = processManager;
            _healthCheckIntervalSeconds = healthCheckIntervalSeconds;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the health monitoring loop.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning)
                    return;

                _isRunning = true;
                _monitoringTask = RunHealthMonitoringLoop(_cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Stops the health monitoring loop.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                _cancellationTokenSource.Cancel();
            }

            _monitoringTask?.Wait(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Performs a single health check for a service.
        /// </summary>
        public async Task<HealthCheckResult> CheckServiceHealthAsync(string serviceId)
        {
            var service = _serviceManager.GetService(serviceId);
            if (service == null)
                return new HealthCheckResult { Status = HealthStatus.Unknown, Message = "Service not found" };

            try
            {
                var result = new HealthCheckResult { ServiceId = serviceId, CheckTime = DateTime.UtcNow };

                if (service.Status != ServiceStatus.Running)
                {
                    result.Status = HealthStatus.Unhealthy;
                    result.Message = $"Service is not running. Status: {service.Status}";
                    return result;
                }

                if (service.ProcessId.HasValue)
                {
                    var processInfo = _processManager.GetProcess(service.ProcessId.Value);
                    if (processInfo == null)
                    {
                        result.Status = HealthStatus.Unhealthy;
                        result.Message = "Process information not available";
                        return result;
                    }

                    if (!processInfo.IsResponding)
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = "Process is not responding";
                        return result;
                    }

                    // Check for excessive restarts
                    if (service.RestartsLastHour > 10)
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = $"Excessive restarts detected: {service.RestartsLastHour} in last hour";
                        return result;
                    }

                    // Check memory usage
                    if (processInfo.MemoryLimit.HasValue && processInfo.MemoryUsage > processInfo.MemoryLimit.Value * 0.9)
                    {
                        result.Status = HealthStatus.Warning;
                        result.Message = $"Memory usage is near limit: {processInfo.MemoryUsage / (1024 * 1024)}MB";
                        return result;
                    }
                }

                result.Status = HealthStatus.Healthy;
                result.Message = "Service is healthy";
                return result;
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    ServiceId = serviceId,
                    Status = HealthStatus.Unknown,
                    Message = $"Health check error: {ex.Message}",
                    CheckTime = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets the last health check result for a service.
        /// </summary>
        public HealthCheckResult? GetLastHealthCheck(string serviceId)
        {
            lock (_lock)
            {
                return _lastResults.TryGetValue(serviceId, out var result) ? result : null;
            }
        }

        /// <summary>
        /// Gets all last health check results.
        /// </summary>
        public Dictionary<string, HealthCheckResult> GetAllLastHealthChecks()
        {
            lock (_lock)
            {
                return new Dictionary<string, HealthCheckResult>(_lastResults);
            }
        }

        private async Task RunHealthMonitoringLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var services = _serviceManager.GetAllServices();

                    foreach (var service in services)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var result = await CheckServiceHealthAsync(service.ServiceId);

                        lock (_lock)
                        {
                            _lastResults[service.ServiceId] = result;
                        }

                        await UpdateServiceHealthStatus(service, result);

                        if (result.Status == HealthStatus.Unhealthy)
                        {
                            OnHealthCheckFailed(service.ServiceId, result.Message);

                            if (service.AutoRestartEnabled && service.CurrentRestartAttempts < service.MaxRestartAttempts)
                            {
                                await HandleFailedService(service);
                            }
                            else if (service.CurrentRestartAttempts >= service.MaxRestartAttempts)
                            {
                                OnHealthAlert(service.ServiceId, $"Max restart attempts ({service.MaxRestartAttempts}) exceeded");
                            }
                        }
                        else if (result.Status == HealthStatus.Healthy)
                        {
                            lock (_lock)
                            {
                                service.CurrentRestartAttempts = 0;
                            }
                        }
                    }

                    await Task.Delay(_healthCheckIntervalSeconds * 1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in health monitoring loop: {ex.Message}");
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }

        private async Task UpdateServiceHealthStatus(ServiceInfo service, HealthCheckResult result)
        {
            lock (_lock)
            {
                service.HealthStatus = result.Status;
                service.LastHealthCheckTime = result.CheckTime;
                service.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async Task HandleFailedService(ServiceInfo service)
        {
            lock (_lock)
            {
                service.CurrentRestartAttempts++;
                service.RestartsLastHour++;
            }

            OnHealthAlert(service.ServiceId, $"Attempting restart ({service.CurrentRestartAttempts}/{service.MaxRestartAttempts})");

            try
            {
                await Task.Delay(2000);
                await _serviceManager.RestartServiceAsync(service.ServiceId);

                OnServiceRestarted(service.ServiceId, service.CurrentRestartAttempts);
            }
            catch (Exception ex)
            {
                OnHealthAlert(service.ServiceId, $"Restart failed: {ex.Message}");
            }
        }

        private void OnHealthCheckFailed(string serviceId, string message)
        {
            HealthCheckFailed?.Invoke(this, new HealthCheckFailedEventArgs { ServiceId = serviceId, Message = message });
        }

        private void OnServiceRestarted(string serviceId, int attemptNumber)
        {
            ServiceRestarted?.Invoke(this, new ServiceRestartedEventArgs { ServiceId = serviceId, AttemptNumber = attemptNumber });
        }

        private void OnHealthAlert(string serviceId, string message)
        {
            HealthAlert?.Invoke(this, new HealthAlertEventArgs { ServiceId = serviceId, AlertMessage = message });
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Represents the result of a single health check.
    /// </summary>
    public class HealthCheckResult
    {
        public string ServiceId { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
        public double? CpuUsage { get; set; }
        public long? MemoryUsage { get; set; }
        public TimeSpan? Uptime { get; set; }
    }

    /// <summary>
    /// Event arguments for health check failures.
    /// </summary>
    public class HealthCheckFailedEventArgs : EventArgs
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event arguments for service restarts.
    /// </summary>
    public class ServiceRestartedEventArgs : EventArgs
    {
        public string ServiceId { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
    }

    /// <summary>
    /// Event arguments for health alerts.
    /// </summary>
    public class HealthAlertEventArgs : EventArgs
    {
        public string ServiceId { get; set; } = string.Empty;
        public string AlertMessage { get; set; } = string.Empty;
    }
}
