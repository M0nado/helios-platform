using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core;

/// <summary>
/// Service container for dependency injection.
/// </summary>
public class ServiceContainer
{
    private static readonly Lazy<ServiceContainer> _instance = new(() => new ServiceContainer());
    public static ServiceContainer Instance => _instance.Value;

    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    private ServiceContainer()
    {
        RegisterDefaults();
    }

    /// <summary>Register default services.</summary>
    private void RegisterDefaults()
    {
        // Defaults disabled - services registered by Program.cs
    }

    /// <summary>Register a singleton service.</summary>
    public void RegisterSingleton<T>(T instance) where T : class
    {
        _services[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
        _factories.Remove(typeof(T));
    }

    /// <summary>Register a factory for creating services.</summary>
    public void RegisterFactory<T>(Func<T> factory) where T : class
    {
        _factories[typeof(T)] = () => factory() ?? throw new InvalidOperationException("Factory returned null");
        _services.Remove(typeof(T));
    }

    /// <summary>Get a service instance.</summary>
    public T? GetService<T>() where T : class
    {
        var type = typeof(T);

        if (_services.TryGetValue(type, out var service))
            return (T)service;

        if (_factories.TryGetValue(type, out var factory))
            return (T)factory();

        return null;
    }

    /// <summary>Get all registered services.</summary>
    public IEnumerable<(Type, object)> GetAllServices()
    {
        foreach (var kvp in _services)
            yield return (kvp.Key, kvp.Value);
    }

    /// <summary>Clear all services.</summary>
    public void Clear()
    {
        _services.Clear();
        _factories.Clear();
    }
}
