using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HELIOS.Fabric.Contracts;

public sealed record EventEnvelope
{
    public string SpecVersion { get; init; } = "1.0";
    public Guid EventId { get; init; }
    public Guid CorrelationId { get; init; }
    public required string EventType { get; init; }
    public required string Source { get; init; }
    public required string Subject { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public required string Environment { get; init; }
    public required string Severity { get; init; }
    public required string Sensitivity { get; init; }
    public required string Summary { get; init; }
    public required EventProvenance Provenance { get; init; }
    public JsonElement Payload { get; init; }
    public IReadOnlyList<string> RequestedActions { get; init; } = [];
    public IReadOnlyList<EventLink> Links { get; init; } = [];
    public EventApproval? Approval { get; init; }
}

public sealed record EventProvenance
{
    public string? Repository { get; init; }
    public string? CommitSha { get; init; }
    public string? Ref { get; init; }
    public string? Workflow { get; init; }
    public string? WorkflowRunId { get; init; }
    public required string Actor { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string ContentSha256 { get; init; }
}

public sealed record EventLink(string Label, Uri Url);
public sealed record ApprovalRecord(string Channel, string Actor, string Decision, DateTimeOffset RecordedAt);
public sealed record EventApproval(string PolicyId, int Required, string State, IReadOnlyList<ApprovalRecord> Records);

public sealed record DeliveryRoute(
    string ConnectorId,
    string Transform,
    string? ApprovalPolicy,
    string RouteId);

public interface IDeliverySink
{
    string ConnectorId { get; }
    Task DeliverAsync(EventEnvelope envelope, DeliveryRoute route, CancellationToken cancellationToken);
}

public static class FabricJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}

public static class CanonicalHash
{
    public static string ComputeEventContentSha256(EventEnvelope envelope)
    {
        var projection = new
        {
            envelope.SpecVersion,
            envelope.EventId,
            envelope.CorrelationId,
            envelope.EventType,
            envelope.Source,
            envelope.Subject,
            occurredAtUnixMs = envelope.OccurredAt.ToUnixTimeMilliseconds(),
            envelope.Environment,
            envelope.Severity,
            envelope.Sensitivity,
            envelope.Summary,
            provenance = new
            {
                envelope.Provenance.Repository,
                envelope.Provenance.CommitSha,
                envelope.Provenance.Ref,
                envelope.Provenance.Workflow,
                envelope.Provenance.WorkflowRunId,
                envelope.Provenance.Actor,
                envelope.Provenance.IdempotencyKey
            },
            envelope.Payload,
            envelope.RequestedActions,
            links = envelope.Links.Select(item => new { item.Label, url = item.Url.ToString() }).ToArray(),
            approval = envelope.Approval is null ? null : new
            {
                envelope.Approval.PolicyId,
                envelope.Approval.Required,
                envelope.Approval.State,
                records = envelope.Approval.Records.Select(item => new
                {
                    item.Channel,
                    item.Actor,
                    item.Decision,
                    recordedAtUnixMs = item.RecordedAt.ToUnixTimeMilliseconds()
                }).ToArray()
            }
        };
        var element = JsonSerializer.SerializeToElement(projection, FabricJson.Options);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            WriteCanonical(writer, element);
        }
        return Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan)).ToLowerInvariant();
    }

    public static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray()) WriteCanonical(writer, item);
                writer.WriteEndArray();
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }
}
