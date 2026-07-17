using System.Text.Json;

namespace HELIOS.IntegrationBroker.Contracts;

public sealed record IntegrationEvent(
    string SchemaVersion,
    string EventId,
    string Source,
    string EventType,
    string? Repository,
    string? EntityId,
    string CorrelationId,
    string? CausationId,
    string Environment,
    DateTimeOffset OccurredAt,
    string DataClassification,
    IntegrationActor? Actor,
    IReadOnlyList<IntegrationLink> Links,
    JsonElement Payload);

public sealed record IntegrationActor(string Type, string Id, string? DisplayName);

public sealed record IntegrationLink(string Rel, Uri Href);
