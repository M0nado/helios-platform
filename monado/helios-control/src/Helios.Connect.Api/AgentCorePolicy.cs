using System.Text.Json;

namespace Helios.Connect.Api;

public sealed record AgentCoreTransition(string From, string To, string? Guard = null);

public sealed record AgentCoreLifecyclePolicy(
    string InitialState,
    IReadOnlyList<string> States,
    IReadOnlyList<string> TerminalStates,
    IReadOnlyList<AgentCoreTransition> Transitions)
{
    public bool AllowsTransition(string? from, string to)
    {
        var source = string.IsNullOrWhiteSpace(from) ? InitialState : from.Trim();
        return string.Equals(source, to, StringComparison.Ordinal) ||
            Transitions.Any(transition =>
                string.Equals(transition.From, source, StringComparison.Ordinal) &&
                string.Equals(transition.To, to, StringComparison.Ordinal));
    }
}

public sealed record AgentCoreImmutableContextPolicy(
    IReadOnlyList<string> RequiredFields,
    bool RejectMissingRequiredFields,
    IReadOnlyList<string> ForbiddenRewrites);

public sealed record AgentCoreRetryPolicy(
    int MaxAttempts,
    IReadOnlyList<string> TransientClassifications,
    IReadOnlyList<string> NeverRetryClassifications,
    string Jitter);

public sealed record AgentCoreEventBusPolicy(
    string Delivery,
    int MaxHopCount,
    string CycleDetection,
    AgentCoreRetryPolicy Retry);

public sealed record AgentCoreConcurrencyPolicy(
    string RepositoryEnvironmentLeases,
    int MaxDeploymentsPerEnvironment,
    string FencingTokens,
    string CompareAndSwapOwnership);

public sealed record AgentCoreBreakGlassPolicy(
    bool Enabled,
    bool RequiresSecondPersonApproval,
    bool RequiresAuditEvidence,
    string Expires);

public sealed record AgentCoreApprovalsPolicy(
    IReadOnlyList<string> AuthoritativeSources,
    IReadOnlyList<string> NonAuthoritativeNotificationSources,
    string Timeouts,
    AgentCoreBreakGlassPolicy BreakGlass);

public sealed record AgentCoreSeverityPolicy(string S0, string S1, string S2, string S3);

public sealed record AgentCoreSuppressionPolicy(
    IReadOnlyList<string> NeverSuppress,
    bool PreserveEvidence);

public sealed record AgentCoreIncidentPolicy(
    AgentCoreSeverityPolicy Severity,
    IReadOnlyList<string> Deduplication,
    AgentCoreSuppressionPolicy Suppression);

public sealed record AgentCoreRollbackPolicy(
    bool AutomaticRollbackOnAgentError,
    bool RequiresGovernedRequest,
    bool RequiresProtectedApproval,
    bool RequiresLastKnownGoodArtifact);

public sealed record AgentCoreCapabilitiesPolicy(
    bool DenyByDefault,
    bool RuntimeSelfEscalationAllowed,
    string ReloadMode);

public sealed record AgentCoreSandboxingPolicy(
    bool ReusableCredentialsExposedToAgents,
    bool IsolatedWorktreeGeneration,
    bool ProtectedBranchDirectPushAllowed);

public sealed record AgentCoreAuditPolicy(
    bool AppendOnly,
    IReadOnlyList<string> RequiredFields,
    bool TamperEvidence);

public sealed record AgentCorePolicy(
    string SchemaVersion,
    string PolicyVersion,
    AgentCoreLifecyclePolicy Lifecycle,
    AgentCoreImmutableContextPolicy ImmutableContext,
    AgentCoreEventBusPolicy EventBus,
    AgentCoreConcurrencyPolicy Concurrency,
    AgentCoreApprovalsPolicy Approvals,
    AgentCoreIncidentPolicy Incident,
    AgentCoreRollbackPolicy Rollback,
    AgentCoreCapabilitiesPolicy Capabilities,
    AgentCoreSandboxingPolicy Sandboxing,
    AgentCoreAuditPolicy Audit)
{
    private static readonly string[] RequiredStates =
    [
        "INIT",
        "PRECHECK",
        "PLAN",
        "AWAIT_APPROVAL",
        "EXECUTE",
        "VERIFY",
        "NOTIFY",
        "COMPLETE",
        "FAILED",
        "ROLLED_BACK"
    ];

    public static AgentCorePolicy Default { get; } = CreateDefault();

    public void Validate()
    {
        if (!string.Equals(SchemaVersion, "1.0", StringComparison.Ordinal))
            throw new InvalidOperationException("agent-core-policy schemaVersion must be '1.0'.");
        if (string.IsNullOrWhiteSpace(PolicyVersion))
            throw new InvalidOperationException("agent-core-policy policyVersion is required.");
        if (!ImmutableContext.RejectMissingRequiredFields)
            throw new InvalidOperationException("agent-core-policy must reject missing immutable context fields.");
        if (EventBus.MaxHopCount <= 0)
            throw new InvalidOperationException("agent-core-policy eventBus.maxHopCount must be positive.");
        if (EventBus.Retry.MaxAttempts <= 0)
            throw new InvalidOperationException("agent-core-policy retry.maxAttempts must be positive.");
        if (!string.Equals(EventBus.Delivery, "at-least-once-idempotent-consumers", StringComparison.Ordinal))
            throw new InvalidOperationException("agent-core-policy delivery mode must be at-least-once with idempotent consumers.");
        if (!Capabilities.DenyByDefault || Capabilities.RuntimeSelfEscalationAllowed)
            throw new InvalidOperationException("agent-core-policy capabilities must be deny-by-default with no runtime self-escalation.");
        if (Sandboxing.ReusableCredentialsExposedToAgents || Sandboxing.ProtectedBranchDirectPushAllowed)
            throw new InvalidOperationException("agent-core-policy sandboxing must prevent reusable credentials and protected-branch direct pushes.");
        if (Rollback.AutomaticRollbackOnAgentError || !Rollback.RequiresGovernedRequest)
            throw new InvalidOperationException("agent-core-policy rollback must be governed and never automatic on agent error.");
        if (!Incident.Suppression.PreserveEvidence ||
            !Incident.Suppression.NeverSuppress.Contains("S0", StringComparer.Ordinal) ||
            !Incident.Suppression.NeverSuppress.Contains("S1", StringComparer.Ordinal))
            throw new InvalidOperationException("agent-core-policy incident suppression must preserve evidence and never suppress S0/S1.");

        foreach (var state in RequiredStates)
        {
            if (!Lifecycle.States.Contains(state, StringComparer.Ordinal))
                throw new InvalidOperationException($"agent-core-policy lifecycle state '{state}' is required.");
        }

        EnsureTransition("INIT", "PRECHECK");
        EnsureTransition("PRECHECK", "PLAN");
        EnsureTransition("PLAN", "AWAIT_APPROVAL");
        EnsureTransition("AWAIT_APPROVAL", "EXECUTE");
        EnsureTransition("EXECUTE", "VERIFY");
        EnsureTransition("VERIFY", "NOTIFY");
        EnsureTransition("NOTIFY", "COMPLETE");
        EnsureTransition("FAILED", "ROLLED_BACK");

        if (!Lifecycle.TerminalStates.Contains("COMPLETE", StringComparer.Ordinal) ||
            !Lifecycle.TerminalStates.Contains("FAILED", StringComparer.Ordinal) ||
            !Lifecycle.TerminalStates.Contains("ROLLED_BACK", StringComparer.Ordinal))
            throw new InvalidOperationException("agent-core-policy terminal states must include COMPLETE, FAILED, and ROLLED_BACK.");
    }

    public bool AllowsTransition(string? from, string to) => Lifecycle.AllowsTransition(from, to);

    private void EnsureTransition(string from, string to)
    {
        if (!AllowsTransition(from, to))
            throw new InvalidOperationException($"agent-core-policy transition '{from}->{to}' is required.");
    }

    private static AgentCorePolicy CreateDefault() =>
        new(
            SchemaVersion: "1.0",
            PolicyVersion: "1.0.0-default",
            Lifecycle: new(
                InitialState: "INIT",
                States: RequiredStates,
                TerminalStates: ["COMPLETE", "FAILED", "ROLLED_BACK", "AWAIT_APPROVAL"],
                Transitions:
                [
                    new("INIT", "PRECHECK"),
                    new("PRECHECK", "PLAN"),
                    new("PLAN", "AWAIT_APPROVAL"),
                    new("PLAN", "EXECUTE"),
                    new("AWAIT_APPROVAL", "EXECUTE"),
                    new("EXECUTE", "VERIFY"),
                    new("VERIFY", "NOTIFY"),
                    new("NOTIFY", "COMPLETE"),
                    new("INIT", "FAILED"),
                    new("PRECHECK", "FAILED"),
                    new("PLAN", "FAILED"),
                    new("AWAIT_APPROVAL", "FAILED"),
                    new("EXECUTE", "FAILED"),
                    new("VERIFY", "FAILED"),
                    new("NOTIFY", "FAILED"),
                    new("FAILED", "ROLLED_BACK")
                ]),
            ImmutableContext: new(
                RequiredFields: ["identity", "tenant", "environment", "repository", "artifactDigest", "approvalState"],
                RejectMissingRequiredFields: true,
                ForbiddenRewrites: ["identity", "tenant", "environment", "repository", "artifactDigest", "approvalState"]),
            EventBus: new(
                Delivery: "at-least-once-idempotent-consumers",
                MaxHopCount: 16,
                CycleDetection: "enabled",
                Retry: new(3, ["timeout"], ["authorization-denied", "schema-invalid", "policy-denied"], "required")),
            Concurrency: new("expiring", 1, "required", "required"),
            Approvals: new(
                AuthoritativeSources: ["github-protected-environment", "governed-approval-service"],
                NonAuthoritativeNotificationSources: ["slack", "outlook", "edge-dashboard", "agent-runtime"],
                Timeouts: "cancel-or-pending-never-approve",
                BreakGlass: new(true, true, true, "required")),
            Incident: new(
                Severity: new("production-outage-or-failed-production-change", "xcore-or-high-impact-degradation", "dev-or-build-degradation", "non-blocking-warning"),
                Deduplication: ["tenant", "repository", "environment", "workflowOrDeploymentId", "artifactDigest", "errorFingerprint", "boundedTimeWindow"],
                Suppression: new(["S0", "S1"], true)),
            Rollback: new(false, true, true, true),
            Capabilities: new(true, false, "atomic"),
            Sandboxing: new(false, true, false),
            Audit: new(true, ["schemaVersion", "eventId", "correlationId", "artifactDigest", "policyVersion", "timestamps", "outcome"], true));
}

public static class AgentCorePolicyLoader
{
    private const string PolicyFileName = "agent-core-policy.json";

    public static AgentCorePolicy Load(IConfiguration configuration, string? contentRootPath = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var explicitPath = configuration["HELIOS_AGENT_CORE_POLICY_PATH"]?.Trim();
        string? resolvedPath;
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var candidate = Path.IsPathFullyQualified(explicitPath)
                ? explicitPath
                : Path.Combine(contentRootPath ?? Directory.GetCurrentDirectory(), explicitPath);
            resolvedPath = Path.GetFullPath(candidate);
            if (!File.Exists(resolvedPath))
                throw new InvalidOperationException($"HELIOS agent-core policy file '{resolvedPath}' was not found.");
        }
        else
        {
            resolvedPath = ResolvePolicyPath(contentRootPath);
        }
        if (resolvedPath is null)
        {
            if (RequiresStrictPolicy(configuration))
                throw new InvalidOperationException($"HELIOS agent-core policy file '{PolicyFileName}' was not found.");
            var fallback = AgentCorePolicy.Default;
            fallback.Validate();
            return fallback;
        }

        var json = File.ReadAllText(resolvedPath);
        var policy = JsonSerializer.Deserialize<AgentCorePolicy>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            throw new InvalidOperationException($"Could not deserialize HELIOS policy from '{resolvedPath}'.");
        policy.Validate();
        return policy;
    }

    private static string? ResolvePolicyPath(string? contentRootPath)
    {
        var roots = new[]
        {
            contentRootPath,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        }
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => root!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var root in roots)
        {
            if (TryResolveFromRoot(root, out var resolved)) return resolved;
        }

        return null;
    }

    private static bool TryResolveFromRoot(string root, out string path)
    {
        for (var depth = 0; depth < 8; depth++)
        {
            var relative = string.Join(Path.DirectorySeparatorChar, Enumerable.Repeat("..", depth));
            var basePath = string.IsNullOrEmpty(relative) ? root : Path.GetFullPath(Path.Combine(root, relative));
            var direct = Path.Combine(basePath, "config", PolicyFileName);
            if (File.Exists(direct))
            {
                path = direct;
                return true;
            }

            var nested = Path.Combine(basePath, "monado", "helios-control", "config", PolicyFileName);
            if (File.Exists(nested))
            {
                path = nested;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    private static bool RequiresStrictPolicy(IConfiguration configuration) =>
        IsEnabled(configuration["HELIOS_REQUIRE_ENTRA_AUTH"]) ||
        IsEnabled(configuration["HELIOS_CLOUD_RUNTIME_ONLY"]);

    private static bool IsEnabled(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
}
