using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HELIOS.AIHub.Engines;
using HELIOS.AIHub.Memory;
using HELIOS.AIHub.Training;

namespace HELIOS.AIHub.Control;

/// <summary>
/// Local HTTP control server that preserves the X-Tier AIHub prototype endpoints without cloud credentials.
/// </summary>
public sealed class ControlServer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly HttpListener _listener = new();
    private readonly SelfTeachingStore _memoryStore;
    private readonly DeepEngineCatalog _engineCatalog;
    private readonly EngineRecommendationService _recommendationService;
    private readonly HermesTrainingLoop _trainingLoop;
    private CancellationTokenSource? _stop;
    private Task? _listenTask;

    public ControlServer(
        Uri baseAddress,
        SelfTeachingStore? memoryStore = null,
        DeepEngineCatalog? engineCatalog = null,
        EngineRecommendationService? recommendationService = null,
        HermesTrainingLoop? trainingLoop = null)
    {
        BaseAddress = baseAddress;
        _memoryStore = memoryStore ?? new SelfTeachingStore();
        _engineCatalog = engineCatalog ?? new DeepEngineCatalog();
        _recommendationService = recommendationService ?? new EngineRecommendationService(_engineCatalog);
        _trainingLoop = trainingLoop ?? new HermesTrainingLoop(store: _memoryStore);
        _listener.Prefixes.Add(NormalizePrefix(baseAddress));
    }

    public Uri BaseAddress { get; }

    public static ControlServer CreateEphemeral(string host = "127.0.0.1")
    {
        using var socket = new TcpListener(IPAddress.Parse(host), 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return new ControlServer(new Uri($"http://{host}:{port}/"));
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listenTask is not null)
        {
            return Task.CompletedTask;
        }

        _stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();
        _listenTask = Task.Run(() => ListenAsync(_stop.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_listenTask is null)
        {
            return;
        }

        _stop?.Cancel();
        _listener.Stop();

        try
        {
            await _listenTask.ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _listenTask = null;
            _stop?.Dispose();
            _stop = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _listener.Close();
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested || !_listener.IsListening)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleAsync(context, cancellationToken), CancellationToken.None);
        }
    }

    private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var request = context.Request;
            var path = request.Url?.AbsolutePath.TrimEnd('/') is { Length: > 0 } value ? value : "/";

            if (request.HttpMethod == "GET")
            {
                await HandleGetAsync(context, path, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (request.HttpMethod == "POST")
            {
                await HandlePostAsync(context, path, cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteJsonAsync(context.Response, 405, new { error = "method_not_allowed" }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await WriteJsonAsync(context.Response, 500, new { error = "server_error", message = exception.Message }, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private Task HandleGetAsync(HttpListenerContext context, string path, CancellationToken cancellationToken) => path switch
    {
        "/health" => WriteJsonAsync(context.Response, 200, new { status = "ok", service = "HELIOS.AIHub.ControlServer", version = "local-xtier", utc = DateTimeOffset.UtcNow }, cancellationToken),
        "/meta" => WriteJsonAsync(context.Response, 200, BuildMeta(), cancellationToken),
        "/memory/search" => WriteJsonAsync(context.Response, 200, SearchMemory(context.Request), cancellationToken),
        "/agents/list" => WriteJsonAsync(context.Response, 200, BuildAgents(), cancellationToken),
        "/teams" => WriteJsonAsync(context.Response, 200, BuildTeams(), cancellationToken),
        "/api/fleet/live" => WriteJsonAsync(context.Response, 200, BuildFleetLive(), cancellationToken),
        "/api/security/live" => WriteJsonAsync(context.Response, 200, BuildSecurityLive(), cancellationToken),
        "/api/engines/catalog" => WriteJsonAsync(context.Response, 200, new { engines = _engineCatalog.GetEngines(), count = _engineCatalog.GetEngines().Count }, cancellationToken),
        "/api/engines/recommend" => WriteJsonAsync(context.Response, 200, RecommendEngine(context.Request), cancellationToken),
        "/api/setup/tracker" => WriteJsonAsync(context.Response, 200, BuildSetupTracker(), cancellationToken),
        "/api/setup/autoboot-plan" => WriteJsonAsync(context.Response, 200, BuildAutobootPlan(), cancellationToken),
        _ => WriteJsonAsync(context.Response, 404, new { error = "not_found", path }, cancellationToken)
    };

    private async Task HandlePostAsync(HttpListenerContext context, string path, CancellationToken cancellationToken)
    {
        switch (path)
        {
            case "/tasks/create":
                var taskRequest = await ReadJsonAsync<CreateTaskRequest>(context.Request, cancellationToken).ConfigureAwait(false) ?? new CreateTaskRequest(null, null, new Dictionary<string, string>());
                var recommendation = _recommendationService.Recommend(new EngineRecommendationRequest(taskRequest.Intent ?? taskRequest.Prompt ?? "local task", null, taskRequest.Intent));
                await WriteJsonAsync(context.Response, 202, new
                {
                    taskId = $"task-{Guid.NewGuid():N}",
                    status = "accepted",
                    intent = taskRequest.Intent ?? "general",
                    prompt = taskRequest.Prompt ?? string.Empty,
                    route = recommendation.PrimaryEngineId,
                    metadata = taskRequest.Metadata
                }, cancellationToken).ConfigureAwait(false);
                break;
            case "/api/train/trigger":
                var trainRequest = await ReadJsonAsync<TrainingTriggerRequest>(context.Request, cancellationToken).ConfigureAwait(false) ?? TrainingTriggerRequest.Default;
                var run = await _trainingLoop.TriggerAsync(trainRequest, cancellationToken).ConfigureAwait(false);
                await WriteJsonAsync(context.Response, 202, new { status = "completed", run }, cancellationToken).ConfigureAwait(false);
                break;
            default:
                await WriteJsonAsync(context.Response, 404, new { error = "not_found", path }, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private object BuildMeta() => new
    {
        artifact = "LOCAL_XTIER_ARTIFACTS_INTEGRATION",
        productionSpine = "C#",
        cloudCredentialsRequired = false,
        implementedEndpoints = new[]
        {
            "GET /health", "GET /meta", "GET /memory/search", "GET /agents/list", "GET /teams",
            "GET /api/fleet/live", "GET /api/security/live", "GET /api/engines/catalog", "GET /api/engines/recommend",
            "GET /api/setup/tracker", "GET /api/setup/autoboot-plan", "POST /tasks/create", "POST /api/train/trigger"
        },
        deferredEndpoints = new[]
        {
            new { endpoint = "GET /api/hyperv/phase1", reason = "Requires HELIOS.Security.HyperVIsolation module wiring." },
            new { endpoint = "GET /api/knowledge/summary", reason = "Requires HELIOS.Requirements transcript integrator indexing." }
        }
    };

    private object SearchMemory(HttpListenerRequest request)
    {
        var query = request.QueryString["q"] ?? request.QueryString["query"] ?? string.Empty;
        var limit = int.TryParse(request.QueryString["limit"], out var parsed) ? parsed : 10;
        var results = _memoryStore.Search(query, limit);
        return new { query, count = results.Count, results };
    }

    private static object BuildAgents() => new
    {
        agents = new[]
        {
            new { id = "xcore-inference", name = "XCore Inference Node", capabilities = new[] { "inference", "routing", "latency" }, localOnly = true },
            new { id = "hermes-feature-engineering", name = "Hermes Feature Engineering Node", capabilities = new[] { "memory", "retrieval", "features" }, localOnly = true },
            new { id = "hermes-trainer", name = "Hermes Local Trainer", capabilities = new[] { "training", "security", "drift" }, localOnly = true }
        }
    };

    private static object BuildTeams() => new
    {
        teams = new[]
        {
            new { id = "control", name = "Gateway + API Control", agents = new[] { "xcore-inference" } },
            new { id = "training", name = "WSL2 Trainer", agents = new[] { "hermes-trainer", "hermes-feature-engineering" } },
            new { id = "security", name = "Security Isolation", agents = new[] { "hermes-trainer" } }
        }
    };

    private static object BuildFleetLive() => new
    {
        status = "local",
        topology = new[]
        {
            new { runtime = "Docker", role = "gateway+api", gpuEnabled = true, memoryGb = 8 },
            new { runtime = "Docker", role = "gui+control", gpuEnabled = false, memoryGb = 4 },
            new { runtime = "WSL2", role = "trainer", gpuEnabled = true, memoryGb = 16 },
            new { runtime = "Hyper-V", role = "security-isolation", gpuEnabled = false, memoryGb = 4 }
        }
    };

    private static object BuildSecurityLive() => new
    {
        status = "guarded",
        profile = "balanced",
        egressMode = "allowlist",
        trainingPolicy = new[] { "signed-artifacts-only", "checkpoint", "drift-guard" },
        cloudCredentialsRequired = false
    };

    private object RecommendEngine(HttpListenerRequest request)
    {
        var task = request.QueryString["task"] ?? request.QueryString["q"] ?? "local AIHub task";
        var dataKind = request.QueryString["data"];
        var intent = request.QueryString["intent"];
        return _recommendationService.Recommend(new EngineRecommendationRequest(task, dataKind, intent));
    }

    private static object BuildSetupTracker() => new
    {
        status = "ready-local",
        steps = new[]
        {
            new { id = "catalog", name = "Seed deep engine catalog", complete = true, deferred = false, reason = string.Empty },
            new { id = "memory", name = "Initialize self-teaching memory", complete = true, deferred = false, reason = string.Empty },
            new { id = "control-server", name = "Start local control server", complete = true, deferred = false, reason = string.Empty },
            new { id = "azure-cli", name = "Azure CLI cloud bootstrap", complete = false, deferred = true, reason = "Cloud credentials intentionally not required for local integration tests." }
        }
    };

    private static object BuildAutobootPlan() => new
    {
        mode = "local-first",
        services = new[]
        {
            new { name = "gateway-api", runtime = "Docker", autoStart = true },
            new { name = "gui-control", runtime = "Docker", autoStart = true },
            new { name = "trainer", runtime = "WSL2", autoStart = true },
            new { name = "security-isolation", runtime = "Hyper-V", autoStart = false }
        }
    };

    private static async Task<T?> ReadJsonAsync<T>(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasEntityBody)
        {
            return default;
        }

        return await JsonSerializer.DeserializeAsync<T>(request.InputStream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, int statusCode, object value, CancellationToken cancellationToken)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions));
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        response.Close();
    }

    private static string NormalizePrefix(Uri baseAddress)
    {
        var value = baseAddress.ToString();
        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }

    private sealed record CreateTaskRequest(string? Prompt, string? Intent, IReadOnlyDictionary<string, string> Metadata);
}
