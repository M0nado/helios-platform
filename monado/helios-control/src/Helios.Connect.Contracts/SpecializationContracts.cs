namespace Helios.Connect.Contracts;

public sealed record SpecializationRegistryDocument(
    string SchemaVersion,
    string RegistryVersion,
    SpecializationParallelPolicy ParallelPolicy,
    IReadOnlyList<SpecializationSkillContract> Skills,
    IReadOnlyList<SpecializationPackContract> Packs,
    MultimodalRoutingContract MultimodalRouting);

public sealed record SpecializationParallelPolicy(
    int MaxGlobalParallelism,
    int MaxFanOut,
    int MaxFanIn,
    int DefaultTimeoutSeconds,
    int MaxTimeoutSeconds,
    bool RequireIdempotencyKey,
    bool RequireCorrelationId,
    bool CancelRunningChildrenOnTimeout);

public sealed record SpecializationSkillContract(
    string Id,
    string ContractVersion,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> DeniedTools,
    bool RequiresCorrelationId,
    bool RequiresEvidenceLinks);

public sealed record SpecializationPackContract(
    string Id,
    string Role,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> DeniedTools,
    int MaxParallelism,
    int MaxFanOut,
    int MaxFanIn,
    int TimeoutSeconds,
    string TimeoutBehavior,
    string PartialFailurePolicy,
    IReadOnlyList<string> BoundSkills,
    IReadOnlyList<string> Modalities);

public sealed record MultimodalRoutingContract(
    IReadOnlyList<string> RequiredProvenanceFields,
    IReadOnlyList<MultimodalLaneContract> Lanes);

public sealed record MultimodalLaneContract(
    string Id,
    IReadOnlyList<string> AllowedContentTypes,
    bool RequiresEvidenceMetadata);

public sealed record SpecializationExecutionRequest(
    string SpecializationId,
    int RequestedParallelism = 0,
    int RequestedFanOut = 0,
    int RequestedFanIn = 0,
    int TimeoutSeconds = 0,
    string? IdempotencyKey = null,
    string? CorrelationId = null,
    IReadOnlyList<string>? RequestedTools = null,
    IReadOnlyList<string>? RequestedSkills = null,
    IReadOnlyList<string>? RequestedModalities = null,
    IReadOnlyList<EvidenceLink>? EvidenceLinks = null);

public sealed record EvidenceLink(
    string Rel,
    string Href);

public sealed record SpecializationExecutionDecision(
    bool Allowed,
    string SchemaVersion,
    string RegistryVersion,
    SpecializationDecisionPolicy? Policy,
    IReadOnlyList<SpecializationViolation> Violations,
    IReadOnlyList<MultimodalEvidenceMetadata> EvidenceMetadata);

public sealed record SpecializationDecisionPolicy(
    string SpecializationId,
    int EffectiveParallelism,
    int EffectiveFanOut,
    int EffectiveFanIn,
    int EffectiveTimeoutSeconds,
    string TimeoutBehavior,
    string PartialFailurePolicy,
    bool CancelRunningChildrenOnTimeout,
    IReadOnlyList<string> EnforcedSkills,
    IReadOnlyList<string> EnforcedTools);

public sealed record SpecializationViolation(
    string Code,
    string Message);

public sealed record MultimodalEvidenceMetadata(
    string Modality,
    IReadOnlyList<string> AllowedContentTypes,
    string CorrelationId,
    string IdempotencyKey,
    string SpecializationId,
    IReadOnlyList<string> SkillIds,
    IReadOnlyList<EvidenceLink> EvidenceLinks,
    IReadOnlyDictionary<string, string> Provenance);
