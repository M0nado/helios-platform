using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.CloudIntegration.Modules
{
    /// <summary>
    /// Central cloud integration service for HELIOS Platform
    /// </summary>
    public interface ICloudIntegrationService
    {
        Task InitializeAsync();
        Task<T> InvokeServiceAsync<T>(string service, Func<IServiceClient, Task<T>> operation);
        Task<ServiceStatus> GetServiceStatusAsync(string service);
        Task<CloudIntegrationReport> GenerateIntegrationReportAsync();
    }

    /// <summary>
    /// Service client interface
    /// </summary>
    public interface IServiceClient
    {
        string ServiceName { get; }
        Task<T> ExecuteAsync<T>(string endpoint, HttpMethod method, object data = null);
    }

    /// <summary>
    /// Service status
    /// </summary>
    public class ServiceStatus
    {
        public string Service { get; set; }
        public bool Available { get; set; }
        public DateTime LastChecked { get; set; }
        public double ResponseTimeMs { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Integration report
    /// </summary>
    public class CloudIntegrationReport
    {
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, ServiceStatus> ServiceStatuses { get; set; } = new();
        public int AvailableServices { get; set; }
        public int TotalServices { get; set; }
        public decimal HealthPercentage { get; set; }
        public List<ServiceAlert> Alerts { get; set; } = new();
    }

    /// <summary>
    /// Service alert
    /// </summary>
    public class ServiceAlert
    {
        public string Service { get; set; }
        public AlertLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime AlertedAt { get; set; }
    }

    /// <summary>
    /// Cloud integration service implementation
    /// </summary>
    public class CloudIntegrationService : ICloudIntegrationService
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly IFallbackChain _fallbackChain;
        private readonly ICostAnalyzer _costAnalyzer;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly Dictionary<string, IServiceClient> _serviceClients;

        public CloudIntegrationService(
            IServiceRegistry serviceRegistry,
            IFallbackChain fallbackChain,
            ICostAnalyzer costAnalyzer,
            HttpClient httpClient,
            ILogger logger)
        {
            _serviceRegistry = serviceRegistry;
            _fallbackChain = fallbackChain;
            _costAnalyzer = costAnalyzer;
            _httpClient = httpClient;
            _logger = logger;
            _serviceClients = new();
        }

        public async Task InitializeAsync()
        {
            _logger.Info("Initializing cloud integration services");

            try
            {
                // Initialize all registered services
                var services = _serviceRegistry.GetAllServices();

                foreach (var service in services)
                {
                    try
                    {
                        _logger.Info($"Initializing {service}");
                        var client = _serviceRegistry.GetServiceClient(service);
                        _serviceClients[service] = client;

                        // Register fallback chain
                        var fallbackChain = _serviceRegistry.GetFallbackChain(service);
                        if (fallbackChain != null)
                        {
                            await _fallbackChain.RegisterFallbackAsync(service, fallbackChain);
                        }

                        _logger.Info($"Successfully initialized {service}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to initialize {service}: {ex.Message}");
                    }
                }

                _logger.Info("Cloud integration initialization complete");
            }
            catch (Exception ex)
            {
                _logger.Error($"Cloud integration initialization failed: {ex.Message}");
                throw;
            }
        }

        public async Task<T> InvokeServiceAsync<T>(string service, Func<IServiceClient, Task<T>> operation)
        {
            return await _fallbackChain.ExecuteAsync(
                async () =>
                {
                    if (!_serviceClients.TryGetValue(service, out var client))
                    {
                        client = _serviceRegistry.GetServiceClient(service);
                        _serviceClients[service] = client;
                    }

                    return await operation(client);
                },
                service
            );
        }

        public async Task<ServiceStatus> GetServiceStatusAsync(string service)
        {
            var startTime = DateTime.UtcNow;
            var status = new ServiceStatus { Service = service };

            try
            {
                if (!_serviceClients.TryGetValue(service, out var client))
                {
                    client = _serviceRegistry.GetServiceClient(service);
                    _serviceClients[service] = client;
                }

                // Perform health check
                await client.ExecuteAsync<object>("/health", HttpMethod.Get);

                status.Available = true;
                status.Message = "Service is healthy";
                _logger.Info($"{service} is healthy");
            }
            catch (Exception ex)
            {
                status.Available = false;
                status.Message = $"Service error: {ex.Message}";
                _logger.Warn($"{service} health check failed: {ex.Message}");
            }

            status.LastChecked = DateTime.UtcNow;
            status.ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return status;
        }

        public async Task<CloudIntegrationReport> GenerateIntegrationReportAsync()
        {
            var report = new CloudIntegrationReport { GeneratedAt = DateTime.UtcNow };

            try
            {
                var services = _serviceRegistry.GetAllServices();

                foreach (var service in services)
                {
                    try
                    {
                        var status = await GetServiceStatusAsync(service);
                        report.ServiceStatuses[service] = status;

                        if (status.Available)
                        {
                            report.AvailableServices++;
                        }
                        else
                        {
                            report.Alerts.Add(new ServiceAlert
                            {
                                Service = service,
                                Level = AlertLevel.Error,
                                Message = status.Message,
                                AlertedAt = DateTime.UtcNow
                            });
                        }

                        report.TotalServices++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error checking {service}: {ex.Message}");
                        report.Alerts.Add(new ServiceAlert
                        {
                            Service = service,
                            Level = AlertLevel.Error,
                            Message = $"Failed to check status: {ex.Message}",
                            AlertedAt = DateTime.UtcNow
                        });
                        report.TotalServices++;
                    }
                }

                report.HealthPercentage = report.TotalServices > 0
                    ? (report.AvailableServices / (double)report.TotalServices) * 100
                    : 0;

                _logger.Info($"Integration report generated: {report.HealthPercentage:F2}% healthy");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generating integration report: {ex.Message}");
            }

            return report;
        }
    }

    /// <summary>
    /// Service registry
    /// </summary>
    public interface IServiceRegistry
    {
        IServiceClient GetServiceClient(string service);
        string[] GetAllServices();
        string[] GetFallbackChain(string service);
    }

    /// <summary>
    /// Alert level enum
    /// </summary>
    public enum AlertLevel { Info, Warning, Error, Critical }

    // Interface definitions for dependencies
    public interface IFallbackChain
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string serviceName = null);
        Task<bool> RegisterFallbackAsync(string service, string[] fallbackChain);
    }

    public interface ICostAnalyzer
    {
        // Cost analyzer interface
    }

    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
