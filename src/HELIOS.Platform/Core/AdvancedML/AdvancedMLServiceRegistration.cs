using HELIOS.Platform.Core.AdvancedML;
using HELIOS.Platform.Core.AdvancedML.Interfaces;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Core;

/// <summary>
/// Registers Phase 5 Tier 1 - Advanced ML Services.
/// Configures dependency injection for all 7 advanced ML services.
/// </summary>
public static class AdvancedMLServiceRegistration
{
    /// <summary>
    /// Registers all Advanced ML services in the ServiceContainer.
    /// Call this during application initialization before using any ML services.
    /// </summary>
    /// <param name="logger">Application logger instance.</param>
    /// <param name="cache">L1 cache service for caching ML predictions.</param>
    public static void RegisterAdvancedMLServices(Logging.ILogger logger, IL1CacheService cache)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cache);

        // Register all 7 advanced ML services
        var deepLearningPredictor = new DeepLearningPredictor(logger, cache);
        var autoMLOptimizer = new AutoMLOptimizer(logger, cache);
        var federatedLearning = new FederatedLearning(logger, cache);
        var reinforcementLearning = new ReinforcementLearning(logger, cache);
        var nlpAnalyzer = new NLPAnalyzer(logger, cache);
        var seasonalityDetector = new SeasonalityDetector(logger, cache);
        var anomalyPrediction = new AnomalyPrediction(logger, cache);

        // Register in ServiceContainer
        ServiceContainer.Instance.RegisterSingleton<IDeepLearningPredictor>(deepLearningPredictor);
        ServiceContainer.Instance.RegisterSingleton<IAutoMLOptimizer>(autoMLOptimizer);
        ServiceContainer.Instance.RegisterSingleton<IFederatedLearning>(federatedLearning);
        ServiceContainer.Instance.RegisterSingleton<IReinforcementLearning>(reinforcementLearning);
        ServiceContainer.Instance.RegisterSingleton<INLPAnalyzer>(nlpAnalyzer);
        ServiceContainer.Instance.RegisterSingleton<ISeasonalityDetector>(seasonalityDetector);
        ServiceContainer.Instance.RegisterSingleton<IAnomalyPrediction>(anomalyPrediction);

        logger.Info("Phase 5 Tier 1 Advanced ML Services registered successfully");
        logger.Info($"  ✓ DeepLearningPredictor");
        logger.Info($"  ✓ AutoMLOptimizer");
        logger.Info($"  ✓ FederatedLearning");
        logger.Info($"  ✓ ReinforcementLearning");
        logger.Info($"  ✓ NLPAnalyzer");
        logger.Info($"  ✓ SeasonalityDetector");
        logger.Info($"  ✓ AnomalyPrediction");
    }
}
