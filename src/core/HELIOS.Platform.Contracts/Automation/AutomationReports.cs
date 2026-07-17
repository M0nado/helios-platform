namespace HELIOS.Platform.Contracts.Automation;

public sealed record FinalGateResult(
    string SchemaVersion,
    string Status,
    bool MergeReady,
    FinalGateBlocker? FirstBlocker,
    IReadOnlyList<FinalGateStep> Results);

public sealed record FinalGateBlocker(
    string Id,
    string Domain,
    string Language,
    string OwnerAgent,
    string Command,
    string RecommendedFixer);

public sealed record FinalGateStep(
    string Id,
    string Domain,
    string Language,
    string OwnerAgent,
    string Status,
    string Command);

public sealed record MergeCandidateDecision(
    string Branch,
    int Score,
    IReadOnlyDictionary<string, int> LanguageImpact,
    IReadOnlyList<string> RequiredChecks,
    string RecommendedParty,
    bool MergeBlocked);

public sealed record LearningEvent(
    DateTimeOffset Utc,
    string Agent,
    string Task,
    string Language,
    string Model,
    string Provider,
    bool Success,
    double Cost,
    int RuntimeMilliseconds);
