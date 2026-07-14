#Requires -Version 5.1
<#
.SYNOPSIS
    Installs durable HELIOS startup audit and Defender scan tasks.

.DESCRIPTION
    Copies the reviewed security scripts into ProgramData and registers:
      - a SYSTEM startup posture audit;
      - a daily Defender quick scan;
      - a weekly Defender full scan.
    It never schedules an automatic offline/rootkit reboot scan.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [string]$InstallRoot = "$env:ProgramData\HELIOS\Security",
    [string]$DailyQuickScanTime = '12:30',
    [string]$WeeklyFullScanTime = '03:00',
    [ValidateSet('Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday')]
    [string]$WeeklyFullScanDay = 'Sunday'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    throw 'Run this script from an elevated PowerShell session.'
}

$sourceRoot = $PSScriptRoot
$scriptRoot = Join-Path $InstallRoot 'Scripts'
$reportRoot = Join-Path $InstallRoot 'Reports'
$stateRoot = Join-Path $InstallRoot 'State'
foreach ($path in @($InstallRoot, $scriptRoot, $reportRoot, $stateRoot)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

$files = @(
    'Get-HeliosBootSecurityPosture.ps1',
    'Set-HeliosWindowsSecurityBaseline.ps1',
    'Invoke-HeliosRootkitRecovery.ps1'
)
foreach ($file in $files) {
    $source = Join-Path $sourceRoot $file
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing required source script: $source" }
    if ($PSCmdlet.ShouldProcess((Join-Path $scriptRoot $file), 'Install reviewed HELIOS security script')) {
        $destination = Join-Path $scriptRoot $file
        Copy-Item -LiteralPath $source -Destination $destination -Force
        Unblock-File -LiteralPath $destination -ErrorAction SilentlyContinue
    }
}

$pwsh = (Get-Command pwsh.exe -ErrorAction SilentlyContinue).Source
if (-not $pwsh) { $pwsh = (Get-Command powershell.exe -ErrorAction Stop).Source }
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 4)

$auditScript = Join-Path $scriptRoot 'Get-HeliosBootSecurityPosture.ps1'
$scanScript = Join-Path $scriptRoot 'Invoke-HeliosRootkitRecovery.ps1'
$auditAction = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File `"$auditScript`" -OutputDirectory `"$reportRoot`""
$auditTrigger = New-ScheduledTaskTrigger -AtStartup

$quickAction = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File `"$scanScript`" -Mode QuickScan -StateDirectory `"$stateRoot`""
$quickTrigger = New-ScheduledTaskTrigger -Daily -At ([datetime]::ParseExact($DailyQuickScanTime, 'HH:mm', [Globalization.CultureInfo]::InvariantCulture))

$fullAction = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File `"$scanScript`" -Mode FullScan -StateDirectory `"$stateRoot`""
$fullTrigger = New-ScheduledTaskTrigger -Weekly -WeeksInterval 1 -DaysOfWeek $WeeklyFullScanDay -At ([datetime]::ParseExact($WeeklyFullScanTime, 'HH:mm', [Globalization.CultureInfo]::InvariantCulture))

$tasks = @(
    [pscustomobject]@{ Name='HELIOS Boot Security Audit'; Action=$auditAction; Trigger=$auditTrigger },
    [pscustomobject]@{ Name='HELIOS Defender Daily Quick Scan'; Action=$quickAction; Trigger=$quickTrigger },
    [pscustomobject]@{ Name='HELIOS Defender Weekly Full Scan'; Action=$fullAction; Trigger=$fullTrigger }
)

foreach ($task in $tasks) {
    if ($PSCmdlet.ShouldProcess("\HELIOS\Security\$($task.Name)", 'Register scheduled task')) {
        Register-ScheduledTask -TaskPath '\HELIOS\Security\' -TaskName $task.Name -Action $task.Action -Trigger $task.Trigger -Principal $principal -Settings $settings -Description 'HELIOS guarded Windows security automation' -Force | Out-Null
    }
}

[pscustomobject]@{
    InstalledRoot = $InstallRoot
    PowerShellHost = $pwsh
    Tasks = @($tasks.Name)
    OfflineScanScheduled = $false
    Note = 'Offline rootkit scanning remains an explicit operator action and is never scheduled automatically.'
} | ConvertTo-Json -Depth 5
