using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Server.Models;
using ManagedServiceType = HELIOS.Platform.Core.Server.Models.ServiceType;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Manages Windows services and custom processes with dependency resolution and clustering support.
    /// </summary>
    public class ServerServiceManager
    {
        private readonly Dictionary<string, ServiceInfo> _services = new();
        private readonly object _lock = new();

        public event EventHandler<ServiceStatusChangedEventArgs>? ServiceStatusChanged;
        public event EventHandler<ServiceErrorEventArgs>? ServiceError;

        /// <summary>
        /// Registers a service for management.
        /// </summary>
        public void RegisterService(ServiceInfo service)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(service.ServiceId))
                    throw new ArgumentException("ServiceId cannot be empty");

                _services[service.ServiceId] = service;
            }
        }

        /// <summary>
        /// Gets service information by ID.
        /// </summary>
        public ServiceInfo? GetService(string serviceId)
        {
            lock (_lock)
            {
                return _services.TryGetValue(serviceId, out var service) ? service : null;
            }
        }

        /// <summary>
        /// Gets all managed services.
        /// </summary>
        public List<ServiceInfo> GetAllServices()
        {
            lock (_lock)
            {
                return _services.Values.ToList();
            }
        }

        /// <summary>
        /// Starts a service and its dependencies using topological sort.
        /// </summary>
        public async Task<bool> StartServiceAsync(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
                throw new ArgumentException($"Service {serviceId} not found");

            try
            {
                var dependenciesToStart = ResolveDependencies(serviceId);

                foreach (var depId in dependenciesToStart)
                {
                    if (!await StartSingleServiceAsync(depId))
                    {
                        OnServiceError(serviceId, $"Failed to start dependency: {depId}");
                        return false;
                    }
                }

                return await StartSingleServiceAsync(serviceId);
            }
            catch (Exception ex)
            {
                OnServiceError(serviceId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops a service and dependent services.
        /// </summary>
        public async Task<bool> StopServiceAsync(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
                throw new ArgumentException($"Service {serviceId} not found");

            try
            {
                var dependentsToStop = ResolveReverseDependencies(serviceId);

                foreach (var depId in dependentsToStop)
                {
                    if (!await StopSingleServiceAsync(depId))
                    {
                        OnServiceError(serviceId, $"Failed to stop dependent: {depId}");
                        return false;
                    }
                }

                return await StopSingleServiceAsync(serviceId);
            }
            catch (Exception ex)
            {
                OnServiceError(serviceId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Restarts a service.
        /// </summary>
        public async Task<bool> RestartServiceAsync(string serviceId)
        {
            if (!await StopServiceAsync(serviceId))
                return false;

            await Task.Delay(1000);
            return await StartServiceAsync(serviceId);
        }

        /// <summary>
        /// Pauses a service (if supported).
        /// </summary>
        public async Task<bool> PauseServiceAsync(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
                throw new ArgumentException($"Service {serviceId} not found");

            try
            {
                if (service.ServiceType != ManagedServiceType.WindowsService)
                    throw new InvalidOperationException("Only Windows services can be paused");

                using (var sc = new ServiceController(service.ServiceId))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                        return false;

                    sc.Pause();
                    sc.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.FromSeconds(30));

                    lock (_lock)
                    {
                        service.Status = ServiceStatus.Paused;
                        service.RunningState = ServiceRunningState.Paused;
                        service.UpdatedAt = DateTime.UtcNow;
                    }

                    OnServiceStatusChanged(serviceId, ServiceStatus.Paused);
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnServiceError(serviceId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Resumes a paused service.
        /// </summary>
        public async Task<bool> ResumeServiceAsync(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
                throw new ArgumentException($"Service {serviceId} not found");

            try
            {
                if (service.ServiceType != ManagedServiceType.WindowsService)
                    throw new InvalidOperationException("Only Windows services can be resumed");

                using (var sc = new ServiceController(service.ServiceId))
                {
                    if (sc.Status != ServiceControllerStatus.Paused)
                        return false;

                    sc.Continue();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                    lock (_lock)
                    {
                        service.Status = ServiceStatus.Running;
                        service.RunningState = ServiceRunningState.Running;
                        service.UpdatedAt = DateTime.UtcNow;
                    }

                    OnServiceStatusChanged(serviceId, ServiceStatus.Running);
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnServiceError(serviceId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Refreshes service status from system.
        /// </summary>
        public void RefreshServiceStatus(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
                return;

            try
            {
                if (service.ServiceType == ManagedServiceType.WindowsService)
                {
                    using (var sc = new ServiceController(service.ServiceId))
                    {
                        lock (_lock)
                        {
                            service.Status = (ServiceStatus)(int)sc.Status;
                            service.RunningState = (ServiceRunningState)(int)sc.Status;
                            service.UpdatedAt = DateTime.UtcNow;

                            if (sc.Status == ServiceControllerStatus.Running && service.ProcessId == null)
                            {
                                service.StartTime = DateTime.UtcNow;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnServiceError(serviceId, $"Failed to refresh status: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all services in a cluster.
        /// </summary>
        public List<ServiceInfo> GetClusterMembers(string clusterId)
        {
            lock (_lock)
            {
                return _services.Values
                    .Where(s => s.ClusterMembers.Contains(clusterId) || s.ServiceId == clusterId)
                    .ToList();
            }
        }

        /// <summary>
        /// Resolves service dependencies using topological sort.
        /// </summary>
        private List<string> ResolveDependencies(string serviceId, HashSet<string>? visited = null)
        {
            visited ??= new HashSet<string>();
            var result = new List<string>();

            if (!visited.Add(serviceId))
                return result;

            var service = GetService(serviceId);
            if (service == null)
                return result;

            foreach (var depId in service.Dependencies)
            {
                result.AddRange(ResolveDependencies(depId, visited));
            }

            result.Add(serviceId);
            return result;
        }

        /// <summary>
        /// Resolves reverse dependencies (dependents).
        /// </summary>
        private List<string> ResolveReverseDependencies(string serviceId, HashSet<string>? visited = null)
        {
            visited ??= new HashSet<string>();
            var result = new List<string>();

            if (!visited.Add(serviceId))
                return result;

            var service = GetService(serviceId);
            if (service == null)
                return result;

            foreach (var depId in service.Dependents)
            {
                result.AddRange(ResolveReverseDependencies(depId, visited));
            }

            result.Add(serviceId);
            return result;
        }

        /// <summary>
        /// Starts a single service without resolving dependencies.
        /// </summary>
        private async Task<bool> StartSingleServiceAsync(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
                return false;

            try
            {
                if (service.Status == ServiceStatus.Running)
                    return true;

                if (service.ServiceType == ManagedServiceType.WindowsService)
                {
                    using (var sc = new ServiceController(service.ServiceId))
                    {
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            lock (_lock)
                            {
                                service.Status = ServiceStatus.Running;
                                service.RunningState = ServiceRunningState.Running;
                            }
                            return true;
                        }

                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                        lock (_lock)
                        {
                            service.Status = ServiceStatus.Running;
                            service.RunningState = ServiceRunningState.Running;
                            service.StartTime = DateTime.UtcNow;
                            service.CurrentRestartAttempts = 0;
                            service.UpdatedAt = DateTime.UtcNow;
                        }

                        OnServiceStatusChanged(serviceId, ServiceStatus.Running);
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnServiceError(serviceId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops a single service without resolving dependents.
        /// </summary>
        private async Task<bool> StopSingleServiceAsync(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
                return false;

            try
            {
                if (service.Status == ServiceStatus.Stopped)
                    return true;

                if (service.ServiceType == ManagedServiceType.WindowsService)
                {
                    using (var sc = new ServiceController(service.ServiceId))
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            lock (_lock)
                            {
                                service.Status = ServiceStatus.Stopped;
                                service.RunningState = ServiceRunningState.Stopped;
                            }
                            return true;
                        }

                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

                        lock (_lock)
                        {
                            service.Status = ServiceStatus.Stopped;
                            service.RunningState = ServiceRunningState.Stopped;
                            service.UpdatedAt = DateTime.UtcNow;
                        }

                        OnServiceStatusChanged(serviceId, ServiceStatus.Stopped);
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnServiceError(serviceId, ex.Message);
                return false;
            }
        }

        private void OnServiceStatusChanged(string serviceId, ServiceStatus newStatus)
        {
            ServiceStatusChanged?.Invoke(this, new ServiceStatusChangedEventArgs { ServiceId = serviceId, NewStatus = newStatus });
        }

        private void OnServiceError(string serviceId, string errorMessage)
        {
            ServiceError?.Invoke(this, new ServiceErrorEventArgs { ServiceId = serviceId, ErrorMessage = errorMessage });
        }
    }

    /// <summary>
    /// Event arguments for service status changes.
    /// </summary>
    public class ServiceStatusChangedEventArgs : EventArgs
    {
        public string ServiceId { get; set; } = string.Empty;
        public ServiceStatus NewStatus { get; set; }
    }

    /// <summary>
    /// Event arguments for service errors.
    /// </summary>
    public class ServiceErrorEventArgs : EventArgs
    {
        public string ServiceId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
