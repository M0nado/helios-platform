using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Helios.Connect.Api;
using Helios.Connect.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<OpenAiResponsesProvider>();
builder.Services.AddHttpClient<IAzureInventoryService, AzureInventoryService>();
var app = builder.Build();
var deliveries = new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.Ordinal);

app.MapGet("/health", () => Results.Ok(new { service = "helios-connect", status = "healthy", mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run" }));

app.MapGet("/health/live", () => Results.Ok(new
{
    service = "helios-connect",
    status = "live"
}));

app.MapGet("/health/ready", (IConfiguration configuration) => BuildReadinessResult(configuration));

app.MapGet("/openapi/v1.json", (HttpRequest request) =>
{
    var origin = $"{request.Scheme}://{request.Host}";
    var paths = new Dictionary<string, object>
    {
        ["/connector/context"] = new { get = new { operationId = "GetAzureContext", summary = "Read the configured Helios Azure context.", responses = new Dictionary<string, object> { ["200"] = new { description = "Azure context" }, ["401"] = new { description = "Entra authentication required" } } } },
        ["/connector/resources"] = new { get = new { operationId = "ListAzureResources", summary = "List read-only resource metadata.", parameters = new[] { new { name = "typePrefix", @in = "query", required = false, schema = new { type = "string" } } }, responses = new Dictionary<string, object> { ["200"] = new { description = "Azure resources" }, ["401"] = new { description = "Entra authentication required" } } } },
        ["/connector/foundry"] = new { get = new { operationId = "ListFoundryResources", summary = "List Foundry-related resources.", responses = new Dictionary<string, object> { ["200"] = new { description = "Foundry resources" }, ["401"] = new { description = "Entra authentication required" } } } }
    };
    return Results.Json(new { openapi = "3.0.1", info = new { title = "Helios Azure Connector", version = "0.2.0" }, servers = new[] { new { url = origin } }, paths });
});

// RFC 9728 discovery for OAuth-protected MCP clients. Both the root and the
// path-specific form are exposed because clients are required to try both.
app.MapGet("/.well-known/oauth-protected-resource", (HttpContext context) =>
    BuildProtectedResourceMetadata(context));
app.MapGet("/.well-known/oauth-protected-resource/mcp", (HttpContext context) =>
    BuildProtectedResourceMetadata(context));

app.MapGet("/connector/context", (HttpContext context, IAzureInventoryService inventory) =>
    IsConnectorAuthorized(context) ? Results.Ok(inventory.GetContext()) : Results.Unauthorized());

app.MapGet("/connector/resources", async (HttpContext context, IAzureInventoryService inventory, string? typePrefix, CancellationToken cancellationToken) =>
{
    if (!IsConnectorAuthorized(context)) return Results.Unauthorized();
    return await RunInventoryQuery(() => inventory.ListResourcesAsync(typePrefix, cancellationToken));
});

app.MapGet("/connector/foundry", async (HttpContext context, IAzureInventoryService inventory, CancellationToken cancellationToken) =>
{
    if (!IsConnectorAuthorized(context)) return Results.Unauthorized();
    return await RunInventoryQuery(() => inventory.ListFoundryResourcesAsync(cancellationToken));
});

app.MapGet("/mcp", (HttpContext context) =>
{
    if (!IsMcpOriginAllowed(context)) return McpError(null, -32000, "Invalid Origin header.", StatusCodes.Status403Forbidden);
    if (!IsConnectorAuthorized(context)) return McpUnauthorized(context);
    context.Response.Headers["Allow"] = "POST, GET, DELETE";
    return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
});

app.MapDelete("/mcp", (HttpContext context) =>
{
    if (!IsMcpOriginAllowed(context)) return McpError(null, -32000, "Invalid Origin header.", StatusCodes.Status403Forbidden);
    if (!IsConnectorAuthorized(context)) return McpUnauthorized(context);
    // Helios deliberately runs statelessly so requests can be served by any
    // Container Apps replica. It never issues Mcp-Session-Id, so there is no
    // server-side session to terminate.
    context.Response.Headers["Allow"] = "POST, GET";
    return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
});

app.MapPost("/mcp", async (HttpContext context, IAzureInventoryService inventory, CancellationToken cancellationToken) =>
{
    if (!IsMcpOriginAllowed(context)) return McpError(null, -32000, "Invalid Origin header.", StatusCodes.Status403Forbidden);
    if (!IsConnectorAuthorized(context)) return McpUnauthorized(context);
    if (!context.Request.HasJsonContentType())
        return McpError(null, -32600, "Content-Type must be application/json.", StatusCodes.Status415UnsupportedMediaType);
    if (context.Request.ContentLength is > 0 && context.Request.ContentLength > GetMaxMcpRequestBytes(context))
        return McpError(null, -32600, "MCP request body is too large.", StatusCodes.Status413PayloadTooLarge);
    var maxBodyFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
    if (maxBodyFeature is { IsReadOnly: false }) maxBodyFeature.MaxRequestBodySize = GetMaxMcpRequestBytes(context);
    if (!AcceptsMcpPostResponse(context.Request))
        return McpError(null, -32600, "Accept must include application/json and text/event-stream.", StatusCodes.Status406NotAcceptable);

    JsonDocument document;
    try { document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: cancellationToken); }
    catch (JsonException) { return McpError(null, -32700, "Parse error.", StatusCodes.Status400BadRequest); }

    using (document)
    {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return McpError(null, -32600, "JSON-RPC body must be one object.", StatusCodes.Status400BadRequest);
        if (!root.TryGetProperty("jsonrpc", out var jsonRpc) || jsonRpc.ValueKind != JsonValueKind.String || jsonRpc.GetString() != "2.0")
            return McpError(ReadJsonRpcId(root), -32600, "Invalid JSON-RPC version.", StatusCodes.Status400BadRequest);

        var hasId = root.TryGetProperty("id", out _);
        var id = ReadJsonRpcId(root);
        if (hasId && root.GetProperty("id").ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.Null))
            return McpError(null, -32600, "JSON-RPC id must be a string, number, or null.", StatusCodes.Status400BadRequest);
        if (!root.TryGetProperty("method", out var methodValue))
        {
            // A client-to-server JSON-RPC response has no method. This server
            // currently sends no requests, but accepting it follows the HTTP
            // transport's response/notification semantics.
            if (hasId && (root.TryGetProperty("result", out _) || root.TryGetProperty("error", out _)))
                return Results.StatusCode(StatusCodes.Status202Accepted);
            return McpError(id, -32600, "Invalid JSON-RPC request.", StatusCodes.Status400BadRequest);
        }
        if (methodValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(methodValue.GetString()))
            return hasId
                ? McpError(id, -32600, "JSON-RPC method must be a non-empty string.", StatusCodes.Status400BadRequest)
                : Results.StatusCode(StatusCodes.Status400BadRequest);

        var method = methodValue.GetString()!;
        var sessionId = context.Request.Headers["Mcp-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(sessionId))
            return McpError(id, -32001, "MCP session was not found because this server is stateless.", StatusCodes.Status404NotFound);

        if (method == "initialize")
        {
            if (!hasId)
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            if (!string.IsNullOrWhiteSpace(sessionId))
                return McpError(id, -32600, "Initialize must not include Mcp-Session-Id.", StatusCodes.Status400BadRequest);
            if (!TryReadInitializeVersion(root, out var requestedVersion))
                return McpError(id, -32602, "Initialize requires params.protocolVersion.");

            var negotiatedVersion = McpProtocolDefaults.SupportedVersions.Contains(requestedVersion, StringComparer.Ordinal)
                ? requestedVersion
                : McpProtocolDefaults.CurrentVersion;

            return McpResult(id, new
            {
                protocolVersion = negotiatedVersion,
                capabilities = new { tools = new { listChanged = false } },
                serverInfo = new { name = "helios-azure-connector", title = "Helios Azure Connector", version = "0.3.0" },
                instructions = "Read-only Azure and Foundry inventory. No deployment, mutation, or role-assignment tools are exposed."
            });
        }

        if (!TryValidateMcpProtocolHeader(context.Request, out var protocolError))
            return McpError(id, -32600, protocolError, StatusCodes.Status400BadRequest);

        // JSON-RPC notifications never receive a JSON-RPC response. Streamable
        // HTTP acknowledges an accepted notification with an empty HTTP 202.
        if (!hasId)
        {
            return Results.StatusCode(StatusCodes.Status202Accepted);
        }

        if (method == "tools/call")
        {
            if (!TryValidateAzureToolCall(root, out var validationError))
                return McpError(id, -32602, validationError);
            return McpResult(id, await BuildAzureToolResultAsync(root, inventory, cancellationToken));
        }

        return method switch
        {
            "ping" => McpResult(id, new { }),
            "tools/list" when HasObjectParamsOrNoParams(root) => McpResult(id, new { tools = BuildAzureToolList() }),
            "tools/list" => McpError(id, -32602, "tools/list params must be an object when provided."),
            _ => McpError(id, -32601, $"Method '{method}' was not found.")
        };
    }
});

app.MapPost("/runtime/webhooks/mcp", async (HttpContext context) =>
{
    var mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run";
    if (mode.Equals("live", StringComparison.OrdinalIgnoreCase)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var remote = context.Connection.RemoteIpAddress;
    if (remote is not null && !System.Net.IPAddress.IsLoopback(remote)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!IsMcpOriginAllowed(context)) return McpError(null, -32000, "Invalid Origin header.", StatusCodes.Status403Forbidden);
    if (!context.Request.HasJsonContentType()) return McpError(null, -32600, "Content-Type must be application/json.", StatusCodes.Status415UnsupportedMediaType);
    if (context.Request.ContentLength is > 0 && context.Request.ContentLength > GetMaxMcpRequestBytes(context)) return McpError(null, -32600, "MCP request body is too large.", StatusCodes.Status413PayloadTooLarge);
    if (!AcceptsMcpPostResponse(context.Request)) return McpError(null, -32600, "Accept must include application/json and text/event-stream.", StatusCodes.Status406NotAcceptable);

    JsonDocument document;
    try { document = await JsonDocument.ParseAsync(context.Request.Body); }
    catch (JsonException) { return McpError(null, -32700, "Parse error.", StatusCodes.Status400BadRequest); }
    using (document)
    {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("jsonrpc", out var jsonRpc) || jsonRpc.ValueKind != JsonValueKind.String || jsonRpc.GetString() != "2.0" ||
            !root.TryGetProperty("method", out var methodValue) || methodValue.ValueKind != JsonValueKind.String)
            return McpError(ReadJsonRpcId(root), -32600, "Invalid JSON-RPC request.", StatusCodes.Status400BadRequest);

        var hasId = root.TryGetProperty("id", out _);
        if (!hasId) return Results.StatusCode(StatusCodes.Status202Accepted);
        var id = ReadJsonRpcId(root);
        var method = methodValue.GetString();
        return method switch
        {
            "initialize" => McpResult(id, new { protocolVersion = "2025-03-26", capabilities = new { tools = new { listChanged = false } }, serverInfo = new { name = "helios-local", version = "0.2.0" } }),
            "tools/list" => McpResult(id, new { tools = new object[] {
                new { name = "hermes_get_status", description = "Read Helios/Hermes local status.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false } },
                new { name = "hermes_list_routes", description = "List configured integration route names without secrets.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false } }
            } }),
            "tools/call" => McpResult(id, BuildToolResult(root)),
            _ => McpError(id, -32601, $"Method '{method}' was not found.")
        };
    }
});

app.MapPost("/webhooks/{provider}", async (string provider, HttpRequest request) =>
{
    var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "github", "linear", "slack", "teams", "sharepoint", "foundry", "copilot" };
    if (!supported.Contains(provider)) return Results.NotFound();

    var maxBytes = int.TryParse(Environment.GetEnvironmentVariable("HELIOS_MAX_WEBHOOK_BYTES"), out var configuredMax) ? configuredMax : 1_048_576;
    if (request.ContentLength is > 0 && request.ContentLength > maxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body)) return Results.BadRequest(new { error = "empty payload" });
    if (Encoding.UTF8.GetByteCount(body) > maxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    try { using var _ = JsonDocument.Parse(body); }
    catch (JsonException) { return Results.BadRequest(new { error = "invalid JSON" }); }

    var mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run";
    if (mode.Equals("live", StringComparison.OrdinalIgnoreCase) && !WebhookVerifier.Verify(provider, request.Headers, body))
        return Results.Unauthorized();

    var deliveryId = request.Headers["X-GitHub-Delivery"].FirstOrDefault()
        ?? request.Headers["X-Linear-Delivery"].FirstOrDefault()
        ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    if (!deliveries.TryAdd($"{provider}:{deliveryId}", DateTimeOffset.UtcNow)) return Results.Ok(new { duplicate = true, deliveryId });
    var evt = new HeliosEvent(deliveryId, $"{provider}.received", provider, provider,
        DateTimeOffset.UtcNow, Guid.NewGuid().ToString("n"), request.Headers["traceparent"].FirstOrDefault(),
        "internal", new Dictionary<string, object?> { ["rawSize"] = body.Length });
    return Results.Accepted(value: evt);
});

app.Run();

static IResult BuildProtectedResourceMetadata(HttpContext context)
{
    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
    var publicOrigin = GetMcpPublicOrigin(context);
    var resource = $"{publicOrigin}/mcp";
    var tenantId = configuration["AZURE_TENANT_ID"];
    var clientId = configuration["HELIOS_ENTRA_CLIENT_ID"];
    var authorizationTenant = string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId;
    var scopes = string.IsNullOrWhiteSpace(clientId)
        ? Array.Empty<string>()
        : new[] { $"api://{clientId}/user_impersonation" };

    context.Response.Headers["Cache-Control"] = "public, max-age=300";
    return Results.Json(new Dictionary<string, object?>
    {
        ["resource"] = resource,
        ["resource_name"] = "Helios Azure Connector MCP",
        ["authorization_servers"] = new[] { $"https://login.microsoftonline.com/{authorizationTenant}/v2.0" },
        ["scopes_supported"] = scopes,
        ["bearer_methods_supported"] = new[] { "header" },
        ["resource_documentation"] = $"{publicOrigin}/openapi/v1.json"
    });
}

static IResult McpUnauthorized(HttpContext context)
{
    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
    var clientId = configuration["HELIOS_ENTRA_CLIENT_ID"];
    var scope = string.IsNullOrWhiteSpace(clientId) ? null : $"api://{clientId}/user_impersonation";
    var metadataUrl = $"{GetMcpPublicOrigin(context)}/.well-known/oauth-protected-resource/mcp";
    var challenge = $"Bearer resource_metadata=\"{metadataUrl}\"";
    if (!string.IsNullOrWhiteSpace(scope)) challenge += $", scope=\"{scope}\"";
    context.Response.Headers["WWW-Authenticate"] = challenge;
    return McpError(null, -32000, "Authentication required.", StatusCodes.Status401Unauthorized);
}

static string GetMcpPublicOrigin(HttpContext context)
{
    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
    var configured = configuration["HELIOS_PUBLIC_BASE_URL"];
    if (!string.IsNullOrWhiteSpace(configured) &&
        Uri.TryCreate(configured, UriKind.Absolute, out var configuredUri) &&
        configuredUri.Scheme == Uri.UriSchemeHttps &&
        configuredUri.AbsolutePath.Trim('/') == string.Empty &&
        string.IsNullOrEmpty(configuredUri.Query) &&
        string.IsNullOrEmpty(configuredUri.Fragment))
    {
        return configuredUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    var requiresEntra = bool.TryParse(configuration["HELIOS_REQUIRE_ENTRA_AUTH"], out var parsed) && parsed;
    var scheme = requiresEntra ? Uri.UriSchemeHttps : context.Request.Scheme;
    return $"{scheme}://{context.Request.Host}".TrimEnd('/');
}

static bool IsMcpOriginAllowed(HttpContext context)
{
    var originHeaders = context.Request.Headers["Origin"];
    if (originHeaders.Count == 0) return true; // Non-browser MCP clients normally omit Origin.
    if (originHeaders.Count != 1 || !TryNormalizeOrigin(originHeaders[0], out var origin)) return false;

    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
    var configuredOrigins = (configuration["HELIOS_MCP_ALLOWED_ORIGINS"] ?? string.Empty)
        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var configuredOrigin in configuredOrigins)
    {
        if (configuredOrigin == "*") continue; // Wildcards defeat DNS-rebinding protection.
        if (TryNormalizeOrigin(configuredOrigin, out var allowed) &&
            string.Equals(origin, allowed, StringComparison.OrdinalIgnoreCase))
            return true;
    }

    var requiresEntra = bool.TryParse(configuration["HELIOS_REQUIRE_ENTRA_AUTH"], out var parsed) && parsed;
    if (requiresEntra) return false; // Cloud/browser origins must be explicitly allowlisted.
    return TryNormalizeOrigin(GetMcpPublicOrigin(context), out var sameOrigin) &&
        string.Equals(origin, sameOrigin, StringComparison.OrdinalIgnoreCase);
}

static bool TryNormalizeOrigin(string? candidate, out string origin)
{
    origin = string.Empty;
    if (string.IsNullOrWhiteSpace(candidate) ||
        !Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
        !string.IsNullOrEmpty(uri.UserInfo) ||
        uri.AbsolutePath.Trim('/') != string.Empty ||
        !string.IsNullOrEmpty(uri.Query) ||
        !string.IsNullOrEmpty(uri.Fragment))
        return false;

    origin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    return true;
}

static bool AcceptsMcpPostResponse(HttpRequest request)
{
    // Missing Accept is tolerated only for compatibility with older MCP clients;
    // when present, the Streamable HTTP contract is enforced exactly.
    var acceptHeaders = request.Headers["Accept"];
    if (acceptHeaders.Count == 0) return true;
    var values = acceptHeaders.SelectMany(value => value.Split(','))
        .Select(value => value.Split(';')[0].Trim())
        .ToArray();
    return values.Contains("application/json", StringComparer.OrdinalIgnoreCase) &&
        values.Contains("text/event-stream", StringComparer.OrdinalIgnoreCase);
}

static long GetMaxMcpRequestBytes(HttpContext context)
{
    var configured = context.RequestServices.GetRequiredService<IConfiguration>()["HELIOS_MAX_MCP_BYTES"];
    return long.TryParse(configured, out var parsed) && parsed is >= 1_024 and <= 10_485_760
        ? parsed
        : 1_048_576;
}

static object? ReadJsonRpcId(JsonElement root)
{
    if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("id", out var id)) return null;
    return id.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.Null
        ? id.Clone()
        : null;
}

static IResult McpResult(object? id, object result) => Results.Json(new Dictionary<string, object?>
{
    ["jsonrpc"] = "2.0",
    ["id"] = id,
    ["result"] = result
});

static IResult McpError(object? id, int code, string message, int statusCode = StatusCodes.Status200OK, object? data = null)
{
    var error = new Dictionary<string, object?>
    {
        ["code"] = code,
        ["message"] = message
    };
    if (data is not null) error["data"] = data;
    return Results.Json(new Dictionary<string, object?>
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["error"] = error
    }, statusCode: statusCode);
}

static bool TryReadInitializeVersion(JsonElement root, out string requestedVersion)
{
    requestedVersion = string.Empty;
    if (!root.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object ||
        !parameters.TryGetProperty("protocolVersion", out var protocolVersion) || protocolVersion.ValueKind != JsonValueKind.String ||
        string.IsNullOrWhiteSpace(protocolVersion.GetString()) ||
        !parameters.TryGetProperty("capabilities", out var capabilities) || capabilities.ValueKind != JsonValueKind.Object ||
        !parameters.TryGetProperty("clientInfo", out var clientInfo) || clientInfo.ValueKind != JsonValueKind.Object ||
        !clientInfo.TryGetProperty("name", out var clientName) || clientName.ValueKind != JsonValueKind.String ||
        !clientInfo.TryGetProperty("version", out var clientVersion) || clientVersion.ValueKind != JsonValueKind.String)
        return false;

    requestedVersion = protocolVersion.GetString()!;
    return true;
}

static bool TryValidateMcpProtocolHeader(HttpRequest request, out string error)
{
    error = string.Empty;
    var headerValues = request.Headers["MCP-Protocol-Version"];
    if (headerValues.Count == 0) return true; // Required backwards-compatible assumption: 2025-03-26.
    if (headerValues.Count != 1 || string.IsNullOrWhiteSpace(headerValues[0]))
    {
        error = "MCP-Protocol-Version must contain exactly one value.";
        return false;
    }
    if (!McpProtocolDefaults.SupportedVersions.Contains(headerValues[0]!, StringComparer.Ordinal))
    {
        error = $"Unsupported MCP-Protocol-Version '{headerValues[0]}'.";
        return false;
    }
    return true;
}

static bool HasObjectParamsOrNoParams(JsonElement root) =>
    !root.TryGetProperty("params", out var parameters) || parameters.ValueKind == JsonValueKind.Object;

static bool TryValidateAzureToolCall(JsonElement root, out string error)
{
    error = string.Empty;
    if (!root.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object ||
        !parameters.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
    {
        error = "tools/call requires params.name and optional object params.arguments.";
        return false;
    }

    var name = nameElement.GetString();
    if (name is not ("azure_get_context" or "azure_list_resources" or "azure_list_foundry_resources"))
    {
        error = $"Unknown tool '{name}'.";
        return false;
    }

    if (!parameters.TryGetProperty("arguments", out var arguments)) return true;
    if (arguments.ValueKind != JsonValueKind.Object)
    {
        error = "params.arguments must be an object.";
        return false;
    }

    foreach (var argument in arguments.EnumerateObject())
    {
        if (name != "azure_list_resources" || argument.Name != "typePrefix" || argument.Value.ValueKind != JsonValueKind.String)
        {
            error = $"Argument '{argument.Name}' is not valid for tool '{name}'.";
            return false;
        }
    }
    return true;
}

static object BuildToolResult(JsonElement root)
{
    var name = root.TryGetProperty("params", out var parameters) && parameters.TryGetProperty("name", out var nameValue)
        ? nameValue.GetString()
        : null;
    var payload = name switch
    {
        "hermes_get_status" => JsonSerializer.Serialize(new { status = "dry-run", learning = "candidate-only", writes = "pull-request-only" }),
        "hermes_list_routes" => JsonSerializer.Serialize(new[] { "github.workflow.failed", "linear.issue.updated", "hermes.training.candidate", "hermes.training.approved", "sharepoint.document.updated" }),
        _ => JsonSerializer.Serialize(new { error = "unknown tool" })
    };
    return new { content = new[] { new { type = "text", text = payload } }, isError = name is not ("hermes_get_status" or "hermes_list_routes") };
}

static bool IsConnectorAuthorized(HttpContext context)
{
    var required = bool.TryParse(context.RequestServices.GetRequiredService<IConfiguration>()["HELIOS_REQUIRE_ENTRA_AUTH"], out var parsed) && parsed;
    return !required || !string.IsNullOrWhiteSpace(context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].FirstOrDefault());
}

static IResult BuildReadinessResult(IConfiguration configuration)
{
    var requiresAzureConfiguration = bool.TryParse(configuration["HELIOS_REQUIRE_ENTRA_AUTH"], out var requiresAuth) && requiresAuth;
    if (!requiresAzureConfiguration)
    {
        return Results.Ok(new { service = "helios-connect", status = "ready", runtime = "development" });
    }

    var requiredSettings = new[]
    {
        "AZURE_TENANT_ID",
        "AZURE_SUBSCRIPTION_ID",
        "AZURE_RESOURCE_GROUP",
        "AZURE_CLIENT_ID",
        "HELIOS_ENTRA_CLIENT_ID",
        "HELIOS_PUBLIC_BASE_URL"
    };
    var missingSettings = requiredSettings
        .Where(name => string.IsNullOrWhiteSpace(configuration[name]))
        .ToArray();

    return missingSettings.Length == 0
        ? Results.Ok(new { service = "helios-connect", status = "ready", runtime = "azure" })
        : Results.Json(
            new { service = "helios-connect", status = "not-ready", missingConfiguration = missingSettings },
            statusCode: StatusCodes.Status503ServiceUnavailable);
}

static async Task<IResult> RunInventoryQuery(Func<Task<IReadOnlyList<AzureInventoryResource>>> query)
{
    try { return Results.Ok(await query()); }
    catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or HttpRequestException or Azure.Identity.AuthenticationFailedException)
    {
        return Results.Problem(title: "Azure inventory query failed", detail: "Managed identity authentication or Reader access did not succeed.", statusCode: StatusCodes.Status502BadGateway);
    }
}

static object[] BuildAzureToolList() => new object[]
{
    new { name = "azure_get_context", title = "Get Azure context", description = "Use this when you need the configured Helios Azure tenant, subscription, resource group, and access mode.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false }, annotations = new { readOnlyHint = true, destructiveHint = false, idempotentHint = true, openWorldHint = false } },
    new { name = "azure_list_resources", title = "List Azure resources", description = "Use this when you need non-secret Azure resource metadata from the authorized resource group.", inputSchema = new { type = "object", properties = new { typePrefix = new { type = "string", description = "Optional Azure resource type prefix." } }, additionalProperties = false }, annotations = new { readOnlyHint = true, destructiveHint = false, idempotentHint = true, openWorldHint = true } },
    new { name = "azure_list_foundry_resources", title = "List Foundry resources", description = "Use this when you need Foundry-related Cognitive Services, Machine Learning, and AI Search resources from the authorized resource group.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false }, annotations = new { readOnlyHint = true, destructiveHint = false, idempotentHint = true, openWorldHint = true } }
};

static async Task<object> BuildAzureToolResultAsync(JsonElement root, IAzureInventoryService inventory, CancellationToken cancellationToken)
{
    var name = root.TryGetProperty("params", out var parameters) && parameters.TryGetProperty("name", out var nameValue)
        ? nameValue.GetString()
        : null;
    try
    {
        object payload = name switch
        {
            "azure_get_context" => inventory.GetContext(),
            "azure_list_resources" => await inventory.ListResourcesAsync(ReadTypePrefix(parameters), cancellationToken),
            "azure_list_foundry_resources" => await inventory.ListFoundryResourcesAsync(cancellationToken),
            _ => new { error = "unknown tool" }
        };
        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(payload) } }, isError = name is not ("azure_get_context" or "azure_list_resources" or "azure_list_foundry_resources") };
    }
    catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or HttpRequestException or Azure.Identity.AuthenticationFailedException)
    {
        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(new { error = "Managed identity authentication or Reader access did not succeed." }) } }, isError = true };
    }
}

static string? ReadTypePrefix(JsonElement parameters)
{
    if (!parameters.TryGetProperty("arguments", out var arguments)) return null;
    return arguments.TryGetProperty("typePrefix", out var prefix) && prefix.ValueKind == JsonValueKind.String
        ? prefix.GetString()
        : null;
}

internal static class McpProtocolDefaults
{
    internal const string CurrentVersion = "2025-11-25";
    internal static readonly string[] SupportedVersions = ["2025-03-26", "2025-06-18", CurrentVersion];
}

public partial class Program { }
