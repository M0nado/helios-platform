using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HELIOS.AIHub.Control;
using Xunit;

namespace HELIOS.AIHub.Tests;

public sealed class ControlServerIntegrationTests : IAsyncLifetime
{
    private ControlServer? _server;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _server = ControlServer.CreateEphemeral();
        await _server.StartAsync();
        _client = new HttpClient { BaseAddress = _server.BaseAddress };
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("/health", "status")]
    [InlineData("/meta", "implementedEndpoints")]
    [InlineData("/memory/search?q=routing", "results")]
    [InlineData("/agents/list", "agents")]
    [InlineData("/teams", "teams")]
    [InlineData("/api/fleet/live", "topology")]
    [InlineData("/api/security/live", "trainingPolicy")]
    [InlineData("/api/engines/catalog", "engines")]
    [InlineData("/api/engines/recommend?task=security%20routing", "primaryEngineId")]
    [InlineData("/api/setup/tracker", "steps")]
    [InlineData("/api/setup/autoboot-plan", "services")]
    public async Task GetEndpoints_ReturnExpectedJsonShape(string endpoint, string requiredProperty)
    {
        using var document = await GetJsonAsync(endpoint);

        Assert.True(document.RootElement.TryGetProperty(requiredProperty, out _), $"Expected property '{requiredProperty}' in {endpoint} response.");
    }

    [Fact]
    public async Task CreateTask_ReturnsAcceptedTaskShape()
    {
        var response = await Client.PostAsJsonAsync("/tasks/create", new
        {
            prompt = "Route a local security analysis task.",
            intent = "security-routing",
            metadata = new Dictionary<string, string> { ["source"] = "integration-test" }
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("accepted", document.RootElement.GetProperty("status").GetString());
        Assert.StartsWith("task-", document.RootElement.GetProperty("taskId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("route").GetString()));
    }

    [Fact]
    public async Task TriggerTraining_ReturnsCompletedRunShape()
    {
        var response = await Client.PostAsJsonAsync("/api/train/trigger", new
        {
            objective = "integration-shape-validation",
            weaknesses = new[] { "routing", "memory" },
            maxTasks = 2,
            explorationRate = 0.0
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("completed", document.RootElement.GetProperty("status").GetString());
        var run = document.RootElement.GetProperty("run");
        Assert.StartsWith("train-", run.GetProperty("runId").GetString());
        Assert.Equal(2, run.GetProperty("tasks").GetArrayLength());
        Assert.True(run.GetProperty("averageQualityScore").GetDouble() > 0);
    }

    private async Task<JsonDocument> GetJsonAsync(string endpoint)
    {
        var response = await Client.GetAsync(endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private HttpClient Client => _client ?? throw new InvalidOperationException("Test client was not initialized.");
}
