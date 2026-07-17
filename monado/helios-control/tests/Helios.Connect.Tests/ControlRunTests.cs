using Helios.Connect.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class ControlRunTests
{
    [Fact]
    public async Task One_button_run_is_saved_idempotent_and_stops_at_approval()
    {
        var store = new InMemoryControlRunStore();
        var dispatcher = new FakeDispatcher();
        var coordinator = new ControlRunCoordinator(store, new FakeInventory(), new EdgeAutomationPlanner(), dispatcher, NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var request = new ControlRunRequest("provision-resources", "dev", null, ["github", "linear", "slack", "sharepoint"]);
            var first = await coordinator.StartAsync(request, "edge-one-button-0001", "principal-1", CancellationToken.None);
            var duplicate = await coordinator.StartAsync(request, "edge-one-button-0001", "principal-1", CancellationToken.None);
            Assert.Equal(first.Id, duplicate.Id);

            var completed = await WaitForTerminalAsync(coordinator, first.Id);
            Assert.Equal("awaiting-approval", completed.Status);
            Assert.NotNull(completed.Plan);
            Assert.False(completed.Plan!.CanApplyFromMcp);
            Assert.Matches("^[0-9a-f]{64}$", completed.EvidenceSha256);
            Assert.Equal(2, completed.ResourceCount);
            Assert.Equal(4, completed.Receipts.Count);
            Assert.All(completed.Steps, step => Assert.Equal("completed", step.Status));
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task One_button_run_rejects_unknown_connectors_and_unsafe_idempotency_keys()
    {
        var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        var unknown = new ControlRunRequest("provision-resources", "dev", null, ["unknown"]);
        await Assert.ThrowsAsync<ArgumentException>(() => coordinator.StartAsync(unknown, "edge-one-button-0002", "principal-1", CancellationToken.None));
        var valid = new ControlRunRequest("provision-resources", "dev");
        await Assert.ThrowsAsync<ArgumentException>(() => coordinator.StartAsync(valid, "bad key; delete", "principal-1", CancellationToken.None));
        coordinator.Dispose();
    }

    [Fact]
    public async Task Reusing_an_idempotency_key_for_a_different_request_is_rejected()
    {
        var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(new ControlRunRequest("provision-resources", "dev", null, ["github"]), "edge-conflict-0001", "principal-1", CancellationToken.None);

        await Assert.ThrowsAsync<ControlRunIdempotencyConflictException>(() => coordinator.StartAsync(
            new ControlRunRequest("provision-resources", "dev", null, ["slack"]),
            "edge-conflict-0001",
            "principal-1",
            CancellationToken.None));
        coordinator.Dispose();
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
            "provision-resources", "dev", "helios-dev-rg", [], "queued", "diagnose-plan-sync", now, now, steps, []);
        await store.CreateOrGetAsync(persisted, CancellationToken.None);

        var coordinator = new ControlRunCoordinator(store, new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
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
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task Empty_connector_selection_is_respected_and_runs_are_owner_scoped()
    {
        var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var started = await coordinator.StartAsync(
                new ControlRunRequest("provision-resources", "dev", null, []),
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
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task Cleanup_run_remains_plan_only_and_protects_shared_resources()
    {
        var coordinator = new ControlRunCoordinator(new InMemoryControlRunStore(), new FakeInventory(), new EdgeAutomationPlanner(), new FakeDispatcher(), NullLogger<ControlRunCoordinator>.Instance);
        await coordinator.StartAsync(CancellationToken.None);
        try
        {
            var run = await coordinator.StartAsync(new ControlRunRequest("cleanup-owned-resources", "dev"), "edge-cleanup-0001", "principal-1", CancellationToken.None);
            var completed = await WaitForTerminalAsync(coordinator, run.Id);
            Assert.Equal("awaiting-approval", completed.Status);
            Assert.Contains(completed.Plan!.Steps, step => step.Gate == "unknown-or-shared-resources-protected");
        }
        finally
        {
            await coordinator.StopAsync(CancellationToken.None);
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task Live_connector_relay_is_signed_and_idempotent_without_exposing_secret()
    {
        var handler = new CaptureHandler();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HELIOS_CONNECTOR_DELIVERY_MODE"] = "live",
            ["HELIOS_CONNECTOR_GITHUB_URL"] = "https://relay.example.test/helios",
            ["HELIOS_CONNECTOR_GITHUB_HMAC_SECRET"] = new string('s', 32),
            ["HELIOS_CONNECTOR_GITHUB_ALLOWED_HOSTS"] = "relay.example.test",
            ["HELIOS_CONNECTOR_GITHUB_HMAC_KEY_ID"] = "test-key-1"
        }).Build();
        var dispatcher = new ConnectorDispatcher(new StaticHttpClientFactory(new HttpClient(handler)), configuration);
        var now = DateTimeOffset.UtcNow;
        var run = new ControlRunSnapshot("0123456789abcdef0123456789abcdef", "control-runs", "edge-relay-0001", "correlation-1", "principal-1",
            "provision-resources", "dev", "helios-dev-rg", ["github"], "awaiting-approval", "diagnose-plan-sync", now, now, [], [],
            EvidenceSha256: new string('a', 64), ResourceCount: 2);

        var receipts = await dispatcher.DispatchAsync(run, CancellationToken.None);

        Assert.Single(receipts);
        Assert.Equal("delivered", receipts[0].Status);
        Assert.Equal("0123456789abcdef0123456789abcdef:github", handler.IdempotencyKey);
        Assert.Matches("^sha256=[0-9a-f]{64}$", handler.Signature);
        Assert.Equal("test-key-1", handler.KeyId);
        Assert.True(long.TryParse(handler.Timestamp, out _));
        Assert.DoesNotContain(new string('s', 32), handler.Body);
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

    private sealed class FakeDispatcher : IConnectorDispatcher
    {
        public IReadOnlyList<ConnectorBindingStatus> GetStatus() =>
            [new("github", true, "test"), new("linear", true, "test"), new("slack", true, "test"), new("sharepoint", true, "test")];

        public Task<IReadOnlyList<ConnectorReceipt>> DispatchAsync(ControlRunSnapshot run, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ConnectorReceipt>>(run.Connectors.Select(connector =>
                new ConnectorReceipt(connector, "delivered", 1, "Test receipt.", DateTimeOffset.UtcNow)).ToArray());
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
