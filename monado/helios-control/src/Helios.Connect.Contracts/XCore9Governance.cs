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

public sealed record XCore9KnaaPolicy(
    string ModelVersion,
    XCore9KnaaThresholds Thresholds,
    bool ConservativeAutoBlock = false);

public sealed record XCore9KnaaAuditPayload(
    string ModelVersion,
    XCore9KnaaThresholds Thresholds,
    IReadOnlyList<string> EvidenceLinks,
    string PolicyMode);

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
        var cleanedEvidence = CleanValues(evidenceLinks);
        var values = BuildKnownValues(vector);
        var confidence = values.Count / 4d;
        var audit = new XCore9KnaaAuditPayload(
            policy.ModelVersion,
            policy.Thresholds,
            cleanedEvidence,
            policy.ConservativeAutoBlock ? "conservative-auto-block" : "advisory");

        if (values.Count < 2)
        {
            return new XCore9KnaaEvaluation(
                XCore9Recommendation.Unknown,
                null,
                confidence,
                "insufficient-evidence",
                audit);
        }

        var score = values.Average();
        var recommendation = ResolveRecommendation(score, policy, out var reason);
        return new XCore9KnaaEvaluation(
            recommendation,
            score,
            confidence,
            reason,
            audit);
    }

    private static List<double> BuildKnownValues(XCore9KnaaVector vector)
    {
        var values = new List<double>(4);
        AddKnownValue(values, vector.Knowledge);
        AddKnownValue(values, vector.Novelty);
        AddKnownValue(values, vector.Actionability);
        AddKnownValue(values, vector.Alignment);
        return values;
    }

    private static void AddKnownValue(ICollection<double> values, double? candidate)
    {
        if (!candidate.HasValue)
        {
            return;
        }

        values.Add(Math.Clamp(candidate.Value, 0d, 1d));
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

public sealed record XCore9SpecializationInvocation(
    string PackId,
    string Tool,
    string InputModality,
    string OutputModality,
    int RequestedParallelism,
    string CorrelationId,
    string? IdempotencyKey,
    IReadOnlyList<string> EvidenceLinks,
    IReadOnlyList<string> CapabilityContracts);

public sealed record XCore9RoutingEvidence(
    string CorrelationId,
    string InputModality,
    string OutputModality,
    IReadOnlyList<string> EvidenceLinks);

public sealed record XCore9SpecializationDecision(
    bool Allowed,
    string Code,
    string Message,
    XCore9RoutingEvidence? Evidence);

public sealed class XCore9SpecializationRegistry
{
    private readonly IReadOnlyDictionary<string, XCore9SpecializationPack> _packs;

    public XCore9SpecializationRegistry(IEnumerable<XCore9SpecializationPack> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);
        _packs = packs.ToDictionary(
            pack => !string.IsNullOrWhiteSpace(pack.Id)
                ? pack.Id
                : throw new ArgumentException("Pack ID is required.", nameof(packs)),
            StringComparer.OrdinalIgnoreCase);
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

        var routingEvidence = new XCore9RoutingEvidence(
            invocation.CorrelationId,
            invocation.InputModality,
            invocation.OutputModality,
            evidence);
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
}
