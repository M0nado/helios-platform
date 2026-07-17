using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class WebhookTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string GitHubWebhookSecret = "helios-test-webhook-secret";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string? _originalGitHubWebhookSecret;

    public WebhookTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _originalGitHubWebhookSecret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET");
        Environment.SetEnvironmentVariable("GITHUB_WEBHOOK_SECRET", GitHubWebhookSecret);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Unknown_provider_is_not_found()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/webhooks/nope", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Empty_payload_is_rejected()
    {
        using var content = new StringContent("");
        using var response = await _client.PostAsync("/webhooks/github", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_json_is_rejected()
    {
        using var content = new StringContent("not-json", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/webhooks/github", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dry_run_still_rejects_unsigned_webhooks()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/webhooks/github", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_signed_webhook_is_accepted_and_immediate_replay_is_detected()
    {
        using var firstRequest = CreateSignedGitHubWebhook("signed-replay", "{\"event\":1}");
        using var firstResponse = await _client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);

        using var replayRequest = CreateSignedGitHubWebhook("signed-replay", "{\"event\":1}");
        using var replayResponse = await _client.SendAsync(replayRequest);
        var replayBody = await replayResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Contains("\"duplicate\":true", replayBody);
    }

    [Fact]
    public async Task Replay_cache_is_bounded_and_evicts_the_oldest_delivery()
    {
        await using var boundedFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("HELIOS_WEBHOOK_REPLAY_CACHE_CAPACITY", "2"));
        using var client = boundedFactory.CreateClient();

        foreach (var deliveryId in new[] { "bounded-a", "bounded-b", "bounded-c" })
        {
            using var request = CreateSignedGitHubWebhook(deliveryId, $"{{\"delivery\":\"{deliveryId}\"}}");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }

        using var oldestRequest = CreateSignedGitHubWebhook("bounded-a", "{\"delivery\":\"bounded-a\"}");
        using var oldestResponse = await client.SendAsync(oldestRequest);
        Assert.Equal(HttpStatusCode.Accepted, oldestResponse.StatusCode);
    }

    [Fact]
    public async Task Replay_cache_entry_expires()
    {
        await using var expiringFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("HELIOS_WEBHOOK_REPLAY_TTL_SECONDS", "1"));
        using var client = expiringFactory.CreateClient();

        using (var firstRequest = CreateSignedGitHubWebhook("expiring", "{\"event\":2}"))
        using (var firstResponse = await client.SendAsync(firstRequest))
            Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);

        await Task.Delay(TimeSpan.FromMilliseconds(1_100));

        using var expiredRequest = CreateSignedGitHubWebhook("expiring", "{\"event\":2}");
        using var expiredResponse = await client.SendAsync(expiredRequest);
        Assert.Equal(HttpStatusCode.Accepted, expiredResponse.StatusCode);
    }

    [Fact]
    public async Task Health_routes_preserve_legacy_and_report_live_and_ready()
    {
        using var legacy = await _client.GetAsync("/health");
        using var live = await _client.GetAsync("/health/live");
        using var ready = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task Readiness_fails_closed_when_cloud_identity_configuration_is_missing()
    {
        await using var securedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true");
            builder.UseSetting("AZURE_TENANT_ID", string.Empty);
            builder.UseSetting("AZURE_SUBSCRIPTION_ID", string.Empty);
            builder.UseSetting("AZURE_RESOURCE_GROUP", string.Empty);
            builder.UseSetting("AZURE_CLIENT_ID", string.Empty);
        });
        using var client = securedFactory.CreateClient();

        using var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Local_mcp_is_not_mapped_without_explicit_opt_in()
    {
        using var request = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/runtime/webhooks/mcp", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Explicit_local_runtime_maps_read_only_local_mcp()
    {
        await using var localFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_LOCAL_RUNTIME_ALLOWED", "true");
            builder.UseSetting("HELIOS_CLOUD_RUNTIME_ONLY", "false");
        });
        using var client = localFactory.CreateClient();
        using var request = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/runtime/webhooks/mcp", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hermes_get_status", body);
        Assert.DoesNotContain("run_sandbox", body);
    }

    [Fact]
    public async Task Cloud_runtime_never_maps_local_mcp_even_if_local_flag_is_true()
    {
        await using var cloudFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_LOCAL_RUNTIME_ALLOWED", "true");
            builder.UseSetting("HELIOS_CLOUD_RUNTIME_ONLY", "true");
        });
        using var client = cloudFactory.CreateClient();
        using var request = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/runtime/webhooks/mcp", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Azure_connector_fails_closed_without_Entra_identity()
    {
        await using var securedFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true"));
        using var client = securedFactory.CreateClient();
        using var response = await client.GetAsync("/connector/context");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Azure_mcp_exposes_only_inventory_tools()
    {
        await using var securedFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true"));
        using var client = securedFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "test-principal");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("azure_list_resources", body);
        Assert.Contains("azure_list_foundry_resources", body);
        Assert.DoesNotContain("deploy", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("role_assignment", body, StringComparison.OrdinalIgnoreCase);
    }

    private static HttpRequestMessage CreateSignedGitHubWebhook(string deliveryId, string body)
    {
        var signature = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(GitHubWebhookSecret),
            Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/github")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", $"sha256={signature}");
        request.Headers.Add("X-GitHub-Delivery", deliveryId);
        return request;
    }

    public void Dispose()
    {
        _client.Dispose();
        Environment.SetEnvironmentVariable("GITHUB_WEBHOOK_SECRET", _originalGitHubWebhookSecret);
    }
}
