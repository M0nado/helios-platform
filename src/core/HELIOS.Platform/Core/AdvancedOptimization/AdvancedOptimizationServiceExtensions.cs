using Microsoft.Extensions.DependencyInjection;
using Contracts = HELIOS.Platform.Core.AdvancedOptimization.Interfaces;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Extension methods for registering Advanced Optimization services in dependency injection container.
    /// </summary>
    public static class AdvancedOptimizationServiceExtensions
    {
        /// <summary>
        /// Adds all Advanced Optimization services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddAdvancedOptimizationServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<Contracts.IAdvancedOptimizationEngine, AdvancedOptimizationEngine>();
            services.AddSingleton<Contracts.IIntelligentResourceAllocator, IntelligentResourceAllocator>();
            services.AddSingleton<Contracts.IAnomalyPredictionEngine, AnomalyPredictionEngine>();
            services.AddSingleton<Contracts.IServiceMeshOptimizer, ServiceMeshOptimizer>();
            services.AddSingleton<Contracts.ISecurityThreatAnalyzer, SecurityThreatAnalyzer>();
            services.AddSingleton<Contracts.IDataCompressionEngine, DataCompressionEngine>();
            services.AddSingleton<Contracts.IPerformancePredictorAI, PerformancePredictorAI>();
            services.AddSingleton<Contracts.IComplexEventProcessor, ComplexEventProcessor>();

            return services;
        }

        /// <summary>
        /// Initializes all Advanced Optimization services asynchronously.
        /// </summary>
        /// <param name="services">The service provider.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task InitializeAdvancedOptimizationServicesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var engine = services.GetRequiredService<Contracts.IAdvancedOptimizationEngine>();
            var allocator = services.GetRequiredService<Contracts.IIntelligentResourceAllocator>();
            var anomaly = services.GetRequiredService<Contracts.IAnomalyPredictionEngine>();
            var mesh = services.GetRequiredService<Contracts.IServiceMeshOptimizer>();
            var security = services.GetRequiredService<Contracts.ISecurityThreatAnalyzer>();
            var compression = services.GetRequiredService<Contracts.IDataCompressionEngine>();
            var performance = services.GetRequiredService<Contracts.IPerformancePredictorAI>();
            var events = services.GetRequiredService<Contracts.IComplexEventProcessor>();

            await Task.WhenAll(
                engine.InitializeAsync(cancellationToken),
                allocator.InitializeAsync(cancellationToken),
                anomaly.InitializeAsync(cancellationToken),
                mesh.InitializeAsync(cancellationToken),
                security.InitializeAsync(cancellationToken),
                compression.InitializeAsync(cancellationToken),
                performance.InitializeAsync(cancellationToken),
                events.InitializeAsync(cancellationToken)
            );
        }

        /// <summary>
        /// Starts all Advanced Optimization services asynchronously.
        /// </summary>
        /// <param name="services">The service provider.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task StartAdvancedOptimizationServicesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var engine = services.GetRequiredService<Contracts.IAdvancedOptimizationEngine>();
            var allocator = services.GetRequiredService<Contracts.IIntelligentResourceAllocator>();
            var anomaly = services.GetRequiredService<Contracts.IAnomalyPredictionEngine>();
            var mesh = services.GetRequiredService<Contracts.IServiceMeshOptimizer>();
            var security = services.GetRequiredService<Contracts.ISecurityThreatAnalyzer>();
            var compression = services.GetRequiredService<Contracts.IDataCompressionEngine>();
            var performance = services.GetRequiredService<Contracts.IPerformancePredictorAI>();
            var events = services.GetRequiredService<Contracts.IComplexEventProcessor>();

            await Task.WhenAll(
                engine.StartAsync(cancellationToken),
                allocator.StartAsync(cancellationToken),
                anomaly.StartAsync(cancellationToken),
                mesh.StartAsync(cancellationToken),
                security.StartAsync(cancellationToken),
                compression.StartAsync(cancellationToken),
                performance.StartAsync(cancellationToken),
                events.StartAsync(cancellationToken)
            );
        }

        /// <summary>
        /// Stops all Advanced Optimization services asynchronously.
        /// </summary>
        /// <param name="services">The service provider.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task StopAdvancedOptimizationServicesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var engine = services.GetRequiredService<Contracts.IAdvancedOptimizationEngine>();
            var allocator = services.GetRequiredService<Contracts.IIntelligentResourceAllocator>();
            var anomaly = services.GetRequiredService<Contracts.IAnomalyPredictionEngine>();
            var mesh = services.GetRequiredService<Contracts.IServiceMeshOptimizer>();
            var security = services.GetRequiredService<Contracts.ISecurityThreatAnalyzer>();
            var compression = services.GetRequiredService<Contracts.IDataCompressionEngine>();
            var performance = services.GetRequiredService<Contracts.IPerformancePredictorAI>();
            var events = services.GetRequiredService<Contracts.IComplexEventProcessor>();

            await Task.WhenAll(
                engine.StopAsync(cancellationToken),
                allocator.StopAsync(cancellationToken),
                anomaly.StopAsync(cancellationToken),
                mesh.StopAsync(cancellationToken),
                security.StopAsync(cancellationToken),
                compression.StopAsync(cancellationToken),
                performance.StopAsync(cancellationToken),
                events.StopAsync(cancellationToken)
            );
        }
    }
}
