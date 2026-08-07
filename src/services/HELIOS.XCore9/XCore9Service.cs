using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using HELIOS.Platform.Contracts.XCore9;

namespace HELIOS.XCore9;

public sealed class XCore9Service : IXCore9Service
{
    private const string EventSchemaVersion = "1.0";
    private const string EventSource = "xcore";
    private const string EventDataClassification = "internal";
    private const string EventRepository = "M0nado/helios-platform";
    private static readonly HashSet<string> AllowedAuditEnvironments = new(StringComparer.Ordinal)
    {
        "local",
        "development",
        "test",
        "staging",
        "production"
    };

    private static readonly StringComparer IdentifierComparer = StringComparer.Ordinal;
    private static readonly HashSet<string> AllowedFeatures = new(IdentifierComparer)
    {
        "success_rate",
        "duration_ratio",
        "cost_ratio",
        "tool_reliability",
        "route_accuracy",
        "retry_rate",
        "dependency_health",
        "holdout_score"
    };
    private static readonly Regex DigestPattern = new("^sha256:[0-9a-f]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IXCoreAnalytics _analytics;
    private readonly IXCoreAuthorization _authorization;
    private readonly IXCoreAuditSink _audit;
    private readonly XCore9Options _options;
    private readonly string _auditEnvironment;
    private readonly IReadOnlyDictionary<string, WorkerTemplate> _templates;
    private readonly IReadOnlyDictionary<string, ToolchainDefinition> _toolchains;
    private readonly IReadOnlyDictionary<string, ToolDefinition> _tools;
    private readonly ConcurrentDictionary<Guid, WorkerLease> _leases = new();
    private readonly List<RunHistoryEntry> _history = [];
    private readonly List<NegotiationRecord> _negotiations = [];
    private readonly object _gate = new();
    private readonly SemaphoreSlim _historyGate = new(1, 1);
    private readonly SemaphoreSlim _negotiationGate = new(1, 1);
    private readonly SemaphoreSlim _promotionGate = new(1, 1);
    private readonly SemaphoreSlim _leaseGate = new(1, 1);

    private RoutingPolicy _activePolicy;
    private RoutingPolicy? _rollbackPolicy;

    public XCore9Service(
        IXCoreAnalytics analytics,
        IXCoreAuthorization authorization,
        IXCoreAuditSink audit,
        IEnumerable<WorkerTemplate> templates,
        IEnumerable<ToolchainDefinition> toolchains,
        IEnumerable<ToolDefinition> tools,
        RoutingPolicy initialPolicy,
        XCore9Options? options = null)
    {
        _analytics = analytics;
        _authorization = authorization;
        _audit = audit;
        _options = options ?? new XCore9Options();

        ValidateOptions(_options);
        _auditEnvironment = NormalizeAuditEnvironment(_options.AuditEnvironment);

        _templates = templates.ToDictionary(
            template => template.TemplateId,
            template => template with
            {
                AllowedToolchainIds = template.AllowedToolchainIds.ToHashSet(IdentifierComparer)
            },
            IdentifierComparer);

        _toolchains = toolchains.ToDictionary(
            toolchain => toolchain.ToolchainId,
            toolchain => toolchain with
            {
                ToolIds = toolchain.ToolIds.ToHashSet(IdentifierComparer)
            },
            IdentifierComparer);

        _tools = tools.ToDictionary(
            tool => tool.ToolId,
            tool => tool with
            {
                Dependencies = tool.Dependencies.ToHashSet(IdentifierComparer)
            },
            IdentifierComparer);

        ValidateCatalogSnapshots();
        _activePolicy = SnapshotPolicy(initialPolicy);
    }

    public async ValueTask IngestRunHistoryAsync(RunHistoryEntry entry, string actor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await DemandAsync(actor, "run-history.ingest", cancellationToken);
        var evidenceLinks = entry.EvidenceLinks.ToArray();
        ValidateEnvelope(entry.CorrelationId, evidenceLinks);

        if (!_templates.ContainsKey(entry.TemplateId))
        {
            throw new InvalidOperationException("Run history references a non-approved template.");
        }

        var sanitized = entry with
        {
            Features = ExtractFeatures(entry),
            EvidenceLinks = evidenceLinks
        };

        await _historyGate.WaitAsync(cancellationToken);
        try
        {
            RunHistoryEntry? evicted = null;
            var skipAuditForDuplicate = false;
            var insertedIndex = -1;
            lock (_gate)
            {
                var existing = _history.FirstOrDefault(historyEntry => historyEntry.RunId == sanitized.RunId);
                if (existing is not null)
                {
                    if (!RunHistoryEquivalent(existing, sanitized))
                    {
                        throw new InvalidOperationException("Run history with the same run ID contains conflicting data.");
                    }

                    skipAuditForDuplicate = true;
                }
                else
                {
                    if (_history.Count >= _options.MaxRunHistoryEntries)
                    {
                        evicted = _history[0];
                        _history.RemoveAt(0);
                    }

                    _history.Add(sanitized);
                    insertedIndex = _history.Count - 1;
                }
            }

            if (skipAuditForDuplicate)
            {
                return;
            }

            try
            {
                await WriteAuditAsync(
                    eventType: "xcore9.run.ingested",
                    actor: actor,
                    correlationId: entry.CorrelationId,
                    evidenceLinks: sanitized.EvidenceLinks,
                    payload: new Dictionary<string, object?>(IdentifierComparer)
                    {
                        ["runId"] = entry.RunId.ToString("N"),
                        ["templateId"] = entry.TemplateId,
                        ["succeeded"] = entry.Succeeded,
                        ["featureCount"] = sanitized.Features.Count
                    },
                    cancellationToken);
            }
            catch
            {
                lock (_gate)
                {
                    if (_history.Count > insertedIndex)
                    {
                        _history.RemoveAt(insertedIndex);
                    }

                    if (evicted is not null)
                    {
                        _history.Insert(0, evicted);
                    }
                }

                throw;
            }
        }
        finally
        {
            _historyGate.Release();
        }
    }

    public IReadOnlyList<SanitizedRunFeature> ExtractFeatures(RunHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Features.Count > _options.MaxFeaturesPerRun)
        {
            throw new InvalidOperationException("Run history feature list exceeds the configured bound.");
        }

        var seen = new HashSet<string>(IdentifierComparer);
        return entry.Features
            .Where(feature => AllowedFeatures.Contains(feature.Name) &&
                              double.IsFinite(feature.Value) &&
                              seen.Add(feature.Name))
            .Take(_options.MaxFeaturesPerRun)
            .Select(feature => feature with { Value = Math.Clamp(feature.Value, 0d, 1d) })
            .ToArray();
    }

    private static bool RunHistoryEquivalent(RunHistoryEntry existing, RunHistoryEntry candidate)
    {
        if (!string.Equals(existing.CorrelationId, candidate.CorrelationId, StringComparison.Ordinal) ||
            !string.Equals(existing.TemplateId, candidate.TemplateId, StringComparison.Ordinal) ||
            existing.Succeeded != candidate.Succeeded ||
            existing.Duration != candidate.Duration ||
            existing.Cost != candidate.Cost ||
            existing.Features.Count != candidate.Features.Count ||
            existing.EvidenceLinks.Count != candidate.EvidenceLinks.Count)
        {
            return false;
        }

        for (var index = 0; index < existing.Features.Count; index++)
        {
            var left = existing.Features[index];
            var right = candidate.Features[index];
            if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal) || left.Value != right.Value)
            {
                return false;
            }
        }

        for (var index = 0; index < existing.EvidenceLinks.Count; index++)
        {
            if (Uri.Compare(existing.EvidenceLinks[index], candidate.EvidenceLinks[index], UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.Ordinal) != 0)
            {
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<RouteScore> ScoreRoutes(IReadOnlyList<CandidateRoute> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count > _options.MaxRoutesPerScoringRequest)
        {
            throw new InvalidOperationException("Candidate set exceeds the configured scoring request bound.");
        }

        if (candidates.Any(candidate => string.IsNullOrWhiteSpace(candidate.RouteId)))
        {
            throw new InvalidOperationException("Every candidate route must declare a non-empty route ID.");
        }

        if (candidates.Select(candidate => candidate.RouteId).Distinct(IdentifierComparer).Count() != candidates.Count)
        {
            throw new InvalidOperationException("Candidate set contains duplicate route IDs.");
        }

        foreach (var candidate in candidates)
        {
            ValidateCandidate(candidate);
        }

        return _analytics.Rank(candidates);
    }

    public async ValueTask<WorkerLease> SelectWorkerAsync(
        string templateId,
        string toolchainId,
        string correlationId,
        string actor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await DemandAsync(actor, "worker.select", cancellationToken);
        ValidateEnvelope(correlationId, Array.Empty<Uri>());

        if (!_templates.TryGetValue(templateId, out var template) ||
            !template.AllowedToolchainIds.Contains(toolchainId))
        {
            throw new UnauthorizedAccessException("Template or toolchain is not pre-approved.");
        }

        _ = ConstructToolchain(toolchainId);

        await _leaseGate.WaitAsync(cancellationToken);
        try
        {
            var expiredLeases = Array.Empty<WorkerLease>();
            lock (_gate)
            {
                expiredLeases = PruneLeases().ToArray();
            }

            if (expiredLeases.Length > 0)
            {
                var auditedExpirationCount = 0;
                try
                {
                    foreach (var expiredLease in expiredLeases)
                    {
                        await WriteAuditAsync(
                            eventType: "xcore9.worker.expired",
                            actor: actor,
                            correlationId: expiredLease.CorrelationId,
                            evidenceLinks: Array.Empty<Uri>(),
                            payload: new Dictionary<string, object?>(IdentifierComparer)
                            {
                                ["leaseId"] = expiredLease.LeaseId.ToString("N"),
                                ["templateId"] = expiredLease.TemplateId,
                                ["expiresAt"] = expiredLease.ExpiresAt
                            },
                            cancellationToken);
                        auditedExpirationCount++;
                    }
                }
                catch
                {
                    lock (_gate)
                    {
                        for (var index = auditedExpirationCount; index < expiredLeases.Length; index++)
                        {
                            var expiredLease = expiredLeases[index];
                            _leases[expiredLease.LeaseId] = expiredLease;
                        }
                    }

                    throw;
                }
            }

            WorkerLease lease;
            lock (_gate)
            {
                var activeLeases = _leases.Values.ToArray();
                var cpuInUse = activeLeases.Sum(lease => (long)_templates[lease.TemplateId].CpuUnits);
                var memoryInUse = activeLeases.Sum(lease => (long)_templates[lease.TemplateId].MemoryMiB);
                if (activeLeases.Length >= _options.MaxTotalInstances ||
                    activeLeases.Count(lease => lease.TemplateId == templateId) >= template.MaxInstances ||
                    cpuInUse + template.CpuUnits > _options.MaxCpuUnits ||
                    memoryInUse + template.MemoryMiB > _options.MaxMemoryMiB)
                {
                    throw new InvalidOperationException("Worker instance or resource limit reached.");
                }

                var now = DateTimeOffset.UtcNow;
                if (_options.EffectiveLeaseDuration > DateTimeOffset.MaxValue - now)
                {
                    throw new InvalidOperationException("Lease duration exceeds representable expiration range.");
                }

                lease = new WorkerLease(
                    LeaseId: Guid.NewGuid(),
                    TemplateId: templateId,
                    CorrelationId: correlationId,
                    ExpiresAt: now + _options.EffectiveLeaseDuration);

                _leases[lease.LeaseId] = lease;
            }

            try
            {
                await WriteAuditAsync(
                    eventType: "xcore9.worker.selected",
                    actor: actor,
                    correlationId: correlationId,
                    evidenceLinks: Array.Empty<Uri>(),
                    payload: new Dictionary<string, object?>(IdentifierComparer)
                    {
                        ["leaseId"] = lease.LeaseId.ToString("N"),
                        ["templateId"] = lease.TemplateId,
                        ["toolchainId"] = toolchainId,
                        ["expiresAt"] = lease.ExpiresAt
                    },
                    cancellationToken);
            }
            catch
            {
                lock (_gate)
                {
                    _leases.TryRemove(lease.LeaseId, out _);
                }

                throw;
            }

            return lease;
        }
        finally
        {
            _leaseGate.Release();
        }
    }

    public ConstructedToolchain ConstructToolchain(string toolchainId)
    {
        if (!_toolchains.TryGetValue(toolchainId, out var toolchain))
        {
            throw new UnauthorizedAccessException("Toolchain is not approved.");
        }

        var ordered = new List<string>();
        var visiting = new HashSet<string>(IdentifierComparer);
        var visited = new HashSet<string>(IdentifierComparer);

        void Visit(string toolId)
        {
            if (!toolchain.ToolIds.Contains(toolId) || !_tools.TryGetValue(toolId, out var tool))
            {
                throw new InvalidOperationException("Toolchain dependency is not approved.");
            }

            if (!visiting.Add(toolId))
            {
                throw new InvalidOperationException("Cyclic tool dependency.");
            }

            if (visited.Add(toolId))
            {
                foreach (var dependency in tool.Dependencies.OrderBy(static dependency => dependency, StringComparer.Ordinal))
                {
                    Visit(dependency);
                }

                ordered.Add(toolId);
            }

            visiting.Remove(toolId);
        }

        foreach (var toolId in toolchain.ToolIds.OrderBy(static toolId => toolId, StringComparer.Ordinal))
        {
            Visit(toolId);
        }

        return new ConstructedToolchain(toolchainId, ordered);
    }

    public RetryClassification ClassifyRetry(string failureCode, int attempt)
    {
        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Retry attempt must be non-negative.");
        }

        return (failureCode, attempt) switch
        {
            (_, _) when attempt >= 3 => new RetryClassification(RetryDisposition.RequiresReview, null, "attempt-limit"),
            ("timeout" or "rate_limited", _) => new RetryClassification(RetryDisposition.RetryWithBackoff, TimeSpan.FromSeconds(Math.Pow(2, attempt)), failureCode),
            ("dependency_unavailable", _) => new RetryClassification(RetryDisposition.RetryAfterDependencyRecovery, null, failureCode),
            ("authorization" or "policy_denied", _) => new RetryClassification(RetryDisposition.DoNotRetry, null, failureCode),
            _ => new RetryClassification(RetryDisposition.RequiresReview, null, "unclassified")
        };
    }

    public async ValueTask RecordNegotiationAsync(NegotiationRecord record, string actor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await DemandAsync(actor, "negotiation.record", cancellationToken);
        ValidateEnvelope(record.CorrelationId, Array.Empty<Uri>());

        if (string.IsNullOrWhiteSpace(record.ProposalDigest) || !DigestPattern.IsMatch(record.ProposalDigest))
        {
            throw new ArgumentException("Proposal digest must be canonical sha256:<64 lowercase hex characters>.", nameof(record));
        }

        await _negotiationGate.WaitAsync(cancellationToken);
        try
        {
            NegotiationRecord? evicted = null;
            int insertedIndex;
            lock (_gate)
            {
                if (_negotiations.Count >= _options.MaxNegotiationEntries)
                {
                    evicted = _negotiations[0];
                    _negotiations.RemoveAt(0);
                }

                _negotiations.Add(record);
                insertedIndex = _negotiations.Count - 1;
            }

            try
            {
                await WriteAuditAsync(
                    eventType: "xcore9.negotiation.recorded",
                    actor: actor,
                    correlationId: record.CorrelationId,
                    evidenceLinks: Array.Empty<Uri>(),
                    payload: new Dictionary<string, object?>(IdentifierComparer)
                    {
                        ["negotiationId"] = record.NegotiationId.ToString("N"),
                        ["proposer"] = record.Proposer,
                        ["counterparty"] = record.Counterparty,
                        ["outcome"] = record.Outcome
                    },
                    cancellationToken);
            }
            catch
            {
                lock (_gate)
                {
                    if (_negotiations.Count > insertedIndex)
                    {
                        _negotiations.RemoveAt(insertedIndex);
                    }

                    if (evicted is not null)
                    {
                        _negotiations.Insert(0, evicted);
                    }
                }

                throw;
            }
        }
        finally
        {
            _negotiationGate.Release();
        }
    }

    public async ValueTask<PolicyDecision> EvaluatePromotionAsync(PromotionRequest request, string actor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await DemandAsync(actor, "policy.promote", cancellationToken);
        var evidenceLinks = request.EvidenceLinks.ToArray();
        ValidateEnvelope(request.CorrelationId, evidenceLinks);

        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            return CurrentDecision(false, "requested-by-required");
        }

        if (evidenceLinks.Length == 0)
        {
            return CurrentDecision(false, "promotion-evidence-required");
        }

        if (!await _authorization.AuthorizeAsync(actor, "policy.promote.external-authority", cancellationToken))
        {
            return CurrentDecision(false, "self-promotion-prohibited");
        }

        if (!HoldoutIsValid(request.Holdout))
        {
            return CurrentDecision(false, "holdout-values-not-finite");
        }

        if (request.Holdout.SampleCount < _options.MinimumHoldoutSamples ||
            !request.Holdout.Passed ||
            request.Holdout.BaselineLoss - request.Holdout.CandidateLoss < _options.MinimumImprovement)
        {
            return CurrentDecision(false, "holdout-requirement-not-met");
        }

        await _promotionGate.WaitAsync(cancellationToken);
        try
        {
            RoutingPolicy activeSnapshot;
            RoutingPolicy? rollbackSnapshot;
            lock (_gate)
            {
                activeSnapshot = _activePolicy;
                rollbackSnapshot = _rollbackPolicy;
            }

            if (string.Equals(activeSnapshot.PolicyId, request.Candidate.PolicyId, StringComparison.Ordinal) &&
                request.Candidate.Version < activeSnapshot.Version)
            {
                return new PolicyDecision(false, "candidate-version-regression", activeSnapshot, rollbackSnapshot);
            }

            if (string.Equals(activeSnapshot.PolicyId, request.Candidate.PolicyId, StringComparison.Ordinal) &&
                activeSnapshot.Version == request.Candidate.Version)
            {
                if (!PoliciesMatch(activeSnapshot, request.Candidate))
                {
                    return new PolicyDecision(false, "candidate-policy-identity-collision", activeSnapshot, rollbackSnapshot);
                }

                return new PolicyDecision(true, "already-active", activeSnapshot, rollbackSnapshot);
            }

            var promotedPolicy = SnapshotPolicy(request.Candidate, activeSnapshot.PolicyId);
            await WriteAuditAsync(
                eventType: "xcore9.policy.promoted",
                actor: actor,
                correlationId: request.CorrelationId,
                evidenceLinks: evidenceLinks,
                payload: new Dictionary<string, object?>(IdentifierComparer)
                {
                    ["candidatePolicyId"] = promotedPolicy.PolicyId,
                    ["candidateVersion"] = promotedPolicy.Version,
                    ["requestedBy"] = request.RequestedBy,
                    ["baselineLoss"] = request.Holdout.BaselineLoss,
                    ["candidateLoss"] = request.Holdout.CandidateLoss,
                    ["minimumImprovement"] = _options.MinimumImprovement
                },
                cancellationToken);

            lock (_gate)
            {
                _rollbackPolicy = activeSnapshot;
                _activePolicy = promotedPolicy;
                return new PolicyDecision(true, "approved-by-external-authority", _activePolicy, _rollbackPolicy);
            }
        }
        finally
        {
            _promotionGate.Release();
        }
    }

    public async ValueTask ReleaseWorkerAsync(WorkerLease lease, string correlationId, string actor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(lease);
        await DemandAsync(actor, "worker.release", cancellationToken);
        ValidateEnvelope(correlationId, Array.Empty<Uri>());

        await _leaseGate.WaitAsync(cancellationToken);
        try
        {
            WorkerLease? activeLease = null;
            lock (_gate)
            {
                if (!_leases.TryGetValue(lease.LeaseId, out var storedLease) || storedLease is null)
                {
                    throw new InvalidOperationException("Worker lease is not active.");
                }

                activeLease = storedLease;

                if (!string.Equals(activeLease.CorrelationId, correlationId, StringComparison.Ordinal))
                {
                    throw new UnauthorizedAccessException("Lease correlation does not match release request.");
                }

                if (!_leases.TryRemove(lease.LeaseId, out _))
                {
                    throw new InvalidOperationException("Worker lease release failed.");
                }
            }

            try
            {
                await WriteAuditAsync(
                    eventType: "xcore9.worker.released",
                    actor: actor,
                    correlationId: correlationId,
                    evidenceLinks: Array.Empty<Uri>(),
                    payload: new Dictionary<string, object?>(IdentifierComparer)
                    {
                        ["leaseId"] = lease.LeaseId.ToString("N"),
                        ["templateId"] = activeLease!.TemplateId
                    },
                    cancellationToken);
            }
            catch
            {
                lock (_gate)
                {
                    _leases[activeLease!.LeaseId] = activeLease;
                }

                throw;
            }
        }
        finally
        {
            _leaseGate.Release();
        }
    }

    private static void ValidateOptions(XCore9Options options)
    {
        if (options.MaxTotalInstances < 1 ||
            options.MaxCpuUnits < 1 ||
            options.MaxMemoryMiB < 1 ||
            options.MaxFeaturesPerRun < 1 ||
            options.MaxRoutesPerScoringRequest < 1 ||
            options.MaxRunHistoryEntries < 1 ||
            options.MaxNegotiationEntries < 1 ||
            options.MaxEvidenceLinks < 1 ||
            options.MaxEvidenceLinkLength < 1 ||
            options.MinimumHoldoutSamples < 1)
        {
            throw new ArgumentException("All XCore-9 bounded limits must be positive.");
        }

        if (!double.IsFinite(options.MinimumImprovement) || options.MinimumImprovement < 0d)
        {
            throw new ArgumentException("Minimum improvement must be finite and non-negative.");
        }

        if (options.LeaseDuration is { } leaseDuration && leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Lease duration must be positive when configured.");
        }

        if (options.LeaseDuration is { } boundedLeaseDuration &&
            boundedLeaseDuration > DateTimeOffset.MaxValue - DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Lease duration must fit within representable DateTimeOffset expirations.");
        }

        if (string.IsNullOrWhiteSpace(options.AuditEnvironment))
        {
            throw new ArgumentException("Audit environment is required.");
        }
    }

    private void ValidateCatalogSnapshots()
    {
        foreach (var template in _templates.Values)
        {
            if (template.MaxInstances < 1 ||
                template.CpuUnits < 1 ||
                template.MemoryMiB < 1 ||
                string.IsNullOrWhiteSpace(template.PromptDigest))
            {
                throw new ArgumentException("Every template must declare bounded resources and an immutable prompt digest.");
            }

            foreach (var allowedToolchainId in template.AllowedToolchainIds.Where(allowedToolchainId => !_toolchains.ContainsKey(allowedToolchainId)))
            {
                throw new ArgumentException("Template references a toolchain outside the approved catalog.");
            }
        }

        foreach (var toolchainId in _toolchains.Keys)
        {
            _ = ConstructToolchain(toolchainId);
        }
    }

    private static RoutingPolicy SnapshotPolicy(RoutingPolicy policy, string? previousPolicyId = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(policy.PolicyId))
        {
            throw new ArgumentException("Policy ID is required.", nameof(policy));
        }

        if (policy.Version < 1)
        {
            throw new ArgumentException("Policy version must be at least 1.", nameof(policy));
        }

        if (policy.Rules is null)
        {
            throw new ArgumentException("Policy rules are required.", nameof(policy));
        }

        var rules = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(policy.Rules, IdentifierComparer));

        return new RoutingPolicy(
            PolicyId: policy.PolicyId,
            Version: policy.Version,
            Rules: rules,
            PreviousPolicyId: previousPolicyId ?? policy.PreviousPolicyId);
    }

    private static bool PoliciesMatch(RoutingPolicy activePolicy, RoutingPolicy candidatePolicy)
    {
        if (!string.Equals(activePolicy.PolicyId, candidatePolicy.PolicyId, StringComparison.Ordinal) ||
            activePolicy.Version != candidatePolicy.Version)
        {
            return false;
        }

        if (candidatePolicy.Rules is null || activePolicy.Rules.Count != candidatePolicy.Rules.Count)
        {
            return false;
        }

        return activePolicy.Rules.All(
            activeRule => candidatePolicy.Rules.TryGetValue(activeRule.Key, out var value) &&
                          string.Equals(value, activeRule.Value, StringComparison.Ordinal));
    }

    private PolicyDecision CurrentDecision(bool approved, string reasonCode)
    {
        lock (_gate)
        {
            return new PolicyDecision(approved, reasonCode, _activePolicy, _rollbackPolicy);
        }
    }

    private static bool HoldoutIsValid(HoldoutEvaluation holdout) =>
        double.IsFinite(holdout.BaselineLoss) &&
        double.IsFinite(holdout.CandidateLoss) &&
        double.IsFinite(holdout.Confidence) &&
        holdout.Confidence is >= 0d and <= 1d;

    private void ValidateCandidate(CandidateRoute candidate)
    {
        if (!_templates.TryGetValue(candidate.WorkerTemplateId, out var template) ||
            !_toolchains.ContainsKey(candidate.ToolchainId))
        {
            throw new InvalidOperationException("Candidate references a non-approved template or toolchain.");
        }

        if (!template.AllowedToolchainIds.Contains(candidate.ToolchainId))
        {
            throw new InvalidOperationException("Candidate references a toolchain that is not approved for the template.");
        }

        _ = ConstructToolchain(candidate.ToolchainId);

        if (candidate.Features.Count > _options.MaxFeaturesPerRun)
        {
            throw new InvalidOperationException("Candidate exceeds the configured feature bound.");
        }

        if (candidate.Features.Any(feature => !AllowedFeatures.Contains(feature.Name) || !double.IsFinite(feature.Value)))
        {
            throw new InvalidOperationException("Candidate contains an unapproved or non-finite feature.");
        }

        if (candidate.Features.Any(feature => feature.Value is < 0d or > 1d))
        {
            throw new InvalidOperationException("Candidate features must be normalized to [0,1].");
        }

        if (candidate.Features.Select(feature => feature.Name).Distinct(IdentifierComparer).Count() != candidate.Features.Count)
        {
            throw new InvalidOperationException("Candidate contains duplicate features.");
        }
    }

    private async ValueTask DemandAsync(string actor, string capability, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actor) ||
            !await _authorization.AuthorizeAsync(actor, capability, cancellationToken))
        {
            throw new UnauthorizedAccessException($"Actor is not authorized for {capability}.");
        }
    }

    private void ValidateEnvelope(string correlationId, IReadOnlyList<Uri> evidenceLinks)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID is required.");
        }

        if (correlationId.Trim().Length < 4)
        {
            throw new ArgumentException("Correlation ID must be at least 4 characters.");
        }

        if (evidenceLinks is null)
        {
            throw new ArgumentNullException(nameof(evidenceLinks));
        }

        if (evidenceLinks.Count > _options.MaxEvidenceLinks)
        {
            throw new ArgumentException("Evidence links exceed the configured bound.");
        }

        if (evidenceLinks.Any(link => link is null || link.Scheme is not ("https" or "urn")))
        {
            throw new ArgumentException("Evidence links must use https or urn.");
        }

        if (evidenceLinks.Any(link => link.OriginalString.Length > _options.MaxEvidenceLinkLength))
        {
            throw new ArgumentException("Evidence links exceed the configured length bound.");
        }
    }

    private async ValueTask WriteAuditAsync(
        string eventType,
        string actor,
        string correlationId,
        IReadOnlyList<Uri> evidenceLinks,
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken cancellationToken)
    {
        var links = evidenceLinks
            .Select((uri, index) => new XCoreEventLink($"evidence-{index + 1}", uri))
            .ToArray();

        var payloadSnapshot = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(payload, IdentifierComparer));

        var envelope = new XCoreAuditEvent(
            SchemaVersion: EventSchemaVersion,
            EventId: Guid.NewGuid().ToString("N"),
            Source: EventSource,
            EventType: eventType,
            Repository: EventRepository,
            CorrelationId: correlationId,
            Environment: _auditEnvironment,
            OccurredAt: DateTimeOffset.UtcNow,
            DataClassification: EventDataClassification,
            Actor: new XCoreEventActor(Type: "service", Id: actor, DisplayName: null),
            Links: links,
            Payload: payloadSnapshot);

        await _audit.WriteAsync(envelope, cancellationToken);
    }

    private static string NormalizeAuditEnvironment(string environment)
    {
        var normalized = environment.Trim().ToLowerInvariant();
        if (!AllowedAuditEnvironments.Contains(normalized))
        {
            throw new ArgumentException("Audit environment must be one of: local, development, test, staging, production.");
        }

        return normalized;
    }

    private static void AppendBounded<T>(List<T> entries, T value, int maxEntries)
    {
        if (entries.Count >= maxEntries)
        {
            entries.RemoveAt(0);
        }

        entries.Add(value);
    }

    private IReadOnlyList<WorkerLease> PruneLeases()
    {
        var expiredLeases = new List<WorkerLease>();
        var now = DateTimeOffset.UtcNow;
        foreach (var lease in _leases.Values.Where(lease => lease.ExpiresAt <= now))
        {
            if (_leases.TryRemove(lease.LeaseId, out var removedLease))
            {
                expiredLeases.Add(removedLease);
            }
        }

        return expiredLeases;
    }
}
