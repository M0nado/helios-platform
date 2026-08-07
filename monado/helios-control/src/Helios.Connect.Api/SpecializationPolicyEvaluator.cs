using System.Text.Json;
using Helios.Connect.Contracts;

namespace Helios.Connect.Api;

public interface ISpecializationPolicyEvaluator
{
    SpecializationRegistryDocument Registry { get; }
    SpecializationExecutionDecision Evaluate(SpecializationExecutionRequest request);
}

public sealed class SpecializationPolicyEvaluator : ISpecializationPolicyEvaluator
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private static readonly HashSet<string> AllowedTimeoutBehaviors = new(Comparer)
    {
        "cancel-running-children",
        "complete-active-children"
    };

    private static readonly HashSet<string> AllowedFailurePolicies = new(Comparer)
    {
        "fail-fast",
        "continue-and-report",
        "continue-and-compensate"
    };

    private readonly Dictionary<string, SpecializationSkillContract> _skills;
    private readonly Dictionary<string, SpecializationPackContract> _packs;
    private readonly Dictionary<string, MultimodalLaneContract> _lanes;

    public SpecializationRegistryDocument Registry { get; }

    public SpecializationPolicyEvaluator(SpecializationRegistryDocument registry)
    {
        Registry = ValidateRegistry(registry);
        _skills = Registry.Skills.ToDictionary(skill => skill.Id, Comparer);
        _packs = Registry.Packs.ToDictionary(pack => pack.Id, Comparer);
        _lanes = Registry.MultimodalRouting.Lanes.ToDictionary(lane => lane.Id, Comparer);
    }

    public static ISpecializationPolicyEvaluator CreateFromConfiguration(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var path = ResolveRegistryPath(configuration, environment);
        if (!File.Exists(path))
            throw new InvalidOperationException($"Specialization registry file '{path}' was not found.");

        try
        {
            var json = File.ReadAllText(path);
            var registry = JsonSerializer.Deserialize<SpecializationRegistryDocument>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (registry is null)
                throw new InvalidOperationException($"Specialization registry file '{path}' is empty or invalid.");
            return new SpecializationPolicyEvaluator(registry);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Specialization registry file '{path}' is not valid JSON.", exception);
        }
    }

    private static string ResolveRegistryPath(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["HELIOS_SPECIALIZATION_CONFIG_PATH"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        }

        var relativePath = Path.Combine("config", "hermes-xcore9-specialization-packs.json");
        var candidates = new[]
        {
            Path.Combine(environment.ContentRootPath, relativePath),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", relativePath)),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "..", relativePath))
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    public SpecializationExecutionDecision Evaluate(SpecializationExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = new List<SpecializationViolation>();
        if (string.IsNullOrWhiteSpace(request.SpecializationId))
            return DeniedDecision(
                Registry.SchemaVersion,
                Registry.RegistryVersion,
                null,
                new("missing-specialization-id", "specializationId is required."));

        if (!_packs.TryGetValue(request.SpecializationId.Trim(), out var pack))
        {
            return DeniedDecision(
                Registry.SchemaVersion,
                Registry.RegistryVersion,
                null,
                new("unknown-specialization", $"Specialization '{request.SpecializationId}' is not defined in the registry."));
        }

        var packAllowedTools = ToSet(pack.AllowedTools, "pack allowedTools");
        var packDeniedTools = ToSet(pack.DeniedTools, "pack deniedTools");
        var packSkills = ToSet(pack.BoundSkills, "pack boundSkills");
        var packModalities = ToSet(pack.Modalities, "pack modalities");

        var selectedSkillIds = request.RequestedSkills is { Count: > 0 }
            ? NormalizeRequestValues(request.RequestedSkills)
            : Normalize(pack.BoundSkills, "pack bound skill");
        var selectedSkills = new List<SpecializationSkillContract>();
        foreach (var skillId in selectedSkillIds)
        {
            if (!packSkills.Contains(skillId))
            {
                violations.Add(new("skill-not-bound",
                    $"Skill '{skillId}' is not bound to specialization '{pack.Id}'."));
                continue;
            }

            if (!_skills.TryGetValue(skillId, out var skill))
            {
                violations.Add(new("unknown-skill-contract",
                    $"Skill '{skillId}' is bound by specialization '{pack.Id}' but no contract was found."));
                continue;
            }
            selectedSkills.Add(skill);
        }

        if (selectedSkills.Count == 0)
            violations.Add(new("missing-skill-binding", "At least one bound skill contract is required."));

        var effectiveParallelism = request.RequestedParallelism > 0 ? request.RequestedParallelism : 1;
        if (effectiveParallelism > Registry.ParallelPolicy.MaxGlobalParallelism)
            violations.Add(new("global-parallelism-exceeded",
                $"Requested parallelism {effectiveParallelism} exceeds global limit {Registry.ParallelPolicy.MaxGlobalParallelism}."));
        if (effectiveParallelism > pack.MaxParallelism)
            violations.Add(new("pack-parallelism-exceeded",
                $"Requested parallelism {effectiveParallelism} exceeds specialization limit {pack.MaxParallelism}."));

        var defaultFanOut = Math.Min(pack.MaxFanOut, Registry.ParallelPolicy.MaxFanOut);
        var effectiveFanOut = request.RequestedFanOut > 0 ? request.RequestedFanOut : defaultFanOut;
        if (effectiveFanOut > Registry.ParallelPolicy.MaxFanOut)
            violations.Add(new("global-fanout-exceeded",
                $"Requested fan-out {effectiveFanOut} exceeds global limit {Registry.ParallelPolicy.MaxFanOut}."));
        if (effectiveFanOut > pack.MaxFanOut)
            violations.Add(new("pack-fanout-exceeded",
                $"Requested fan-out {effectiveFanOut} exceeds specialization limit {pack.MaxFanOut}."));

        var defaultFanIn = Math.Min(pack.MaxFanIn, Registry.ParallelPolicy.MaxFanIn);
        var effectiveFanIn = request.RequestedFanIn > 0 ? request.RequestedFanIn : defaultFanIn;
        if (effectiveFanIn > Registry.ParallelPolicy.MaxFanIn)
            violations.Add(new("global-fanin-exceeded",
                $"Requested fan-in {effectiveFanIn} exceeds global limit {Registry.ParallelPolicy.MaxFanIn}."));
        if (effectiveFanIn > pack.MaxFanIn)
            violations.Add(new("pack-fanin-exceeded",
                $"Requested fan-in {effectiveFanIn} exceeds specialization limit {pack.MaxFanIn}."));

        var defaultTimeout = Math.Min(pack.TimeoutSeconds, Registry.ParallelPolicy.DefaultTimeoutSeconds);
        var effectiveTimeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : defaultTimeout;
        if (effectiveTimeoutSeconds <= 0)
            violations.Add(new("invalid-timeout", "timeoutSeconds must be greater than zero."));
        if (effectiveTimeoutSeconds > Registry.ParallelPolicy.MaxTimeoutSeconds)
            violations.Add(new("global-timeout-exceeded",
                $"timeoutSeconds {effectiveTimeoutSeconds} exceeds global limit {Registry.ParallelPolicy.MaxTimeoutSeconds}."));
        if (effectiveTimeoutSeconds > pack.TimeoutSeconds)
            violations.Add(new("pack-timeout-exceeded",
                $"timeoutSeconds {effectiveTimeoutSeconds} exceeds specialization limit {pack.TimeoutSeconds}."));

        var idempotencyKey = request.IdempotencyKey?.Trim() ?? string.Empty;
        if (Registry.ParallelPolicy.RequireIdempotencyKey && !IsSafeIdempotencyKey(idempotencyKey))
            violations.Add(new("invalid-idempotency-key",
                "A safe idempotencyKey (8-128 alphanumeric plus . _ : -) is required."));

        var correlationId = request.CorrelationId?.Trim() ?? string.Empty;
        var requiresCorrelationId = Registry.ParallelPolicy.RequireCorrelationId ||
            selectedSkills.Any(skill => skill.RequiresCorrelationId);
        if (requiresCorrelationId && !IsSafeCorrelationId(correlationId))
            violations.Add(new("invalid-correlation-id",
                "A safe correlationId (4-128 printable non-control characters) is required."));

        var selectedModalities = request.RequestedModalities is { Count: > 0 }
            ? NormalizeRequestValues(request.RequestedModalities)
            : Normalize(pack.Modalities, "pack modality");
        var activeLanes = new List<MultimodalLaneContract>();
        foreach (var modality in selectedModalities)
        {
            if (!packModalities.Contains(modality))
            {
                violations.Add(new("modality-not-allowed",
                    $"Modality '{modality}' is not allowed by specialization '{pack.Id}'."));
                continue;
            }

            if (!_lanes.TryGetValue(modality, out var lane))
            {
                violations.Add(new("unknown-modality",
                    $"Modality '{modality}' is not declared in multimodal routing lanes."));
                continue;
            }
            activeLanes.Add(lane);
        }

        var normalizedTools = request.RequestedTools is { Count: > 0 }
            ? NormalizeRequestValues(request.RequestedTools)
            : BuildDefaultToolSet(packAllowedTools, packDeniedTools, selectedSkills);
        if (normalizedTools.Count == 0)
            violations.Add(new("missing-tools",
                $"Specialization '{pack.Id}' has no executable tool set after capability filtering."));

        foreach (var tool in normalizedTools)
        {
            if (packDeniedTools.Contains(tool))
            {
                violations.Add(new("tool-denied",
                    $"Tool '{tool}' is explicitly denied by specialization '{pack.Id}'."));
                continue;
            }
            if (!packAllowedTools.Contains(tool))
            {
                violations.Add(new("tool-not-allowed",
                    $"Tool '{tool}' is not declared in specialization '{pack.Id}' allowedTools."));
                continue;
            }

            foreach (var skill in selectedSkills)
            {
                var skillAllowed = ToSet(skill.AllowedTools, $"skill {skill.Id} allowedTools");
                var skillDenied = ToSet(skill.DeniedTools, $"skill {skill.Id} deniedTools");
                if (skillDenied.Contains(tool))
                {
                    violations.Add(new("tool-denied-by-skill",
                        $"Tool '{tool}' is denied by skill contract '{skill.Id}'."));
                    continue;
                }
                if (!skillAllowed.Contains(tool))
                {
                    violations.Add(new("tool-not-declared-by-skill",
                        $"Tool '{tool}' is not declared by skill contract '{skill.Id}'."));
                }
            }
        }

        var evidenceLinks = NormalizeEvidenceLinks(request.EvidenceLinks, violations);
        var requiresEvidenceLinks = selectedSkills.Any(skill => skill.RequiresEvidenceLinks) ||
            activeLanes.Any(lane => lane.RequiresEvidenceMetadata);
        if (requiresEvidenceLinks && evidenceLinks.Count == 0)
        {
            violations.Add(new("missing-evidence-links",
                "At least one evidence link is required by the selected skills/modalities."));
        }

        var policy = new SpecializationDecisionPolicy(
            pack.Id,
            effectiveParallelism,
            effectiveFanOut,
            effectiveFanIn,
            effectiveTimeoutSeconds,
            pack.TimeoutBehavior,
            pack.PartialFailurePolicy,
            Registry.ParallelPolicy.CancelRunningChildrenOnTimeout ||
                string.Equals(pack.TimeoutBehavior, "cancel-running-children", StringComparison.OrdinalIgnoreCase),
            selectedSkills.Select(skill => skill.Id).OrderBy(value => value, Comparer).ToArray(),
            normalizedTools.OrderBy(value => value, Comparer).ToArray());

        if (violations.Count > 0)
            return new SpecializationExecutionDecision(
                Allowed: false,
                SchemaVersion: Registry.SchemaVersion,
                RegistryVersion: Registry.RegistryVersion,
                Policy: policy,
                Violations: violations,
                EvidenceMetadata: []);

        var metadata = activeLanes
            .OrderBy(lane => lane.Id, Comparer)
            .Select(lane => new MultimodalEvidenceMetadata(
                Modality: lane.Id,
                AllowedContentTypes: Normalize(lane.AllowedContentTypes, $"lane {lane.Id} allowedContentTypes"),
                CorrelationId: correlationId,
                IdempotencyKey: idempotencyKey,
                SpecializationId: pack.Id,
                SkillIds: selectedSkills.Select(skill => skill.Id).OrderBy(value => value, Comparer).ToArray(),
                EvidenceLinks: evidenceLinks,
                Provenance: BuildProvenance(
                    Registry.MultimodalRouting.RequiredProvenanceFields,
                    lane.Id,
                    correlationId,
                    idempotencyKey,
                    pack.Id,
                    selectedSkills.Select(skill => skill.Id),
                    evidenceLinks)))
            .ToArray();

        return new SpecializationExecutionDecision(
            Allowed: true,
            SchemaVersion: Registry.SchemaVersion,
            RegistryVersion: Registry.RegistryVersion,
            Policy: policy,
            Violations: [],
            EvidenceMetadata: metadata);
    }

    private static SpecializationExecutionDecision DeniedDecision(
        string schemaVersion,
        string registryVersion,
        SpecializationDecisionPolicy? policy,
        SpecializationViolation violation) =>
        new(
            Allowed: false,
            SchemaVersion: schemaVersion,
            RegistryVersion: registryVersion,
            Policy: policy,
            Violations: [violation],
            EvidenceMetadata: []);

    private static SpecializationRegistryDocument ValidateRegistry(SpecializationRegistryDocument registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (string.IsNullOrWhiteSpace(registry.SchemaVersion))
            throw new InvalidOperationException("Specialization registry schemaVersion is required.");
        if (!string.Equals(registry.SchemaVersion, "1.0", StringComparison.Ordinal))
            throw new InvalidOperationException("Specialization registry schemaVersion must be 1.0.");
        if (string.IsNullOrWhiteSpace(registry.RegistryVersion))
            throw new InvalidOperationException("Specialization registry registryVersion is required.");
        if (registry.ParallelPolicy is null)
            throw new InvalidOperationException("Specialization registry parallelPolicy is required.");
        if (registry.ParallelPolicy.MaxGlobalParallelism <= 0)
            throw new InvalidOperationException("parallelPolicy.maxGlobalParallelism must be positive.");
        if (registry.ParallelPolicy.MaxFanOut <= 0 || registry.ParallelPolicy.MaxFanIn <= 0)
            throw new InvalidOperationException("parallelPolicy fan-out/fan-in limits must be positive.");
        if (registry.ParallelPolicy.DefaultTimeoutSeconds <= 0 ||
            registry.ParallelPolicy.MaxTimeoutSeconds < registry.ParallelPolicy.DefaultTimeoutSeconds)
            throw new InvalidOperationException("parallelPolicy timeout limits are invalid.");
        if (registry.Skills is null || registry.Skills.Count == 0)
            throw new InvalidOperationException("Specialization registry skills cannot be empty.");
        if (registry.Packs is null || registry.Packs.Count == 0)
            throw new InvalidOperationException("Specialization registry packs cannot be empty.");
        if (registry.MultimodalRouting is null)
            throw new InvalidOperationException("Specialization registry multimodalRouting is required.");
        if (registry.MultimodalRouting.Lanes is null || registry.MultimodalRouting.Lanes.Count == 0)
            throw new InvalidOperationException("multimodalRouting.lanes cannot be empty.");

        var laneIds = new HashSet<string>(Comparer);
        foreach (var lane in registry.MultimodalRouting.Lanes)
        {
            if (string.IsNullOrWhiteSpace(lane.Id))
                throw new InvalidOperationException("Every multimodal lane must define id.");
            if (!laneIds.Add(lane.Id.Trim()))
                throw new InvalidOperationException($"Duplicate multimodal lane '{lane.Id}'.");
            var laneContentTypes = ToSet(lane.AllowedContentTypes, $"lane {lane.Id} allowedContentTypes");
            if (laneContentTypes.Count == 0)
                throw new InvalidOperationException($"lane {lane.Id} must declare at least one content type.");
        }

        var requiredProvenanceFields = Normalize(
            registry.MultimodalRouting.RequiredProvenanceFields,
            "multimodalRouting requiredProvenanceFields");
        foreach (var required in new[] { "correlationId", "idempotencyKey", "specializationId", "modality", "evidenceLinks" })
        {
            if (!requiredProvenanceFields.Contains(required, Comparer))
            {
                throw new InvalidOperationException(
                    $"multimodalRouting.requiredProvenanceFields must include '{required}'.");
            }
        }

        var skillIds = new HashSet<string>(Comparer);
        foreach (var skill in registry.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Id))
                throw new InvalidOperationException("Every skill contract must define id.");
            if (!skillIds.Add(skill.Id.Trim()))
                throw new InvalidOperationException($"Duplicate skill contract '{skill.Id}'.");
            var allowed = ToSet(skill.AllowedTools, $"skill {skill.Id} allowedTools");
            var denied = ToSet(skill.DeniedTools, $"skill {skill.Id} deniedTools");
            if (allowed.Count == 0)
                throw new InvalidOperationException($"skill {skill.Id} must declare at least one allowed tool.");
            if (allowed.Overlaps(denied))
                throw new InvalidOperationException($"skill {skill.Id} allows and denies the same tool.");
        }

        var packIds = new HashSet<string>(Comparer);
        foreach (var pack in registry.Packs)
        {
            if (string.IsNullOrWhiteSpace(pack.Id))
                throw new InvalidOperationException("Every specialization pack must define id.");
            if (!packIds.Add(pack.Id.Trim()))
                throw new InvalidOperationException($"Duplicate specialization pack '{pack.Id}'.");
            if (pack.MaxParallelism <= 0 || pack.MaxParallelism > registry.ParallelPolicy.MaxGlobalParallelism)
                throw new InvalidOperationException($"specialization {pack.Id} maxParallelism is out of bounds.");
            if (pack.MaxFanOut <= 0 || pack.MaxFanOut > registry.ParallelPolicy.MaxFanOut)
                throw new InvalidOperationException($"specialization {pack.Id} maxFanOut is out of bounds.");
            if (pack.MaxFanIn <= 0 || pack.MaxFanIn > registry.ParallelPolicy.MaxFanIn)
                throw new InvalidOperationException($"specialization {pack.Id} maxFanIn is out of bounds.");
            if (pack.TimeoutSeconds <= 0 || pack.TimeoutSeconds > registry.ParallelPolicy.MaxTimeoutSeconds)
                throw new InvalidOperationException($"specialization {pack.Id} timeoutSeconds is out of bounds.");
            if (!AllowedTimeoutBehaviors.Contains(pack.TimeoutBehavior))
                throw new InvalidOperationException($"specialization {pack.Id} timeoutBehavior is not allowed.");
            if (!AllowedFailurePolicies.Contains(pack.PartialFailurePolicy))
                throw new InvalidOperationException($"specialization {pack.Id} partialFailurePolicy is not allowed.");

            var allowedTools = ToSet(pack.AllowedTools, $"specialization {pack.Id} allowedTools");
            var deniedTools = ToSet(pack.DeniedTools, $"specialization {pack.Id} deniedTools");
            if (allowedTools.Count == 0)
                throw new InvalidOperationException($"specialization {pack.Id} must declare at least one allowed tool.");
            if (allowedTools.Overlaps(deniedTools))
                throw new InvalidOperationException($"specialization {pack.Id} allows and denies the same tool.");

            var boundSkills = ToSet(pack.BoundSkills, $"specialization {pack.Id} boundSkills");
            if (boundSkills.Count == 0)
                throw new InvalidOperationException($"specialization {pack.Id} must bind at least one skill.");
            foreach (var skillId in boundSkills)
            {
                if (!skillIds.Contains(skillId))
                    throw new InvalidOperationException(
                        $"specialization {pack.Id} references unknown skill '{skillId}'.");
            }

            var modalities = ToSet(pack.Modalities, $"specialization {pack.Id} modalities");
            if (modalities.Count == 0)
                throw new InvalidOperationException($"specialization {pack.Id} must declare at least one modality.");
            foreach (var modality in modalities)
            {
                if (!laneIds.Contains(modality))
                    throw new InvalidOperationException(
                        $"specialization {pack.Id} references unknown modality '{modality}'.");
            }
        }

        return registry;
    }

    private static List<string> BuildDefaultToolSet(
        HashSet<string> packAllowedTools,
        HashSet<string> packDeniedTools,
        IReadOnlyList<SpecializationSkillContract> selectedSkills)
    {
        var result = new HashSet<string>(packAllowedTools, Comparer);
        result.ExceptWith(packDeniedTools);
        foreach (var skill in selectedSkills)
        {
            var allowed = ToSet(skill.AllowedTools, $"skill {skill.Id} allowedTools");
            var denied = ToSet(skill.DeniedTools, $"skill {skill.Id} deniedTools");
            result.IntersectWith(allowed);
            result.ExceptWith(denied);
        }
        return result.OrderBy(value => value, Comparer).ToList();
    }

    private static IReadOnlyList<EvidenceLink> NormalizeEvidenceLinks(
        IReadOnlyList<EvidenceLink>? links,
        ICollection<SpecializationViolation> violations)
    {
        if (links is null || links.Count == 0) return [];
        var normalized = new Dictionary<string, EvidenceLink>(Comparer);
        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Rel) || string.IsNullOrWhiteSpace(link.Href))
            {
                violations.Add(new("invalid-evidence-link",
                    "Every evidence link must contain both rel and href."));
                continue;
            }

            if (!Uri.TryCreate(link.Href, UriKind.Absolute, out _))
            {
                violations.Add(new("invalid-evidence-link",
                    $"Evidence link '{link.Href}' is not an absolute URI."));
                continue;
            }

            var rel = link.Rel.Trim();
            var href = link.Href.Trim();
            normalized[$"{rel}|{href}"] = new EvidenceLink(rel, href);
        }
        return normalized.Values.OrderBy(value => value.Rel, Comparer).ThenBy(value => value.Href, Comparer).ToArray();
    }

    private static IReadOnlyDictionary<string, string> BuildProvenance(
        IReadOnlyList<string> requiredFields,
        string modality,
        string correlationId,
        string idempotencyKey,
        string specializationId,
        IEnumerable<string> skillIds,
        IReadOnlyList<EvidenceLink> links)
    {
        var normalizedFields = Normalize(requiredFields, "required provenance field");
        var now = DateTimeOffset.UtcNow.ToString("O");
        var skills = string.Join(",", skillIds.OrderBy(value => value, Comparer));
        var evidence = string.Join(";", links.Select(link => $"{link.Rel}:{link.Href}"));
        var provenance = new Dictionary<string, string>(Comparer);
        foreach (var field in normalizedFields)
        {
            var value = field.ToLowerInvariant() switch
            {
                "correlationid" => correlationId,
                "idempotencykey" => idempotencyKey,
                "specializationid" => specializationId,
                "specializationpackid" => specializationId,
                "skillids" => skills,
                "modality" => modality,
                "evidencelinks" => evidence,
                "emittedat" => now,
                "schemaversion" => "specialization-evidence.v1",
                _ => "set-by-policy"
            };
            provenance[field] = value;
        }
        return provenance;
    }

    private static HashSet<string> ToSet(IReadOnlyList<string> values, string fieldName) =>
        new(Normalize(values, fieldName), Comparer);

    private static List<string> NormalizeRequestValues(IReadOnlyList<string>? values)
    {
        if (values is null) return [];
        var normalized = new HashSet<string>(Comparer);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            normalized.Add(value.Trim());
        }
        return normalized.OrderBy(value => value, Comparer).ToList();
    }

    private static List<string> Normalize(IReadOnlyList<string>? values, string fieldName)
    {
        if (values is null) return [];
        var normalized = new HashSet<string>(Comparer);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{fieldName} contains an empty value.");
            normalized.Add(value.Trim());
        }
        return normalized.OrderBy(value => value, Comparer).ToList();
    }

    private static bool IsSafeIdempotencyKey(string value) =>
        value.Length is >= 8 and <= 128 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    private static bool IsSafeCorrelationId(string value) =>
        value.Length is >= 4 and <= 128 && value.All(character => !char.IsControl(character));
}
