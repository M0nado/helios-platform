using Microsoft.Extensions.DependencyInjection;
using HELIOS.Platform.Core.GlobalIntelligence.Interfaces;

namespace HELIOS.Platform.Core.GlobalIntelligence
{
    /// <summary>
    /// Extension methods for registering Global Intelligence services in the dependency injection container.
    /// </summary>
    public static class GlobalIntelligenceServiceExtensions
    {
        /// <summary>
        /// Registers all Global Intelligence services in the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to register services in.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddGlobalIntelligenceServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register metrics aggregation services
            services.AddSingleton<IGlobalMetricsAggregator, GlobalMetricsAggregator>();
            services.AddSingleton<ICostOptimizer, CostOptimizer>();

            // Register capacity and planning services
            services.AddSingleton<IGlobalCapacityPlanner, GlobalCapacityPlanner>();

            // Register load balancing and failover services
            services.AddSingleton<IGlobalLoadBalancer, GlobalLoadBalancer>();
            services.AddSingleton<IRegionFailover, RegionFailover>();

            // Register latency optimization services
            services.AddSingleton<ILatencyOptimizer, LatencyOptimizer>();

            // Register CDN services
            services.AddSingleton<ICDNController, CDNController>();

            return services;
        }
    }
}
