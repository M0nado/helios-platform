using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonadoBlade.Monitoring
{
    /// <summary>
    /// Query filter for metrics
    /// </summary>
    public class MetricFilter
    {
        public string MetricName { get; set; }
        public Dictionary<string, string> TagFilters { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// Query result with trend analysis
    /// </summary>
    public class QueryResult
    {
        public List<(DateTime Time, double Value)> DataPoints { get; set; }
        public double Mean { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double StdDev { get; set; }
        public double P50 { get; set; }
        public double P95 { get; set; }
        public double P99 { get; set; }
        public List<double> TrendLine { get; set; }
        public List<double> Forecast { get; set; }
    }

    /// <summary>
    /// Interface for metrics query engine
    /// </summary>
    public interface IMetricsQueryEngine
    {
        QueryResult Query(MetricFilter filter);
        List<(DateTime, double)> GetMovingAverage(string metricName, int windowSize);
        List<double> CalculateTrend(List<(DateTime, double)> dataPoints, int forecastPoints);
        double CalculateDerivative(List<(DateTime, double)> dataPoints);
        Dictionary<string, double> AnalyzeCorrelation(string metric1, string metric2);
        QueryResult AggregateByTimeRange(MetricFilter filter, TimeSpan interval);
    }

    /// <summary>
    /// Metrics query engine with complex query support
    /// </summary>
    public class MetricsQueryEngine : IMetricsQueryEngine
    {
        private readonly Dictionary<string, List<(DateTime, double)>> _metricHistory = new();
        private readonly object _historyLock = new();

        /// <summary>
        /// Record metric value for querying
        /// </summary>
        public void RecordMetric(string metricName, double value, DateTime timestamp)
        {
            lock (_historyLock)
            {
                if (!_metricHistory.ContainsKey(metricName))
                {
                    _metricHistory[metricName] = new List<(DateTime, double)>();
                }

                _metricHistory[metricName].Add((timestamp, value));

                // Keep only last 10000 points per metric
                if (_metricHistory[metricName].Count > 10000)
                {
                    _metricHistory[metricName] = _metricHistory[metricName]
                        .TakeLast(10000)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// Execute complex metric query with filtering and aggregation
        /// </summary>
        public QueryResult Query(MetricFilter filter)
        {
            lock (_historyLock)
            {
                if (!_metricHistory.TryGetValue(filter.MetricName, out var data) || data.Count == 0)
                {
                    return CreateEmptyResult();
                }

                var filtered = FilterDataPoints(data, filter.StartTime, filter.EndTime);
                if (filtered.Count == 0)
                {
                    return CreateEmptyResult();
                }

                var values = filtered.Select(d => d.Item2).ToList();

                var result = new QueryResult
                {
                    DataPoints = filtered,
                    Mean = values.Average(),
                    Min = values.Min(),
                    Max = values.Max(),
                    StdDev = CalculateStdDev(values),
                    P50 = CalculatePercentile(values, 50),
                    P95 = CalculatePercentile(values, 95),
                    P99 = CalculatePercentile(values, 99),
                    TrendLine = CalculateLinearRegression(filtered),
                    Forecast = CalculateForecast(filtered, 5)
                };

                return result;
            }
        }

        /// <summary>
        /// Calculate moving average for a metric
        /// </summary>
        public List<(DateTime, double)> GetMovingAverage(string metricName, int windowSize)
        {
            lock (_historyLock)
            {
                if (!_metricHistory.TryGetValue(metricName, out var data) || data.Count == 0)
                {
                    return new List<(DateTime, double)>();
                }

                var result = new List<(DateTime, double)>();
                for (int i = 0; i < data.Count; i++)
                {
                    var start = Math.Max(0, i - windowSize + 1);
                    var window = data.Skip(start).Take(i - start + 1);
                    var avg = window.Average(d => d.Item2);
                    result.Add((data[i].Item1, avg));
                }

                return result;
            }
        }

        /// <summary>
        /// <summary>
        /// Calculate trend and forecast future values
        /// </summary>
        public List<double> CalculateTrend(List<(DateTime, double)> dataPoints, int forecastPoints)
        {
            if (dataPoints.Count < 2)
                return new List<double>();

            return CalculateExponentialSmoothing(dataPoints.Select(d => d.Item2).ToList(), forecastPoints);
        }

        /// <summary>
        /// Calculate rate of change (derivative) of metric
        /// </summary>
        public double CalculateDerivative(List<(DateTime, double)> dataPoints)
        {
            if (dataPoints.Count < 2)
                return 0;

            var firstPoint = dataPoints.First();
            var lastPoint = dataPoints.Last();

            var timeSpanSeconds = (lastPoint.Item1 - firstPoint.Item1).TotalSeconds;
            if (timeSpanSeconds == 0)
                return 0;

            var valueDelta = lastPoint.Item2 - firstPoint.Item2;
            return valueDelta / timeSpanSeconds;
        }

        /// <summary>
        /// Analyze correlation between two metrics
        /// </summary>
        public Dictionary<string, double> AnalyzeCorrelation(string metric1, string metric2)
        {
            lock (_historyLock)
            {
                if (!_metricHistory.TryGetValue(metric1, out var data1) || data1.Count == 0 ||
                    !_metricHistory.TryGetValue(metric2, out var data2) || data2.Count == 0)
                {
                    return new Dictionary<string, double>();
                }

                var result = new Dictionary<string, double>();

                // Simple correlation: align time windows and calculate correlation coefficient
                var minCount = Math.Min(data1.Count, data2.Count);
                var values1 = data1.TakeLast(minCount).Select(d => d.Item2).ToList();
                var values2 = data2.TakeLast(minCount).Select(d => d.Item2).ToList();

                result["pearson_correlation"] = CalculatePearsonCorrelation(values1, values2);
                result["metric1_mean"] = values1.Average();
                result["metric2_mean"] = values2.Average();
                result["metric1_variance"] = CalculateVariance(values1);
                result["metric2_variance"] = CalculateVariance(values2);

                return result;
            }
        }

        /// <summary>
        /// Aggregate metrics by time range
        /// </summary>
        public QueryResult AggregateByTimeRange(MetricFilter filter, TimeSpan interval)
        {
            lock (_historyLock)
            {
                if (!_metricHistory.TryGetValue(filter.MetricName, out var data) || data.Count == 0)
                {
                    return CreateEmptyResult();
                }

                var filtered = FilterDataPoints(data, filter.StartTime, filter.EndTime);
                var aggregated = new List<(DateTime, double)>();

                var current = filtered.First().Item1;
                var intervalEnd = current.Add(interval);

                var currentBucket = new List<double>();

                foreach (var point in filtered)
                {
                    if (point.Item1 >= intervalEnd)
                    {
                        if (currentBucket.Count > 0)
                        {
                            aggregated.Add((current, currentBucket.Average()));
                        }

                        current = point.Item1;
                        intervalEnd = current.Add(interval);
                        currentBucket.Clear();
                    }

                    currentBucket.Add(point.Item2);
                }

                if (currentBucket.Count > 0)
                {
                    aggregated.Add((current, currentBucket.Average()));
                }

                var values = aggregated.Select(a => a.Item2).ToList();
                var result = new QueryResult
                {
                    DataPoints = aggregated,
                    Mean = values.Any() ? values.Average() : 0,
                    Min = values.Any() ? values.Min() : 0,
                    Max = values.Any() ? values.Max() : 0,
                    StdDev = values.Any() ? CalculateStdDev(values) : 0,
                    P50 = values.Any() ? CalculatePercentile(values, 50) : 0,
                    P95 = values.Any() ? CalculatePercentile(values, 95) : 0,
                    P99 = values.Any() ? CalculatePercentile(values, 99) : 0,
                };

                return result;
            }
        }

        private List<(DateTime, double)> FilterDataPoints(List<(DateTime, double)> data, DateTime? startTime, DateTime? endTime)
        {
            var filtered = data;

            if (startTime.HasValue)
                filtered = filtered.Where(d => d.Item1 >= startTime.Value).ToList();

            if (endTime.HasValue)
                filtered = filtered.Where(d => d.Item1 <= endTime.Value).ToList();

            return filtered;
        }

        private double CalculateStdDev(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            var mean = values.Average();
            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
            return Math.Sqrt(variance);
        }

        private double CalculatePercentile(List<double> values, double percentile)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
            return index >= 0 && index < sorted.Count ? sorted[index] : 0;
        }

        private List<double> CalculateLinearRegression(List<(DateTime, double)> dataPoints)
        {
            if (dataPoints.Count < 2)
                return new List<double>();

            var n = dataPoints.Count;
            var x = Enumerable.Range(0, n).Select(i => (double)i).ToList();
            var y = dataPoints.Select(d => d.Item2).ToList();

            var sumX = x.Sum();
            var sumY = y.Sum();
            var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            var sumX2 = x.Sum(xi => xi * xi);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            return x.Select(xi => slope * xi + intercept).ToList();
        }

        private List<double> CalculateExponentialSmoothing(List<double> values, int forecastPoints)
        {
            if (values.Count == 0)
                return new List<double>();

            var forecast = new List<double>();
            var alpha = 0.3;
            double smoothed = values[0];

            foreach (var value in values.Skip(1))
            {
                smoothed = alpha * value + (1 - alpha) * smoothed;
            }

            for (int i = 0; i < forecastPoints; i++)
            {
                forecast.Add(smoothed);
            }

            return forecast;
        }

        private double CalculatePearsonCorrelation(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count < 2)
                return 0;

            var meanX = x.Average();
            var meanY = y.Average();

            var covariance = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum() / (x.Count - 1);
            var stdDevX = Math.Sqrt(x.Sum(xi => Math.Pow(xi - meanX, 2)) / (x.Count - 1));
            var stdDevY = Math.Sqrt(y.Sum(yi => Math.Pow(yi - meanY, 2)) / (y.Count - 1));

            if (stdDevX == 0 || stdDevY == 0)
                return 0;

            return covariance / (stdDevX * stdDevY);
        }

        private double CalculateVariance(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            var mean = values.Average();
            return values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
        }

        private List<double> CalculateForecast(List<(DateTime, double)> dataPoints, int points)
        {
            var values = dataPoints.Select(d => d.Item2).ToList();
            return CalculateExponentialSmoothing(values, points);
        }

        private QueryResult CreateEmptyResult()
        {
            return new QueryResult
            {
                DataPoints = new List<(DateTime, double)>(),
                Mean = 0,
                Min = 0,
                Max = 0,
                StdDev = 0,
                P50 = 0,
                P95 = 0,
                P99 = 0,
                TrendLine = new List<double>(),
                Forecast = new List<double>()
            };
        }
    }
}
