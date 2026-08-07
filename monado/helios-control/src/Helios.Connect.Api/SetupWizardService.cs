using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Helios.Connect.Api;

public sealed record SetupBootstrapRequest(string TenantId, string SubscriptionId, string ResourceGroup, string Environment);
public sealed record SetupBootstrapResult(string Script, string ScriptSha256, string Mode, string SubscriptionSelection, IReadOnlyDictionary<string, string> OperatorStages, bool ContainsSecrets, bool AppliesChanges);
public sealed record UpgradeProposalRequest(string Capability, string Reason, string Target = "helios-control");
public sealed record UpgradeProposal(string ProposalId, string Capability, string Reason, string Target, string Promotion, bool AutomaticApply, bool AutomaticMerge, IReadOnlyList<string> RequiredChecks);

public interface ISetupWizardService
{
    SetupBootstrapResult CreateBootstrap(SetupBootstrapRequest request);
    UpgradeProposal CreateUpgradeProposal(UpgradeProposalRequest request);
}

public sealed partial class SetupWizardService : ISetupWizardService
{
    private const string UnconfiguredSourceSha = "0000000000000000000000000000000000000000";
    private static readonly HashSet<string> Environments = new(StringComparer.OrdinalIgnoreCase) { "dev", "test", "preview", "prod" };
    private readonly string _sourceCommitSha;

    public SetupWizardService(IConfiguration? configuration = null)
    {
        var candidate = configuration?["HELIOS_SOURCE_SHA"] ?? Environment.GetEnvironmentVariable("HELIOS_SOURCE_SHA");
        _sourceCommitSha = !string.IsNullOrWhiteSpace(candidate) && SourceCommitPattern().IsMatch(candidate)
            ? candidate.ToLowerInvariant()
            : UnconfiguredSourceSha;
    }

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
            $"$sourceSha = '{_sourceCommitSha}'",
            "if ($sourceSha -eq '0000000000000000000000000000000000000000') { throw 'HELIOS_SOURCE_SHA is not configured on the deployed API; refusing to clone a moving branch.' }",
            "az login --tenant $tenantId --use-device-code",
            "if (-not $subscriptionId) {",
            "  $enabled = @(az account list --all --query \"[?tenantId=='$tenantId' && state=='Enabled'].id\" -o tsv)",
            "  $matching = @($enabled | Where-Object { az account set --subscription $_; (az group exists --name $resourceGroup) -eq 'true' })",
            "  if ($matching.Count -eq 1) { $subscriptionId = $matching[0] } elseif ($enabled.Count -eq 1) { $subscriptionId = $enabled[0] } else { az account list --all --query \"[?tenantId=='$tenantId'].{Name:name,Id:id,State:state}\" -o table; $subscriptionId = Read-Host 'Enter one enabled subscription ID' }",
            "}",
            "if ($subscriptionId -notmatch '^[0-9a-fA-F-]{36}$') { throw 'A valid subscription ID is required.' }",
            "az account set --subscription $subscriptionId",
            "az account show --query '{tenantId:tenantId,subscriptionId:id,subscriptionName:name}' --output table",
            "$containerImage = if ($env:HELIOS_CONTAINER_IMAGE) { $env:HELIOS_CONTAINER_IMAGE } else { Read-Host 'Enter the immutable ACR image reference ending in @sha256:<digest>' }",
            "$registryName = if ($env:HELIOS_CONTAINER_REGISTRY_NAME) { $env:HELIOS_CONTAINER_REGISTRY_NAME } else { Read-Host 'Enter the Azure Container Registry resource name' }",
            "$entraClientId = if ($env:HELIOS_ENTRA_CLIENT_ID) { $env:HELIOS_ENTRA_CLIENT_ID } else { Read-Host 'Enter the HELIOS Entra application client ID' }",
            "$allowedPrincipalObjectId = if ($env:HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID) { $env:HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID } else { Read-Host 'Enter the allowed principal object ID' }",
            "$containerAppsInfrastructureSubnetId = if ($env:HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID) { $env:HELIOS_CONTAINER_APPS_INFRASTRUCTURE_SUBNET_ID } elseif ($environmentName -eq 'prod') { Read-Host 'Enter the delegated Container Apps infrastructure subnet resource ID' } else { '' }",
            "git clone --filter=blob:none --no-checkout https://github.com/M0nado/helios-platform.git",
            "git -C ./helios-platform fetch --depth 1 origin $sourceSha",
            "git -C ./helios-platform checkout --detach $sourceSha",
            "if ((git -C ./helios-platform rev-parse HEAD) -ne $sourceSha) { throw 'Checked-out HELIOS source does not match the API build SHA.' }",
            "Set-Location ./helios-platform/monado/helios-control",
            "$evidence = Join-Path $HOME 'clouddrive/helios-evidence'",
            "./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Diagnose -TenantId $tenantId -SubscriptionId $subscriptionId -ResourceGroup $resourceGroup -EnvironmentName $environmentName -EvidenceDirectory $evidence",
            "./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Plan -TenantId $tenantId -SubscriptionId $subscriptionId -ResourceGroup $resourceGroup -EnvironmentName $environmentName -ContainerImage $containerImage -ContainerRegistryName $registryName -EntraClientId $entraClientId -AllowedPrincipalObjectId $allowedPrincipalObjectId -ContainerAppsInfrastructureSubnetId $containerAppsInfrastructureSubnetId -SourceCommitSha $sourceSha -EvidenceDirectory $evidence",
            "Write-Host 'STOP: review the what-if file and SHA-256. This bootstrap never applies.'"
        });
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script))).ToLowerInvariant();
        var stages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["inventory"] = "Read resource metadata and HELIOS ownership tags only.",
            ["identity-readiness"] = "Inspect Entra, managed-identity, OIDC, and RBAC readiness without granting access.",
            ["deployment-preview"] = "Validate Bicep and persist canonical ARM what-if evidence to Cloud Shell storage.",
            ["health-verification"] = "Verify HTTPS, Entra boundary, MCP inventory, telemetry, and delivery receipts."
        };
        return new(script, digest, "diagnose-then-plan", subscription is null ? "unique-resource-group-match-then-interactive-fallback" : "explicit", stages, ContainsSecrets: false, AppliesChanges: false);
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

    [GeneratedRegex("^[0-9a-fA-F]{40}$")] private static partial Regex SourceCommitPattern();
    [GeneratedRegex("^[A-Za-z0-9._()\\-]+$")] private static partial Regex ResourceGroupPattern();
    [GeneratedRegex("^[A-Za-z0-9-]+$")] private static partial Regex SimpleNamePattern();
    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9._/-]*$")] private static partial Regex CapabilityPattern();
    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9._/-]*$")] private static partial Regex TargetPattern();
}
