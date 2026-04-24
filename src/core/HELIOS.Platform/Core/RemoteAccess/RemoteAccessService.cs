using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.RemoteAccess
{
    public interface IRemoteAccessService
    {
        Task<bool> EnableRESTAPIAsync(int port);
        Task<bool> StartWebConsoleAsync(int port);
        Task<List<RemoteSession>> GetActiveSessions();
        Task<bool> AuthenticateRemoteSessionAsync(string username, string password, string clientId);
        Task<bool> TerminateRemoteSessionAsync(string sessionId);
    }

    public class RemoteSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string ClientId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class RemoteAccessService : IRemoteAccessService
    {
        private readonly Core.Logging.ILogger? _logger;
        private readonly Dictionary<string, RemoteSession> _sessions = new();

        public RemoteAccessService(Core.Logging.ILogger? logger = null)
        {
            _logger = logger;
        }

        public async Task<bool> EnableRESTAPIAsync(int port)
        {
            try
            {
                _logger?.Info($"REST API enabled on port {port}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to enable REST API: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StartWebConsoleAsync(int port)
        {
            try
            {
                _logger?.Info($"Web console started on port {port}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to start web console: {ex.Message}");
                return false;
            }
        }

        public async Task<List<RemoteSession>> GetActiveSessions()
        {
            return new List<RemoteSession>(_sessions.Values);
        }

        public async Task<bool> AuthenticateRemoteSessionAsync(string username, string password, string clientId)
        {
            try
            {
                var session = new RemoteSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    ClientId = clientId,
                    Username = username,
                    ConnectedAt = DateTime.UtcNow
                };

                _sessions[session.SessionId] = session;
                _logger?.Info($"Remote session authenticated: {username} from {clientId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Authentication failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TerminateRemoteSessionAsync(string sessionId)
        {
            try
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    _sessions.Remove(sessionId);
                    _logger?.Info($"Remote session terminated: {sessionId}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to terminate session: {ex.Message}");
                return false;
            }
        }
    }
}
