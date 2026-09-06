using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using HELIOS.Fabric.Contracts;
using HELIOS.Fabric.Worker;
using HELIOS.Fabric.Worker.Delivery;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;
var clientId = configuration["AZURE_CLIENT_ID"];
var credential = string.IsNullOrWhiteSpace(clientId)
    ? new DefaultAzureCredential()
    : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });
builder.Services.AddSingleton<TokenCredential>(credential);

var serviceBusNamespace = configuration["HELIOS_SERVICEBUS_NAMESPACE"]
    ?? throw new InvalidOperationException("HELIOS_SERVICEBUS_NAMESPACE is required.");
builder.Services.AddSingleton(new ServiceBusClient(serviceBusNamespace, credential));

var storageAccount = configuration["HELIOS_EVIDENCE_STORAGE_ACCOUNT"]
    ?? throw new InvalidOperationException("HELIOS_EVIDENCE_STORAGE_ACCOUNT is required.");
builder.Services.AddSingleton(new BlobServiceClient(new Uri($"https://{storageAccount}.blob.core.windows.net"), credential));

var vaultUri = configuration["HELIOS_KEYVAULT_URI"]
    ?? throw new InvalidOperationException("HELIOS_KEYVAULT_URI is required.");
builder.Services.AddSingleton(new SecretProvider(new SecretClient(new Uri(vaultUri), credential)));

var configRoot = configuration["HELIOS_CONFIG_ROOT"] ?? "/app/config";
builder.Services.AddSingleton(ConnectorConfiguration.Load(
    Path.Combine(configRoot, "fabric", "connector-registry.json"),
    Path.Combine(configRoot, "fabric", "routing-policy.json")));
builder.Services.AddSingleton<DeliveryTemplateRenderer>();
builder.Services.AddSingleton<ConnectorStateStore>();
builder.Services.AddSingleton<GitHubAppTokenProvider>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IDeliverySink, SlackSink>();
builder.Services.AddSingleton<IDeliverySink, LinearSink>();
builder.Services.AddSingleton<IDeliverySink, TeamsSink>();
builder.Services.AddSingleton<IDeliverySink, SharePointEvidenceSink>();
builder.Services.AddSingleton<IDeliverySink, GitHubControlSink>();
builder.Services.AddHostedService<FabricWorker>();
await builder.Build().RunAsync();

