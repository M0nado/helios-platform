# Phase 3 Tier 3 - API & Web Layer Services - Usage Guide

## Overview

This guide demonstrates how to use the 6 implemented API & Web Layer Services in the HELIOS Platform.

---

## 1. API Gateway

### Setup

```csharp
using HELIOS.Platform.Core.API.Services;
using Microsoft.Extensions.Logging;

var logger = _serviceProvider.GetRequiredService<ILogger<APIGateway>>();
var cache = _serviceProvider.GetRequiredService<IL1CacheService>();

var gateway = new APIGateway(logger, cache);
```

### Register Routes

```csharp
// Register a simple GET route
await gateway.RegisterRouteAsync("GET", "/api/users/{id}",
    async (request) => new APIResponse
    {
        StatusCode = 200,
        Success = true,
        Body = "{\"id\": 123, \"name\": \"John\"}"
    });

// Register a POST route
await gateway.RegisterRouteAsync("POST", "/api/users",
    async (request) => new APIResponse
    {
        StatusCode = 201,
        Success = true,
        Body = request.Body
    });
```

### Configure Rate Limiting

```csharp
var apiKey = "your_api_key_here";
await gateway.ConfigureRateLimitAsync(apiKey, 1000); // 1000 requests per minute
```

### Enable Caching

```csharp
await gateway.EnableCachingAsync("GET:/api/users/{id}", TimeSpan.FromMinutes(5));
```

### Process Requests

```csharp
var request = new APIRequest
{
    Method = "GET",
    Path = "/api/users/123",
    ApiKey = apiKey,
    Headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } }
};

var response = await gateway.ProcessRequestAsync(request);
if (response.Success)
{
    Console.WriteLine($"Response: {response.Body}");
    Console.WriteLine($"Latency: {response.ProcessingTime.TotalMilliseconds}ms");
}
```

### Get Statistics

```csharp
var stats = await gateway.GetStatsAsync();
Console.WriteLine($"Total Requests: {stats.TotalRequests}");
Console.WriteLine($"Success Rate: {stats.SuccessfulRequests}/{stats.TotalRequests}");
Console.WriteLine($"Average Latency: {stats.AverageLatency}ms");
```

---

## 2. GraphQL Server

### Setup

```csharp
var logger = _serviceProvider.GetRequiredService<ILogger<GraphQLServer>>();
var cache = _serviceProvider.GetRequiredService<IL1CacheService>();

var graphqlServer = new GraphQLServer(logger, cache);
```

### Register Types

```csharp
// Register custom types
await graphqlServer.RegisterTypeAsync("User", typeof(UserModel));
await graphqlServer.RegisterTypeAsync("Post", typeof(PostModel));

// Define your models
public class UserModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### Register Query Resolvers

```csharp
await graphqlServer.RegisterQueryFieldAsync("getUser", async () =>
{
    return new UserModel { Id = 1, Name = "John", Email = "john@example.com" };
});

await graphqlServer.RegisterQueryFieldAsync("getAllUsers", async () =>
{
    return new List<UserModel>
    {
        new UserModel { Id = 1, Name = "John" },
        new UserModel { Id = 2, Name = "Jane" }
    };
});
```

### Execute Queries

```csharp
var query = "{ getUser { id name email } }";
var result = await graphqlServer.ExecuteQueryAsync(query);
Console.WriteLine($"Result: {result}");
```

### Get Schema

```csharp
var schema = await graphqlServer.GetSchemaAsync();
foreach (var type in schema.Types)
{
    Console.WriteLine($"Type: {type.Name}");
    foreach (var field in type.Fields)
    {
        Console.WriteLine($"  - {field.Name}: {field.Type}");
    }
}
```

---

## 3. WebSocket Broker

### Setup

```csharp
var logger = _serviceProvider.GetRequiredService<ILogger<WebSocketBroker>>();
var broker = new WebSocketBroker(logger);
```

### Register Handlers

```csharp
// Register a handler for a topic
await broker.RegisterHandlerAsync("notifications", async (message) =>
{
    Console.WriteLine($"Received: {message.Payload}");
});

// Register multiple handlers for the same topic
await broker.RegisterHandlerAsync("notifications", async (message) =>
{
    // Log the notification
    Console.WriteLine($"Logged: {message.Payload}");
});
```

### Subscribe Clients

```csharp
var clientId = "client_123";
var topic = "notifications";

await broker.SubscribeAsync(clientId, topic);
Console.WriteLine($"{clientId} subscribed to {topic}");
```

### Publish Messages

```csharp
var message = new WebSocketMessage
{
    Topic = "notifications",
    Payload = "New user registered: John"
};

await broker.PublishAsync("notifications", message);
```

### Get Subscribers

```csharp
var subscribers = await broker.GetSubscribersAsync("notifications");
Console.WriteLine($"Subscribers: {string.Join(", ", subscribers)}");
```

### Cleanup Inactive Clients

```csharp
var removed = await broker.RemoveInactiveClientsAsync(TimeSpan.FromMinutes(30));
Console.WriteLine($"Removed {removed} inactive clients");
```

---

## 4. Session Manager

### Setup

```csharp
var logger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
var cache = _serviceProvider.GetRequiredService<IL1CacheService>();

var sessionManager = new SessionManager(logger, cache);
```

### Create Sessions

```csharp
var userId = "user_123";
var initialData = new Dictionary<string, object>
{
    { "loginTime", DateTime.UtcNow },
    { "ipAddress", "192.168.1.1" }
};

var sessionId = await sessionManager.CreateSessionAsync(userId, initialData);
Console.WriteLine($"Session created: {sessionId}");
```

### Retrieve Sessions

```csharp
var session = await sessionManager.GetSessionAsync(sessionId);
if (session.SessionId != null)
{
    Console.WriteLine($"User: {session.UserId}");
    Console.WriteLine($"Expires: {session.ExpiresAt}");
    foreach (var kvp in session.Data)
    {
        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
    }
}
```

### Update Sessions

```csharp
await sessionManager.UpdateSessionAsync(sessionId, new Dictionary<string, object>
{
    { "role", "admin" },
    { "lastActivity", DateTime.UtcNow }
});
```

### Validate Sessions

```csharp
if (await sessionManager.ValidateSessionAsync(sessionId))
{
    Console.WriteLine("Session is valid");
}
else
{
    Console.WriteLine("Session has expired");
}
```

### Destroy Sessions

```csharp
await sessionManager.DestroySessionAsync(sessionId);
Console.WriteLine("Session destroyed");
```

### Cleanup Expired Sessions

```csharp
var cleaned = await sessionManager.CleanupExpiredSessionsAsync();
Console.WriteLine($"Cleaned up {cleaned} expired sessions");
```

---

## 5. Web UI Server

### Setup

```csharp
var logger = _serviceProvider.GetRequiredService<ILogger<WebUIServer>>();
var cache = _serviceProvider.GetRequiredService<IL1CacheService>();

var uiServer = new WebUIServer(logger, cache);
```

### Register Pages

```csharp
await uiServer.RegisterPageAsync("home", @"
    <html>
        <head><title>{{pageTitle}}</title></head>
        <body>
            <h1>Welcome {{userName}}</h1>
            <p>{{content}}</p>
        </body>
    </html>
");
```

### Render Pages

```csharp
var html = await uiServer.RenderPageAsync("home", new Dictionary<string, object>
{
    { "pageTitle", "Home Page" },
    { "userName", "John" },
    { "content", "Welcome to HELIOS Platform" }
});

Console.WriteLine(html);
```

### Register Components

```csharp
await uiServer.RegisterPageAsync("button", "<button>{{label}}</button>");
```

### Register Layouts

```csharp
await uiServer.RegisterLayoutAsync("default", @"
    <html>
        <body>
            {{content}}
        </body>
    </html>
");
```

### Server Lifecycle

```csharp
// Start server
await uiServer.StartServerAsync(8080);
Console.WriteLine("Server started on port 8080");

// Check health
var health = await uiServer.GetHealthAsync();
Console.WriteLine($"Server healthy: {health.IsHealthy}");

// Stop server
await uiServer.StopServerAsync();
Console.WriteLine("Server stopped");
```

---

## 6. Theme Manager

### Setup

```csharp
var logger = _serviceProvider.GetRequiredService<ILogger<ThemeManager>>();
var cache = _serviceProvider.GetRequiredService<IL1CacheService>();

var themeManager = new ThemeManager(logger, cache);
```

### Register Themes

```csharp
var darkTheme = new ThemeDefinition
{
    Name = "dark",
    Colors = new Dictionary<string, string>
    {
        { "primary", "#1e1e1e" },
        { "background", "#000000" },
        { "text", "#ffffff" }
    },
    Fonts = new Dictionary<string, string>
    {
        { "body", "Arial, sans-serif" }
    }
};

await themeManager.RegisterThemeAsync("dark", darkTheme);
```

### Set Default Theme

```csharp
await themeManager.SetDefaultThemeAsync("dark");
Console.WriteLine("Default theme set to: dark");
```

### Get Current Theme

```csharp
var currentTheme = await themeManager.GetCurrentThemeAsync();
Console.WriteLine($"Current theme: {currentTheme.Name}");
```

### List Themes

```csharp
var themes = await themeManager.ListThemesAsync();
Console.WriteLine($"Available themes: {string.Join(", ", themes)}");
```

### Get Theme as CSS

```csharp
var css = await themeManager.GetThemeCssAsync("dark");
Console.WriteLine(css);
```

### Delete Theme

```csharp
await themeManager.DeleteThemeAsync("dark");
Console.WriteLine("Theme deleted");
```

---

## Integration Example

### Complete Flow

```csharp
public async Task RunIntegrationExample()
{
    // Initialize services
    var logger = _serviceProvider.GetRequiredService<ILogger<APIGateway>>();
    var cache = _serviceProvider.GetRequiredService<IL1CacheService>();
    
    var gateway = new APIGateway(logger, cache);
    var sessionManager = new SessionManager(logger, cache);
    
    // Create a session
    var sessionId = await sessionManager.CreateSessionAsync("user_1");
    
    // Register API endpoint that uses the session
    await gateway.RegisterRouteAsync("GET", "/api/profile",
        async (request) =>
        {
            var session = await sessionManager.GetSessionAsync(sessionId);
            
            return new APIResponse
            {
                StatusCode = 200,
                Success = true,
                Body = System.Text.Json.JsonSerializer.Serialize(session)
            };
        });
    
    // Process request
    var request = new APIRequest
    {
        Method = "GET",
        Path = "/api/profile",
        ApiKey = "test_key"
    };
    
    var response = await gateway.ProcessRequestAsync(request);
    Console.WriteLine($"Response: {response.Body}");
}
```

---

## Performance Monitoring

### Get Metrics

```csharp
// API Gateway metrics
var apiMetrics = gateway.GetMetrics();
Console.WriteLine($"Cache Hit Rate: {apiMetrics.CacheHitRate:P}");
Console.WriteLine($"Success Rate: {apiMetrics.SuccessRate:P}");

// GraphQL metrics
var graphqlMetrics = graphqlServer.GetMetrics();
Console.WriteLine($"Total Queries: {graphqlMetrics.TotalQueries}");

// Session metrics
var sessionMetrics = sessionManager.GetMetrics();
Console.WriteLine($"Active Sessions: {sessionMetrics.ActiveSessions}");

// Theme metrics
var themeMetrics = await themeManager.GetMetricsAsync();
Console.WriteLine($"Total Themes: {themeMetrics.TotalThemes}");
```

---

## Error Handling

```csharp
try
{
    var response = await gateway.ProcessRequestAsync(request);
    if (!response.Success)
    {
        Console.WriteLine($"Error: {response.ErrorMessage}");
    }
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

---

## Best Practices

1. **Always check for null responses** from async operations
2. **Use appropriate TTL values** for caching (balance between freshness and performance)
3. **Monitor cache hit rates** to optimize performance
4. **Handle rate limiting gracefully** with retry logic
5. **Clean up resources** (sessions, subscriptions) when done
6. **Log important operations** for debugging and auditing
7. **Validate user input** before processing

---

## Summary

The Phase 3 Tier 3 API & Web Layer Services provide a comprehensive, high-performance foundation for building modern web applications with HELIOS Platform. All services are production-ready and fully tested.

For more information, refer to the comprehensive documentation files included in the delivery package.
