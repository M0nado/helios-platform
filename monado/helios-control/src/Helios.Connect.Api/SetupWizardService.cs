using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Helios.Connect.Api;

public sealed record SetupBootstrapRequest(string TenantId, string SubscriptionId, string ResourceGroup, string Environment);
public sealed record SetupBootstrapResult(string Script, string ScriptSha256, string Mode, string SubscriptionSelection, IReadOnlyDictionary<string, string> ShellPackets, bool ContainsSecrets, bool AppliesChanges);
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
        var subscription = string.IsNullOrWhiteSpace(request.SubscriptionId) ? null : RequireGuid(request.SubscriptionId, nameof(request.SubscriptionId));
        var group = RequireMatch(request.ResourceGroup, nameof(request.ResourceGroup), ResourceGroupPattern(), 90);
        var environment = RequireMatch(request.Environment, nameof(request.Environment), SimpleNamePattern(), 16).ToLowerInvariant();
        if (!Environments.Contains(environment)) throw new ArgumentException("Environment must be dev, test, preview, or prod.", nameof(request.Environment));

        var script = string.Join("\n", new[]
        {
            "$ErrorActionPreference = 'Stop'",
            $"$tenantId = '{tenant}'",
            $"$subscriptionId = '{subscription ?? string.Empty}'",
            $"$resourceGroup = '{group}'",
            $"$environmentName = '{environment}'",
            "az login --tenant $tenantId --use-device-code",
            "if (-not $subscriptionId) {",
            "  $enabled = @(az account list --all --query \"[?tenantId=='$tenantId' && state=='Enabled'].id\" -o tsv)",
            "  $matching = @($enabled | Where-Object { az account set --subscription $_; (az group exists --name $resourceGroup) -eq 'true' })",
            "  if ($matching.Count -eq 1) { $subscriptionId = $matching[0] } elseif ($enabled.Count -eq 1) { $subscriptionId = $enabled[0] } else { az account list --all --query \"[?tenantId=='$tenantId'].{Name:name,Id:id,State:state}\" -o table; $subscriptionId = Read-Host 'Enter one enabled subscription ID' }",
            "}",
            "if ($subscriptionId -notmatch '^[0-9a-fA-F-]{36}$') { throw 'A valid subscription ID is required.' }",
            "az account set --subscription $subscriptionId",
            "az account show --query '{tenantId:tenantId,subscriptionId:id,subscriptionName:name}' --output table",
            "git clone https://github.com/M0nado/helios-platform.git",
            "Set-Location ./helios-platform/monado/helios-control",
            "$evidence = Join-Path $HOME 'clouddrive/helios-evidence'",
            "./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Diagnose -TenantId $tenantId -SubscriptionId $subscriptionId -ResourceGroup $resourceGroup -EnvironmentName $environmentName -EvidenceDirectory $evidence",
            "./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Plan -TenantId $tenantId -SubscriptionId $subscriptionId -ResourceGroup $resourceGroup -EnvironmentName $environmentName -EvidenceDirectory $evidence",
            "Write-Host 'STOP: review the what-if file and SHA-256. This bootstrap never applies.'"
        });
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script))).ToLowerInvariant();
        var packets = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["inventory"] = "Read resource metadata and HELIOS ownership tags only.",
            ["identity-readiness"] = "Inspect Entra, managed-identity, OIDC, and RBAC readiness without granting access.",
            ["deployment-preview"] = "Validate Bicep and persist canonical ARM what-if evidence to Cloud Shell storage.",
            ["health-verification"] = "Verify HTTPS, Entra boundary, MCP inventory, telemetry, and delivery receipts."
        };
        return new(script, digest, "diagnose-then-plan", subscription is null ? "unique-resource-group-match-then-interactive-fallback" : "explicit", packets, ContainsSecrets: false, AppliesChanges: false);
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
