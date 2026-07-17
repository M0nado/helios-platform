using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class WebhookTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public WebhookTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Unknown_provider_is_not_found() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync("/webhooks/nope", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);

    [Fact]
    public async Task Empty_payload_is_rejected() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync("/webhooks/github", new StringContent(""))).StatusCode);

    [Fact]
    public async Task Invalid_json_is_rejected() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync("/webhooks/github", new StringContent("not-json", Encoding.UTF8, "application/json"))).StatusCode);

    [Fact]
    public async Task Health_routes_preserve_legacy_and_report_live_and_ready()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Readiness_fails_closed_when_cloud_identity_configuration_is_missing()
    {
        await using var securedFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true");
            builder.UseSetting("AZURE_TENANT_ID", string.Empty);
            builder.UseSetting("AZURE_SUBSCRIPTION_ID", string.Empty);
            builder.UseSetting("AZURE_RESOURCE_GROUP", string.Empty);
            builder.UseSetting("AZURE_CLIENT_ID", string.Empty);
        });
        using var client = securedFactory.CreateClient();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Local_mcp_lists_only_read_tools()
    {
        var request = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/runtime/webhooks/mcp", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hermes_get_status", body);
        Assert.DoesNotContain("run_sandbox", body);
    }

    [Fact]
    public async Task Azure_connector_fails_closed_without_Entra_identity()
    {
        await using var securedFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true"));
        using var client = securedFactory.CreateClient();
        var response = await client.GetAsync("/connector/context");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Azure_mcp_exposes_only_inventory_tools()
    {
        await using var securedFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("HELIOS_REQUIRE_ENTRA_AUTH", "true"));
        using var client = securedFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "test-principal");
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("azure_list_resources", body);
        Assert.Contains("azure_list_foundry_resources", body);
        Assert.DoesNotContain("deploy", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("role_assignment", body, StringComparison.OrdinalIgnoreCase);
    }
}
