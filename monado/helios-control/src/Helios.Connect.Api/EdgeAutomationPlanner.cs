using System.Security.Cryptography;
using System.Text;

namespace Helios.Connect.Api;

public sealed record EdgeAutomationRequest(
    string Intent,
    string Environment,
    string? Target = null,
    string? Connector = null);

public sealed record EdgeAutomationStep(
    int Order,
    string Action,
    string Executor,
    bool Mutating,
    string Gate);

public sealed record EdgeAutomationPlan(
    string PlanId,
    string Intent,
    string Environment,
    string? Target,
    string? Connector,
    string Mode,
    bool CanApplyFromMcp,
    bool DirectMainWrite,
    bool AutomaticMerge,
    IReadOnlyList<string> RequiredApprovals,
    IReadOnlyList<EdgeAutomationStep> Steps);

public interface IEdgeAutomationPlanner
{
    EdgeAutomationPlan CreatePlan(EdgeAutomationRequest request);
}

public sealed class EdgeAutomationPlanner : IEdgeAutomationPlanner
{
    private static readonly HashSet<string> Environments = new(StringComparer.OrdinalIgnoreCase)
    {
        "dev", "test", "preview", "prod"
    };

    private static readonly HashSet<string> Connectors = new(StringComparer.OrdinalIgnoreCase)
    {
        "github", "linear", "slack", "sharepoint", "copilot", "codex", "all"
    };

    public EdgeAutomationPlan CreatePlan(EdgeAutomationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var intent = NormalizeRequired(request.Intent, nameof(request.Intent), 64).ToLowerInvariant();
        var environment = NormalizeRequired(request.Environment, nameof(request.Environment), 16).ToLowerInvariant();
        if (!Environments.Contains(environment))
            throw new ArgumentException("Environment must be dev, test, preview, or prod.", nameof(request.Environment));

        var target = NormalizeOptional(request.Target, nameof(request.Target), 256);
        var connector = NormalizeOptional(request.Connector, nameof(request.Connector), 32)?.ToLowerInvariant();
        if (connector is not null && !Connectors.Contains(connector))
            throw new ArgumentException("Connector is not in the governed allowlist.", nameof(request.Connector));

        var steps = intent switch
        {
            "provision-resources" => ProvisionSteps(),
            "rotate-secret" => SecretSteps(RequireTarget(target, intent)),
            "repair-issue" => IssueSteps(RequireTarget(target, intent), connector),
            "sync-release" => ReleaseSteps(RequireTarget(target, intent), connector),
            _ => throw new ArgumentException("Intent must be provision-resources, rotate-secret, repair-issue, or sync-release.", nameof(request.Intent))
        };

        var approvals = new List<string> { "reviewed-plan-sha256" };
        if (steps.Any(step => step.Mutating)) approvals.Add("protected-environment-reviewer");
        if (environment == "prod") approvals.Add("production-owner");
        if (intent == "rotate-secret") approvals.Add("secret-owner");

        var canonical = string.Join("\n", new[]
        {
            intent,
            environment,
            target ?? string.Empty,
            connector ?? string.Empty,
            string.Join('|', steps.Select(step => $"{step.Order}:{step.Action}:{step.Executor}:{step.Mutating}:{step.Gate}"))
        });
        var planId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();

        return new EdgeAutomationPlan(
            planId,
            intent,
            environment,
            target,
            connector,
            "plan-only",
            CanApplyFromMcp: false,
            DirectMainWrite: false,
            AutomaticMerge: false,
            approvals,
            steps);
    }

    private static IReadOnlyList<EdgeAutomationStep> ProvisionSteps() =>
    [
        Step(1, "discover-current-azure-context", "azure-cli", false, "tenant-subscription-resource-group-boundary"),
        Step(2, "validate-bicep-and-provider-registration", "azure-cli", false, "pinned-toolchain"),
        Step(3, "capture-arm-what-if-and-sha256", "azure-cli", false, "no-unreviewed-delete-or-replace"),
        Step(4, "approve-plan", "github-environment", false, "reviewed-plan-sha256"),
        Step(5, "apply-incremental-deployment", "azure-cli", true, "typed-confirmation-and-protected-environment"),
        Step(6, "verify-health-identity-and-rollback", "helios-edge", false, "evidence-required")
    ];

    private static IReadOnlyList<EdgeAutomationStep> SecretSteps(string target) =>
    [
        Step(1, $"inspect-secret-metadata:{target}", "azure-cli", false, "metadata-only-never-read-value"),
        Step(2, "capture-new-value-through-secure-prompt", "operator-cli", false, "never-mcp-never-command-line"),
        Step(3, $"create-new-secret-version:{target}", "azure-cli", true, "typed-confirmation-and-secret-owner"),
        Step(4, "verify-dependent-service-health", "helios-edge", false, "no-secret-output"),
        Step(5, "disable-prior-version-after-validation", "azure-cli", true, "separate-approval")
    ];

    private static IReadOnlyList<EdgeAutomationStep> IssueSteps(string target, string? connector) =>
    [
        Step(1, $"fetch-issue:{target}", connector ?? "linear", false, "read-only-source"),
        Step(2, "reproduce-and-diagnose", "codex", false, "sandbox-and-evidence"),
        Step(3, "create-task-branch", "github", true, "no-direct-main"),
        Step(4, "apply-scoped-fix-and-run-tests", "codex", true, "workspace-only"),
        Step(5, "open-draft-pull-request", "github", true, "human-review-required"),
        Step(6, "sync-checkpoint-and-receipts", "linear-slack-sharepoint", true, "idempotent-outbox")
    ];

    private static IReadOnlyList<EdgeAutomationStep> ReleaseSteps(string target, string? connector) =>
    [
        Step(1, $"verify-release-evidence:{target}", "github", false, "commit-image-plan-hashes-match"),
        Step(2, "stage-normalized-release-envelope", "helios-edge", false, "schema-validation"),
        Step(3, $"deliver-to:{connector ?? "all"}", "connector-outbox", true, "idempotency-key"),
        Step(4, "reconcile-delivery-receipts", "connector-outbox", false, "dead-letter-on-failure")
    ];

    private static EdgeAutomationStep Step(int order, string action, string executor, bool mutating, string gate) =>
        new(order, action, executor, mutating, gate);

    private static string RequireTarget(string? target, string intent) =>
        target ?? throw new ArgumentException($"Intent '{intent}' requires a target.", nameof(EdgeAutomationRequest.Target));

    private static string NormalizeRequired(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
        return Normalize(value, name, maxLength);
    }

    private static string? NormalizeOptional(string? value, string name, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value, name, maxLength);

    private static string Normalize(string value, string name, int maxLength)
    {
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"{name} is too long.", name);
        if (normalized.Any(char.IsControl)) throw new ArgumentException($"{name} contains control characters.", name);
        return normalized;
    }
}
