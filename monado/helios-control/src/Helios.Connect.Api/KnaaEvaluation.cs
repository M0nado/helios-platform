using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Helios.Connect.Api;

public sealed record KnaaThresholds(double Block, double Warn, double ReviewRequired);

public sealed record KnaaEvaluatorOptions(
    string SchemaVersion,
    string ModelVersion,
    KnaaThresholds Thresholds,
    bool ConservativeAutoBlock)
{
    public static KnaaEvaluatorOptions Default { get; } = new(
        "helios.knaa.v1",
        "xcore9-knaa-1.0.0",
        new KnaaThresholds(0.35, 0.55, 0.75),
        ConservativeAutoBlock: false);

    public static KnaaEvaluatorOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var defaults = Default;
        var schemaVersion = ReadString(configuration, "HELIOS_KNAA_SCHEMA_VERSION", defaults.SchemaVersion);
        if (!string.Equals(schemaVersion, defaults.SchemaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"HELIOS_KNAA_SCHEMA_VERSION is fixed to {defaults.SchemaVersion} for KNAA v1.");
        var modelVersion = ReadString(configuration, "HELIOS_KNAA_MODEL_VERSION", defaults.ModelVersion);
        var block = ReadDouble(configuration, "HELIOS_KNAA_THRESHOLD_BLOCK", defaults.Thresholds.Block);
        var warn = ReadDouble(configuration, "HELIOS_KNAA_THRESHOLD_WARN", defaults.Thresholds.Warn);
        var reviewRequired = ReadDouble(configuration, "HELIOS_KNAA_THRESHOLD_REVIEW_REQUIRED", defaults.Thresholds.ReviewRequired);
        if (!(block < warn && warn < reviewRequired))
            throw new InvalidOperationException("KNAA thresholds must satisfy block < warn < review-required.");
        var conservativeAutoBlock = ReadBoolean(configuration, "HELIOS_KNAA_CONSERVATIVE_AUTO_BLOCK", defaults.ConservativeAutoBlock);
        return new KnaaEvaluatorOptions(
            defaults.SchemaVersion,
            modelVersion,
            new KnaaThresholds(block, warn, reviewRequired),
            conservativeAutoBlock);
    }

    private static string ReadString(IConfiguration configuration, string key, string fallback)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return value.Trim();
    }

    private static bool ReadBoolean(IConfiguration configuration, string key, bool fallback)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (!bool.TryParse(value, out var parsed))
            throw new InvalidOperationException($"{key} must be true or false.");
        return parsed;
    }

    private static double ReadDouble(IConfiguration configuration, string key, double fallback)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
            throw new InvalidOperationException($"{key} must be a floating-point number between 0.0 and 1.0.");
        if (parsed is < 0.0 or > 1.0)
            throw new InvalidOperationException($"{key} must be between 0.0 and 1.0.");
        return parsed;
    }
}

public sealed record KnaaSourceSignal(
    string Id,
    string Source,
    double? RawValue,
    double? NormalizedValue,
    string State,
    string Detail);

public sealed record KnaaVectorComponent(
    string Name,
    double? Value,
    string State,
    IReadOnlyList<string> SourceSignalIds);

public sealed record KnaaPolicyDecision(
    string Outcome,
    bool AdvisoryOnly,
    bool AutoBlockTriggered,
    KnaaThresholds Thresholds,
    string Reason);

public sealed record KnaaRecommendation(
    string Outcome,
    string Detail,
    bool PromotionRecommended,
    bool DeploymentAuthorized,
    IReadOnlyList<string> BasedOnComponents);

public sealed record KnaaAssessment(
    string SchemaVersion,
    string ModelVersion,
    DateTimeOffset EvaluatedAt,
    string EvidenceState,
    double? Score,
    double Confidence,
    string Uncertainty,
    IReadOnlyList<KnaaSourceSignal> SourceSignals,
    IReadOnlyList<KnaaVectorComponent> Vector,
    KnaaPolicyDecision Policy,
    KnaaRecommendation Recommendation,
    IReadOnlyList<string> EvidenceLinks);

public interface IKnaaEvaluator
{
    KnaaAssessment Evaluate(ControlRunSnapshot run);
}

public sealed class KnaaEvaluator(KnaaEvaluatorOptions options) : IKnaaEvaluator
{
    private const string Known = "known";
    private const string Unknown = "unknown";
    private const string InsufficientEvidence = "insufficient-evidence";

    private readonly KnaaEvaluatorOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public KnaaAssessment Evaluate(ControlRunSnapshot run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var sourceSignals = BuildSignals(run);
        var signalLookup = sourceSignals.ToDictionary(signal => signal.Id, StringComparer.Ordinal);
        var vector = new[]
        {
            BuildComponent("knowledge", signalLookup, ["context-verified", "resource-coverage", "evidence-digest"]),
            BuildComponent("normalization", signalLookup, ["plan-present", "mutating-gate", "approval-step"]),
            BuildComponent("actionability", signalLookup, ["plan-present", "mutating-gate", "connector-selection-coverage"]),
            BuildComponent("assurance", signalLookup, ["evidence-digest", "approval-step", "connector-selection-coverage"])
        };

        var score = WeightedAverage(vector, new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["knowledge"] = 0.30,
            ["normalization"] = 0.20,
            ["actionability"] = 0.25,
            ["assurance"] = 0.25
        });
        var knownSignals = sourceSignals.Count(signal => signal.State == Known && signal.NormalizedValue.HasValue);
        var knownComponents = vector.Count(component => component.State == Known && component.Value.HasValue);
        var partialComponents = vector.Count(component => component.Value.HasValue);
        var signalCoverage = sourceSignals.Count == 0 ? 0d : knownSignals / (double)sourceSignals.Count;
        var componentCoverage = vector.Length == 0 ? 0d : partialComponents / (double)vector.Length;
        var confidence = Round(Math.Clamp(0.25 + (signalCoverage * 0.45) + (componentCoverage * 0.30), 0.0, 1.0));

        var requiredSignalIds = new[] { "context-verified", "plan-present", "evidence-digest" };
        var requiredSignalsKnown = requiredSignalIds.Count(id =>
            signalLookup.TryGetValue(id, out var signal) &&
            signal.State == Known &&
            signal.NormalizedValue.HasValue);
        var requiredCoverageComplete = requiredSignalsKnown == requiredSignalIds.Length;
        if (!requiredCoverageComplete) score = null;
        var uncertainty = !requiredCoverageComplete || !score.HasValue || confidence < 0.45
            ? InsufficientEvidence
            : confidence < 0.70
                ? Unknown
                : "none";
        var evidenceState = uncertainty == "none" ? "sufficient" : uncertainty;

        var policy = EvaluatePolicy(score, uncertainty);
        var recommendation = new KnaaRecommendation(
            policy.Outcome,
            BuildRecommendationDetail(policy.Outcome, uncertainty),
            PromotionRecommended: policy.Outcome == "pass" && uncertainty == "none",
            DeploymentAuthorized: false,
            BasedOnComponents: vector.Select(component => component.Name).ToArray());

        return new KnaaAssessment(
            _options.SchemaVersion,
            _options.ModelVersion,
            ResolveEvaluatedAt(run),
            evidenceState,
            score,
            confidence,
            uncertainty,
            sourceSignals,
            vector,
            policy,
            recommendation,
            BuildEvidenceLinks(run));
    }

    private KnaaPolicyDecision EvaluatePolicy(double? score, string uncertainty)
    {
        string outcome;
        string reason;
        if (uncertainty == InsufficientEvidence || !score.HasValue)
        {
            outcome = _options.ConservativeAutoBlock ? "block" : "review-required";
            reason = _options.ConservativeAutoBlock
                ? "Insufficient KNAA evidence under conservative auto-block mode."
                : "Insufficient KNAA evidence requires human review.";
        }
        else if (score.Value < _options.Thresholds.Block)
        {
            outcome = "block";
            reason = "KNAA score is below the block threshold.";
        }
        else if (score.Value < _options.Thresholds.Warn)
        {
            outcome = "warn";
            reason = "KNAA score is below the warn threshold.";
        }
        else if (score.Value < _options.Thresholds.ReviewRequired)
        {
            outcome = "review-required";
            reason = "KNAA score requires additional review.";
        }
        else
        {
            outcome = "pass";
            reason = "KNAA score is above all review thresholds.";
        }

        var autoBlockTriggered = _options.ConservativeAutoBlock && outcome == "block";
        return new KnaaPolicyDecision(
            outcome,
            AdvisoryOnly: !autoBlockTriggered,
            AutoBlockTriggered: autoBlockTriggered,
            _options.Thresholds,
            reason);
    }

    private static string BuildRecommendationDetail(string outcome, string uncertainty) => outcome switch
    {
        "block" when uncertainty == InsufficientEvidence => "Recommendation is blocked until evidence is completed and re-scored.",
        "block" => "Recommendation is blocked by KNAA policy and requires explicit operator intervention.",
        "warn" => "Recommendation is warning-level; proceed only with documented mitigations and reviewer sign-off.",
        "review-required" => "Recommendation needs manual reviewer judgment before any promotion decision.",
        "pass" => "Recommendation can be considered for promotion review, but never bypasses human approval gates.",
        _ => "Recommendation outcome is unknown; escalation to manual review is required."
    };

    private static IReadOnlyList<string> BuildEvidenceLinks(ControlRunSnapshot run)
    {
        var links = new List<string> { $"run://control-runs/{run.Id}" };
        if (!string.IsNullOrWhiteSpace(run.EvidenceSha256)) links.Add($"evidence://sha256/{run.EvidenceSha256}");
        if (!string.IsNullOrWhiteSpace(run.Plan?.PlanId)) links.Add($"plan://{run.Plan.PlanId}");
        return links;
    }

    private static DateTimeOffset ResolveEvaluatedAt(ControlRunSnapshot run)
    {
        if (run.Knaa is { EvaluatedAt: var persistedEvaluatedAt } && persistedEvaluatedAt != default)
            return persistedEvaluatedAt;
        return run.CreatedAt;
    }

    private static IReadOnlyList<KnaaSourceSignal> BuildSignals(ControlRunSnapshot run)
    {
        var contextStep = run.Steps.FirstOrDefault(step => string.Equals(step.Name, "context", StringComparison.OrdinalIgnoreCase));
        var contextVerified = contextStep is null ? (double?)null : string.Equals(contextStep.Status, "completed", StringComparison.OrdinalIgnoreCase) ? 1d : 0d;
        var contextState = contextStep is null ? InsufficientEvidence : Known;

        var digestPresent = !string.IsNullOrWhiteSpace(run.EvidenceSha256);
        var digestValid = digestPresent && run.EvidenceSha256!.Length == 64 && run.EvidenceSha256.All(character =>
            (character >= '0' && character <= '9') ||
            (character >= 'a' && character <= 'f') ||
            (character >= 'A' && character <= 'F'));
        double? evidenceDigest = digestPresent ? (digestValid ? 1d : 0d) : null;
        var digestState = digestPresent ? Known : InsufficientEvidence;

        var planPresent = run.Plan is null ? (double?)null : 1d;
        var planState = run.Plan is null ? InsufficientEvidence : Known;
        double? mutatingGate = run.Plan?.Steps.Any(step => step.Mutating) is bool mutating ? (mutating ? 1d : 0d) : null;
        var mutatingState = run.Plan is null ? Unknown : Known;

        var approvalStep = run.Steps.Any(step => string.Equals(step.Name, "approval", StringComparison.OrdinalIgnoreCase)) ? 1d : (double?)null;
        var approvalState = approvalStep.HasValue ? Known : Unknown;

        var connectorSelectionCoverage = NormalizeRatio(run.Connectors.Count, 4);
        var connectorState = Known;

        return
        [
            Signal("context-verified", "control-run-step.context", contextVerified, contextVerified, contextState, "Whether Azure boundary verification completed."),
            Signal("resource-coverage", "azure.inventory.resourceCount", run.ResourceCount, NormalizePositiveCount(run.ResourceCount, 3), Known, "Normalized inventory coverage from discovered resources."),
            Signal("evidence-digest", "control-run.evidenceSha256", evidenceDigest, evidenceDigest, digestState, "Whether the canonical orchestration evidence digest is present and valid."),
            Signal("plan-present", "edge.plan", planPresent, planPresent, planState, "Whether a deterministic orchestration plan was generated."),
            Signal("mutating-gate", "edge.plan.steps", mutatingGate, mutatingGate, mutatingState, "Whether the generated plan contains mutation-gated steps."),
            Signal("approval-step", "control-run-step.approval", approvalStep, approvalStep, approvalState, "Whether the approval boundary step exists in the run contract."),
            Signal("connector-selection-coverage", "control-run.connectors", run.Connectors.Count, connectorSelectionCoverage, connectorState, "Normalized selected connector count coverage.")
        ];
    }

    private static KnaaSourceSignal Signal(
        string id,
        string source,
        double? rawValue,
        double? normalizedValue,
        string state,
        string detail) =>
        new(id, source, rawValue, normalizedValue, state, detail);

    private static KnaaVectorComponent BuildComponent(
        string name,
        IReadOnlyDictionary<string, KnaaSourceSignal> signals,
        IReadOnlyList<string> sourceSignalIds)
    {
        var members = sourceSignalIds
            .Where(id => signals.ContainsKey(id))
            .Select(id => signals[id])
            .ToArray();
        var values = members.Where(member => member.NormalizedValue.HasValue)
            .Select(member => member.NormalizedValue!.Value)
            .ToArray();
        double? value = values.Length == 0 ? null : Round(values.Average());
        var knownCount = members.Count(member => member.State == Known && member.NormalizedValue.HasValue);
        var state = knownCount == sourceSignalIds.Count
            ? Known
            : knownCount == 0
                ? InsufficientEvidence
                : Unknown;
        return new KnaaVectorComponent(name, value, state, sourceSignalIds.ToArray());
    }

    private static double? WeightedAverage(
        IReadOnlyList<KnaaVectorComponent> vector,
        IReadOnlyDictionary<string, double> weights)
    {
        var weightedTotal = 0d;
        var weightSum = 0d;
        foreach (var component in vector)
        {
            if (!component.Value.HasValue) continue;
            if (!weights.TryGetValue(component.Name, out var weight)) continue;
            weightedTotal += component.Value.Value * weight;
            weightSum += weight;
        }
        return weightSum <= 0d ? null : Round(weightedTotal / weightSum);
    }

    private static double NormalizePositiveCount(int value, int saturationPoint)
    {
        if (saturationPoint <= 0) return 1d;
        if (value <= 0) return 0d;
        var normalized = Math.Log10(1 + value) / Math.Log10(1 + saturationPoint);
        return Round(Math.Clamp(normalized, 0d, 1d));
    }

    private static double NormalizeRatio(int value, int denominator)
    {
        if (denominator <= 0) return 1d;
        if (value <= 0) return 0d;
        return Round(Math.Clamp(value / (double)denominator, 0d, 1d));
    }

    private static double Round(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
