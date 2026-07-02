<#
.SYNOPSIS
Validates and bootstraps the HELIOS/HERMES specialist workstation prerequisites.

.DESCRIPTION
Checks the local repository, Git branch/remote state, Azure CLI availability,
Python, .NET, PowerShell, and optional C++/WinUI build tools. The script is
safe by default: it reports missing prerequisites and prints setup guidance
without installing packages or changing Azure subscriptions unless explicitly
requested.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string[]]$FocusBranches = @('helios-control', 'hermes-fleet-production'),
    [switch]$LoginAzure,
    [switch]$ConfigureDefaults,
    [string]$AzureSubscription,
    [string]$AzureLocation = 'eastus',
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-CommandAvailable {
    param([Parameter(Mandatory)][string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function New-CheckResult {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Status,
        [string]$Detail = '',
        [string]$Remediation = ''
    )

    [pscustomobject]@{
        Name = $Name
        Status = $Status
        Detail = $Detail
        Remediation = $Remediation
    }
}

function Invoke-GitText {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed: $output"
    }
    return ($output | Out-String).Trim()
}

$results = [System.Collections.Generic.List[object]]::new()

if (-not (Test-CommandAvailable git)) {
    $results.Add((New-CheckResult 'Git' 'Fail' 'git is not available.' 'Install Git and rerun from the repository root.'))
} else {
    $repoRoot = Invoke-GitText @('rev-parse', '--show-toplevel')
    $currentBranch = Invoke-GitText @('rev-parse', '--abbrev-ref', 'HEAD')
    $status = Invoke-GitText @('status', '--short')
    $remoteNames = Invoke-GitText @('remote')
    $allBranches = Invoke-GitText @('branch', '--all', '--no-color')

    $results.Add((New-CheckResult 'Repository' 'Pass' "Root: $repoRoot; branch: $currentBranch" ''))
    $results.Add((New-CheckResult 'Working tree' ($(if ([string]::IsNullOrWhiteSpace($status)) { 'Pass' } else { 'Warn' })) $status 'Commit or stash local changes before branch integration.'))
    $results.Add((New-CheckResult 'Git remotes' ($(if ([string]::IsNullOrWhiteSpace($remoteNames)) { 'Warn' } else { 'Pass' })) $remoteNames 'Add an origin/upstream remote before fetching or pushing.'))

    foreach ($branch in $FocusBranches) {
        $escaped = [regex]::Escape($branch)
        $present = $allBranches -match "(^|/)${escaped}$"
        $results.Add((New-CheckResult "Focus branch: $branch" ($(if ($present) { 'Pass' } else { 'Warn' })) ($(if ($present) { 'Branch ref found.' } else { 'Branch ref not found locally.' })) 'Fetch remotes or create the branch before attempting merges.'))
    }
}

foreach ($tool in @(
    @{ Name = 'Azure CLI'; Command = 'az'; Remediation = 'Install Azure CLI 2.53+ from Microsoft Learn.' },
    @{ Name = '.NET SDK'; Command = 'dotnet'; Remediation = 'Install the .NET SDK required by HELIOS projects.' },
    @{ Name = 'Python'; Command = 'python'; Remediation = 'Install Python 3.11+ for AIHub integration and analytics tooling.' },
    @{ Name = 'PowerShell'; Command = 'pwsh'; Remediation = 'Install PowerShell 7+ for cross-platform automation.' },
    @{ Name = 'CMake'; Command = 'cmake'; Remediation = 'Install CMake for C++ performance backend builds.' }
)) {
    if (Test-CommandAvailable $tool.Command) {
        $version = (& $tool.Command --version 2>&1 | Select-Object -First 1 | Out-String).Trim()
        $results.Add((New-CheckResult $tool.Name 'Pass' $version ''))
    } else {
        $results.Add((New-CheckResult $tool.Name 'Warn' "$($tool.Command) not found." $tool.Remediation))
    }
}

if (Test-CommandAvailable az) {
    if ($LoginAzure -and $PSCmdlet.ShouldProcess('Azure CLI', 'Run az login')) {
        & az login | Out-Null
    }

    $accountJson = & az account show --output json 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($accountJson)) {
        $account = $accountJson | ConvertFrom-Json
        $results.Add((New-CheckResult 'Azure account' 'Pass' "Signed in as $($account.user.name); subscription: $($account.name)" ''))
    } else {
        $results.Add((New-CheckResult 'Azure account' 'Warn' 'Azure CLI is installed but not signed in.' 'Run ./scripts/setup/setup-specialist-environment.ps1 -LoginAzure.'))
    }

    if ($ConfigureDefaults) {
        if (-not [string]::IsNullOrWhiteSpace($AzureSubscription) -and $PSCmdlet.ShouldProcess('Azure CLI', "Set subscription $AzureSubscription")) {
            & az account set --subscription $AzureSubscription
        }
        if ($PSCmdlet.ShouldProcess('Azure CLI', "Set default location $AzureLocation")) {
            & az configure --defaults location=$AzureLocation
        }
    }
}

if ($Json) {
    $results | ConvertTo-Json -Depth 4
} else {
    $results | Format-Table -AutoSize
}

$failed = @($results | Where-Object Status -eq 'Fail')
if ($failed.Count -gt 0) {
    exit 1
}
