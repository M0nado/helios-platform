using System;
using Microsoft.Extensions.DependencyInjection;
using HELIOS.Platform.Core.Ecosystem;
using HELIOS.Platform.Core.Ecosystem.Interfaces;

namespace HELIOS.Platform.Core.Container
{
    /// <summary>
    /// Service container for registering HELIOS Platform ecosystem services.
    /// Provides dependency injection configuration for all ecosystem components.
    /// </summary>
    public static class ServiceContainer
    {
        /// <summary>
        /// Registers all ecosystem services in the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to register services with</param>
        /// <returns>The modified service collection</returns>
        public static IServiceCollection AddEcosystemServices(this IServiceCollection services)
        {
            // Register Marketplace Integration service
            services.AddScoped<IMarketplaceIntegration, MarketplaceIntegration>();

            // Register API Marketplace service
            services.AddScoped<IAPIMarketplace, APIMarketplace>();

            // Register SLA Marketplace service
            services.AddScoped<ISLAMarketplace, SLAMarketplace>();

            // Register Partner Networking service
            services.AddScoped<IPartnerNetworking, PartnerNetworking>();

            return services;
        }

        /// <summary>
        /// Registers ecosystem services with custom configuration options.
        /// </summary>
        /// <param name="services">The service collection to register services with</param>
        /// <param name="configureOptions">Optional action to configure service options</param>
        /// <returns>The modified service collection</returns>
        public static IServiceCollection AddEcosystemServices(
            this IServiceCollection services,
            Action<EcosystemServiceOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services.AddEcosystemServices();
        }
    }

    /// <summary>
    /// Configuration options for ecosystem services.
    /// </summary>
    public class EcosystemServiceOptions
    {
        /// <summary>Gets or sets the marketplace base URL.</summary>
        public string MarketplaceBaseUrl { get; set; } = "https://marketplace.helios.local";

        /// <summary>Gets or sets whether to enable caching for marketplace queries.</summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>Gets or sets the cache duration in minutes.</summary>
        public int CacheDurationMinutes { get; set; } = 15;

        /// <summary>Gets or sets the maximum concurrent API calls allowed.</summary>
        public int MaxConcurrentCalls { get; set; } = 100;

        /// <summary>Gets or sets the request timeout in seconds.</summary>
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>Gets or sets the enable audit logging flag.</summary>
        public bool EnableAuditLogging { get; set; } = true;

        /// <summary>Gets or sets the enable metrics collection flag.</summary>
        public bool EnableMetricsCollection { get; set; } = true;
    }
}
