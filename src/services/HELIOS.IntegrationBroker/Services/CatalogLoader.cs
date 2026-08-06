using System.Text.Json;
using HELIOS.IntegrationBroker.Contracts;

namespace HELIOS.IntegrationBroker.Services;

public sealed class CatalogLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CatalogLoader(IConfiguration configuration, IWebHostEnvironment environment)
    {
        ToolCatalog = Load<ToolCatalogDocument>(
            configuration["Broker:ToolCatalogPath"],
            "tool-catalog.json",
            Path.Combine(environment.ContentRootPath, "config", "integrations", "tool-catalog.json"));

        ServiceCatalog = Load<ServiceCatalogDocument>(
            configuration["Broker:ServiceCatalogPath"],
            "service-catalog.json",
            Path.Combine(environment.ContentRootPath, "config", "integrations", "service-catalog.json"));
    }

    public ToolCatalogDocument ToolCatalog { get; }

    public ServiceCatalogDocument ServiceCatalog { get; }

    public ToolDescriptor? FindTool(string name) =>
        ToolCatalog.Tools.FirstOrDefault(tool =>
            string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase));

    private static T Load<T>(string? configuredPath, string outputFileName, string repositoryPath)
    {
        var candidates = new[]
        {
            configuredPath,
            Path.Combine(AppContext.BaseDirectory, outputFileName),
            repositoryPath
        };

        var path = candidates.FirstOrDefault(candidate =>
            !string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate));

        if (path is null)
        {
            throw new InvalidOperationException($"Unable to locate required catalog '{outputFileName}'.");
        }

        var document = JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions);
        return document ?? throw new InvalidOperationException($"Catalog '{path}' could not be deserialized.");
    }
}
