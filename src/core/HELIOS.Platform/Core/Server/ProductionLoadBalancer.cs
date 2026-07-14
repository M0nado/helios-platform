using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Production load balancer implementation with round-robin, weighted distribution, and connection pooling.
    /// </summary>
    public class ProductionLoadBalancer : IProductionLoadBalancer
    {
        private readonly Dictionary<string, ServiceEndpoint> _services = new();
        private readonly Dictionary<string, ServiceHealthStatus> _health = new();
        private readonly Dictionary<string, List<PooledConnection>> _connectionPools = new();
        private readonly object _lock = new();
        private int _currentRoundRobinIndex;
        private long _totalRequests;
        private long _totalDistributionTimeMs;
        private readonly int _maxConnectionsPerService;

        public ProductionLoadBalancer(int maxConnectionsPerService = 100)
        {
            _maxConnectionsPerService = maxConnectionsPerService;
        }

        public Task<bool> RegisterServiceAsync(string serviceId, string endpoint, int weight = 1)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID cannot be empty", nameof(serviceId));
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));
            if (weight < 1)
                throw new ArgumentException("Weight must be at least 1", nameof(weight));

            lock (_lock)
            {
                _services[serviceId] = new ServiceEndpoint
                {
                    ServiceId = serviceId,
                    Endpoint = endpoint,
                    Weight = weight,
                    HealthStatus = new ServiceHealthStatus
                    {
                        ServiceId = serviceId,
                        IsHealthy = true,
                        LastCheckedAt = DateTime.UtcNow
                    },
                    RegisteredAt = DateTime.UtcNow,
                    RequestsHandled = 0,
                    AverageResponseTimeMs = 0,
                    ConnectionPoolSize = 0
                };

                _health[serviceId] = new ServiceHealthStatus
                {
                    ServiceId = serviceId,
                    IsHealthy = true,
                    LastCheckedAt = DateTime.UtcNow,
                    ResponseTimeMs = 0,
                    ConsecutiveFailures = 0
                };

                _connectionPools[serviceId] = new List<PooledConnection>();

                return Task.FromResult(true);
            }
        }

        public Task<bool> DeregisterServiceAsync(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID cannot be empty", nameof(serviceId));

            lock (_lock)
            {
                _services.Remove(serviceId);
                _health.Remove(serviceId);
                _connectionPools.Remove(serviceId);
                return Task.FromResult(true);
            }
        }

        public Task<LoadBalancedEndpoint?> GetNextServiceAsync(string? preferredStrategy = null)
        {
            lock (_lock)
            {
                var activeServices = _services.Values.Where(s => _health[s.ServiceId].IsHealthy).ToList();
                if (activeServices.Count == 0)
                    return Task.FromResult<LoadBalancedEndpoint?>(null);

                if (preferredStrategy?.ToLower() == "weighted")
                    return GetNextWeightedServiceAsync();

                // Round-robin
                var service = activeServices[_currentRoundRobinIndex % activeServices.Count];
                _currentRoundRobinIndex++;

                _totalRequests++;
                return Task.FromResult<LoadBalancedEndpoint?>(new LoadBalancedEndpoint
                {
                    ServiceId = service.ServiceId,
                    Endpoint = service.Endpoint,
                    Weight = service.Weight,
                    HealthStatus = _health[service.ServiceId]
                });
            }
        }

        public Task<LoadBalancedEndpoint?> GetNextWeightedServiceAsync()
        {
            lock (_lock)
            {
                var activeServices = _services.Values.Where(s => _health[s.ServiceId].IsHealthy).ToList();
                if (activeServices.Count == 0)
                    return Task.FromResult<LoadBalancedEndpoint?>(null);

                // Weighted round-robin: Select service proportional to weight
                int totalWeight = activeServices.Sum(s => s.Weight);
                int randomValue = (int)(new Random().Next(totalWeight));
                int cumulativeWeight = 0;

                ServiceEndpoint? selectedService = null;
                foreach (var service in activeServices)
                {
                    cumulativeWeight += service.Weight;
                    if (randomValue < cumulativeWeight)
                    {
                        selectedService = service;
                        break;
                    }
                }

                if (selectedService == null)
                    selectedService = activeServices.Last();

                _totalRequests++;
                return Task.FromResult<LoadBalancedEndpoint?>(new LoadBalancedEndpoint
                {
                    ServiceId = selectedService.ServiceId,
                    Endpoint = selectedService.Endpoint,
                    Weight = selectedService.Weight,
                    HealthStatus = _health[selectedService.ServiceId]
                });
            }
        }

        public Task<bool> ReportHealthAsync(string serviceId, ServiceHealthStatus healthStatus)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID cannot be empty", nameof(serviceId));
            if (healthStatus == null)
                throw new ArgumentNullException(nameof(healthStatus));

            lock (_lock)
            {
                if (!_health.ContainsKey(serviceId))
                    return Task.FromResult(false);

                var currentHealth = _health[serviceId];
                if (healthStatus.IsHealthy)
                {
                    currentHealth.ConsecutiveFailures = 0;
                }
                else
                {
                    currentHealth.ConsecutiveFailures++;
                }

                currentHealth.IsHealthy = healthStatus.IsHealthy;
                currentHealth.LastCheckedAt = DateTime.UtcNow;
                currentHealth.ResponseTimeMs = healthStatus.ResponseTimeMs;
                currentHealth.ErrorMessage = healthStatus.ErrorMessage;

                // Update service health reference
                if (_services.TryGetValue(serviceId, out var service))
                {
                    service.HealthStatus = currentHealth;
                    service.AverageResponseTimeMs = currentHealth.ResponseTimeMs;
                }

                return Task.FromResult(true);
            }
        }

        public Task<ServiceHealthStatus?> GetServiceHealthAsync(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID cannot be empty", nameof(serviceId));

            lock (_lock)
            {
                _health.TryGetValue(serviceId, out var health);
                return Task.FromResult(health);
            }
        }

        public Task<List<ServiceEndpoint>> GetAllServicesAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_services.Values.ToList());
            }
        }

        public Task<List<ServiceEndpoint>> GetActiveServicesAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_services.Values.Where(s => _health[s.ServiceId].IsHealthy).ToList());
            }
        }

        public Task<bool> UpdateServiceWeightAsync(string serviceId, int newWeight)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID cannot be empty", nameof(serviceId));
            if (newWeight < 1)
                throw new ArgumentException("Weight must be at least 1", nameof(newWeight));

            lock (_lock)
            {
                if (_services.TryGetValue(serviceId, out var service))
                {
                    service.Weight = newWeight;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<PooledConnection?> AcquireConnectionAsync(string serviceId, int timeoutMs = 5000)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID cannot be empty", nameof(serviceId));

            lock (_lock)
            {
                if (!_connectionPools.ContainsKey(serviceId))
                    return Task.FromResult<PooledConnection?>(null);

                var pool = _connectionPools[serviceId];
                var availableConnection = pool.FirstOrDefault(c => !c.IsActive);

                PooledConnection connection;
                if (availableConnection != null)
                {
                    connection = availableConnection;
                    connection.IsActive = true;
                }
                else if (pool.Count < _maxConnectionsPerService)
                {
                    connection = new PooledConnection
                    {
                        ConnectionId = Guid.NewGuid().ToString(),
                        ServiceId = serviceId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastUsedAt = DateTime.UtcNow
                    };
                    pool.Add(connection);
                }
                else
                {
                    return Task.FromResult<PooledConnection?>(null);
                }

                connection.LastUsedAt = DateTime.UtcNow;
                if (_services.TryGetValue(serviceId, out var service))
                {
                    service.ConnectionPoolSize = pool.Count;
                }

                return Task.FromResult<PooledConnection?>(connection);
            }
        }

        public Task<bool> ReleaseConnectionAsync(PooledConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            lock (_lock)
            {
                if (_connectionPools.TryGetValue(connection.ServiceId, out var pool))
                {
                    var pooledConn = pool.FirstOrDefault(c => c.ConnectionId == connection.ConnectionId);
                    if (pooledConn != null)
                    {
                        pooledConn.IsActive = false;
                        pooledConn.LastUsedAt = DateTime.UtcNow;
                        return Task.FromResult(true);
                    }
                }

                return Task.FromResult(false);
            }
        }

        public Task<LoadBalancerStatistics> GetStatisticsAsync()
        {
            lock (_lock)
            {
                var healthyCount = _health.Count(h => h.Value.IsHealthy);
                var unhealthyCount = _health.Count - healthyCount;
                var totalConnections = _connectionPools.Values.SelectMany(p => p).Count();

                var stats = new LoadBalancerStatistics
                {
                    TotalServices = _services.Count,
                    HealthyServices = healthyCount,
                    UnhealthyServices = unhealthyCount,
                    TotalRequestsDistributed = _totalRequests,
                    AverageDistributionTimeMs = _totalRequests > 0 ? (double)_totalDistributionTimeMs / _totalRequests : 0,
                    ActiveConnections = _connectionPools.Values.SelectMany(p => p.Where(c => c.IsActive)).Count(),
                    PooledConnections = totalConnections,
                    CollectedAt = DateTime.UtcNow
                };

                return Task.FromResult(stats);
            }
        }
    }

    /// <summary>
    /// Health status constants.
    /// </summary>
    public static class ServiceHealthStatusConstants
    {
        public const string Healthy = "Healthy";
        public const string Unhealthy = "Unhealthy";
    }
}
