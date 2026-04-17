using Microsoft.Extensions.DependencyInjection;
using HELIOS.Platform.Core.RemoteAccess.Services;

namespace HELIOS.Platform.Core.RemoteAccess;

/// <summary>
/// Extension methods for registering Remote Access services with the DI container.
/// </summary>
public static class RemoteAccessServiceRegistration
{
    /// <summary>
    /// Registers all remote access services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRemoteAccessServices(this IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<IRemoteConnectionManager, RemoteConnectionManager>();
        services.AddSingleton<IRemoteExecutor, RemoteExecutor>();
        services.AddSingleton<IRemoteMonitor, RemoteMonitor>();
        services.AddSingleton<IRemoteFileTransferService, RemoteFileTransferService>();
        services.AddSingleton<IRemoteSessionManager, RemoteSessionManager>();

        // Register transport and cryptography placeholders (implement as needed)
        services.AddSingleton<IConnectionTransport, DefaultConnectionTransport>();
        services.AddSingleton<ICryptographyService, DefaultCryptographyService>();

        return services;
    }

    /// <summary>
    /// Default connection transport implementation.
    /// </summary>
    private class DefaultConnectionTransport : IConnectionTransport
    {
        private readonly ILogger<DefaultConnectionTransport> _logger;

        public DefaultConnectionTransport(ILogger<DefaultConnectionTransport> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ConnectAsync(RemoteConnectionInfo connection, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Connecting via {Protocol} to {Host}:{Port}", 
                connection.Protocol, connection.RemoteHost, connection.RemotePort);
            await Task.Delay(100, cancellationToken);
            return true;
        }

        public async Task<bool> DisconnectAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Disconnecting {ConnectionId}", connectionId);
            await Task.Delay(50, cancellationToken);
            return true;
        }

        public async Task<bool> HealthCheckAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Health check for {ConnectionId}", connectionId);
            await Task.Delay(50, cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// Default cryptography service implementation.
    /// </summary>
    private class DefaultCryptographyService : ICryptographyService
    {
        public string Encrypt(string plaintext)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
        }

        public string Decrypt(string ciphertext)
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
        }
    }
}
