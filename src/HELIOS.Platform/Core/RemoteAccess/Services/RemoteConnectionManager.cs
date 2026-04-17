using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.RemoteAccess.Services;

/// <summary>
/// Manages remote connections and their lifecycle.
/// </summary>
public interface IRemoteConnectionManager
{
    /// <summary>Creates a new remote connection.</summary>
    Task<string> CreateConnectionAsync(RemoteConnectionInfo connectionInfo, CancellationToken cancellationToken = default);

    /// <summary>Establishes a connection.</summary>
    Task<bool> ConnectAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>Disconnects an existing connection.</summary>
    Task<bool> DisconnectAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>Gets connection information.</summary>
    Task<RemoteConnectionInfo?> GetConnectionAsync(string connectionId);

    /// <summary>Lists all active connections.</summary>
    Task<IEnumerable<RemoteConnectionInfo>> ListConnectionsAsync();

    /// <summary>Validates a connection.</summary>
    Task<bool> ValidateConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>Gets connection status.</summary>
    Task<ConnectionState> GetConnectionStatusAsync(string connectionId);

    /// <summary>Removes a connection.</summary>
    Task<bool> RemoveConnectionAsync(string connectionId);

    /// <summary>Updates connection credentials.</summary>
    Task<bool> UpdateCredentialsAsync(string connectionId, EncryptedCredentials credentials);
}

/// <summary>
/// Default implementation of IRemoteConnectionManager.
/// </summary>
public class RemoteConnectionManager : IRemoteConnectionManager
{
    private readonly ConcurrentDictionary<string, RemoteConnectionInfo> _connections;
    private readonly ILogger<RemoteConnectionManager> _logger;
    private readonly ICryptographyService _cryptographyService;
    private readonly IConnectionTransport _transport;

    public RemoteConnectionManager(
        ILogger<RemoteConnectionManager> logger,
        ICryptographyService cryptographyService,
        IConnectionTransport transport)
    {
        _logger = logger;
        _cryptographyService = cryptographyService;
        _transport = transport;
        _connections = new ConcurrentDictionary<string, RemoteConnectionInfo>();
    }

    /// <inheritdoc/>
    public async Task<string> CreateConnectionAsync(RemoteConnectionInfo connectionInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating remote connection to {Host}:{Port} using {Protocol}",
                connectionInfo.RemoteHost, connectionInfo.RemotePort, connectionInfo.Protocol);

            // Validate connection info
            if (string.IsNullOrWhiteSpace(connectionInfo.RemoteHost))
                throw new ArgumentException("Remote host cannot be empty");

            if (connectionInfo.RemotePort <= 0 || connectionInfo.RemotePort > 65535)
                throw new ArgumentException("Invalid port number");

            // Encrypt sensitive data
            if (connectionInfo.Credentials != null)
            {
                await EncryptCredentialsAsync(connectionInfo.Credentials, cancellationToken);
            }

            // Add to collection
            if (!_connections.TryAdd(connectionInfo.ConnectionId, connectionInfo))
            {
                throw new InvalidOperationException("Failed to add connection to collection");
            }

            _logger.LogInformation("Remote connection created successfully with ID {ConnectionId}",
                connectionInfo.ConnectionId);

            return connectionInfo.ConnectionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating remote connection");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                _logger.LogWarning("Connection {ConnectionId} not found", connectionId);
                return false;
            }

            connection.State = ConnectionState.Connecting;
            _logger.LogInformation("Attempting to connect {ConnectionId}", connectionId);

            // Establish transport connection
            bool connected = await _transport.ConnectAsync(connection, cancellationToken);

            if (connected)
            {
                connection.State = ConnectionState.Connected;
                connection.LastActivityAt = DateTime.UtcNow;
                _logger.LogInformation("Connection {ConnectionId} established successfully", connectionId);
            }
            else
            {
                connection.State = ConnectionState.Error;
                _logger.LogWarning("Failed to establish connection {ConnectionId}", connectionId);
            }

            return connected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to {ConnectionId}", connectionId);
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                connection.State = ConnectionState.Error;
            }
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DisconnectAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                return false;
            }

            _logger.LogInformation("Disconnecting {ConnectionId}", connectionId);

            bool disconnected = await _transport.DisconnectAsync(connectionId, cancellationToken);

            if (disconnected)
            {
                connection.State = ConnectionState.Disconnected;
                _logger.LogInformation("Connection {ConnectionId} disconnected successfully", connectionId);
            }

            return disconnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting {ConnectionId}", connectionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<RemoteConnectionInfo?> GetConnectionAsync(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            // Check for timeout
            if (DateTime.UtcNow - connection.LastActivityAt > TimeSpan.FromSeconds(connection.TimeoutSeconds))
            {
                connection.State = ConnectionState.Suspended;
                _logger.LogWarning("Connection {ConnectionId} timed out", connectionId);
            }

            return connection;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteConnectionInfo>> ListConnectionsAsync()
    {
        // Validate all connections and update states
        foreach (var connection in _connections.Values)
        {
            if (connection.State == ConnectionState.Connected)
            {
                if (DateTime.UtcNow - connection.LastActivityAt > TimeSpan.FromSeconds(connection.TimeoutSeconds))
                {
                    connection.State = ConnectionState.Suspended;
                    _logger.LogWarning("Connection {ConnectionId} marked as suspended due to timeout", connection.ConnectionId);
                }
            }
        }

        return _connections.Values.ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                return false;
            }

            if (connection.State != ConnectionState.Connected)
            {
                return false;
            }

            // Perform connection health check
            bool isHealthy = await _transport.HealthCheckAsync(connectionId, cancellationToken);

            if (!isHealthy)
            {
                connection.State = ConnectionState.Error;
                _logger.LogWarning("Connection {ConnectionId} failed health check", connectionId);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating connection {ConnectionId}", connectionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ConnectionState> GetConnectionStatusAsync(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            return connection.State;
        }

        return ConnectionState.Disconnected;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveConnectionAsync(string connectionId)
    {
        try
        {
            // Disconnect first
            await DisconnectAsync(connectionId);

            // Remove from collection
            if (_connections.TryRemove(connectionId, out _))
            {
                _logger.LogInformation("Connection {ConnectionId} removed", connectionId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {ConnectionId}", connectionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateCredentialsAsync(string connectionId, EncryptedCredentials credentials)
    {
        try
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                return false;
            }

            await EncryptCredentialsAsync(credentials);
            connection.Credentials = credentials;

            _logger.LogInformation("Credentials updated for connection {ConnectionId}", connectionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credentials for connection {ConnectionId}", connectionId);
            return false;
        }
    }

    private async Task EncryptCredentialsAsync(EncryptedCredentials credentials, CancellationToken cancellationToken = default)
    {
        // Credentials are already encrypted in most cases
        // This is a placeholder for additional encryption steps if needed
        await Task.CompletedTask;
    }
}

/// <summary>
/// Interface for connection transport implementations.
/// </summary>
public interface IConnectionTransport
{
    /// <summary>Connects to a remote host.</summary>
    Task<bool> ConnectAsync(RemoteConnectionInfo connection, CancellationToken cancellationToken = default);

    /// <summary>Disconnects from a remote host.</summary>
    Task<bool> DisconnectAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>Performs a health check on the connection.</summary>
    Task<bool> HealthCheckAsync(string connectionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for cryptography operations.
/// </summary>
public interface ICryptographyService
{
    /// <summary>Encrypts sensitive data.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts sensitive data.</summary>
    string Decrypt(string ciphertext);
}
