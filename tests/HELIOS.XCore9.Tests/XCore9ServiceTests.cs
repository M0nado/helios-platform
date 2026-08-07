using HELIOS.Analytics.FSharp;
using HELIOS.Platform.Contracts.XCore9;
using HELIOS.XCore9;
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
            catch (Exception ex)
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
    public void Toolchains_are_dependency_ordered_and_closed_over_approved_tools()
    {
        var (service, _) = Create();
        var ordered = service.ConstructToolchain("safe").OrderedToolIds;
        Assert.Equal(new[] { "read", "test" }, ordered);
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
            Create(options: new XCore9Options(AuditEnvironment: "qa")));
    }

    private static (XCore9Service Service, RecordingAuditSink Audit) Create(
        XCore9Options? options = null,
        IXCoreAuthorization? authorization = null,
        RecordingAuditSink? audit = null)
    {
        audit ??= new RecordingAuditSink();
        authorization ??= new CapabilityAuthorization();

        var service = new XCore9Service(
            analytics: new XCoreAnalytics(),
            authorization: authorization,
            audit: audit,
            templates:
            [
                new WorkerTemplate(
                    TemplateId: "reviewer",
                    MaxInstances: 1,
                    CpuUnits: 2,
                    MemoryMiB: 512,
                    AllowedToolchainIds: new HashSet<string> { "safe" },
                    PromptDigest: "sha256:fixed")
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

    private sealed class RecordingAuditSink(string? failEventType = null) : IXCoreAuditSink
    {
        public List<XCoreAuditEvent> Events { get; } = [];

        public ValueTask WriteAsync(XCoreAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (string.Equals(failEventType, auditEvent.EventType, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("audit-sink-failure");
            }

            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
