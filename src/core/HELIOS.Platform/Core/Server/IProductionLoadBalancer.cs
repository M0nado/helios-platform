using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Production load balancer interface for distributing requests across services with health tracking.
    /// </summary>
    public interface IProductionLoadBalancer
    {
        /// <summary>
        /// Registers a service endpoint for load balancing.
        /// </summary>
        Task<bool> RegisterServiceAsync(string serviceId, string endpoint, int weight = 1);

        /// <summary>
        /// Deregisters a service endpoint.
        /// </summary>
        Task<bool> DeregisterServiceAsync(string serviceId);

        /// <summary>
        /// Gets the next available service using round-robin strategy.
        /// </summary>
        Task<LoadBalancedEndpoint?> GetNextServiceAsync(string? preferredStrategy = null);

        /// <summary>
        /// Gets the next service using weighted round-robin strategy.
        /// </summary>
        Task<LoadBalancedEndpoint?> GetNextWeightedServiceAsync();

        /// <summary>
        /// Reports service health status.
        /// </summary>
        Task<bool> ReportHealthAsync(string serviceId, ServiceHealthStatus healthStatus);

        /// <summary>
        /// Gets current service health status.
        /// </summary>
        Task<ServiceHealthStatus?> GetServiceHealthAsync(string serviceId);

        /// <summary>
        /// Gets all registered services.
        /// </summary>
        Task<List<ServiceEndpoint>> GetAllServicesAsync();

        /// <summary>
        /// Gets active services (healthy).
        /// </summary>
        Task<List<ServiceEndpoint>> GetActiveServicesAsync();

        /// <summary>
        /// Updates service weight for weighted round-robin.
        /// </summary>
        Task<bool> UpdateServiceWeightAsync(string serviceId, int newWeight);

        /// <summary>
        /// Acquires a connection from the pool for a service.
        /// </summary>
        Task<PooledConnection?> AcquireConnectionAsync(string serviceId, int timeoutMs = 5000);

        /// <summary>
        /// Releases a connection back to the pool.
        /// </summary>
        Task<bool> ReleaseConnectionAsync(PooledConnection connection);

        /// <summary>
        /// Gets load balancer statistics.
        /// </summary>
        Task<LoadBalancerStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Represents a load-balanced endpoint.
    /// </summary>
    public class LoadBalancedEndpoint
    {
        public string ServiceId { get; set; }
        public string Endpoint { get; set; }
        public int Weight { get; set; }
        public ServiceHealthStatus HealthStatus { get; set; }
    }

    /// <summary>
    /// Service endpoint configuration.
    /// </summary>
    public class ServiceEndpoint
    {
        public string ServiceId { get; set; }
        public string Endpoint { get; set; }
        public int Weight { get; set; }
        public ServiceHealthStatus HealthStatus { get; set; }
        public DateTime RegisteredAt { get; set; }
        public long RequestsHandled { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public int ConnectionPoolSize { get; set; }
    }

    /// <summary>
    /// Service health status.
    /// </summary>
    public class ServiceHealthStatus
    {
        public string ServiceId { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime LastCheckedAt { get; set; }
        public double ResponseTimeMs { get; set; }
        public int ConsecutiveFailures { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Pooled connection for connection pooling.
    /// </summary>
    public class PooledConnection
    {
        public string ConnectionId { get; set; }
        public string ServiceId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsedAt { get; set; }
    }

    /// <summary>
    /// Load balancer statistics.
    /// </summary>
    public class LoadBalancerStatistics
    {
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public int UnhealthyServices { get; set; }
        public long TotalRequestsDistributed { get; set; }
        public double AverageDistributionTimeMs { get; set; }
        public int ActiveConnections { get; set; }
        public int PooledConnections { get; set; }
        public DateTime CollectedAt { get; set; }
    }
}
