using System.Collections.Concurrent;
using HELIOS.IntegrationBroker.Contracts;

namespace HELIOS.IntegrationBroker.Services;

public sealed class InMemoryBrokerStore
{
    private readonly ConcurrentDictionary<string, IntegrationEvent> _events =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, PendingAction> _actions =
        new(StringComparer.OrdinalIgnoreCase);

    public int EventCount => _events.Count;

    public int PendingActionCount => _actions.Count;

    public bool TryAddEvent(IntegrationEvent integrationEvent)
    {
        var stored = integrationEvent with { Payload = integrationEvent.Payload.Clone() };
        return _events.TryAdd(stored.EventId, stored);
    }

    public IntegrationEvent? GetEvent(string eventId) =>
        _events.TryGetValue(eventId, out var integrationEvent) ? integrationEvent : null;

    public IReadOnlyList<IntegrationEvent> SearchEvents(string? correlationId, int limit)
    {
        var boundedLimit = Math.Clamp(limit, 1, 100);
        IEnumerable<IntegrationEvent> query = _events.Values;

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(item =>
                string.Equals(item.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(item => item.OccurredAt)
            .Take(boundedLimit)
            .ToArray();
    }

    public PendingAction AddAction(ToolActionRequest request, ToolDescriptor tool)
    {
        var action = new PendingAction(
            RequestId: $"req_{Guid.NewGuid():N}",
            ToolName: tool.Name,
            CorrelationId: request.CorrelationId,
            Environment: request.Environment,
            Arguments: request.Arguments.Clone(),
            RequestedBy: request.RequestedBy,
            Reason: request.Reason,
            Status: "pending-approval",
            Approval: tool.Approval,
            CreatedAt: DateTimeOffset.UtcNow);

        if (!_actions.TryAdd(action.RequestId, action))
        {
            throw new InvalidOperationException("Unable to allocate a unique pending-action identifier.");
        }

        return action;
    }

    public PendingAction? GetAction(string requestId) =>
        _actions.TryGetValue(requestId, out var action) ? action : null;
}
