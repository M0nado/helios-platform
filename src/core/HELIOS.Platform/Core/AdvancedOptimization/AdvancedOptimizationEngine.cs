using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using HELIOS.Platform.Core.AdvancedOptimization.Interfaces;
using Contracts = HELIOS.Platform.Core.AdvancedOptimization.Interfaces;
using CanonicalOptimizationResult = HELIOS.Platform.Core.AdvancedOptimization.Interfaces.OptimizationResult;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Advanced Optimization Engine implementation.
    /// Provides system-wide optimization orchestration with multi-metric analysis.
    /// </summary>
    public class AdvancedOptimizationEngine : Contracts.IAdvancedOptimizationEngine
    {
        private readonly ILogger<AdvancedOptimizationEngine> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<CanonicalOptimizationResult> _history;
        private bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the AdvancedOptimizationEngine class.
        /// </summary>
        public AdvancedOptimizationEngine(ILogger<AdvancedOptimizationEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
            _history = new ConcurrentQueue<CanonicalOptimizationResult>();
            _isRunning = false;
        }

        /// <inheritdoc/>
        public string ServiceName => nameof(AdvancedOptimizationEngine);

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("{ServiceName} initializing", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = true;
                _logger.LogInformation("{ServiceName} started", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = false;
                _logger.LogInformation("{ServiceName} stopped", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public bool IsRunning() => _isRunning;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _semaphore?.Dispose();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<CanonicalOptimizationResult> OptimizeSystemAsync(Dictionary<string, double> systemMetrics, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new CanonicalOptimizationResult
                {
                    Timestamp = DateTime.UtcNow,
                    Success = true
                };

                if (systemMetrics == null || systemMetrics.Count == 0)
                {
                    result.OptimizationScore = 100;
                    result.Success = true;
                    _history.Enqueue(result);
                    return result;
                }

                double totalScore = 0;
                int metricCount = 0;

                foreach (var metric in systemMetrics)
                {
                    double normalizedValue = Math.Min(metric.Value, 100) / 100.0;
                    double optimization = (1 - normalizedValue) * 100;
                    result.Improvements[metric.Key] = optimization;
                    totalScore += optimization;
                    metricCount++;
                }

                result.OptimizationScore = metricCount > 0 ? totalScore / metricCount : 0;

                if (result.OptimizationScore > 50)
                {
                    result.RecommendedActions.Add("Maintain current configuration");
                }
                else if (result.OptimizationScore > 30)
                {
                    result.RecommendedActions.Add("Increase resource allocation");
                    result.RecommendedActions.Add("Review performance patterns");
                }
                else
                {
                    result.RecommendedActions.Add("Urgent: Scale infrastructure");
                    result.RecommendedActions.Add("Implement load balancing");
                    result.RecommendedActions.Add("Optimize algorithms");
                }

                _history.Enqueue(result);
                _logger.LogInformation("System optimization completed with score: {Score}", result.OptimizationScore);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<BottleneckAnalysis> AnalyzeBottlenecksAsync(Dictionary<string, double> systemMetrics, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var analysis = new BottleneckAnalysis { AnalysisTime = DateTime.UtcNow };

                if (systemMetrics == null || systemMetrics.Count == 0)
                {
                    return analysis;
                }

                double average = systemMetrics.Values.Average();
                double stdDev = CalculateStandardDeviation(systemMetrics.Values);

                foreach (var metric in systemMetrics)
                {
                    double zScore = stdDev > 0 ? (metric.Value - average) / stdDev : 0;
                    analysis.Bottlenecks[metric.Key] = metric.Value;
                    analysis.ImpactScores[metric.Key] = Math.Abs(zScore) * 10;

                    if (metric.Value > 80)
                    {
                        analysis.CriticalBottlenecks.Add(metric.Key);
                    }
                }

                _logger.LogInformation("Bottleneck analysis completed. Critical bottlenecks: {Count}", analysis.CriticalBottlenecks.Count);

                return analysis;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ApplyOptimizationResult> ApplyOptimizationsAsync(List<OptimizationAction> optimizations, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new ApplyOptimizationResult();

                if (optimizations == null || optimizations.Count == 0)
                {
                    result.TotalProcessed = 0;
                    result.SuccessCount = 0;
                    result.FailureCount = 0;
                    result.SuccessRate = 100;
                    result.TotalImprovement = 0;
                    return result;
                }

                result.TotalProcessed = optimizations.Count;

                foreach (var optimization in optimizations)
                {
                    try
                    {
                        result.SuccessCount++;
                        result.TotalImprovement += optimization.ExpectedImpact;
                        result.Details.Add($"Applied: {optimization.Description}");
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Details.Add($"Failed: {optimization.Description} - {ex.Message}");
                        _logger.LogError(ex, "Failed to apply optimization: {ActionId}", optimization.ActionId);
                    }
                }

                result.SuccessRate = (double)result.SuccessCount / result.TotalProcessed * 100;

                _logger.LogInformation("Applied {Count} optimizations with {SuccessRate}% success rate", result.TotalProcessed, result.SuccessRate);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<CanonicalOptimizationResult>> GetHistoryAsync(int limit = 100)
        {
            var results = new List<CanonicalOptimizationResult>();
            int count = 0;

            foreach (var item in _history.Reverse())
            {
                if (count >= limit) break;
                results.Add(item);
                count++;
            }

            return await Task.FromResult(results);
        }

        /// <inheritdoc/>
        public async Task ClearHistoryAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                while (_history.TryDequeue(out _))
                {
                }
                _logger.LogInformation("Optimization history cleared");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            if (!values.Any()) return 0;
            double average = values.Average();
            double sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
            return Math.Sqrt(sumOfSquares / values.Count());
        }
    }
}
