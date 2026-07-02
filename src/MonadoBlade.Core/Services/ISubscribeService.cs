namespace MonadoBlade.Core.Services;

/// <summary>
/// Subscribe service providing real-time data updates via SignalR.
/// Responsible for event subscriptions, real-time notifications, and change tracking.
/// </summary>
/// <remarks>
/// Segregation Pattern: Real-Time Operations
/// - Event subscriptions and notifications
/// - Change feeds for real-time updates
/// - SignalR integration
/// - Disposable subscriptions
/// </remarks>
public interface ISubscribeService : IService
{
    /// <summary>
    /// Subscribes to changes on a specific entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="handler">Handler to invoke when entity changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable subscription.</returns>
    IAsyncDisposable SubscribeToEntityChangesAsync<T>(
        Func<EntityChange<T>, Task> handler,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Subscribes to changes on a specific entity by ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entityId">The entity identifier to monitor.</param>
    /// <param name="handler">Handler to invoke when entity changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable subscription.</returns>
    IAsyncDisposable SubscribeToEntityByIdAsync<T>(
        string entityId,
        Func<EntityChange<T>, Task> handler,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Subscribes to a custom event or topic.
    /// </summary>
    /// <param name="eventName">The event or topic name.</param>
    /// <param name="handler">Handler to invoke when event is raised.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable subscription.</returns>
    IAsyncDisposable SubscribeToEventAsync(
        string eventName,
        Func<object?, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="data">The event data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the publish operation.</returns>
    Task PublishEventAsync(string eventName, object? data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a change feed for an entity type for tailing new changes.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="fromVersion">Optional starting version number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of entity changes.</returns>
    IAsyncEnumerable<EntityChange<T>> GetChangeFeedAsync<T>(
        long? fromVersion = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Connects to the real-time hub (SignalR or similar).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the connection.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the real-time hub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the disconnection.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection status.
    /// </summary>
    /// <returns>True if connected; otherwise false.</returns>
    bool IsConnected { get; }

    /// <summary>
    /// Subscribes to connection state changes.
    /// </summary>
    /// <param name="handler">Handler to invoke when connection state changes.</param>
    /// <returns>A disposable subscription.</returns>
    IDisposable SubscribeToConnectionStateChanges(Action<bool> handler);

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    /// <returns>The number of active subscriptions.</returns>
    int GetActiveSubscriptionCount();

    /// <summary>
    /// Unsubscribes from all subscriptions of a specific entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A task representing the unsubscribe operation.</returns>
    Task UnsubscribeAllAsync<T>() where T : class;
}

/// <summary>
/// Represents a change to an entity in the system.
/// </summary>
public class EntityChange<T> where T : class
{
    /// <summary>Gets or sets the change type.</summary>
    public EntityChangeType Type { get; set; }

    /// <summary>Gets or sets the changed entity.</summary>
    public T Entity { get; set; } = null!;

    /// <summary>Gets or sets the previous version of the entity (for updates).</summary>
    public T? PreviousEntity { get; set; }

    /// <summary>Gets or sets the change timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the change version number for ordering.</summary>
    public long Version { get; set; }

    /// <summary>Gets or sets the user who made the change.</summary>
    public string? ChangedBy { get; set; }

    /// <summary>Gets or sets specific fields that changed (for updates).</summary>
    public List<string> ChangedFields { get; set; } = new();
}

/// <summary>
/// Defines types of entity changes.
/// </summary>
public enum EntityChangeType
{
    /// <summary>Entity was created.</summary>
    Created,

    /// <summary>Entity was updated.</summary>
    Updated,

    /// <summary>Entity was deleted.</summary>
    Deleted,

    /// <summary>Entity was soft deleted.</summary>
    SoftDeleted,

    /// <summary>Entity was restored from soft delete.</summary>
    Restored
}
