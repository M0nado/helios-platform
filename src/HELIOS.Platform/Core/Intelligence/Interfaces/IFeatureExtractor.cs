using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Intelligence.Interfaces
{
    /// <summary>
    /// Interface for extracting statistical features from time-series data.
    /// </summary>
    public interface IFeatureExtractor
    {
        /// <summary>
        /// Extracts features from time-series data.
        /// </summary>
        /// <param name="dataPoints">Time-series data points.</param>
        /// <returns>Dictionary of extracted features.</returns>
        Task<Dictionary<string, double>> ExtractFeaturesAsync(List<double> dataPoints);

        /// <summary>
        /// Extracts moving average features for a window size.
        /// </summary>
        /// <param name="dataPoints">Time-series data points.</param>
        /// <param name="windowSize">Size of the moving average window.</param>
        /// <returns>Moving average values.</returns>
        Task<List<double>> ExtractMovingAverageAsync(List<double> dataPoints, int windowSize);

        /// <summary>
        /// Calculates trend slope using linear regression.
        /// </summary>
        /// <param name="dataPoints">Time-series data points.</param>
        /// <returns>Slope value indicating trend direction and magnitude.</returns>
        Task<double> CalculateTrendSlopeAsync(List<double> dataPoints);

        /// <summary>
        /// Extracts seasonal components from data.
        /// </summary>
        /// <param name="dataPoints">Time-series data points.</param>
        /// <param name="seasonalityPeriod">Expected seasonality period.</param>
        /// <returns>Dictionary of seasonal features.</returns>
        Task<Dictionary<string, double>> ExtractSeasonalComponentsAsync(List<double> dataPoints, int seasonalityPeriod);
    }
}
