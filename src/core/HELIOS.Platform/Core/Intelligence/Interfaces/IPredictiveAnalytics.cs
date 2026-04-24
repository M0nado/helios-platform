using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Intelligence.Interfaces
{
    /// <summary>
    /// Interface for predictive analytics and trend forecasting.
    /// </summary>
    public interface IPredictiveAnalytics
    {
        /// <summary>
        /// Predicts future values using trend analysis.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="periodsAhead">Number of periods to forecast ahead.</param>
        /// <returns>List of predicted values.</returns>
        Task<List<double>> PredictTrendAsync(string seriesName, int periodsAhead);

        /// <summary>
        /// Calculates confidence intervals for predictions.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="periodsAhead">Number of periods to forecast ahead.</param>
        /// <param name="confidenceLevel">Confidence level (0.0-1.0, typically 0.95).</param>
        /// <returns>List of confidence intervals as tuples (lower, upper).</returns>
        Task<List<(double, double)>> GetPredictionConfidenceIntervalsAsync(string seriesName, int periodsAhead, double confidenceLevel = 0.95);

        /// <summary>
        /// Forecasts peak values for resource planning.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="lookAheadPeriod">Time period to look ahead (in hours).</param>
        /// <returns>Predicted peak value and time.</returns>
        Task<(double peakValue, DateTime peakTime)> ForecastPeakAsync(string seriesName, int lookAheadPeriod = 24);

        /// <summary>
        /// Predicts if a metric will exceed a threshold soon.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="threshold">Threshold value.</param>
        /// <param name="lookAheadPeriods">Number of periods to check ahead.</param>
        /// <returns>Probability of threshold breach (0-1).</returns>
        Task<double> PredictThresholdBreachAsync(string seriesName, double threshold, int lookAheadPeriods);
    }
}
