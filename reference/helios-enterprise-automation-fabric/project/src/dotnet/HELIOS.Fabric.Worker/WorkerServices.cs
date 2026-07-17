using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using HELIOS.Fabric.Contracts;

namespace HELIOS.Fabric.Worker.Delivery;

public sealed class ConnectorConfiguration
{
    private static readonly IReadOnlyDictionary<string, int> Severity = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["debug"] = 0, ["info"] = 1, ["notice"] = 2, ["warning"] = 3, ["error"] = 4, ["critical"] = 5
    };
    private readonly IReadOnlyDictionary<string, bool> _enabled;
    private readonly IReadOnlyList<RouteDefinition> _routes;

    private ConnectorConfiguration(IReadOnlyDictionary<string, bool> enabled, IReadOnlyList<RouteDefinition> routes)
    {
        _enabled = enabled;
        _routes = routes;
    }

    public static ConnectorConfiguration Load(string registryPath, string routingPath)
    {
        using var registry = JsonDocument.Parse(File.ReadAllText(registryPath));
        using var routing = JsonDocument.Parse(File.ReadAllText(routingPath));
        if (registry.RootElement.GetProperty("defaultMode").GetString() != "deny")
        {
            throw new InvalidOperationException("Connector registry must be deny-by-default.");
        }
        var enabled = registry.RootElement.GetProperty("connectors").EnumerateArray()
            .ToDictionary(
                item => item.GetProperty("id").GetString()!,
                item => item.GetProperty("enabled").GetBoolean(),
                StringComparer.Ordinal);
        var routes = routing.RootElement.GetProperty("routes").EnumerateArray()
            .Select(RouteDefinition.Parse)
            .OrderByDescending(item => item.Priority)
            .ToArray();
        return new ConnectorConfiguration(enabled, routes);
    }

    public IReadOnlyList<DeliveryRoute> Resolve(EventEnvelope envelope)
    {
        var result = new List<DeliveryRoute>();
        foreach (var route in _routes)
        {
            if (!route.Enabled || !route.Matches(envelope, Severity)) continue;
            result.AddRange(route.Sinks
                .Where(sink => _enabled.GetValueOrDefault(sink, false))
                .Select(sink => new DeliveryRoute(sink, route.Transform, route.ApprovalPolicy, route.Id)));
            if (!route.Continue) break;
        }
        return result;
    }

    private sealed record RouteDefinition(
        string Id,
        bool Enabled,
        int Priority,
        string[] EventTypes,
        string[] Environments,
        string MinimumSeverity,
        string[] RequestedActions,
        string[] Sinks,
        string Transform,
        string? ApprovalPolicy,
        bool Continue)
    {
        public static RouteDefinition Parse(JsonElement value)
        {
            var match = value.GetProperty("match");
            static string[] Strings(JsonElement parent, string property) =>
                parent.GetProperty(property).EnumerateArray().Select(item => item.GetString()!).ToArray();
            return new RouteDefinition(
                value.GetProperty("id").GetString()!,
                value.GetProperty("enabled").GetBoolean(),
                value.GetProperty("priority").GetInt32(),
                Strings(match, "eventTypes"),
                Strings(match, "environments"),
                match.GetProperty("minimumSeverity").GetString()!,
                Strings(match, "requestedActions"),
                Strings(value, "sinks"),
                value.GetProperty("transform").GetString()!,
                value.GetProperty("approvalPolicy").ValueKind == JsonValueKind.Null ? null : value.GetProperty("approvalPolicy").GetString(),
                value.GetProperty("continue").GetBoolean());
        }

        public bool Matches(EventEnvelope envelope, IReadOnlyDictionary<string, int> severity)
        {
            if (!Environments.Contains(envelope.Environment, StringComparer.Ordinal)) return false;
            if (severity.GetValueOrDefault(envelope.Severity, -1) < severity.GetValueOrDefault(MinimumSeverity, 0)) return false;
            if (!EventTypes.Any(pattern => Wildcard(pattern, envelope.EventType))) return false;
            return RequestedActions.Length == 0 || RequestedActions.Intersect(envelope.RequestedActions, StringComparer.Ordinal).Any();
        }

        private static bool Wildcard(string pattern, string value)
        {
            if (pattern.EndsWith('*')) return value.StartsWith(pattern[..^1], StringComparison.Ordinal);
            return string.Equals(pattern, value, StringComparison.Ordinal);
        }
    }
}

public sealed class SecretProvider(SecretClient client)
{
    public async Task<string> RequiredAsync(string name, CancellationToken cancellationToken)
    {
        var value = (await client.GetSecretAsync(name, cancellationToken: cancellationToken)).Value.Value;
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Key Vault secret {name} is empty.")
            : value;
    }
}

public sealed record LinearPresentation(string Title, string Description, int Priority);

public sealed class DeliveryTemplateRenderer
{
    public LinearPresentation RenderLinear(EventEnvelope envelope, DeliveryRoute route) =>
        new(
            $"[HELIOS] {envelope.Summary}",
            $"Event: `{envelope.EventType}`\n\nCorrelation: `{envelope.CorrelationId}`\n\n{envelope.Summary}\n\nRoute: `{route.RouteId}`",
            envelope.Severity is "critical" or "error" ? 1 : 2);

    public JsonObject RenderSlack(EventEnvelope envelope, DeliveryRoute route) =>
        new()
        {
            ["blocks"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "section",
                    ["text"] = new JsonObject
                    {
                        ["type"] = "mrkdwn",
                        ["text"] = $"*HELIOS {envelope.Severity.ToUpperInvariant()}*\n{envelope.Summary}\n`{envelope.CorrelationId}`"
                    }
                }
            }
        };

    public object RenderTeams(EventEnvelope envelope, DeliveryRoute route) => new
    {
        type = "AdaptiveCard",
        version = "1.5",
        body = new object[]
        {
            new { type = "TextBlock", size = "Large", weight = "Bolder", text = $"HELIOS {envelope.Severity}" },
            new { type = "TextBlock", wrap = true, text = envelope.Summary },
            new { type = "TextBlock", wrap = true, text = $"Correlation: {envelope.CorrelationId}" }
        },
        route = route.RouteId
    };
}

public sealed record LinearIssueState(string IssueId, string Identifier, string Url, DateTimeOffset CreatedAt);

public sealed class ConnectorStateStore(BlobServiceClient client)
{
    private BlobContainerClient Container => client.GetBlobContainerClient("connector-state");

    public async Task<LinearIssueState?> ReadLinearAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        var blob = Container.GetBlobClient($"linear/{correlationId}.json");
        if (!(await blob.ExistsAsync(cancellationToken)).Value) return null;
        var content = await blob.DownloadContentAsync(cancellationToken);
        return content.Value.Content.ToObjectFromJson<LinearIssueState>(FabricJson.Options);
    }

    public async Task WriteLinearOnceAsync(Guid correlationId, LinearIssueState state, CancellationToken cancellationToken)
    {
        var blob = Container.GetBlobClient($"linear/{correlationId}.json");
        try
        {
            await blob.UploadAsync(BinaryData.FromObjectAsJson(state, FabricJson.Options), overwrite: false, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
        }
    }
}

public sealed class GitHubAppTokenProvider(
    IHttpClientFactory clients,
    SecretProvider secrets,
    IConfiguration configuration,
    TimeProvider? timeProvider = null)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && _expiresAt > _time.GetUtcNow().AddMinutes(5)) return _token;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && _expiresAt > _time.GetUtcNow().AddMinutes(5)) return _token;
            var appId = configuration["HELIOS_GITHUB_APP_ID"] ?? await secrets.RequiredAsync("github-app-id", cancellationToken);
            var installationId = configuration["HELIOS_GITHUB_INSTALLATION_ID"] ?? await secrets.RequiredAsync("github-app-installation-id", cancellationToken);
            var privateKey = await secrets.RequiredAsync("github-app-private-key", cancellationToken);
            var jwt = CreateJwt(appId, privateKey, _time.GetUtcNow());
            var client = clients.CreateClient(nameof(GitHubAppTokenProvider));
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "helios-enterprise-fabric/2.0");
            using var response = await client.PostAsync($"app/installations/{Uri.EscapeDataString(installationId)}/access_tokens", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<InstallationToken>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("GitHub returned an empty installation token.");
            _token = payload.Token;
            _expiresAt = payload.ExpiresAt;
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string CreateJwt(string appId, string privateKey, DateTimeOffset now)
    {
        static string Encode(ReadOnlySpan<byte> value) =>
            Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iat = now.AddSeconds(-30).ToUnixTimeSeconds(),
            exp = now.AddMinutes(9).ToUnixTimeSeconds(),
            iss = appId
        }));
        var unsigned = $"{header}.{payload}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{unsigned}.{Encode(signature)}";
    }

    private sealed record InstallationToken(
        [property: System.Text.Json.Serialization.JsonPropertyName("token")] string Token,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_at")] DateTimeOffset ExpiresAt);
}

