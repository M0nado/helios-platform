namespace MonadoBlade.Core.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MonadoBlade.Core.Logging;

/// <summary>
/// Registers core infrastructure services into the dependency injection container.
/// </summary>
public static class CoreServiceExtensions
{
    /// <summary>
    /// Registers all core infrastructure services.
    /// </summary>
    public static IServiceCollection AddCoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? applicationName = null)
    {
        // Logging setup
        services.AddLogging(builder =>
        {
            builder.AddSerilog(applicationName);
        });

        // Configuration
        services.AddSingleton(configuration);

        return services;
    }
}
