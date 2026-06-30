using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HELIOS.Platform.Minimal.Tests;

public sealed class MinimalApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MinimalApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health")]
    [InlineData("/api/status")]
    public async Task Endpoint_ReturnsOk(string path)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        using var client = _factory.CreateClient();

        var payload = await client.GetFromJsonAsync<HealthResponse>("/health");

        Assert.NotNull(payload);
        Assert.Equal("healthy", payload.Status);
    }

    private sealed record HealthResponse(string Status, DateTimeOffset Timestamp);
}
