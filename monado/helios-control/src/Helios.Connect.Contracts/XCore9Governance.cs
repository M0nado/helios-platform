namespace Helios.Connect.Contracts;

public enum XCore9Recommendation
{
    Unknown,
    Block,
    Warn,
    ReviewRequired,
    Pass
}

public sealed record XCore9KnaaVector(
    double? Knowledge,
    double? Novelty,
    double? Actionability,
    double? Alignment);

public sealed record XCore9KnaaThresholds(
    double BlockBelow,
    double WarnBelow,
    double ReviewBelow)
{
    public void Validate()
    {
        if (BlockBelow is < 0 or > 1 ||
            WarnBelow is < 0 or > 1 ||
            ReviewBelow is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(BlockBelow), "KNAA thresholds must be between 0 and 1.");
        }

        if (!(BlockBelow < WarnBelow && WarnBelow < ReviewBelow))
        {
            throw new ArgumentException("KNAA thresholds must satisfy block < warn < review.");
        }
    }
}

public sealed record XCore9KnaaWeights(
    double Knowledge = 0.3,
    double Novelty = 0.2,
    double Actionability = 0.3,
    double Alignment = 0.2)
{
    public void Validate()
    {
        var values = new[] { Knowledge, Novelty, Actionability, Alignment };
        if (values.Any(weight => !double.IsFinite(weight) || weight is < 0 or > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(Knowledge), "KNAA weights must be between 0 and 1.");
        }

        if (values.Sum() <= 0)
        {
            throw new ArgumentException("At least one KNAA weight must be greater than zero.");
        }
    }
}

public sealed record XCore9KnaaPolicy(
    string ModelVersion,
    XCore9KnaaThresholds Thresholds,
    bool ConservativeAutoBlock = false,
    XCore9KnaaWeights? Weights = null);

public sealed record XCore9KnaaAuditPayload(
    string ModelVersion,
    XCore9KnaaThresholds Thresholds,
    IReadOnlyList<string> EvidenceLinks,
    string PolicyMode,
    XCore9Recommendation Recommendation,
    double Confidence);

public sealed record XCore9KnaaEvaluation(
    XCore9Recommendation Recommendation,
    double? CompositeScore,
    double Confidence,
    string Reason,
    XCore9KnaaAuditPayload Audit);

public static class XCore9KnaaEvaluator
{
    public static XCore9KnaaEvaluation Evaluate(
        XCore9KnaaVector vector,
        XCore9KnaaPolicy policy,
        IReadOnlyList<string>? evidenceLinks = null)
    {
        ArgumentNullException.ThrowIfNull(vector);
        ArgumentNullException.ThrowIfNull(policy);
        if (string.IsNullOrWhiteSpace(policy.ModelVersion))
        {
            throw new ArgumentException("ModelVersion is required.", nameof(policy));
        }

        policy.Thresholds.Validate();
        var weights = policy.Weights ?? new XCore9KnaaWeights();
        weights.Validate();

        var cleanedEvidence = CleanValues(evidenceLinks);
        var knownValues = BuildKnownValues(vector, weights);
        var confidence = knownValues.Count / 4d;
        var policyMode = policy.ConservativeAutoBlock ? "conservative-auto-block" : "advisory";

        if (knownValues.Count < 2)
        {
            var unknownRecommendation = XCore9Recommendation.Unknown;
            var audit = new XCore9KnaaAuditPayload(
                policy.ModelVersion,
                policy.Thresholds,
                cleanedEvidence,
                policyMode,
                unknownRecommendation,
                confidence);
            return new XCore9KnaaEvaluation(
                unknownRecommendation,
                null,
                confidence,
                "insufficient-evidence",
                audit);
        }

        var weightedValues = knownValues.Where(value => value.Weight > 0).ToArray();
        var weightTotal = weightedValues.Sum(value => value.Weight);
        if (weightTotal <= 0)
        {
            var unknownRecommendation = XCore9Recommendation.Unknown;
            var audit = new XCore9KnaaAuditPayload(
                policy.ModelVersion,
                policy.Thresholds,
                cleanedEvidence,
                policyMode,
                unknownRecommendation,
                confidence);
            return new XCore9KnaaEvaluation(
                unknownRecommendation,
                null,
                confidence,
                "insufficient-scoring-weight",
                audit);
        }

        var score = weightedValues.Sum(value => value.Value * value.Weight) / weightTotal;
        var recommendation = ResolveRecommendation(score, policy, out var reason);
        var scoredAudit = new XCore9KnaaAuditPayload(
            policy.ModelVersion,
            policy.Thresholds,
            cleanedEvidence,
            policyMode,
            recommendation,
            confidence);
        return new XCore9KnaaEvaluation(
            recommendation,
            score,
            confidence,
            reason,
            scoredAudit);
    }

    private sealed record XCore9KnaaDimensionValue(double Value, double Weight);

    private static List<XCore9KnaaDimensionValue> BuildKnownValues(XCore9KnaaVector vector, XCore9KnaaWeights weights)
    {
        var values = new List<XCore9KnaaDimensionValue>(4);
        AddKnownValue(values, vector.Knowledge, weights.Knowledge);
        AddKnownValue(values, vector.Novelty, weights.Novelty);
        AddKnownValue(values, vector.Actionability, weights.Actionability);
        AddKnownValue(values, vector.Alignment, weights.Alignment);
        return values;
    }

    private static void AddKnownValue(ICollection<XCore9KnaaDimensionValue> values, double? candidate, double weight)
    {
        if (!candidate.HasValue)
        {
            return;
        }

        if (!double.IsFinite(candidate.Value))
        {
            return;
        }

        values.Add(new XCore9KnaaDimensionValue(Math.Clamp(candidate.Value, 0d, 1d), weight));
    }

    private static List<string> CleanValues(IReadOnlyList<string>? values) =>
        (values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static XCore9Recommendation ResolveRecommendation(
        double score,
        XCore9KnaaPolicy policy,
        out string reason)
    {
        if (score < policy.Thresholds.BlockBelow)
        {
            if (policy.ConservativeAutoBlock)
            {
                reason = "below-block-threshold";
                return XCore9Recommendation.Block;
            }

            reason = "below-block-threshold-advisory";
            return XCore9Recommendation.ReviewRequired;
        }

        if (score < policy.Thresholds.WarnBelow)
        {
            reason = "below-warn-threshold";
            return XCore9Recommendation.Warn;
        }

        if (score < policy.Thresholds.ReviewBelow)
        {
            reason = "review-threshold";
            return XCore9Recommendation.ReviewRequired;
        }

        reason = "pass-threshold";
        return XCore9Recommendation.Pass;
    }
}

public sealed record XCore9SpecializationPack(
    string Id,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> DeniedTools,
    IReadOnlyList<string> RequiredCapabilityContracts,
    int MaxParallelism,
    int TimeoutSeconds,
    bool RequireIdempotencyKey);

public sealed record XCore9SpecializationGlobalPolicy(
    int MaxFanOut = 4,
    int MaxFanIn = 4)
{
    public void Validate()
    {
        if (MaxFanOut < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFanOut), "MaxFanOut must be greater than zero.");
        }

        if (MaxFanIn < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFanIn), "MaxFanIn must be greater than zero.");
        }
    }
}

public sealed record XCore9SpecializationInvocation(
    string PackId,
    string Tool,
    string InputModality,
    string OutputModality,
    int RequestedParallelism,
    string CorrelationId,
    string? IdempotencyKey,
    IReadOnlyList<string> EvidenceLinks,
    IReadOnlyList<string> CapabilityContracts,
    int CoordinatorFanOut = 1,
    int CoordinatorFanIn = 1,
    IReadOnlyDictionary<string, string>? Provenance = null);

public sealed record XCore9RoutingEvidence(
    string CorrelationId,
    string InputModality,
    string OutputModality,
    IReadOnlyList<string> EvidenceLinks,
    IReadOnlyDictionary<string, string> Provenance,
    int TimeoutSeconds);

public sealed record XCore9SpecializationDecision(
    bool Allowed,
    string Code,
    string Message,
    XCore9RoutingEvidence? Evidence);

public sealed class XCore9SpecializationRegistry
{
    private readonly IReadOnlyDictionary<string, XCore9SpecializationPack> _packs;
    private readonly IReadOnlyDictionary<string, IReadOnlySet<string>> _laneProvenanceRequirements;
    private readonly XCore9SpecializationGlobalPolicy _globalPolicy;

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> DefaultLaneProvenanceRequirements =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["text"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "correlationId",
                "evidenceLinks",
                "sourceEventId",
            },
            ["code"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "correlationId",
                "evidenceLinks",
                "sourceCommit",
            },
            ["docs"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "correlationId",
                "evidenceLinks",
                "documentId",
            },
            ["telemetry"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "correlationId",
                "evidenceLinks",
                "traceParent",
            },
            ["media"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "correlationId",
                "evidenceLinks",
                "mediaDigest",
            },
        };

    public XCore9SpecializationRegistry(
        IEnumerable<XCore9SpecializationPack> packs,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? laneProvenanceRequirements = null,
        XCore9SpecializationGlobalPolicy? globalPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(packs);
        _packs = packs
            .Select(NormalizePack)
            .ToDictionary(
            pack => !string.IsNullOrWhiteSpace(pack.Id)
                ? pack.Id
                : throw new ArgumentException("Pack ID is required.", nameof(packs)),
            StringComparer.OrdinalIgnoreCase);
        _laneProvenanceRequirements = laneProvenanceRequirements is null
            ? DefaultLaneProvenanceRequirements
            : NormalizeLaneProvenanceRequirements(laneProvenanceRequirements);
        _globalPolicy = globalPolicy ?? new XCore9SpecializationGlobalPolicy();
        _globalPolicy.Validate();
    }

    public XCore9SpecializationDecision Evaluate(XCore9SpecializationInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        if (!_packs.TryGetValue(invocation.PackId, out var pack))
        {
            return Deny("unknown-pack", $"Pack '{invocation.PackId}' is not registered.");
        }

        if (invocation.RequestedParallelism < 1)
        {
            return Deny("invalid-parallelism", "Requested parallelism must be greater than zero.");
        }

        if (invocation.RequestedParallelism > pack.MaxParallelism)
        {
            return Deny("parallelism-exceeded", $"Requested parallelism exceeds maxParallelism ({pack.MaxParallelism}).");
        }

        if (string.IsNullOrWhiteSpace(invocation.CorrelationId))
        {
            return Deny("missing-correlation-id", "CorrelationId is required.");
        }
        var normalizedCorrelationId = invocation.CorrelationId.Trim();

        var evidence = CleanValues(invocation.EvidenceLinks);
        if (evidence.Count == 0)
        {
            return Deny("missing-evidence-links", "At least one evidence link is required.");
        }

        var deniedTools = ToSet(pack.DeniedTools);
        if (deniedTools.Contains(invocation.Tool))
        {
            return Deny("tool-denied", $"Tool '{invocation.Tool}' is denied for pack '{pack.Id}'.");
        }

        var allowedTools = ToSet(pack.AllowedTools);
        if (!allowedTools.Contains(invocation.Tool))
        {
            return Deny("tool-not-declared", $"Tool '{invocation.Tool}' is not declared in allowedTools.");
        }

        if (!ToSet(pack.Inputs).Contains(invocation.InputModality))
        {
            return Deny("input-modality-not-declared", $"Input modality '{invocation.InputModality}' is not allowed.");
        }

        if (!ToSet(pack.Outputs).Contains(invocation.OutputModality))
        {
            return Deny("output-modality-not-declared", $"Output modality '{invocation.OutputModality}' is not allowed.");
        }

        var presentedContracts = ToSet(invocation.CapabilityContracts);
        var missingContracts = ToSet(pack.RequiredCapabilityContracts)
            .Where(requiredContract => !presentedContracts.Contains(requiredContract))
            .ToArray();
        if (missingContracts.Length > 0)
        {
            return Deny("missing-capability-contract", $"Missing capability contracts: {string.Join(", ", missingContracts)}");
        }

        if (pack.RequireIdempotencyKey && string.IsNullOrWhiteSpace(invocation.IdempotencyKey))
        {
            return Deny("missing-idempotency-key", "Idempotency key is required by this pack.");
        }

        if (pack.TimeoutSeconds <= 0)
        {
            return Deny("invalid-pack-timeout", "Pack timeoutSeconds must be greater than zero.");
        }

        if (invocation.CoordinatorFanOut < 1 || invocation.CoordinatorFanIn < 1)
        {
            return Deny("invalid-coordinator-load", "Coordinator fan-out and fan-in must be greater than zero.");
        }

        if (invocation.CoordinatorFanOut > _globalPolicy.MaxFanOut)
        {
            return Deny(
                "fan-out-exceeded",
                $"Coordinator fan-out exceeds maxFanOut ({_globalPolicy.MaxFanOut}).");
        }

        if (invocation.CoordinatorFanIn > _globalPolicy.MaxFanIn)
        {
            return Deny(
                "fan-in-exceeded",
                $"Coordinator fan-in exceeds maxFanIn ({_globalPolicy.MaxFanIn}).");
        }

        var provenance = NormalizeProvenance(invocation.Provenance, normalizedCorrelationId, evidence);
        var requiredProvenance = RequiredProvenanceForModalities(invocation.InputModality, invocation.OutputModality);
        var missingProvenance = requiredProvenance
            .Where(required => !provenance.ContainsKey(required))
            .ToArray();
        if (missingProvenance.Length > 0)
        {
            return Deny(
                "missing-lane-provenance",
                $"Missing lane provenance fields: {string.Join(", ", missingProvenance)}");
        }

        var routingEvidence = new XCore9RoutingEvidence(
            normalizedCorrelationId,
            invocation.InputModality,
            invocation.OutputModality,
            evidence,
            provenance,
            pack.TimeoutSeconds);
        return new XCore9SpecializationDecision(true, "allowed", "Invocation is allowed.", routingEvidence);
    }

    private static XCore9SpecializationDecision Deny(string code, string message) =>
        new(false, code, message, null);

    private static HashSet<string> ToSet(IEnumerable<string>? values) =>
        new(
            (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()),
            StringComparer.OrdinalIgnoreCase);

    private static List<string> CleanValues(IEnumerable<string>? values) =>
        (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static XCore9SpecializationPack NormalizePack(XCore9SpecializationPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        return new XCore9SpecializationPack(
            Id: pack.Id?.Trim() ?? string.Empty,
            Inputs: SnapshotValues(pack.Inputs),
            Outputs: SnapshotValues(pack.Outputs),
            AllowedTools: SnapshotValues(pack.AllowedTools),
            DeniedTools: SnapshotValues(pack.DeniedTools),
            RequiredCapabilityContracts: SnapshotValues(pack.RequiredCapabilityContracts),
            MaxParallelism: pack.MaxParallelism,
            TimeoutSeconds: pack.TimeoutSeconds,
            RequireIdempotencyKey: pack.RequireIdempotencyKey);
    }

    private static string[] SnapshotValues(IEnumerable<string>? values) =>
        (values ?? Array.Empty<string>())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private IReadOnlySet<string> RequiredProvenanceForModalities(params string[] modalities)
    {
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var modality in modalities.Where(modality => _laneProvenanceRequirements.ContainsKey(modality)))
        {
            required.UnionWith(_laneProvenanceRequirements[modality]);
        }

        return required;
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> NormalizeLaneProvenanceRequirements(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> requirements)
    {
        var normalized = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in requirements)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            var keys = new HashSet<string>(
                (entry.Value ?? Array.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (keys.Count > 0)
            {
                normalized[entry.Key.Trim()] = keys;
            }
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, string> NormalizeProvenance(
        IReadOnlyDictionary<string, string>? values,
        string correlationId,
        IReadOnlyList<string> evidenceLinks)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values is not null)
        {
            foreach (var entry in values)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                normalized[entry.Key.Trim()] = entry.Value.Trim();
            }
        }

        normalized["correlationId"] = correlationId.Trim();
        normalized["evidenceLinks"] = string.Join(",", evidenceLinks);
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(normalized);
    }
}
