using System.Text.Json;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using HELIOS.Fabric.Contracts;
using HELIOS.Fabric.Worker.Delivery;

namespace HELIOS.Fabric.Worker;

public sealed class FabricWorker(
    ServiceBusClient client,
    IConfiguration configuration,
    ConnectorConfiguration connectors,
    IEnumerable<IDeliverySink> sinks,
    BlobServiceClient blobService,
    ILogger<FabricWorker> logger) : BackgroundService
{
    private readonly IReadOnlyDictionary<string, IDeliverySink> _sinks =
        sinks.ToDictionary(item => item.ConnectorId, StringComparer.Ordinal);
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topic = configuration["HELIOS_SERVICEBUS_TOPIC"] ?? "helios-events";
        var subscription = configuration["HELIOS_SERVICEBUS_SUBSCRIPTION"] ?? "delivery-worker";
        _processor = client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 4,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10),
            PrefetchCount = 20
        });
        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;
        await _processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        EventEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope>(args.Message.Body.ToMemory().Span, FabricJson.Options)
                ?? throw new JsonException("Empty event envelope.");
            ValidateEnvelope(envelope);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Dead-lettering malformed HELIOS event {MessageId}", args.Message.MessageId);
            await args.DeadLetterMessageAsync(args.Message, "InvalidEventEnvelope", Truncate(exception.Message, 1024), args.CancellationToken);
            return;
        }

        var routes = connectors.Resolve(envelope);
        var failures = new List<string>();
        var delivered = new List<string>();
        var evidenceContainer = blobService.GetBlobContainerClient("automation-evidence");

        foreach (var route in routes)
        {
            if (!_sinks.TryGetValue(route.ConnectorId, out var sink))
            {
                failures.Add($"No sink registered for {route.ConnectorId}");
                continue;
            }
            if (await ReceiptExistsAsync(evidenceContainer, envelope, route.ConnectorId, args.CancellationToken))
            {
                delivered.Add(route.ConnectorId);
                continue;
            }
            try
            {
                await sink.DeliverAsync(envelope, route, args.CancellationToken);
                await WriteReceiptAsync(evidenceContainer, envelope, route, args.CancellationToken);
                delivered.Add(route.ConnectorId);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Delivery failed for {ConnectorId} and event {EventId}", route.ConnectorId, envelope.EventId);
                failures.Add($"{route.ConnectorId}: {exception.GetType().Name}");
            }
        }

        if (failures.Count > 0)
        {
            await args.AbandonMessageAsync(
                args.Message,
                new Dictionary<string, object> { ["heliosFailures"] = Truncate(string.Join("; ", failures), 1024) },
                args.CancellationToken);
            return;
        }

        await WriteEvidenceAsync(evidenceContainer, envelope, routes, delivered, args.CancellationToken);
        await WriteCorrelationStatusAsync(blobService.GetBlobContainerClient("connector-state"), envelope, routes, delivered, args.CancellationToken);
        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private static void ValidateEnvelope(EventEnvelope envelope)
    {
        if (envelope.SpecVersion != "1.0") throw new InvalidOperationException("Unsupported event specVersion.");
        if (string.IsNullOrWhiteSpace(envelope.EventType)) throw new InvalidOperationException("Missing event type.");
        if (string.IsNullOrWhiteSpace(envelope.Provenance.IdempotencyKey)) throw new InvalidOperationException("Missing idempotency key.");
        if (!string.Equals(
                CanonicalHash.ComputeEventContentSha256(envelope),
                envelope.Provenance.ContentSha256,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Event content hash mismatch.");
        }
    }

    private static string ReceiptPath(EventEnvelope envelope, string connectorId) =>
        $"receipts/{envelope.EventId}/{connectorId}.json";

    private static async Task<bool> ReceiptExistsAsync(
        BlobContainerClient container,
        EventEnvelope envelope,
        string connectorId,
        CancellationToken cancellationToken) =>
        (await container.GetBlobClient(ReceiptPath(envelope, connectorId)).ExistsAsync(cancellationToken)).Value;

    private static async Task WriteReceiptAsync(
        BlobContainerClient container,
        EventEnvelope envelope,
        DeliveryRoute route,
        CancellationToken cancellationToken)
    {
        var receipt = JsonSerializer.SerializeToUtf8Bytes(new
        {
            envelope.EventId,
            envelope.CorrelationId,
            connectorId = route.ConnectorId,
            route.Transform,
            route.ApprovalPolicy,
            deliveredAt = DateTimeOffset.UtcNow,
            eventContentSha256 = envelope.Provenance.ContentSha256
        }, FabricJson.Options);
        var blob = container.GetBlobClient(ReceiptPath(envelope, route.ConnectorId));
        try
        {
            await blob.UploadAsync(new BinaryData(receipt), overwrite: false, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            // Another retry or worker completed the same deterministic receipt.
        }
    }

    private static async Task WriteEvidenceAsync(
        BlobContainerClient container,
        EventEnvelope envelope,
        IReadOnlyList<DeliveryRoute> routes,
        IReadOnlyList<string> delivered,
        CancellationToken cancellationToken)
    {
        var date = envelope.OccurredAt.UtcDateTime.ToString("yyyy/MM/dd");
        var blob = container.GetBlobClient($"events/{date}/{envelope.CorrelationId}/{envelope.EventId}.json");
        var evidence = JsonSerializer.SerializeToUtf8Bytes(new
        {
            envelope,
            selected = routes.Select(item => item.ConnectorId).ToArray(),
            delivered = delivered.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            completedAt = DateTimeOffset.UtcNow
        }, FabricJson.Options);
        try
        {
            await blob.UploadAsync(new BinaryData(evidence), overwrite: false, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            // Evidence is immutable and was already written by a previous delivery attempt.
        }
    }


    private static async Task WriteCorrelationStatusAsync(
        BlobContainerClient container,
        EventEnvelope envelope,
        IReadOnlyList<DeliveryRoute> routes,
        IReadOnlyList<string> delivered,
        CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient($"correlations/{envelope.CorrelationId}/latest.json");
        var status = JsonSerializer.SerializeToUtf8Bytes(new
        {
            envelope.CorrelationId,
            latestEventId = envelope.EventId,
            envelope.EventType,
            envelope.Environment,
            envelope.Severity,
            envelope.Summary,
            sourceSha = envelope.Provenance.CommitSha,
            contentSha256 = envelope.Provenance.ContentSha256,
            selected = routes.Select(item => item.ConnectorId).ToArray(),
            delivered = delivered.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            updatedAt = DateTimeOffset.UtcNow
        }, FabricJson.Options);
        await blob.UploadAsync(new BinaryData(status), overwrite: true, cancellationToken: cancellationToken);
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus processor error in {ErrorSource} entity {EntityPath}", args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }

    private static string Truncate(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum];
}
