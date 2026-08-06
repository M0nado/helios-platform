using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Intelligence.Interfaces;
using Contracts = HELIOS.Platform.Core.Intelligence.Interfaces;

namespace HELIOS.Platform.Core.Intelligence
{
    /// <summary>
    /// Extension methods for registering ML Intelligence services in dependency injection container.
    /// </summary>
    public static class IntelligenceServiceExtensions
    {
        /// <summary>
        /// Registers all ML Intelligence services with the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMLIntelligenceServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register services as singletons for consistent state
            services.AddSingleton<Contracts.IDataCollector>(sp =>
                new DataCollector(sp.GetRequiredService<ILogger<DataCollector>>()));

            services.AddSingleton<Contracts.IDataNormalizer>(sp =>
                new DataNormalizer(sp.GetRequiredService<ILogger<DataNormalizer>>()));

            services.AddSingleton<Contracts.IFeatureExtractor>(sp =>
                new FeatureExtractor(sp.GetRequiredService<ILogger<FeatureExtractor>>()));

            services.AddSingleton<Contracts.ITimeSeriesDB>(sp =>
                new InMemoryTimeSeriesDB(sp.GetRequiredService<ILogger<InMemoryTimeSeriesDB>>()));

            services.AddSingleton<Contracts.IAnomalyDetector>(sp =>
                new AnomalyDetector(sp.GetRequiredService<ILogger<AnomalyDetector>>()));

            services.AddSingleton<Contracts.IPredictiveAnalytics>(sp =>
                new PredictiveAnalytics(sp.GetRequiredService<ILogger<PredictiveAnalytics>>()));

            services.AddSingleton<Contracts.IMLModelManager>(sp =>
                new MLModelManager(sp.GetRequiredService<ILogger<MLModelManager>>()));

            return services;
        }

        /// <summary>
        /// Registers ML Intelligence services with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMLIntelligenceServices(
            this IServiceCollection services,
            Action<MLIntelligenceOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new MLIntelligenceOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            return services.AddMLIntelligenceServices();
        }
    }

    /// <summary>
    /// Configuration options for ML Intelligence services.
    /// </summary>
    public class MLIntelligenceOptions
    {
        /// <summary>
        /// Gets or sets the maximum history size for data normalization.
        /// </summary>
        public int MaxNormalizationHistorySize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum number of data points per time series.
        /// </summary>
        public int MaxTimeSeriesPoints { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the default anomaly detection sensitivity (1-10).
        /// </summary>
        public int DefaultAnomalySensitivity { get; set; } = 5;

        /// <summary>
        /// Gets or sets the default confidence level for predictions (0.0-1.0).
        /// </summary>
        public double DefaultConfidenceLevel { get; set; } = 0.95;

        /// <summary>
        /// Gets or sets whether to enable caching for predictions.
        /// </summary>
        public bool EnablePredictionCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the cache TTL for predictions in seconds.
        /// </summary>
        public int PredictionCacheTtlSeconds { get; set; } = 300; // 5 minutes
    }
}
