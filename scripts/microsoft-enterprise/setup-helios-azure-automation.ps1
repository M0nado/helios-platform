<#
.SYNOPSIS
Idempotent Azure + local automation bootstrap for HELIOS Platform.

.DESCRIPTION
Creates the Azure-side baseline needed for HELIOS automation while also writing a local
runtime manifest that the WinUI/C#/C++/F#/Python components and Copilot Studio flows can
consume. The script is intentionally non-interactive for CI, Cloud Shell, and automation
agents. Run with -WhatIf first, then run without -WhatIf after `az login`.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$EnvironmentName = "prod",
    [string]$ResourceGroupName = "rg-helios-platform-prod",
    [string]$Location = "eastus2",
    [string]$LocalConfigPath = "config/azure-automation.local.json",
    [string]$TagsJson = '{"system":"helios","owner":"platform","workload":"automation"}',
    [switch]$SkipCopilotStudioManifest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[HELIOS Azure Setup] $Message" -ForegroundColor Cyan
}

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. Install Azure CLI and retry."
    }
}

function Invoke-AzCliJson {
    param([string[]]$Arguments)
    $output = & az @Arguments --only-show-errors 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed: $output"
    }
    if ([string]::IsNullOrWhiteSpace($output)) { return $null }
    return $output | ConvertFrom-Json
}

function Invoke-AzCliText {
    param([string[]]$Arguments)
    $output = & az @Arguments --only-show-errors 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed: $output"
    }
    return $output
}

Require-Command az

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$localConfigFullPath = Join-Path $repoRoot $LocalConfigPath
$localConfigDirectory = Split-Path $localConfigFullPath -Parent
if (-not (Test-Path $localConfigDirectory)) {
    New-Item -ItemType Directory -Path $localConfigDirectory -Force | Out-Null
}

$tags = $TagsJson | ConvertFrom-Json -AsHashtable
$tagArguments = @()
foreach ($tag in $tags.GetEnumerator()) { $tagArguments += "$($tag.Key)=$($tag.Value)" }

Write-Step "Validating Azure CLI login and active subscription"
$account = Invoke-AzCliJson @("account", "show", "--output", "json")
$subscriptionId = $account.id
$tenantId = $account.tenantId

$unique = ($subscriptionId.Replace('-', '') + $EnvironmentName.ToLowerInvariant())
if ($unique.Length -gt 12) { $unique = $unique.Substring(0, 12) }
$storageName = ("sthelios" + $unique).ToLowerInvariant()
$keyVaultName = ("kv-helios-" + $unique).ToLowerInvariant()
$workspaceName = "law-helios-$EnvironmentName"
$appInsightsName = "appi-helios-$EnvironmentName"
$eventHubNamespace = "evhns-helios-$EnvironmentName-$unique"
$eventHubName = "helios-automation-events"
$serviceBusNamespace = "sbns-helios-$EnvironmentName-$unique"
$queueName = "helios-automation-jobs"
$managedIdentityName = "id-helios-automation-$EnvironmentName"

if ($PSCmdlet.ShouldProcess($ResourceGroupName, "Create/update HELIOS Azure automation baseline")) {
    Write-Step "Creating resource group $ResourceGroupName in $Location"
    $cliArgs = @("group", "create", "--name", $ResourceGroupName, "--location", $Location, "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null

    Write-Step "Creating Log Analytics workspace"
    $cliArgs = @("monitor", "log-analytics", "workspace", "create", "--resource-group", $ResourceGroupName, "--workspace-name", $workspaceName, "--location", $Location, "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null

    Write-Step "Creating Application Insights component"
    $cliArgs = @("monitor", "app-insights", "component", "create", "--app", $appInsightsName, "--location", $Location, "--resource-group", $ResourceGroupName, "--workspace", $workspaceName, "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null

    Write-Step "Creating Key Vault for secret references"
    $cliArgs = @("keyvault", "create", "--name", $keyVaultName, "--resource-group", $ResourceGroupName, "--location", $Location, "--enable-rbac-authorization", "true", "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null

    Write-Step "Creating secure storage account for artifacts and local/cloud sync"
    $cliArgs = @("storage", "account", "create", "--name", $storageName, "--resource-group", $ResourceGroupName, "--location", $Location, "--sku", "Standard_ZRS", "--kind", "StorageV2", "--min-tls-version", "TLS1_2", "--allow-blob-public-access", "false", "--https-only", "true", "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null

    Write-Step "Creating managed identity for HELIOS automation workers"
    $cliArgs = @("identity", "create", "--name", $managedIdentityName, "--resource-group", $ResourceGroupName, "--location", $Location, "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null

    Write-Step "Creating Event Hubs namespace and event hub"
    $cliArgs = @("eventhubs", "namespace", "create", "--name", $eventHubNamespace, "--resource-group", $ResourceGroupName, "--location", $Location, "--sku", "Standard", "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null
    Invoke-AzCliText @("eventhubs", "eventhub", "create", "--name", $eventHubName, "--namespace-name", $eventHubNamespace, "--resource-group", $ResourceGroupName, "--partition-count", "4", "--message-retention", "7") | Out-Null

    Write-Step "Creating Service Bus queue for automation jobs"
    $cliArgs = @("servicebus", "namespace", "create", "--name", $serviceBusNamespace, "--resource-group", $ResourceGroupName, "--location", $Location, "--sku", "Standard", "--tags") + $tagArguments
    Invoke-AzCliText $cliArgs | Out-Null
    Invoke-AzCliText @("servicebus", "queue", "create", "--name", $queueName, "--namespace-name", $serviceBusNamespace, "--resource-group", $ResourceGroupName, "--max-delivery-count", "5", "--default-message-time-to-live", "P14D") | Out-Null
}

$manifest = [ordered]@{
    schema = "https://helios.local/schemas/azure-automation-manifest.v1.json"
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    environment = $EnvironmentName
    tenantId = $tenantId
    subscriptionId = $subscriptionId
    resourceGroup = $ResourceGroupName
    location = $Location
    resources = [ordered]@{
        managedIdentity = $managedIdentityName
        keyVault = $keyVaultName
        storageAccount = $storageName
        logAnalyticsWorkspace = $workspaceName
        applicationInsights = $appInsightsName
        eventHubNamespace = $eventHubNamespace
        eventHub = $eventHubName
        serviceBusNamespace = $serviceBusNamespace
        serviceBusQueue = $queueName
    }
    localIntegration = [ordered]@{
        heliosControl = "WinUI 3/C# control surface reads this file for Azure resource names."
        hermesFleetProduction = "C++/Python workers use Event Hubs and Service Bus names for high-throughput automation."
        xcoreSpecialist = "F# analytics and prediction jobs use Log Analytics/Application Insights references."
    }
}

if ($PSCmdlet.ShouldProcess($localConfigFullPath, "Write HELIOS local Azure automation manifest")) {
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $localConfigFullPath -Encoding utf8
    Write-Step "Wrote $localConfigFullPath"
}

if (-not $SkipCopilotStudioManifest) {
    $copilotPath = Join-Path $repoRoot "microsoft-ecosystem/copilot/copilot-studio-helios-actions.json"
    $copilotManifest = [ordered]@{
        name = "HELIOS Platform Automation"
        description = "Copilot Studio action map for local + Azure HELIOS automation."
        environment = $EnvironmentName
        actions = @(
            @{ name = "submitAutomationJob"; target = "serviceBusQueue"; resource = $queueName; purpose = "Queue local or cloud automation work." },
            @{ name = "publishFleetEvent"; target = "eventHub"; resource = $eventHubName; purpose = "Publish Hermes fleet telemetry and state changes." },
            @{ name = "queryOperationalHealth"; target = "logAnalyticsWorkspace"; resource = $workspaceName; purpose = "Answer operational health questions from telemetry." },
            @{ name = "retrieveSecretReference"; target = "keyVault"; resource = $keyVaultName; purpose = "Resolve approved secret references without exposing values." }
        )
    }
    if ($PSCmdlet.ShouldProcess($copilotPath, "Write Copilot Studio action manifest")) {
        $copilotManifest | ConvertTo-Json -Depth 8 | Set-Content -Path $copilotPath -Encoding utf8
        Write-Step "Wrote $copilotPath"
    }
}

Write-Step "Azure automation bootstrap completed. Review RBAC assignments before production rollout."
