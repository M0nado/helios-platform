namespace HELIOS.Platform.Contracts.Analytics;

/// <summary>
/// Shared metric observation contract consumed by analytics, ML, and UI components.
/// </summary>
public interface IMetricPoint
{
    DateTimeOffset Timestamp { get; }
    string Name { get; }
    double Value { get; }
    IReadOnlyDictionary<string, string> Tags { get; }
}

/// <summary>
/// Shared statistical summary contract for numeric analytics workloads.
/// </summary>
public interface IStatisticalSummary
{
    int Count { get; }
    double Mean { get; }
    double Median { get; }
    double Variance { get; }
    double StandardDeviation { get; }
    double Minimum { get; }
    double Maximum { get; }
    IReadOnlyDictionary<string, double> Percentiles { get; }
}

/// <summary>
/// Shared forecast contract used by HELIOS prediction and optimization services.
/// </summary>
public interface IPredictionResult
{
    double PredictedValue { get; }
    double Confidence { get; }
    string Model { get; }
    DateTimeOffset GeneratedAt { get; }
    IReadOnlyDictionary<string, double> Diagnostics { get; }
}

/// <summary>
/// Shared anomaly score emitted by statistical analytics.
/// </summary>
public interface IAnomalyScore
{
    double Value { get; }
    double Score { get; }
}

/// <summary>
/// Shared window aggregate contract for time-series analytics.
/// </summary>
public interface IAnalyticsWindow
{
    string Name { get; }
    DateTimeOffset StartedAt { get; }
    DateTimeOffset EndedAt { get; }
    IStatisticalSummary Summary { get; }
    double Trend { get; }
    string TrendClassification { get; }
    double HealthScore { get; }
}

/// <summary>
/// C#-friendly analytics engine contract for cross-language consumers.
/// </summary>
public interface IAnalyticsEngine
{
    IStatisticalSummary Summarize(IEnumerable<double> values);
    IReadOnlyList<IAnomalyScore> DetectAnomalies(IEnumerable<double> values, double threshold);
    IAnalyticsWindow BuildWindow(string name, DateTimeOffset startedAt, DateTimeOffset endedAt, IEnumerable<IMetricPoint> points);
    IPredictionResult MovingAverageForecast(IEnumerable<double> values, int period);
    IPredictionResult LinearForecast(IEnumerable<double> values);
}
