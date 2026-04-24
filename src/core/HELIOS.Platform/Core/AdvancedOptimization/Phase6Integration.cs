using HELIOS.Platform.Core.AdvancedOptimization;
using LoggerInterface = HELIOS.Platform.Core.Logging.ILogger;

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
            logger?.Info("═══════════════════════════════════════════════════════════════");
            logger?.Info("PHASE 6: ADVANCED OPTIMIZATION & INTELLIGENT SYSTEMS");
            logger?.Info("═══════════════════════════════════════════════════════════════");

            try
            {
                // 1. Advanced Optimization Engine
                var optimizationEngine = new AdvancedOptimizationEngine(logger);
                ServiceContainer.Instance.RegisterSingleton<IAdvancedOptimizationEngine>(optimizationEngine);
                logger?.Info("✓ Advanced Optimization Engine registered");

                // 2. Intelligent Resource Allocator
                var resourceAllocator = new IntelligentResourceAllocator(logger);
                ServiceContainer.Instance.RegisterSingleton<IIntelligentResourceAllocator>(resourceAllocator);
                logger?.Info("✓ Intelligent Resource Allocator registered");

                // 3. Anomaly Prediction Engine
                var anomalyEngine = new AnomalyPredictionEngine(logger);
                ServiceContainer.Instance.RegisterSingleton<IAnomalyPredictionEngine>(anomalyEngine);
                logger?.Info("✓ Anomaly Prediction Engine registered");

                // 4. Service Mesh Optimizer
                var meshOptimizer = new ServiceMeshOptimizer(logger);
                ServiceContainer.Instance.RegisterSingleton<IServiceMeshOptimizer>(meshOptimizer);
                logger?.Info("✓ Service Mesh Optimizer registered");

                // 5. Security Threat Analyzer
                var threatAnalyzer = new SecurityThreatAnalyzer(logger);
                ServiceContainer.Instance.RegisterSingleton<ISecurityThreatAnalyzer>(threatAnalyzer);
                logger?.Info("✓ Security Threat Analyzer registered");

                // 6. Data Compression Engine
                var compressionEngine = new DataCompressionEngine(logger);
                ServiceContainer.Instance.RegisterSingleton<IDataCompressionEngine>(compressionEngine);
                logger?.Info("✓ Data Compression Engine registered");

                // 7. Performance Predictor AI
                var performancePredictor = new PerformancePredictorAI(logger);
                ServiceContainer.Instance.RegisterSingleton<IPerformancePredictorAI>(performancePredictor);
                logger?.Info("✓ Performance Predictor AI registered");

                // 8. Complex Event Processor
                var eventProcessor = new ComplexEventProcessor(logger);
                ServiceContainer.Instance.RegisterSingleton<IComplexEventProcessor>(eventProcessor);
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
                var optimization = ServiceContainer.Instance.GetService<IAdvancedOptimizationEngine>();
                var resources = ServiceContainer.Instance.GetService<IIntelligentResourceAllocator>();
                var anomaly = ServiceContainer.Instance.GetService<IAnomalyPredictionEngine>();
                var mesh = ServiceContainer.Instance.GetService<IServiceMeshOptimizer>();
                var threat = ServiceContainer.Instance.GetService<ISecurityThreatAnalyzer>();
                var compression = ServiceContainer.Instance.GetService<IDataCompressionEngine>();
                var performance = ServiceContainer.Instance.GetService<IPerformancePredictorAI>();
                var events = ServiceContainer.Instance.GetService<IComplexEventProcessor>();

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
                status.OptimizationEngineActive = ServiceContainer.Instance.GetService<IAdvancedOptimizationEngine>() != null;
                status.ResourceAllocatorActive = ServiceContainer.Instance.GetService<IIntelligentResourceAllocator>() != null;
                status.AnomalyEngineActive = ServiceContainer.Instance.GetService<IAnomalyPredictionEngine>() != null;
                status.MeshOptimizerActive = ServiceContainer.Instance.GetService<IServiceMeshOptimizer>() != null;
                status.ThreatAnalyzerActive = ServiceContainer.Instance.GetService<ISecurityThreatAnalyzer>() != null;
                status.CompressionEngineActive = ServiceContainer.Instance.GetService<IDataCompressionEngine>() != null;
                status.PerformancePredictorActive = ServiceContainer.Instance.GetService<IPerformancePredictorAI>() != null;
                status.EventProcessorActive = ServiceContainer.Instance.GetService<IComplexEventProcessor>() != null;
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
