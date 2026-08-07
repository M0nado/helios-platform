using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Azure.Core;
using Azure.Identity;
using Helios.Connect.Contracts;
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
    string LifecycleState = "INIT",
    int AttemptCount = 0,
    int HopCount = 0,
    string ApprovalState = "pending",
    string PolicyVersion = "1.0.0-default",
    string TenantId = "unknown-tenant",
    string Repository = "M0nado/helios-platform",
    string ArtifactDigest = "unavailable",
    string? IncidentSeverity = null,
    string? IncidentFingerprint = null,
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

public sealed class CosmosControlRunStore : IControlRunStore, IDisposable
{
    private readonly CosmosClient _client;
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
        _client = new CosmosClient(endpointUri.ToString(), credential, new CosmosClientOptions
        {
            ApplicationName = "helios-connect/control-runs",
            SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
        });
        _container = _client.GetContainer(
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

    public void Dispose()
    {
        _client.Dispose();
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

            // Retries must reproduce the exact body for the same idempotency key.
            var occurredAt = run.CreatedAt;
            var envelope = new HeliosEvent(
                Id: $"{run.Id}:{connector}:status",
                Type: "helios.control-run.status",
                Source: "helios-control",
                Subject: $"control-runs/{run.Id}",
                OccurredAt: occurredAt,
                CorrelationId: run.CorrelationId,
                TraceParent: null,
                DataClassification: "internal",
                Payload: new Dictionary<string, object?>
                {
                    ["schema"] = "helios.connectorDelivery.v1",
                    ["schemaVersion"] = "1.0",
                    ["connector"] = connector,
                    ["runId"] = run.Id,
                    ["status"] = run.Status,
                    ["lifecycleState"] = run.LifecycleState,
                    ["approvalState"] = run.ApprovalState,
                    ["intent"] = run.Intent,
                    ["environment"] = run.Environment,
                    ["target"] = run.Target,
                    ["tenant"] = run.TenantId,
                    ["repository"] = run.Repository,
                    ["artifactDigest"] = run.ArtifactDigest,
                    ["policyVersion"] = run.PolicyVersion,
                    ["attemptCount"] = run.AttemptCount,
                    ["hopCount"] = run.HopCount,
                    ["incidentSeverity"] = run.IncidentSeverity,
                    ["incidentFingerprint"] = run.IncidentFingerprint,
                    ["evidenceSha256"] = run.EvidenceSha256,
                    ["resourceCount"] = run.ResourceCount,
                    ["planId"] = run.Plan?.PlanId
                });
            var payload = JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var idempotencyKey = $"{run.Id}:{connector}";
            var timestamp = occurredAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var signedEnvelope = $"{timestamp}\n{idempotencyKey}\n{payload}";
            var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret!), Encoding.UTF8.GetBytes(signedEnvelope))).ToLowerInvariant();
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
                    request.Headers.Add("X-Helios-Idempotency-Key", idempotencyKey);
                    request.Headers.Add("X-Helios-Correlation-Id", run.CorrelationId);
                    request.Headers.Add("X-Helios-Signature", $"sha256={signature}");
                    request.Headers.Add("X-Helios-Timestamp", timestamp);
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
                if (attempt < 3) await Task.Delay(TimeSpan.FromMilliseconds(attempt * 250d), cancellationToken);
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
        if (!Uri.TryCreate(rawEndpoint, UriKind.Absolute, out var parsedEndpoint)) return false;
        endpoint = parsedEndpoint;
        var host = parsedEndpoint.Host;
        return parsedEndpoint.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(parsedEndpoint.UserInfo)
            && Uri.CheckHostName(host) == UriHostNameType.Dns
            && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            && allowedHosts.Any(allowedHost => string.Equals(allowedHost, host, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(secret)
            && Encoding.UTF8.GetByteCount(secret) >= 32;
    }
}

public sealed record ControlRunCoordinatorTiming(
    TimeSpan LeaseDuration,
    TimeSpan HeartbeatInterval,
    TimeSpan RecoveryInterval)
{
    public static ControlRunCoordinatorTiming Default { get; } = new(
        TimeSpan.FromMinutes(2),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(5));

    public static ControlRunCoordinatorTiming Validate(ControlRunCoordinatorTiming? timing)
    {
        var value = timing ?? Default;
        if (value.LeaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timing), "Lease duration must be positive.");
        if (value.HeartbeatInterval <= TimeSpan.Zero || value.HeartbeatInterval > value.LeaseDuration / 3)
            throw new ArgumentOutOfRangeException(nameof(timing), "Heartbeat interval must be positive and no more than one third of the lease duration.");
        if (value.RecoveryInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timing), "Recovery interval must be positive.");
        return value;
    }
}

public sealed partial class ControlRunCoordinator(
    IControlRunStore store,
    IAzureInventoryService inventory,
    IEdgeAutomationPlanner planner,
    IConnectorDispatcher dispatcher,
    ILogger<ControlRunCoordinator> logger,
    ControlRunCoordinatorTiming? timing = null,
    AgentCorePolicy? policy = null) : BackgroundService
{
    private const string DefaultRepository = "M0nado/helios-platform";
    private static readonly HashSet<string> Environments = new(StringComparer.OrdinalIgnoreCase) { "x-tier-dev", "x-tier-xcore", "x-tier-prod" };
    private static readonly Dictionary<string, string> LegacyEnvironmentAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dev"] = "x-tier-dev",
        ["azure-dev"] = "x-tier-dev",
        ["test"] = "x-tier-xcore",
        ["preview"] = "x-tier-xcore",
        ["azure-test"] = "x-tier-xcore",
        ["prod"] = "x-tier-prod",
        ["azure-prod"] = "x-tier-prod"
    };
    private static readonly HashSet<string> Intents = new(StringComparer.OrdinalIgnoreCase) { "provision-resources", "cleanup-owned-resources" };
    private static readonly HashSet<string> Connectors = new(StringComparer.OrdinalIgnoreCase) { "github", "linear", "slack", "sharepoint", "teams", "copilot" };
    private readonly ControlRunCoordinatorTiming _timing = ControlRunCoordinatorTiming.Validate(timing);
    private readonly AgentCorePolicy _policy = policy ?? AgentCorePolicy.Default;
    private readonly Channel<string> _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(256)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():n}";
    private readonly SemaphoreSlim _leaseWriteGate = new(1, 1);

    public async Task<ControlRunSnapshot> StartAsync(ControlRunRequest request, string idempotencyKey, string requestedBy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _policy.Validate();
        if (!IdempotencyPattern().IsMatch(idempotencyKey)) throw new ArgumentException("Idempotency-Key must be 8-128 safe characters.", nameof(idempotencyKey));
        var intent = Normalize(request.Intent, "intent", 64).ToLowerInvariant();
        if (!Intents.Contains(intent)) throw new ArgumentException("One-button runs support provision-resources or cleanup-owned-resources.", nameof(request.Intent));
        var requestedEnvironment = Normalize(request.Environment, "environment", 16).ToLowerInvariant();
        var environment = NormalizePersistedEnvironmentName(requestedEnvironment);
        if (!Environments.Contains(environment)) throw new ArgumentException("Environment must be x-tier-dev, x-tier-xcore, or x-tier-prod.", nameof(request.Environment));
        var context = inventory.GetContext();
        var target = context.ResourceGroup;
        if (!context.Configured || string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("The server Azure resource-group boundary is not configured.");
        if (!string.IsNullOrWhiteSpace(request.Target))
        {
            var requestedTarget = Normalize(request.Target, "target", 90);
            if (!string.Equals(requestedTarget, target, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Control runs cannot override the configured Azure resource group.", nameof(request.Target));
        }
        var requestedConnectors = request.Connectors ?? ["github", "linear", "slack", "sharepoint"];
        var normalizedConnectors = requestedConnectors.Select(value => Normalize(value, "connector", 32).ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (normalizedConnectors.Any(value => !Connectors.Contains(value))) throw new ArgumentException("A connector is not in the governed allowlist.", nameof(request.Connectors));
        requestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "authorized-user" : Normalize(requestedBy, "requestedBy", 128);
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{requestedBy}\n{idempotencyKey}"))).ToLowerInvariant()[..32];
        var requestSha256 = ComputeRequestSha256(intent, environment, target, normalizedConnectors);
        var now = DateTimeOffset.UtcNow;
        var tenantId = NormalizeTenant(context.TenantId);
        var repository = ResolveRepository();
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
            intent, environment, target, normalizedConnectors, "queued", "diagnose-plan-sync", now, now, steps, [],
            LifecycleState: _policy.Lifecycle.InitialState,
            ApprovalState: "pending",
            PolicyVersion: _policy.PolicyVersion,
            TenantId: tenantId,
            Repository: repository,
            ArtifactDigest: requestSha256);
        var (snapshot, created) = await store.CreateOrGetAsync(candidate, cancellationToken);
        if (!created)
            snapshot = await NormalizePersistedEnvironmentAsync(snapshot, cancellationToken);
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
        var reset = snapshot with
        {
            Status = "queued",
            Error = null,
            LifecycleState = _policy.Lifecycle.InitialState,
            HopCount = 0,
            ApprovalState = "pending",
            LeaseOwner = null,
            LeaseExpiresAt = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
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
            catch (Exception exception) when (IsExpectedOperationalFailure(exception))
            {
                logger.LogError(exception, "Control run {RunId} failed with an expected operational error.", id);
                await FailAsync(id, stoppingToken);
            }
        }
    }

    private async Task RecoverRunnableAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_timing.RecoveryInterval);
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
                catch (Exception exception) when (IsExpectedOperationalFailure(exception))
                {
                    logger.LogWarning(exception, "Control run recovery scan failed; the next bounded scan will retry.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Control run recovery loop stopped during application shutdown.");
        }
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
            var nextAttempt = run.AttemptCount + 1;
            run = await ReplaceAsync(run, run with
            {
                Status = "running",
                AttemptCount = nextAttempt,
                LifecycleState = NormalizeLifecycleState(run.LifecycleState),
                PolicyVersion = _policy.PolicyVersion,
                LeaseOwner = _workerId,
                LeaseExpiresAt = now.Add(_timing.LeaseDuration),
                UpdatedAt = now
            }, cancellationToken);
            run = await NormalizePersistedEnvironmentAsync(run, cancellationToken);
            if (nextAttempt > _policy.EventBus.Retry.MaxAttempts)
                throw new InvalidOperationException("The run exceeded the configured maximum retry attempts.");
            run = await TransitionLifecycleAsync(run, "PRECHECK", cancellationToken);
        }
        catch (ControlRunConcurrencyException) { return; }

        using var leaseLostCts = new CancellationTokenSource();
        using var heartbeatStopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var workCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, leaseLostCts.Token);
        var heartbeatTask = MaintainLeaseAsync(id, heartbeatStopCts.Token, leaseLostCts);
        try
        {
            var workToken = workCts.Token;
            run = await SetStepAsync(run, "context", "running", "Verifying tenant, subscription, and resource group.", workToken);
            var context = inventory.GetContext();
            if (!context.Configured || !string.Equals(context.ResourceGroup, run.Target, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The server Azure context does not match the requested resource group.");
            run = await SetStepAsync(run, "context", "completed", "Configured Azure boundary verified.", workToken);

            run = await SetStepAsync(run, "inventory", "running", "Reading Azure resource metadata with managed identity.", workToken);
            var resources = await inventory.ListResourcesAsync(null, workToken);
            run = await ReplaceAsync(run, run with { ResourceCount = resources.Count, UpdatedAt = DateTimeOffset.UtcNow }, workToken);
            run = await SetStepAsync(run, "inventory", "completed", $"Read metadata for {resources.Count} resources.", workToken);

            run = await TransitionLifecycleAsync(run, "PLAN", workToken);
            run = await SetStepAsync(run, "plan", "running", "Creating deterministic plan-only automation.", workToken);
            var plan = planner.CreatePlan(new EdgeAutomationRequest(run.Intent, run.Environment, run.Target, "all"));
            run = await ReplaceAsync(run, run with { Plan = plan, UpdatedAt = DateTimeOffset.UtcNow }, workToken);
            run = await SetStepAsync(run, "plan", "completed", $"Plan {plan.PlanId[..12]} created; apply is unavailable from Edge.", workToken);

            run = await SetStepAsync(run, "evidence", "running", "Canonicalizing non-secret evidence.", workToken);
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
            run = await ReplaceAsync(run, run with { EvidenceSha256 = digest, UpdatedAt = DateTimeOffset.UtcNow }, workToken);
            run = await SetStepAsync(run, "evidence", "completed", $"Evidence SHA-256 {digest}.", workToken);

            run = await SetStepAsync(run, "connectors", "running", "Reconciling external status receipts.", workToken);
            var awaitsApproval = plan.Steps.Any(step => step.Mutating);
            if (awaitsApproval)
            {
                run = await TransitionLifecycleAsync(run, "AWAIT_APPROVAL", workToken);
            }
            else
            {
                run = await TransitionLifecycleAsync(run, "EXECUTE", workToken);
                run = await TransitionLifecycleAsync(run, "VERIFY", workToken);
                run = await TransitionLifecycleAsync(run, "NOTIFY", workToken);
            }
            var receipts = await dispatcher.DispatchAsync(run with { Status = awaitsApproval ? "awaiting-approval" : "completed" }, workToken);
            run = await ReplaceAsync(run, run with { Receipts = receipts, UpdatedAt = DateTimeOffset.UtcNow }, workToken);
            run = await SetStepAsync(run, "connectors", "completed", $"Recorded {receipts.Count} connector receipts.", workToken);

            run = await SetStepAsync(run, "approval", "running", "Evaluating protected mutation gates.", workToken);
            run = await SetStepAsync(run, "approval", "completed", awaitsApproval
                ? "Stopped before mutation. Review what-if evidence and approve through the protected workflow."
                : "No mutating step is present.", workToken);
            if (!awaitsApproval) run = await TransitionLifecycleAsync(run, "COMPLETE", workToken);
            await ReplaceAsync(run, run with
            {
                Status = awaitsApproval ? "awaiting-approval" : "completed",
                ApprovalState = awaitsApproval ? "awaiting-approval" : "approved",
                LeaseOwner = null,
                LeaseExpiresAt = null,
                UpdatedAt = DateTimeOffset.UtcNow
            }, workToken);
        }
        catch (OperationCanceledException) when (leaseLostCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new ControlRunConcurrencyException();
        }
        finally
        {
            heartbeatStopCts.Cancel();
            await heartbeatTask;
        }
    }

    private async Task MaintainLeaseAsync(string id, CancellationToken cancellationToken, CancellationTokenSource leaseLostCts)
    {
        using var timer = new PeriodicTimer(_timing.HeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await _leaseWriteGate.WaitAsync(cancellationToken);
                try
                {
                    var current = await store.GetAsync(id, cancellationToken);
                    if (current is not null && current.Status is ("completed" or "awaiting-approval" or "failed")) return;
                    if (current is null || current.Status != "running" ||
                        !string.Equals(current.LeaseOwner, _workerId, StringComparison.Ordinal))
                        throw new ControlRunConcurrencyException();

                    _ = await store.ReplaceAsync(current with
                    {
                        LeaseOwner = _workerId,
                        LeaseExpiresAt = DateTimeOffset.UtcNow.Add(_timing.LeaseDuration)
                    }, current.ETag, cancellationToken);
                }
                finally
                {
                    _leaseWriteGate.Release();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Control run {RunId} lease heartbeat stopped.", id);
        }
        catch (ControlRunConcurrencyException)
        {
            logger.LogWarning("Control run {RunId} lease heartbeat lost ETag ownership; active work is being cancelled.", id);
            leaseLostCts.Cancel();
        }
        catch (Exception exception) when (IsExpectedOperationalFailure(exception))
        {
            logger.LogWarning(exception, "Control run {RunId} lease heartbeat failed; active work is being cancelled for safe recovery.", id);
            leaseLostCts.Cancel();
        }
    }

    private async Task FailAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var run = await store.GetAsync(id, cancellationToken);
            if (run is null || run.Status is "completed" or "awaiting-approval" ||
                !string.Equals(run.LeaseOwner, _workerId, StringComparison.Ordinal)) return;
            if (!_policy.AllowsTransition(NormalizeLifecycleState(run.LifecycleState), "FAILED")) return;
            try
            {
                var severity = ClassifyIncidentSeverity(run.Environment);
                var fingerprint = BuildIncidentFingerprint(run, severity, DateTimeOffset.UtcNow);
                var failureHopCount = run.HopCount >= _policy.EventBus.MaxHopCount
                    ? run.HopCount
                    : run.HopCount + 1;
                await ReplaceAsync(run, run with
                {
                    Status = "failed",
                    LifecycleState = "FAILED",
                    HopCount = failureHopCount,
                    IncidentSeverity = severity,
                    IncidentFingerprint = fingerprint,
                    Error = "The run failed. Review server telemetry using the correlation ID; secret-bearing exception text is never returned.",
                    LeaseOwner = null,
                    LeaseExpiresAt = null,
                    UpdatedAt = DateTimeOffset.UtcNow
                }, cancellationToken);
            }
            catch (ControlRunConcurrencyException)
            {
                logger.LogDebug("Control run {RunId} failure state was superseded by another worker.", id);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsExpectedOperationalFailure(exception))
        {
            logger.LogWarning(exception, "Could not persist failed state for control run {RunId}; its lease will expire for bounded recovery.", id);
        }
    }

    private static bool IsExpectedOperationalFailure(Exception exception) =>
        exception is CosmosException or
            HttpRequestException or
            InvalidOperationException or
            ArgumentException or
            CryptographicException or
            JsonException or
            AuthenticationFailedException or
            OperationCanceledException;

    private async Task<ControlRunSnapshot> NormalizePersistedEnvironmentAsync(ControlRunSnapshot run, CancellationToken cancellationToken)
    {
        var canonicalEnvironment = NormalizePersistedEnvironmentName(run.Environment);
        if (!Environments.Contains(canonicalEnvironment))
            throw new InvalidOperationException(
                $"Persisted control run environment '{run.Environment}' is not supported.");

        var canonicalRequestSha256 = ComputeRequestSha256(run.Intent, canonicalEnvironment, run.Target, run.Connectors);
        var hasLegacyEnvironment = !string.Equals(run.Environment, canonicalEnvironment, StringComparison.Ordinal);
        var hasLegacyRequestHash = !string.Equals(run.RequestSha256, canonicalRequestSha256, StringComparison.Ordinal);
        if (!hasLegacyEnvironment && !hasLegacyRequestHash) return run;

        var artifactDigest = string.Equals(run.ArtifactDigest, run.RequestSha256, StringComparison.Ordinal)
            ? canonicalRequestSha256
            : run.ArtifactDigest;
        logger.LogInformation(
            "Normalizing persisted legacy environment/hash for run {RunId}: environment '{LegacyEnvironment}' -> '{CanonicalEnvironment}', request hash update required={RequestHashUpdated}.",
            run.Id,
            run.Environment,
            canonicalEnvironment,
            hasLegacyRequestHash);
        return await ReplaceAsync(run, run with
        {
            Environment = canonicalEnvironment,
            RequestSha256 = canonicalRequestSha256,
            ArtifactDigest = artifactDigest,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private async Task<ControlRunSnapshot> TransitionLifecycleAsync(ControlRunSnapshot run, string nextState, CancellationToken cancellationToken)
    {
        var currentState = NormalizeLifecycleState(run.LifecycleState);
        if (string.Equals(currentState, nextState, StringComparison.Ordinal)) return run;
        if (!_policy.AllowsTransition(currentState, nextState))
            throw new InvalidOperationException($"Illegal lifecycle transition '{currentState}' -> '{nextState}'.");

        var nextHopCount = run.HopCount + 1;
        if (nextHopCount > _policy.EventBus.MaxHopCount)
            throw new InvalidOperationException($"The run exceeded the max hop count ({_policy.EventBus.MaxHopCount}).");

        return await ReplaceAsync(run, run with
        {
            LifecycleState = nextState,
            HopCount = nextHopCount,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
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

    private async Task<ControlRunSnapshot> ReplaceAsync(ControlRunSnapshot current, ControlRunSnapshot replacement, CancellationToken cancellationToken)
    {
        await _leaseWriteGate.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(current.LeaseOwner, _workerId, StringComparison.Ordinal))
            {
                var latest = await store.GetAsync(current.Id, cancellationToken);
                if (latest is null || latest.Status != "running" ||
                    !string.Equals(latest.LeaseOwner, _workerId, StringComparison.Ordinal))
                    throw new ControlRunConcurrencyException();

                if (replacement.Status == "running")
                    replacement = replacement with
                    {
                        LeaseOwner = _workerId,
                        LeaseExpiresAt = DateTimeOffset.UtcNow.Add(_timing.LeaseDuration)
                    };
                return await store.ReplaceAsync(replacement, latest.ETag, cancellationToken);
            }

            return await store.ReplaceAsync(replacement, current.ETag, cancellationToken);
        }
        finally
        {
            _leaseWriteGate.Release();
        }
    }

    public override void Dispose()
    {
        _leaseWriteGate.Dispose();
        base.Dispose();
    }

    private static string NormalizeTenant(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? "unknown-tenant" : tenantId.Trim().ToLowerInvariant();

    private static string ResolveRepository()
    {
        var repository = Environment.GetEnvironmentVariable("HELIOS_CANONICAL_REPOSITORY");
        return string.IsNullOrWhiteSpace(repository) ? DefaultRepository : repository.Trim();
    }

    private static string NormalizePersistedEnvironmentName(string environment)
    {
        var normalized = environment.Trim().ToLowerInvariant();
        return LegacyEnvironmentAliases.TryGetValue(normalized, out var canonical)
            ? canonical
            : normalized;
    }

    private string NormalizeLifecycleState(string? lifecycleState)
    {
        if (string.IsNullOrWhiteSpace(lifecycleState)) return _policy.Lifecycle.InitialState;
        var normalized = lifecycleState.Trim().ToUpperInvariant();
        return _policy.Lifecycle.States.Contains(normalized, StringComparer.Ordinal)
            ? normalized
            : _policy.Lifecycle.InitialState;
    }

    private static string ClassifyIncidentSeverity(string environment) =>
        NormalizePersistedEnvironmentName(environment) switch
        {
            "x-tier-prod" => "S0",
            "x-tier-xcore" => "S1",
            _ => "S2"
        };

    private static string ComputeRequestSha256(string intent, string environment, string target, IReadOnlyList<string> connectors)
    {
        var normalizedConnectors = connectors
            .Select(value => value.Trim().ToLowerInvariant())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var canonicalRequest = JsonSerializer.Serialize(new
        {
            intent = intent.Trim().ToLowerInvariant(),
            environment = NormalizePersistedEnvironmentName(environment),
            target = target.Trim().ToLowerInvariant(),
            connectors = normalizedConnectors
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))).ToLowerInvariant();
    }

    private static string BuildIncidentFingerprint(ControlRunSnapshot run, string severity, DateTimeOffset occurredAt)
    {
        var bucket = occurredAt.ToUnixTimeSeconds() / 900;
        var canonical = string.Join('\n', new[]
        {
            run.TenantId,
            run.Repository,
            run.Environment,
            run.Id,
            run.ArtifactDigest,
            severity,
            bucket.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
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
