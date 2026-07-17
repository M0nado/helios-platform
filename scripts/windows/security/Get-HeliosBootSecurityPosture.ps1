#Requires -Version 5.1
<#
.SYNOPSIS
    Collects a non-mutating HELIOS Windows boot and endpoint-security posture report.

.DESCRIPTION
    This script never changes security settings. It records Secure Boot, TPM,
    Defender, firewall, WinRE, BitLocker, VBS/HVCI, startup persistence, and
    recent Defender/Code Integrity events into a JSON report.
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = "$env:ProgramData\HELIOS\Security\Reports",
    [int]$EventLookbackHours = 24,
    [switch]$FailOnCritical
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Registry: Read-only inspection of approved HKLM/HKCU startup, Device Guard,
# vulnerable-driver blocklist, and LSA configuration. This script performs no
# registry writes, so rollback is not applicable.

function Invoke-HeliosSafeCheck {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$ScriptBlock
    )

    try {
        [pscustomobject]@{
            Name = $Name
            Success = $true
            Value = & $ScriptBlock
            Error = $null
        }
    }
    catch {
        [pscustomobject]@{
            Name = $Name
            Success = $false
            Value = $null
            Error = $_.Exception.Message
        }
    }
}

function Get-RegistryRunEntries {
    $paths = @(
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Run',
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\RunOnce',
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run',
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\RunOnce'
    )

    foreach ($path in $paths) {
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $item = Get-ItemProperty -LiteralPath $path
        foreach ($property in $item.PSObject.Properties) {
            if ($property.Name -like 'PS*') { continue }
            [pscustomobject]@{
                RegistryPath = $path
                Name = $property.Name
                Command = [string]$property.Value
            }
        }
    }
}

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$systemRoot = if ($env:SystemRoot) { $env:SystemRoot } else { 'C:\Windows' }
$system32 = Join-Path $systemRoot 'System32'
$reagentc = Join-Path $system32 'reagentc.exe'
$manageBde = Join-Path $system32 'manage-bde.exe'
$bcdedit = Join-Path $system32 'bcdedit.exe'

$checks = [ordered]@{}
$checks.OperatingSystem = Invoke-HeliosSafeCheck -Name 'OperatingSystem' -ScriptBlock {
    Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, BuildNumber, OSArchitecture, LastBootUpTime
}
$checks.SecureBoot = Invoke-HeliosSafeCheck -Name 'SecureBoot' -ScriptBlock {
    [pscustomobject]@{ Enabled = [bool](Confirm-SecureBootUEFI) }
}
$checks.Tpm = Invoke-HeliosSafeCheck -Name 'TPM' -ScriptBlock {
    Get-Tpm | Select-Object TpmPresent, TpmReady, TpmEnabled, TpmActivated, ManagedAuthLevel, ManufacturerIdTxt, ManufacturerVersion
}
$checks.DeviceGuard = Invoke-HeliosSafeCheck -Name 'DeviceGuard' -ScriptBlock {
    Get-CimInstance -Namespace 'root\Microsoft\Windows\DeviceGuard' -ClassName Win32_DeviceGuard |
        Select-Object VirtualizationBasedSecurityStatus, SecurityServicesConfigured, SecurityServicesRunning,
            AvailableSecurityProperties, RequiredSecurityProperties, CodeIntegrityPolicyEnforcementStatus,
            UsermodeCodeIntegrityPolicyEnforcementStatus
}
$checks.DefenderStatus = Invoke-HeliosSafeCheck -Name 'DefenderStatus' -ScriptBlock {
    Get-MpComputerStatus | Select-Object AMServiceEnabled, AntispywareEnabled, AntivirusEnabled,
        BehaviorMonitorEnabled, IoavProtectionEnabled, NISEnabled, OnAccessProtectionEnabled,
        RealTimeProtectionEnabled, IsTamperProtected, AntivirusSignatureVersion,
        AntivirusSignatureLastUpdated, QuickScanAge, FullScanAge, DefenderSignaturesOutOfDate
}
$checks.DefenderPreferences = Invoke-HeliosSafeCheck -Name 'DefenderPreferences' -ScriptBlock {
    Get-MpPreference | Select-Object PUAProtection, EnableNetworkProtection, EnableControlledFolderAccess,
        MAPSReporting, SubmitSamplesConsent, CloudBlockLevel, DisableArchiveScanning,
        DisableBehaviorMonitoring, DisableIOAVProtection, DisableRealtimeMonitoring,
        DisableRemovableDriveScanning, DisableScriptScanning, AttackSurfaceReductionRules_Ids,
        AttackSurfaceReductionRules_Actions
}
$checks.Firewall = Invoke-HeliosSafeCheck -Name 'Firewall' -ScriptBlock {
    Get-NetFirewallProfile | Select-Object Name, Enabled, DefaultInboundAction, DefaultOutboundAction,
        NotifyOnListen, LogBlocked, LogAllowed, LogFileName, LogMaxSizeKilobytes
}
$checks.WinRE = Invoke-HeliosSafeCheck -Name 'WinRE' -ScriptBlock {
    if (-not (Test-Path -LiteralPath $reagentc)) { throw "Missing $reagentc" }
    (& $reagentc /info 2>&1 | Out-String).Trim()
}
$checks.BitLocker = Invoke-HeliosSafeCheck -Name 'BitLocker' -ScriptBlock {
    if (Get-Command Get-BitLockerVolume -ErrorAction SilentlyContinue) {
        Get-BitLockerVolume -MountPoint $env:SystemDrive | Select-Object MountPoint, VolumeStatus, ProtectionStatus,
            EncryptionPercentage, EncryptionMethod, AutoUnlockEnabled, KeyProtector
    }
    elseif (Test-Path -LiteralPath $manageBde) {
        (& $manageBde -status $env:SystemDrive 2>&1 | Out-String).Trim()
    }
    else {
        throw 'BitLocker status tooling is unavailable.'
    }
}
$checks.BootConfiguration = Invoke-HeliosSafeCheck -Name 'BootConfiguration' -ScriptBlock {
    if (-not (Test-Path -LiteralPath $bcdedit)) { throw "Missing $bcdedit" }
    (& $bcdedit /enum '{current}' 2>&1 | Out-String).Trim()
}
$checks.VulnerableDriverBlocklist = Invoke-HeliosSafeCheck -Name 'VulnerableDriverBlocklist' -ScriptBlock {
    $path = 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Config'
    $value = (Get-ItemProperty -LiteralPath $path -Name VulnerableDriverBlocklistEnable -ErrorAction SilentlyContinue).VulnerableDriverBlocklistEnable
    [pscustomobject]@{ RegistryValue = $value; Enabled = ($value -eq 1) }
}
$checks.LsaProtection = Invoke-HeliosSafeCheck -Name 'LsaProtection' -ScriptBlock {
    $path = 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa'
    $values = Get-ItemProperty -LiteralPath $path -ErrorAction Stop
    [pscustomobject]@{
        RunAsPPL = $values.RunAsPPL
        RunAsPPLBoot = $values.RunAsPPLBoot
        LsaCfgFlags = $values.LsaCfgFlags
    }
}
$checks.StartupCommands = Invoke-HeliosSafeCheck -Name 'StartupCommands' -ScriptBlock {
    Get-CimInstance Win32_StartupCommand | Select-Object Name, Command, Location, User
}
$checks.RegistryRunEntries = Invoke-HeliosSafeCheck -Name 'RegistryRunEntries' -ScriptBlock {
    @(Get-RegistryRunEntries)
}
$checks.RecentDefenderEvents = Invoke-HeliosSafeCheck -Name 'RecentDefenderEvents' -ScriptBlock {
    $start = (Get-Date).AddHours(-1 * [math]::Abs($EventLookbackHours))
    Get-WinEvent -FilterHashtable @{
        LogName = 'Microsoft-Windows-Windows Defender/Operational'
        StartTime = $start
    } -MaxEvents 150 -ErrorAction Stop | Select-Object TimeCreated, Id, LevelDisplayName, Message
}
$checks.RecentCodeIntegrityEvents = Invoke-HeliosSafeCheck -Name 'RecentCodeIntegrityEvents' -ScriptBlock {
    $start = (Get-Date).AddHours(-1 * [math]::Abs($EventLookbackHours))
    Get-WinEvent -FilterHashtable @{
        LogName = 'Microsoft-Windows-CodeIntegrity/Operational'
        StartTime = $start
    } -MaxEvents 150 -ErrorAction Stop | Select-Object TimeCreated, Id, LevelDisplayName, Message
}

$findings = [System.Collections.Generic.List[object]]::new()
function Add-Finding {
    param([string]$Severity, [string]$Code, [string]$Message)
    $findings.Add([pscustomobject]@{ Severity = $Severity; Code = $Code; Message = $Message })
}

if ($checks.SecureBoot.Success -and -not $checks.SecureBoot.Value.Enabled) {
    Add-Finding -Severity 'Critical' -Code 'secure-boot-disabled' -Message 'Secure Boot is disabled.'
}
if ($checks.Tpm.Success -and (-not $checks.Tpm.Value.TpmPresent -or -not $checks.Tpm.Value.TpmReady)) {
    Add-Finding -Severity 'Critical' -Code 'tpm-not-ready' -Message 'TPM is absent or not ready.'
}
if ($checks.DefenderStatus.Success -and -not $checks.DefenderStatus.Value.RealTimeProtectionEnabled) {
    Add-Finding -Severity 'Critical' -Code 'defender-realtime-disabled' -Message 'Microsoft Defender real-time protection is disabled.'
}
if ($checks.DefenderStatus.Success -and $checks.DefenderStatus.Value.DefenderSignaturesOutOfDate) {
    Add-Finding -Severity 'High' -Code 'defender-signatures-outdated' -Message 'Microsoft Defender signatures are out of date.'
}
if ($checks.DeviceGuard.Success -and ($checks.DeviceGuard.Value.SecurityServicesRunning -notcontains 2)) {
    Add-Finding -Severity 'High' -Code 'memory-integrity-not-running' -Message 'Memory integrity (HVCI) is not reported as running.'
}
if ($checks.VulnerableDriverBlocklist.Success -and -not $checks.VulnerableDriverBlocklist.Value.Enabled) {
    Add-Finding -Severity 'High' -Code 'driver-blocklist-disabled' -Message 'The vulnerable driver blocklist is not explicitly enabled.'
}
if ($checks.Firewall.Success) {
    foreach ($profile in @($checks.Firewall.Value)) {
        if (-not $profile.Enabled) {
            Add-Finding -Severity 'Critical' -Code "firewall-$($profile.Name)-disabled" -Message "Windows Firewall profile $($profile.Name) is disabled."
        }
    }
}

$criticalCount = @($findings | Where-Object Severity -eq 'Critical').Count
$highCount = @($findings | Where-Object Severity -eq 'High').Count
$report = [ordered]@{
    SchemaVersion = 1
    GeneratedUtc = (Get-Date).ToUniversalTime().ToString('o')
    ComputerName = $env:COMPUTERNAME
    UserName = [Environment]::UserName
    IsElevated = ([Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    Summary = [ordered]@{
        Critical = $criticalCount
        High = $highCount
        TotalFindings = $findings.Count
        Overall = if ($criticalCount -gt 0) { 'critical' } elseif ($highCount -gt 0) { 'attention' } else { 'healthy' }
    }
    Findings = @($findings)
    Checks = $checks
}

$reportPath = Join-Path $OutputDirectory "boot-security-posture-$timestamp.json"
$latestPath = Join-Path $OutputDirectory 'boot-security-posture-latest.json'
$json = $report | ConvertTo-Json -Depth 12
$json | Set-Content -LiteralPath $reportPath -Encoding UTF8
$json | Set-Content -LiteralPath $latestPath -Encoding UTF8

$report
Write-Host "HELIOS boot-security report: $reportPath" -ForegroundColor Green
if ($FailOnCritical -and $criticalCount -gt 0) { exit 2 }
