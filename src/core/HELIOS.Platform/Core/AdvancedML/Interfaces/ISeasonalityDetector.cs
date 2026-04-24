namespace HELIOS.Platform.Core.AdvancedML.Interfaces;

/// <summary>
/// Advanced seasonality detection for time-series pattern recognition.
/// Identifies recurring patterns and seasonal components in data.
/// </summary>
public interface ISeasonalityDetector
{
    /// <summary>
    /// Detects seasonal patterns in time-series data.
    /// </summary>
    /// <param name="timeSeries">Time-series values.</param>
    /// <param name="timestamps">Corresponding timestamps.</param>
    /// <returns>Detected seasonal pattern.</returns>
    Task<SeasonalPattern> DetectSeasonalityAsync(double[] timeSeries, DateTime[] timestamps);

    /// <summary>
    /// Removes seasonal component from data (deseasonalization).
    /// </summary>
    Task<double[]> DeseasonalizeAsync(double[] timeSeries, SeasonalPattern pattern);

    /// <summary>
    /// Forecasts future values considering seasonality.
    /// </summary>
    Task<double[]> ForecastWithSeasonalityAsync(double[] timeSeries, int stepsAhead, SeasonalPattern pattern);

    /// <summary>
    /// Detects multiple seasonal periods (e.g., daily, weekly, monthly).
    /// </summary>
    Task<List<SeasonalPeriod>> DetectMultiplePeriodsAsync(double[] timeSeries, DateTime[] timestamps);

    /// <summary>
    /// Calculates seasonal indices for decomposition.
    /// </summary>
    Task<SeasonalDecomposition> DecomposeAsync(double[] timeSeries);
}

/// <summary>
/// Detected seasonal pattern.
/// </summary>
public class SeasonalPattern
{
    /// <summary>Period of seasonality (e.g., 24 for hourly, 7 for daily, 365 for yearly).</summary>
    public int Period { get; set; }

    /// <summary>Strength of seasonality (0-1).</summary>
    public double Strength { get; set; }

    /// <summary>Seasonal indices for each period position.</summary>
    public double[] SeasonalIndices { get; set; } = Array.Empty<double>();

    /// <summary>Seasonal factors relative to mean.</summary>
    public double[] SeasonalFactors { get; set; } = Array.Empty<double>();

    /// <summary>Type of seasonality (additive or multiplicative).</summary>
    public string SeasonalityType { get; set; } = "additive";

    /// <summary>Autocorrelation at seasonal lag.</summary>
    public double SeasonalAutocorrelation { get; set; }

    /// <summary>Confidence in detected seasonality (0-1).</summary>
    public double Confidence { get; set; }

    /// <summary>Timestamp when pattern was detected.</summary>
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Multiple seasonal periods in time-series.
/// </summary>
public class SeasonalPeriod
{
    /// <summary>Period length (samples or time units).</summary>
    public int Period { get; set; }

    /// <summary>Period duration as human-readable string.</summary>
    public string PeriodDescription { get; set; } = string.Empty;

    /// <summary>Strength of this seasonal component (0-1).</summary>
    public double Strength { get; set; }

    /// <summary>Autocorrelation value at this period.</summary>
    public double Autocorrelation { get; set; }

    /// <summary>Seasonal pattern for this period.</summary>
    public double[] Pattern { get; set; } = Array.Empty<double>();

    /// <summary>Rank among all detected periods (1=strongest).</summary>
    public int Rank { get; set; }
}

/// <summary>
/// Time-series decomposition into trend, seasonal, and residual.
/// </summary>
public class SeasonalDecomposition
{
    /// <summary>Original time-series.</summary>
    public double[] Original { get; set; } = Array.Empty<double>();

    /// <summary>Trend component.</summary>
    public double[] Trend { get; set; } = Array.Empty<double>();

    /// <summary>Seasonal component.</summary>
    public double[] Seasonal { get; set; } = Array.Empty<double>();

    /// <summary>Residual (noise) component.</summary>
    public double[] Residual { get; set; } = Array.Empty<double>();

    /// <summary>Decomposition method used.</summary>
    public string Method { get; set; } = "Classical";

    /// <summary>Variance explained by seasonal component (0-1).</summary>
    public double SeasonalVarianceRatio { get; set; }

    /// <summary>Variance explained by trend component (0-1).</summary>
    public double TrendVarianceRatio { get; set; }

    /// <summary>Variance explained by residual (0-1).</summary>
    public double ResidualVarianceRatio { get; set; }

    /// <summary>Quality of decomposition fit (0-1).</summary>
    public double DecompositionQuality { get; set; }

    /// <summary>Timestamp of decomposition.</summary>
    public DateTime DecomposedAt { get; set; }
}
