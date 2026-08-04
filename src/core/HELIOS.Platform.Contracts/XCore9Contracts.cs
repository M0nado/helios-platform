namespace HELIOS.Platform.Contracts.XCore9;

public sealed record SanitizedRunFeature(string Name, double Value);
public sealed record RunHistoryEntry(Guid RunId, string CorrelationId, string TemplateId, bool Succeeded, TimeSpan Duration, decimal Cost, IReadOnlyList<SanitizedRunFeature> Features, IReadOnlyList<Uri> EvidenceLinks);
public sealed record CandidateRoute(string RouteId, string WorkerTemplateId, string ToolchainId, IReadOnlyList<SanitizedRunFeature> Features);
public sealed record RouteScore(string RouteId, double Score, double Confidence, bool IsAnomalous, IReadOnlyDictionary<string, double> Diagnostics);
public sealed record WorkerTemplate(string TemplateId, int MaxInstances, int CpuUnits, int MemoryMiB, IReadOnlySet<string> AllowedToolchainIds, string PromptDigest);
public sealed record WorkerLease(Guid LeaseId, string TemplateId, string CorrelationId, DateTimeOffset ExpiresAt);
public sealed record ToolDefinition(string ToolId, IReadOnlySet<string> Dependencies);
public sealed record ToolchainDefinition(string ToolchainId, IReadOnlySet<string> ToolIds);
public sealed record ConstructedToolchain(string ToolchainId, IReadOnlyList<string> OrderedToolIds);
public enum RetryDisposition { DoNotRetry, RetryWithBackoff, RetryAfterDependencyRecovery, RequiresReview }
public sealed record RetryClassification(RetryDisposition Disposition, TimeSpan? Delay, string ReasonCode);
public sealed record NegotiationRecord(Guid NegotiationId, string CorrelationId, string Proposer, string Counterparty, string ProposalDigest, string Outcome, DateTimeOffset RecordedAt);
public sealed record HoldoutEvaluation(int SampleCount, double BaselineLoss, double CandidateLoss, double Confidence, bool Passed);
public sealed record RoutingPolicy(string PolicyId, int Version, IReadOnlyDictionary<string, string> Rules, string? PreviousPolicyId);
public sealed record PromotionRequest(RoutingPolicy Candidate, HoldoutEvaluation Holdout, string RequestedBy, IReadOnlyList<Uri> EvidenceLinks);
public sealed record PolicyDecision(bool Approved, string ReasonCode, RoutingPolicy ActivePolicy, RoutingPolicy? RollbackPolicy);
public sealed record PredictionEvaluation(double MeanAbsoluteError, double RootMeanSquaredError, double CalibrationError);

public interface IXCoreAnalytics
{
    IReadOnlyList<RouteScore> Rank(IReadOnlyList<CandidateRoute> candidates);
    double CalibrateConfidence(double rawConfidence, int sampleCount);
    IReadOnlyList<int> DetectAnomalies(IReadOnlyList<double> values, double threshold);
    PredictionEvaluation EvaluatePredictions(IReadOnlyList<double> predicted, IReadOnlyList<double> actual, IReadOnlyList<double> confidence);
}

public interface IXCoreAuthorization { ValueTask<bool> AuthorizeAsync(string actor, string capability, CancellationToken cancellationToken); }
public interface IXCoreAuditSink { ValueTask WriteAsync(string correlationId, string eventType, string actor, IReadOnlyList<Uri> evidenceLinks, CancellationToken cancellationToken); }
public interface IXCore9Service
{
    ValueTask IngestRunHistoryAsync(RunHistoryEntry entry, string actor, CancellationToken cancellationToken);
    IReadOnlyList<SanitizedRunFeature> ExtractFeatures(RunHistoryEntry entry);
    IReadOnlyList<RouteScore> ScoreRoutes(IReadOnlyList<CandidateRoute> candidates);
    ValueTask<WorkerLease> SelectWorkerAsync(string templateId, string toolchainId, string correlationId, string actor, CancellationToken cancellationToken);
    ConstructedToolchain ConstructToolchain(string toolchainId);
    RetryClassification ClassifyRetry(string failureCode, int attempt);
    ValueTask RecordNegotiationAsync(NegotiationRecord record, string actor, CancellationToken cancellationToken);
    ValueTask<PolicyDecision> EvaluatePromotionAsync(PromotionRequest request, string actor, CancellationToken cancellationToken);
    void ReleaseWorker(WorkerLease lease);
}
