using System.Text.Json;
using Helios.Connect.Api;
using Helios.Connect.Contracts;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class SpecializationPolicyTests
{
    [Fact]
    public void Registry_and_example_are_versioned_and_loadable()
    {
        var runtimeRegistry = LoadRegistry("hermes-xcore9-specialization-packs.json");
        var exampleRegistry = LoadRegistry("hermes-xcore9-specialization-packs.example.json");

        var runtimeEvaluator = new SpecializationPolicyEvaluator(runtimeRegistry);
        var exampleEvaluator = new SpecializationPolicyEvaluator(exampleRegistry);

        Assert.Equal("1.0", runtimeEvaluator.Registry.SchemaVersion);
        Assert.Equal("1.0", exampleEvaluator.Registry.SchemaVersion);
        Assert.NotEmpty(runtimeEvaluator.Registry.Packs);
        Assert.NotEmpty(exampleEvaluator.Registry.Packs);
    }

    [Fact]
    public void Undeclared_skill_binding_is_rejected()
    {
        var evaluator = new SpecializationPolicyEvaluator(LoadRegistry("hermes-xcore9-specialization-packs.json"));
        var decision = evaluator.Evaluate(new SpecializationExecutionRequest(
            SpecializationId: "hermes-xcore9-orchestrator",
            IdempotencyKey: "specialization-skill-0001",
            CorrelationId: "corr-241-skill",
            RequestedTools: ["search"],
            RequestedSkills: ["not-declared-skill"],
            RequestedModalities: ["text"],
            EvidenceLinks: [new EvidenceLink("issue", "https://github.com/M0nado/helios-platform/issues/241")]));

        Assert.False(decision.Allowed);
        Assert.Contains(decision.Violations, violation => violation.Code == "skill-not-bound");
    }

    [Fact]
    public void Undeclared_tool_use_via_skill_contract_is_rejected()
    {
        var evaluator = new SpecializationPolicyEvaluator(LoadRegistry("hermes-xcore9-specialization-packs.json"));
        var decision = evaluator.Evaluate(new SpecializationExecutionRequest(
            SpecializationId: "xcore9-multimodal-review",
            IdempotencyKey: "specialization-tool-0001",
            CorrelationId: "corr-241-tool",
            RequestedTools: ["helios_get_control_plane_status"],
            RequestedModalities: ["text"],
            EvidenceLinks: [new EvidenceLink("issue", "https://github.com/M0nado/helios-platform/issues/241")]));

        Assert.False(decision.Allowed);
        Assert.Contains(decision.Violations, violation => violation.Code == "tool-not-declared-by-skill");
    }

    [Fact]
    public void Parallelism_and_fan_limits_are_bounded()
    {
        var evaluator = new SpecializationPolicyEvaluator(LoadRegistry("hermes-xcore9-specialization-packs.json"));
        var decision = evaluator.Evaluate(new SpecializationExecutionRequest(
            SpecializationId: "hermes-xcore9-orchestrator",
            RequestedParallelism: 10,
            RequestedFanOut: 6,
            RequestedFanIn: 6,
            TimeoutSeconds: 300,
            IdempotencyKey: "specialization-bounds-0001",
            CorrelationId: "corr-241-bounds",
            RequestedTools: ["search"],
            RequestedSkills: ["helios-control-skill"],
            RequestedModalities: ["text"],
            EvidenceLinks: [new EvidenceLink("issue", "https://github.com/M0nado/helios-platform/issues/241")]));

        Assert.False(decision.Allowed);
        Assert.Contains(decision.Violations, violation => violation.Code == "pack-parallelism-exceeded");
        Assert.Contains(decision.Violations, violation => violation.Code == "pack-fanout-exceeded");
        Assert.Contains(decision.Violations, violation => violation.Code == "pack-fanin-exceeded");
        Assert.NotEmpty(decision.EvidenceMetadata);
        Assert.All(decision.EvidenceMetadata, metadata =>
        {
            Assert.Equal("corr-241-bounds", metadata.CorrelationId);
            Assert.Equal("specialization-bounds-0001", metadata.IdempotencyKey);
        });
    }

    [Fact]
    public void Negative_requested_limits_are_rejected()
    {
        var evaluator = new SpecializationPolicyEvaluator(LoadRegistry("hermes-xcore9-specialization-packs.json"));
        var decision = evaluator.Evaluate(new SpecializationExecutionRequest(
            SpecializationId: "hermes-xcore9-orchestrator",
            RequestedParallelism: -1,
            RequestedFanOut: -2,
            RequestedFanIn: -3,
            TimeoutSeconds: -4,
            IdempotencyKey: "specialization-negative-0001",
            CorrelationId: "corr-241-negative",
            RequestedTools: ["search"],
            RequestedSkills: ["helios-control-skill"],
            RequestedModalities: ["text"],
            EvidenceLinks: [new EvidenceLink("issue", "https://github.com/M0nado/helios-platform/issues/241")]));

        Assert.False(decision.Allowed);
        Assert.Contains(decision.Violations, violation => violation.Code == "invalid-requested-parallelism");
        Assert.Contains(decision.Violations, violation => violation.Code == "invalid-requested-fanout");
        Assert.Contains(decision.Violations, violation => violation.Code == "invalid-requested-fanin");
        Assert.Contains(decision.Violations, violation => violation.Code == "invalid-timeout");
    }

    [Fact]
    public void Blank_requested_modalities_are_rejected()
    {
        var evaluator = new SpecializationPolicyEvaluator(LoadRegistry("hermes-xcore9-specialization-packs.json"));
        var decision = evaluator.Evaluate(new SpecializationExecutionRequest(
            SpecializationId: "hermes-xcore9-orchestrator",
            RequestedParallelism: 1,
            RequestedFanOut: 1,
            RequestedFanIn: 1,
            TimeoutSeconds: 60,
            IdempotencyKey: "specialization-blank-modalities-0001",
            CorrelationId: "corr-241-modalities",
            RequestedTools: ["search"],
            RequestedSkills: ["helios-control-skill"],
            RequestedModalities: ["  "],
            EvidenceLinks: [new EvidenceLink("issue", "https://github.com/M0nado/helios-platform/issues/241")]));

        Assert.False(decision.Allowed);
        Assert.Contains(decision.Violations, violation => violation.Code == "invalid-requested-modalities");
    }

    [Fact]
    public void Null_evidence_link_entries_are_rejected_without_throwing()
    {
        var evaluator = new SpecializationPolicyEvaluator(LoadRegistry("hermes-xcore9-specialization-packs.json"));
        var decision = evaluator.Evaluate(new SpecializationExecutionRequest(
            SpecializationId: "hermes-xcore9-orchestrator",
            RequestedParallelism: 1,
            RequestedFanOut: 1,
            RequestedFanIn: 1,
            TimeoutSeconds: 60,
            IdempotencyKey: "specialization-null-links-0001",
            CorrelationId: "corr-241-links",
            RequestedTools: ["search"],
            RequestedSkills: ["helios-control-skill"],
            RequestedModalities: ["text"],
            EvidenceLinks: [null!, new EvidenceLink("issue", "https://github.com/M0nado/helios-platform/issues/241")]));

        Assert.False(decision.Allowed);
        Assert.Contains(decision.Violations, violation => violation.Code == "invalid-evidence-link");
    }

    [Fact]
    public void Unsupported_required_provenance_fields_are_rejected()
    {
        var registry = LoadRegistry("hermes-xcore9-specialization-packs.json");
        var invalidRegistry = registry with
        {
            MultimodalRouting = registry.MultimodalRouting with
            {
                RequiredProvenanceFields = [..registry.MultimodalRouting.RequiredProvenanceFields, "commitSha"]
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => new SpecializationPolicyEvaluator(invalidRegistry));
        Assert.Contains("Unsupported required provenance field", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Multimodal_routing_emits_normalized_evidence_metadata()
    {
        var evaluator = new SpecializationPolicyEvaluator(LoadRegistry("hermes-xcore9-specialization-packs.json"));
        var decision = evaluator.Evaluate(new SpecializationExecutionRequest(
            SpecializationId: "hermes-xcore9-orchestrator",
            RequestedParallelism: 3,
            RequestedFanOut: 3,
            RequestedFanIn: 3,
            TimeoutSeconds: 180,
            IdempotencyKey: "specialization-meta-0001",
            CorrelationId: "corr-241-meta",
            RequestedTools: ["search", "fetch", "helios_plan_specialization_run"],
            RequestedSkills: ["helios-control-skill"],
            RequestedModalities: ["text", "code"],
            EvidenceLinks:
            [
                new EvidenceLink("issue", "https://github.com/M0nado/helios-platform/issues/241"),
                new EvidenceLink("pr", "https://github.com/M0nado/helios-platform/pull/241")
            ]));

        Assert.True(decision.Allowed);
        Assert.NotNull(decision.Policy);
        Assert.Equal("continue-and-report", decision.Policy!.PartialFailurePolicy);
        Assert.Equal(2, decision.EvidenceMetadata.Count);
        Assert.All(decision.EvidenceMetadata, metadata =>
        {
            Assert.Equal("corr-241-meta", metadata.CorrelationId);
            Assert.Equal("specialization-meta-0001", metadata.IdempotencyKey);
            Assert.Equal("hermes-xcore9-orchestrator", metadata.SpecializationId);
            Assert.NotEmpty(metadata.EvidenceLinks);
            Assert.Contains("correlationId", metadata.Provenance.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("idempotencyKey", metadata.Provenance.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("specializationId", metadata.Provenance.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("modality", metadata.Provenance.Keys, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("evidenceLinks", metadata.Provenance.Keys, StringComparer.OrdinalIgnoreCase);
        });
    }

    private static SpecializationRegistryDocument LoadRegistry(string fileName)
    {
        var path = FindConfigPath(fileName);
        var json = File.ReadAllText(path);
        var registry = JsonSerializer.Deserialize<SpecializationRegistryDocument>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return registry ?? throw new InvalidOperationException($"Could not deserialize specialization registry at '{path}'.");
    }

    private static string FindConfigPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Config file name is required.", nameof(fileName));
        if (Path.IsPathRooted(fileName) ||
            fileName.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            fileName.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Config file name must be a relative leaf file name.", nameof(fileName));

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "config", fileName);
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate config file '{fileName}' by searching parent directories from '{AppContext.BaseDirectory}'.");
    }
}
