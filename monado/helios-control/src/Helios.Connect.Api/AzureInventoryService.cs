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
    private const int MaximumPages = 100;
    private const int MaximumResources = 10_000;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly TokenCredential _credential;

    public AzureInventoryService(HttpClient httpClient, IConfiguration configuration)
        : this(httpClient, configuration, CreateCredential(configuration))
    {
    }

    public AzureInventoryService(HttpClient httpClient, IConfiguration configuration, TokenCredential credential)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _credential = credential;
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
        Uri? uri = new Uri(
            $"https://management.azure.com/subscriptions/{subscription}/resourceGroups/{resourceGroup}/resources?api-version=2021-04-01");

        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(new[] { ManagementScope }),
            cancellationToken);
        var resources = new List<AzureInventoryResource>();
        for (var page = 0; uri is not null; page++)
        {
            if (page >= MaximumPages)
                throw new InvalidOperationException($"Azure inventory exceeded the {MaximumPages}-page safety limit.");

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("value", out var values) && values.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in values.EnumerateArray())
                {
                    var type = ReadString(item, "type") ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(typePrefix)
                        && !type.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase)) continue;
                    if (resources.Count >= MaximumResources)
                        throw new InvalidOperationException($"Azure inventory exceeded the {MaximumResources}-resource safety limit.");
                    resources.Add(new AzureInventoryResource(
                        ReadString(item, "id") ?? string.Empty,
                        ReadString(item, "name") ?? string.Empty,
                        type,
                        ReadString(item, "location")));
                }
            }

            uri = ReadValidatedNextLink(document.RootElement);
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

    private static Uri? ReadValidatedNextLink(JsonElement root)
    {
        var nextLink = ReadString(root, "nextLink");
        if (string.IsNullOrWhiteSpace(nextLink)) return null;
        if (!Uri.TryCreate(nextLink, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, "management.azure.com", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Azure inventory returned an invalid pagination link.");
        return uri;
    }

    private static TokenCredential CreateCredential(IConfiguration configuration)
    {
        var clientId = configuration["AZURE_CLIENT_ID"];
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId
        });
    }
}
