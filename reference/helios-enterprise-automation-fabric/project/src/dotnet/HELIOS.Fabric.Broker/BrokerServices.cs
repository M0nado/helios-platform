using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using HELIOS.Fabric.Contracts;

namespace HELIOS.Fabric.Broker.Security
{

public sealed class HmacVerifier(TimeProvider timeProvider)
{
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromMinutes(5);

    public bool VerifyGitHub(string secret, ReadOnlySpan<byte> body, string signature)
    {
        if (!signature.StartsWith("sha256=", StringComparison.Ordinal)) return false;
        return VerifyHex(secret, body, signature["sha256=".Length..]);
    }

    public bool VerifySlack(string secret, ReadOnlySpan<byte> body, string timestamp, string signature)
    {
        if (!ValidTimestamp(timestamp) || !signature.StartsWith("v0=", StringComparison.Ordinal)) return false;
        var prefix = Encoding.UTF8.GetBytes($"v0:{timestamp}:");
        var signed = new byte[prefix.Length + body.Length];
        prefix.CopyTo(signed, 0);
        body.CopyTo(signed.AsSpan(prefix.Length));
        return VerifyHex(secret, signed, signature["v0=".Length..]);
    }

    public bool VerifyLinear(string secret, ReadOnlySpan<byte> body, string signature, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;
        if (VerifyHex(secret, body, signature)) return true;
        if (string.IsNullOrWhiteSpace(timestamp)) return false;
        var prefix = Encoding.UTF8.GetBytes($"{timestamp}.");
        var signed = new byte[prefix.Length + body.Length];
        prefix.CopyTo(signed, 0);
        body.CopyTo(signed.AsSpan(prefix.Length));
        return VerifyHex(secret, signed, signature);
    }

    public bool VerifyTeams(string secret, ReadOnlySpan<byte> body, string timestamp, string signature)
    {
        if (!ValidTimestamp(timestamp) || string.IsNullOrWhiteSpace(signature)) return false;
        var prefix = Encoding.UTF8.GetBytes($"{timestamp}.");
        var signed = new byte[prefix.Length + body.Length];
        prefix.CopyTo(signed, 0);
        body.CopyTo(signed.AsSpan(prefix.Length));
        return VerifyHex(secret, signed, signature.StartsWith("sha256=", StringComparison.Ordinal) ? signature[7..] : signature);
    }

    public static bool FixedEquals(string expected, string supplied)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied)) return false;
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied)));
    }

    private bool ValidTimestamp(string value)
    {
        if (!long.TryParse(value, out var unix)) return false;
        var supplied = DateTimeOffset.FromUnixTimeSeconds(unix);
        return (timeProvider.GetUtcNow() - supplied).Duration() <= ReplayWindow;
    }

    private static bool VerifyHex(string secret, ReadOnlySpan<byte> value, string signature)
    {
        if (string.IsNullOrWhiteSpace(secret) || signature.Length != 64) return false;
        byte[] supplied;
        try
        {
            supplied = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return false;
        }
        if (supplied.Length != 32) return false;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = hmac.ComputeHash(value.ToArray());
        return CryptographicOperations.FixedTimeEquals(computed, supplied);
    }
}
}

namespace HELIOS.Fabric.Broker.Services
{

public interface ISecretResolver
{
    Task<string?> GetAsync(string environmentName, string vaultName, CancellationToken cancellationToken);
}

public sealed class KeyVaultSecretResolver(SecretClient client)
{
    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            return (await client.GetSecretAsync(name, cancellationToken: cancellationToken)).Value.Value;
        }
        catch (Azure.RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }
}

public sealed class CompositeSecretResolver(KeyVaultSecretResolver? vault, bool allowEnvironment) : ISecretResolver
{
    public async Task<string?> GetAsync(string environmentName, string vaultName, CancellationToken cancellationToken)
    {
        if (vault is not null)
        {
            var value = await vault.GetAsync(vaultName, cancellationToken);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return allowEnvironment ? Environment.GetEnvironmentVariable(environmentName) : null;
    }
}

public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken);
}

public sealed class InMemoryEventPublisher : IEventPublisher
{
    private readonly List<EventEnvelope> _events = [];
    public IReadOnlyList<EventEnvelope> Events => _events;
    public Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_events) _events.Add(envelope);
        return Task.CompletedTask;
    }
}

public sealed class ServiceBusEventPublisher(ServiceBusClient client, string topic) : IEventPublisher
{
    public async Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        await using var sender = client.CreateSender(topic);
        var message = new ServiceBusMessage(new BinaryData(JsonSerializer.SerializeToUtf8Bytes(envelope, FabricJson.Options)))
        {
            MessageId = envelope.Provenance.IdempotencyKey,
            CorrelationId = envelope.CorrelationId.ToString(),
            Subject = envelope.EventType,
            ContentType = "application/json"
        };
        await sender.SendMessageAsync(message, cancellationToken);
    }
}

public sealed class WebhookNormalizer(IConfiguration configuration, TimeProvider timeProvider)
{
    public EventEnvelope Normalize(
        string source,
        string eventType,
        string subject,
        string summary,
        JsonElement payload,
        string actor,
        string idempotencyKey,
        IReadOnlyList<string> requestedActions,
        string? repository = null,
        string? commitSha = null,
        string? workflow = null,
        string? workflowRunId = null)
    {
        var eventId = CanonicalHash.DeterministicGuid(idempotencyKey);
        var envelope = new EventEnvelope
        {
            EventId = eventId,
            CorrelationId = eventId,
            EventType = eventType.ToLowerInvariant(),
            Source = source.ToLowerInvariant(),
            Subject = subject,
            OccurredAt = timeProvider.GetUtcNow(),
            Environment = configuration["HELIOS_ENVIRONMENT"] ?? "dev",
            Severity = configuration["HELIOS_DEFAULT_SEVERITY"] ?? "info",
            Sensitivity = configuration["HELIOS_DEFAULT_SENSITIVITY"] ?? "confidential",
            Summary = summary,
            Provenance = new EventProvenance
            {
                Repository = repository,
                CommitSha = commitSha,
                Ref = configuration["GITHUB_REF"],
                Workflow = workflow,
                WorkflowRunId = workflowRunId,
                Actor = actor,
                IdempotencyKey = idempotencyKey,
                ContentSha256 = string.Empty
            },
            Payload = payload.Clone(),
            RequestedActions = requestedActions.Distinct(StringComparer.Ordinal).ToArray(),
            Links = []
        };
        return envelope with
        {
            Provenance = envelope.Provenance with
            {
                ContentSha256 = CanonicalHash.ComputeEventContentSha256(envelope)
            }
        };
    }
}

public sealed class CorrelationStatusReader(BlobServiceClient client)
{
    public async Task<JsonDocument?> ReadAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        var blob = client.GetBlobContainerClient("connector-state")
            .GetBlobClient($"correlations/{correlationId}/latest.json");
        if (!(await blob.ExistsAsync(cancellationToken)).Value) return null;
        var download = await blob.DownloadContentAsync(cancellationToken);
        return JsonDocument.Parse(download.Value.Content.ToMemory());
    }
}
}
