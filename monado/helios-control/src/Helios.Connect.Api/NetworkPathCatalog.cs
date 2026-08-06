using System.Text.Json;

namespace Helios.Connect.Api;

public sealed class NetworkPathCatalog
{
    private readonly JsonElement _document;

    public NetworkPathCatalog(IWebHostEnvironment environment)
    {
        var configuredPath = Environment.GetEnvironmentVariable("HELIOS_NETWORK_PATHS_FILE");
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "network-paths.json")
            : configuredPath;
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        _document = document.RootElement.Clone();
    }

    public object GetEffectivePaths()
    {
        var enabledProfiles = (Environment.GetEnvironmentVariable("HELIOS_ENABLED_EGRESS_PROFILES") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new
        {
            policy = _document,
            enabledProfiles,
            evaluatedAt = DateTimeOffset.UtcNow,
            note = "Only destination groups named in enabledProfiles are effective allows; every other outbound destination is denied."
        };
    }
}
