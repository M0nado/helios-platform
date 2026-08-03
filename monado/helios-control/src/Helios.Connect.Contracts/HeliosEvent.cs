namespace Helios.Connect.Contracts;

public sealed record HeliosEvent(
    string Id,
    string Type,
    string Source,
    string Subject,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string? TraceParent,
    string DataClassification,
    IReadOnlyDictionary<string, object?> Payload);
