#Requires -Version 5.1
<#
.SYNOPSIS
    Applies a guarded Microsoft Defender and Windows boot-security baseline.

.DESCRIPTION
    The default profile keeps disruptive controls in Audit mode. Memory integrity,
    Credential Guard, and block-mode ASR require explicit switches. The script
    backs up the current configuration and supports -WhatIf.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [ValidateSet('Audit','Balanced','Strict')]
    [string]$Profile = 'Audit',
    [switch]$EnableMemoryIntegrity,
    [switch]$EnableCredentialGuard,
    [switch]$EnableControlledFolderAccessBlock,
    [string]$BackupDirectory = "$env:ProgramData\HELIOS\Security\Backups"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Registry: Enables the Microsoft vulnerable-driver blocklist and optional
# Device Guard/HVCI/Credential Guard/LSA protections. Every affected HKLM key is
# exported before mutation. Rollback uses the generated .reg files after review.

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Export-HeliosRegistryKey {
    param(
        [Parameter(Mandatory)][string]$NativePath,
        [Parameter(Mandatory)][string]$Destination
    )

    $regExe = Join-Path $env:SystemRoot 'System32\reg.exe'
    $output = & $regExe export $NativePath $Destination /y 2>&1
    [pscustomobject]@{
        RegistryPath = $NativePath
        Destination = $Destination
        ExitCode = $LASTEXITCODE
        Exported = ($LASTEXITCODE -eq 0)
        Detail = ($output | Out-String).Trim()
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'This baseline can only run on Windows.'
}
if (-not (Test-IsAdministrator)) {
    throw 'Run this script from an elevated PowerShell session.'
}

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
$defenderBackup = Join-Path $BackupDirectory "defender-preferences-$timestamp.json"
$firewallBackup = Join-Path $BackupDirectory "firewall-$timestamp.wfw"

Get-MpPreference | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $defenderBackup -Encoding UTF8
& "$env:SystemRoot\System32\netsh.exe" advfirewall export $firewallBackup | Out-Null

$registryBackups = @(
    Export-HeliosRegistryKey -NativePath 'HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard' -Destination (Join-Path $BackupDirectory "deviceguard-$timestamp.reg")
    Export-HeliosRegistryKey -NativePath 'HKLM\SYSTEM\CurrentControlSet\Control\CI\Config' -Destination (Join-Path $BackupDirectory "code-integrity-$timestamp.reg")
    Export-HeliosRegistryKey -NativePath 'HKLM\SYSTEM\CurrentControlSet\Control\Lsa' -Destination (Join-Path $BackupDirectory "lsa-$timestamp.reg")
)

$asrRules = [ordered]@{
    '56a863a9-875e-4185-98a7-b882c64b5ce5' = 'Block abuse of exploited vulnerable signed drivers'
    '9e6c4e1f-7d60-472f-ba1a-a39ef669e4b2' = 'Block credential stealing from LSASS'
    'e6db77e5-3df2-4cf1-b95a-636979351e5b' = 'Block persistence through WMI event subscription'
    '5beb7efe-fd9a-4556-801d-275e5ffc04cc' = 'Block execution of potentially obfuscated scripts'
    'd3e037e1-3eb8-44c8-a917-57927947596d' = 'Block JavaScript or VBScript from launching downloaded executable content'
    'c1db55ab-c21a-4637-bb3f-a12568109d35' = 'Use advanced protection against ransomware'
    'd4f940ab-401b-4efc-aadc-ad5f3c50688a' = 'Block Office applications from creating child processes'
    '3b576869-a4ec-4529-8536-b80a7769e899' = 'Block Office applications from creating executable content'
    '75668c1f-73b5-4cf0-bb93-3ecf5cb7cc84' = 'Block Office applications from injecting code into other processes'
    'be9ba2d9-53ea-4cdc-84e5-9b1eeee46550' = 'Block executable content from email client and webmail'
    'b2b3f03d-6a65-4f7b-a9c7-1c7ef74a9ba4' = 'Block untrusted and unsigned processes that run from USB'
}

$asrAction = if ($Profile -eq 'Strict') { 'Enabled' } else { 'AuditMode' }
$cfaAction = if ($EnableControlledFolderAccessBlock -or $Profile -eq 'Strict') { 'Enabled' } else { 'AuditMode' }
$tamperProtected = [bool](Get-MpComputerStatus).IsTamperProtected

if ($PSCmdlet.ShouldProcess('Microsoft Defender Antivirus', "Apply $Profile HELIOS baseline")) {
    Set-MpPreference `
        -DisableRealtimeMonitoring $false `
        -DisableBehaviorMonitoring $false `
        -DisableIOAVProtection $false `
        -DisableScriptScanning $false `
        -DisableArchiveScanning $false `
        -DisableRemovableDriveScanning $false `
        -PUAProtection Enabled `
        -MAPSReporting Advanced `
        -SubmitSamplesConsent SendSafeSamples `
        -CloudBlockLevel High `
        -EnableNetworkProtection Enabled `
        -EnableControlledFolderAccess $cfaAction

    foreach ($rule in $asrRules.GetEnumerator()) {
        Add-MpPreference -AttackSurfaceReductionRules_Ids $rule.Key -AttackSurfaceReductionRules_Actions $asrAction
    }
}

if ($PSCmdlet.ShouldProcess('Windows Firewall', 'Enable all profiles with blocked inbound and allowed outbound defaults')) {
    Set-NetFirewallProfile -Profile Domain,Private,Public -Enabled True -DefaultInboundAction Block -DefaultOutboundAction Allow -NotifyOnListen True -LogBlocked True -LogAllowed False
}

if ($PSCmdlet.ShouldProcess('Windows vulnerable driver blocklist', 'Enable')) {
    $ciPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Config'
    New-Item -Path $ciPath -Force | Out-Null
    New-ItemProperty -Path $ciPath -Name VulnerableDriverBlocklistEnable -PropertyType DWord -Value 1 -Force | Out-Null
}

if ($EnableMemoryIntegrity) {
    if ($PSCmdlet.ShouldProcess('VBS/HVCI', 'Enable memory integrity without UEFI lock')) {
        $deviceGuard = 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'
        $hvci = Join-Path $deviceGuard 'Scenarios\HypervisorEnforcedCodeIntegrity'
        New-Item -Path $deviceGuard -Force | Out-Null
        New-Item -Path $hvci -Force | Out-Null
        New-ItemProperty -Path $deviceGuard -Name EnableVirtualizationBasedSecurity -PropertyType DWord -Value 1 -Force | Out-Null
        New-ItemProperty -Path $deviceGuard -Name RequirePlatformSecurityFeatures -PropertyType DWord -Value 1 -Force | Out-Null
        New-ItemProperty -Path $deviceGuard -Name Locked -PropertyType DWord -Value 0 -Force | Out-Null
        New-ItemProperty -Path $hvci -Name Enabled -PropertyType DWord -Value 1 -Force | Out-Null
        New-ItemProperty -Path $hvci -Name Locked -PropertyType DWord -Value 0 -Force | Out-Null
    }
}

if ($EnableCredentialGuard) {
    if ($PSCmdlet.ShouldProcess('Credential Guard and LSA protection', 'Enable without UEFI lock')) {
        $deviceGuard = 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'
        $lsa = 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa'
        New-Item -Path $deviceGuard -Force | Out-Null
        New-ItemProperty -Path $deviceGuard -Name EnableVirtualizationBasedSecurity -PropertyType DWord -Value 1 -Force | Out-Null
        New-ItemProperty -Path $deviceGuard -Name RequirePlatformSecurityFeatures -PropertyType DWord -Value 1 -Force | Out-Null
        New-ItemProperty -Path $lsa -Name LsaCfgFlags -PropertyType DWord -Value 2 -Force | Out-Null
        New-ItemProperty -Path $lsa -Name RunAsPPL -PropertyType DWord -Value 2 -Force | Out-Null
        New-ItemProperty -Path $lsa -Name RunAsPPLBoot -PropertyType DWord -Value 2 -Force | Out-Null
    }
}

$result = [ordered]@{
    AppliedUtc = (Get-Date).ToUniversalTime().ToString('o')
    Profile = $Profile
    AsrMode = $asrAction
    ControlledFolderAccess = $cfaAction
    MemoryIntegrityRequested = [bool]$EnableMemoryIntegrity
    CredentialGuardRequested = [bool]$EnableCredentialGuard
    TamperProtectionDetected = $tamperProtected
    DefenderBackup = $defenderBackup
    FirewallBackup = $firewallBackup
    RegistryBackups = $registryBackups
    RebootRecommended = [bool]($EnableMemoryIntegrity -or $EnableCredentialGuard)
    Notes = @(
        'Secure Boot and TPM are audited separately because firmware settings cannot be safely forced by this script.',
        'Memory integrity can expose incompatible drivers; use the audit workflow and recovery documentation before enabling it fleet-wide.',
        $(if ($tamperProtected) { 'Tamper Protection is enabled. Verify effective Defender settings after the run; enterprise-managed settings may require Intune or Defender portal policy.' } else { 'Tamper Protection was not reported as enabled.' })
    )
}
$result | ConvertTo-Json -Depth 8
