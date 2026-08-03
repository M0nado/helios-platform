namespace HELIOS.Platform.Core.Cloud;
using HELIOS.Platform.Core.Logging;

/// <summary>
/// Factory for creating cloud storage provider instances.
/// </summary>
public interface ICloudStorageProviderFactory
{
    /// <summary>Create a cloud storage provider instance.</summary>
    ICloudStorageProvider CreateProvider(CloudProviderType providerType);

    /// <summary>Get registered providers.</summary>
    IEnumerable<CloudProviderType> GetRegisteredProviders();
}

/// <summary>
/// Default implementation of cloud storage provider factory.
/// </summary>
public class CloudStorageProviderFactory : ICloudStorageProviderFactory
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<CloudProviderType, Func<ICloudStorageProvider>> _providers;

    public CloudStorageProviderFactory(ILogger logger, HttpClient? httpClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();

        _providers = new Dictionary<CloudProviderType, Func<ICloudStorageProvider>>
        {
            { CloudProviderType.OneDrive, () => new OneDriveProvider(_logger, _httpClient) },
            { CloudProviderType.AzureBlob, () => new AzureStorageProvider(_logger) }
        };
    }

    public ICloudStorageProvider CreateProvider(CloudProviderType providerType)
    {
        if (!_providers.TryGetValue(providerType, out var factory))
        {
            _logger.Warning($"Cloud provider not supported: {providerType}");
            throw new NotSupportedException($"Cloud provider not supported: {providerType}");
        }

        _logger.Info($"Creating cloud storage provider: {providerType}");
        return factory();
    }

    public IEnumerable<CloudProviderType> GetRegisteredProviders()
    {
        return _providers.Keys;
    }
}

/// <summary>
/// Extension methods for cloud storage provider registration.
/// </summary>
public static class CloudStorageProviderExtensions
{
    /// <summary>Add cloud storage provider factory to DI container.</summary>
    public static IServiceCollection AddCloudStorageProvider(this IServiceCollection services)
    {
        services.AddSingleton<ICloudStorageProviderFactory, CloudStorageProviderFactory>();
        return services;
    }
}
