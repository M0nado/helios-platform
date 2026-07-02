# QUICK REFERENCE: Service Layer Integration

## Service Registration (One Line)

```csharp
services.AddMonadoBladeServices();
```

## Core Services at a Glance

### 1. IQueryService - Read Operations
```csharp
var user = await queryService.GetByIdAsync<User>("id");
var page = await queryService.PageAsync<User>(1, 20);
var results = await queryService.SearchAsync<User>("query", new[] { "Name" });
```

### 2. IMutationService - Write Operations
```csharp
var created = await mutationService.CreateAsync(user);
var updated = await mutationService.UpdateAsync("id", user);
await mutationService.DeleteAsync<User>("id");
```

### 3. ISubscribeService - Real-Time
```csharp
await subscribeService.ConnectAsync();
var sub = await subscribeService.SubscribeToEntityChangesAsync<User>(handler);
await subscribeService.PublishEventAsync("EventName", data);
```

### 4. IManageService - Admin
```csharp
await manageService.SetConfigurationAsync("key", value);
await manageService.GrantPermissionAsync("principal", "permission");
await manageService.LogAuditAsync(auditLog);
```

### 5. IDashboardService - Analytics
```csharp
var metrics = await dashboardService.GetMetricsAsync(range);
var insights = await dashboardService.GenerateInsightsAsync(data);
var alerts = await dashboardService.GetActiveAlertsAsync();
```

### 6. ISettingsService - Configuration
```csharp
var setting = await settingsService.GetSettingAsync<T>("key");
await settingsService.SetSettingAsync("key", value);
```

## Data Access Layer

```csharp
var stats = await dataAccessLayer.GetPoolStatsAsync();
var metrics = dataAccessLayer.GetQueryMetrics();
var plans = await dataAccessLayer.AnalyzeQueriesAsync();
```

## Typical Usage Pattern

```csharp
// Inject services
public MyController(
    IQueryService queryService,
    IMutationService mutationService,
    ISubscribeService subscribeService,
    IManageService manageService)
{
    // Store services
}

// Use in methods
public async Task<User> GetUser(string id)
{
    return await _queryService.GetByIdAsync<User>(id);
}

public async Task CreateUser(User user)
{
    var created = await _mutationService.CreateAsync(user);
    await _subscribeService.PublishEventAsync("UserCreated", created);
}
```

## Key Features

- ✅ 100% Async/Await (no blocking)
- ✅ Automatic cache invalidation
- ✅ Real-time change notifications
- ✅ Built-in audit logging
- ✅ Connection pooling (max 20)
- ✅ Query performance metrics
- ✅ ACID transaction support

## Configuration Options

```csharp
var options = new ServiceLayerOptions
{
    MaxConnectionPoolSize = 20,
    DefaultCacheDuration = TimeSpan.FromMinutes(5),
    EnablePerformanceTracking = true,
    SlowQueryThresholdMs = 100
};
```

## Files Overview

| Component | File | Purpose |
|-----------|------|---------|
| Query Service | IQueryService.cs | Read operations with caching |
| Mutation Service | IMutationService.cs | Write operations |
| Subscribe Service | ISubscribeService.cs | Real-time updates |
| Manage Service | IManageService.cs | Admin operations |
| Data Layer | DataAccessLayer.cs | Connection pooling + optimization |
| Configuration | ServiceCollectionExtensions.cs | DI setup |
| Base Class | ServiceBase.cs | Common functionality |

## See Also

- **Detailed Guide**: SERVICE_IMPLEMENTATION_GUIDE.md
- **Completion Report**: P2_COMPLETION_SUMMARY.md
- **Architecture**: INTEGRATION_ARCHITECTURE.md
