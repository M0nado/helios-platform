#Requires -Version 7.0
<#
.SYNOPSIS
    Installs HELIOS startup posture audit and Defender scan scheduled tasks.

.DESCRIPTION
    One-time administrator bootstrap. Installs only:

    - a SYSTEM startup posture audit;
    - a daily Microsoft Defender quick scan;
    - an optional weekly full scan when explicitly requested.

    Microsoft Defender Offline is never scheduled. The installer copies the
    reviewed scripts to ProgramData, writes a task-definition backup, and never
    restarts the computer.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [string]$InstallRoot = (
        Join-Path $env:ProgramData 'HELIOS\Security'
    ),
    [ValidatePattern('^([01]\d|2[0-3]):[0-5]\d$')]
    [string]$DailyQuickScanTime = '12:30',
    [switch]$InstallWeeklyFullScan,
    [ValidatePattern('^([01]\d|2[0-3]):[0-5]\d$')]
    [string]$WeeklyFullScanTime = '03:00',
    [ValidateSet(
        'Sunday', 'Monday', 'Tuesday', 'Wednesday',
        'Thursday', 'Friday', 'Saturday'
    )]
    [string]$WeeklyFullScanDay = 'Sunday',
    [Parameter(Mandatory)]
    [string]$Confirmation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$requiredConfirmation = 'INSTALL HELIOS SECURITY TASKS'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )
}

function Convert-ToInvariantTime {
    param([Parameter(Mandatory)][string]$Value)

    return [datetime]::ParseExact(
        $Value,
        'HH:mm',
        [Globalization.CultureInfo]::InvariantCulture
    )
}

if (-not $IsWindows) {
    throw 'HELIOS security task installation can run only on Windows.'
}
if (-not (Test-IsAdministrator)) {
    throw 'Run this script from an elevated PowerShell session.'
}
if ($Confirmation -cne $requiredConfirmation) {
    throw "Exact confirmation required: $requiredConfirmation"
}

foreach ($command in @(
        'Register-ScheduledTask',
        'New-ScheduledTaskAction',
        'New-ScheduledTaskTrigger',
        'New-ScheduledTaskPrincipal',
        'New-ScheduledTaskSettingsSet'
    )) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required ScheduledTasks command is unavailable: $command"
    }
}

$sourceRoot = $PSScriptRoot
$scriptRoot = Join-Path $InstallRoot 'Scripts'
$reportRoot = Join-Path $InstallRoot 'Reports'
$stateRoot = Join-Path $InstallRoot 'State'
$backupRoot = Join-Path $InstallRoot 'TaskBackups'
foreach ($path in @(
        $InstallRoot,
        $scriptRoot,
        $reportRoot,
        $stateRoot,
        $backupRoot
    )) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

$sourceScripts = @(
    'Get-HeliosBootSecurityPosture.ps1',
    'Set-HeliosWindowsSecurityBaseline.ps1',
    'Invoke-HeliosDefenderRecovery.ps1'
)
foreach ($name in $sourceScripts) {
    $source = Join-Path $sourceRoot $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required reviewed source script is missing: $source"
    }
    $destination = Join-Path $scriptRoot $name
    if ($PSCmdlet.ShouldProcess(
            $destination,
            'Install reviewed HELIOS security script'
        )) {
        Copy-Item -LiteralPath $source -Destination $destination -Force
        Unblock-File -LiteralPath $destination -ErrorAction SilentlyContinue
    }
}

$pwshCommand = Get-Command pwsh.exe -ErrorAction SilentlyContinue
$powerShellExecutable = if ($pwshCommand) {
    $pwshCommand.Source
}
else {
    (Get-Command powershell.exe -ErrorAction Stop).Source
}

$taskPath = '\HELIOS\Security\'
$principal = New-ScheduledTaskPrincipal `
    -UserId 'SYSTEM' `
    -LogonType ServiceAccount `
    -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Hours 6)

$auditScript = Join-Path $scriptRoot 'Get-HeliosBootSecurityPosture.ps1'
$recoveryScript = Join-Path $scriptRoot 'Invoke-HeliosDefenderRecovery.ps1'

$auditAction = New-ScheduledTaskAction `
    -Execute $powerShellExecutable `
    -Argument (
        '-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned ' +
        "-File `"$auditScript`" -OutputDirectory `"$reportRoot`""
    )
$auditTrigger = New-ScheduledTaskTrigger -AtStartup

$quickAction = New-ScheduledTaskAction `
    -Execute $powerShellExecutable `
    -Argument (
        '-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned ' +
        "-File `"$recoveryScript`" -Mode QuickScan " +
        "-EvidenceDirectory `"$reportRoot`" " +
        "-StateDirectory `"$stateRoot`""
    )
$quickTrigger = New-ScheduledTaskTrigger `
    -Daily `
    -At (Convert-ToInvariantTime -Value $DailyQuickScanTime)

$definitions = [Collections.Generic.List[object]]::new()
$definitions.Add([ordered]@{
    name = 'HELIOS Boot Security Audit'
    action = $auditAction
    trigger = $auditTrigger
    purpose = 'Non-mutating startup posture evidence'
})
$definitions.Add([ordered]@{
    name = 'HELIOS Defender Daily Quick Scan'
    action = $quickAction
    trigger = $quickTrigger
    purpose = 'Daily Microsoft Defender quick scan'
})

if ($InstallWeeklyFullScan) {
    $fullAction = New-ScheduledTaskAction `
        -Execute $powerShellExecutable `
        -Argument (
            '-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned ' +
            "-File `"$recoveryScript`" -Mode FullScan " +
            "-EvidenceDirectory `"$reportRoot`" " +
            "-StateDirectory `"$stateRoot`" " +
            "-Confirmation `"RUN HELIOS FULL DEFENDER SCAN`""
        )
    $fullTrigger = New-ScheduledTaskTrigger `
        -Weekly `
        -WeeksInterval 1 `
        -DaysOfWeek $WeeklyFullScanDay `
        -At (Convert-ToInvariantTime -Value $WeeklyFullScanTime)

    $definitions.Add([ordered]@{
        name = 'HELIOS Defender Weekly Full Scan'
        action = $fullAction
        trigger = $fullTrigger
        purpose = 'Explicitly opted-in weekly Microsoft Defender full scan'
    })
}

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$existing = @(
    foreach ($definition in $definitions) {
        $task = Get-ScheduledTask `
            -TaskPath $taskPath `
            -TaskName $definition.name `
            -ErrorAction SilentlyContinue
        if ($task) {
            [ordered]@{
                name = $definition.name
                xml = Export-ScheduledTask `
                    -TaskPath $taskPath `
                    -TaskName $definition.name
            }
        }
    }
)
$backupPath = Join-Path $backupRoot "scheduled-tasks-before-$timestamp.json"
$existing | ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $backupPath -Encoding UTF8

foreach ($definition in $definitions) {
    $target = "$taskPath$($definition.name)"
    if ($PSCmdlet.ShouldProcess($target, 'Register scheduled task')) {
        Register-ScheduledTask `
            -TaskPath $taskPath `
            -TaskName $definition.name `
            -Action $definition.action `
            -Trigger $definition.trigger `
            -Principal $principal `
            -Settings $settings `
            -Description $definition.purpose `
            -Force | Out-Null
    }
}

$evidence = [ordered]@{
    schemaVersion = 2
    installedUtc = (Get-Date).ToUniversalTime().ToString('o')
    installRoot = $InstallRoot
    powerShellExecutable = $powerShellExecutable
    taskPath = $taskPath
    tasks = @(
        foreach ($definition in $definitions) {
            [ordered]@{
                name = $definition.name
                purpose = $definition.purpose
            }
        }
    )
    weeklyFullScanInstalled = [bool]$InstallWeeklyFullScan
    offlineScanScheduled = $false
    automaticRebootConfigured = $false
    backupPath = $backupPath
}
$evidencePath = Join-Path $reportRoot "security-task-install-$timestamp.json"
$evidence | ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $evidencePath -Encoding UTF8
$evidence | ConvertTo-Json -Depth 10
Write-Host "HELIOS security task evidence: $evidencePath" -ForegroundColor Green
