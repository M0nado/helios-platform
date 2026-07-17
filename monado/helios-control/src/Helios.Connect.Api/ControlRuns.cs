using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Helios.Connect.Api;

public sealed record ControlRunRequest(
    string Intent,
    string Environment,
    string? Target = null,
    IReadOnlyList<string>? Connectors = null);

public sealed record ControlRunStep(
    string Name,
    string Status,
    string Detail,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null);

public sealed record ConnectorReceipt(
    string Connector,
    string Status,
    int Attempts,
    string Detail,
    DateTimeOffset? DeliveredAt = null);

public sealed record ConnectorBindingStatus(string Connector, bool Configured, string Mode);

public sealed record ControlRunSnapshot(
    string Id,
    string PartitionKey,
    [property: System.Text.Json.Serialization.JsonIgnore] string RequestSha256,
    string CorrelationId,
    [property: System.Text.Json.Serialization.JsonIgnore] string RequestedBy,
    string Intent,
    string Environment,
    string Target,
    IReadOnlyList<string> Connectors,
    string Status,
    string Mode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ControlRunStep> Steps,
    IReadOnlyList<ConnectorReceipt> Receipts,
    EdgeAutomationPlan? Plan = null,
    string? EvidenceSha256 = null,
    int ResourceCount = 0,
    string? Error = null,
    [property: System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonProperty(PropertyName = "_etag")] string? ETag = null,
    [property: System.Text.Json.Serialization.JsonIgnore] string? LeaseOwner = null,
    [property: System.Text.Json.Serialization.JsonIgnore] DateTimeOffset? LeaseExpiresAt = null);

public interface IControlRunStore
{
    Task<(ControlRunSnapshot Snapshot, bool Created)> CreateOrGetAsync(ControlRunSnapshot snapshot, CancellationToken cancellationToken);
    Task<ControlRunSnapshot?> GetAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ControlRunSnapshot>> ListRunnableAsync(DateTimeOffset now, int maximum, CancellationToken cancellationToken);
    Task<ControlRunSnapshot> ReplaceAsync(ControlRunSnapshot snapshot, string? expectedETag, CancellationToken cancellationToken);
    Task ProbeAsync(CancellationToken cancellationToken);
}

public sealed class InMemoryControlRunStore : IControlRunStore
{
    private readonly ConcurrentDictionary<string, ControlRunSnapshot> _runs = new(StringComparer.Ordinal);

    public Task<(ControlRunSnapshot Snapshot, bool Created)> CreateOrGetAsync(ControlRunSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var created = snapshot with { ETag = Guid.NewGuid().ToString("n") };
        var result = _runs.GetOrAdd(snapshot.Id, created);
        return Task.FromResult((result, ReferenceEquals(result, created)));
    }

    public Task<ControlRunSnapshot?> GetAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _runs.TryGetValue(id, out var snapshot);
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<ControlRunSnapshot>> ListRunnableAsync(DateTimeOffset now, int maximum, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ControlRunSnapshot> snapshots = _runs.Values
            .Where(run => run.Status == "queued" ||
                (run.Status == "running" && (run.LeaseExpiresAt is null || run.LeaseExpiresAt <= now)))
            .OrderBy(run => run.CreatedAt)
            .Take(maximum)
            .ToArray();
        return Task.FromResult(snapshots);
    }

    public Task<ControlRunSnapshot> ReplaceAsync(ControlRunSnapshot snapshot, string? expectedETag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (true)
        {
            if (!_runs.TryGetValue(snapshot.Id, out var current)) throw new KeyNotFoundException("Control run was not found.");
            if (!string.Equals(current.ETag, expectedETag, StringComparison.Ordinal)) throw new ControlRunConcurrencyException();
            var replacement = snapshot with { ETag = Guid.NewGuid().ToString("n") };
            if (_runs.TryUpdate(snapshot.Id, replacement, current)) return Task.FromResult(replacement);
        }
    }

    public Task ProbeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public sealed class CosmosControlRunStore : IControlRunStore
{
    private readonly Container _container;

    public CosmosControlRunStore(IConfiguration configuration)
    {
        var endpoint = configuration["HELIOS_COSMOS_ENDPOINT"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) || endpointUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("HELIOS_COSMOS_ENDPOINT must be an HTTPS endpoint.");
        var clientId = configuration["AZURE_CLIENT_ID"];
        TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId
        });
        var client = new CosmosClient(endpointUri.ToString(), credential, new CosmosClientOptions
        {
            ApplicationName = "helios-connect/control-runs",
            SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
        });
        _container = client.GetContainer(
            configuration["HELIOS_COSMOS_DATABASE"] ?? "helios",
            configuration["HELIOS_COSMOS_CONTAINER"] ?? "control-runs");
    }

    public async Task<(ControlRunSnapshot Snapshot, bool Created)> CreateOrGetAsync(ControlRunSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.CreateItemAsync(snapshot, new PartitionKey(snapshot.PartitionKey), cancellationToken: cancellationToken);
            return (response.Resource with { ETag = response.ETag }, true);
        }
        catch (CosmosException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var existing = await GetAsync(snapshot.Id, cancellationToken);
            return (existing ?? throw new InvalidOperationException("Cosmos reported a duplicate run that could not be read."), false);
        }
    }

    public async Task<ControlRunSnapshot?> GetAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<ControlRunSnapshot>(id, new PartitionKey("control-runs"), cancellationToken: cancellationToken);
            return response.Resource with { ETag = response.ETag };
        }
        catch (CosmosException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ControlRunSnapshot>> ListRunnableAsync(DateTimeOffset now, int maximum, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT TOP @maximum * FROM c WHERE c.partitionKey = @partitionKey AND " +
            "(c.status = 'queued' OR (c.status = 'running' AND " +
            "(NOT IS_DEFINED(c.leaseExpiresAt) OR IS_NULL(c.leaseExpiresAt) OR c.leaseExpiresAt <= @now))) ORDER BY c.createdAt")
            .WithParameter("@maximum", Math.Clamp(maximum, 1, 256))
            .WithParameter("@partitionKey", "control-runs")
            .WithParameter("@now", now.ToUniversalTime().ToString("O"));
        var results = new List<ControlRunSnapshot>();
        using var iterator = _container.GetItemQueryIterator<ControlRunSnapshot>(query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey("control-runs"), MaxItemCount = maximum });
        while (iterator.HasMoreResults && results.Count < maximum)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Resource.Take(maximum - results.Count));
        }
        return results;
    }

    public async Task<ControlRunSnapshot> ReplaceAsync(ControlRunSnapshot snapshot, string? expectedETag, CancellationToken cancellationToken)
    {
        try
        {
            var options = new ItemRequestOptions { IfMatchEtag = expectedETag };
            var response = await _container.ReplaceItemAsync(snapshot with { ETag = null }, snapshot.Id, new PartitionKey(snapshot.PartitionKey), options, cancellationToken);
            return response.Resource with { ETag = response.ETag };
        }
        catch (CosmosException exception) when (exception.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            throw new ControlRunConcurrencyException();
        }
    }

    public async Task ProbeAsync(CancellationToken cancellationToken)
    {
        _ = await _container.ReadContainerAsync(cancellationToken: cancellationToken);
    }
}

public sealed class ControlRunConcurrencyException : Exception
{
    public ControlRunConcurrencyException() : base("The control run changed concurrently.") { }
}

public sealed class ControlRunIdempotencyConflictException : Exception
{
    public ControlRunIdempotencyConflictException() : base("The Idempotency-Key was already used for a different control run request.") { }
}

public interface IConnectorDispatcher
{
    IReadOnlyList<ConnectorBindingStatus> GetStatus();
    Task<IReadOnlyList<ConnectorReceipt>> DispatchAsync(ControlRunSnapshot run, CancellationToken cancellationToken);
}

public sealed class ConnectorDispatcher(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IConnectorDispatcher
{
    private static readonly string[] AllowedConnectors = ["github", "linear", "slack", "sharepoint", "teams", "copilot"];

    public IReadOnlyList<ConnectorBindingStatus> GetStatus()
    {
        var mode = IsLive ? "live" : "dry-run";
        return AllowedConnectors.Select(name => new ConnectorBindingStatus(name, TryReadBinding(name, out _, out _), mode)).ToArray();
    }

    public async Task<IReadOnlyList<ConnectorReceipt>> DispatchAsync(ControlRunSnapshot run, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("helios-connectors");
        var receipts = new List<ConnectorReceipt>();
        foreach (var connector in run.Connectors.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!AllowedConnectors.Contains(connector, StringComparer.OrdinalIgnoreCase))
            {
                receipts.Add(new(connector, "rejected", 0, "Connector is not in the governed allowlist."));
                continue;
            }
            if (!TryReadBinding(connector, out var endpoint, out var secret))
            {
                receipts.Add(new(connector, "unbound", 0, "No HTTPS relay and HMAC binding is configured."));
                continue;
            }
            if (!IsLive)
            {
                receipts.Add(new(connector, "dry-run", 0, "Binding verified; delivery is disabled until HELIOS_CONNECTOR_DELIVERY_MODE=live."));
                continue;
            }

            var occurredAt = DateTimeOffset.UtcNow;
            var payload = JsonSerializer.Serialize(new
            {
                schema = "helios.connectorDelivery.v1",
                connector,
                runId = run.Id,
                run.CorrelationId,
                run.Status,
                run.Intent,
                run.Environment,
                run.Target,
                run.EvidenceSha256,
                run.ResourceCount,
                planId = run.Plan?.PlanId,
                occurredAt
            });
            var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret!), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            var keyId = configuration[$"HELIOS_CONNECTOR_{connector.ToUpperInvariant()}_HMAC_KEY_ID"] ?? "v1";
            if (!IsSafeKeyId(keyId))
            {
                receipts.Add(new(connector, "failed", 0, "Relay HMAC key ID is invalid."));
                continue;
            }
            ConnectorReceipt? receipt = null;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    request.Headers.Add("X-Helios-Idempotency-Key", $"{run.Id}:{connector}");
                    request.Headers.Add("X-Helios-Correlation-Id", run.CorrelationId);
                    request.Headers.Add("X-Helios-Signature", $"sha256={signature}");
                    request.Headers.Add("X-Helios-Timestamp", occurredAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
                    request.Headers.Add("X-Helios-Key-Id", keyId);
                    request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var response = await httpClient.SendAsync(request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        receipt = new(connector, "delivered", attempt, "Relay accepted the normalized event.", DateTimeOffset.UtcNow);
                        break;
                    }
                    receipt = new(connector, "failed", attempt, $"Relay returned HTTP {(int)response.StatusCode}.");
                    var statusCode = (int)response.StatusCode;
                    var retryable = statusCode is 408 or 429 || statusCode >= 500;
                    if (!retryable) break;
                }
                catch (HttpRequestException)
                {
                    receipt = new(connector, "failed", attempt, "Relay request failed without exposing transport details.");
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    receipt = new(connector, "failed", attempt, "Relay request timed out.");
                }
                if (attempt < 3) await Task.Delay(TimeSpan.FromMilliseconds(attempt * 250), cancellationToken);
            }
            receipts.Add(receipt ?? new(connector, "failed", 3, "Relay delivery failed."));
        }
        return receipts;
    }

    private bool IsLive => string.Equals(configuration["HELIOS_CONNECTOR_DELIVERY_MODE"], "live", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeKeyId(string value) => value.Length is >= 1 and <= 64 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    private bool TryReadBinding(string connector, out Uri? endpoint, out string? secret)
    {
        endpoint = null;
        secret = null;
        var prefix = $"HELIOS_CONNECTOR_{connector.ToUpperInvariant()}";
        var rawEndpoint = configuration[$"{prefix}_URL"];
        secret = configuration[$"{prefix}_HMAC_SECRET"];
        var allowedHosts = (configuration[$"{prefix}_ALLOWED_HOSTS"] ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Uri.TryCreate(rawEndpoint, UriKind.Absolute, out endpoint)
            && endpoint.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(endpoint.UserInfo)
            && Uri.CheckHostName(endpoint.Host) == UriHostNameType.Dns
            && !endpoint.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && !endpoint.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            && !endpoint.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            && allowedHosts.Any(host => string.Equals(host, endpoint.Host, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(secret)
            && Encoding.UTF8.GetByteCount(secret) >= 32;
    }
}

public sealed partial class ControlRunCoordinator(
    IControlRunStore store,
    IAzureInventoryService inventory,
    IEdgeAutomationPlanner planner,
    IConnectorDispatcher dispatcher,
    ILogger<ControlRunCoordinator> logger) : BackgroundService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RecoveryInterval = TimeSpan.FromSeconds(5);
    private static readonly HashSet<string> Environments = new(StringComparer.OrdinalIgnoreCase) { "dev", "test", "preview", "prod" };
    private static readonly HashSet<string> Intents = new(StringComparer.OrdinalIgnoreCase) { "provision-resources", "cleanup-owned-resources" };
    private static readonly HashSet<string> Connectors = new(StringComparer.OrdinalIgnoreCase) { "github", "linear", "slack", "sharepoint", "teams", "copilot" };
    private readonly Channel<string> _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(256)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():n}";

    public async Task<ControlRunSnapshot> StartAsync(ControlRunRequest request, string idempotencyKey, string requestedBy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!IdempotencyPattern().IsMatch(idempotencyKey)) throw new ArgumentException("Idempotency-Key must be 8-128 safe characters.", nameof(idempotencyKey));
        var intent = Normalize(request.Intent, "intent", 64).ToLowerInvariant();
        if (!Intents.Contains(intent)) throw new ArgumentException("One-button runs support provision-resources or cleanup-owned-resources.", nameof(request.Intent));
        var environment = Normalize(request.Environment, "environment", 16).ToLowerInvariant();
        if (!Environments.Contains(environment)) throw new ArgumentException("Environment must be dev, test, preview, or prod.", nameof(request.Environment));
        var context = inventory.GetContext();
        var target = string.IsNullOrWhiteSpace(request.Target) ? context.ResourceGroup : Normalize(request.Target, "target", 90);
        if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("A configured or requested resource group is required.", nameof(request.Target));
        var requestedConnectors = request.Connectors ?? ["github", "linear", "slack", "sharepoint"];
        var normalizedConnectors = requestedConnectors.Select(value => Normalize(value, "connector", 32).ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (normalizedConnectors.Any(value => !Connectors.Contains(value))) throw new ArgumentException("A connector is not in the governed allowlist.", nameof(request.Connectors));
        requestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "authorized-user" : Normalize(requestedBy, "requestedBy", 128);
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{requestedBy}\n{idempotencyKey}"))).ToLowerInvariant()[..32];
        var canonicalRequest = JsonSerializer.Serialize(new
        {
            intent,
            environment,
            target = target.ToLowerInvariant(),
            connectors = normalizedConnectors
        });
        var requestSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var steps = new[]
        {
            Step("context", "queued", "Verify the configured Azure boundary."),
            Step("inventory", "queued", "Read non-secret resource metadata."),
            Step("plan", "queued", "Create a deterministic governed plan."),
            Step("evidence", "queued", "Hash the normalized plan and inventory summary."),
            Step("connectors", "queued", "Deliver idempotent status receipts through configured relays."),
            Step("approval", "queued", "Stop at the protected approval boundary.")
        };
        var candidate = new ControlRunSnapshot(id, "control-runs", requestSha256, Guid.NewGuid().ToString("n"), requestedBy,
            intent, environment, target, normalizedConnectors, "queued", "diagnose-plan-sync", now, now, steps, []);
        var (snapshot, created) = await store.CreateOrGetAsync(candidate, cancellationToken);
        if (!created &&
            (!string.Equals(snapshot.RequestSha256, requestSha256, StringComparison.Ordinal) ||
             !string.Equals(snapshot.RequestedBy, requestedBy, StringComparison.Ordinal) ||
             !string.Equals(snapshot.Intent, intent, StringComparison.Ordinal)))
            throw new ControlRunIdempotencyConflictException();
        if (snapshot.Status == "queued") _queue.Writer.TryWrite(snapshot.Id);
        return snapshot;
    }

    public Task<ControlRunSnapshot?> GetAsync(string id, CancellationToken cancellationToken) =>
        RunIdPattern().IsMatch(id) ? store.GetAsync(id, cancellationToken) : Task.FromResult<ControlRunSnapshot?>(null);

    public async Task<ControlRunSnapshot?> GetAsync(string id, string requestedBy, CancellationToken cancellationToken)
    {
        var snapshot = await GetAsync(id, cancellationToken);
        return snapshot is not null && string.Equals(snapshot.RequestedBy, requestedBy, StringComparison.Ordinal)
            ? snapshot
            : null;
    }

    public async Task<ControlRunSnapshot?> ResumeAsync(string id, CancellationToken cancellationToken)
    {
        var snapshot = await GetAsync(id, cancellationToken);
        if (snapshot is null) return null;
        if (snapshot.Status != "failed") return snapshot;
        var reset = snapshot with { Status = "queued", Error = null, LeaseOwner = null, LeaseExpiresAt = null, UpdatedAt = DateTimeOffset.UtcNow };
        try { snapshot = await ReplaceAsync(snapshot, reset, cancellationToken); }
        catch (ControlRunConcurrencyException) { return await GetAsync(id, cancellationToken); }
        _queue.Writer.TryWrite(id);
        return snapshot;
    }

    public async Task<ControlRunSnapshot?> ResumeAsync(string id, string requestedBy, CancellationToken cancellationToken)
    {
        var snapshot = await GetAsync(id, requestedBy, cancellationToken);
        return snapshot is null ? null : await ResumeAsync(snapshot.Id, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(ConsumeQueueAsync(stoppingToken), RecoverRunnableAsync(stoppingToken));
    }

    private async Task ConsumeQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try { await ProcessAsync(id, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (ControlRunConcurrencyException)
            {
                logger.LogInformation("Control run {RunId} lease or ETag ownership moved to another worker.", id);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Control run {RunId} failed.", id);
                await FailAsync(id, stoppingToken);
            }
        }
    }

    private async Task RecoverRunnableAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RecoveryInterval);
        try
        {
            do
            {
                try
                {
                    var runnable = await store.ListRunnableAsync(DateTimeOffset.UtcNow, 256, stoppingToken);
                    foreach (var run in runnable) _queue.Writer.TryWrite(run.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Control run recovery scan failed; the next bounded scan will retry.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task ProcessAsync(string id, CancellationToken cancellationToken)
    {
        var run = await store.GetAsync(id, cancellationToken);
        if (run is null || run.Status is "completed" or "awaiting-approval" or "failed") return;
        var now = DateTimeOffset.UtcNow;
        if (run.Status == "running" && run.LeaseExpiresAt > now) return;
        if (run.Status is not ("queued" or "running")) return;
        try
        {
            run = await ReplaceAsync(run, run with
            {
                Status = "running",
                LeaseOwner = _workerId,
                LeaseExpiresAt = now.Add(LeaseDuration),
                UpdatedAt = now
            }, cancellationToken);
        }
        catch (ControlRunConcurrencyException) { return; }

        run = await SetStepAsync(run, "context", "running", "Verifying tenant, subscription, and resource group.", cancellationToken);
        var context = inventory.GetContext();
        if (!context.Configured || !string.Equals(context.ResourceGroup, run.Target, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The server Azure context does not match the requested resource group.");
        run = await SetStepAsync(run, "context", "completed", "Configured Azure boundary verified.", cancellationToken);

        run = await SetStepAsync(run, "inventory", "running", "Reading Azure resource metadata with managed identity.", cancellationToken);
        var resources = await inventory.ListResourcesAsync(null, cancellationToken);
        run = await ReplaceAsync(run, run with { ResourceCount = resources.Count, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken);
        run = await SetStepAsync(run, "inventory", "completed", $"Read metadata for {resources.Count} resources.", cancellationToken);

        run = await SetStepAsync(run, "plan", "running", "Creating deterministic plan-only automation.", cancellationToken);
        var plan = planner.CreatePlan(new EdgeAutomationRequest(run.Intent, run.Environment, run.Target, "all"));
        run = await ReplaceAsync(run, run with { Plan = plan, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken);
        run = await SetStepAsync(run, "plan", "completed", $"Plan {plan.PlanId[..12]} created; apply is unavailable from Edge.", cancellationToken);

        run = await SetStepAsync(run, "evidence", "running", "Canonicalizing non-secret evidence.", cancellationToken);
        var resourceEvidence = resources.OrderBy(resource => resource.Id, StringComparer.Ordinal)
            .Select(resource => new
            {
                resource.Id,
                resource.Name,
                resource.Type,
                resource.Location,
                tags = resource.Tags?.OrderBy(tag => tag.Key, StringComparer.Ordinal)
            });
        var canonical = JsonSerializer.Serialize(new
        {
            schema = "helios.orchestrationEvidence.v1",
            azure = new { context.TenantId, context.SubscriptionId, context.ResourceGroup },
            request = new { run.Intent, run.Environment, run.Target, run.Connectors },
            plan,
            resources = resourceEvidence
        });
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        run = await ReplaceAsync(run, run with { EvidenceSha256 = digest, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken);
        run = await SetStepAsync(run, "evidence", "completed", $"Evidence SHA-256 {digest}.", cancellationToken);

        run = await SetStepAsync(run, "connectors", "running", "Reconciling external status receipts.", cancellationToken);
        var awaitsApproval = plan.Steps.Any(step => step.Mutating);
        var receipts = await dispatcher.DispatchAsync(run with { Status = awaitsApproval ? "awaiting-approval" : "completed" }, cancellationToken);
        run = await ReplaceAsync(run, run with { Receipts = receipts, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken);
        run = await SetStepAsync(run, "connectors", "completed", $"Recorded {receipts.Count} connector receipts.", cancellationToken);

        run = await SetStepAsync(run, "approval", "running", "Evaluating protected mutation gates.", cancellationToken);
        run = await SetStepAsync(run, "approval", "completed", awaitsApproval
            ? "Stopped before mutation. Review what-if evidence and approve through the protected workflow."
            : "No mutating step is present.", cancellationToken);
        await ReplaceAsync(run, run with
        {
            Status = awaitsApproval ? "awaiting-approval" : "completed",
            LeaseOwner = null,
            LeaseExpiresAt = null,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private async Task FailAsync(string id, CancellationToken cancellationToken)
    {
        var run = await store.GetAsync(id, cancellationToken);
        if (run is null || run.Status is "completed" or "awaiting-approval" ||
            !string.Equals(run.LeaseOwner, _workerId, StringComparison.Ordinal)) return;
        try
        {
            await ReplaceAsync(run, run with
            {
                Status = "failed",
                Error = "The run failed. Review server telemetry using the correlation ID; secret-bearing exception text is never returned.",
                LeaseOwner = null,
                LeaseExpiresAt = null,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        catch (ControlRunConcurrencyException) { }
    }

    private async Task<ControlRunSnapshot> SetStepAsync(ControlRunSnapshot run, string name, string status, string detail, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var steps = run.Steps.Select(step => step.Name == name
            ? step with
            {
                Status = status,
                Detail = detail,
                StartedAt = step.StartedAt ?? (status == "running" ? now : null),
                CompletedAt = status == "completed" ? now : step.CompletedAt
            }
            : step).ToArray();
        return await ReplaceAsync(run, run with { Steps = steps, UpdatedAt = now }, cancellationToken);
    }

    private Task<ControlRunSnapshot> ReplaceAsync(ControlRunSnapshot current, ControlRunSnapshot replacement, CancellationToken cancellationToken)
    {
        if (string.Equals(current.LeaseOwner, _workerId, StringComparison.Ordinal) && replacement.Status == "running")
            replacement = replacement with { LeaseOwner = _workerId, LeaseExpiresAt = DateTimeOffset.UtcNow.Add(LeaseDuration) };
        return store.ReplaceAsync(replacement, current.ETag, cancellationToken);
    }

    private static ControlRunStep Step(string name, string status, string detail) => new(name, status, detail);

    private static string Normalize(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
        var normalized = value.Trim();
        if (normalized.Length > maxLength || normalized.Any(char.IsControl)) throw new ArgumentException($"{name} is invalid.", name);
        return normalized;
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]{8,128}$")] private static partial Regex IdempotencyPattern();
    [GeneratedRegex("^[0-9a-f]{32}$")] private static partial Regex RunIdPattern();
}
