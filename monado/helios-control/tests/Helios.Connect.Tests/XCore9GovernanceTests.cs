using System.Text.Json;
using Helios.Connect.Contracts;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class XCore9GovernanceTests
{
    [Fact]
    public void Runtime_matrix_declares_required_modes_and_deny_lists()
    {
        using var matrix = ReadConfig("xcore9-runtime-matrix.v1.json");
        var root = matrix.RootElement;

        Assert.Equal("1.0.0", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.GetProperty("nonDestructiveDefault").GetBoolean());
        Assert.Equal("local-windows", root.GetProperty("defaultMode").GetString());

        var modes = root.GetProperty("modes").EnumerateArray().ToList();
        Assert.Equal(3, modes.Count);

        var ids = modes.Select(mode => mode.GetProperty("id").GetString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("local-windows", ids);
        Assert.Contains("local-docker", ids);
        Assert.Contains("hybrid-fleet", ids);

        foreach (var mode in modes)
        {
            Assert.True(mode.TryGetProperty("startupContract", out _));
            Assert.True(mode.TryGetProperty("artifactPinning", out _));
            Assert.True(mode.TryGetProperty("resourceEnvelope", out _));
            Assert.True(mode.TryGetProperty("rollback", out _));

            var denyList = mode.GetProperty("denyList").EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();
            Assert.NotEmpty(denyList);
            Assert.Contains("production-mutation-without-protected-approval", denyList);
            Assert.Contains("cross-tenant-secret-token-reuse", denyList);
            Assert.Contains("background-self-expanding-agent-runs", denyList);
        }

        var localWindows = modes.Single(mode => string.Equals(mode.GetProperty("id").GetString(), "local-windows", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            "GET /health/ready returns 200",
            localWindows.GetProperty("startupContract").GetProperty("healthProbe").GetString());
        Assert.Contains(
            localWindows.GetProperty("startupContract").GetProperty("deterministicCommands").EnumerateArray().Select(cmd => cmd.GetString()),
            command => string.Equals(
                command,
                "dotnet run --project monado/helios-control/src/Helios.Connect.Api/Helios.Connect.Api.csproj --configuration Release --no-build -- --urls http://127.0.0.1:8080",
                StringComparison.Ordinal));

        var localDocker = modes.Single(mode => string.Equals(mode.GetProperty("id").GetString(), "local-docker", StringComparison.OrdinalIgnoreCase));
        var localDockerCommands = localDocker.GetProperty("startupContract").GetProperty("deterministicCommands").EnumerateArray()
            .Select(cmd => cmd.GetString())
            .ToList();
        Assert.Contains(
            localDockerCommands,
            command => string.Equals(
                command,
                "docker run --rm --read-only --detach --name helios-connect-local --publish 127.0.0.1:8080:8080 <immutableDigest>",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            localDockerCommands,
            command => !string.IsNullOrWhiteSpace(command) && command.Contains("--health-check", StringComparison.Ordinal));

        var hybridFleet = modes.Single(mode => string.Equals(mode.GetProperty("id").GetString(), "hybrid-fleet", StringComparison.OrdinalIgnoreCase));
        var hybridCommands = hybridFleet.GetProperty("startupContract").GetProperty("deterministicCommands").EnumerateArray()
            .Select(cmd => cmd.GetString())
            .ToList();
        Assert.Contains("pwsh -File monado/helios-control/scripts/Start-HeliosLocalFleet.ps1 -Mode Plan", hybridCommands);
        Assert.Contains(
            hybridCommands,
            command => !string.IsNullOrWhiteSpace(command)
                && command.Contains("Start-Process dotnet", StringComparison.Ordinal)
                && command.Contains("Helios.Connect.Api.csproj", StringComparison.Ordinal));
        Assert.DoesNotContain(
            hybridCommands,
            command => !string.IsNullOrWhiteSpace(command)
                && command.Contains("Start-Process dotnet", StringComparison.Ordinal)
                && command.Contains("--no-build", StringComparison.Ordinal));
        Assert.Contains("pwsh -File monado/helios-control/scripts/Start-HeliosLocalFleet.ps1 -Mode Status", hybridCommands);
    }

    [Fact]
    public void Knaa_contract_declares_thresholds_and_required_audit_fields()
    {
        using var knaa = ReadConfig("xcore9-knaa-model.v1.json");
        var root = knaa.RootElement;

        Assert.Equal("1.0.0", root.GetProperty("schemaVersion").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("modelVersion").GetString()));
        Assert.True(root.GetProperty("governance").GetProperty("outputsAreAdvisoryByDefault").GetBoolean());

        var thresholds = root.GetProperty("thresholds");
        var block = thresholds.GetProperty("blockBelow").GetDouble();
        var warn = thresholds.GetProperty("warnBelow").GetDouble();
        var review = thresholds.GetProperty("reviewBelow").GetDouble();
        Assert.True(block < warn && warn < review);

        var requiredFields = root.GetProperty("auditPayloadRequiredFields")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("modelVersion", requiredFields);
        Assert.Contains("thresholds", requiredFields);
        Assert.Contains("evidenceLinks", requiredFields);
        Assert.Contains("recommendation", requiredFields);
        Assert.Contains("confidence", requiredFields);
        Assert.Contains("policyMode", requiredFields);
    }

    [Fact]
    public void Knaa_evaluator_returns_unknown_when_insufficient_evidence()
    {
        var policy = new XCore9KnaaPolicy("knaa-2026-08-06", new XCore9KnaaThresholds(0.35, 0.55, 0.75));
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(0.9, null, null, null),
            policy,
            new[] { "https://example.test/evidence/1" });

        Assert.Equal(XCore9Recommendation.Unknown, evaluation.Recommendation);
        Assert.Null(evaluation.CompositeScore);
        Assert.Equal("insufficient-evidence", evaluation.Reason);
        Assert.Equal("knaa-2026-08-06", evaluation.Audit.ModelVersion);
    }

    [Fact]
    public void Knaa_evaluator_is_advisory_by_default_for_block_threshold_scores()
    {
        var policy = new XCore9KnaaPolicy("knaa-2026-08-06", new XCore9KnaaThresholds(0.35, 0.55, 0.75));
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(0.1, 0.1, 0.2, 0.2),
            policy,
            new[] { "https://example.test/evidence/2", "https://example.test/evidence/2" });

        Assert.Equal(XCore9Recommendation.ReviewRequired, evaluation.Recommendation);
        Assert.Equal("below-block-threshold-advisory", evaluation.Reason);
        Assert.Equal("advisory", evaluation.Audit.PolicyMode);
        Assert.Single(evaluation.Audit.EvidenceLinks);
    }

    [Fact]
    public void Knaa_evaluator_supports_conservative_auto_block()
    {
        var policy = new XCore9KnaaPolicy(
            "knaa-2026-08-06",
            new XCore9KnaaThresholds(0.35, 0.55, 0.75),
            ConservativeAutoBlock: true);
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(0.1, 0.2, 0.1, 0.1),
            policy,
            new[] { "https://example.test/evidence/3" });

        Assert.Equal(XCore9Recommendation.Block, evaluation.Recommendation);
        Assert.Equal("below-block-threshold", evaluation.Reason);
        Assert.Equal("conservative-auto-block", evaluation.Audit.PolicyMode);
    }

    [Fact]
    public void Knaa_evaluator_uses_weighted_scoring_model()
    {
        var policy = new XCore9KnaaPolicy("knaa-2026-08-06", new XCore9KnaaThresholds(0.35, 0.55, 0.75));
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(1.0, 0.0, 1.0, 0.0),
            policy,
            new[] { "https://example.test/evidence/weighted" });

        Assert.Equal(XCore9Recommendation.ReviewRequired, evaluation.Recommendation);
        Assert.Equal(0.6, evaluation.CompositeScore);
        Assert.Equal(XCore9Recommendation.ReviewRequired, evaluation.Audit.Recommendation);
        Assert.Equal(evaluation.Confidence, evaluation.Audit.Confidence);
    }

    [Fact]
    public void Knaa_evaluator_counts_zero_weight_dimensions_as_known_evidence()
    {
        var policy = new XCore9KnaaPolicy(
            "knaa-2026-08-06",
            new XCore9KnaaThresholds(0.35, 0.55, 0.75),
            Weights: new XCore9KnaaWeights(1.0, 0.0, 0.0, 0.0));
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(0.8, 0.2, null, null),
            policy,
            new[] { "https://example.test/evidence/weighted-zero" });

        Assert.Equal(XCore9Recommendation.Pass, evaluation.Recommendation);
        Assert.Equal(0.8, evaluation.CompositeScore);
        Assert.Equal(0.5, evaluation.Confidence);
    }

    [Fact]
    public void Knaa_evaluator_returns_unknown_when_known_dimensions_have_no_scoring_weight()
    {
        var policy = new XCore9KnaaPolicy(
            "knaa-2026-08-06",
            new XCore9KnaaThresholds(0.35, 0.55, 0.75),
            Weights: new XCore9KnaaWeights(1.0, 0.0, 0.0, 0.0));
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(null, 0.7, 0.6, null),
            policy,
            new[] { "https://example.test/evidence/no-score-weight" });

        Assert.Equal(XCore9Recommendation.Unknown, evaluation.Recommendation);
        Assert.Null(evaluation.CompositeScore);
        Assert.Equal(0.5, evaluation.Confidence);
        Assert.Equal("insufficient-scoring-weight", evaluation.Reason);
    }

    [Fact]
    public void Knaa_evaluator_treats_non_finite_dimensions_as_unknown()
    {
        var policy = new XCore9KnaaPolicy("knaa-2026-08-06", new XCore9KnaaThresholds(0.35, 0.55, 0.75));
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(double.NaN, 0.8, double.PositiveInfinity, null),
            policy,
            new[] { "https://example.test/evidence/non-finite" });

        Assert.Equal(XCore9Recommendation.Unknown, evaluation.Recommendation);
        Assert.Null(evaluation.CompositeScore);
        Assert.Equal("insufficient-evidence", evaluation.Reason);
    }

    [Fact]
    public void Knaa_evaluator_rejects_non_finite_weights()
    {
        var policy = new XCore9KnaaPolicy(
            "knaa-2026-08-06",
            new XCore9KnaaThresholds(0.35, 0.55, 0.75),
            Weights: new XCore9KnaaWeights(double.NaN, 0.2, 0.3, 0.5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            XCore9KnaaEvaluator.Evaluate(
                new XCore9KnaaVector(0.8, 0.8, 0.8, 0.8),
                policy,
                new[] { "https://example.test/evidence/non-finite-weight" }));
    }

    [Fact]
    public void Knaa_evaluator_exposes_read_only_audit_evidence_links()
    {
        var policy = new XCore9KnaaPolicy("knaa-2026-08-06", new XCore9KnaaThresholds(0.35, 0.55, 0.75));
        var evaluation = XCore9KnaaEvaluator.Evaluate(
            new XCore9KnaaVector(0.9, 0.7, null, null),
            policy,
            new[] { "https://example.test/evidence/audit-1", "https://example.test/evidence/audit-2" });

        var mutableView = Assert.IsAssignableFrom<IList<string>>(evaluation.Audit.EvidenceLinks);
        Assert.Throws<NotSupportedException>(() => mutableView[0] = "https://example.test/evidence/tampered");
    }

    [Fact]
    public void Specialization_manifest_requires_lane_provenance_and_parallel_limits()
    {
        using var registry = ReadConfig("xcore9-specialization-packs.v1.json");
        var root = registry.RootElement;

        Assert.Equal("1.0.0", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.GetProperty("pluginBindings").GetProperty("requiresCapabilityContracts").GetBoolean());
        Assert.True(root.GetProperty("globalPolicy").GetProperty("maxFanOut").GetInt32() > 0);
        Assert.True(root.GetProperty("globalPolicy").GetProperty("maxFanIn").GetInt32() > 0);

        var lanes = root.GetProperty("multimodalRouting").GetProperty("lanes").EnumerateArray().ToList();
        Assert.NotEmpty(lanes);
        foreach (var provenance in lanes.Select(
                     lane => lane.GetProperty("requiredProvenance").EnumerateArray()
                         .Select(item => item.GetString())
                         .Where(item => !string.IsNullOrWhiteSpace(item))
                         .ToHashSet(StringComparer.OrdinalIgnoreCase)))
        {
            Assert.Contains("correlationId", provenance);
            Assert.Contains("evidenceLinks", provenance);
        }

        var packs = root.GetProperty("packs").EnumerateArray().ToList();
        Assert.NotEmpty(packs);
        foreach (var pack in packs)
        {
            Assert.True(pack.GetProperty("maxParallelism").GetInt32() > 0);
            Assert.True(pack.GetProperty("timeoutSeconds").GetInt32() > 0);
            Assert.NotEmpty(pack.GetProperty("deniedTools").EnumerateArray());
        }
    }

    [Fact]
    public void Specialization_registry_rejects_undeclared_tool_use()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation(tool: "plugin.secret-read"));

        Assert.False(decision.Allowed);
        Assert.Equal("tool-not-declared", decision.Code);
        Assert.Null(decision.Evidence);
    }

    [Fact]
    public void Specialization_registry_rejects_parallelism_outside_pack_limit()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation(requestedParallelism: 5));

        Assert.False(decision.Allowed);
        Assert.Equal("parallelism-exceeded", decision.Code);
    }

    [Fact]
    public void Specialization_registry_rejects_fan_out_above_global_limit()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation(coordinatorFanOut: 5));

        Assert.False(decision.Allowed);
        Assert.Equal("fan-out-exceeded", decision.Code);
    }

    [Fact]
    public void Specialization_registry_rejects_fan_in_above_global_limit()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation(coordinatorFanIn: 5));

        Assert.False(decision.Allowed);
        Assert.Equal("fan-in-exceeded", decision.Code);
    }

    [Fact]
    public void Specialization_registry_requires_declared_capability_contracts()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation(capabilityContracts: new[] { "capability.repo.read-only" }));

        Assert.False(decision.Allowed);
        Assert.Equal("missing-capability-contract", decision.Code);
    }

    [Fact]
    public void Specialization_registry_requires_lane_provenance()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(
            CreateInvocation(
                provenance: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sourceCommit"] = "abc123",
                }));

        Assert.False(decision.Allowed);
        Assert.Equal("missing-lane-provenance", decision.Code);
    }

    [Fact]
    public void Specialization_registry_emits_normalized_evidence_metadata()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation());

        Assert.True(decision.Allowed);
        Assert.NotNull(decision.Evidence);
        Assert.Equal("corr-123", decision.Evidence!.CorrelationId);
        Assert.Equal("code", decision.Evidence.InputModality);
        Assert.Equal("telemetry", decision.Evidence.OutputModality);
        Assert.Equal(2, decision.Evidence.EvidenceLinks.Count);
        Assert.Equal(900, decision.Evidence.TimeoutSeconds);
        Assert.Equal("corr-123", decision.Evidence.Provenance["correlationId"]);
        Assert.Equal("abc123", decision.Evidence.Provenance["sourceCommit"]);
        Assert.Equal("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01", decision.Evidence.Provenance["traceParent"]);
    }

    [Fact]
    public void Specialization_registry_exposes_read_only_provenance()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation());

        Assert.True(decision.Allowed);
        Assert.NotNull(decision.Evidence);
        var mutableView = Assert.IsAssignableFrom<IDictionary<string, string>>(decision.Evidence!.Provenance);
        Assert.Throws<NotSupportedException>(() => mutableView["correlationId"] = "tampered");
    }

    [Fact]
    public void Specialization_registry_exposes_read_only_evidence_links()
    {
        var registry = CreateRegistry();
        var decision = registry.Evaluate(CreateInvocation());

        Assert.True(decision.Allowed);
        Assert.NotNull(decision.Evidence);
        var mutableView = Assert.IsAssignableFrom<IList<string>>(decision.Evidence!.EvidenceLinks);
        Assert.Throws<NotSupportedException>(() => mutableView[0] = "https://example.test/evidence/tampered");
    }

    [Fact]
    public void Specialization_registry_snapshots_pack_collections()
    {
        var inputs = new List<string> { "code" };
        var outputs = new List<string> { "telemetry" };
        var allowedTools = new List<string> { "repo.read" };
        var deniedTools = new List<string> { "git.push" };
        var requiredContracts = new List<string> { "capability.repo.read-only", "capability.tests.non-destructive" };

        var registry = new XCore9SpecializationRegistry(new[]
        {
            new XCore9SpecializationPack(
                Id: "xcore9-code-analysis",
                Inputs: inputs,
                Outputs: outputs,
                AllowedTools: allowedTools,
                DeniedTools: deniedTools,
                RequiredCapabilityContracts: requiredContracts,
                MaxParallelism: 2,
                TimeoutSeconds: 900,
                RequireIdempotencyKey: true)
        });

        allowedTools.Add("plugin.secret-read");
        deniedTools.Clear();
        inputs.Add("docs");
        outputs.Add("docs");
        requiredContracts.Clear();

        var undeclaredDecision = registry.Evaluate(CreateInvocation(tool: "plugin.secret-read"));
        Assert.False(undeclaredDecision.Allowed);
        Assert.Equal("tool-not-declared", undeclaredDecision.Code);

        var deniedDecision = registry.Evaluate(CreateInvocation(tool: "git.push"));
        Assert.False(deniedDecision.Allowed);
        Assert.Equal("tool-denied", deniedDecision.Code);
    }

    [Fact]
    public void Specialization_registry_rejects_unmapped_pack_modalities()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new XCore9SpecializationRegistry(new[]
            {
                new XCore9SpecializationPack(
                    Id: "xcore9-audio",
                    Inputs: new[] { "audio" },
                    Outputs: new[] { "audio" },
                    AllowedTools: new[] { "repo.read" },
                    DeniedTools: new[] { "git.push" },
                    RequiredCapabilityContracts: new[] { "capability.repo.read-only" },
                    MaxParallelism: 1,
                    TimeoutSeconds: 120,
                    RequireIdempotencyKey: false)
            }));

        Assert.Contains("without provenance policies", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static XCore9SpecializationRegistry CreateRegistry() =>
        new(new[]
        {
            new XCore9SpecializationPack(
                Id: "xcore9-code-analysis",
                Inputs: new[] { "code", "telemetry", "text" },
                Outputs: new[] { "code", "telemetry", "text" },
                AllowedTools: new[] { "repo.read", "dotnet.test", "plugin.lint" },
                DeniedTools: new[] { "git.push", "az.deployment.apply" },
                RequiredCapabilityContracts: new[] { "capability.repo.read-only", "capability.tests.non-destructive" },
                MaxParallelism: 2,
                TimeoutSeconds: 900,
                RequireIdempotencyKey: true)
        });

    private static XCore9SpecializationInvocation CreateInvocation(
        string tool = "repo.read",
        int requestedParallelism = 2,
        IReadOnlyList<string>? capabilityContracts = null,
        int coordinatorFanOut = 1,
        int coordinatorFanIn = 1,
        IReadOnlyDictionary<string, string>? provenance = null) =>
        new(
            PackId: "xcore9-code-analysis",
            Tool: tool,
            InputModality: "code",
            OutputModality: "telemetry",
            RequestedParallelism: requestedParallelism,
            CorrelationId: "corr-123",
            IdempotencyKey: "idem-123",
            EvidenceLinks: new[] { "https://example.test/evidence/10", "https://example.test/evidence/10", "https://example.test/evidence/11" },
            CapabilityContracts: capabilityContracts ?? new[] { "capability.repo.read-only", "capability.tests.non-destructive" },
            CoordinatorFanOut: coordinatorFanOut,
            CoordinatorFanIn: coordinatorFanIn,
            Provenance: provenance ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceCommit"] = "abc123",
                ["traceParent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
            });

    private static JsonDocument ReadConfig(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.IsPathRooted(fileName))
        {
            throw new ArgumentException("Configuration fileName must be relative.", nameof(fileName));
        }

        var path = CombineRelative(ResolveRepositoryRoot(), "monado", "helios-control", "config", fileName);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = CombineRelative(directory.FullName, "monado", "helios-control", "config");
            if (Directory.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be resolved from test context.");
    }

    private static string CombineRelative(string basePath, params string[] segments)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path is required.", nameof(basePath));
        }

        var combined = basePath;
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment) || Path.IsPathRooted(segment))
            {
                throw new ArgumentException("All path segments must be non-empty relative paths.", nameof(segments));
            }

            combined = Path.Join(combined, segment);
        }

        return combined;
    }
}
