namespace MonadoBlade.Core.Configuration;

using Microsoft.Extensions.Configuration;
using System.Reflection;

/// <summary>
/// Configures application settings from appsettings.json files.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Creates an IConfiguration instance from appsettings.json.
    /// </summary>
    public static IConfigurationRoot BuildConfiguration(string? basePath = null, string environment = "Development")
    {
        basePath ??= Directory.GetCurrentDirectory();

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Binds configuration section to a strongly-typed object.
    /// </summary>
    public static T GetSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = configuration.GetSection(sectionName);
        var instance = new T();
        section.Bind(instance);
        return instance;
    }
}
