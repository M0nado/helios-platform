#Requires -Version 7.0
<#
.SYNOPSIS
    Audits and safely repairs critical Windows environment-variable resolution.

.DESCRIPTION
    Audit is the default and performs no mutation. Process repair requires no
    administrator rights and lasts only for the current PowerShell process.
    User repair changes only the current user's environment. Machine repair is
    separately elevation- and confirmation-gated.

    Existing PATH entries are preserved. Required Windows entries are appended
    only when missing. This script never runs SFC, DISM, WMI repair, network
    reset, driver operations, security-policy changes, or a reboot.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [ValidateSet('Audit', 'Process', 'User', 'Machine')]
    [string]$Mode = 'Audit',

    [string]$BaselinePath = (
        Join-Path (
            (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
        ) 'config\windows\environment-baseline.v2.json'
    ),

    [string]$ReportDirectory = (
        Join-Path $env:LOCALAPPDATA 'HELIOS\Evidence\Environment'
    ),

    [string]$Confirmation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

class HeliosEnvironmentError : System.Exception {
    HeliosEnvironmentError([string]$message) : base($message) {}
}

function Test-IsAdministrator {
    if (-not $IsWindows) {
        return $false
    }

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )
}

function Get-WindowsRoot {
    $candidates = @(
        [Environment]::GetEnvironmentVariable('SystemRoot', 'Machine'),
        $env:SystemRoot,
        [Environment]::GetEnvironmentVariable('windir', 'Machine'),
        $env:windir,
        'C:\Windows'
    )

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }
        $expanded = [Environment]::ExpandEnvironmentVariables($candidate)
        if (Test-Path -LiteralPath $expanded -PathType Container) {
            return (Resolve-Path -LiteralPath $expanded).Path.TrimEnd('\')
        }
    }

    throw [HeliosEnvironmentError]::new(
        'Unable to locate a valid Windows root directory.'
    )
}

function Split-EnvironmentPath {
    param([AllowNull()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return @()
    }

    return @(
        $Value.Split(
            [IO.Path]::PathSeparator,
            [StringSplitOptions]::RemoveEmptyEntries
        ) |
            ForEach-Object { $_.Trim().Trim('"') } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Merge-EnvironmentPath {
    param(
        [AllowNull()][string]$Existing,
        [Parameter(Mandatory)][string[]]$Required
    )

    $seen = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    $result = [Collections.Generic.List[string]]::new()

    foreach ($entry in @((Split-EnvironmentPath -Value $Existing) + $Required)) {
        if ([string]::IsNullOrWhiteSpace($entry)) {
            continue
        }
        $normalized = $entry.Trim().Trim('"').TrimEnd('\')
        if (-not $normalized) {
            continue
        }
        if ($seen.Add($normalized)) {
            $result.Add($normalized)
        }
    }

    return ($result -join [IO.Path]::PathSeparator)
}

function Get-EnvironmentSnapshot {
    $names = @(
        'Path',
        'TEMP',
        'TMP',
        'SystemRoot',
        'windir',
        'ComSpec',
        'PATHEXT'
    )

    $snapshot = [ordered]@{}
    foreach ($scope in @('Process', 'User', 'Machine')) {
        $scopeValues = [ordered]@{}
        foreach ($name in $names) {
            $scopeValues[$name] = [Environment]::GetEnvironmentVariable(
                $name,
                [EnvironmentVariableTarget]::$scope
            )
        }
        $snapshot[$scope] = $scopeValues
    }
    return $snapshot
}

function Set-EnvironmentValueIfNeeded {
    param(
        [Parameter(Mandatory)][string]$Name,
        [AllowNull()][string]$Value,
        [Parameter(Mandatory)]
        [ValidateSet('Process', 'User', 'Machine')]
        [string]$Target,
        [Parameter(Mandatory)]
        [Collections.Generic.List[object]]$Changes
    )

    $targetValue = [EnvironmentVariableTarget]::$Target
    $oldValue = [Environment]::GetEnvironmentVariable($Name, $targetValue)
    if ($oldValue -ceq $Value) {
        return
    }

    $change = [ordered]@{
        target = $Target
        name = $Name
        oldValue = $oldValue
        newValue = $Value
        applied = $false
    }

    if ($PSCmdlet.ShouldProcess("$Target environment variable $Name", 'Set value')) {
        [Environment]::SetEnvironmentVariable($Name, $Value, $targetValue)
        $change.applied = $true
    }
    $Changes.Add([pscustomobject]$change)
}

if (-not $IsWindows) {
    throw [HeliosEnvironmentError]::new(
        'This environment repair adapter can run only on Windows.'
    )
}
if (-not (Test-Path -LiteralPath $BaselinePath -PathType Leaf)) {
    throw [HeliosEnvironmentError]::new(
        "Environment baseline not found: $BaselinePath"
    )
}

try {
    $baseline = Get-Content -LiteralPath $BaselinePath -Raw |
        ConvertFrom-Json -Depth 20
}
catch {
    throw [HeliosEnvironmentError]::new(
        "Unable to parse environment baseline: $($_.Exception.Message)"
    )
}
if ($baseline.schemaVersion -ne 2) {
    throw [HeliosEnvironmentError]::new(
        "Unsupported environment baseline schema: $($baseline.schemaVersion)"
    )
}

$windowsRoot = Get-WindowsRoot
$localAppData = [Environment]::GetFolderPath(
    [Environment+SpecialFolder]::LocalApplicationData
)
if (-not $localAppData) {
    throw [HeliosEnvironmentError]::new(
        'Unable to resolve the current user local application-data directory.'
    )
}

$requiredMachinePath = @(
    foreach ($suffix in @($baseline.systemPathSuffixes)) {
        if ([string]::IsNullOrEmpty([string]$suffix)) {
            $windowsRoot
        }
        else {
            Join-Path $windowsRoot ([string]$suffix)
        }
    }
)
$requiredUserPath = @(
    foreach ($suffix in @($baseline.userPathSuffixes)) {
        Join-Path $localAppData ([string]$suffix)
    }
)

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
$before = Get-EnvironmentSnapshot
$backupPath = Join-Path $ReportDirectory "environment-before-$timestamp.json"
$before | ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $backupPath -Encoding UTF8

$toolChecks = @(
    foreach ($tool in @($baseline.criticalTools)) {
        $absolutePath = Join-Path $windowsRoot ([string]$tool.relativePath)
        [ordered]@{
            name = [string]$tool.name
            path = $absolutePath
            exists = Test-Path -LiteralPath $absolutePath -PathType Leaf
            resolvedFromCurrentPath = [bool](
                Get-Command ([string]$tool.name) -ErrorAction SilentlyContinue
            )
        }
    }
)

$changes = [Collections.Generic.List[object]]::new()
$machineConfirmation = [string]$baseline.machineWrite.confirmationPhrase

switch ($Mode) {
    'Audit' {
        # Intentionally read-only.
    }
    'Process' {
        $desiredProcessPath = Merge-EnvironmentPath `
            -Existing ([Environment]::GetEnvironmentVariable('Path', 'Process')) `
            -Required @($requiredMachinePath + $requiredUserPath)

        Set-EnvironmentValueIfNeeded `
            -Name 'Path' `
            -Value $desiredProcessPath `
            -Target 'Process' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'SystemRoot' `
            -Value $windowsRoot `
            -Target 'Process' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'windir' `
            -Value $windowsRoot `
            -Target 'Process' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'ComSpec' `
            -Value (Join-Path $windowsRoot 'System32\cmd.exe') `
            -Target 'Process' `
            -Changes $changes

        $processTemp = Join-Path $localAppData 'Temp'
        New-Item -ItemType Directory -Path $processTemp -Force | Out-Null
        Set-EnvironmentValueIfNeeded `
            -Name 'TEMP' `
            -Value $processTemp `
            -Target 'Process' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'TMP' `
            -Value $processTemp `
            -Target 'Process' `
            -Changes $changes
    }
    'User' {
        $desiredUserPath = Merge-EnvironmentPath `
            -Existing ([Environment]::GetEnvironmentVariable('Path', 'User')) `
            -Required $requiredUserPath
        $userTemp = Join-Path $localAppData 'Temp'
        New-Item -ItemType Directory -Path $userTemp -Force | Out-Null

        Set-EnvironmentValueIfNeeded `
            -Name 'Path' `
            -Value $desiredUserPath `
            -Target 'User' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'TEMP' `
            -Value $userTemp `
            -Target 'User' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'TMP' `
            -Value $userTemp `
            -Target 'User' `
            -Changes $changes
    }
    'Machine' {
        if (-not (Test-IsAdministrator)) {
            throw [HeliosEnvironmentError]::new(
                'Machine environment repair requires an elevated PowerShell session.'
            )
        }
        if ($Confirmation -cne $machineConfirmation) {
            throw [HeliosEnvironmentError]::new(
                "Machine repair requires exact confirmation: $machineConfirmation"
            )
        }

        $desiredMachinePath = Merge-EnvironmentPath `
            -Existing ([Environment]::GetEnvironmentVariable('Path', 'Machine')) `
            -Required $requiredMachinePath
        $machineTemp = Join-Path $windowsRoot 'Temp'
        New-Item -ItemType Directory -Path $machineTemp -Force | Out-Null

        Set-EnvironmentValueIfNeeded `
            -Name 'Path' `
            -Value $desiredMachinePath `
            -Target 'Machine' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'TEMP' `
            -Value $machineTemp `
            -Target 'Machine' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'TMP' `
            -Value $machineTemp `
            -Target 'Machine' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'SystemRoot' `
            -Value $windowsRoot `
            -Target 'Machine' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'windir' `
            -Value $windowsRoot `
            -Target 'Machine' `
            -Changes $changes
        Set-EnvironmentValueIfNeeded `
            -Name 'ComSpec' `
            -Value (Join-Path $windowsRoot 'System32\cmd.exe') `
            -Target 'Machine' `
            -Changes $changes

        $existingPathExt = [Environment]::GetEnvironmentVariable(
            'PATHEXT',
            'Machine'
        )
        if ([string]::IsNullOrWhiteSpace($existingPathExt)) {
            Set-EnvironmentValueIfNeeded `
                -Name 'PATHEXT' `
                -Value '.COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC;.CPL' `
                -Target 'Machine' `
                -Changes $changes
        }
    }
}

$after = Get-EnvironmentSnapshot
$report = [ordered]@{
    schemaVersion = 2
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    computerName = $env:COMPUTERNAME
    mode = $Mode
    isAdministrator = Test-IsAdministrator
    windowsRoot = $windowsRoot
    baselinePath = $BaselinePath
    backupPath = $backupPath
    changes = @($changes)
    toolChecksBeforeNewProcess = $toolChecks
    after = $after
    rebootRequired = $false
    newSessionRecommended = $Mode -in @('User', 'Machine')
    prohibitedOperationsExecuted = @()
}

$reportPath = Join-Path $ReportDirectory "environment-$($Mode.ToLowerInvariant())-$timestamp.json"
$latestPath = Join-Path $ReportDirectory 'environment-latest.json'
$report | ConvertTo-Json -Depth 12 |
    Set-Content -LiteralPath $reportPath -Encoding UTF8
$report | ConvertTo-Json -Depth 12 |
    Set-Content -LiteralPath $latestPath -Encoding UTF8

$report | ConvertTo-Json -Depth 12
Write-Host "HELIOS environment evidence: $reportPath" -ForegroundColor Green
