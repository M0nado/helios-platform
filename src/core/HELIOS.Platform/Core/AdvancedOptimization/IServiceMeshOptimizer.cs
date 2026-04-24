using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Service mesh optimizer that optimizes inter-service communication,
    /// routes requests through optimal paths, and implements circuit breaker patterns.
    /// </summary>
    public interface IServiceMeshOptimizer
    {
        Task<bool> InitializeAsync();
        Task<ServiceRoute[]> OptimizeRoutesAsync();
        Task<bool> ApplyRouteAsync(ServiceRoute route);
        Task<CircuitBreakerStatus[]> GetCircuitBreakerStatusAsync();
        Task<bool> UpdateCircuitBreakerAsync(string serviceId, CircuitBreakerConfig config);
        Task<MeshPerformanceMetrics> GetMeshMetricsAsync();
        Task<CacheStats> GetCacheStatsAsync();
    }

    /// <summary>Represents an optimized service route.</summary>
    public class ServiceRoute
    {
        public string RouteId { get; set; } = Guid.NewGuid().ToString();
        public string SourceService { get; set; } = string.Empty;
        public string DestinationService { get; set; } = string.Empty;
        public List<string> PathNodes { get; set; } = new();
        public double LatencyMs { get; set; }
        public double Throughput { get; set; }
        public int Priority { get; set; }
        public double OptimizationScore { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Circuit breaker configuration and status.</summary>
    public class CircuitBreakerStatus
    {
        public string ServiceId { get; set; } = string.Empty;
        public CircuitBreakerState State { get; set; }
        public int FailureCount { get; set; }
        public int SuccessCount { get; set; }
        public int RequestCount { get; set; }
        public double FailureRate { get; set; }
        public DateTime LastFailureTime { get; set; }
        public DateTime StateChangedAt { get; set; }
        public double HealthScore { get; set; }
    }

    /// <summary>Circuit breaker states.</summary>
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen,
        Testing
    }

    /// <summary>Circuit breaker configuration.</summary>
    public class CircuitBreakerConfig
    {
        public string ServiceId { get; set; } = string.Empty;
        public int FailureThreshold { get; set; } = 5;
        public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public int HalfOpenMaxRequests { get; set; } = 3;
        public double FailureRateThreshold { get; set; } = 0.5;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>Service mesh performance metrics.</summary>
    public class MeshPerformanceMetrics
    {
        public int TotalServices { get; set; }
        public int OptimizedRoutes { get; set; }
        public int CircuitBreakersActive { get; set; }
        public double AverageLatencyMs { get; set; }
        public double AverageThroughput { get; set; }
        public double RequestSuccessRate { get; set; }
        public long TotalRequestsProcessed { get; set; }
        public int CircuitBreakersTripped { get; set; }
        public double CacheHitRate { get; set; }
        public DateTime LastMetricsUpdate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Service response cache statistics.</summary>
    public class CacheStats
    {
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double HitRate { get; set; }
        public long CachedItems { get; set; }
        public long CacheSize { get; set; }
        public long MaxCacheSize { get; set; }
        public double EvictionRate { get; set; }
        public DateTime LastClearedAt { get; set; }
        public List<CacheEntry> TopCachedResponses { get; set; } = new();
    }

    /// <summary>Individual cache entry information.</summary>
    public class CacheEntry
    {
        public string Key { get; set; } = string.Empty;
        public int Size { get; set; }
        public int AccessCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public TimeSpan TimeToLive { get; set; }
    }
}
