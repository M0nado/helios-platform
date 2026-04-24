using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Administration;

/// <summary>
/// Resilience service with retry logic and circuit breaker patterns
/// Ensures robust operation in face of transient failures
/// </summary>
public interface IResilienceService
{
    /// <summary>Execute operation with automatic retry and exponential backoff</summary>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3);
    
    /// <summary>Execute operation with circuit breaker pattern</summary>
    Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string circuit, int failureThreshold = 5);
    
    /// <summary>Execute operation with timeout</summary>
    Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout);
    
    /// <summary>Get circuit breaker status</summary>
    CircuitBreakerStatus GetCircuitBreakerStatus(string circuitName);
    
    /// <summary>Reset circuit breaker</summary>
    void ResetCircuitBreaker(string circuitName);
}

public class ResilienceService : IResilienceService
{
    private readonly Dictionary<string, CircuitBreakerState> _circuits = new();
    private readonly Dictionary<string, int> _failureCount = new();

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        int attempt = 0;
        Exception lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt < maxRetries)
                {
                    var delayMs = (int)Math.Pow(2, attempt - 1) * 1000; // Exponential backoff
                    await Task.Delay(delayMs);
                }
            }
        }

        throw new InvalidOperationException(
            $"Operation '{operationName}' failed after {maxRetries} retries", lastException);
    }

    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string circuit, int failureThreshold = 5)
    {
        if (!_circuits.ContainsKey(circuit))
        {
            _circuits[circuit] = CircuitBreakerState.Closed;
            _failureCount[circuit] = 0;
        }

        var state = _circuits[circuit];

        if (state == CircuitBreakerState.Open)
        {
            throw new InvalidOperationException($"Circuit breaker '{circuit}' is OPEN");
        }

        if (state == CircuitBreakerState.HalfOpen)
        {
            try
            {
                var result = await operation();
                _circuits[circuit] = CircuitBreakerState.Closed;
                _failureCount[circuit] = 0;
                return result;
            }
            catch
            {
                _circuits[circuit] = CircuitBreakerState.Open;
                throw;
            }
        }

        // Closed state
        try
        {
            var result = await operation();
            _failureCount[circuit] = 0;
            return result;
        }
        catch
        {
            _failureCount[circuit]++;

            if (_failureCount[circuit] >= failureThreshold)
            {
                _circuits[circuit] = CircuitBreakerState.HalfOpen;
            }

            throw;
        }
    }

    public async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout)
    {
        using (var cts = new CancellationTokenSource(timeout))
        {
            try
            {
                return await operation();
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }
    }

    public CircuitBreakerStatus GetCircuitBreakerStatus(string circuitName)
    {
        if (!_circuits.TryGetValue(circuitName, out var state))
            return null;

        return new CircuitBreakerStatus
        {
            CircuitName = circuitName,
            State = state.ToString(),
            FailureCount = _failureCount.TryGetValue(circuitName, out var count) ? count : 0,
            Timestamp = DateTime.UtcNow
        };
    }

    public void ResetCircuitBreaker(string circuitName)
    {
        if (_circuits.ContainsKey(circuitName))
        {
            _circuits[circuitName] = CircuitBreakerState.Closed;
            _failureCount[circuitName] = 0;
        }
    }
}

public enum CircuitBreakerState
{
    Closed,    // Normal operation
    Open,      // Failing, reject requests
    HalfOpen   // Testing if recovered
}

public class CircuitBreakerStatus
{
    public string CircuitName { get; set; }
    public string State { get; set; }
    public int FailureCount { get; set; }
    public DateTime Timestamp { get; set; }
}
