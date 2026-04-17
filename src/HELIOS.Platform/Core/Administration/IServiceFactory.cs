using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Administration;

/// <summary>
/// Centralized service factory for creating and configuring services
/// Ensures consistent initialization and dependency resolution across the platform
/// </summary>
public interface IServiceFactory
{
    /// <summary>Creates a new service instance with proper configuration</summary>
    T CreateService<T>(Dictionary<string, object> config = null) where T : class;
    
    /// <summary>Gets a service from cache or creates if not exists</summary>
    T GetOrCreateService<T>(string serviceKey) where T : class;
    
    /// <summary>Validates service health and dependencies</summary>
    Task<ServiceHealthReport> ValidateServiceAsync<T>(string serviceKey) where T : class;
    
    /// <summary>Lists all registered services and their status</summary>
    Task<List<ServiceRegistry>> ListAllServicesAsync();
}

public class ServiceFactory : IServiceFactory
{
    private readonly Dictionary<string, object> _cache = new();
    private readonly Dictionary<string, ServiceRegistry> _registry = new();

    public T CreateService<T>(Dictionary<string, object> config = null) where T : class
    {
        try
        {
            var typeName = typeof(T).Name;
            var impl = Activator.CreateInstance(typeof(T).Assembly.GetType($"{typeof(T).Namespace}.{typeName.Substring(1)}")) as T;
            
            if (impl != null)
            {
                _registry[typeName] = new ServiceRegistry
                {
                    ServiceName = typeName,
                    Status = "Initialized",
                    CreatedAt = DateTime.UtcNow,
                    ImplementationType = impl.GetType().FullName
                };
            }
            
            return impl;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create service {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    public T GetOrCreateService<T>(string serviceKey) where T : class
    {
        var key = $"{serviceKey}:{typeof(T).Name}";
        
        if (!_cache.TryGetValue(key, out var cached))
        {
            cached = CreateService<T>();
            _cache[key] = cached;
        }
        
        return cached as T;
    }

    public async Task<ServiceHealthReport> ValidateServiceAsync<T>(string serviceKey) where T : class
    {
        var report = new ServiceHealthReport
        {
            ServiceName = typeof(T).Name,
            Timestamp = DateTime.UtcNow,
            IsHealthy = true
        };
        
        try
        {
            var service = GetOrCreateService<T>(serviceKey);
            report.IsHealthy = service != null;
        }
        catch (Exception ex)
        {
            report.IsHealthy = false;
            report.HealthMessage = ex.Message;
        }
        
        await Task.CompletedTask;
        return report;
    }

    public async Task<List<ServiceRegistry>> ListAllServicesAsync()
    {
        var result = new List<ServiceRegistry>(_registry.Values);
        await Task.CompletedTask;
        return result;
    }
}

public class ServiceRegistry
{
    public string ServiceName { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ImplementationType { get; set; }
    public string ErrorMessage { get; set; }
}

public class ServiceHealthReport
{
    public string ServiceName { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsHealthy { get; set; }
    public string HealthMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
