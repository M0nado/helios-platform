namespace MonadoBlade.Core.Services;

/// <summary>
/// Dashboard metrics for a given time range.
/// </summary>
public class DashboardMetrics
{
    /// <summary>Gets or sets the time range start.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Gets or sets the time range end.</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Gets or sets general metrics.</summary>
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>Gets or sets time series data for charting.</summary>
    public Dictionary<string, List<(DateTime, double)>> TimeSeries { get; set; } = new();

    /// <summary>Gets or sets aggregated metrics.</summary>
    public Dictionary<string, (double Min, double Max, double Avg, double Median)> Aggregates { get; set; } = new();
}

/// <summary>
/// An actionable insight derived from metrics data.
/// </summary>
public class Insight
{
    /// <summary>Gets or sets the insight title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the insight description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the insight severity (info, warning, critical).</summary>
    public string Severity { get; set; } = "info";

    /// <summary>Gets or sets the metric this insight is based on.</summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>Gets or sets the value that triggered this insight.</summary>
    public object? Value { get; set; }

    /// <summary>Gets or sets recommended actions.</summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>Gets or sets the confidence in this insight (0-1).</summary>
    public float Confidence { get; set; } = 1.0f;
}

/// <summary>
/// An alert indicating abnormal system behavior.
/// </summary>
public class Alert
{
    /// <summary>Gets or sets the alert ID.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the alert title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the alert message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity level.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Gets or sets the metric that triggered the alert.</summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>Gets or sets the threshold that was exceeded.</summary>
    public object? Threshold { get; set; }

    /// <summary>Gets or sets the current value.</summary>
    public object? CurrentValue { get; set; }

    /// <summary>Gets or sets when the alert was detected.</summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets whether the alert has been acknowledged.</summary>
    public bool IsAcknowledged { get; set; }
}

/// <summary>
/// Aggregated metrics data for analysis.
/// </summary>
public class MetricsData
{
    /// <summary>Gets or sets the raw metrics.</summary>
    public Dictionary<string, object> Values { get; set; } = new();

    /// <summary>Gets or sets the time range.</summary>
    public (DateTime Start, DateTime End) TimeRange { get; set; }

    /// <summary>Gets or sets the sampling interval.</summary>
    public TimeSpan SamplingInterval { get; set; } = TimeSpan.FromSeconds(60);
}

/// <summary>
/// Date range for filtering.
/// </summary>
public class DateRange
{
    /// <summary>Gets or sets the start date.</summary>
    public DateTime Start { get; set; }

    /// <summary>Gets or sets the end date.</summary>
    public DateTime End { get; set; }

    /// <summary>Creates a DateRange for the last N days.</summary>
    public static DateRange Last(int days) => new() 
    { 
        Start = DateTime.UtcNow.AddDays(-days), 
        End = DateTime.UtcNow 
    };

    /// <summary>Creates a DateRange for the last N hours.</summary>
    public static DateRange LastHours(int hours) => new() 
    { 
        Start = DateTime.UtcNow.AddHours(-hours), 
        End = DateTime.UtcNow 
    };
}

/// <summary>
/// Dashboard service providing real-time metrics, insights, and anomaly detection.
/// Responsible for aggregating data across services and presenting actionable intelligence.
/// </summary>
public interface IDashboardService : IService
{
    /// <summary>
    /// Retrieves metrics for a given time range.
    /// </summary>
    /// <param name="range">The date/time range to retrieve metrics for.</param>
    /// <returns>The aggregated metrics.</returns>
    /// <exception cref="OperationFailedException">Thrown when metrics retrieval fails.</exception>
    Task<DashboardMetrics> GetMetricsAsync(DateRange range);

    /// <summary>
    /// Generates actionable insights from metrics data.
    /// </summary>
    /// <param name="data">The metrics data to analyze.</param>
    /// <returns>A collection of insights.</returns>
    Task<List<Insight>> GenerateInsightsAsync(MetricsData data);

    /// <summary>
    /// Detects anomalies in the metrics data.
    /// </summary>
    /// <param name="data">The metrics data to analyze.</param>
    /// <returns>A collection of detected anomalies as alerts.</returns>
    Task<Alert[]> DetectAnomaliesAsync(MetricsData data);

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    /// <returns>A collection of currently active alerts.</returns>
    Task<List<Alert>> GetActiveAlertsAsync();

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    /// <param name="alertId">The alert ID.</param>
    /// <returns>A task representing the acknowledgment operation.</returns>
    Task AcknowledgeAlertAsync(string alertId);

    /// <summary>
    /// Clears a resolved alert.
    /// </summary>
    /// <param name="alertId">The alert ID.</param>
    /// <returns>A task representing the clear operation.</returns>
    Task ClearAlertAsync(string alertId);

    /// <summary>
    /// Exports metrics data in a specific format.
    /// </summary>
    /// <param name="format">Export format (csv, json, etc).</param>
    /// <param name="range">The date range to export.</param>
    /// <returns>The exported data as string.</returns>
    Task<string> ExportMetricsAsync(string format, DateRange range);

    /// <summary>
    /// Subscribes to real-time metric updates.
    /// </summary>
    /// <param name="metricName">The metric to subscribe to.</param>
    /// <param name="handler">Handler to invoke when metric updates.</param>
    /// <returns>A disposable subscription.</returns>
    IDisposable SubscribeToMetricUpdates(string metricName, Action<object?> handler);

    /// <summary>
    /// Gets custom dashboard configuration.
    /// </summary>
    /// <returns>The dashboard configuration.</returns>
    Task<Dictionary<string, object>> GetDashboardConfigAsync();

    /// <summary>
    /// Saves custom dashboard configuration.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <returns>A task representing the save operation.</returns>
    Task SaveDashboardConfigAsync(Dictionary<string, object> config);
}
