using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.BackendServices.ApiGateway
{
    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public class RateLimitConfig
    {
        public int RequestsPerMinute { get; set; } = 100;
        public int BurstSize { get; set; } = 150;
        public int TimeWindowSeconds { get; set; } = 60;
    }

    public class RateLimitKey
    {
        public string ClientId { get; set; }
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; }
    }

    /// <summary>
    /// Rate limiting service
    /// Prevents abuse and ensures fair resource usage
    /// </summary>
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string clientId);
        Task ResetLimitAsync(string clientId);
        Task<RateLimitKey> GetLimitStatusAsync(string clientId);
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly RateLimitConfig _config;
        private readonly ILogger<RateLimitService> _logger;
        private readonly Dictionary<string, RateLimitKey> _limits = new();

        public RateLimitService(RateLimitConfig config, ILogger<RateLimitService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> IsAllowedAsync(string clientId)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                    return false;

                lock (_limits)
                {
                    var now = DateTime.UtcNow;
                    
                    if (!_limits.TryGetValue(clientId, out var limiter))
                    {
                        limiter = new RateLimitKey
                        {
                            ClientId = clientId,
                            WindowStart = now,
                            RequestCount = 1
                        };
                        _limits[clientId] = limiter;
                        return true;
                    }

                    // Check if window has expired
                    if ((now - limiter.WindowStart).TotalSeconds > _config.TimeWindowSeconds)
                    {
                        limiter.WindowStart = now;
                        limiter.RequestCount = 1;
                        return true;
                    }

                    // Check if limit exceeded
                    if (limiter.RequestCount >= _config.RequestsPerMinute)
                    {
                        _logger.LogWarning($"Rate limit exceeded for {clientId}");
                        return false;
                    }

                    limiter.RequestCount++;
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit");
                return false;
            }
        }

        public async Task ResetLimitAsync(string clientId)
        {
            try
            {
                lock (_limits)
                {
                    if (_limits.Remove(clientId))
                    {
                        _logger.LogInformation($"Rate limit reset for {clientId}");
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting rate limit");
            }
        }

        public async Task<RateLimitKey> GetLimitStatusAsync(string clientId)
        {
            try
            {
                if (_limits.TryGetValue(clientId, out var limiter))
                {
                    return await Task.FromResult(limiter);
                }
                return await Task.FromResult<RateLimitKey>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rate limit status");
                return null;
            }
        }
    }

    /// <summary>
    /// Circuit breaker for upstream service protection
    /// Prevents cascading failures
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,      // Normal operation
        Open,        // Blocked, failing
        HalfOpen     // Testing recovery
    }

    public interface ICircuitBreaker
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName);
        CircuitBreakerState GetState();
        Task ResetAsync();
    }

    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly ILogger<CircuitBreaker> _logger;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _failureCount = 0;
        private DateTime _lastFailureTime = DateTime.UtcNow;
        
        private const int FailureThreshold = 5;
        private const int TimeoutSeconds = 30;
        private const int SuccessThreshold = 2;

        public CircuitBreaker(ILogger<CircuitBreaker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
        {
            try
            {
                if (_state == CircuitBreakerState.Open)
                {
                    // Check if timeout has elapsed
                    if ((DateTime.UtcNow - _lastFailureTime).TotalSeconds > TimeoutSeconds)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _logger.LogInformation($"Circuit breaker transitioning to HalfOpen for {operationName}");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Circuit breaker is Open for {operationName}");
                    }
                }

                var result = await operation();
                
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _failureCount = 0;
                    _state = CircuitBreakerState.Closed;
                    _logger.LogInformation($"Circuit breaker closed for {operationName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_failureCount >= FailureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                    _logger.LogError(ex, $"Circuit breaker opened for {operationName} after {_failureCount} failures");
                }

                throw;
            }
        }

        public CircuitBreakerState GetState()
        {
            return _state;
        }

        public async Task ResetAsync()
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _logger.LogInformation("Circuit breaker reset");
            await Task.CompletedTask;
        }
    }
}
