using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class WebhookTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string GitHubWebhookSecret = "helios-test-webhook-secret";
    private const string LinearWebhookSecret = "helios-test-linear-secret";
    private const string SlackSigningSecret = "helios-test-slack-secret";
    private const string AllowedOAuthClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string? _originalGitHubWebhookSecret;
    private readonly string? _originalLinearWebhookSecret;
    private readonly string? _originalSlackSigningSecret;

    public WebhookTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _originalGitHubWebhookSecret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET");
        _originalLinearWebhookSecret = Environment.GetEnvironmentVariable("LINEAR_WEBHOOK_SECRET");
        _originalSlackSigningSecret = Environment.GetEnvironmentVariable("SLACK_SIGNING_SECRET");
        Environment.SetEnvironmentVariable("GITHUB_WEBHOOK_SECRET", GitHubWebhookSecret);
        Environment.SetEnvironmentVariable("LINEAR_WEBHOOK_SECRET", LinearWebhookSecret);
        Environment.SetEnvironmentVariable("SLACK_SIGNING_SECRET", SlackSigningSecret);
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
    public async Task Signed_invalid_json_is_rejected_after_authentication()
    {
        using var request = CreateSignedGitHubWebhook("invalid-json", "not-json");
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unsigned_invalid_json_is_rejected_before_json_parsing()
    {
        using var content = new StringContent("not-json", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/webhooks/github", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task Valid_linear_signature_is_accepted()
    {
        const string body = "{\"action\":\"update\"}";
        var signature = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(LinearWebhookSecret),
            Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/linear")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Linear-Signature", signature);
        request.Headers.Add("X-Linear-Delivery", "linear-valid");

        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Valid_current_slack_signature_is_accepted_but_stale_is_rejected()
    {
        const string body = "{\"type\":\"event_callback\"}";
        var current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using (var currentRequest = CreateSignedSlackWebhook(current, body))
        using (var currentResponse = await _client.SendAsync(currentRequest))
            Assert.Equal(HttpStatusCode.Accepted, currentResponse.StatusCode);

        using var staleRequest = CreateSignedSlackWebhook(current - 600, body);
        using var staleResponse = await _client.SendAsync(staleRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);
    }

    [Fact]
    public async Task Microsoft_webhook_without_provider_specific_Entra_validation_fails_closed()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/webhooks/teams", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task Chunked_webhook_body_over_configured_limit_is_rejected()
    {
        await using var boundedFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("HELIOS_MAX_WEBHOOK_BYTES", "1024"));
        using var client = boundedFactory.CreateClient();
        using var content = new ChunkedJsonContent(
            Encoding.UTF8.GetBytes($"{{\"padding\":\"{new string('a', 2_048)}\"}}"));

        Assert.Null(content.Headers.ContentLength);
        using var response = await client.PostAsync("/webhooks/github", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Chunked_cloud_mcp_body_over_configured_limit_is_rejected()
    {
        await using var boundedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_MAX_MCP_BYTES", "1024");
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "false");
            builder.UseSetting("HELIOS_CLOUD_RUNTIME_ONLY", "false");
        });
        using var client = boundedFactory.CreateClient();
        using var content = new ChunkedJsonContent(
            Encoding.UTF8.GetBytes($"{{\"padding\":\"{new string('a', 2_048)}\"}}"));

        Assert.Null(content.Headers.ContentLength);
        using var response = await client.PostAsync("/mcp", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Chunked_local_mcp_body_over_configured_limit_is_rejected()
    {
        await using var boundedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_MAX_MCP_BYTES", "1024");
            builder.UseSetting("HELIOS_LOCAL_RUNTIME_ALLOWED", "true");
            builder.UseSetting("HELIOS_CLOUD_RUNTIME_ONLY", "false");
        });
        using var client = boundedFactory.CreateClient();
        using var content = new ChunkedJsonContent(
            Encoding.UTF8.GetBytes($"{{\"padding\":\"{new string('a', 2_048)}\"}}"));

        Assert.Null(content.Headers.ContentLength);
        using var response = await client.PostAsync("/runtime/webhooks/mcp", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
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
    public async Task Teams_package_policy_routes_are_available()
    {
        using var privacy = await _client.GetAsync("/privacy");
        using var terms = await _client.GetAsync("/terms");
        Assert.Equal(HttpStatusCode.OK, privacy.StatusCode);
        Assert.Equal(HttpStatusCode.OK, terms.StatusCode);
    }

    [Fact]
    public async Task Edge_control_routes_expose_safe_connector_state_and_require_idempotency()
    {
        using var connectors = await _client.GetAsync("/control/connectors");
        var connectorBody = await connectors.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, connectors.StatusCode);
        Assert.Contains("no-store", connectors.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("github", connectorBody);
        Assert.DoesNotContain("HMAC_SECRET", connectorBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", connectorBody, StringComparison.OrdinalIgnoreCase);

        using var content = new StringContent("{\"intent\":\"provision-resources\",\"environment\":\"dev\"}", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/control/runs", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Edge_control_route_rejects_oversized_chunked_bodies()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/control/runs")
        {
            Content = new ChunkedJsonContent(Encoding.UTF8.GetBytes($"{{\"intent\":\"provision-resources\",\"environment\":\"dev\",\"padding\":\"{new string('a', 20_000)}\"}}"))
        };
        request.Headers.Add("Idempotency-Key", "chunked-control-0001");

        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Edge_manifest_is_served_with_installable_content_type()
    {
        using var response = await _client.GetAsync("/wizard/manifest.webmanifest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/manifest+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Microsoft_tab_uses_Teams_SSO_popup_fallback_and_current_cloud_hosts()
    {
        using var page = await _client.GetAsync("/wizard/index.html");
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("https://res.cdn.office.net/teams-js/2.53.1/", html);
        Assert.Contains("sha384-PIuQ2V7hlz4b1x3G1mPCYYZiWTjxzRTL6bf547xR9ARsAeNv2DAzti86LQnFCwlo", html);

        Assert.True(page.Headers.TryGetValues("Content-Security-Policy", out var policies));
        var policy = Assert.Single(policies!);
        Assert.Contains("https://res.cdn.office.net", policy);
        Assert.Contains("https://*.cloud.microsoft", policy);

        var script = await _client.GetStringAsync("/wizard/wizard.js");
        Assert.Contains("authentication.getAuthToken", script);
        Assert.Contains("authentication.authenticate", script);
        Assert.True(
            script.IndexOf("if (embeddedHost)", StringComparison.Ordinal) <
            script.IndexOf("window.location.assign", StringComparison.Ordinal),
            "Embedded Microsoft hosts must never navigate their iframe into Entra login.");

        var authStart = await _client.GetStringAsync("/wizard/auth-start.html");
        var authEnd = await _client.GetStringAsync("/wizard/auth-end.js");
        Assert.Contains("/.auth/login/aad", authStart);
        Assert.Contains("authentication.notifySuccess('session-established')", authEnd);
        Assert.DoesNotContain("notifySuccess(accessToken", authEnd, StringComparison.Ordinal);
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
    public async Task Cloud_only_runtime_requires_Entra_even_when_explicit_auth_flag_is_false()
    {
        await using var cloudFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_CLOUD_RUNTIME_ONLY", "true");
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "false");
        });
        using var client = cloudFactory.CreateClient();

        using var response = await client.GetAsync("/connector/context");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cloud_only_readiness_fails_when_Azure_identity_configuration_is_missing()
    {
        await using var cloudFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_CLOUD_RUNTIME_ONLY", "true");
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "false");
            builder.UseSetting("AZURE_TENANT_ID", string.Empty);
            builder.UseSetting("AZURE_SUBSCRIPTION_ID", string.Empty);
            builder.UseSetting("AZURE_RESOURCE_GROUP", string.Empty);
            builder.UseSetting("AZURE_CLIENT_ID", string.Empty);
            builder.UseSetting("HELIOS_ENTRA_CLIENT_ID", string.Empty);
            builder.UseSetting("HELIOS_PUBLIC_BASE_URL", string.Empty);
        });
        using var client = cloudFactory.CreateClient();

        using var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
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
    public async Task Azure_mcp_exposes_client_id_scope_approved_tool_contracts()
    {
        const string applicationIdUri = "api://11111111-1111-1111-1111-111111111111";
        await using var securedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true");
            builder.UseSetting("HELIOS_ENTRA_CLIENT_ID", "11111111-1111-1111-1111-111111111111");
            builder.UseSetting("HELIOS_ALLOWED_CLIENT_IDS", AllowedOAuthClientId);
            builder.UseSetting("HELIOS_ENTRA_APPLICATION_ID_URI", applicationIdUri);
            builder.UseSetting("HELIOS_PUBLIC_BASE_URL", "https://helios.example.test");
        });
        using var client = securedFactory.CreateClient();

        using var listRequest = CreateMcpRequest("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}");
        using var listResponse = await client.SendAsync(listRequest);
        var listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var listDocument = JsonDocument.Parse(listBody);
        var tools = listDocument.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray().ToArray();
        var names = tools
            .Select(tool => tool.GetProperty("name").GetString()!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[]
        {
            "azure_get_context",
            "azure_list_foundry_resources",
            "azure_list_resources",
            "fetch",
            "helios_get_control_plane_status",
            "helios_get_run",
            "helios_list_connectors",
            "helios_plan_automation",
            "helios_propose_upgrade",
            "helios_render_control_center",
            "search"
        }, names);

        foreach (var tool in tools)
        {
            var scheme = Assert.Single(tool.GetProperty("securitySchemes").EnumerateArray());
            Assert.Equal("oauth2", scheme.GetProperty("type").GetString());
            Assert.Equal(applicationIdUri + "/access_as_user",
                Assert.Single(scheme.GetProperty("scopes").EnumerateArray()).GetString());
            var compatibilityScheme = Assert.Single(
                tool.GetProperty("_meta").GetProperty("securitySchemes").EnumerateArray());
            Assert.Equal(scheme.GetRawText(), compatibilityScheme.GetRawText());
        }

        foreach (var name in new[] { "search", "fetch", "helios_get_control_plane_status", "helios_render_control_center" })
        {
            var tool = Assert.Single(tools, candidate => candidate.GetProperty("name").GetString() == name);
            Assert.True(tool.TryGetProperty("outputSchema", out _));
        }

        var renderTool = Assert.Single(tools, tool =>
            tool.GetProperty("name").GetString() == "helios_render_control_center");
        Assert.Equal("ui://helios/control-center-v2.html",
            renderTool.GetProperty("_meta").GetProperty("openai/outputTemplate").GetString());
        Assert.True(renderTool.GetProperty("_meta").GetProperty("openai/widgetAccessible").GetBoolean());

        using var resourcesRequest = CreateMcpRequest("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"resources/list\"}");
        using var resourcesResponse = await client.SendAsync(resourcesRequest);
        var resourcesBody = await resourcesResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resourcesResponse.StatusCode);
        using var resourcesDocument = JsonDocument.Parse(resourcesBody);
        var resource = Assert.Single(resourcesDocument.RootElement.GetProperty("result")
            .GetProperty("resources").EnumerateArray());
        Assert.Equal("text/html;profile=mcp-app", resource.GetProperty("mimeType").GetString());
        Assert.Equal("ui://helios/control-center-v2.html", resource.GetProperty("uri").GetString());
        var resourceMetadata = resource.GetProperty("_meta");
        Assert.Equal("https://helios.example.test",
            resourceMetadata.GetProperty("ui").GetProperty("domain").GetString());
        Assert.Equal("https://helios.example.test",
            resourceMetadata.GetProperty("openai/widgetDomain").GetString());
        var redirectDomains = resourceMetadata.GetProperty("openai/widgetCSP")
            .GetProperty("redirect_domains").EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();
        Assert.Contains("https://github.com", redirectDomains);
        Assert.Contains("https://heli0s-my.sharepoint.com", redirectDomains);
        Assert.Contains("https://helios-xk97943.slack.com", redirectDomains);
        Assert.Contains("https://linear.app", redirectDomains);
        Assert.Contains("https://helios-control-center.thepatman64.chatgpt.site", redirectDomains);

        using var resourceRequest = CreateMcpRequest(
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"resources/read\",\"params\":{\"uri\":\"ui://helios/control-center-v2.html\"}}");
        using var resourceResponse = await client.SendAsync(resourceRequest);
        var resourceBody = await resourceResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resourceResponse.StatusCode);
        using var resourceDocument = JsonDocument.Parse(resourceBody);
        var widget = Assert.Single(resourceDocument.RootElement.GetProperty("result")
            .GetProperty("contents").EnumerateArray()).GetProperty("text").GetString()!;
        Assert.Contains("textContent", widget);
        Assert.DoesNotContain("innerHTML", widget, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ui/notifications/tool-result", widget);
        Assert.Contains("openExternal", widget);
        Assert.Contains("event.source!==window.parent", widget);

        using var searchRequest = CreateMcpRequest(
            "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"tools/call\",\"params\":{\"name\":\"search\",\"arguments\":{\"query\":\"Azure OIDC\"}}}",
            "openid access_as_user");
        using var searchResponse = await client.SendAsync(searchRequest);
        using var searchDocument = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.True(searchDocument.RootElement.GetProperty("result")
            .GetProperty("structuredContent").TryGetProperty("results", out _));

        using var fetchRequest = CreateMcpRequest(
            "{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"tools/call\",\"params\":{\"name\":\"fetch\",\"arguments\":{\"id\":\"github\"}}}",
            "openid access_as_user");
        using var fetchResponse = await client.SendAsync(fetchRequest);
        using var fetchDocument = JsonDocument.Parse(await fetchResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);
        Assert.Equal("github", fetchDocument.RootElement.GetProperty("result")
            .GetProperty("structuredContent").GetProperty("id").GetString());

        using var renderRequest = CreateMcpRequest(
            "{\"jsonrpc\":\"2.0\",\"id\":6,\"method\":\"tools/call\",\"params\":{\"name\":\"helios_render_control_center\",\"arguments\":{}}}",
            "openid access_as_user");
        using var renderResponse = await client.SendAsync(renderRequest);
        using var renderDocument = JsonDocument.Parse(await renderResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, renderResponse.StatusCode);
        Assert.Equal("governed-configuration-snapshot", renderDocument.RootElement.GetProperty("result")
            .GetProperty("structuredContent").GetProperty("source").GetString());
    }

    [Fact]
    public async Task OAuth_metadata_and_tool_challenge_use_client_id_access_scope()
    {
        const string applicationIdUri = "api://11111111-1111-1111-1111-111111111111";
        await using var securedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true");
            builder.UseSetting("HELIOS_ENTRA_CLIENT_ID", "11111111-1111-1111-1111-111111111111");
            builder.UseSetting("HELIOS_ALLOWED_CLIENT_IDS", AllowedOAuthClientId);
            builder.UseSetting("HELIOS_ENTRA_APPLICATION_ID_URI", applicationIdUri);
            builder.UseSetting("HELIOS_PUBLIC_BASE_URL", "https://helios.example.test");
        });
        using var client = securedFactory.CreateClient();

        using (var metadataResponse = await client.GetAsync("/.well-known/oauth-protected-resource/mcp"))
        {
            var body = await metadataResponse.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, metadataResponse.StatusCode);
            using var metadata = JsonDocument.Parse(body);
            Assert.Equal("https://helios.example.test/mcp", metadata.RootElement.GetProperty("resource").GetString());
            Assert.Equal(applicationIdUri + "/access_as_user",
                Assert.Single(metadata.RootElement.GetProperty("scopes_supported").EnumerateArray()).GetString());
        }

        using (var discoveryRequest = CreateMcpRequest(
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}"))
        using (var discoveryResponse = await client.SendAsync(discoveryRequest))
        {
            Assert.Equal(HttpStatusCode.OK, discoveryResponse.StatusCode);
            using var discoveryDocument = JsonDocument.Parse(await discoveryResponse.Content.ReadAsStringAsync());
            var statusTool = Assert.Single(
                discoveryDocument.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray(),
                tool => tool.GetProperty("name").GetString() == "helios_get_control_plane_status");
            Assert.Equal("oauth2", Assert.Single(statusTool.GetProperty("securitySchemes")
                .EnumerateArray()).GetProperty("type").GetString());
        }

        const string toolCall =
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"helios_get_control_plane_status\",\"arguments\":{}}}";
        using (var wrongScopeRequest = CreateMcpRequest(toolCall, "wrong_scope"))
        using (var wrongScopeResponse = await client.SendAsync(wrongScopeRequest))
        {
            Assert.Equal(HttpStatusCode.OK, wrongScopeResponse.StatusCode);
            using var challengeDocument = JsonDocument.Parse(await wrongScopeResponse.Content.ReadAsStringAsync());
            var result = challengeDocument.RootElement.GetProperty("result");
            Assert.True(result.GetProperty("isError").GetBoolean());
            var challenge = Assert.Single(result.GetProperty("_meta")
                .GetProperty("mcp/www_authenticate").EnumerateArray()).GetString()!;
            Assert.Contains("resource_metadata=\"https://helios.example.test/.well-known/oauth-protected-resource/mcp\"", challenge);
            Assert.Contains("scope=\"" + applicationIdUri + "/access_as_user\"", challenge);
            Assert.Contains("error=\"invalid_token\"", challenge);
        }

        using (var wrongAudienceRequest = CreateMcpRequest(
            toolCall,
            "access_as_user",
            "22222222-2222-2222-2222-222222222222"))
        using (var wrongAudienceResponse = await client.SendAsync(wrongAudienceRequest))
        {
            Assert.Equal(HttpStatusCode.OK, wrongAudienceResponse.StatusCode);
            using var challengeDocument = JsonDocument.Parse(await wrongAudienceResponse.Content.ReadAsStringAsync());
            Assert.True(challengeDocument.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        }

        using (var wrongClientRequest = CreateMcpRequest(
            toolCall,
            "access_as_user",
            "11111111-1111-1111-1111-111111111111",
            "33333333-3333-3333-3333-333333333333"))
        using (var wrongClientResponse = await client.SendAsync(wrongClientRequest))
        {
            Assert.Equal(HttpStatusCode.OK, wrongClientResponse.StatusCode);
            using var challengeDocument = JsonDocument.Parse(await wrongClientResponse.Content.ReadAsStringAsync());
            Assert.True(challengeDocument.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        }

        using var getResponse = await client.GetAsync("/mcp");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
        var httpChallenge = Assert.Single(getResponse.Headers.WwwAuthenticate).ToString();
        Assert.Contains("resource_metadata=\"https://helios.example.test/.well-known/oauth-protected-resource/mcp\"", httpChallenge);
        Assert.Contains("scope=\"" + applicationIdUri + "/access_as_user\"", httpChallenge);
    }

    [Fact]
    public async Task Automation_plan_is_deterministic_and_never_applies_from_rest()
    {
        const string payload = "{\"intent\":\"repair-issue\",\"environment\":\"dev\",\"target\":\"JOH-36\",\"connector\":\"linear\"}";
        using var firstContent = new StringContent(payload, Encoding.UTF8, "application/json");
        using var secondContent = new StringContent(payload, Encoding.UTF8, "application/json");
        using var first = await _client.PostAsync("/automation/plan", firstContent);
        using var second = await _client.PostAsync("/automation/plan", secondContent);
        var firstBody = await first.Content.ReadAsStringAsync();
        var secondBody = await second.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(firstBody, secondBody);
        Assert.Contains("\"mode\":\"plan-only\"", firstBody);
        Assert.Contains("\"canApplyFromMcp\":false", firstBody);
        Assert.Contains("\"directMainWrite\":false", firstBody);
        Assert.Contains("open-draft-pull-request", firstBody);
    }

    [Fact]
    public async Task Automation_plan_rejects_secret_rotation_without_target()
    {
        using var content = new StringContent("{\"intent\":\"rotate-secret\",\"environment\":\"dev\"}", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/automation/plan", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Azure_mcp_exposes_plan_only_automation_tool()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json")
        };
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("helios_plan_automation", body);
        Assert.Contains("readOnlyHint", body);
        Assert.DoesNotContain("helios_apply", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Azure_mcp_returns_issue_repair_plan_without_apply_capability()
    {
        const string payload = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"helios_plan_automation\",\"arguments\":{\"intent\":\"repair-issue\",\"environment\":\"dev\",\"target\":\"JOH-36\",\"connector\":\"linear\"}}}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("canApplyFromMcp", body);
        Assert.Contains("open-draft-pull-request", body);
        Assert.DoesNotContain("automaticMerge\\\":true", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Setup_wizard_is_served_and_bootstrap_remains_plan_only()
    {
        using var page = await _client.GetAsync("/setup");
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Azure Setup Wizard", html);

        const string payload = "{\"tenantId\":\"11111111-1111-1111-1111-111111111111\",\"subscriptionId\":\"22222222-2222-2222-2222-222222222222\",\"resourceGroup\":\"helios-dev-rg\",\"environment\":\"dev\"}";
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/setup/bootstrap", content);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("-Mode Diagnose", body);
        Assert.Contains("-Mode Plan", body);
        Assert.DoesNotContain("-Mode Apply", body);
        Assert.Contains("\"appliesChanges\":false", body);
    }

    [Fact]
    public async Task Mcp_exposes_upgrade_proposals_but_no_upgrade_apply_tool()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json")
        };
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("helios_propose_upgrade", body);
        Assert.DoesNotContain("helios_apply_upgrade", body, StringComparison.OrdinalIgnoreCase);
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

    private static HttpRequestMessage CreateMcpRequest(
        string body,
        string? scopes = null,
        string audience = "11111111-1111-1111-1111-111111111111",
        string authorizedParty = AllowedOAuthClientId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (scopes is not null)
        {
            request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "test-principal");
            request.Headers.Add("X-MS-CLIENT-PRINCIPAL", CreateEasyAuthPrincipal(scopes, audience, authorizedParty));
        }
        return request;
    }

    private static string CreateEasyAuthPrincipal(
        string scopes,
        string audience = "11111111-1111-1111-1111-111111111111",
        string authorizedParty = AllowedOAuthClientId)
    {
        var principal = JsonSerializer.Serialize(new
        {
            claims = new[]
            {
                new { typ = "aud", val = audience },
                new { typ = "scp", val = scopes },
                new { typ = "azp", val = authorizedParty }
            }
        });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(principal));
    }

    private static HttpRequestMessage CreateSignedSlackWebhook(long timestamp, string body)
    {
        var baseString = $"v0:{timestamp}:{body}";
        var signature = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(SlackSigningSecret),
            Encoding.UTF8.GetBytes(baseString))).ToLowerInvariant();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/slack")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Slack-Request-Timestamp", timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-Slack-Signature", $"v0={signature}");
        return request;
    }

    private sealed class ChunkedJsonContent : HttpContent
    {
        private readonly byte[] _bytes;

        internal ChunkedJsonContent(byte[] bytes)
        {
            _bytes = bytes;
            Headers.TryAddWithoutValidation("Content-Type", "application/json");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(_bytes, 0, _bytes.Length);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        Environment.SetEnvironmentVariable("GITHUB_WEBHOOK_SECRET", _originalGitHubWebhookSecret);
        Environment.SetEnvironmentVariable("LINEAR_WEBHOOK_SECRET", _originalLinearWebhookSecret);
        Environment.SetEnvironmentVariable("SLACK_SIGNING_SECRET", _originalSlackSigningSecret);
    }
}
