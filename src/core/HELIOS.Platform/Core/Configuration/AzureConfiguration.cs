using System;
using Microsoft.Extensions.Configuration;

namespace HELIOS.Platform.Core.Configuration
{
    /// <summary>
    /// Configuration for Azure cloud services
    /// </summary>
    public class AzureConfiguration
    {
        public string? TenantId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? StorageAccountName { get; set; }
        public string? StorageAccountKey { get; set; }
        public string? KeyVaultUri { get; set; }
        public string? CosmosDbEndpoint { get; set; }
        public string? CosmosDbKey { get; set; }
        public string? SqlConnectionString { get; set; }
        public AuthenticationMethod AuthMethod { get; set; } = AuthenticationMethod.DefaultAzureCredential;
        public int TokenCacheExpiryMinutes { get; set; } = 60;
    }

    /// <summary>
    /// Azure authentication methods
    /// </summary>
    public enum AuthenticationMethod
    {
        DefaultAzureCredential,
        DeviceFlow,
        Interactive,
        ServicePrincipal,
        ManagedIdentity
    }

    /// <summary>
    /// Configuration builder for Azure services
    /// </summary>
    public class CloudConfigurationBuilder
    {
        private readonly AzureConfiguration _config;

        public CloudConfigurationBuilder()
        {
            _config = new AzureConfiguration();
        }

        /// <summary>
        /// Loads configuration from IConfiguration
        /// </summary>
        public static CloudConfigurationBuilder FromConfiguration(IConfiguration configuration)
        {
            var builder = new CloudConfigurationBuilder();
            configuration.GetSection("Azure").Bind(builder._config);
            return builder;
        }

        /// <summary>
        /// Sets tenant ID
        /// </summary>
        public CloudConfigurationBuilder WithTenantId(string tenantId)
        {
            _config.TenantId = tenantId;
            return this;
        }

        /// <summary>
        /// Sets subscription ID
        /// </summary>
        public CloudConfigurationBuilder WithSubscriptionId(string subscriptionId)
        {
            _config.SubscriptionId = subscriptionId;
            return this;
        }

        /// <summary>
        /// Sets service principal credentials
        /// </summary>
        public CloudConfigurationBuilder WithServicePrincipal(string clientId, string clientSecret)
        {
            _config.ClientId = clientId;
            _config.ClientSecret = clientSecret;
            _config.AuthMethod = AuthenticationMethod.ServicePrincipal;
            return this;
        }

        /// <summary>
        /// Sets storage account
        /// </summary>
        public CloudConfigurationBuilder WithStorageAccount(string accountName, string accountKey)
        {
            _config.StorageAccountName = accountName;
            _config.StorageAccountKey = accountKey;
            return this;
        }

        /// <summary>
        /// Sets Key Vault URI
        /// </summary>
        public CloudConfigurationBuilder WithKeyVault(string keyVaultUri)
        {
            _config.KeyVaultUri = keyVaultUri;
            return this;
        }

        /// <summary>
        /// Sets CosmosDB endpoint
        /// </summary>
        public CloudConfigurationBuilder WithCosmosDb(string endpoint, string key)
        {
            _config.CosmosDbEndpoint = endpoint;
            _config.CosmosDbKey = key;
            return this;
        }

        /// <summary>
        /// Sets SQL connection string
        /// </summary>
        public CloudConfigurationBuilder WithSqlConnectionString(string connectionString)
        {
            _config.SqlConnectionString = connectionString;
            return this;
        }

        /// <summary>
        /// Sets authentication method
        /// </summary>
        public CloudConfigurationBuilder WithAuthenticationMethod(AuthenticationMethod method)
        {
            _config.AuthMethod = method;
            return this;
        }

        /// <summary>
        /// Builds the configuration
        /// </summary>
        public AzureConfiguration Build()
        {
            if (string.IsNullOrEmpty(_config.TenantId))
                throw new InvalidOperationException("Tenant ID must be configured");

            return _config;
        }
    }
}
