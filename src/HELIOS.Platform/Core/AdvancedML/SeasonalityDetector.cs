using HELIOS.Platform.Core.AdvancedML.Interfaces;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Core.AdvancedML;

/// <summary>
/// Advanced seasonality detector for time-series pattern recognition.
/// </summary>
public class SeasonalityDetector : ISeasonalityDetector
{
    private readonly Logging.ILogger _logger;
    private readonly IL1CacheService _cache;

    public SeasonalityDetector(Logging.ILogger logger, IL1CacheService cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<SeasonalPattern> DetectSeasonalityAsync(double[] timeSeries, DateTime[] timestamps)
    {
        ArgumentNullException.ThrowIfNull(timeSeries);
        ArgumentNullException.ThrowIfNull(timestamps);
        if (timeSeries.Length != timestamps.Length) throw new ArgumentException("Length mismatch");
        if (timeSeries.Length < 24) throw new ArgumentException("Need at least 24 data points");

        try
        {
            var cacheKey = $"seasonality_{GetDataHash(timeSeries)}";
            return await _cache.GetAsync(cacheKey,
                async () => await ComputeSeasonalityAsync(timeSeries, timestamps),
                TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DetectSeasonalityAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<double[]> DeseasonalizeAsync(double[] timeSeries, SeasonalPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(timeSeries);
        ArgumentNullException.ThrowIfNull(pattern);

        try
        {
            await Task.CompletedTask;

            var deseasonalized = new double[timeSeries.Length];
            for (int i = 0; i < timeSeries.Length; i++)
            {
                int seasonalIndex = i % pattern.Period;
                if (pattern.SeasonalityType == "additive")
                    deseasonalized[i] = timeSeries[i] - pattern.SeasonalIndices[seasonalIndex];
                else
                    deseasonalized[i] = timeSeries[i] / (pattern.SeasonalIndices[seasonalIndex] + 0.0001);
            }

            return deseasonalized;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DeseasonalizeAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<double[]> ForecastWithSeasonalityAsync(double[] timeSeries, int stepsAhead, SeasonalPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(timeSeries);
        ArgumentNullException.ThrowIfNull(pattern);
        if (stepsAhead < 1) throw new ArgumentException("Steps ahead must be positive");

        try
        {
            await Task.CompletedTask;

            var forecast = new double[stepsAhead];
            var trend = ComputeTrend(timeSeries);

            for (int i = 0; i < stepsAhead; i++)
            {
                int seasonalIndex = (timeSeries.Length + i) % pattern.Period;
                double trendValue = trend + (i * 0.01); // Simple linear trend

                if (pattern.SeasonalityType == "additive")
                    forecast[i] = trendValue + pattern.SeasonalIndices[seasonalIndex];
                else
                    forecast[i] = trendValue * pattern.SeasonalIndices[seasonalIndex];
            }

            return forecast;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in ForecastWithSeasonalityAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<SeasonalPeriod>> DetectMultiplePeriodsAsync(double[] timeSeries, DateTime[] timestamps)
    {
        ArgumentNullException.ThrowIfNull(timeSeries);
        ArgumentNullException.ThrowIfNull(timestamps);

        try
        {
            await Task.CompletedTask;

            var periods = new List<SeasonalPeriod>();
            var possiblePeriods = new[] { 24, 7, 30, 365 }; // hourly, daily, monthly, yearly

            for (int i = 0; i < possiblePeriods.Length; i++)
            {
                int period = possiblePeriods[i];
                if (period >= timeSeries.Length) continue;

                var strength = ComputeAutoCorrelationAtLag(timeSeries, period);
                if (strength > 0.4)
                {
                    periods.Add(new SeasonalPeriod
                    {
                        Period = period,
                        PeriodDescription = GetPeriodDescription(period),
                        Strength = strength,
                        Autocorrelation = strength,
                        Pattern = ExtractSeasonalPattern(timeSeries, period),
                        Rank = i + 1
                    });
                }
            }

            // Sort by strength
            periods.Sort((a, b) => b.Strength.CompareTo(a.Strength));
            for (int i = 0; i < periods.Count; i++)
                periods[i].Rank = i + 1;

            return periods;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DetectMultiplePeriodsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<Interfaces.SeasonalDecomposition> DecomposeAsync(double[] timeSeries)
    {
        ArgumentNullException.ThrowIfNull(timeSeries);
        if (timeSeries.Length < 2) throw new ArgumentException("Need at least 2 data points");

        try
        {
            await Task.CompletedTask;

            var trend = ComputeTrendComponent(timeSeries);
            var seasonal = new double[timeSeries.Length];
            var residual = new double[timeSeries.Length];

            for (int i = 0; i < timeSeries.Length; i++)
            {
                seasonal[i] = timeSeries[i] - trend[i];
                residual[i] = timeSeries[i] - trend[i] - seasonal[i];
            }

            // Calculate variance ratios
            var seasonalVar = seasonal.Sum(x => x * x) / timeSeries.Length;
            var trendVar = trend.Sum(x => x * x) / timeSeries.Length;
            var residualVar = residual.Sum(x => x * x) / timeSeries.Length;
            var totalVar = seasonalVar + trendVar + residualVar + 0.0001;

            var decomposition = new Interfaces.SeasonalDecomposition
            {
                Original = timeSeries,
                Trend = trend,
                Seasonal = seasonal,
                Residual = residual,
                Method = "Classical",
                SeasonalVarianceRatio = seasonalVar / totalVar,
                TrendVarianceRatio = trendVar / totalVar,
                ResidualVarianceRatio = residualVar / totalVar,
                DecompositionQuality = 1.0 - (residualVar / totalVar),
                DecomposedAt = DateTime.UtcNow
            };

            return decomposition;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DecomposeAsync: {ex.Message}");
            throw;
        }
    }

    private async Task<SeasonalPattern> ComputeSeasonalityAsync(double[] timeSeries, DateTime[] timestamps)
    {
        await Task.CompletedTask;

        // Try common periods
        int detectedPeriod = 24;
        double maxAutocorrelation = 0;

        foreach (var period in new[] { 24, 7, 30, 365 })
        {
            if (period >= timeSeries.Length) continue;
            var acf = ComputeAutoCorrelationAtLag(timeSeries, period);
            if (acf > maxAutocorrelation)
            {
                maxAutocorrelation = acf;
                detectedPeriod = period;
            }
        }

        var seasonalIndices = ExtractSeasonalPattern(timeSeries, detectedPeriod);

        return new SeasonalPattern
        {
            Period = detectedPeriod,
            Strength = maxAutocorrelation,
            SeasonalIndices = seasonalIndices,
            SeasonalFactors = seasonalIndices,
            SeasonalityType = "additive",
            SeasonalAutocorrelation = maxAutocorrelation,
            Confidence = Math.Min(0.95, 0.5 + maxAutocorrelation),
            DetectedAt = DateTime.UtcNow
        };
    }

    private double ComputeAutoCorrelationAtLag(double[] timeSeries, int lag)
    {
        if (lag >= timeSeries.Length) return 0;

        double mean = timeSeries.Average();
        double c0 = timeSeries.Select(x => (x - mean) * (x - mean)).Average();
        double ck = 0;

        for (int i = lag; i < timeSeries.Length; i++)
        {
            ck += (timeSeries[i] - mean) * (timeSeries[i - lag] - mean);
        }
        ck /= timeSeries.Length;

        return c0 > 0 ? ck / c0 : 0;
    }

    private double[] ExtractSeasonalPattern(double[] timeSeries, int period)
    {
        var pattern = new double[period];
        var counts = new int[period];

        for (int i = 0; i < timeSeries.Length; i++)
        {
            pattern[i % period] += timeSeries[i];
            counts[i % period]++;
        }

        for (int i = 0; i < period; i++)
            pattern[i] = counts[i] > 0 ? pattern[i] / counts[i] : 0;

        return pattern;
    }

    private double[] ComputeTrendComponent(double[] timeSeries)
    {
        var trend = new double[timeSeries.Length];
        int windowSize = Math.Max(3, timeSeries.Length / 10);

        for (int i = 0; i < timeSeries.Length; i++)
        {
            int start = Math.Max(0, i - windowSize / 2);
            int end = Math.Min(timeSeries.Length, i + windowSize / 2 + 1);
            trend[i] = timeSeries[start..end].Average();
        }

        return trend;
    }

    private double ComputeTrend(double[] timeSeries)
    {
        if (timeSeries.Length < 2) return timeSeries[0];

        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = timeSeries.Length;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += timeSeries[i];
            sumXY += i * timeSeries[i];
            sumX2 += i * i;
        }

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }

    private static string GetPeriodDescription(int period) =>
        period switch
        {
            24 => "Daily",
            7 => "Weekly",
            30 => "Monthly",
            365 => "Yearly",
            _ => $"{period} periods"
        };

    private static string GetDataHash(double[] data) =>
        $"{(data.Length > 0 ? data[0] : 0):F2}_{data.Length}".GetHashCode().ToString();
}
