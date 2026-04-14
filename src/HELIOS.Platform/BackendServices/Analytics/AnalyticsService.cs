using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.BackendServices.Analytics
{
    /// <summary>
    /// Performance metrics tracked by analytics service
    /// </summary>
    public class PerformanceMetrics
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string MetricName { get; set; }
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class RequestMetrics
    {
        public Guid RequestId { get; set; }
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public int StatusCode { get; set; }
        public long LatencyMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Analytics and monitoring service
    /// Collects and aggregates performance metrics
    /// </summary>
    public interface IAnalyticsService
    {
        Task RecordRequestAsync(RequestMetrics metrics);
        Task RecordMetricAsync(PerformanceMetrics metric);
        Task<IEnumerable<RequestMetrics>> GetRequestMetricsAsync(TimeSpan window);
        Task<Dictionary<string, double>> GetAggregatedMetricsAsync(TimeSpan window);
        Task<(double P50, double P95, double P99)> GetLatencyPercentilesAsync(TimeSpan window);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ILogger<AnalyticsService> _logger;
        private readonly List<RequestMetrics> _requestMetrics = new();
        private readonly List<PerformanceMetrics> _performanceMetrics = new();
        private const int MaxMetricsRetention = 10000;

        public AnalyticsService(ILogger<AnalyticsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RecordRequestAsync(RequestMetrics metrics)
        {
            try
            {
                if (metrics == null)
                    throw new ArgumentNullException(nameof(metrics));

                _requestMetrics.Add(metrics);

                // Maintain size limit
                if (_requestMetrics.Count > MaxMetricsRetention)
                {
                    _requestMetrics.RemoveRange(0, _requestMetrics.Count - MaxMetricsRetention);
                }

                _logger.LogDebug($"Recorded request: {metrics.Endpoint} {metrics.LatencyMs}ms");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording request metrics");
            }
        }

        public async Task RecordMetricAsync(PerformanceMetrics metric)
        {
            try
            {
                if (metric == null)
                    throw new ArgumentNullException(nameof(metric));

                _performanceMetrics.Add(metric);

                // Maintain size limit
                if (_performanceMetrics.Count > MaxMetricsRetention)
                {
                    _performanceMetrics.RemoveRange(0, _performanceMetrics.Count - MaxMetricsRetention);
                }

                _logger.LogDebug($"Recorded metric: {metric.MetricName} = {metric.Value}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording performance metrics");
            }
        }

        public async Task<IEnumerable<RequestMetrics>> GetRequestMetricsAsync(TimeSpan window)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.Subtract(window);
                var recentMetrics = _requestMetrics
                    .Where(m => m.Timestamp > cutoffTime)
                    .ToList();

                return await Task.FromResult<IEnumerable<RequestMetrics>>(recentMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving request metrics");
                return Enumerable.Empty<RequestMetrics>();
            }
        }

        public async Task<Dictionary<string, double>> GetAggregatedMetricsAsync(TimeSpan window)
        {
            try
            {
                var metrics = await GetRequestMetricsAsync(window);
                var aggregated = new Dictionary<string, double>
                {
                    { "TotalRequests", metrics.Count() },
                    { "AvgLatency", metrics.Any() ? metrics.Average(m => m.LatencyMs) : 0 },
                    { "MaxLatency", metrics.Any() ? metrics.Max(m => m.LatencyMs) : 0 },
                    { "MinLatency", metrics.Any() ? metrics.Min(m => m.LatencyMs) : 0 },
                    { "ErrorRate", CalculateErrorRate(metrics) }
                };

                return await Task.FromResult(aggregated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating metrics");
                return new Dictionary<string, double>();
            }
        }

        public async Task<(double P50, double P95, double P99)> GetLatencyPercentilesAsync(TimeSpan window)
        {
            try
            {
                var metrics = await GetRequestMetricsAsync(window);
                if (!metrics.Any())
                    return (0, 0, 0);

                var latencies = metrics
                    .OrderBy(m => m.LatencyMs)
                    .Select(m => (double)m.LatencyMs)
                    .ToList();

                var p50 = CalculatePercentile(latencies, 0.50);
                var p95 = CalculatePercentile(latencies, 0.95);
                var p99 = CalculatePercentile(latencies, 0.99);

                return await Task.FromResult((p50, p95, p99));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating percentiles");
                return (0, 0, 0);
            }
        }

        private double CalculatePercentile(List<double> values, double percentile)
        {
            if (!values.Any())
                return 0;

            var index = (int)Math.Ceiling(values.Count * percentile) - 1;
            return values[Math.Max(0, Math.Min(index, values.Count - 1))];
        }

        private double CalculateErrorRate(IEnumerable<RequestMetrics> metrics)
        {
            if (!metrics.Any())
                return 0;

            var total = metrics.Count();
            var errors = metrics.Count(m => m.StatusCode >= 400);
            return (double)errors / total;
        }
    }
}
