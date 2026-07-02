using HELIOS.AIHub.Abstractions;

namespace HELIOS.AIHub.Providers;

/// <summary>Local-only AI provider adapter for safe AIHub planning, drafting, and dry-run workflows.</summary>
public sealed class LocalAiProvider : IAiProvider
{
    private readonly LocalAiProviderOptions _options;

    /// <summary>Creates a local AI provider with the supplied options.</summary>
    public LocalAiProvider(LocalAiProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ProviderId))
        {
            throw new ArgumentException("Provider id is required.", nameof(options));
        }

        if (options.MaxPromptCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Max prompt characters must be positive.");
        }

        _options = options;
    }

    /// <inheritdoc />
    public string Id => _options.ProviderId;

    /// <inheritdoc />
    public string DisplayName => _options.DisplayName;

    /// <inheritdoc />
    public AiProviderKind Kind => AiProviderKind.LocalLlm;

    /// <inheritdoc />
    public IReadOnlySet<string> Capabilities => _options.Capabilities;

    /// <inheritdoc />
    public IReadOnlySet<AiDataClass> AllowedDataClasses => _options.AllowedDataClasses;

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hasLocalModel = !string.IsNullOrWhiteSpace(_options.ModelPath) && Directory.Exists(_options.ModelPath);
        var hasLocalEndpoint = _options.Endpoint is { IsLoopback: true };
        var isOfflinePlanner = _options.ModelPath is null && _options.Endpoint is null;

        return Task.FromResult(hasLocalModel || hasLocalEndpoint || isOfflinePlanner);
    }

    /// <inheritdoc />
    public Task<AiTaskResponse> ExecuteAsync(AiTaskRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.CancellationToken.ThrowIfCancellationRequested();

        if (!AllowedDataClasses.Contains(request.DataClass))
        {
            return Task.FromResult(CreateRejectedResponse(request, $"Data class '{request.DataClass}' is not allowed for local provider '{Id}'."));
        }

        if (request.RequestedMode is AiExecutionMode.Execute or AiExecutionMode.ElevatedExecute)
        {
            return Task.FromResult(CreateRejectedResponse(request, "Local AI provider only supports suggest, draft, and dry-run modes."));
        }

        if (request.Prompt.Length > _options.MaxPromptCharacters)
        {
            return Task.FromResult(CreateRejectedResponse(request, $"Prompt exceeds the local provider limit of {_options.MaxPromptCharacters} characters."));
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerKind"] = Kind.ToString(),
            ["executionMode"] = request.RequestedMode.ToString(),
            ["dataClass"] = request.DataClass.ToString(),
            ["localOnly"] = "true"
        };

        if (_options.ModelPath is not null)
        {
            metadata["modelPath"] = _options.ModelPath;
        }

        if (_options.Endpoint is not null)
        {
            metadata["endpoint"] = _options.Endpoint.ToString();
        }

        return Task.FromResult(new AiTaskResponse(
            request.TaskId,
            Id,
            Success: true,
            Summary: $"Prepared local AI {request.RequestedMode.ToString().ToLowerInvariant()} plan for '{request.Intent}'.",
            Content: BuildLocalPlan(request),
            Metadata: metadata));
    }

    private static AiTaskResponse CreateRejectedResponse(AiTaskRequest request, string reason)
    {
        return new AiTaskResponse(
            request.TaskId,
            ProviderId: string.Empty,
            Success: false,
            Summary: reason,
            Content: null,
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["rejected"] = "true"
            });
    }

    private static string BuildLocalPlan(AiTaskRequest request)
    {
        var contextSummary = request.Context.Count == 0
            ? "No external context supplied."
            : $"Context keys: {string.Join(", ", request.Context.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))}.";

        return string.Join(Environment.NewLine, new[]
        {
            $"Intent: {request.Intent}",
            $"Mode: {request.RequestedMode}",
            contextSummary,
            "Local AI guardrails: keep data on-device, prefer dry-run validation, and require an explicit trusted executor for side effects.",
            $"Prompt preview: {CreatePreview(request.Prompt)}"
        });
    }

    private static string CreatePreview(string prompt)
    {
        const int previewLength = 240;
        if (prompt.Length <= previewLength)
        {
            return prompt;
        }

        return string.Concat(prompt.AsSpan(0, previewLength), "...");
    }
}
