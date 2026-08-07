namespace Helios.Connect.Contracts;

public sealed record HeliosIntegrationEvent(
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
    HeliosIntegrationActor? Actor,
    IReadOnlyList<HeliosIntegrationLink> Links,
    IReadOnlyDictionary<string, object?> Payload);

public sealed record HeliosIntegrationActor(string Type, string Id, string? DisplayName);

public sealed record HeliosIntegrationLink(string Rel, Uri Href);
