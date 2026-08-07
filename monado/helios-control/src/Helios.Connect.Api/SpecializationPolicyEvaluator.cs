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
    private static readonly StringComparer LinkHrefComparer = StringComparer.Ordinal;
    private const int MaxParallelBound = 64;
    private const int MaxTimeoutBound = 3_600;
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
    private static readonly HashSet<string> SupportedProvenanceFields = new(Comparer)
    {
        "correlationId",
        "idempotencyKey",
        "specializationId",
        "specializationPackId",
        "skillIds",
        "modality",
        "evidenceLinks",
        "emittedAt",
        "schemaVersion"
    };

    private readonly Dictionary<string, SpecializationSkillContract> _skills;
    private readonly Dictionary<string, SpecializationPackContract> _packs;
    private readonly Dictionary<string, MultimodalLaneContract> _lanes;

    public SpecializationRegistryDocument Registry { get; }

    public SpecializationPolicyEvaluator(SpecializationRegistryDocument registry)
    {
        Registry = ValidateRegistry(registry);
        _skills = Registry.Skills.ToDictionary(skill => skill.Id.Trim(), Comparer);
        _packs = Registry.Packs.ToDictionary(pack => pack.Id.Trim(), Comparer);
        _lanes = Registry.MultimodalRouting.Lanes.ToDictionary(lane => lane.Id.Trim(), Comparer);
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
            using var document = JsonDocument.Parse(json);
            ValidateRequiredRegistryJsonFields(document.RootElement, path);
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
        var configuredPath = configuration["HELIOS_SPECIALIZATION_CONFIG_PATH"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(configuredPath, environment.ContentRootPath);
        }
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "config", "hermes-xcore9-specialization-packs.json")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "config", "hermes-xcore9-specialization-packs.json")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "..", "config", "hermes-xcore9-specialization-packs.json"))
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

        var requestedSkillsSpecified = request.RequestedSkills is { Count: > 0 };
        var selectedSkillIds = requestedSkillsSpecified
            ? NormalizeRequestValues(request.RequestedSkills)
            : Normalize(pack.BoundSkills, "pack bound skill");
        if (requestedSkillsSpecified && selectedSkillIds.Count == 0)
        {
            violations.Add(new("invalid-requested-skills",
                "requestedSkills must include at least one non-empty value when provided."));
        }

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

        if (request.RequestedParallelism < 0)
        {
            violations.Add(new("invalid-requested-parallelism",
                "requestedParallelism cannot be negative."));
        }
        var effectiveParallelism = request.RequestedParallelism > 0 ? request.RequestedParallelism : 1;
        if (effectiveParallelism > Registry.ParallelPolicy.MaxGlobalParallelism)
            violations.Add(new("global-parallelism-exceeded",
                $"Requested parallelism {effectiveParallelism} exceeds global limit {Registry.ParallelPolicy.MaxGlobalParallelism}."));
        if (effectiveParallelism > pack.MaxParallelism)
            violations.Add(new("pack-parallelism-exceeded",
                $"Requested parallelism {effectiveParallelism} exceeds specialization limit {pack.MaxParallelism}."));

        if (request.RequestedFanOut < 0)
        {
            violations.Add(new("invalid-requested-fanout",
                "requestedFanOut cannot be negative."));
        }
        var defaultFanOut = Math.Min(pack.MaxFanOut, Registry.ParallelPolicy.MaxFanOut);
        var effectiveFanOut = request.RequestedFanOut > 0 ? request.RequestedFanOut : defaultFanOut;
        if (effectiveFanOut > Registry.ParallelPolicy.MaxFanOut)
            violations.Add(new("global-fanout-exceeded",
                $"Requested fan-out {effectiveFanOut} exceeds global limit {Registry.ParallelPolicy.MaxFanOut}."));
        if (effectiveFanOut > pack.MaxFanOut)
            violations.Add(new("pack-fanout-exceeded",
                $"Requested fan-out {effectiveFanOut} exceeds specialization limit {pack.MaxFanOut}."));

        if (request.RequestedFanIn < 0)
        {
            violations.Add(new("invalid-requested-fanin",
                "requestedFanIn cannot be negative."));
        }
        var defaultFanIn = Math.Min(pack.MaxFanIn, Registry.ParallelPolicy.MaxFanIn);
        var effectiveFanIn = request.RequestedFanIn > 0 ? request.RequestedFanIn : defaultFanIn;
        if (effectiveFanIn > Registry.ParallelPolicy.MaxFanIn)
            violations.Add(new("global-fanin-exceeded",
                $"Requested fan-in {effectiveFanIn} exceeds global limit {Registry.ParallelPolicy.MaxFanIn}."));
        if (effectiveFanIn > pack.MaxFanIn)
            violations.Add(new("pack-fanin-exceeded",
                $"Requested fan-in {effectiveFanIn} exceeds specialization limit {pack.MaxFanIn}."));

        if (request.TimeoutSeconds < 0)
        {
            violations.Add(new("invalid-timeout",
                "timeoutSeconds cannot be negative."));
        }
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

        var requestedModalitiesSpecified = request.RequestedModalities is { Count: > 0 };
        var selectedModalities = requestedModalitiesSpecified
            ? NormalizeRequestValues(request.RequestedModalities)
            : Normalize(pack.Modalities, "pack modality");
        if (requestedModalitiesSpecified && selectedModalities.Count == 0)
        {
            violations.Add(new("invalid-requested-modalities",
                "requestedModalities must include at least one non-empty value when provided."));
        }

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

        var requestedToolsSpecified = request.RequestedTools is { Count: > 0 };
        var normalizedTools = requestedToolsSpecified
            ? NormalizeRequestValues(request.RequestedTools)
            : BuildDefaultToolSet(packAllowedTools, packDeniedTools, selectedSkills);
        if (requestedToolsSpecified && normalizedTools.Count == 0)
        {
            violations.Add(new("invalid-requested-tools",
                "requestedTools must include at least one non-empty value when provided."));
        }
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

        var effectiveTimeoutBehavior = Registry.ParallelPolicy.CancelRunningChildrenOnTimeout ||
            string.Equals(pack.TimeoutBehavior, "cancel-running-children", StringComparison.OrdinalIgnoreCase)
                ? "cancel-running-children"
                : "complete-active-children";
        var cancelRunningChildrenOnTimeout = string.Equals(
            effectiveTimeoutBehavior,
            "cancel-running-children",
            StringComparison.OrdinalIgnoreCase);
        var policy = new SpecializationDecisionPolicy(
            pack.Id,
            effectiveParallelism,
            effectiveFanOut,
            effectiveFanIn,
            effectiveTimeoutSeconds,
            effectiveTimeoutBehavior,
            pack.PartialFailurePolicy,
            cancelRunningChildrenOnTimeout,
            selectedSkills.Select(skill => skill.Id).OrderBy(value => value, Comparer).ToArray(),
            normalizedTools.OrderBy(value => value, Comparer).ToArray());

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

        if (violations.Count > 0)
            return new SpecializationExecutionDecision(
                Allowed: false,
                SchemaVersion: Registry.SchemaVersion,
                RegistryVersion: Registry.RegistryVersion,
                Policy: policy,
                Violations: violations,
                EvidenceMetadata: metadata);

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

    private static void ValidateRequiredRegistryJsonFields(JsonElement root, string path)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Specialization registry file '{path}' must contain a JSON object.");

        var parallelPolicy = ReadObjectProperty(root, "parallelPolicy", path);
        EnsureBooleanProperty(parallelPolicy, "requireIdempotencyKey", "parallelPolicy");
        EnsureBooleanProperty(parallelPolicy, "requireCorrelationId", "parallelPolicy");
        EnsureBooleanProperty(parallelPolicy, "cancelRunningChildrenOnTimeout", "parallelPolicy");
        ValidateIntegerRangeProperty(parallelPolicy, "maxGlobalParallelism", 1, MaxParallelBound, "parallelPolicy");
        ValidateIntegerRangeProperty(parallelPolicy, "maxFanOut", 1, MaxParallelBound, "parallelPolicy");
        ValidateIntegerRangeProperty(parallelPolicy, "maxFanIn", 1, MaxParallelBound, "parallelPolicy");
        ValidateIntegerRangeProperty(parallelPolicy, "defaultTimeoutSeconds", 1, MaxTimeoutBound, "parallelPolicy");
        ValidateIntegerRangeProperty(parallelPolicy, "maxTimeoutSeconds", 1, MaxTimeoutBound, "parallelPolicy");

        var skills = ReadArrayProperty(root, "skills", path);
        var skillIndex = 0;
        foreach (var skill in skills.EnumerateArray())
        {
            if (skill.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"skills[{skillIndex}] must be an object.");
            EnsureBooleanProperty(skill, "requiresCorrelationId", $"skills[{skillIndex}]");
            EnsureBooleanProperty(skill, "requiresEvidenceLinks", $"skills[{skillIndex}]");
            skillIndex++;
        }

        var packs = ReadArrayProperty(root, "packs", path);
        var packIndex = 0;
        foreach (var pack in packs.EnumerateArray())
        {
            if (pack.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"packs[{packIndex}] must be an object.");
            ValidateIntegerRangeProperty(pack, "maxParallelism", 1, MaxParallelBound, $"packs[{packIndex}]");
            ValidateIntegerRangeProperty(pack, "maxFanOut", 1, MaxParallelBound, $"packs[{packIndex}]");
            ValidateIntegerRangeProperty(pack, "maxFanIn", 1, MaxParallelBound, $"packs[{packIndex}]");
            ValidateIntegerRangeProperty(pack, "timeoutSeconds", 1, MaxTimeoutBound, $"packs[{packIndex}]");
            packIndex++;
        }

        var multimodalRouting = ReadObjectProperty(root, "multimodalRouting", path);
        var lanes = ReadArrayProperty(multimodalRouting, "lanes", path);
        var laneIndex = 0;
        foreach (var lane in lanes.EnumerateArray())
        {
            if (lane.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"multimodalRouting.lanes[{laneIndex}] must be an object.");
            EnsureBooleanProperty(lane, "requiresEvidenceMetadata", $"multimodalRouting.lanes[{laneIndex}]");
            laneIndex++;
        }
    }

    private static JsonElement ReadObjectProperty(JsonElement parent, string name, string context)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Specialization registry '{context}' must include object property '{name}'.");
        return value;
    }

    private static JsonElement ReadArrayProperty(JsonElement parent, string name, string context)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Specialization registry '{context}' must include array property '{name}'.");
        return value;
    }

    private static void EnsureBooleanProperty(JsonElement parent, string name, string context)
    {
        if (!parent.TryGetProperty(name, out var value) ||
            (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
            throw new InvalidOperationException($"Specialization registry '{context}' property '{name}' must be a boolean.");
    }

    private static void ValidateIntegerRangeProperty(JsonElement parent, string name, int min, int max, string context)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var parsed))
            throw new InvalidOperationException($"Specialization registry '{context}' property '{name}' must be an integer.");
        if (parsed < min || parsed > max)
            throw new InvalidOperationException($"Specialization registry '{context}' property '{name}' must be between {min} and {max}.");
    }

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
        if (registry.ParallelPolicy.MaxGlobalParallelism <= 0 ||
            registry.ParallelPolicy.MaxGlobalParallelism > MaxParallelBound)
            throw new InvalidOperationException($"parallelPolicy.maxGlobalParallelism must be between 1 and {MaxParallelBound}.");
        if (registry.ParallelPolicy.MaxFanOut <= 0 ||
            registry.ParallelPolicy.MaxFanOut > MaxParallelBound ||
            registry.ParallelPolicy.MaxFanIn <= 0 ||
            registry.ParallelPolicy.MaxFanIn > MaxParallelBound)
            throw new InvalidOperationException($"parallelPolicy fan-out/fan-in limits must be between 1 and {MaxParallelBound}.");
        if (registry.ParallelPolicy.DefaultTimeoutSeconds <= 0 ||
            registry.ParallelPolicy.DefaultTimeoutSeconds > MaxTimeoutBound ||
            registry.ParallelPolicy.MaxTimeoutSeconds < registry.ParallelPolicy.DefaultTimeoutSeconds ||
            registry.ParallelPolicy.MaxTimeoutSeconds > MaxTimeoutBound)
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
            var laneId = lane.Id.Trim();
            if (!string.Equals(lane.Id, laneId, StringComparison.Ordinal))
                throw new InvalidOperationException($"multimodal lane id '{lane.Id}' must not include surrounding whitespace.");
            if (!laneIds.Add(laneId))
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
        foreach (var field in requiredProvenanceFields)
        {
            if (!SupportedProvenanceFields.Contains(field))
                throw new InvalidOperationException($"Unsupported required provenance field '{field}'.");
        }

        var skillIds = new HashSet<string>(Comparer);
        foreach (var skill in registry.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Id))
                throw new InvalidOperationException("Every skill contract must define id.");
            var skillId = skill.Id.Trim();
            if (!string.Equals(skill.Id, skillId, StringComparison.Ordinal))
                throw new InvalidOperationException($"skill contract id '{skill.Id}' must not include surrounding whitespace.");
            if (!skillIds.Add(skillId))
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
            var packId = pack.Id.Trim();
            if (!string.Equals(pack.Id, packId, StringComparison.Ordinal))
                throw new InvalidOperationException($"specialization id '{pack.Id}' must not include surrounding whitespace.");
            if (!packIds.Add(packId))
                throw new InvalidOperationException($"Duplicate specialization pack '{pack.Id}'.");
            if (pack.MaxParallelism <= 0 ||
                pack.MaxParallelism > MaxParallelBound ||
                pack.MaxParallelism > registry.ParallelPolicy.MaxGlobalParallelism)
                throw new InvalidOperationException($"specialization {pack.Id} maxParallelism is out of bounds.");
            if (pack.MaxFanOut <= 0 ||
                pack.MaxFanOut > MaxParallelBound ||
                pack.MaxFanOut > registry.ParallelPolicy.MaxFanOut)
                throw new InvalidOperationException($"specialization {pack.Id} maxFanOut is out of bounds.");
            if (pack.MaxFanIn <= 0 ||
                pack.MaxFanIn > MaxParallelBound ||
                pack.MaxFanIn > registry.ParallelPolicy.MaxFanIn)
                throw new InvalidOperationException($"specialization {pack.Id} maxFanIn is out of bounds.");
            if (pack.TimeoutSeconds <= 0 ||
                pack.TimeoutSeconds > MaxTimeoutBound ||
                pack.TimeoutSeconds > registry.ParallelPolicy.MaxTimeoutSeconds)
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
        var normalized = new Dictionary<string, EvidenceLink>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            if (link is null)
            {
                violations.Add(new("invalid-evidence-link",
                    "Evidence links cannot contain null items."));
                continue;
            }
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
            normalized[$"{rel.ToLowerInvariant()}|{href}"] = new EvidenceLink(rel, href);
        }
        return normalized.Values.OrderBy(value => value.Rel, Comparer).ThenBy(value => value.Href, LinkHrefComparer).ToArray();
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
                _ => throw new InvalidOperationException($"Unsupported required provenance field '{field}'.")
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
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
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
