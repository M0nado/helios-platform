using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Helios.Connect.Api;

public sealed record SetupBootstrapRequest(string TenantId, string SubscriptionId, string ResourceGroup, string Environment);
public sealed record SetupBootstrapResult(string Script, string ScriptSha256, string Mode, bool ContainsSecrets, bool AppliesChanges);
public sealed record UpgradeProposalRequest(string Capability, string Reason, string Target = "helios-control");
public sealed record UpgradeProposal(string ProposalId, string Capability, string Reason, string Target, string Promotion, bool AutomaticApply, bool AutomaticMerge, IReadOnlyList<string> RequiredChecks);

public interface ISetupWizardService
{
    SetupBootstrapResult CreateBootstrap(SetupBootstrapRequest request);
    UpgradeProposal CreateUpgradeProposal(UpgradeProposalRequest request);
}

public sealed partial class SetupWizardService : ISetupWizardService
{
    private static readonly HashSet<string> Environments = new(StringComparer.OrdinalIgnoreCase) { "dev", "test", "preview", "prod" };

    public SetupBootstrapResult CreateBootstrap(SetupBootstrapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenant = RequireGuid(request.TenantId, nameof(request.TenantId));
        var subscription = RequireGuid(request.SubscriptionId, nameof(request.SubscriptionId));
        var group = RequireMatch(request.ResourceGroup, nameof(request.ResourceGroup), ResourceGroupPattern(), 90);
        var environment = RequireMatch(request.Environment, nameof(request.Environment), SimpleNamePattern(), 16).ToLowerInvariant();
        if (!Environments.Contains(environment)) throw new ArgumentException("Environment must be dev, test, preview, or prod.", nameof(request.Environment));

        var script = string.Join("\n", new[]
        {
            "$ErrorActionPreference = 'Stop'",
            $"$tenantId = '{tenant}'",
            $"$subscriptionId = '{subscription}'",
            $"$resourceGroup = '{group}'",
            $"$environmentName = '{environment}'",
            "az login --tenant $tenantId --use-device-code",
            "az account set --subscription $subscriptionId",
            "az account show --query '{tenantId:tenantId,subscriptionId:id,subscriptionName:name}' --output table",
            "git clone https://github.com/M0nado/helios-platform.git",
            "Set-Location ./helios-platform/monado/helios-control",
            "./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Diagnose -TenantId $tenantId -SubscriptionId $subscriptionId -ResourceGroup $resourceGroup -EnvironmentName $environmentName",
            "./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Plan -TenantId $tenantId -SubscriptionId $subscriptionId -ResourceGroup $resourceGroup -EnvironmentName $environmentName",
            "Write-Host 'STOP: review the what-if file and SHA-256. This bootstrap never applies.'"
        });
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script))).ToLowerInvariant();
        return new(script, digest, "diagnose-then-plan", ContainsSecrets: false, AppliesChanges: false);
    }

    public UpgradeProposal CreateUpgradeProposal(UpgradeProposalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var capability = RequireMatch(request.Capability, nameof(request.Capability), CapabilityPattern(), 80).ToLowerInvariant();
        var reason = RequireText(request.Reason, nameof(request.Reason), 500);
        var target = RequireMatch(request.Target, nameof(request.Target), TargetPattern(), 120).ToLowerInvariant();
        var canonical = $"{capability}\n{reason}\n{target}\ndraft-pr-only\nno-auto-merge";
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return new(id, capability, reason, target, "task-branch-and-draft-pull-request", AutomaticApply: false, AutomaticMerge: false,
            new[] { "schema-validation", "security-guardrails", "unit-tests", "integration-tests", "protected-review" });
    }

    private static string RequireGuid(string value, string name) => Guid.TryParse(value, out var parsed)
        ? parsed.ToString()
        : throw new ArgumentException($"{name} must be a GUID.", name);

    private static string RequireMatch(string value, string name, Regex pattern, int maxLength)
    {
        var normalized = RequireText(value, name, maxLength);
        return pattern.IsMatch(normalized) ? normalized : throw new ArgumentException($"{name} contains unsupported characters.", name);
    }

    private static string RequireText(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
        var normalized = value.Trim();
        if (normalized.Length > maxLength || normalized.Any(char.IsControl)) throw new ArgumentException($"{name} is invalid.", name);
        return normalized;
    }

    [GeneratedRegex("^[A-Za-z0-9._()\\-]+$")] private static partial Regex ResourceGroupPattern();
    [GeneratedRegex("^[A-Za-z0-9-]+$")] private static partial Regex SimpleNamePattern();
    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9._/-]*$")] private static partial Regex CapabilityPattern();
    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9._/-]*$")] private static partial Regex TargetPattern();
}
