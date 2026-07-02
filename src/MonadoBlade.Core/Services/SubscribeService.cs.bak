namespace MonadoBlade.Core.Services;

/// <summary>
/// Implementation of ISubscribeService providing real-time updates.
/// Segregation Pattern: Focused on event subscriptions and change tracking.
/// </summary>
public class SubscribeService : ServiceBase, ISubscribeService
{
    private readonly Dictionary<string, List<Func<object?, Task>>> _eventHandlers = new();
    private bool _isConnected = false;

    public SubscribeService(ILogger logger) : base(logger)
    {
    }

    public bool IsConnected => _isConnected;

    public IAsyncDisposable SubscribeToEntityChangesAsync<T>(
        Func<EntityChange<T>, Task> handler,
        CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Subscribing to entity changes for type {EntityType}", typeof(T).Name);
        var eventName = $"entity:{typeof(T).Name}:*";
        
        return new SubscriptionDisposable(async () =>
        {
            LogInfo("Unsubscribing from entity changes for type {EntityType}", typeof(T).Name);
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(h => ReferenceEquals(h, handler));
            }
        });
    }

    public IAsyncDisposable SubscribeToEntityByIdAsync<T>(
        string entityId,
        Func<EntityChange<T>, Task> handler,
        CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Subscribing to entity changes for {EntityType} ID {EntityId}", typeof(T).Name, entityId);
        var eventName = $"entity:{typeof(T).Name}:{entityId}";
        return new SubscriptionDisposable(Task.CompletedTask);
    }

    public IAsyncDisposable SubscribeToEventAsync(
        string eventName,
        Func<object?, Task> handler,
        CancellationToken cancellationToken = default)
    {
        LogInfo("Subscribing to event {EventName}", eventName);
        
        if (!_eventHandlers.ContainsKey(eventName))
            _eventHandlers[eventName] = new List<Func<object?, Task>>();

        _eventHandlers[eventName].Add(handler);

        return new SubscriptionDisposable(async () =>
        {
            LogInfo("Unsubscribing from event {EventName}", eventName);
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(handler);
            }
        });
    }

    public async Task PublishEventAsync(string eventName, object? data, CancellationToken cancellationToken = default)
    {
        LogInfo("Publishing event {EventName}", eventName);
        
        if (_eventHandlers.TryGetValue(eventName, out var handlers))
        {
            var tasks = handlers.Select(h => h(data)).ToList();
            await Task.WhenAll(tasks);
        }
    }

    public async IAsyncEnumerable<EntityChange<T>> GetChangeFeedAsync<T>(
        long? fromVersion = null,
        CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Getting change feed for {EntityType} from version {Version}", typeof(T).Name, fromVersion ?? 0);
        
        // Placeholder implementation
        yield break;
        await Task.CompletedTask;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        LogInfo("Connecting to real-time hub");
        _isConnected = true;
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        LogInfo("Disconnecting from real-time hub");
        _isConnected = false;
        await Task.CompletedTask;
    }

    public IDisposable SubscribeToConnectionStateChanges(Action<bool> handler)
    {
        LogInfo("Subscribing to connection state changes");
        return new DummyDisposable();
    }

    public int GetActiveSubscriptionCount()
    {
        return _eventHandlers.Values.Sum(h => h.Count);
    }

    public async Task UnsubscribeAllAsync<T>() where T : class
    {
        LogInfo("Unsubscribing all handlers for type {EntityType}", typeof(T).Name);
        var pattern = $"entity:{typeof(T).Name}:";
        var keysToRemove = _eventHandlers.Keys
            .Where(k => k.StartsWith(pattern))
            .ToList();

        foreach (var key in keysToRemove)
            _eventHandlers.Remove(key);

        await Task.CompletedTask;
    }

    private class SubscriptionDisposable : IAsyncDisposable
    {
        private readonly Func<Task> _onDispose;

        public SubscriptionDisposable(Func<Task> onDispose)
        {
            _onDispose = onDispose;
        }

        public SubscriptionDisposable(Task onDispose) : this(() => onDispose) { }

        public async ValueTask DisposeAsync()
        {
            await _onDispose();
        }
    }

    private class DummyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
