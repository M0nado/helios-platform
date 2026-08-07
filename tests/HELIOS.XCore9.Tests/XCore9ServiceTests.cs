using HELIOS.Analytics.FSharp;
using HELIOS.Platform.Contracts.XCore9;
using HELIOS.XCore9;
using System.Reflection;
using Xunit;

namespace HELIOS.XCore9.Tests;

public sealed class XCore9ServiceTests
{
    [Fact]
    public async Task Selection_rejects_unknown_templates_and_enforces_limits()
    {
        var (service, _) = Create(options: new XCore9Options(MaxTotalInstances: 1, MaxCpuUnits: 2, MaxMemoryMiB: 512));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SelectWorkerAsync("generated", "safe", "corr-1", "operator", default).AsTask());

        var lease = await service.SelectWorkerAsync("reviewer", "safe", "corr-1", "operator", default);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SelectWorkerAsync("reviewer", "safe", "corr-2", "operator", default).AsTask());

        await service.ReleaseWorkerAsync(lease, "corr-1", "operator", default);
    }

    [Fact]
    public async Task Selection_prevents_integer_overflow_from_bypassing_resource_limits()
    {
        const int heavyUnits = 1_500_000_000;
        var options = new XCore9Options(MaxTotalInstances: 3, MaxCpuUnits: int.MaxValue, MaxMemoryMiB: int.MaxValue);
        var template = new WorkerTemplate(
            TemplateId: "reviewer",
            MaxInstances: 3,
            CpuUnits: heavyUnits,
            MemoryMiB: heavyUnits,
            AllowedToolchainIds: new HashSet<string> { "safe" },
            PromptDigest: "sha256:fixed");
        var (service, _) = Create(options: options, template: template);

        _ = await service.SelectWorkerAsync("reviewer", "safe", "corr-overflow-1", "operator", default);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SelectWorkerAsync("reviewer", "safe", "corr-overflow-2", "operator", default).AsTask());
    }

    [Fact]
    public async Task Selection_is_audited_and_rolls_back_on_audit_failure()
    {
        var (service, audit) = Create();
        var lease = await service.SelectWorkerAsync("reviewer", "safe", "corr-1", "operator", default);

        var selectedEvent = Assert.Single(audit.Events, evt => evt.EventType == "xcore9.worker.selected");
        Assert.Equal("corr-1", selectedEvent.CorrelationId);
        Assert.Equal("reviewer", selectedEvent.Payload["templateId"]);

        await service.ReleaseWorkerAsync(lease, "corr-1", "operator", default);

        var failingAudit = new RecordingAuditSink(failEventType: "xcore9.worker.selected");
        var (failingService, _) = Create(
            options: new XCore9Options(MaxTotalInstances: 1),
            audit: failingAudit);

        var first = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failingService.SelectWorkerAsync("reviewer", "safe", "corr-2", "operator", default).AsTask());
        Assert.Equal("audit-sink-failure", first.Message);

        var second = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failingService.SelectWorkerAsync("reviewer", "safe", "corr-3", "operator", default).AsTask());
        Assert.Equal("audit-sink-failure", second.Message);
    }

    [Fact]
    public async Task Selection_enforces_minimum_correlation_id_length()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SelectWorkerAsync("reviewer", "safe", "c-1", "operator", default).AsTask());
    }

    [Fact]
    public async Task Worker_release_requires_matching_correlation()
    {
        var (service, _) = Create();
        var lease = await service.SelectWorkerAsync("reviewer", "safe", "corr-6", "operator", default);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ReleaseWorkerAsync(lease, "corr-7", "operator", default).AsTask());
    }

    [Fact]
    public async Task Worker_release_keeps_lease_active_when_release_audit_fails()
    {
        var audit = new RecordingAuditSink(failEventType: "xcore9.worker.released");
        var (service, _) = Create(options: new XCore9Options(MaxTotalInstances: 1), audit: audit);
        var lease = await service.SelectWorkerAsync("reviewer", "safe", "corr-4", "operator", default);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReleaseWorkerAsync(lease, "corr-4", "operator", default).AsTask());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SelectWorkerAsync("reviewer", "safe", "corr-5", "operator", default).AsTask());
    }

    [Fact]
    public async Task Worker_release_is_serialized_and_audited_once_for_duplicate_requests()
    {
        var (service, audit) = Create();
        var lease = await service.SelectWorkerAsync("reviewer", "safe", "corr-8", "operator", default);

        async Task<Exception?> TryReleaseAsync()
        {
            try
            {
                await service.ReleaseWorkerAsync(lease, "corr-8", "operator", default);
                return null;
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }
        }

        var outcomes = await Task.WhenAll(TryReleaseAsync(), TryReleaseAsync());
        Assert.Single(outcomes, outcome => outcome is null);
        Assert.Single(outcomes.OfType<InvalidOperationException>());
        Assert.Single(audit.Events, evt => evt.EventType == "xcore9.worker.released");
    }

    [Fact]
    public async Task Selection_prunes_expired_leases_and_audits_expiration()
    {
        var (service, audit) = Create(options: new XCore9Options(MaxTotalInstances: 1, LeaseDuration: TimeSpan.FromMilliseconds(5)));

        _ = await service.SelectWorkerAsync("reviewer", "safe", "corr-9", "operator", default);
        await Task.Delay(30);
        _ = await service.SelectWorkerAsync("reviewer", "safe", "corr-10", "operator", default);

        var expiredEvent = Assert.Single(audit.Events, evt => evt.EventType == "xcore9.worker.expired");
        Assert.Equal("corr-9", expiredEvent.CorrelationId);
    }

    [Fact]
    public async Task Selection_reapplies_only_unaudited_expired_leases_after_audit_failure()
    {
        var audit = new RecordingAuditSink(failEventType: "xcore9.worker.expired", failOnOccurrence: 2);
        var options = new XCore9Options(MaxTotalInstances: 4, LeaseDuration: TimeSpan.FromMilliseconds(20));
        var template = new WorkerTemplate(
            TemplateId: "reviewer",
            MaxInstances: 4,
            CpuUnits: 2,
            MemoryMiB: 512,
            AllowedToolchainIds: new HashSet<string> { "safe" },
            PromptDigest: "sha256:fixed");
        var (service, _) = Create(options: options, audit: audit, template: template);

        _ = await service.SelectWorkerAsync("reviewer", "safe", "corr-exp-a", "operator", default);
        _ = await service.SelectWorkerAsync("reviewer", "safe", "corr-exp-b", "operator", default);

        await Task.Delay(80);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SelectWorkerAsync("reviewer", "safe", "corr-exp-c", "operator", default).AsTask());

        _ = await service.SelectWorkerAsync("reviewer", "safe", "corr-exp-d", "operator", default);

        var expiredEvents = audit.Events
            .Where(evt => evt.EventType == "xcore9.worker.expired")
            .Select(evt => evt.CorrelationId)
            .ToArray();
        Assert.Equal(new[] { "corr-exp-a", "corr-exp-b" }, expiredEvents.OrderBy(static value => value, StringComparer.Ordinal));
    }

    [Fact]
    public void Scoring_rejects_template_toolchain_pair_outside_allowlist()
    {
        var (service, _) = Create();

        var candidate = new CandidateRoute(
            RouteId: "risky-route",
            WorkerTemplateId: "reviewer",
            ToolchainId: "risky",
            Features: [new SanitizedRunFeature("success_rate", 0.8)]);

        Assert.Throws<InvalidOperationException>(() => service.ScoreRoutes([candidate]));
    }

    [Fact]
    public void Scoring_rejects_candidate_sets_above_configured_bound()
    {
        var (service, _) = Create(options: new XCore9Options(MaxRoutesPerScoringRequest: 1));
        var candidates = new[]
        {
            new CandidateRoute("a", "reviewer", "safe", [new SanitizedRunFeature("success_rate", .8)]),
            new CandidateRoute("b", "reviewer", "safe", [new SanitizedRunFeature("success_rate", .7)])
        };

        Assert.Throws<InvalidOperationException>(() => service.ScoreRoutes(candidates));
    }

    [Fact]
    public void Scoring_rejects_duplicate_route_ids()
    {
        var (service, _) = Create();
        var candidates = new[]
        {
            new CandidateRoute("dup", "reviewer", "safe", [new SanitizedRunFeature("success_rate", .8)]),
            new CandidateRoute("dup", "reviewer", "safe", [new SanitizedRunFeature("route_accuracy", .7)])
        };

        Assert.Throws<InvalidOperationException>(() => service.ScoreRoutes(candidates));
    }

    [Fact]
    public void Feature_extraction_removes_unknown_non_finite_and_bounds_values()
    {
        var (service, _) = Create();
        var entry = new RunHistoryEntry(
            Guid.NewGuid(),
            "c",
            "reviewer",
            true,
            TimeSpan.Zero,
            0,
            [
                new SanitizedRunFeature("success_rate", 2),
                new SanitizedRunFeature("raw_prompt", 1),
                new SanitizedRunFeature("cost_ratio", double.NaN)
            ],
            []);

        var feature = Assert.Single(service.ExtractFeatures(entry));
        Assert.Equal("success_rate", feature.Name);
        Assert.Equal(1, feature.Value);
    }

    [Fact]
    public void Feature_extraction_deduplicates_allowed_features_before_bounding()
    {
        var (service, _) = Create(options: new XCore9Options(MaxFeaturesPerRun: 3));
        var entry = new RunHistoryEntry(
            Guid.NewGuid(),
            "corr-features",
            "reviewer",
            true,
            TimeSpan.Zero,
            0,
            [
                new SanitizedRunFeature("success_rate", 0.9),
                new SanitizedRunFeature("success_rate", 0.1),
                new SanitizedRunFeature("route_accuracy", 0.7)
            ],
            []);

        var features = service.ExtractFeatures(entry);
        Assert.Equal(2, features.Count);
        Assert.Equal("success_rate", features[0].Name);
        Assert.Equal("route_accuracy", features[1].Name);
    }

    [Fact]
    public async Task Run_history_rejects_feature_lists_above_configured_bound()
    {
        var (service, _) = Create(options: new XCore9Options(MaxFeaturesPerRun: 2));
        var entry = new RunHistoryEntry(
            Guid.NewGuid(),
            "corr-features-oversized",
            "reviewer",
            true,
            TimeSpan.FromMilliseconds(20),
            0,
            [
                new SanitizedRunFeature("success_rate", 0.9),
                new SanitizedRunFeature("route_accuracy", 0.8),
                new SanitizedRunFeature("dependency_health", 0.7)
            ],
            [new Uri("https://evidence.test/1")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IngestRunHistoryAsync(entry, "operator", default).AsTask());
    }

    [Fact]
    public async Task Run_history_rejects_evidence_links_above_configured_bounds()
    {
        var options = new XCore9Options(MaxEvidenceLinks: 2, MaxEvidenceLinkLength: 32);
        var (service, _) = Create(options: options);
        var oversizedCount = new RunHistoryEntry(
            Guid.NewGuid(),
            "corr-links-1",
            "reviewer",
            true,
            TimeSpan.FromMilliseconds(20),
            0,
            [new SanitizedRunFeature("success_rate", 0.8)],
            [
                new Uri("https://evidence.test/1"),
                new Uri("https://evidence.test/2"),
                new Uri("https://evidence.test/3")
            ]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.IngestRunHistoryAsync(oversizedCount, "operator", default).AsTask());

        var oversizedLength = oversizedCount with
        {
            CorrelationId = "corr-links-2",
            EvidenceLinks = [new Uri("https://evidence.test/path-that-exceeds-the-configured-boundary")]
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.IngestRunHistoryAsync(oversizedLength, "operator", default).AsTask());
    }

    [Fact]
    public async Task Run_history_rejects_non_catalog_templates()
    {
        var (service, _) = Create();
        var entry = new RunHistoryEntry(
            Guid.NewGuid(),
            "corr-11",
            "generated-template",
            true,
            TimeSpan.FromMilliseconds(300),
            0,
            [new SanitizedRunFeature("success_rate", 0.8)],
            []);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IngestRunHistoryAsync(entry, "operator", default).AsTask());
    }

    [Fact]
    public async Task Run_history_rolls_back_state_when_audit_fails()
    {
        var audit = new RecordingAuditSink(failEventType: "xcore9.run.ingested", failOnOccurrence: 1);
        var (service, _) = Create(options: new XCore9Options(MaxRunHistoryEntries: 1), audit: audit);
        var failing = new RunHistoryEntry(
            Guid.NewGuid(),
            "corr-history-1",
            "reviewer",
            true,
            TimeSpan.FromMilliseconds(10),
            0,
            [new SanitizedRunFeature("success_rate", 0.8)],
            [new Uri("https://evidence.test/1")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IngestRunHistoryAsync(failing, "operator", default).AsTask());
        Assert.Empty(HistorySnapshot(service));

        var succeeding = failing with
        {
            RunId = Guid.NewGuid(),
            CorrelationId = "corr-history-2"
        };

        await service.IngestRunHistoryAsync(succeeding, "operator", default);
        var history = HistorySnapshot(service);
        Assert.Single(history);
        Assert.Equal(succeeding.RunId, history[0].RunId);
    }

    [Fact]
    public async Task Run_history_is_idempotent_for_identical_run_ids_and_rejects_conflicts()
    {
        var (service, audit) = Create();
        var runId = Guid.NewGuid();
        var entry = new RunHistoryEntry(
            runId,
            "corr-history-idempotent",
            "reviewer",
            true,
            TimeSpan.FromMilliseconds(40),
            2.5m,
            [new SanitizedRunFeature("success_rate", 0.9)],
            [new Uri("https://evidence.test/idempotent")]);

        await service.IngestRunHistoryAsync(entry, "operator", default);
        await service.IngestRunHistoryAsync(entry, "operator", default);

        var history = HistorySnapshot(service);
        Assert.Single(history);
        Assert.Single(audit.Events, evt => evt.EventType == "xcore9.run.ingested");

        var conflicting = entry with { Succeeded = false };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IngestRunHistoryAsync(conflicting, "operator", default).AsTask());
    }

    [Fact]
    public void Toolchains_are_dependency_ordered_and_closed_over_approved_tools()
    {
        var (service, _) = Create();
        var ordered = service.ConstructToolchain("safe").OrderedToolIds;
        Assert.Equal(new[] { "read", "test" }, ordered);
    }

    [Fact]
    public void Retry_classification_rejects_negative_attempts()
    {
        var (service, _) = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => service.ClassifyRetry("timeout", -1));
    }

    [Fact]
    public async Task Negotiation_records_roll_back_when_audit_fails()
    {
        var audit = new RecordingAuditSink(failEventType: "xcore9.negotiation.recorded", failOnOccurrence: 1);
        var (service, _) = Create(options: new XCore9Options(MaxNegotiationEntries: 1), audit: audit);
        var failedRecord = new NegotiationRecord(
            Guid.NewGuid(),
            "corr-neg-1",
            "builder",
            "reviewer",
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "counter-offer",
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordNegotiationAsync(failedRecord, "operator", default).AsTask());
        Assert.Empty(NegotiationSnapshot(service));

        var succeededRecord = failedRecord with
        {
            NegotiationId = Guid.NewGuid(),
            CorrelationId = "corr-neg-2"
        };
        await service.RecordNegotiationAsync(succeededRecord, "operator", default);

        var negotiations = NegotiationSnapshot(service);
        Assert.Single(negotiations);
        Assert.Equal(succeededRecord.NegotiationId, negotiations[0].NegotiationId);
    }

    [Fact]
    public async Task Negotiation_rejects_non_digest_payloads()
    {
        var (service, _) = Create();
        var record = new NegotiationRecord(
            Guid.NewGuid(),
            "corr-neg-digest",
            "builder",
            "reviewer",
            "raw negotiation text",
            "counter-offer",
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordNegotiationAsync(record, "operator", default).AsTask());
    }

    [Fact]
    public async Task Promotion_requires_external_authority_and_preserves_rollback()
    {
        var (service, _) = Create();
        var candidate = new RoutingPolicy("candidate", 2, new Dictionary<string, string>(), null);
        var holdout = new HoldoutEvaluation(100, .4, .2, .9, true);
        var request = new PromotionRequest("corr-p1", candidate, holdout, "xcore-9", [new Uri("https://evidence.test/1")]);

        var self = await service.EvaluatePromotionAsync(request, "xcore-9", default);
        Assert.False(self.Approved);
        Assert.Equal("self-promotion-prohibited", self.ReasonCode);

        var approved = await service.EvaluatePromotionAsync(
            request with
            {
                CorrelationId = "corr-p2",
                RequestedBy = "guardian"
            },
            "guardian",
            default);

        Assert.True(approved.Approved);
        Assert.Equal("initial", approved.RollbackPolicy!.PolicyId);
    }

    [Fact]
    public async Task Promotion_rejects_non_finite_holdout_values()
    {
        var (service, _) = Create();
        var candidate = new RoutingPolicy("candidate", 2, new Dictionary<string, string>(), null);
        var holdout = new HoldoutEvaluation(100, double.NaN, .2, .9, true);

        var decision = await service.EvaluatePromotionAsync(
            new PromotionRequest("corr-p3", candidate, holdout, "guardian", [new Uri("https://evidence.test/1")]),
            "guardian",
            default);

        Assert.False(decision.Approved);
        Assert.Equal("holdout-values-not-finite", decision.ReasonCode);
        Assert.Equal("initial", decision.ActivePolicy.PolicyId);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public async Task Promotion_rejects_out_of_range_holdout_confidence(double confidence)
    {
        var (service, _) = Create();
        var candidate = new RoutingPolicy("candidate", 2, new Dictionary<string, string>(), null);
        var holdout = new HoldoutEvaluation(100, .4, .2, confidence, true);

        var decision = await service.EvaluatePromotionAsync(
            new PromotionRequest("corr-p3b", candidate, holdout, "guardian", [new Uri("https://evidence.test/1")]),
            "guardian",
            default);

        Assert.False(decision.Approved);
        Assert.Equal("holdout-values-not-finite", decision.ReasonCode);
        Assert.Equal("initial", decision.ActivePolicy.PolicyId);
    }

    [Fact]
    public async Task Promotion_uses_evidence_snapshot_after_validation()
    {
        var authorization = new GatedExternalAuthorityAuthorization();
        var (service, audit) = Create(authorization: authorization);
        var holdout = new HoldoutEvaluation(100, .4, .2, .9, true);
        var mutableLinks = new List<Uri> { new("https://evidence.test/original") };
        var request = new PromotionRequest(
            "corr-p3c",
            new RoutingPolicy("candidate", 2, new Dictionary<string, string>(), null),
            holdout,
            "guardian",
            mutableLinks);

        var decisionTask = service.EvaluatePromotionAsync(request, "guardian", default).AsTask();
        await authorization.ExternalAuthorityStarted;

        mutableLinks.Add(new Uri("https://evidence.test/mutated"));
        authorization.Release();

        var decision = await decisionTask;
        Assert.True(decision.Approved);
        var promotedEvent = Assert.Single(audit.Events, evt => evt.EventType == "xcore9.policy.promoted");
        var link = Assert.Single(promotedEvent.Links);
        Assert.Equal("https://evidence.test/original", link.Href.ToString());
    }

    [Fact]
    public async Task Promotion_is_idempotent_for_already_active_candidate()
    {
        var (service, _) = Create();
        var candidate = new RoutingPolicy("candidate", 2, new Dictionary<string, string> { ["route"] = "safe" }, null);
        var holdout = new HoldoutEvaluation(100, .4, .2, .9, true);

        var first = await service.EvaluatePromotionAsync(
            new PromotionRequest("corr-p4", candidate, holdout, "guardian", [new Uri("https://evidence.test/1")]),
            "guardian",
            default);
        Assert.True(first.Approved);

        var second = await service.EvaluatePromotionAsync(
            new PromotionRequest("corr-p5", candidate, holdout, "guardian", [new Uri("https://evidence.test/2")]),
            "guardian",
            default);

        Assert.True(second.Approved);
        Assert.Equal("already-active", second.ReasonCode);
        Assert.Equal("initial", second.RollbackPolicy!.PolicyId);
    }

    [Fact]
    public async Task Promotion_rejects_policy_identity_reuse_with_different_rules()
    {
        var (service, _) = Create();
        var holdout = new HoldoutEvaluation(100, .4, .2, .9, true);

        var promoted = await service.EvaluatePromotionAsync(
            new PromotionRequest(
                "corr-p5a",
                new RoutingPolicy("candidate", 2, new Dictionary<string, string> { ["route"] = "safe" }, null),
                holdout,
                "guardian",
                [new Uri("https://evidence.test/1")]),
            "guardian",
            default);
        Assert.True(promoted.Approved);

        var reusedIdentity = await service.EvaluatePromotionAsync(
            new PromotionRequest(
                "corr-p5b",
                new RoutingPolicy("candidate", 2, new Dictionary<string, string> { ["route"] = "unsafe" }, null),
                holdout,
                "guardian",
                [new Uri("https://evidence.test/2")]),
            "guardian",
            default);

        Assert.False(reusedIdentity.Approved);
        Assert.Equal("candidate-policy-identity-collision", reusedIdentity.ReasonCode);
        Assert.Equal("candidate", reusedIdentity.ActivePolicy.PolicyId);
        Assert.Equal(2, reusedIdentity.ActivePolicy.Version);
        Assert.Equal("initial", reusedIdentity.RollbackPolicy!.PolicyId);
    }

    [Fact]
    public async Task Promotion_rejects_version_regression_for_same_policy_id()
    {
        var (service, _) = Create();
        var holdout = new HoldoutEvaluation(100, .4, .2, .9, true);

        var promoted = await service.EvaluatePromotionAsync(
            new PromotionRequest(
                "corr-p5c",
                new RoutingPolicy("candidate", 5, new Dictionary<string, string> { ["route"] = "safe" }, null),
                holdout,
                "guardian",
                [new Uri("https://evidence.test/1")]),
            "guardian",
            default);
        Assert.True(promoted.Approved);

        var regressed = await service.EvaluatePromotionAsync(
            new PromotionRequest(
                "corr-p5d",
                new RoutingPolicy("candidate", 4, new Dictionary<string, string> { ["route"] = "safe" }, null),
                holdout,
                "guardian",
                [new Uri("https://evidence.test/2")]),
            "guardian",
            default);

        Assert.False(regressed.Approved);
        Assert.Equal("candidate-version-regression", regressed.ReasonCode);
        Assert.Equal("candidate", regressed.ActivePolicy.PolicyId);
        Assert.Equal(5, regressed.ActivePolicy.Version);
    }

    [Fact]
    public async Task Promotion_does_not_commit_state_when_audit_fails()
    {
        var audit = new RecordingAuditSink(failEventType: "xcore9.policy.promoted");
        var (service, _) = Create(audit: audit);
        var candidate = new RoutingPolicy("candidate", 2, new Dictionary<string, string>(), null);
        var holdout = new HoldoutEvaluation(100, .4, .2, .9, true);
        var request = new PromotionRequest("corr-p6", candidate, holdout, "guardian", [new Uri("https://evidence.test/1")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluatePromotionAsync(request, "guardian", default).AsTask());

        var blocked = await service.EvaluatePromotionAsync(
            request with
            {
                CorrelationId = "corr-p7",
                Holdout = holdout with { Passed = false }
            },
            "guardian",
            default);

        Assert.Equal("initial", blocked.ActivePolicy.PolicyId);
    }

    [Fact]
    public void Analytics_ranking_is_deterministic_and_prediction_metrics_are_computed()
    {
        IXCoreAnalytics analytics = new XCoreAnalytics();
        var routes = new[]
        {
            new CandidateRoute("b", "reviewer", "safe", [new SanitizedRunFeature("success_rate", .8)]),
            new CandidateRoute("a", "reviewer", "safe", [new SanitizedRunFeature("success_rate", .8)])
        };

        Assert.Equal(new[] { "a", "b" }, analytics.Rank(routes).Select(score => score.RouteId));

        var evaluation = analytics.EvaluatePredictions([1, 2], [1, 3], [.9, .2]);
        Assert.Equal(.5, evaluation.MeanAbsoluteError, 6);
    }

    [Fact]
    public void Analytics_surface_non_finite_inputs_as_errors_or_anomalies()
    {
        IXCoreAnalytics analytics = new XCoreAnalytics();

        var anomalies = analytics.DetectAnomalies([double.NaN, 0, 0], 2);
        Assert.Contains(0, anomalies);

        Assert.Throws<ArgumentException>(() => analytics.DetectAnomalies([0, 1], double.NaN));

        Assert.Throws<ArgumentException>(() => analytics.EvaluatePredictions(
            [double.NaN, 1],
            [0, 1],
            [.8, .9]));

        Assert.Throws<ArgumentException>(() => analytics.EvaluatePredictions(
            [double.MaxValue],
            [-double.MaxValue],
            [.9]));

        var extreme = analytics.DetectAnomalies([0.0, double.MaxValue], 1.0);
        Assert.Equal(new[] { 0, 1 }, extreme);

        var finiteLarge = analytics.EvaluatePredictions([1e200], [0.0], [.9]);
        Assert.True(double.IsFinite(finiteLarge.RootMeanSquaredError));
    }

    [Fact]
    public void Analytics_penalizes_higher_retry_cost_and_duration_features()
    {
        IXCoreAnalytics analytics = new XCoreAnalytics();
        var routes = new[]
        {
            new CandidateRoute(
                "stable",
                "reviewer",
                "safe",
                [
                    new SanitizedRunFeature("success_rate", 0.8),
                    new SanitizedRunFeature("retry_rate", 0.1),
                    new SanitizedRunFeature("cost_ratio", 0.1),
                    new SanitizedRunFeature("duration_ratio", 0.2)
                ]),
            new CandidateRoute(
                "unstable",
                "reviewer",
                "safe",
                [
                    new SanitizedRunFeature("success_rate", 0.8),
                    new SanitizedRunFeature("retry_rate", 0.9),
                    new SanitizedRunFeature("cost_ratio", 0.9),
                    new SanitizedRunFeature("duration_ratio", 0.8)
                ])
        };

        var ranked = analytics.Rank(routes);
        Assert.Equal("stable", ranked[0].RouteId);
        Assert.True(ranked[0].Score > ranked[1].Score);
    }

    [Fact]
    public void Constructor_rejects_non_positive_leases_and_bounds()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(options: new XCore9Options(LeaseDuration: TimeSpan.Zero)));

        Assert.Throws<ArgumentException>(() =>
            Create(options: new XCore9Options(LeaseDuration: TimeSpan.MaxValue)));

        Assert.Throws<ArgumentException>(() =>
            Create(options: new XCore9Options(AuditEnvironment: "qa")));

        Assert.Throws<ArgumentException>(() =>
            Create(options: new XCore9Options(MaxEvidenceLinks: 0)));

        Assert.Throws<ArgumentException>(() =>
            Create(options: new XCore9Options(MaxEvidenceLinkLength: 0)));
    }

    private static (XCore9Service Service, RecordingAuditSink Audit) Create(
        XCore9Options? options = null,
        IXCoreAuthorization? authorization = null,
        RecordingAuditSink? audit = null,
        WorkerTemplate? template = null)
    {
        audit ??= new RecordingAuditSink();
        authorization ??= new CapabilityAuthorization();
        template ??= new WorkerTemplate(
            TemplateId: "reviewer",
            MaxInstances: 1,
            CpuUnits: 2,
            MemoryMiB: 512,
            AllowedToolchainIds: new HashSet<string> { "safe" },
            PromptDigest: "sha256:fixed");

        var service = new XCore9Service(
            analytics: new XCoreAnalytics(),
            authorization: authorization,
            audit: audit,
            templates:
            [
                template
            ],
            toolchains:
            [
                new ToolchainDefinition("safe", new HashSet<string> { "read", "test" }),
                new ToolchainDefinition("risky", new HashSet<string> { "read", "unsafe" })
            ],
            tools:
            [
                new ToolDefinition("read", new HashSet<string>()),
                new ToolDefinition("test", new HashSet<string> { "read" }),
                new ToolDefinition("unsafe", new HashSet<string> { "read" })
            ],
            initialPolicy: new RoutingPolicy("initial", 1, new Dictionary<string, string>(), null),
            options: options);

        return (service, audit);
    }

    private static IReadOnlyList<RunHistoryEntry> HistorySnapshot(XCore9Service service)
    {
        var historyField = typeof(XCore9Service).GetField("_history", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(historyField);
        var history = Assert.IsType<List<RunHistoryEntry>>(historyField!.GetValue(service));
        return history.ToArray();
    }

    private static IReadOnlyList<NegotiationRecord> NegotiationSnapshot(XCore9Service service)
    {
        var negotiationsField = typeof(XCore9Service).GetField("_negotiations", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(negotiationsField);
        var negotiations = Assert.IsType<List<NegotiationRecord>>(negotiationsField!.GetValue(service));
        return negotiations.ToArray();
    }

    private sealed class CapabilityAuthorization : IXCoreAuthorization
    {
        public ValueTask<bool> AuthorizeAsync(string actor, string capability, CancellationToken cancellationToken)
        {
            if (string.Equals(capability, "policy.promote.external-authority", StringComparison.Ordinal) &&
                actor.StartsWith("xcore", StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.FromResult(false);
            }

            return ValueTask.FromResult(true);
        }
    }

    private sealed class GatedExternalAuthorityAuthorization : IXCoreAuthorization
    {
        private readonly TaskCompletionSource<bool> _externalAuthorityStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ExternalAuthorityStarted => _externalAuthorityStarted.Task;

        public void Release() => _release.TrySetResult(true);

        public async ValueTask<bool> AuthorizeAsync(string actor, string capability, CancellationToken cancellationToken)
        {
            if (string.Equals(capability, "policy.promote.external-authority", StringComparison.Ordinal))
            {
                _externalAuthorityStarted.TrySetResult(true);
                await _release.Task.WaitAsync(cancellationToken);
            }

            return true;
        }
    }

    private sealed class RecordingAuditSink(string? failEventType = null, int? failOnOccurrence = null) : IXCoreAuditSink
    {
        private int _matchingWrites;

        public List<XCoreAuditEvent> Events { get; } = [];

        public ValueTask WriteAsync(XCoreAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (string.Equals(failEventType, auditEvent.EventType, StringComparison.Ordinal))
            {
                _matchingWrites++;
                if (failOnOccurrence is null || _matchingWrites == failOnOccurrence.Value)
                {
                    throw new InvalidOperationException("audit-sink-failure");
                }
            }

            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
