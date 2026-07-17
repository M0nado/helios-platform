using System.Text.Json;
using System.Threading.RateLimiting;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using HELIOS.Fabric.Broker.Security;
using HELIOS.Fabric.Broker.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

const long maximumBodyBytes = 1_048_576;
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maximumBodyBytes);
builder.Services.Configure<FormOptions>(options =>
{
    options.BufferBody = false;
    options.MultipartBodyLengthLimit = maximumBodyBytes;
    options.ValueLengthLimit = (int)maximumBodyBytes;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{context.Request.Path}:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<HmacVerifier>();
builder.Services.AddSingleton<CorrelationStatusReader>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    var account = configuration["HELIOS_EVIDENCE_STORAGE_ACCOUNT"];
    if (string.IsNullOrWhiteSpace(account))
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("HELIOS_EVIDENCE_STORAGE_ACCOUNT is required outside Development.");
        return new CorrelationStatusReader(new BlobServiceClient("UseDevelopmentStorage=true"));
    }
    var clientId = configuration["AZURE_CLIENT_ID"];
    var credential = string.IsNullOrWhiteSpace(clientId)
        ? new DefaultAzureCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });
    return new CorrelationStatusReader(new BlobServiceClient(new Uri($"https://{account}.blob.core.windows.net"), credential));
});
builder.Services.AddSingleton<WebhookNormalizer>();
builder.Services.AddSingleton<ISecretResolver>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    var vaultUri = configuration["HELIOS_KEYVAULT_URI"];
    var allowEnvironment = configuration.GetValue("HELIOS_ALLOW_ENV_SECRETS", environment.IsDevelopment());
    if (Uri.TryCreate(vaultUri, UriKind.Absolute, out var uri))
    {
        var clientId = configuration["AZURE_CLIENT_ID"];
        var credential = string.IsNullOrWhiteSpace(clientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });
        return new CompositeSecretResolver(new KeyVaultSecretResolver(new SecretClient(uri, credential)), allowEnvironment);
    }
    if (!environment.IsDevelopment())
    {
        throw new InvalidOperationException("HELIOS_KEYVAULT_URI is required outside Development.");
    }
    return new CompositeSecretResolver(null, allowEnvironment);
});
builder.Services.AddSingleton<IEventPublisher>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    var fullyQualifiedNamespace = configuration["HELIOS_SERVICEBUS_NAMESPACE"];
    var topic = configuration["HELIOS_SERVICEBUS_TOPIC"] ?? "helios-events";
    if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("HELIOS_SERVICEBUS_NAMESPACE is required outside Development.");
        }
        return new InMemoryEventPublisher();
    }
    var clientId = configuration["AZURE_CLIENT_ID"];
    var credential = string.IsNullOrWhiteSpace(clientId)
        ? new DefaultAzureCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });
    return new ServiceBusEventPublisher(new ServiceBusClient(fullyQualifiedNamespace, credential), topic);
});

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError;
    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    var payload = JsonSerializer.Serialize(new
    {
        type = "about:blank",
        title = status == 500 ? "Internal server error" : "Request rejected",
        status,
        traceId = context.TraceIdentifier
    });
    await context.Response.WriteAsync(payload);
}));
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Cache-Control"] = "no-store";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", (IEventPublisher publisher) =>
{
    var localOnly = publisher is InMemoryEventPublisher;
    return Results.Json(
        new { status = localOnly ? "development-only" : "ready", publisher = publisher.GetType().Name },
        statusCode: localOnly && !app.Environment.IsDevelopment() ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK);
});

WebhookEndpoints.Map(app);
app.Run();

public partial class Program;
