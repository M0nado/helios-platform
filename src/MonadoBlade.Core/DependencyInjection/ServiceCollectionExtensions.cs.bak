namespace MonadoBlade.Core.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using MonadoBlade.Core.Data;
using MonadoBlade.Core.Services;

/// <summary>
/// Dependency injection extensions for configuring the service layer.
/// Provides convenient registration methods for all segregated services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all segregated service interfaces to the dependency container.
    /// </summary>
    /// <remarks>
    /// Registers the following services with their implementations:
    /// - IQueryService: Read-only query operations
    /// - IMutationService: Write operations with cache invalidation
    /// - ISubscribeService: Real-time updates via SignalR
    /// - IManageService: Administrative operations
    /// - IDashboardService: Metrics and analytics
    /// - ISettingsService: Configuration management
    /// - IDataAccessLayer: Optimized data access with connection pooling
    /// </remarks>
    /// <param name="services">The dependency injection container.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMonadoBladeServices(this IServiceCollection services)
    {
        // Register segregated service interfaces
        services.AddScoped<IQueryService, QueryService>();
        services.AddScoped<IMutationService, MutationService>();
        services.AddScoped<ISubscribeService, SubscribeService>();
        services.AddScoped<IManageService, ManageService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISettingsService, SettingsService>();

        // Register data access layer with connection pooling (max 20 connections)
        services.AddScoped<IDataAccessLayer>(sp =>
        {
            var dbContext = sp.GetRequiredService<DbContext>();
            return new DataAccessLayer(dbContext, maxPoolSize: 20);
        });

        return services;
    }

    /// <summary>
    /// Adds individual service interface registrations for fine-grained control.
    /// </summary>
    /// <param name="services">The dependency injection container.</param>
    /// <param name="queryService">Optional custom IQueryService implementation.</param>
    /// <param name="mutationService">Optional custom IMutationService implementation.</param>
    /// <param name="subscribeService">Optional custom ISubscribeService implementation.</param>
    /// <param name="manageService">Optional custom IManageService implementation.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceLayer(
        this IServiceCollection services,
        Type? queryService = null,
        Type? mutationService = null,
        Type? subscribeService = null,
        Type? manageService = null)
    {
        if (queryService != null)
            services.AddScoped(typeof(IQueryService), queryService);
        else
            services.AddScoped<IQueryService, QueryService>();

        if (mutationService != null)
            services.AddScoped(typeof(IMutationService), mutationService);
        else
            services.AddScoped<IMutationService, MutationService>();

        if (subscribeService != null)
            services.AddScoped(typeof(ISubscribeService), subscribeService);
        else
            services.AddScoped<ISubscribeService, SubscribeService>();

        if (manageService != null)
            services.AddScoped(typeof(IManageService), manageService);
        else
            services.AddScoped<IManageService, ManageService>();

        return services;
    }
}

/// <summary>
/// Configuration class for service layer options.
/// </summary>
public class ServiceLayerOptions
{
    /// <summary>Gets or sets the maximum connection pool size.</summary>
    public int MaxConnectionPoolSize { get; set; } = 20;

    /// <summary>Gets or sets the query cache duration.</summary>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets whether to enable query performance tracking.</summary>
    public bool EnablePerformanceTracking { get; set; } = true;

    /// <summary>Gets or sets the slow query threshold in milliseconds.</summary>
    public int SlowQueryThresholdMs { get; set; } = 100;

    /// <summary>Gets or sets whether to enable audit logging.</summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>Gets or sets the transaction timeout.</summary>
    public TimeSpan TransactionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
