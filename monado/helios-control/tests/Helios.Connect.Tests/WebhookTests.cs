using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class WebhookTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebhookTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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
    public async Task Local_mcp_lists_only_read_tools()
    {
        using var request = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/runtime/webhooks/mcp", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hermes_get_status", body);
        Assert.DoesNotContain("run_sandbox", body);
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

    public void Dispose() => _client.Dispose();
}
