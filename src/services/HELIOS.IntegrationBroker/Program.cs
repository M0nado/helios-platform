using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HELIOS.IntegrationBroker.Contracts;
using HELIOS.IntegrationBroker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<CatalogLoader>();
builder.Services.AddSingleton<InMemoryBrokerStore>();
builder.Services.AddSingleton<ToolPolicyService>();

var app = builder.Build();
app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/healthz"))
    {
        await next();
        return;
    }

    var requireAuthentication = app.Configuration.GetValue("Broker:RequireAuthentication", true);
    if (!requireAuthentication)
    {
        await next();
        return;
    }

    var configuredToken = Environment.GetEnvironmentVariable("HELIOS_BROKER_TOKEN");
    if (string.IsNullOrWhiteSpace(configuredToken))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "broker_not_configured",
            message = "HELIOS_BROKER_TOKEN must be supplied by the local secret store or Azure Key Vault."
        });
        return;
    }

    var header = context.Request.Headers.Authorization.ToString();
    const string prefix = "Bearer ";
    if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var suppliedToken = header[prefix.Length..].Trim();
    var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(configuredToken));
    var suppliedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken));
    if (!CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});

app.MapHealthChecks("/healthz");

app.MapGet("/api/v1/status", (CatalogLoader catalogs, InMemoryBrokerStore store) => Results.Ok(new
{
    service = "helios-integration-broker",
    version = "0.1.0",
    state = "ready",
    utc = DateTimeOffset.UtcNow,
    toolCount = catalogs.ToolCatalog.Tools.Count,
    connectorCount = catalogs.ServiceCatalog.Services.Count,
    eventCount = store.EventCount,
    pendingActionCount = store.PendingActionCount,
    executionBoundary = "request-and-review"
}));

app.MapGet("/api/v1/tools", (CatalogLoader catalogs) => Results.Ok(catalogs.ToolCatalog));
app.MapGet("/api/v1/connectors", (CatalogLoader catalogs) => Results.Ok(catalogs.ServiceCatalog));

app.MapGet("/api/v1/events/{eventId}", (string eventId, InMemoryBrokerStore store) =>
{
    var integrationEvent = store.GetEvent(eventId);
    return integrationEvent is null ? Results.NotFound() : Results.Ok(integrationEvent);
});

app.MapGet("/api/v1/events", (string? correlationId, int? limit, InMemoryBrokerStore store) =>
    Results.Ok(store.SearchEvents(correlationId, limit ?? 20)));

app.MapPost("/api/v1/events", (IntegrationEvent integrationEvent, InMemoryBrokerStore store) =>
{
    var errors = IntegrationEventValidator.Validate(integrationEvent);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    if (!store.TryAddEvent(integrationEvent))
    {
        return Results.Conflict(new { error = "duplicate_event", integrationEvent.EventId });
    }

    return Results.Accepted($"/api/v1/events/{integrationEvent.EventId}", new
    {
        integrationEvent.EventId,
        integrationEvent.CorrelationId,
        state = "accepted"
    });
});

app.MapPost("/api/v1/tools/preview", (ToolActionRequest request, CatalogLoader catalogs, ToolPolicyService policy) =>
{
    var tool = catalogs.FindTool(request.ToolName);
    if (tool is null)
    {
        return Results.NotFound(new { error = "unknown_tool", request.ToolName });
    }

    return Results.Ok(policy.Preview(request, tool));
});

app.MapPost("/api/v1/tools/requests", (ToolActionRequest request, CatalogLoader catalogs, ToolPolicyService policy, InMemoryBrokerStore store) =>
{
    var tool = catalogs.FindTool(request.ToolName);
    if (tool is null)
    {
        return Results.NotFound(new { error = "unknown_tool", request.ToolName });
    }

    var preview = policy.Preview(request, tool);
    if (!preview.Allowed)
    {
        return Results.BadRequest(preview);
    }

    if (tool.ReadOnly)
    {
        return Results.BadRequest(new
        {
            error = "read_only_tool",
            message = "Read-only tools must be invoked through their dedicated read endpoint; no action request is required."
        });
    }

    var action = store.AddAction(request, tool);
    return Results.Accepted($"/api/v1/tools/requests/{action.RequestId}", action);
});

app.MapGet("/api/v1/tools/requests/{requestId}", (string requestId, InMemoryBrokerStore store) =>
{
    var action = store.GetAction(requestId);
    return action is null ? Results.NotFound() : Results.Ok(action);
});

app.Run();
