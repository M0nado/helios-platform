using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Helios.Connect.Api;
using Helios.Connect.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<OpenAiResponsesProvider>();
builder.Services.AddHttpClient<IAzureInventoryService, AzureInventoryService>();
builder.Services.AddSingleton<IEdgeAutomationPlanner, EdgeAutomationPlanner>();
var app = builder.Build();
var deliveries = new ExpiringDeliveryReplayCache(
    GetBoundedInt(builder.Configuration["HELIOS_WEBHOOK_REPLAY_CACHE_CAPACITY"], 4_096, 1, 100_000),
    TimeSpan.FromSeconds(GetBoundedInt(builder.Configuration["HELIOS_WEBHOOK_REPLAY_TTL_SECONDS"], 86_400, 1, 604_800)));
var localMcpEnabled = IsEnabled(builder.Configuration["HELIOS_LOCAL_RUNTIME_ALLOWED"]) &&
    !IsEnabled(builder.Configuration["HELIOS_CLOUD_RUNTIME_ONLY"]);

app.MapGet("/health", () => Results.Ok(new { service = "helios-connect", status = "healthy", mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run" }));

app.MapGet("/health/live", () => Results.Ok(new
{
    service = "helios-connect",
    status = "live"
}));

app.MapGet("/health/ready", (IConfiguration configuration) => BuildReadinessResult(configuration));

app.MapGet("/privacy", () => Results.Text(
    "Helios processes only administrator-approved integration metadata. The tenant administrator supplies the governing privacy notice.",
    "text/plain; charset=utf-8"));

app.MapGet("/terms", () => Results.Text(
    "Use is limited to authorized users and approved read-only or governed workflows under tenant policy.",
    "text/plain; charset=utf-8"));

app.MapGet("/openapi/v1.json", (HttpRequest request) =>
{
    var origin = $"{request.Scheme}://{request.Host}";
    var paths = new Dictionary<string, object>
    {
        ["/connector/context"] = new { get = new { operationId = "GetAzureContext", summary = "Read the configured Helios Azure context.", responses = new Dictionary<string, object> { ["200"] = new { description = "Azure context" }, ["401"] = new { description = "Entra authentication required" } } } },
        ["/connector/resources"] = new { get = new { operationId = "ListAzureResources", summary = "List read-only resource metadata.", parameters = new[] { new { name = "typePrefix", @in = "query", required = false, schema = new { type = "string" } } }, responses = new Dictionary<string, object> { ["200"] = new { description = "Azure resources" }, ["401"] = new { description = "Entra authentication required" } } } },
        ["/connector/foundry"] = new { get = new { operationId = "ListFoundryResources", summary = "List Foundry-related resources.", responses = new Dictionary<string, object> { ["200"] = new { description = "Foundry resources" }, ["401"] = new { description = "Entra authentication required" } } } }
    };
    return Results.Json(new { openapi = "3.0.1", info = new { title = "Helios Azure Connector", version = "0.3.0" }, servers = new[] { new { url = origin } }, paths });
});

// RFC 9728 discovery for OAuth-protected MCP clients. Both the root and the
// path-specific form are exposed because clients are required to try both.
app.MapGet("/.well-known/oauth-protected-resource", (HttpContext context) =>
    BuildProtectedResourceMetadata(context));
app.MapGet("/.well-known/oauth-protected-resource/mcp", (HttpContext context) =>
    BuildProtectedResourceMetadata(context));

app.MapGet("/connector/context", (HttpContext context) =>
{
    if (!IsConnectorAuthorized(context)) return Results.Unauthorized();
    var inventory = context.RequestServices.GetRequiredService<IAzureInventoryService>();
    return Results.Ok(inventory.GetContext());
});

app.MapGet("/connector/resources", async (HttpContext context, string? typePrefix, CancellationToken cancellationToken) =>
{
    if (!IsConnectorAuthorized(context)) return Results.Unauthorized();
    var inventory = context.RequestServices.GetRequiredService<IAzureInventoryService>();
    return await RunInventoryQuery(() => inventory.ListResourcesAsync(typePrefix, cancellationToken));
});

app.MapGet("/connector/foundry", async (HttpContext context, CancellationToken cancellationToken) =>
{
    if (!IsConnectorAuthorized(context)) return Results.Unauthorized();
    var inventory = context.RequestServices.GetRequiredService<IAzureInventoryService>();
    return await RunInventoryQuery(() => inventory.ListFoundryResourcesAsync(cancellationToken));
});

app.MapPost("/automation/plan", async (HttpContext context, IEdgeAutomationPlanner planner, CancellationToken cancellationToken) =>
{
    if (!IsConnectorAuthorized(context)) return Results.Unauthorized();
    const int maxPlanBytes = 16_384;
    if (!context.Request.HasJsonContentType()) return Results.BadRequest(new { error = "Content-Type must be application/json." });
    if (context.Request.ContentLength is > maxPlanBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

    try
    {
        var request = await JsonSerializer.DeserializeAsync<EdgeAutomationRequest>(
            context.Request.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);
        if (request is null) return Results.BadRequest(new { error = "Automation request is required." });
        return Results.Ok(planner.CreatePlan(request));
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { error = "Automation request is invalid JSON." });
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
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

app.MapPost("/mcp", async (HttpContext context, CancellationToken cancellationToken) =>
{
    if (!IsMcpOriginAllowed(context)) return McpError(null, -32000, "Invalid Origin header.", StatusCodes.Status403Forbidden);
    if (!IsConnectorAuthorized(context)) return McpUnauthorized(context);
    if (!context.Request.HasJsonContentType())
        return McpError(null, -32600, "Content-Type must be application/json.", StatusCodes.Status415UnsupportedMediaType);
    var maxMcpBytes = GetMaxMcpRequestBytes(context);
    if (!BoundedRequestBody.Prepare(context, maxMcpBytes))
        return McpError(null, -32600, "MCP request body is too large.", StatusCodes.Status413PayloadTooLarge);
    if (!AcceptsMcpPostResponse(context.Request))
        return McpError(null, -32600, "Accept must include application/json and text/event-stream.", StatusCodes.Status406NotAcceptable);

    var body = await BoundedRequestBody.ReadAsync(context.Request.Body, maxMcpBytes, cancellationToken);
    if (body.IsTooLarge)
        return McpError(null, -32600, "MCP request body is too large.", StatusCodes.Status413PayloadTooLarge);
    JsonDocument document;
    try { document = JsonDocument.Parse(body.Bytes); }
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
                instructions = "Read-only Azure and Foundry inventory plus deterministic HELIOS automation planning. No deployment, secret-write, merge, or role-assignment tools are exposed."
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
            var inventory = context.RequestServices.GetRequiredService<IAzureInventoryService>();
            var planner = context.RequestServices.GetRequiredService<IEdgeAutomationPlanner>();
            return McpResult(id, await BuildAzureToolResultAsync(root, inventory, planner, cancellationToken));
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

if (localMcpEnabled)
{
    app.MapPost("/runtime/webhooks/mcp", async (HttpContext context, CancellationToken cancellationToken) =>
    {
        var mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run";
        if (mode.Equals("live", StringComparison.OrdinalIgnoreCase)) return Results.StatusCode(StatusCodes.Status403Forbidden);
        var remote = context.Connection.RemoteIpAddress;
        if (remote is not null && !System.Net.IPAddress.IsLoopback(remote)) return Results.StatusCode(StatusCodes.Status403Forbidden);
        if (!IsMcpOriginAllowed(context)) return McpError(null, -32000, "Invalid Origin header.", StatusCodes.Status403Forbidden);
        if (!context.Request.HasJsonContentType()) return McpError(null, -32600, "Content-Type must be application/json.", StatusCodes.Status415UnsupportedMediaType);
        var maxMcpBytes = GetMaxMcpRequestBytes(context);
        if (!BoundedRequestBody.Prepare(context, maxMcpBytes)) return McpError(null, -32600, "MCP request body is too large.", StatusCodes.Status413PayloadTooLarge);
        if (!AcceptsMcpPostResponse(context.Request)) return McpError(null, -32600, "Accept must include application/json and text/event-stream.", StatusCodes.Status406NotAcceptable);

        var body = await BoundedRequestBody.ReadAsync(context.Request.Body, maxMcpBytes, cancellationToken);
        if (body.IsTooLarge) return McpError(null, -32600, "MCP request body is too large.", StatusCodes.Status413PayloadTooLarge);
        JsonDocument document;
        try { document = JsonDocument.Parse(body.Bytes); }
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
                "initialize" => McpResult(id, new { protocolVersion = "2025-03-26", capabilities = new { tools = new { listChanged = false } }, serverInfo = new { name = "helios-local", version = "0.3.0" } }),
                "tools/list" => McpResult(id, new { tools = new object[] {
                    new { name = "hermes_get_status", description = "Read Helios/Hermes local status.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false } },
                    new { name = "hermes_list_routes", description = "List configured integration route names without secrets.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false } }
                } }),
                "tools/call" => McpResult(id, BuildToolResult(root)),
                _ => McpError(id, -32601, $"Method '{method}' was not found.")
            };
        }
    });
}

app.MapPost("/webhooks/{provider}", async (string provider, HttpRequest request, CancellationToken cancellationToken) =>
{
    var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "github", "linear", "slack", "teams", "sharepoint", "foundry", "copilot" };
    if (!supported.Contains(provider)) return Results.NotFound();

    var maxBytes = GetBoundedInt(
        request.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["HELIOS_MAX_WEBHOOK_BYTES"],
        1_048_576,
        1_024,
        10_485_760);
    if (!BoundedRequestBody.Prepare(request.HttpContext, maxBytes)) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    var body = await BoundedRequestBody.ReadAsync(request.Body, maxBytes, cancellationToken);
    if (body.IsTooLarge) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    if (body.Bytes.Length == 0) return Results.BadRequest(new { error = "empty payload" });

    // Dry-run controls downstream side effects; it never weakens ingress
    // authentication. Unsupported providers remain fail-closed in the verifier.
    // Verify the provider's signature over the exact raw bytes before parsing
    // attacker-controlled JSON.
    if (!WebhookVerifier.Verify(provider, request.Headers, body.Bytes))
        return Results.Unauthorized();
    try { using var _ = JsonDocument.Parse(body.Bytes); }
    catch (JsonException) { return Results.BadRequest(new { error = "invalid JSON" }); }

    var deliveryId = request.Headers["X-GitHub-Delivery"].FirstOrDefault()
        ?? request.Headers["X-Linear-Delivery"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(deliveryId))
        deliveryId = Convert.ToHexString(SHA256.HashData(body.Bytes)).ToLowerInvariant();
    var replayKey = Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes($"{provider.ToLowerInvariant()}\n{deliveryId}"))).ToLowerInvariant();
    if (!deliveries.TryRegister(replayKey, DateTimeOffset.UtcNow)) return Results.Ok(new { duplicate = true, deliveryId });
    var evt = new HeliosEvent(deliveryId, $"{provider}.received", provider, provider,
        DateTimeOffset.UtcNow, Guid.NewGuid().ToString("n"), request.Headers["traceparent"].FirstOrDefault(),
        "internal", new Dictionary<string, object?> { ["rawSize"] = body.Bytes.Length });
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

    var requiresEntra = RequiresEntraAuthorization(configuration);
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

    var requiresEntra = RequiresEntraAuthorization(configuration);
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
    var values = acceptHeaders.SelectMany(value => value?.Split(',') ?? Array.Empty<string>())
        .Select(value => value.Split(';')[0].Trim())
        .ToArray();
    return values.Contains("application/json", StringComparer.OrdinalIgnoreCase) &&
        values.Contains("text/event-stream", StringComparer.OrdinalIgnoreCase);
}

static int GetMaxMcpRequestBytes(HttpContext context)
{
    var configured = context.RequestServices.GetRequiredService<IConfiguration>()["HELIOS_MAX_MCP_BYTES"];
    return int.TryParse(configured, out var parsed) && parsed is >= 1_024 and <= 10_485_760
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
    if (name is not ("azure_get_context" or "azure_list_resources" or "azure_list_foundry_resources" or "helios_plan_automation"))
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
        var valid = name switch
        {
            "azure_list_resources" => argument.Name == "typePrefix" && argument.Value.ValueKind == JsonValueKind.String,
            "helios_plan_automation" => argument.Name is "intent" or "environment" or "target" or "connector" && argument.Value.ValueKind == JsonValueKind.String,
            _ => false
        };
        if (!valid)
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
    var required = RequiresEntraAuthorization(context.RequestServices.GetRequiredService<IConfiguration>());
    return !required || !string.IsNullOrWhiteSpace(context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].FirstOrDefault());
}

static IResult BuildReadinessResult(IConfiguration configuration)
{
    var requiresAzureConfiguration = RequiresEntraAuthorization(configuration);
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

static bool RequiresEntraAuthorization(IConfiguration configuration) =>
    IsEnabled(configuration["HELIOS_REQUIRE_ENTRA_AUTH"]) ||
    IsEnabled(configuration["HELIOS_CLOUD_RUNTIME_ONLY"]);

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
    new { name = "azure_list_foundry_resources", title = "List Foundry resources", description = "Use this when you need Foundry-related Cognitive Services, Machine Learning, and AI Search resources from the authorized resource group.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false }, annotations = new { readOnlyHint = true, destructiveHint = false, idempotentHint = true, openWorldHint = true } },
    new { name = "helios_plan_automation", title = "Plan HELIOS automation", description = "Use this when you need a deterministic, non-executing plan for Azure provisioning, Key Vault rotation, issue repair, or cross-system release synchronization.", inputSchema = new { type = "object", required = new[] { "intent", "environment" }, properties = new { intent = new { type = "string", @enum = new[] { "provision-resources", "rotate-secret", "repair-issue", "sync-release" } }, environment = new { type = "string", @enum = new[] { "dev", "test", "preview", "prod" } }, target = new { type = "string", description = "Required for secret, issue, and release plans." }, connector = new { type = "string", @enum = new[] { "github", "linear", "slack", "sharepoint", "copilot", "codex", "all" } } }, additionalProperties = false }, annotations = new { readOnlyHint = true, destructiveHint = false, idempotentHint = true, openWorldHint = false } }
};

static async Task<object> BuildAzureToolResultAsync(JsonElement root, IAzureInventoryService inventory, IEdgeAutomationPlanner planner, CancellationToken cancellationToken)
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
            "helios_plan_automation" => planner.CreatePlan(ReadAutomationRequest(parameters)),
            _ => new { error = "unknown tool" }
        };
        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(payload) } }, isError = name is not ("azure_get_context" or "azure_list_resources" or "azure_list_foundry_resources" or "helios_plan_automation") };
    }
    catch (ArgumentException exception)
    {
        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(new { error = exception.Message }) } }, isError = true };
    }
    catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or Azure.Identity.AuthenticationFailedException)
    {
        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(new { error = "Managed identity authentication or Reader access did not succeed." }) } }, isError = true };
    }
}

static EdgeAutomationRequest ReadAutomationRequest(JsonElement parameters)
{
    if (!parameters.TryGetProperty("arguments", out var arguments) || arguments.ValueKind != JsonValueKind.Object)
        throw new ArgumentException("helios_plan_automation requires arguments.");

    string? Read(string name) => arguments.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
        ? value.GetString()
        : null;
    return new EdgeAutomationRequest(Read("intent") ?? string.Empty, Read("environment") ?? string.Empty, Read("target"), Read("connector"));
}

static string? ReadTypePrefix(JsonElement parameters)
{
    if (!parameters.TryGetProperty("arguments", out var arguments)) return null;
    return arguments.TryGetProperty("typePrefix", out var prefix) && prefix.ValueKind == JsonValueKind.String
        ? prefix.GetString()
        : null;
}

static bool IsEnabled(string? value) => bool.TryParse(value, out var enabled) && enabled;

static int GetBoundedInt(string? value, int defaultValue, int minimum, int maximum) =>
    int.TryParse(value, out var parsed) && parsed >= minimum && parsed <= maximum
        ? parsed
        : defaultValue;

internal static class McpProtocolDefaults
{
    internal const string CurrentVersion = "2025-11-25";
    internal static readonly string[] SupportedVersions = ["2025-03-26", "2025-06-18", CurrentVersion];
}

public partial class Program { }
