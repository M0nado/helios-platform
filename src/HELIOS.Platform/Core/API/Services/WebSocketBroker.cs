using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.API.Interfaces;

namespace HELIOS.Platform.Core.API.Services;

/// <summary>
/// High-performance WebSocket broker for real-time pub/sub messaging.
/// </summary>
public class WebSocketBroker : IWebSocketBroker
{
    private readonly ILogger<WebSocketBroker> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, List<Func<WebSocketMessage, Task>>> _handlers = new();
    private readonly Dictionary<string, HashSet<string>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, DateTime> _clientLastActive = new();
    private long _messagesPublished = 0;
    private long _errorsEncountered = 0;

    public WebSocketBroker(ILogger<WebSocketBroker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("WebSocket Broker initialized");
    }

    /// <summary>
    /// Registers a message handler for a topic.
    /// </summary>
    public async Task<bool> RegisterHandlerAsync(string topic, Func<WebSocketMessage, Task> handler)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be empty", nameof(topic));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        await _semaphore.WaitAsync();
        try
        {
            if (!_handlers.ContainsKey(topic))
                _handlers[topic] = new List<Func<WebSocketMessage, Task>>();

            _handlers[topic].Add(handler);
            _logger.LogInformation("Handler registered for topic: {Topic}", topic);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering handler for topic: {Topic}", topic);
            Interlocked.Increment(ref _errorsEncountered);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Publishes a message to all subscribers of a topic.
    /// </summary>
    public async Task<bool> PublishAsync(string topic, WebSocketMessage message)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be empty", nameof(topic));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var startTime = DateTime.UtcNow;

        try
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_handlers.TryGetValue(topic, out var handlers))
                {
                    var tasks = handlers.Select(h => PublishToHandlerSafeAsync(h, message, topic)).ToList();
                    await Task.WhenAll(tasks);
                }

                Interlocked.Increment(ref _messagesPublished);
                var elapsed = DateTime.UtcNow - startTime;

                if (elapsed.TotalMilliseconds > 20)
                    _logger.LogWarning("Slow WebSocket publish detected ({Latency}ms) for topic: {Topic}",
                        elapsed.TotalMilliseconds, topic);

                _logger.LogDebug("Message published to topic: {Topic} ({Latency}ms)", topic, elapsed.TotalMilliseconds);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to topic: {Topic}", topic);
            Interlocked.Increment(ref _errorsEncountered);
            return false;
        }
    }

    private async Task PublishToHandlerSafeAsync(Func<WebSocketMessage, Task> handler, WebSocketMessage message, string topic)
    {
        try
        {
            await handler(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in handler for topic: {Topic}", topic);
            Interlocked.Increment(ref _errorsEncountered);
        }
    }

    /// <summary>
    /// Subscribes a client to a topic.
    /// </summary>
    public async Task<bool> SubscribeAsync(string clientId, string topic)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID cannot be empty", nameof(clientId));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be empty", nameof(topic));

        await _semaphore.WaitAsync();
        try
        {
            if (!_subscriptions.ContainsKey(topic))
                _subscriptions[topic] = new HashSet<string>();

            _subscriptions[topic].Add(clientId);
            _clientLastActive[clientId] = DateTime.UtcNow;
            _logger.LogInformation("Client subscribed: {ClientId} -> {Topic}", clientId, topic);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing client {ClientId} to topic {Topic}", clientId, topic);
            Interlocked.Increment(ref _errorsEncountered);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Unsubscribes a client from a topic.
    /// </summary>
    public async Task<bool> UnsubscribeAsync(string clientId, string topic)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID cannot be empty", nameof(clientId));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be empty", nameof(topic));

        await _semaphore.WaitAsync();
        try
        {
            if (_subscriptions.TryGetValue(topic, out var clients))
            {
                clients.Remove(clientId);
            }
            _logger.LogInformation("Client unsubscribed: {ClientId} <- {Topic}", clientId, topic);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets all subscribers for a topic.
    /// </summary>
    public async Task<List<string>> GetSubscribersAsync(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be empty", nameof(topic));

        await _semaphore.WaitAsync();
        try
        {
            return _subscriptions.TryGetValue(topic, out var clients)
                ? clients.ToList()
                : new List<string>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets broker metrics.
    /// </summary>
    public WebSocketMetrics GetMetrics()
    {
        return new WebSocketMetrics
        {
            TotalMessagesPublished = Interlocked.Read(ref _messagesPublished),
            TotalErrorsEncountered = Interlocked.Read(ref _errorsEncountered),
            ActiveTopics = _handlers.Count,
            ActiveSubscriptions = _subscriptions.Values.Sum(s => s.Count),
            ActiveClients = _clientLastActive.Count
        };
    }

    /// <summary>
    /// Removes inactive clients.
    /// </summary>
    public async Task<int> RemoveInactiveClientsAsync(TimeSpan inactivityThreshold)
    {
        var cutoffTime = DateTime.UtcNow - inactivityThreshold;
        var inactiveClients = _clientLastActive
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var clientId in inactiveClients)
        {
            _clientLastActive.TryRemove(clientId, out _);
            foreach (var topic in _subscriptions.Keys)
            {
                if (_subscriptions[topic].Contains(clientId))
                    await UnsubscribeAsync(clientId, topic);
            }
        }

        _logger.LogInformation("Removed {Count} inactive clients", inactiveClients.Count);
        return inactiveClients.Count;
    }
}

/// <summary>
/// WebSocket broker metrics.
/// </summary>
public class WebSocketMetrics
{
    public long TotalMessagesPublished { get; set; }
    public long TotalErrorsEncountered { get; set; }
    public int ActiveTopics { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ActiveClients { get; set; }
}
