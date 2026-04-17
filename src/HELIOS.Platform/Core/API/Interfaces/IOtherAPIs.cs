using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.API.Interfaces;

/// <summary>
/// GraphQL server for flexible querying.
/// </summary>
public interface IGraphQLServer
{
    Task<string> ExecuteQueryAsync(string query, Dictionary<string, object>? variables = null);
    Task<bool> RegisterTypeAsync(string typeName, Type clrType);
    Task<bool> RegisterQueryFieldAsync(string fieldName, Func<Task<object>> resolver);
    Task<GraphQLSchema> GetSchemaAsync();
}

public class GraphQLSchema
{
    public List<GraphQLType> Types { get; set; } = new();
}

public class GraphQLType
{
    public string Name { get; set; } = string.Empty;
    public List<GraphQLField> Fields { get; set; } = new();
}

public class GraphQLField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// WebSocket broker for real-time communication.
/// </summary>
public interface IWebSocketBroker
{
    Task<bool> RegisterHandlerAsync(string topic, Func<WebSocketMessage, Task> handler);
    Task<bool> PublishAsync(string topic, WebSocketMessage message);
    Task<bool> SubscribeAsync(string clientId, string topic);
    Task<bool> UnsubscribeAsync(string clientId, string topic);
    Task<List<string>> GetSubscribersAsync(string topic);
}

public class WebSocketMessage
{
    public string Topic { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Web UI renderer for server-side rendering.
/// </summary>
public interface IWebUIServer
{
    Task<string> RenderPageAsync(string pageName, Dictionary<string, object>? model = null);
    Task<string> RenderComponentAsync(string componentName, Dictionary<string, object>? props = null);
    Task<bool> RegisterLayoutAsync(string layoutName, string html);
    Task<bool> RegisterPageAsync(string pageName, string html);
    Task<string> GetThemeAsync(string themeName);
    Task<bool> StartServerAsync(int port);
    Task<bool> StopServerAsync();
    Task<WebServerHealthStatus> GetHealthAsync();
}

/// <summary>
/// Session management for user state.
/// </summary>
public interface ISessionManager
{
    Task<string> CreateSessionAsync(string userId, Dictionary<string, object>? data = null);
    Task<SessionData> GetSessionAsync(string sessionId);
    Task<bool> UpdateSessionAsync(string sessionId, Dictionary<string, object> data);
    Task<bool> DestroySessionAsync(string sessionId);
    Task<bool> ValidateSessionAsync(string sessionId);
}

public class SessionData
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Theme engine for UI customization.
/// </summary>
public interface IThemeManager
{
    Task<string> GetThemeAsync(string themeName);
    Task<bool> RegisterThemeAsync(string themeName, ThemeDefinition theme);
    Task<List<string>> ListThemesAsync();
    Task<bool> SetDefaultThemeAsync(string themeName);
    Task<ThemeDefinition> GetCurrentThemeAsync();
    Task<bool> DeleteThemeAsync(string themeName);
    Task<ThemeMetrics> GetMetricsAsync();
}

/// <summary>
/// Web server health status.
/// </summary>
public class WebServerHealthStatus
{
    public bool IsHealthy { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public string Status { get; set; } = "Unknown";
}

/// <summary>
/// Theme metrics for monitoring.
/// </summary>
public class ThemeMetrics
{
    public int TotalThemes { get; set; }
    public string CurrentTheme { get; set; } = string.Empty;
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double CacheHitRate => (CacheHits + CacheMisses) > 0 ? (double)CacheHits / (CacheHits + CacheMisses) : 0;
}

public class ThemeDefinition
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Colors { get; set; } = new();
    public Dictionary<string, string> Fonts { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}
