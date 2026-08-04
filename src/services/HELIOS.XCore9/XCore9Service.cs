using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using HELIOS.Platform.Contracts.XCore9;

namespace HELIOS.XCore9;

public sealed class XCore9Service : IXCore9Service
{
    private static readonly FrozenSet<string> AllowedFeatures = new[] { "success_rate", "duration_ratio", "cost_ratio", "tool_reliability", "route_accuracy", "retry_rate", "dependency_health", "holdout_score" }.ToFrozenSet(StringComparer.Ordinal);
    private readonly IXCoreAnalytics _analytics;
    private readonly IXCoreAuthorization _authorization;
    private readonly IXCoreAuditSink _audit;
    private readonly XCore9Options _options;
    private readonly IReadOnlyDictionary<string, WorkerTemplate> _templates;
    private readonly IReadOnlyDictionary<string, ToolchainDefinition> _toolchains;
    private readonly IReadOnlyDictionary<string, ToolDefinition> _tools;
    private readonly ConcurrentDictionary<Guid, WorkerLease> _leases = new();
    private readonly List<RunHistoryEntry> _history = [];
    private readonly List<NegotiationRecord> _negotiations = [];
    private readonly object _gate = new();
    private readonly SemaphoreSlim _promotionGate = new(1, 1);
    private RoutingPolicy _activePolicy;
    private RoutingPolicy? _rollbackPolicy;

    public XCore9Service(IXCoreAnalytics analytics, IXCoreAuthorization authorization, IXCoreAuditSink audit, IEnumerable<WorkerTemplate> templates, IEnumerable<ToolchainDefinition> toolchains, IEnumerable<ToolDefinition> tools, RoutingPolicy initialPolicy, XCore9Options? options = null)
    {
        _analytics = analytics;
        _authorization = authorization;
        _audit = audit;
        _options = options ?? new();
        _templates = templates.Select(x => x with { AllowedToolchainIds = x.AllowedToolchainIds.ToFrozenSet(StringComparer.Ordinal) }).ToFrozenDictionary(x => x.TemplateId, StringComparer.Ordinal);
        _toolchains = toolchains.Select(x => x with { ToolIds = x.ToolIds.ToFrozenSet(StringComparer.Ordinal) }).ToFrozenDictionary(x => x.ToolchainId, StringComparer.Ordinal);
        _tools = tools.Select(x => x with { Dependencies = x.Dependencies.ToFrozenSet(StringComparer.Ordinal) }).ToFrozenDictionary(x => x.ToolId, StringComparer.Ordinal);
        _activePolicy = SnapshotPolicy(initialPolicy);
        if (_templates.Values.Any(x => x.MaxInstances < 1 || x.CpuUnits < 1 || x.MemoryMiB < 1 || string.IsNullOrWhiteSpace(x.PromptDigest))) throw new ArgumentException("Every template must declare bounded resources and an immutable prompt digest.");
    }

    public async ValueTask IngestRunHistoryAsync(RunHistoryEntry entry, string actor, CancellationToken token) { token.ThrowIfCancellationRequested(); await Demand(actor, "run-history.ingest", token); ValidateEnvelope(entry.CorrelationId, entry.EvidenceLinks); var sanitized = entry with { Features = ExtractFeatures(entry) }; lock (_gate) _history.Add(sanitized); await _audit.WriteAsync(entry.CorrelationId, "xcore9.run.ingested", actor, entry.EvidenceLinks, token); }
    public IReadOnlyList<SanitizedRunFeature> ExtractFeatures(RunHistoryEntry entry) => entry.Features.Where(x => AllowedFeatures.Contains(x.Name) && double.IsFinite(x.Value)).Take(_options.MaxFeaturesPerRun).Select(x => x with { Value = Math.Clamp(x.Value, 0, 1) }).ToArray();

    public IReadOnlyList<RouteScore> ScoreRoutes(IReadOnlyList<CandidateRoute> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!_templates.TryGetValue(candidate.WorkerTemplateId, out var template) || !template.AllowedToolchainIds.Contains(candidate.ToolchainId)) throw new UnauthorizedAccessException("Candidate template and toolchain pairing is not pre-approved.");
            ConstructToolchain(candidate.ToolchainId);
            if (candidate.Features.Count > _options.MaxFeaturesPerRun || candidate.Features.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != candidate.Features.Count) throw new InvalidOperationException("Candidate features must be unique and within the configured bound.");
            if (candidate.Features.Any(x => !AllowedFeatures.Contains(x.Name) || !double.IsFinite(x.Value))) throw new InvalidOperationException("Candidate contains an unapproved or non-finite feature.");
        }
        return _analytics.Rank(candidates);
    }

    public async ValueTask<WorkerLease> SelectWorkerAsync(string templateId, string toolchainId, string correlationId, string actor, CancellationToken token) { token.ThrowIfCancellationRequested(); await Demand(actor, "worker.select", token); ValidateEnvelope(correlationId, []); if (!_templates.TryGetValue(templateId, out var template) || !template.AllowedToolchainIds.Contains(toolchainId)) throw new UnauthorizedAccessException("Template or toolchain is not pre-approved."); ConstructToolchain(toolchainId); lock (_gate) { PruneLeases(); var active = _leases.Values.ToArray(); if (active.Length >= _options.MaxTotalInstances || active.Count(x => x.TemplateId == templateId) >= template.MaxInstances || active.Sum(x => _templates[x.TemplateId].CpuUnits) + template.CpuUnits > _options.MaxCpuUnits || active.Sum(x => _templates[x.TemplateId].MemoryMiB) + template.MemoryMiB > _options.MaxMemoryMiB) throw new InvalidOperationException("Worker instance or resource limit reached."); var lease = new WorkerLease(Guid.NewGuid(), templateId, correlationId, DateTimeOffset.UtcNow + _options.EffectiveLeaseDuration); _leases[lease.LeaseId] = lease; return lease; } }
    public ConstructedToolchain ConstructToolchain(string id) { if (!_toolchains.TryGetValue(id, out var chain)) throw new UnauthorizedAccessException("Toolchain is not approved."); var ordered = new List<string>(); var visiting = new HashSet<string>(); var visited = new HashSet<string>(); void Visit(string toolId) { if (!chain.ToolIds.Contains(toolId) || !_tools.TryGetValue(toolId, out var tool)) throw new InvalidOperationException("Toolchain dependency is not approved."); if (!visiting.Add(toolId)) throw new InvalidOperationException("Cyclic tool dependency."); if (visited.Add(toolId)) { foreach (var dependency in tool.Dependencies.Order()) Visit(dependency); ordered.Add(toolId); } visiting.Remove(toolId); } foreach (var tool in chain.ToolIds.Order()) Visit(tool); return new(id, ordered); }
    public RetryClassification ClassifyRetry(string code, int attempt) => (code, attempt) switch { (_, _) when attempt >= 3 => new(RetryDisposition.RequiresReview, null, "attempt-limit"), ("timeout" or "rate_limited", _) => new(RetryDisposition.RetryWithBackoff, TimeSpan.FromSeconds(Math.Pow(2, attempt)), code), ("dependency_unavailable", _) => new(RetryDisposition.RetryAfterDependencyRecovery, null, code), ("authorization" or "policy_denied", _) => new(RetryDisposition.DoNotRetry, null, code), _ => new(RetryDisposition.RequiresReview, null, "unclassified") };
    public async ValueTask RecordNegotiationAsync(NegotiationRecord record, string actor, CancellationToken token) { token.ThrowIfCancellationRequested(); await Demand(actor, "negotiation.record", token); ValidateEnvelope(record.CorrelationId, []); if (string.IsNullOrWhiteSpace(record.ProposalDigest)) throw new ArgumentException("Only a sanitized proposal digest may be recorded."); lock (_gate) _negotiations.Add(record); await _audit.WriteAsync(record.CorrelationId, "xcore9.negotiation.recorded", actor, [], token); }

    public async ValueTask<PolicyDecision> EvaluatePromotionAsync(PromotionRequest request, string actor, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Demand(actor, "policy.promote", token);
        await _promotionGate.WaitAsync(token);
        try
        {
            var active = _activePolicy;
            var rollback = _rollbackPolicy;
            if (actor.StartsWith("xcore", StringComparison.OrdinalIgnoreCase) || request.RequestedBy.StartsWith("xcore", StringComparison.OrdinalIgnoreCase)) return new(false, "self-promotion-prohibited", active, rollback);
            var holdout = request.Holdout;
            if (!double.IsFinite(holdout.BaselineLoss) || !double.IsFinite(holdout.CandidateLoss) || !double.IsFinite(holdout.Confidence) || holdout.SampleCount < _options.MinimumHoldoutSamples || !holdout.Passed || holdout.BaselineLoss - holdout.CandidateLoss < _options.MinimumImprovement) return new(false, "holdout-requirement-not-met", active, rollback);
            if (request.EvidenceLinks.Count == 0) return new(false, "promotion-evidence-required", active, rollback);
            ValidateEnvelope("policy-" + request.Candidate.PolicyId, request.EvidenceLinks);

            var candidate = SnapshotPolicy(request.Candidate with { PreviousPolicyId = active.PolicyId });
            // Persist approval evidence before changing in-memory routing state. The event deliberately
            // describes approval rather than activation so a process stop between these statements
            // cannot leave an audit record that falsely claims the policy became active.
            await _audit.WriteAsync("policy-" + candidate.PolicyId, "xcore9.policy.promotion-approved", actor, request.EvidenceLinks, token);
            _rollbackPolicy = active;
            _activePolicy = candidate;
            return new(true, "approved-by-external-authority", candidate, active);
        }
        finally
        {
            _promotionGate.Release();
        }
    }

    public void ReleaseWorker(WorkerLease lease) => _leases.TryRemove(lease.LeaseId, out _);
    private async ValueTask Demand(string actor, string capability, CancellationToken token) { if (string.IsNullOrWhiteSpace(actor) || !await _authorization.AuthorizeAsync(actor, capability, token)) throw new UnauthorizedAccessException($"Actor is not authorized for {capability}."); }
    private static RoutingPolicy SnapshotPolicy(RoutingPolicy policy) => policy with { Rules = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(policy.Rules, StringComparer.Ordinal)) };
    private static void ValidateEnvelope(string correlationId, IReadOnlyList<Uri> evidence) { if (string.IsNullOrWhiteSpace(correlationId)) throw new ArgumentException("Correlation ID is required."); if (evidence.Any(x => x.Scheme is not ("https" or "urn"))) throw new ArgumentException("Evidence links must use https or urn."); }
    private void PruneLeases() { foreach (var lease in _leases.Values.Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow)) _leases.TryRemove(lease.LeaseId, out _); }
}
