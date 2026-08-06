using HELIOS.Platform.Core.AdvancedOptimization;
using Microsoft.Extensions.Logging;
using LoggerInterface = HELIOS.Platform.Core.Logging.ILogger;
using Contracts = HELIOS.Platform.Core.AdvancedOptimization.Interfaces;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace HELIOS.Platform.Core
{
    /// <summary>
    /// Phase 6 Advanced Optimization & Intelligent Systems Integration
    /// Initializes all 8 advanced AI and optimization services.
    /// </summary>
    public static class Phase6Integration
    {
        /// <summary>
        /// Initialize all Phase 6 services and register them in the ServiceContainer.
        /// Should be called during application startup after basic services are initialized.
        /// </summary>
        public static void InitializePhase6Services(LoggerInterface? logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            logger?.Info("═══════════════════════════════════════════════════════════════");
            logger?.Info("PHASE 6: ADVANCED OPTIMIZATION & INTELLIGENT SYSTEMS");
            logger?.Info("═══════════════════════════════════════════════════════════════");

            try
            {
                // 1. Advanced Optimization Engine
                var optimizationEngine = new AdvancedOptimizationEngine(
                    new Phase6LoggerAdapter<AdvancedOptimizationEngine>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.IAdvancedOptimizationEngine>(optimizationEngine);
                logger?.Info("✓ Advanced Optimization Engine registered");

                // 2. Intelligent Resource Allocator
                var resourceAllocator = new IntelligentResourceAllocator(
                    new Phase6LoggerAdapter<IntelligentResourceAllocator>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.IIntelligentResourceAllocator>(resourceAllocator);
                logger?.Info("✓ Intelligent Resource Allocator registered");

                // 3. Anomaly Prediction Engine
                var anomalyEngine = new AnomalyPredictionEngine(
                    new Phase6LoggerAdapter<AnomalyPredictionEngine>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.IAnomalyPredictionEngine>(anomalyEngine);
                logger?.Info("✓ Anomaly Prediction Engine registered");

                // 4. Service Mesh Optimizer
                var meshOptimizer = new ServiceMeshOptimizer(
                    new Phase6LoggerAdapter<ServiceMeshOptimizer>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.IServiceMeshOptimizer>(meshOptimizer);
                logger?.Info("✓ Service Mesh Optimizer registered");

                // 5. Security Threat Analyzer
                var threatAnalyzer = new SecurityThreatAnalyzer(
                    new Phase6LoggerAdapter<SecurityThreatAnalyzer>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.ISecurityThreatAnalyzer>(threatAnalyzer);
                logger?.Info("✓ Security Threat Analyzer registered");

                // 6. Data Compression Engine
                var compressionEngine = new DataCompressionEngine(
                    new Phase6LoggerAdapter<DataCompressionEngine>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.IDataCompressionEngine>(compressionEngine);
                logger?.Info("✓ Data Compression Engine registered");

                // 7. Performance Predictor AI
                var performancePredictor = new PerformancePredictorAI(
                    new Phase6LoggerAdapter<PerformancePredictorAI>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.IPerformancePredictorAI>(performancePredictor);
                logger?.Info("✓ Performance Predictor AI registered");

                // 8. Complex Event Processor
                var eventProcessor = new ComplexEventProcessor(
                    new Phase6LoggerAdapter<ComplexEventProcessor>(logger));
                ServiceContainer.Instance.RegisterSingleton<Contracts.IComplexEventProcessor>(eventProcessor);
                logger?.Info("✓ Complex Event Processor registered");

                logger?.Info("═══════════════════════════════════════════════════════════════");
                logger?.Info("Phase 6: All 8 advanced services initialized successfully");
                logger?.Info("═══════════════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                logger?.Error($"Phase 6 initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Async initialization with startup tasks for Phase 6 services.
        /// </summary>
        public static async System.Threading.Tasks.Task InitializePhase6ServicesAsync(LoggerInterface? logger)
        {
            logger?.Info("Starting Phase 6 async initialization...");

            try
            {
                var optimization = ServiceContainer.Instance.GetService<Contracts.IAdvancedOptimizationEngine>();
                var resources = ServiceContainer.Instance.GetService<Contracts.IIntelligentResourceAllocator>();
                var anomaly = ServiceContainer.Instance.GetService<Contracts.IAnomalyPredictionEngine>();
                var mesh = ServiceContainer.Instance.GetService<Contracts.IServiceMeshOptimizer>();
                var threat = ServiceContainer.Instance.GetService<Contracts.ISecurityThreatAnalyzer>();
                var compression = ServiceContainer.Instance.GetService<Contracts.IDataCompressionEngine>();
                var performance = ServiceContainer.Instance.GetService<Contracts.IPerformancePredictorAI>();
                var events = ServiceContainer.Instance.GetService<Contracts.IComplexEventProcessor>();

                if (optimization != null && resources != null && anomaly != null && mesh != null &&
                    threat != null && compression != null && performance != null && events != null)
                {
                    await System.Threading.Tasks.Task.WhenAll(
                        optimization.InitializeAsync(),
                        resources.InitializeAsync(),
                        anomaly.InitializeAsync(),
                        mesh.InitializeAsync(),
                        threat.InitializeAsync(),
                        compression.InitializeAsync(),
                        performance.InitializeAsync(),
                        events.InitializeAsync()
                    );

                    logger?.Info("All Phase 6 services initialized asynchronously");
                }
            }
            catch (Exception ex)
            {
                logger?.Error($"Async Phase 6 initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get status of all Phase 6 services.
        /// </summary>
        public static Phase6Status GetPhase6Status(LoggerInterface? logger)
        {
            var status = new Phase6Status();

            try
            {
                status.OptimizationEngineActive = ServiceContainer.Instance.GetService<Contracts.IAdvancedOptimizationEngine>() != null;
                status.ResourceAllocatorActive = ServiceContainer.Instance.GetService<Contracts.IIntelligentResourceAllocator>() != null;
                status.AnomalyEngineActive = ServiceContainer.Instance.GetService<Contracts.IAnomalyPredictionEngine>() != null;
                status.MeshOptimizerActive = ServiceContainer.Instance.GetService<Contracts.IServiceMeshOptimizer>() != null;
                status.ThreatAnalyzerActive = ServiceContainer.Instance.GetService<Contracts.ISecurityThreatAnalyzer>() != null;
                status.CompressionEngineActive = ServiceContainer.Instance.GetService<Contracts.IDataCompressionEngine>() != null;
                status.PerformancePredictorActive = ServiceContainer.Instance.GetService<Contracts.IPerformancePredictorAI>() != null;
                status.EventProcessorActive = ServiceContainer.Instance.GetService<Contracts.IComplexEventProcessor>() != null;
                status.AllServicesActive = status.OptimizationEngineActive && status.ResourceAllocatorActive &&
                                           status.AnomalyEngineActive && status.MeshOptimizerActive &&
                                           status.ThreatAnalyzerActive && status.CompressionEngineActive &&
                                           status.PerformancePredictorActive && status.EventProcessorActive;
            }
            catch (Exception ex)
            {
                logger?.Error($"Failed to get Phase 6 status: {ex.Message}");
            }

            return status;
        }

        private sealed class Phase6LoggerAdapter<T> : Microsoft.Extensions.Logging.ILogger<T>
        {
            private readonly LoggerInterface _logger;

            public Phase6LoggerAdapter(LoggerInterface logger)
            {
                _logger = logger;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

            public bool IsEnabled(MsLogLevel logLevel) => logLevel != MsLogLevel.None;

            public void Log<TState>(
                MsLogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                switch (logLevel)
                {
                    case MsLogLevel.Trace:
                    case MsLogLevel.Debug:
                        _logger.Debug(message);
                        break;
                    case MsLogLevel.Information:
                        _logger.Info(message);
                        break;
                    case MsLogLevel.Warning:
                        _logger.Warning(message);
                        break;
                    case MsLogLevel.Error:
                        _logger.Error(message, exception);
                        break;
                    case MsLogLevel.Critical:
                        _logger.Critical(message, exception);
                        break;
                }
            }

            private sealed class NoopScope : IDisposable
            {
                public static NoopScope Instance { get; } = new();

                public void Dispose()
                {
                }
            }
        }
    }

    /// <summary>Status of Phase 6 services.</summary>
    public class Phase6Status
    {
        public bool OptimizationEngineActive { get; set; }
        public bool ResourceAllocatorActive { get; set; }
        public bool AnomalyEngineActive { get; set; }
        public bool MeshOptimizerActive { get; set; }
        public bool ThreatAnalyzerActive { get; set; }
        public bool CompressionEngineActive { get; set; }
        public bool PerformancePredictorActive { get; set; }
        public bool EventProcessorActive { get; set; }
        public bool AllServicesActive { get; set; }

        public override string ToString()
        {
            return $@"
═══ PHASE 6 SERVICE STATUS ═══
Optimization Engine:       {(OptimizationEngineActive ? "✓" : "✗")}
Resource Allocator:        {(ResourceAllocatorActive ? "✓" : "✗")}
Anomaly Engine:            {(AnomalyEngineActive ? "✓" : "✗")}
Mesh Optimizer:            {(MeshOptimizerActive ? "✓" : "✗")}
Threat Analyzer:           {(ThreatAnalyzerActive ? "✓" : "✗")}
Compression Engine:        {(CompressionEngineActive ? "✓" : "✗")}
Performance Predictor:     {(PerformancePredictorActive ? "✓" : "✗")}
Event Processor:           {(EventProcessorActive ? "✓" : "✗")}
═════════════════════════════════
Overall Status:            {(AllServicesActive ? "✓ ALL ACTIVE" : "⚠ PARTIAL")}
═════════════════════════════════";
        }
    }
}
