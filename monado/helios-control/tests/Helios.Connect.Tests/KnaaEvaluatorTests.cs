using Helios.Connect.Api;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class KnaaEvaluatorTests
{
    [Fact]
    public void Evaluate_returns_pass_for_complete_evidence_with_default_thresholds()
    {
        var evaluator = new KnaaEvaluator(KnaaEvaluatorOptions.Default);
        var run = CreateSnapshot(
            steps:
            [
                new ControlRunStep("context", "completed", "ok"),
                new ControlRunStep("approval", "queued", "pending")
            ],
            plan: CreatePlan(mutating: true),
            evidenceSha256: new string('a', 64),
            resourceCount: 3,
            connectors: ["github", "slack"]);

        var result = evaluator.Evaluate(run);

        Assert.Equal("helios.knaa.v1", result.SchemaVersion);
        Assert.Equal("xcore9-knaa-1.0.0", result.ModelVersion);
        Assert.Equal("sufficient", result.EvidenceState);
        Assert.Equal("none", result.Uncertainty);
        Assert.NotNull(result.Score);
        Assert.InRange(result.Score!.Value, 0.75, 1.0);
        Assert.InRange(result.Confidence, 0.70, 1.0);
        Assert.Equal("pass", result.Policy.Outcome);
    }

    [Fact]
    public void Evaluate_returns_insufficient_evidence_when_required_signals_are_missing()
    {
        var evaluator = new KnaaEvaluator(KnaaEvaluatorOptions.Default);
        var run = CreateSnapshot(
            steps:
            [
                new ControlRunStep("context", "queued", "pending")
            ],
            plan: null,
            evidenceSha256: null,
            resourceCount: 0,
            connectors: []);

        var result = evaluator.Evaluate(run);

        Assert.Equal("insufficient-evidence", result.EvidenceState);
        Assert.Equal("insufficient-evidence", result.Uncertainty);
        Assert.Null(result.Score);
        Assert.Equal("review-required", result.Policy.Outcome);
        Assert.True(result.Policy.AdvisoryOnly);
        Assert.False(result.Recommendation.PromotionRecommended);
    }

    [Fact]
    public void Evaluate_honors_block_warn_and_review_required_outcomes()
    {
        var completeRun = CreateSnapshot(
            steps:
            [
                new ControlRunStep("context", "completed", "ok"),
                new ControlRunStep("approval", "queued", "pending")
            ],
            plan: CreatePlan(mutating: true),
            evidenceSha256: new string('a', 64),
            resourceCount: 3,
            connectors: ["github", "slack"]);
        var defaultEvaluator = new KnaaEvaluator(KnaaEvaluatorOptions.Default);
        var baseline = defaultEvaluator.Evaluate(completeRun);
        Assert.NotNull(baseline.Score);
        var score = baseline.Score!.Value;

        var warnOptions = new KnaaEvaluatorOptions(
            "helios.knaa.v1",
            "xcore9-knaa-1.0.0",
            new KnaaThresholds(
                Block: Math.Max(0.0, score - 0.30),
                Warn: Math.Min(0.99, score + 0.01),
                ReviewRequired: Math.Min(1.0, score + 0.10)),
            ConservativeAutoBlock: false);
        var warnResult = new KnaaEvaluator(warnOptions).Evaluate(completeRun);
        Assert.Equal("warn", warnResult.Policy.Outcome);

        var reviewThresholds = new KnaaThresholds(
            Block: Math.Max(0.0, score - 0.30),
            Warn: Math.Max(0.0, score - 0.01),
            ReviewRequired: Math.Min(1.0, score + 0.01));
        Assert.True(reviewThresholds.Block < reviewThresholds.Warn && reviewThresholds.Warn < reviewThresholds.ReviewRequired);
        var reviewOptions = new KnaaEvaluatorOptions(
            "helios.knaa.v1",
            "xcore9-knaa-1.0.0",
            reviewThresholds,
            ConservativeAutoBlock: false);
        var reviewResult = new KnaaEvaluator(reviewOptions).Evaluate(completeRun);
        Assert.Equal("review-required", reviewResult.Policy.Outcome);

        var lowSignalRun = CreateSnapshot(
            steps:
            [
                new ControlRunStep("context", "queued", "pending"),
                new ControlRunStep("approval", "queued", "pending")
            ],
            plan: CreatePlan(mutating: false),
            evidenceSha256: "invalid-digest",
            resourceCount: 0,
            connectors: []);
        var blockOptions = new KnaaEvaluatorOptions(
            "helios.knaa.v1",
            "xcore9-knaa-1.0.0",
            new KnaaThresholds(0.80, 0.90, 0.95),
            ConservativeAutoBlock: false);
        var blockResult = new KnaaEvaluator(blockOptions).Evaluate(lowSignalRun);
        Assert.Equal("block", blockResult.Policy.Outcome);
        Assert.NotNull(blockResult.Score);
        Assert.True(blockResult.Score < blockOptions.Thresholds.Block);
    }

    [Fact]
    public void Evaluate_conservative_auto_block_escalates_insufficient_evidence()
    {
        var options = new KnaaEvaluatorOptions(
            "helios.knaa.v1",
            "xcore9-knaa-1.0.0",
            new KnaaThresholds(0.35, 0.55, 0.75),
            ConservativeAutoBlock: true);
        var evaluator = new KnaaEvaluator(options);
        var run = CreateSnapshot(
            steps: [new ControlRunStep("context", "queued", "pending")],
            plan: null,
            evidenceSha256: null,
            resourceCount: 0,
            connectors: []);

        var result = evaluator.Evaluate(run);

        Assert.Equal("block", result.Policy.Outcome);
        Assert.True(result.Policy.AutoBlockTriggered);
        Assert.False(result.Policy.AdvisoryOnly);
    }

    [Fact]
    public void FromConfiguration_rejects_non_v1_schema_version()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HELIOS_KNAA_SCHEMA_VERSION"] = "helios.knaa.v2"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => KnaaEvaluatorOptions.FromConfiguration(configuration));
        Assert.Contains("HELIOS_KNAA_SCHEMA_VERSION", exception.Message);
    }

    [Fact]
    public void Evaluate_reuses_persisted_evaluated_timestamp_for_recovery()
    {
        var evaluator = new KnaaEvaluator(KnaaEvaluatorOptions.Default);
        var run = CreateSnapshot(
            steps:
            [
                new ControlRunStep("context", "completed", "ok"),
                new ControlRunStep("approval", "queued", "pending")
            ],
            plan: CreatePlan(mutating: true),
            evidenceSha256: new string('a', 64),
            resourceCount: 3,
            connectors: ["github", "slack"]);

        var initialAssessment = evaluator.Evaluate(run);
        var persistedAssessment = initialAssessment with { EvaluatedAt = initialAssessment.EvaluatedAt.AddMinutes(-5) };

        var recovered = evaluator.Evaluate(run with
        {
            Knaa = persistedAssessment,
            UpdatedAt = run.UpdatedAt.AddMinutes(15)
        });

        Assert.Equal(persistedAssessment.EvaluatedAt, recovered.EvaluatedAt);
    }

    [Fact]
    public void Evaluate_scores_empty_connector_selection_as_known_zero_coverage()
    {
        var evaluator = new KnaaEvaluator(KnaaEvaluatorOptions.Default);
        var noConnectorRun = CreateSnapshot(
            steps:
            [
                new ControlRunStep("context", "completed", "ok"),
                new ControlRunStep("approval", "queued", "pending")
            ],
            plan: CreatePlan(mutating: true),
            evidenceSha256: new string('a', 64),
            resourceCount: 3,
            connectors: []);

        var oneConnectorRun = noConnectorRun with { Connectors = ["github"] };

        var noConnectorResult = evaluator.Evaluate(noConnectorRun);
        var oneConnectorResult = evaluator.Evaluate(oneConnectorRun);

        var connectorCoverage = Assert.Single(noConnectorResult.SourceSignals, signal => signal.Id == "connector-selection-coverage");
        Assert.Equal("known", connectorCoverage.State);
        Assert.Equal(0d, connectorCoverage.NormalizedValue);
        Assert.NotNull(noConnectorResult.Score);
        Assert.NotNull(oneConnectorResult.Score);
        Assert.True(noConnectorResult.Score <= oneConnectorResult.Score);
    }

    private static EdgeAutomationPlan CreatePlan(bool mutating) =>
        new(
            "plan-123",
            "provision-resources",
            "dev",
            "helios-dev-rg",
            "all",
            "plan-only",
            CanApplyFromMcp: false,
            DirectMainWrite: false,
            AutomaticMerge: false,
            RequiredApprovals: ["reviewed-plan-sha256"],
            Steps: [new EdgeAutomationStep(1, "gate", "protected-workflow", mutating, "approval")]);

    private static ControlRunSnapshot CreateSnapshot(
        IReadOnlyList<ControlRunStep> steps,
        EdgeAutomationPlan? plan,
        string? evidenceSha256,
        int resourceCount,
        IReadOnlyList<string> connectors) =>
        new(
            Id: "0123456789abcdef0123456789abcdef",
            PartitionKey: "control-runs",
            RequestSha256: new string('b', 64),
            CorrelationId: "corr-1",
            RequestedBy: "principal-1",
            Intent: "provision-resources",
            Environment: "dev",
            Target: "helios-dev-rg",
            Connectors: connectors,
            Status: "running",
            Mode: "diagnose-plan-sync",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Steps: steps,
            Receipts: [],
            Plan: plan,
            EvidenceSha256: evidenceSha256,
            ResourceCount: resourceCount);
}
