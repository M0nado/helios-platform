# Stream 1 Phase 1: Quick Start Guide

**For Teams Ready to Implement Phase 2**

---

## 🎯 Your Mission

You have inherited a **enterprise-grade service architecture** that clearly defines:
- ✅ 6 core service boundaries
- ✅ Service contracts (interfaces with full documentation)
- ✅ Exception handling strategy
- ✅ GitHub workflows and CI/CD pipeline
- ✅ Team collaboration framework

**Your Job**: Implement the services and make them production-ready.

---

## 📋 What You Have

### 4 Reference Documents

1. **[STREAM1_PHASE1_INDEX.md](./STREAM1_PHASE1_INDEX.md)** ← START HERE
   - 15-minute overview of everything
   - Quick reference guide
   - How to use the architecture

2. **[INTEGRATION_ARCHITECTURE.md](./INTEGRATION_ARCHITECTURE.md)** (30 min read)
   - Component-to-service mapping
   - Data flow patterns
   - Architecture diagrams
   - Dependency analysis

3. **[GITHUB_UNIFIED_PLAN.md](./GITHUB_UNIFIED_PLAN.md)** (reference as needed)
   - Repository structure
   - Branch strategy
   - CI/CD workflows
   - PR requirements

4. **[PHASE1_COMPLETION_REPORT.md](./PHASE1_COMPLETION_REPORT.md)** (reference as needed)
   - Architectural patterns extracted
   - Success metrics
   - Phase 2 planning

### 6 Service Interfaces

Located in `src/MonadoBlade.Core/Services/`:

1. **IDataService.cs** - CRUD operations + pagination
2. **ICloudSyncService.cs** - Synchronization + conflict resolution
3. **IMLService.cs** - AI/ML queries + provider routing
4. **IPluginService.cs** - Plugin lifecycle management
5. **IDashboardService.cs** - Metrics + insights + anomaly detection
6. **ISettingsService.cs** - Configuration management

### Exception Contracts

Located in `src/MonadoBlade.Core/Exceptions/`:

- **ServiceExceptions.cs** - 9 exception types for consistent error handling

---

## 🚀 Getting Started (30 minutes)

### Step 1: Read the Overview (10 min)
```
→ Open STREAM1_PHASE1_INDEX.md
→ Read "Quick Navigation" section
→ Scan the 3 "Reusable Architectural Patterns"
```

### Step 2: Review the Service Interfaces (15 min)
```
→ Open src/MonadoBlade.Core/Services/IDataService.cs
→ Read the XML documentation on each method
→ Understand the pattern (async Task-based, generic types)
→ Repeat for all 6 services

Key Pattern:
  public interface IDataService : IService
  {
      Task<T?> GetByIdAsync<T>(string id) where T : class;
      // Each method: async, Task-based, strongly typed
  }
```

### Step 3: Understand the Exception Model (5 min)
```
→ Open src/MonadoBlade.Core/Exceptions/ServiceExceptions.cs
→ Each exception inherits from BusinessException
→ Each exception has: ErrorCode + UserMessage + Context
→ Use these exceptions in all service implementations
```

---

## 📝 Phase 2 Task Breakdown

### Task 1: Implement Core Services (Week 1)

**IDataService Implementation**:
```csharp
public class DataService : IDataService
{
    private readonly IRepository<T> _repository;
    
    public async Task<T?> GetByIdAsync<T>(string id) where T : class
    {
        try
        {
            return await _repository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            throw new OperationFailedException("GetById", ex.Message, innerException: ex);
        }
    }
    
    // Implement all other methods following the same pattern
}
```

**Key Implementation Pattern**:
1. Inject dependencies (repositories, event bus, cache)
2. Implement each interface method
3. Wrap in try-catch with appropriate exception
4. Log operations (if logging service available)
5. Publish events for cross-service communication

### Task 2: Write Unit Tests (Week 1)

```csharp
public class DataServiceTests
{
    private readonly DataService _service;
    private readonly Mock<IRepository<Entity>> _repositoryMock;
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var id = "test-id";
        var entity = new Entity { Id = id };
        _repositoryMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(entity);
        
        // Act
        var result = await _service.GetByIdAsync<Entity>(id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsOperationFailedException()
    {
        // Arrange
        var id = "invalid-id";
        _repositoryMock.Setup(r => r.GetByIdAsync(id))
            .ThrowsAsync(new Exception("Not found"));
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationFailedException>(
            () => _service.GetByIdAsync<Entity>(id));
    }
}
```

### Task 3: Integration Tests (Week 2)

Test service interactions:
```csharp
[Integration]
public class DataServiceIntegrationTests
{
    [Fact]
    public async Task Create_PublishesEntityCreatedEvent()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var service = new DataService(_repository, eventBus);
        
        // Act
        var entity = await service.CreateAsync(new Entity { Name = "Test" });
        
        // Assert
        var publishedEvents = eventBus.GetPublished<EntityCreatedEvent>();
        Assert.Single(publishedEvents);
        Assert.Equal(entity.Id, publishedEvents[0].EntityId);
    }
}
```

### Task 4: Setup DI Configuration (Week 2)

```csharp
// In startup/configuration
services.AddScoped<IDataService, DataService>();
services.AddScoped<ICloudSyncService, CloudSyncService>();
services.AddScoped<IMLService, MLService>();
services.AddScoped<IPluginService, PluginService>();
services.AddScoped<IDashboardService, DashboardService>();
services.AddScoped<ISettingsService, SettingsService>();

// Register repositories
services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

// Register event bus
services.AddSingleton<IEventBus, EventAggregator>();
```

---

## ✅ Phase 2 Success Criteria

- [ ] All 6 services fully implemented
- [ ] Each interface method has implementation
- [ ] Unit tests for all services (≥80% coverage)
- [ ] Integration tests for service interactions
- [ ] No circular dependencies
- [ ] All tests passing
- [ ] No compiler warnings
- [ ] Code follows style guide

---

## 🎓 Key Principles to Remember

### 1. Depend on Abstractions, Not Implementations
```csharp
// ❌ Wrong
public class CloudSyncService
{
    private readonly DataService _dataService; // Direct dependency
}

// ✅ Right
public class CloudSyncService
{
    public CloudSyncService(IDataService dataService, IEventBus events)
    {
        _dataService = dataService;
        _events = events;
    }
}
```

### 2. Use Async/Await for All I/O
```csharp
// ❌ Wrong
public Task<Data> GetDataAsync()
{
    var result = _db.Query(); // Blocking call
    return Task.FromResult(result);
}

// ✅ Right
public async Task<Data> GetDataAsync()
{
    return await _db.QueryAsync();
}
```

### 3. Throw Appropriate Exceptions
```csharp
// Use specific exception types
if (entity == null)
    throw new EntityNotFoundException(typeof(T).Name, id);

if (validationErrors.Any())
    throw new ValidationFailedException(typeof(T).Name, validationErrors);

if (service.IsUnavailable)
    throw new ServiceUnavailableException("ExternalService");
```

### 4. Publish Events for Cross-Service Communication
```csharp
// After creating an entity
await _eventBus.PublishAsync(new EntityCreatedEvent
{
    EntityType = typeof(T).Name,
    EntityId = entity.Id,
    CreatedAt = DateTime.UtcNow
});

// Dashboard listens and updates metrics
// Sync service listens and marks for sync
// Audit service listens and logs
```

---

## 📚 Reference Implementation Pattern

Use this as a template for implementing all 6 services:

```csharp
namespace MonadoBlade.Services.{ServiceName};

/// <summary>
/// Implementation of I{ServiceName}Service.
/// Responsible for: [what this service does]
/// </summary>
public class {ServiceName}Service : I{ServiceName}Service
{
    private readonly IRepository<> _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<{ServiceName}Service> _logger;
    
    public {ServiceName}Service(
        IRepository<> repository,
        IEventBus eventBus,
        ILogger<{ServiceName}Service> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }
    
    /// <summary>
    /// [Implementation of interface method]
    /// </summary>
    public async Task<TResult> MethodAsync(TParam parameter)
    {
        try
        {
            // Validate input
            if (parameter == null)
                throw new ValidationFailedException(nameof(TParam), new[] { "Parameter required" });
            
            // Execute operation
            var result = await _repository.OperationAsync(parameter);
            
            // Publish events
            await _eventBus.PublishAsync(new OperationCompletedEvent { /* ... */ });
            
            // Log
            _logger.LogInformation("Operation completed successfully");
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed");
            throw new OperationFailedException(nameof(MethodAsync), ex.Message, innerException: ex);
        }
    }
}
```

---

## 🔗 How to Connect Everything

### Service-to-Service Communication

```
UI Component (ViewModels)
    ↓
IService Interface
    ↓
Service Implementation
    ├── Repository (for data)
    ├── Other Services (via DI)
    └── Event Bus (for notifications)
```

### Example: Dashboard needs data from Data Service

```csharp
public class DashboardService : IDashboardService
{
    public DashboardService(IDataService dataService, IEventBus eventBus)
    {
        _dataService = dataService;
        _eventBus = eventBus;
    }
    
    public async Task<DashboardMetrics> GetMetricsAsync(DateRange range)
    {
        // Ask Data Service for entities
        var entities = await _dataService.QueryAsync<Entity>(
            q => q.Where(e => e.CreatedAt >= range.Start && e.CreatedAt <= range.End)
        );
        
        // Aggregate into metrics
        var metrics = AggregateMetrics(entities);
        
        // Publish event for audit
        await _eventBus.PublishAsync(new MetricsGeneratedEvent { /* ... */ });
        
        return metrics;
    }
}
```

---

## ⚡ Quick Commands

### Build
```powershell
dotnet build --configuration Release
```

### Run Unit Tests
```powershell
dotnet test ./tests/Unit --configuration Release
```

### Run Specific Service Tests
```powershell
dotnet test ./tests/Unit/Services.Tests/Data --configuration Release
```

### Check Code Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage" ./tests/Unit
```

---

## 🆘 Getting Help

### If you're stuck on:

**Service Interface**: Look at INTEGRATION_ARCHITECTURE.md section on that service

**Exception Usage**: Check ServiceExceptions.cs for examples of each exception

**Testing Pattern**: Look at other services' test files for reference

**DI Setup**: Check GITHUB_UNIFIED_PLAN.md for dependency injection section

**Architecture Question**: Review the 3 patterns in STREAM1_PHASE1_INDEX.md

---

## 🎉 You're Ready!

You now have:
- ✅ Clear service boundaries
- ✅ Explicit contracts
- ✅ Exception handling strategy
- ✅ Reference implementation patterns
- ✅ Test templates
- ✅ Documentation

**Proceed with Phase 2 implementation with confidence!**

---

**Need to reference something quickly?**
→ Use STREAM1_PHASE1_INDEX.md "Quick Reference" section
→ All file locations documented
→ All key concepts explained
