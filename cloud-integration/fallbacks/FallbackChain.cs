using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.CloudIntegration.Fallbacks
{
    /// <summary>
    /// Manages fallback chains for service redundancy
    /// </summary>
    public interface IFallbackChain
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> primaryOperation, string serviceName = null);
        Task<bool> RegisterFallbackAsync(string service, string[] fallbackChain);
        Task<ServiceHealth> GetServiceHealthAsync(string service);
        Task<FallbackStatistics> GetFallbackStatisticsAsync();
    }

    /// <summary>
    /// Fallback chain configuration
    /// </summary>
    public class FallbackChainConfig
    {
        public string PrimaryService { get; set; }
        public string[] FallbackServices { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public bool CircuitBreakerEnabled { get; set; } = true;
    }

    /// <summary>
    /// Service health information
    /// </summary>
    public class ServiceHealth
    {
        public string Service { get; set; }
        public HealthStatus Status { get; set; }
        public DateTime LastChecked { get; set; }
        public double ResponseTimeMs { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
        public decimal SuccessRate => (SuccessCount + ErrorCount) > 0 ? (SuccessCount / (double)(SuccessCount + ErrorCount)) * 100 : 100;
        public DateTime LastError { get; set; }
    }

    /// <summary>
    /// Fallback statistics
    /// </summary>
    public class FallbackStatistics
    {
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, ServiceStatistics> ServiceStats { get; set; } = new();
        public List<FalloverEvent> RecentFalloverEvents { get; set; } = new();
        public decimal TotalSuccessRate { get; set; }
        public int TotalFailoverCount { get; set; }
    }

    /// <summary>
    /// Per-service statistics
    /// </summary>
    public class ServiceStatistics
    {
        public string Service { get; set; }
        public long RequestsProcessed { get; set; }
        public long RequestsFailed { get; set; }
        public long FalloverCount { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public DateTime LastUsed { get; set; }
    }

    /// <summary>
    /// Fallover event record
    /// </summary>
    public class FalloverEvent
    {
        public DateTime Timestamp { get; set; }
        public string FromService { get; set; }
        public string ToService { get; set; }
        public string Reason { get; set; }
        public int AttemptNumber { get; set; }
        public bool Successful { get; set; }
    }

    /// <summary>
    /// Circuit breaker for preventing cascade failures
    /// </summary>
    public class CircuitBreaker
    {
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private DateTime _stateChangeTime = DateTime.UtcNow;
        private int _failureCount = 0;
        private int _successCount = 0;

        public int FailureThreshold { get; set; } = 5;
        public int SuccessThreshold { get; set; } = 2;
        public int TimeoutSeconds { get; set; } = 60;

        public CircuitBreakerState CurrentState => _state;

        public bool CanExecute()
        {
            if (_state == CircuitBreakerState.Closed)
                return true;

            if (_state == CircuitBreakerState.Open)
            {
                if ((DateTime.UtcNow - _stateChangeTime).TotalSeconds > TimeoutSeconds)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _successCount = 0;
                    return true;
                }
                return false;
            }

            // HalfOpen
            return true;
        }

        public void RecordSuccess()
        {
            _failureCount = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _successCount++;
                if (_successCount >= SuccessThreshold)
                {
                    _state = CircuitBreakerState.Closed;
                    _stateChangeTime = DateTime.UtcNow;
                }
            }
        }

        public void RecordFailure()
        {
            _failureCount++;

            if (_state == CircuitBreakerState.Closed && _failureCount >= FailureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _stateChangeTime = DateTime.UtcNow;
            }
            else if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Open;
                _stateChangeTime = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Fallback chain implementation
    /// </summary>
    public class FallbackChainManager : IFallbackChain
    {
        private readonly Dictionary<string, FallbackChainConfig> _chains;
        private readonly Dictionary<string, CircuitBreaker> _circuitBreakers;
        private readonly IServiceHealthMonitor _healthMonitor;
        private readonly ILogger _logger;
        private readonly FallbackStatistics _statistics;

        public FallbackChainManager(IServiceHealthMonitor healthMonitor, ILogger logger)
        {
            _chains = new();
            _circuitBreakers = new();
            _healthMonitor = healthMonitor;
            _logger = logger;
            _statistics = new();
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> primaryOperation, string serviceName = null)
        {
            serviceName = serviceName ?? "default";

            if (!_chains.TryGetValue(serviceName, out var config))
            {
                return await primaryOperation();
            }

            var services = new[] { config.PrimaryService }.Concat(config.FallbackServices).ToList();
            Exception lastException = null;

            foreach (var service in services)
            {
                try
                {
                    // Check circuit breaker
                    if (!_circuitBreakers.TryGetValue(service, out var breaker))
                    {
                        breaker = new CircuitBreaker();
                        _circuitBreakers[service] = breaker;
                    }

                    if (!breaker.CanExecute())
                    {
                        _logger.Warn($"Circuit breaker OPEN for {service}, skipping");
                        continue;
                    }

                    // Check service health
                    var health = await _healthMonitor.GetHealthAsync(service);
                    if (health.Status == HealthStatus.Unavailable)
                    {
                        _logger.Warn($"Service {service} unavailable, trying fallback");
                        continue;
                    }

                    // Execute operation
                    _logger.Info($"Attempting operation on {service}");
                    var result = await primaryOperation();

                    // Record success
                    breaker.RecordSuccess();
                    _statistics.ServiceStats.TryGetValue(service, out var stats);
                    if (stats == null)
                    {
                        stats = new ServiceStatistics { Service = service };
                        _statistics.ServiceStats[service] = stats;
                    }
                    stats.RequestsProcessed++;
                    stats.LastUsed = DateTime.UtcNow;

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.Error($"Operation failed on {service}: {ex.Message}");

                    // Record failure
                    _circuitBreakers[service].RecordFailure();
                    _statistics.ServiceStats.TryGetValue(service, out var failStats);
                    if (failStats == null)
                    {
                        failStats = new ServiceStatistics { Service = service };
                        _statistics.ServiceStats[service] = failStats;
                    }
                    failStats.RequestsFailed++;
                    failStats.FalloverCount++;

                    // Record fallover event
                    var nextService = services.FirstOrDefault(s => s != service);
                    if (nextService != null)
                    {
                        _statistics.RecentFalloverEvents.Add(new FalloverEvent
                        {
                            Timestamp = DateTime.UtcNow,
                            FromService = service,
                            ToService = nextService,
                            Reason = ex.Message,
                            AttemptNumber = services.IndexOf(service) + 1,
                            Successful = false
                        });
                    }

                    // Continue to next fallback
                    if (services.IndexOf(service) < services.Count - 1)
                    {
                        continue;
                    }
                }
            }

            _logger.Error($"All services failed for {serviceName}");
            throw new AggregateException($"All fallback services failed for {serviceName}", lastException);
        }

        public Task<bool> RegisterFallbackAsync(string service, string[] fallbackChain)
        {
            _chains[service] = new FallbackChainConfig
            {
                PrimaryService = service,
                FallbackServices = fallbackChain
            };

            _logger.Info($"Registered fallback chain: {service} -> {string.Join(" -> ", fallbackChain)}");
            return Task.FromResult(true);
        }

        public async Task<ServiceHealth> GetServiceHealthAsync(string service)
        {
            return await _healthMonitor.GetHealthAsync(service);
        }

        public Task<FallbackStatistics> GetFallbackStatisticsAsync()
        {
            _statistics.GeneratedAt = DateTime.UtcNow;
            _statistics.TotalSuccessRate = _statistics.ServiceStats.Values.Average(s => s.RequestsProcessed > 0 ? (s.RequestsProcessed - s.RequestsFailed) / (double)s.RequestsProcessed * 100 : 100);
            _statistics.TotalFailoverCount = _statistics.ServiceStats.Values.Sum(s => s.FalloverCount);

            return Task.FromResult(_statistics);
        }
    }

    /// <summary>
    /// Service health monitor
    /// </summary>
    public interface IServiceHealthMonitor
    {
        Task<ServiceHealth> GetHealthAsync(string service);
        Task StartMonitoringAsync(string service);
        Task StopMonitoringAsync(string service);
    }

    // Enums
    public enum HealthStatus { Healthy, Degraded, Unavailable, Unknown }
    public enum CircuitBreakerState { Closed, Open, HalfOpen }

    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
