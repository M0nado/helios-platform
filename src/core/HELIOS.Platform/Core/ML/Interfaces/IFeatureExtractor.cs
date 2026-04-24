namespace HELIOS.Platform.Core.ML.Interfaces;

/// <summary>
/// Manages feature engineering and statistical feature extraction from metrics.
/// Transforms raw metrics into ML-ready features.
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>
    /// Extracts statistical features from a time series.
    /// </summary>
    Task<FeatureVector> ExtractFeaturesAsync(IList<MetricSnapshot> timeSeries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes rolling window features (moving average, standard deviation, etc).
    /// </summary>
    Task<IList<FeatureVector>> ExtractRollingFeaturesAsync(IList<MetricSnapshot> timeSeries, int windowSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts trend and seasonal features using decomposition.
    /// </summary>
    Task<SeasonalDecomposition> DecomposeTimeSeriesAsync(IList<MetricSnapshot> timeSeries, int seasonalPeriod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes correlation between features.
    /// </summary>
    Task<CorrelationMatrix> ComputeCorrelationsAsync(IList<FeatureVector> features, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs feature selection to reduce dimensionality.
    /// </summary>
    Task<IList<string>> SelectTopFeaturesAsync(IList<FeatureVector> features, int topK, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a vector of features for ML models.
/// </summary>
public class FeatureVector
{
    public DateTime Timestamp { get; set; }
    public IDictionary<string, double> Features { get; set; } = new Dictionary<string, double>();
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets feature by name.
    /// </summary>
    public double GetFeature(string name) => Features.TryGetValue(name, out var value) ? value : 0.0;
}

/// <summary>
/// Represents time series decomposition into trend, seasonal, and residual components.
/// </summary>
public class SeasonalDecomposition
{
    public IList<double> Trend { get; set; } = new List<double>();
    public IList<double> Seasonal { get; set; } = new List<double>();
    public IList<double> Residual { get; set; } = new List<double>();
    public double TrendStrength { get; set; }
    public double SeasonalStrength { get; set; }
}

/// <summary>
/// Correlation matrix for feature relationships.
/// </summary>
public class CorrelationMatrix
{
    public IDictionary<string, IDictionary<string, double>> Correlations { get; set; } = new Dictionary<string, IDictionary<string, double>>();
    
    public double GetCorrelation(string feature1, string feature2)
    {
        if (Correlations.TryGetValue(feature1, out var row) && row.TryGetValue(feature2, out var value))
            return value;
        return 0.0;
    }
}
