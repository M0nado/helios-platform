using Helios.Connect.Api;
using Helios.Connect.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class ControlRunTests
{
    [Fact]
    public async Task One_button_run_is_saved_idempotent_and_stops_at_approval()
    {
        var store = new InMemoryControlRunStore();
        var dispatcher = new FakeDispatcher();
        using var coordinator = new ControlRunCoordinator(store, new FakeInventory(), new EdgeAutomationPlanner(), dispatcher, NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var request = new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github", "linear", "slack", "sharepoint"]);
            var first = await coordinator.StartAsync(request, "edge-one-button-0001", "principal-1", CancellationToken.None);
            var duplicate = await coordinator.StartAsync(request, "edge-one-button-0001", "principal-1", CancellationToken.None);
            Assert.Equal(first.Id, duplicate.Id);

            var completed = await WaitForTerminalAsync(coordinator, first.Id);
            Assert.Equal("awaiting-approval", completed.Status);
            Assert.Equal("AWAIT_APPROVAL", completed.LifecycleState);
            Assert.Equal("awaiting-approval", completed.ApprovalState);
            Assert.NotNull(completed.Plan);
            Assert.False(completed.Plan!.CanApplyFromMcp);
            Assert.Matches("^[0-9a-f]{64}$", completed.EvidenceSha256);
            Assert.Matches("^[0-9]+\\.[0-9]+\\.[0-9]+", completed.PolicyVersion);
            Assert.True(completed.AttemptCount >= 1);
            Assert.True(completed.HopCount >= 1);
            Assert.Equal(2, completed.ResourceCount);
            Assert.Equal(4, completed.Receipts.Count);
            Assert.All(completed.Steps, step => Assert.Equal("completed", step.Status));
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task One_button_run_rejects_unknown_connectors_and_unsafe_idempotency_keys()
    {
        using var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        var unknown = new ControlRunRequest("provision-resources", "x-tier-dev", null, ["unknown"]);
        await Assert.ThrowsAsync<ArgumentException>(() => coordinator.StartAsync(unknown, "edge-one-button-0002", "principal-1", CancellationToken.None));
        var valid = new ControlRunRequest("provision-resources", "x-tier-dev");
        await Assert.ThrowsAsync<ArgumentException>(() => coordinator.StartAsync(valid, "bad key; delete", "principal-1", CancellationToken.None));
    }

    [Fact]
    public async Task Resource_group_override_is_rejected_before_a_run_is_saved()
    {
        using var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        var request = new ControlRunRequest("provision-resources", "x-tier-dev", "different-resource-group", ["github"]);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => coordinator.StartAsync(
            request, "edge-boundary-0001", "principal-1", CancellationToken.None));

        Assert.Contains("cannot override", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reusing_an_idempotency_key_for_a_different_request_is_rejected()
    {
        using var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github"]), "edge-conflict-0001", "principal-1", CancellationToken.None);

        await Assert.ThrowsAsync<ControlRunIdempotencyConflictException>(() => coordinator.StartAsync(
            new ControlRunRequest("provision-resources", "x-tier-dev", null, ["slack"]),
            "edge-conflict-0001",
            "principal-1",
            CancellationToken.None));
    }

    [Fact]
    public async Task Persisted_legacy_request_hash_is_normalized_before_idempotency_comparison()
    {
        var store = new InMemoryControlRunStore();
        var requestedBy = "principal-1";
        var idempotencyKey = "edge-legacy-hash-0001";
        var runId = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{requestedBy}\n{idempotencyKey}"))).ToLowerInvariant()[..32];
        var now = DateTimeOffset.UtcNow;
        var legacyRequestSha = ComputeRequestSha256ForTest(
            "provision-resources",
            "preview",
            "helios-dev-rg",
            ["github"]);
        var persisted = new ControlRunSnapshot(
            runId, "control-runs", legacyRequestSha, "correlation-legacy-hash", requestedBy,
            "provision-resources", "preview", "helios-dev-rg", ["github"], "queued", "diagnose-plan-sync", now, now, [], [],
            ArtifactDigest: legacyRequestSha);
        await store.CreateOrGetAsync(persisted, CancellationToken.None);

        using var coordinator = new ControlRunCoordinator(
            store, new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(),
            NullLogger<ControlRunCoordinator>.Instance);
        var retried = await coordinator.StartAsync(
            new ControlRunRequest("provision-resources", "x-tier-xcore", null, ["github"]),
            idempotencyKey,
            requestedBy,
            CancellationToken.None);

        Assert.Equal(runId, retried.Id);
        Assert.Equal("x-tier-xcore", retried.Environment);
        var expectedRequestSha = ComputeRequestSha256ForTest(
            "provision-resources",
            "x-tier-xcore",
            "helios-dev-rg",
            ["github"]);
        Assert.Equal(expectedRequestSha, retried.RequestSha256);
        Assert.Equal(expectedRequestSha, retried.ArtifactDigest);
    }

    [Fact]
    public async Task Persisted_queued_run_is_recovered_after_the_request_replica_is_gone()
    {
        var store = new InMemoryControlRunStore();
        var now = DateTimeOffset.UtcNow;
        var steps = new[]
        {
            new ControlRunStep("context", "queued", "queued"),
            new ControlRunStep("inventory", "queued", "queued"),
            new ControlRunStep("plan", "queued", "queued"),
            new ControlRunStep("evidence", "queued", "queued"),
            new ControlRunStep("connectors", "queued", "queued"),
            new ControlRunStep("approval", "queued", "queued")
        };
        var persisted = new ControlRunSnapshot(
            "abcdefabcdefabcdefabcdefabcdefab", "control-runs", new string('a', 64), "correlation-recovery", "principal-1",
            "provision-resources", "x-tier-dev", "helios-dev-rg", [], "queued", "diagnose-plan-sync", now, now, steps, []);
        await store.CreateOrGetAsync(persisted, CancellationToken.None);

        using var coordinator = new ControlRunCoordinator(store, new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var completed = await WaitForTerminalAsync(coordinator, persisted.Id);
            Assert.Equal("awaiting-approval", completed.Status);
            Assert.Null(completed.LeaseOwner);
            Assert.Null(completed.LeaseExpiresAt);
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Persisted_legacy_environment_is_normalized_before_planning()
    {
        var store = new InMemoryControlRunStore();
        var now = DateTimeOffset.UtcNow;
        var persisted = new ControlRunSnapshot(
            "0123456789abcdef0123456789abcdea", "control-runs", new string('a', 64), "correlation-legacy-environment", "principal-1",
            "provision-resources", "preview", "helios-dev-rg", [], "queued", "diagnose-plan-sync", now, now, [], []);
        await store.CreateOrGetAsync(persisted, CancellationToken.None);

        using var coordinator = new ControlRunCoordinator(store, new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var completed = await WaitForTerminalAsync(coordinator, persisted.Id);
            Assert.Equal("awaiting-approval", completed.Status);
            Assert.Equal("x-tier-xcore", completed.Environment);
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Empty_connector_selection_is_respected_and_runs_are_owner_scoped()
    {
        using var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var started = await coordinator.StartAsync(
                new ControlRunRequest("provision-resources", "x-tier-dev", null, []),
                "edge-no-connectors-0001", "principal-1", CancellationToken.None);
            var completed = await WaitForTerminalAsync(coordinator, started.Id);

            Assert.Empty(completed.Connectors);
            Assert.Empty(completed.Receipts);
            Assert.Null(await coordinator.GetAsync(started.Id, "principal-2", CancellationToken.None));
            Assert.Null(await coordinator.ResumeAsync(started.Id, "principal-2", CancellationToken.None));
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Cleanup_run_remains_plan_only_and_protects_shared_resources()
    {
        using var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var run = await coordinator.StartAsync(new ControlRunRequest("cleanup-owned-resources", "x-tier-dev"), "edge-cleanup-0001", "principal-1", CancellationToken.None);
            var completed = await WaitForTerminalAsync(coordinator, run.Id);
            Assert.Equal("awaiting-approval", completed.Status);
            Assert.Contains(completed.Plan!.Steps, step => step.Gate == "unknown-or-shared-resources-protected");
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Legacy_prod_run_exceeding_max_attempts_keeps_production_severity()
    {
        var basePolicy = AgentCorePolicy.Default;
        var boundedPolicy = basePolicy with
        {
            EventBus = basePolicy.EventBus with
            {
                Retry = basePolicy.EventBus.Retry with { MaxAttempts = 0 }
            }
        };
        var store = new InMemoryControlRunStore();
        var now = DateTimeOffset.UtcNow;
        var persisted = new ControlRunSnapshot(
            "abcdefabcdefabcdefabcdefabcdef12",
            "control-runs",
            ComputeRequestSha256ForTest("provision-resources", "prod", "helios-dev-rg", ["github"]),
            "correlation-legacy-prod",
            "principal-1",
            "provision-resources",
            "prod",
            "helios-dev-rg",
            ["github"],
            "queued",
            "diagnose-plan-sync",
            now,
            now,
            [],
            [],
            ArtifactDigest: ComputeRequestSha256ForTest("provision-resources", "prod", "helios-dev-rg", ["github"]));
        await store.CreateOrGetAsync(persisted, CancellationToken.None);

        using var coordinator = new ControlRunCoordinator(
            store,
            new FakeInventory(),
            new EdgeAutomationPlanner(),
            new FakeDispatcher(),
            NullLogger<ControlRunCoordinator>.Instance,
            policy: boundedPolicy);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var failed = await WaitForTerminalAsync(coordinator, persisted.Id);
            Assert.Equal("failed", failed.Status);
            Assert.Equal("x-tier-prod", failed.Environment);
            Assert.Equal("S0", failed.IncidentSeverity);
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Lease_heartbeat_prevents_a_second_replica_from_dispatching_a_long_run()
    {
        var store = new InMemoryControlRunStore();
        var dispatcher = new BlockingDispatcher();
        var timing = new ControlRunCoordinatorTiming(
            TimeSpan.FromMilliseconds(120),
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(15));
        using var firstCoordinator = new ControlRunCoordinator(
            store, new FakeInventory(), new EdgeAutomationPlanner(), dispatcher,
            NullLogger<ControlRunCoordinator>.Instance, timing);
        using var secondCoordinator = new ControlRunCoordinator(
            store, new FakeInventory(), new EdgeAutomationPlanner(), dispatcher,
            NullLogger<ControlRunCoordinator>.Instance, timing);
        await firstCoordinator.StartAsync(CancellationToken.None);
        await secondCoordinator.StartAsync(CancellationToken.None);
        try
        {
            var run = await firstCoordinator.StartAsync(
                new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github"]),
                "edge-heartbeat-0001", "principal-1", CancellationToken.None);
            await dispatcher.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await Task.Delay(TimeSpan.FromMilliseconds(420));

            Assert.Equal(1, dispatcher.CallCount);
            dispatcher.Release();
            var completed = await WaitForTerminalAsync(firstCoordinator, run.Id);
            Assert.Equal("awaiting-approval", completed.Status);
            Assert.Single(completed.Receipts);
        }
        finally
        {
            dispatcher.Release();
            await firstCoordinator.StopAsync(CancellationToken.None);
            await secondCoordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Losing_the_lease_cancels_dispatch_and_does_not_persist_receipts()
    {
        var store = new InMemoryControlRunStore();
        var dispatcher = new BlockingDispatcher();
        var timing = new ControlRunCoordinatorTiming(
            TimeSpan.FromMilliseconds(120),
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(15));
        using var coordinator = new ControlRunCoordinator(
            store, new FakeInventory(), new EdgeAutomationPlanner(), dispatcher,
            NullLogger<ControlRunCoordinator>.Instance, timing);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var run = await coordinator.StartAsync(
                new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github"]),
                "edge-heartbeat-loss-0001", "principal-1", CancellationToken.None);
            await dispatcher.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            while (true)
            {
                var current = await store.GetAsync(run.Id, CancellationToken.None)
                    ?? throw new InvalidOperationException("Run disappeared.");
                try
                {
                    await store.ReplaceAsync(
                        current with
                        {
                            LeaseOwner = "replacement-worker",
                            LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1)
                        },
                        current.ETag,
                        CancellationToken.None);
                    break;
                }
                catch (ControlRunConcurrencyException)
                {
                    // The heartbeat renewed between read and replace; retry with its ETag.
                }
            }

            await dispatcher.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Delay(TimeSpan.FromMilliseconds(75));
            var persisted = await store.GetAsync(run.Id, CancellationToken.None)
                ?? throw new InvalidOperationException("Run disappeared.");

            Assert.Equal("replacement-worker", persisted.LeaseOwner);
            Assert.Empty(persisted.Receipts);
            Assert.Equal(1, dispatcher.CallCount);
        }
        finally
        {
            dispatcher.Release();
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Failed_run_records_incident_metadata_without_automatic_rollback()
    {
        using var coordinator = new ControlRunCoordinator(
            new InMemoryControlRunStore(),
            new FailingInventory("tenant-a"),
            new EdgeAutomationPlanner(),
            new FakeDispatcher(),
            NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var started = await coordinator.StartAsync(
                new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github"]),
                "edge-failure-0001",
                "principal-1",
                CancellationToken.None);
            var failed = await WaitForTerminalAsync(coordinator, started.Id);

            Assert.Equal("failed", failed.Status);
            Assert.Equal("FAILED", failed.LifecycleState);
            Assert.Equal("S2", failed.IncidentSeverity);
            Assert.Matches("^[0-9a-f]{64}$", failed.IncidentFingerprint);
            Assert.NotEqual("rolled_back", failed.Status);
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Exceeding_max_attempts_fails_run_instead_of_leaving_it_queued()
    {
        var basePolicy = AgentCorePolicy.Default;
        var boundedPolicy = basePolicy with
        {
            EventBus = basePolicy.EventBus with
            {
                Retry = basePolicy.EventBus.Retry with { MaxAttempts = 2 }
            }
        };
        using var coordinator = new ControlRunCoordinator(
            new InMemoryControlRunStore(),
            new FailingInventory("tenant-a"),
            new EdgeAutomationPlanner(),
            new FakeDispatcher(),
            NullLogger<ControlRunCoordinator>.Instance,
            policy: boundedPolicy);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var started = await coordinator.StartAsync(
                new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github"]),
                "edge-max-attempts-0001",
                "principal-1",
                CancellationToken.None);
            var firstFailure = await WaitForTerminalAsync(coordinator, started.Id);
            Assert.Equal("failed", firstFailure.Status);
            Assert.Equal(1, firstFailure.AttemptCount);

            _ = await coordinator.ResumeAsync(started.Id, CancellationToken.None);
            var secondFailure = await WaitForTerminalAsync(coordinator, started.Id);
            Assert.Equal("failed", secondFailure.Status);
            Assert.Equal(2, secondFailure.AttemptCount);

            _ = await coordinator.ResumeAsync(started.Id, CancellationToken.None);
            var exceededFailure = await WaitForTerminalAsync(coordinator, started.Id);

            Assert.Equal("failed", exceededFailure.Status);
            Assert.Equal("FAILED", exceededFailure.LifecycleState);
            Assert.True(exceededFailure.AttemptCount > boundedPolicy.EventBus.Retry.MaxAttempts);
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Incident_fingerprint_isolated_by_tenant_for_same_run_identity()
    {
        var idempotencyKey = "edge-tenant-isolation-0001";
        var requestedBy = "principal-tenant";

        using var coordinatorA = new ControlRunCoordinator(
            new InMemoryControlRunStore(),
            new FailingInventory("tenant-a"),
            new EdgeAutomationPlanner(),
            new FakeDispatcher(),
            NullLogger<ControlRunCoordinator>.Instance);
        using var coordinatorB = new ControlRunCoordinator(
            new InMemoryControlRunStore(),
            new FailingInventory("tenant-b"),
            new EdgeAutomationPlanner(),
            new FakeDispatcher(),
            NullLogger<ControlRunCoordinator>.Instance);
        await coordinatorA.StartAsync(CancellationToken.None);
        await coordinatorB.StartAsync(CancellationToken.None);
        try
        {
            var runA = await coordinatorA.StartAsync(
                new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github"]),
                idempotencyKey,
                requestedBy,
                CancellationToken.None);
            var runB = await coordinatorB.StartAsync(
                new ControlRunRequest("provision-resources", "x-tier-dev", null, ["github"]),
                idempotencyKey,
                requestedBy,
                CancellationToken.None);

            var failedA = await WaitForTerminalAsync(coordinatorA, runA.Id);
            var failedB = await WaitForTerminalAsync(coordinatorB, runB.Id);

            Assert.Equal(runA.Id, runB.Id);
            Assert.Equal("failed", failedA.Status);
            Assert.Equal("failed", failedB.Status);
            Assert.NotEqual(failedA.TenantId, failedB.TenantId);
            Assert.NotEqual(failedA.IncidentFingerprint, failedB.IncidentFingerprint);
        }
        finally
        {
            await coordinatorA.StopAsync(CancellationToken.None);
            await coordinatorB.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Live_connector_relay_is_signed_and_idempotent_without_exposing_secret()
    {
        var handler = new CaptureHandler();
        using var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HELIOS_CONNECTOR_DELIVERY_MODE"] = "live",
            ["HELIOS_CONNECTOR_GITHUB_URL"] = "https://relay.example.test/helios",
            ["HELIOS_CONNECTOR_GITHUB_HMAC_SECRET"] = new string('s', 32),
            ["HELIOS_CONNECTOR_GITHUB_ALLOWED_HOSTS"] = "relay.example.test",
            ["HELIOS_CONNECTOR_GITHUB_HMAC_KEY_ID"] = "test-key-1"
        }).Build();
        var dispatcher = new ConnectorDispatcher(new StaticHttpClientFactory(httpClient), configuration);
        var now = DateTimeOffset.UtcNow;
        var run = new ControlRunSnapshot("0123456789abcdef0123456789abcdef", "control-runs", "edge-relay-0001", "correlation-1", "principal-1",
            "provision-resources", "x-tier-dev", "helios-dev-rg", ["github"], "awaiting-approval", "diagnose-plan-sync", now, now, [], [],
            EvidenceSha256: new string('a', 64), ResourceCount: 2, LifecycleState: "AWAIT_APPROVAL", PolicyVersion: "1.0.0",
            TenantId: "tenant-a", Repository: "M0nado/helios-platform", ArtifactDigest: new string('b', 64), AttemptCount: 1, HopCount: 4,
            ApprovalState: "awaiting-approval");

        var receipts = await dispatcher.DispatchAsync(run, CancellationToken.None);
        var firstBody = handler.Body;
        var firstTimestamp = handler.Timestamp;
        var firstSignature = handler.Signature;
        var retryReceipts = await dispatcher.DispatchAsync(run, CancellationToken.None);

        Assert.Single(receipts);
        Assert.Single(retryReceipts);
        Assert.Equal("delivered", receipts[0].Status);
        Assert.Equal(firstBody, handler.Body);
        Assert.Equal(firstTimestamp, handler.Timestamp);
        Assert.Equal(firstSignature, handler.Signature);
        Assert.Equal("0123456789abcdef0123456789abcdef:github", handler.IdempotencyKey);
        Assert.Matches("^sha256=[0-9a-f]{64}$", handler.Signature);
        var signedEnvelope = $"{handler.Timestamp}\n{handler.IdempotencyKey}\n{handler.Body}";
        var expectedSignature = Convert.ToHexString(HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(new string('s', 32)),
            System.Text.Encoding.UTF8.GetBytes(signedEnvelope))).ToLowerInvariant();
        Assert.Equal($"sha256={expectedSignature}", handler.Signature);
        Assert.Equal("test-key-1", handler.KeyId);
        Assert.True(long.TryParse(handler.Timestamp, out _));
        Assert.DoesNotContain(new string('s', 32), handler.Body);
        var envelope = JsonSerializer.Deserialize<HeliosEvent>(handler.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(envelope);
        Assert.Equal("helios.control-run.status", envelope!.Type);
        Assert.Equal("helios-control", envelope.Source);
        Assert.Equal("control-runs/0123456789abcdef0123456789abcdef", envelope.Subject);
        Assert.Equal("correlation-1", envelope.CorrelationId);
        Assert.Equal("internal", envelope.DataClassification);
        Assert.Equal("github", ((JsonElement)envelope.Payload["connector"]!).GetString());
        Assert.Equal("0123456789abcdef0123456789abcdef", ((JsonElement)envelope.Payload["runId"]!).GetString());
        Assert.Equal("AWAIT_APPROVAL", ((JsonElement)envelope.Payload["lifecycleState"]!).GetString());
        Assert.Equal("tenant-a", ((JsonElement)envelope.Payload["tenant"]!).GetString());
        Assert.Equal("M0nado/helios-platform", ((JsonElement)envelope.Payload["repository"]!).GetString());
        Assert.Equal("awaiting-approval", ((JsonElement)envelope.Payload["approvalState"]!).GetString());
    }

    private static string ComputeRequestSha256ForTest(string intent, string environment, string target, IReadOnlyList<string> connectors)
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
            environment = environment.Trim().ToLowerInvariant(),
            target = target.Trim().ToLowerInvariant(),
            connectors = normalizedConnectors
        });
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonicalRequest))).ToLowerInvariant();
    }

    private static async Task<ControlRunSnapshot> WaitForTerminalAsync(ControlRunCoordinator coordinator, string id)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var run = await coordinator.GetAsync(id, CancellationToken.None) ?? throw new InvalidOperationException("Run disappeared.");
            if (run.Status is "completed" or "awaiting-approval" or "failed") return run;
            await Task.Delay(25);
        }
        throw new TimeoutException("Control run did not reach a terminal state.");
    }

    private sealed class FakeInventory : IAzureInventoryService
    {
        public AzureConnectorContext GetContext() => new("11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "helios-dev-rg", "read-only-resource-group", true);

        public Task<IReadOnlyList<AzureInventoryResource>> ListResourcesAsync(string? typePrefix, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AzureInventoryResource>>([
                new("/subscriptions/test/resourceGroups/helios-dev-rg/providers/Microsoft.App/containerApps/api", "api", "Microsoft.App/containerApps", "eastus2"),
                new("/subscriptions/test/resourceGroups/helios-dev-rg/providers/Microsoft.KeyVault/vaults/vault", "vault", "Microsoft.KeyVault/vaults", "eastus2")
            ]);

        public Task<IReadOnlyList<AzureInventoryResource>> ListFoundryResourcesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AzureInventoryResource>>([]);
    }

    private sealed class FailingInventory(string tenantId) : IAzureInventoryService
    {
        public AzureConnectorContext GetContext() =>
            new(tenantId, "22222222-2222-2222-2222-222222222222", "helios-dev-rg", "read-only-resource-group", true);

        public Task<IReadOnlyList<AzureInventoryResource>> ListResourcesAsync(string? typePrefix, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Injected inventory failure.");

        public Task<IReadOnlyList<AzureInventoryResource>> ListFoundryResourcesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AzureInventoryResource>>([]);
    }

    private sealed class FakeDispatcher : IConnectorDispatcher
    {
        public IReadOnlyList<ConnectorBindingStatus> GetStatus() =>
            [new("github", true, "test"), new("linear", true, "test"), new("slack", true, "test"), new("sharepoint", true, "test")];

        public Task<IReadOnlyList<ConnectorReceipt>> DispatchAsync(ControlRunSnapshot run, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ConnectorReceipt>>(run.Connectors.Select(connector =>
                new ConnectorReceipt(connector, "delivered", 1, "Test receipt.", DateTimeOffset.UtcNow)).ToArray());
    }

    private sealed class BlockingDispatcher : IConnectorDispatcher
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancellationObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public TaskCompletionSource Entered => _entered;
        public TaskCompletionSource CancellationObserved => _cancellationObserved;
        public int CallCount => Volatile.Read(ref _callCount);

        public IReadOnlyList<ConnectorBindingStatus> GetStatus() => [new("github", true, "test")];

        public async Task<IReadOnlyList<ConnectorReceipt>> DispatchAsync(ControlRunSnapshot run, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            _entered.TrySetResult();
            try
            {
                await _release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _cancellationObserved.TrySetResult();
                throw;
            }
            return [new ConnectorReceipt("github", "delivered", 1, "Test receipt.", DateTimeOffset.UtcNow)];
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string IdempotencyKey { get; private set; } = string.Empty;
        public string Signature { get; private set; } = string.Empty;
        public string Timestamp { get; private set; } = string.Empty;
        public string KeyId { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IdempotencyKey = request.Headers.GetValues("X-Helios-Idempotency-Key").Single();
            Signature = request.Headers.GetValues("X-Helios-Signature").Single();
            Timestamp = request.Headers.GetValues("X-Helios-Timestamp").Single();
            KeyId = request.Headers.GetValues("X-Helios-Key-Id").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }
}
