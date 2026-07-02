namespace MonadoBlade.Core.Abstractions;

/// <summary>
/// Marker interface for all service abstractions in the system.
/// </summary>
public interface IService
{
}

/// <summary>
/// Base interface for services that require initialization.
/// </summary>
public interface IInitializable : IService
{
    /// <summary>
    /// Initializes the service.
    /// </summary>
    Task InitializeAsync();
}

/// <summary>
/// Base interface for services that manage their lifecycle.
/// </summary>
public interface ILifecycleService : IInitializable, IAsyncDisposable
{
    /// <summary>
    /// Indicates whether the service is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }
}
