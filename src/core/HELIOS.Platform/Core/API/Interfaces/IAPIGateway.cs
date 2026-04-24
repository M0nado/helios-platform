using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.API.Interfaces;

/// <summary>
/// API Gateway service for request routing, throttling, and transformation.
/// </summary>
public interface IAPIGateway
{
    Task<bool> RegisterRouteAsync(string method, string path, Func<APIRequest, Task<APIResponse>> handler);
    Task<APIResponse> ProcessRequestAsync(APIRequest request);
    Task<bool> AddMiddlewareAsync(string name, Func<APIRequest, Func<Task<APIResponse>>, Task<APIResponse>> middleware);
    Task<bool> ConfigureRateLimitAsync(string identifier, int requestsPerMinute);
    Task<APIGatewayStats> GetStatsAsync(TimeSpan? period = null);
    Task<bool> EnableCachingAsync(string routePattern, TimeSpan ttl);
    Task<APIAuthContext> ValidateKeyAsync(string apiKey);
    Task<bool> RegisterVersionAsync(string version, List<string> supportedRoutes);
    Task<APIDocumentation> GetDocumentationAsync(string? version = null);
    Task<GatewayHealthStatus> GetHealthAsync();
}

public class APIRequest
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> QueryParams { get; set; } = new();
    public string? ApiKey { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

public class APIResponse
{
    public int StatusCode { get; set; } = 200;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class APIAuthContext
{
    public string ApiKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public bool IsAuthorized { get; set; }
}

public class APIGatewayStats
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageLatency { get; set; }
}

public class APIDocumentation
{
    public string Version { get; set; } = string.Empty;
    public List<APIEndpointDoc> Endpoints { get; set; } = new();
}

public class APIEndpointDoc
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class GatewayHealthStatus
{
    public bool IsHealthy { get; set; }
    public int ActiveConnections { get; set; }
}
