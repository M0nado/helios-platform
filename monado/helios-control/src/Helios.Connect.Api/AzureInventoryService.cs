using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace Helios.Connect.Api;

public sealed record AzureConnectorContext(
    string? TenantId,
    string? SubscriptionId,
    string? ResourceGroup,
    string Access,
    bool Configured);

public sealed record AzureInventoryResource(string Id, string Name, string Type, string? Location);

public interface IAzureInventoryService
{
    AzureConnectorContext GetContext();
    Task<IReadOnlyList<AzureInventoryResource>> ListResourcesAsync(string? typePrefix, CancellationToken cancellationToken);
    Task<IReadOnlyList<AzureInventoryResource>> ListFoundryResourcesAsync(CancellationToken cancellationToken);
}

public sealed class AzureInventoryService : IAzureInventoryService
{
    private const string ManagementScope = "https://management.azure.com/.default";
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly TokenCredential _credential;

    public AzureInventoryService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        var clientId = configuration["AZURE_CLIENT_ID"];
        _credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId
        });
    }

    public AzureConnectorContext GetContext()
    {
        var tenantId = _configuration["AZURE_TENANT_ID"];
        var subscriptionId = _configuration["AZURE_SUBSCRIPTION_ID"];
        var resourceGroup = _configuration["AZURE_RESOURCE_GROUP"];
        return new AzureConnectorContext(
            tenantId,
            subscriptionId,
            resourceGroup,
            "read-only-resource-group",
            !string.IsNullOrWhiteSpace(tenantId)
                && !string.IsNullOrWhiteSpace(subscriptionId)
                && !string.IsNullOrWhiteSpace(resourceGroup));
    }

    public async Task<IReadOnlyList<AzureInventoryResource>> ListResourcesAsync(
        string? typePrefix,
        CancellationToken cancellationToken)
    {
        var context = GetContext();
        if (!context.Configured) throw new InvalidOperationException("Azure connector context is incomplete.");
        if (typePrefix is { Length: > 128 }) throw new ArgumentException("typePrefix is too long.", nameof(typePrefix));

        var subscription = Uri.EscapeDataString(context.SubscriptionId!);
        var resourceGroup = Uri.EscapeDataString(context.ResourceGroup!);
        var uri = new Uri(
            $"https://management.azure.com/subscriptions/{subscription}/resourceGroups/{resourceGroup}/resources?api-version=2021-04-01");

        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(new[] { ManagementScope }),
            cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var resources = new List<AzureInventoryResource>();
        if (!document.RootElement.TryGetProperty("value", out var values)) return resources;

        foreach (var item in values.EnumerateArray())
        {
            var type = ReadString(item, "type") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(typePrefix)
                && !type.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase)) continue;
            resources.Add(new AzureInventoryResource(
                ReadString(item, "id") ?? string.Empty,
                ReadString(item, "name") ?? string.Empty,
                type,
                ReadString(item, "location")));
        }

        return resources;
    }

    public async Task<IReadOnlyList<AzureInventoryResource>> ListFoundryResourcesAsync(CancellationToken cancellationToken)
    {
        var resources = await ListResourcesAsync(null, cancellationToken);
        return resources.Where(resource =>
                resource.Type.StartsWith("Microsoft.CognitiveServices/", StringComparison.OrdinalIgnoreCase)
                || resource.Type.StartsWith("Microsoft.MachineLearningServices/", StringComparison.OrdinalIgnoreCase)
                || resource.Type.StartsWith("Microsoft.Search/", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
