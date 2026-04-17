using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.ML.Services;

/// <summary>
/// Feature extraction service using statistical and signal processing techniques.
/// </summary>
public class FeatureExtractor : IFeatureExtractor
{
    private readonly ILogger<FeatureExtractor> _logger;

    public FeatureExtractor(ILogger<FeatureExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<FeatureVector> ExtractFeaturesAsync(IList<MetricSnapshot> timeSeries, CancellationToken cancellationToken = default)
    {
        var features = new Dictionary<string, double>();

        if (!timeSeries.Any())
            return new FeatureVector { Features = features, Timestamp = DateTime.UtcNow };

        var values = ExtractValues(timeSeries);

        // Statistical features
        features["mean"] = values.Average();
        features["median"] = Percentile(values, 50);
        features["std_dev"] = CalculateStdDev(values);
        features["variance"] = Math.Pow(features["std_dev"], 2);
        features["min"] = values.Min();
        features["max"] = values.Max();
        features["range"] = features["max"] - features["min"];
        features["skewness"] = CalculateSkewness(values);
        features["kurtosis"] = CalculateKurtosis(values);
        
        // Trend features
        features["trend"] = CalculateTrend(values);
        features["trend_strength"] = Math.Abs(features["trend"]);
        
        // Autocorrelation features
        features["autocorr_lag1"] = CalculateAutocorrelation(values, 1);
        features["autocorr_lag7"] = CalculateAutocorrelation(values, Math.Min(7, values.Count / 2));

        return await Task.FromResult(new FeatureVector
        {
            Timestamp = timeSeries.Last().Timestamp,
            Features = features,
            ServiceName = timeSeries.First().ServiceName
        });
    }

    public async Task<IList<FeatureVector>> ExtractRollingFeaturesAsync(IList<MetricSnapshot> timeSeries, int windowSize, CancellationToken cancellationToken = default)
    {
        var rollingFeatures = new List<FeatureVector>();
        var values = ExtractValues(timeSeries);

        for (int i = windowSize; i <= values.Count; i++)
        {
            var window = values.Skip(i - windowSize).Take(windowSize).ToList();
            var windowTime = timeSeries.Skip(i - windowSize).Take(windowSize).ToList();
            
            var snapshot = await ExtractFeaturesAsync(windowTime, cancellationToken);
            rollingFeatures.Add(snapshot);
        }

        return rollingFeatures;
    }

    public async Task<SeasonalDecomposition> DecomposeTimeSeriesAsync(IList<MetricSnapshot> timeSeries, int seasonalPeriod, CancellationToken cancellationToken = default)
    {
        var values = ExtractValues(timeSeries);
        
        // Simple seasonal decomposition
        var trend = CalculateTrendComponent(values, Math.Max(3, seasonalPeriod / 2));
        var seasonal = new List<double>();
        var residual = new List<double>();

        for (int i = 0; i < values.Count; i++)
        {
            var s = i < seasonal.Count ? seasonal[i] : 0;
            var r = values[i] - trend[i] - s;
            residual.Add(r);
        }

        return await Task.FromResult(new SeasonalDecomposition
        {
            Trend = trend,
            Seasonal = seasonal,
            Residual = residual,
            TrendStrength = CalculateTrendStrength(trend, residual),
            SeasonalStrength = CalculateSeasonalStrength(seasonal, residual)
        });
    }

    public async Task<CorrelationMatrix> ComputeCorrelationsAsync(IList<FeatureVector> features, CancellationToken cancellationToken = default)
    {
        var matrix = new CorrelationMatrix();
        
        if (!features.Any())
            return matrix;

        var featureNames = features.First().Features.Keys.ToList();
        
        foreach (var f1 in featureNames)
        {
            if (!matrix.Correlations.ContainsKey(f1))
                matrix.Correlations[f1] = new Dictionary<string, double>();

            foreach (var f2 in featureNames)
            {
                var values1 = features.Select(f => f.GetFeature(f1)).ToList();
                var values2 = features.Select(f => f.GetFeature(f2)).ToList();
                matrix.Correlations[f1][f2] = CalculatePearsonCorrelation(values1, values2);
            }
        }

        return await Task.FromResult(matrix);
    }

    public async Task<IList<string>> SelectTopFeaturesAsync(IList<FeatureVector> features, int topK, CancellationToken cancellationToken = default)
    {
        if (!features.Any())
            return new List<string>();

        var featureScores = new Dictionary<string, double>();
        var featureNames = features.First().Features.Keys;

        foreach (var name in featureNames)
        {
            var values = features.Select(f => f.GetFeature(name)).ToList();
            featureScores[name] = values.Average() * CalculateStdDev(values);
        }

        return await Task.FromResult(
            featureScores.OrderByDescending(kvp => Math.Abs(kvp.Value))
                .Take(topK)
                .Select(kvp => kvp.Key)
                .ToList()
        );
    }

    #region Helper Methods

    private List<double> ExtractValues(IList<MetricSnapshot> timeSeries)
    {
        return timeSeries.SelectMany(ts => ts.Metrics.Values)
            .OfType<double>()
            .ToList();
    }

    private double CalculateStdDev(IList<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }

    private double CalculateSkewness(IList<double> values)
    {
        if (values.Count < 3) return 0;
        var mean = values.Average();
        var stdDev = CalculateStdDev(values);
        if (stdDev == 0) return 0;
        return values.Average(x => Math.Pow((x - mean) / stdDev, 3));
    }

    private double CalculateKurtosis(IList<double> values)
    {
        if (values.Count < 4) return 3;
        var mean = values.Average();
        var stdDev = CalculateStdDev(values);
        if (stdDev == 0) return 3;
        return values.Average(x => Math.Pow((x - mean) / stdDev, 4)) - 3;
    }

    private double CalculateTrend(IList<double> values)
    {
        if (values.Count < 2) return 0;
        var indices = Enumerable.Range(0, values.Count).Select(x => (double)x).ToList();
        return CalculatePearsonCorrelation(indices, values);
    }

    private double CalculateAutocorrelation(IList<double> values, int lag)
    {
        if (values.Count <= lag) return 0;
        var original = values.Take(values.Count - lag).ToList();
        var lagged = values.Skip(lag).ToList();
        return CalculatePearsonCorrelation(original, lagged);
    }

    private double CalculatePearsonCorrelation(IList<double> x, IList<double> y)
    {
        if (x.Count != y.Count || x.Count < 2) return 0;
        var meanX = x.Average();
        var meanY = y.Average();
        var covariance = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum() / x.Count;
        var stdX = CalculateStdDev(x.ToList());
        var stdY = CalculateStdDev(y.ToList());
        return stdX > 0 && stdY > 0 ? covariance / (stdX * stdY) : 0;
    }

    private List<double> CalculateTrendComponent(IList<double> values, int windowSize)
    {
        var trend = new List<double>();
        for (int i = 0; i < values.Count; i++)
        {
            var start = Math.Max(0, i - windowSize / 2);
            var end = Math.Min(values.Count, i + windowSize / 2);
            var window = values.Skip(start).Take(end - start).ToList();
            trend.Add(window.Average());
        }
        return trend;
    }

    private double CalculateTrendStrength(IList<double> trend, IList<double> residual)
    {
        if (!trend.Any() || !residual.Any()) return 0;
        var trendVar = CalculateStdDev(trend.ToList());
        var residualVar = CalculateStdDev(residual.ToList());
        return residualVar > 0 ? trendVar / (trendVar + residualVar) : 0;
    }

    private double CalculateSeasonalStrength(IList<double> seasonal, IList<double> residual)
    {
        if (!seasonal.Any() || !residual.Any()) return 0;
        var seasonalVar = CalculateStdDev(seasonal.ToList());
        var residualVar = CalculateStdDev(residual.ToList());
        return residualVar > 0 ? seasonalVar / (seasonalVar + residualVar) : 0;
    }

    private double Percentile(IList<double> values, double percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    #endregion
}
