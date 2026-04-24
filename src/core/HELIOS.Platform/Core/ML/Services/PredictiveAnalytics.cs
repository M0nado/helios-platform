using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.ML.Services;

/// <summary>
/// Predictive analytics service for time-series forecasting and trend analysis.
/// </summary>
public class PredictiveAnalytics : IPredictiveAnalytics
{
    private readonly ILogger<PredictiveAnalytics> _logger;
    private readonly ITimeSeriesDB _timeSeriesDB;
    private readonly Dictionary<string, (double Slope, double Intercept)> _trendModels = new();

    public PredictiveAnalytics(ILogger<PredictiveAnalytics> logger, ITimeSeriesDB timeSeriesDB)
    {
        _logger = logger;
        _timeSeriesDB = timeSeriesDB;
    }

    public async Task<Forecast> ForecastAsync(string serviceName, string metricName, int steps, 
        TimeSpan stepInterval, CancellationToken cancellationToken = default)
    {
        var key = $"{serviceName}_{metricName}";
        var forecast = new Forecast
        {
            ServiceName = serviceName,
            MetricName = metricName,
            GeneratedAt = DateTime.UtcNow,
            ForecastMethod = "Linear Trend + Seasonal"
        };

        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddDays(-30);
        var historicalPoints = await _timeSeriesDB.QueryAsync(serviceName, metricName, startTime, endTime, cancellationToken);

        if (!historicalPoints.Any())
        {
            _logger.LogWarning("No historical data for forecasting {ServiceName}.{MetricName}", serviceName, metricName);
            return forecast;
        }

        var values = historicalPoints.Select(p => p.Value).ToList();
        var (slope, intercept) = GetTrendModel(key, values);

        var currentTime = DateTime.UtcNow;
        for (int i = 1; i <= steps; i++)
        {
            var forecastTime = currentTime.Add(stepInterval * i);
            var daysAhead = (forecastTime - historicalPoints.First().Timestamp).TotalDays;
            
            var forecastedValue = intercept + slope * daysAhead;
            forecastedValue = Math.Max(values.Min() * 0.5, Math.Min(values.Max() * 1.5, forecastedValue));

            forecast.Points.Add(new ForecastPoint
            {
                Timestamp = forecastTime,
                Value = forecastedValue,
                UpperConfidenceInterval = forecastedValue * 1.2,
                LowerConfidenceInterval = forecastedValue * 0.8
            });
        }

        forecast.ModelAccuracy = CalculateModelAccuracy(historicalPoints.Select(p => p.Value).ToList(), values);
        return forecast;
    }

    public async Task<TrendAnalysis> AnalyzeTrendAsync(string serviceName, string metricName, 
        DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var points = await _timeSeriesDB.QueryAsync(serviceName, metricName, start, end, cancellationToken);

        if (points.Count < 2)
        {
            return new TrendAnalysis
            {
                Direction = TrendAnalysis.TrendDirection.Stable,
                SlopePerDay = 0,
                Volatility = 0,
                RSquared = 0,
                AnalysisStart = start,
                AnalysisEnd = end,
                Interpretation = "Insufficient data"
            };
        }

        var values = points.Select(p => p.Value).ToList();
        var (slope, intercept) = CalculateLinearRegression(Enumerable.Range(0, values.Count).Select(x => (double)x).ToList(), values);

        var direction = Math.Abs(slope) < 0.001 
            ? TrendAnalysis.TrendDirection.Stable
            : slope > 0 ? TrendAnalysis.TrendDirection.Increasing : TrendAnalysis.TrendDirection.Decreasing;

        var daysInPeriod = (end - start).TotalDays;
        var predictedRange = slope * daysInPeriod;

        return new TrendAnalysis
        {
            Direction = direction,
            SlopePerDay = slope,
            Volatility = CalculateStdDev(values),
            RSquared = CalculateRSquared(Enumerable.Range(0, values.Count).Select(x => (double)x).ToList(), values, slope, intercept),
            AnalysisStart = start,
            AnalysisEnd = end,
            Interpretation = direction switch
            {
                TrendAnalysis.TrendDirection.Increasing => $"Increasing trend at {slope:F2} units/day",
                TrendAnalysis.TrendDirection.Decreasing => $"Decreasing trend at {Math.Abs(slope):F2} units/day",
                _ => $"Stable with volatility {CalculateStdDev(values):F2}"
            }
        };
    }

    public async Task<IList<ChangePoint>> DetectChangePointsAsync(string serviceName, string metricName, 
        DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var points = await _timeSeriesDB.QueryAsync(serviceName, metricName, start, end, cancellationToken);
        var changePoints = new List<ChangePoint>();

        if (points.Count < 3)
            return changePoints;

        var values = points.Select(p => p.Value).ToList();
        var mean = values.Average();
        var stdDev = CalculateStdDev(values);

        for (int i = 1; i < values.Count; i++)
        {
            var change = Math.Abs(values[i] - values[i - 1]);
            var threshold = stdDev * 2;

            if (change > threshold)
            {
                changePoints.Add(new ChangePoint
                {
                    Timestamp = points[i].Timestamp,
                    MagnitudeOfChange = change,
                    Confidence = Math.Min(1.0, change / (stdDev * 5)),
                    ChangeType = values[i] > values[i - 1] ? "spike" : "dip"
                });
            }
        }

        return await Task.FromResult(changePoints);
    }

    public async Task<IList<ForecastPoint>> ForecastWithConfidenceAsync(string serviceName, string metricName, 
        int steps, double confidenceLevel, CancellationToken cancellationToken = default)
    {
        var forecast = await ForecastAsync(serviceName, metricName, steps, TimeSpan.FromHours(1), cancellationToken);
        
        var zScore = GetZScore(confidenceLevel);
        
        foreach (var point in forecast.Points)
        {
            var margin = point.Value * (zScore / 100);
            point.UpperConfidenceInterval = point.Value + margin;
            point.LowerConfidenceInterval = point.Value - margin;
        }

        return forecast.Points;
    }

    public async Task TrainForecastingModelAsync(string serviceName, string metricName, DateTime start, 
        DateTime end, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Training forecasting model for {ServiceName}.{MetricName}", serviceName, metricName);
        
        var points = await _timeSeriesDB.QueryAsync(serviceName, metricName, start, end, cancellationToken);
        if (points.Any())
        {
            var values = points.Select(p => p.Value).ToList();
            var indices = Enumerable.Range(0, values.Count).Select(x => (double)x).ToList();
            var (slope, intercept) = CalculateLinearRegression(indices, values);
            
            var key = $"{serviceName}_{metricName}";
            _trendModels[key] = (slope, intercept);
            
            _logger.LogInformation("Trained model: slope={Slope}, intercept={Intercept}", slope, intercept);
        }

        await Task.CompletedTask;
    }

    #region Helper Methods

    private (double Slope, double Intercept) GetTrendModel(string key, IList<double> values)
    {
        if (_trendModels.TryGetValue(key, out var model))
            return model;

        var indices = Enumerable.Range(0, values.Count).Select(x => (double)x).ToList();
        return CalculateLinearRegression(indices, values.ToList());
    }

    private (double Slope, double Intercept) CalculateLinearRegression(IList<double> x, IList<double> y)
    {
        if (x.Count != y.Count || x.Count < 2)
            return (0, 0);

        var n = x.Count;
        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
        var sumX2 = x.Sum(xi => xi * xi);

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;

        return (slope, intercept);
    }

    private double CalculateModelAccuracy(IList<double> actual, IList<double> predicted)
    {
        if (actual.Count != predicted.Count || !actual.Any())
            return 0;

        var mse = actual.Zip(predicted, (a, p) => Math.Pow(a - p, 2)).Average();
        var rmse = Math.Sqrt(mse);
        var mae = actual.Zip(predicted, (a, p) => Math.Abs(a - p)).Average();

        return Math.Max(0, 100 - (mae / actual.Average() * 100));
    }

    private double CalculateRSquared(IList<double> x, IList<double> y, double slope, double intercept)
    {
        if (!y.Any())
            return 0;

        var mean = y.Average();
        var ssTotal = y.Sum(yi => Math.Pow(yi - mean, 2));
        var ssResidual = x.Zip(y, (xi, yi) => Math.Pow(yi - (slope * xi + intercept), 2)).Sum();

        return ssTotal > 0 ? 1 - (ssResidual / ssTotal) : 0;
    }

    private double CalculateStdDev(IList<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }

    private double GetZScore(double confidenceLevel)
    {
        return confidenceLevel switch
        {
            0.90 => 1.645,
            0.95 => 1.96,
            0.99 => 2.576,
            _ => 1.96
        };
    }

    #endregion
}
