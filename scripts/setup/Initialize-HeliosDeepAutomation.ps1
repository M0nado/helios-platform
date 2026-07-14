#Requires -Version 7.0
<#
.SYNOPSIS
Initializes HELIOS deep GitHub, Azure CLI, AI hub, and workflow automation guardrails.

.DESCRIPTION
Creates an idempotent automation manifest for the HELIOS Control and Hermes fleet lanes,
checks GitHub CLI and Azure CLI readiness, inventories local and remote branches, and can
optionally apply repository labels and variables through GitHub CLI.

The script is intentionally safe by default: without -Apply it only validates and writes a
local manifest. With -Apply it uses gh and az only when both CLIs are authenticated.

.PARAMETER Owner
GitHub organization or owner.

.PARAMETER Repository
GitHub repository name.

.PARAMETER ManifestPath
Path for the generated automation manifest JSON.

.PARAMETER Apply
Apply supported GitHub repository settings instead of dry-running.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$Owner = 'M0nado',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$Repository = 'helios-platform',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$ManifestPath = 'build/automation/helios-deep-automation-manifest.json',

    [Parameter()]
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-CommandAvailable {
    param([Parameter(Mandatory)][string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Invoke-ExternalJson {
    param(
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $output = & $Command @Arguments 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($output -join ''))) {
        return $null
    }

    return ($output -join "`n") | ConvertFrom-Json -ErrorAction Stop
}

function Get-GitBranches {
    if (-not (Test-CommandAvailable git)) {
        return @()
    }

    $branches = & git for-each-ref --format='%(refname:short)|%(objectname:short)|%(committerdate:iso8601)' refs/heads refs/remotes 2>$null
    if ($LASTEXITCODE -ne 0) {
        return @()
    }

    return @($branches | Where-Object { $_ -and ($_ -notmatch '^origin/HEAD') } | ForEach-Object {
        $parts = $_ -split '\|', 3
        [ordered]@{
            name = $parts[0]
            commit = $parts[1]
            lastCommitUtc = $parts[2]
        }
    })
}

function New-AutomationManifest {
    param(
        [Parameter(Mandatory)][object[]]$Branches,
        [Parameter(Mandatory)][bool]$GitHubReady,
        [Parameter(Mandatory)][bool]$AzureReady
    )

    $lanes = @(
        [ordered]@{
            name = 'helios-control'
            purpose = 'C# and WinUI 3 control-plane orchestration, dashboard automation, release governance'
            owners = @('platform', 'security', 'release')
            validation = @('dotnet restore', 'dotnet build', 'PowerShell syntax', 'GitHub workflow validation')
        },
        [ordered]@{
            name = 'hermes-fleet-production'
            purpose = 'C++ performance back end, F# analytics, Python AIHub, and Hermes XCore fleet specialization'
            owners = @('performance', 'analytics', 'aihub')
            validation = @('CMake configure when present', 'dotnet F# projects when present', 'Python compileall', 'AI governance manifest')
        }
    )

    return [ordered]@{
        schemaVersion = '2026-06-09'
        repository = "$Owner/$Repository"
        generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
        dryRun = -not $Apply.IsPresent
        cliReadiness = [ordered]@{
            git = (Test-CommandAvailable git)
            gh = (Test-CommandAvailable gh)
            ghAuthenticated = $GitHubReady
            az = (Test-CommandAvailable az)
            azAuthenticated = $AzureReady
            pwsh = $true
        }
        branchInventory = $Branches
        integrationLanes = $lanes
        requiredSecrets = @(
            'AZURE_CLIENT_ID',
            'AZURE_TENANT_ID',
            'AZURE_SUBSCRIPTION_ID',
            'HELIOS_AI_ROUTER_KEY',
            'HERMES_FLEET_SIGNING_KEY'
        )
        repositoryVariables = [ordered]@{
            HELIOS_AUTOMATION_MODE = 'deep'
            HELIOS_AI_GOVERNANCE = 'enabled'
            HELIOS_CONTROL_LANE = 'helios-control'
            HERMES_FLEET_LANE = 'hermes-fleet-production'
            AZURE_LOCATION = 'eastus'
        }
        protectionPolicy = [ordered]@{
            requiredChecks = @('discover', 'dotnet-validation', 'python-validation', 'security-and-governance', 'azure-cli-readiness')
            requirePullRequest = $true
            requireSignedCommits = $true
            blockSecretLeaks = $true
        }
    }
}

function Set-GitHubRepositoryAutomation {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param([Parameter(Mandatory)][hashtable]$Variables)

    if (-not $Apply) {
        Write-Host 'Dry run: repository variables and labels were not applied.' -ForegroundColor Yellow
        return
    }

    foreach ($entry in $Variables.GetEnumerator()) {
        if ($PSCmdlet.ShouldProcess("$Owner/$Repository", "Set variable $($entry.Key)")) {
            & gh variable set $entry.Key --repo "$Owner/$Repository" --body ([string]$entry.Value)
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to set GitHub variable $($entry.Key)."
            }
        }
    }

    $labels = @(
        @{ name = 'helios-control'; color = '0E8A16'; description = 'Control plane, C# and WinUI automation lane' },
        @{ name = 'hermes-fleet'; color = '5319E7'; description = 'Hermes fleet production, XCore, performance and AIHub lane' },
        @{ name = 'ai-governance'; color = '1D76DB'; description = 'AI assisted changes requiring governance checks' },
        @{ name = 'azure-automation'; color = 'FBCA04'; description = 'Azure CLI and cloud workflow automation' }
    )

    foreach ($label in $labels) {
        if ($PSCmdlet.ShouldProcess("$Owner/$Repository", "Ensure label $($label.name)")) {
            & gh label create $label.name --repo "$Owner/$Repository" --color $label.color --description $label.description --force
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create or update label $($label.name)."
            }
        }
    }
}

Write-Host 'HELIOS deep automation initialization' -ForegroundColor Cyan
Write-Host "Repository: $Owner/$Repository" -ForegroundColor Cyan

$ghReady = $false
if (Test-CommandAvailable gh) {
    & gh auth status --hostname github.com 1>$null 2>$null
    $ghReady = ($LASTEXITCODE -eq 0)
}

$azReady = $false
if (Test-CommandAvailable az) {
    $account = Invoke-ExternalJson -Command az -Arguments @('account', 'show', '--only-show-errors')
    $azReady = $null -ne $account
}

if ($Apply -and (-not $ghReady)) {
    throw 'Apply mode requires an authenticated GitHub CLI session. Run: gh auth login'
}

if ($Apply -and (-not $azReady)) {
    Write-Warning 'Azure CLI is not authenticated. Run: az login and az account set --subscription <id> before cloud deployment.'
}

$branchInventory = @(Get-GitBranches)
$manifest = New-AutomationManifest -Branches $branchInventory -GitHubReady $ghReady -AzureReady $azReady

$manifestDirectory = Split-Path -Parent $ManifestPath
if ($manifestDirectory) {
    New-Item -ItemType Directory -Path $manifestDirectory -Force | Out-Null
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $ManifestPath -Encoding UTF8
Write-Host "Automation manifest written to $ManifestPath" -ForegroundColor Green

Set-GitHubRepositoryAutomation -Variables ([hashtable]$manifest.repositoryVariables)

Write-Host ''
Write-Host 'Readiness summary:' -ForegroundColor Cyan
Write-Host "  git branches discovered: $($branchInventory.Count)"
Write-Host "  GitHub CLI authenticated: $ghReady"
Write-Host "  Azure CLI authenticated: $azReady"
Write-Host "  Apply mode: $($Apply.IsPresent)"

