namespace HELIOS.Platform.Core.ML.Interfaces;

/// <summary>
/// Provides time-series forecasting and trend analysis for system metrics.
/// Predicts future values based on historical patterns.
/// </summary>
public interface IPredictiveAnalytics
{
    /// <summary>
    /// Forecasts metric values for a future time period.
    /// </summary>
    Task<Forecast> ForecastAsync(string serviceName, string metricName, int steps, 
        TimeSpan stepInterval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes trend in a time series.
    /// </summary>
    Task<TrendAnalysis> AnalyzeTrendAsync(string serviceName, string metricName, 
        DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects change points in metric behavior.
    /// </summary>
    Task<IList<ChangePoint>> DetectChangePointsAsync(string serviceName, string metricName, 
        DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes confidence intervals for forecast.
    /// </summary>
    Task<IList<ForecastPoint>> ForecastWithConfidenceAsync(string serviceName, string metricName, 
        int steps, double confidenceLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trains forecasting model on historical data.
    /// </summary>
    Task TrainForecastingModelAsync(string serviceName, string metricName, DateTime start, 
        DateTime end, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a forecast for a metric.
/// </summary>
public class Forecast
{
    public string ServiceName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public IList<ForecastPoint> Points { get; set; } = new List<ForecastPoint>();
    public DateTime GeneratedAt { get; set; }
    public double ModelAccuracy { get; set; }
    public string ForecastMethod { get; set; } = string.Empty;
}

/// <summary>
/// A single point in a forecast.
/// </summary>
public class ForecastPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public double? UpperConfidenceInterval { get; set; }
    public double? LowerConfidenceInterval { get; set; }
}

/// <summary>
/// Analysis of trend in data.
/// </summary>
public class TrendAnalysis
{
    public enum TrendDirection { Increasing, Decreasing, Stable }
    
    public TrendDirection Direction { get; set; }
    public double SlopePerDay { get; set; }
    public double Volatility { get; set; }
    public double RSquared { get; set; }
    public DateTime AnalysisStart { get; set; }
    public DateTime AnalysisEnd { get; set; }
    public string Interpretation { get; set; } = string.Empty;
}

/// <summary>
/// Represents a detected change point in behavior.
/// </summary>
public class ChangePoint
{
    public DateTime Timestamp { get; set; }
    public double MagnitudeOfChange { get; set; }
    public double Confidence { get; set; }
    public string ChangeType { get; set; } = string.Empty; // spike, drift, level-shift
}
