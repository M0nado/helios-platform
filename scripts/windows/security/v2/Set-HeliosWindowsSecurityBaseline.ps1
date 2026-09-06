#Requires -Version 7.0
<#
.SYNOPSIS
    Applies a guarded HELIOS Microsoft Defender and Windows security baseline.

.DESCRIPTION
    This is an explicit administrator operation. It backs up Defender,
    Firewall, Device Guard, Code Integrity, and LSA state before mutation.
    Existing ASR rules are preserved and HELIOS-managed rule IDs are merged.

    Secure Boot and TPM are never changed by this script. HVCI and Credential
    Guard require explicit switches and are enabled without UEFI lock so an
    administrator can recover from incompatible drivers.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [ValidateSet('Audit', 'Balanced', 'Strict')]
    [string]$Profile = 'Audit',

    [string]$PolicyPath = (
        Join-Path (
            (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
        ) 'config\windows\boot-security.v2.json'
    ),

    [string]$BackupDirectory = (
        Join-Path $env:ProgramData 'HELIOS\Security\Backups'
    ),

    [string]$EvidenceDirectory = (
        Join-Path $env:ProgramData 'HELIOS\Security\Reports'
    ),

    [switch]$EnableMemoryIntegrity,
    [switch]$EnableCredentialGuard,
    [switch]$EnableControlledFolderAccessBlock,

    [Parameter(Mandatory)]
    [string]$Confirmation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$requiredConfirmation = 'APPLY HELIOS WINDOWS SECURITY BASELINE'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )
}

function Export-RegistryKeySafely {
    param(
        [Parameter(Mandatory)][string]$NativePath,
        [Parameter(Mandatory)][string]$Destination
    )

    $windowsRoot = if ($env:SystemRoot) { $env:SystemRoot } else { 'C:\Windows' }
    $reg = Join-Path $windowsRoot 'System32\reg.exe'
    if (-not (Test-Path -LiteralPath $reg -PathType Leaf)) {
        throw "Missing registry export utility: $reg"
    }

    $output = & $reg export $NativePath $Destination /y 2>&1
    return [ordered]@{
        registryPath = $NativePath
        destination = $Destination
        exported = $LASTEXITCODE -eq 0
        exitCode = $LASTEXITCODE
        # Store only the bounded command result, never registry values.
        detail = (($output | Out-String).Trim() -replace '[\r\n]+', ' ')
    }
}

function Get-AsrDictionary {
    param([Parameter(Mandatory)]$Preference)

    $dictionary = [Collections.Generic.Dictionary[string, string]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    $ids = @($Preference.AttackSurfaceReductionRules_Ids)
    $actions = @($Preference.AttackSurfaceReductionRules_Actions)
    for ($index = 0; $index -lt $ids.Count; $index++) {
        $action = if ($index -lt $actions.Count) {
            [string]$actions[$index]
        }
        else {
            'NotConfigured'
        }
        $dictionary[[string]$ids[$index]] = $action
    }
    return $dictionary
}

if (-not $IsWindows) {
    throw 'HELIOS Windows security baseline can run only on Windows.'
}
if (-not (Test-IsAdministrator)) {
    throw 'Run this script from an elevated PowerShell session.'
}
if ($Confirmation -cne $requiredConfirmation) {
    throw "Exact confirmation required: $requiredConfirmation"
}
if (-not (Test-Path -LiteralPath $PolicyPath -PathType Leaf)) {
    throw "Boot-security policy not found: $PolicyPath"
}

$policy = Get-Content -LiteralPath $PolicyPath -Raw |
    ConvertFrom-Json -Depth 20
if ($policy.schemaVersion -ne 2) {
    throw "Unsupported boot-security policy schema: $($policy.schemaVersion)"
}
if ($policy.startup.automaticOfflineScan -or
    $policy.scheduledScans.offlineScanScheduled) {
    throw 'Policy rejected: offline scanning cannot be automatic or scheduled.'
}
if ($policy.safety.formatDisks -or
    $policy.safety.modifyPartitions -or
    $policy.safety.resetTpm -or
    $policy.safety.changeSecureBoot -or
    $policy.safety.disableBitLockerPermanently -or
    $policy.safety.broadDefenderExclusions -or
    $policy.safety.automaticRebootLoop) {
    throw 'Policy rejected: a prohibited safety flag is enabled.'
}

foreach ($command in @(
        'Get-MpComputerStatus',
        'Get-MpPreference',
        'Set-MpPreference',
        'Set-NetFirewallProfile'
    )) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required Windows security command is unavailable: $command"
    }
}

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null

$defenderBackup = Join-Path $BackupDirectory "defender-preferences-$timestamp.json"
$firewallBackup = Join-Path $BackupDirectory "firewall-$timestamp.wfw"
Get-MpPreference | ConvertTo-Json -Depth 12 |
    Set-Content -LiteralPath $defenderBackup -Encoding UTF8

$windowsRoot = if ($env:SystemRoot) { $env:SystemRoot } else { 'C:\Windows' }
$netsh = Join-Path $windowsRoot 'System32\netsh.exe'
if (-not (Test-Path -LiteralPath $netsh -PathType Leaf)) {
    throw "Missing firewall export utility: $netsh"
}
& $netsh advfirewall export $firewallBackup | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to export Windows Firewall policy.'
}

$registryBackups = @(
    Export-RegistryKeySafely `
        -NativePath 'HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard' `
        -Destination (Join-Path $BackupDirectory "deviceguard-$timestamp.reg")
    Export-RegistryKeySafely `
        -NativePath 'HKLM\SYSTEM\CurrentControlSet\Control\CI\Config' `
        -Destination (Join-Path $BackupDirectory "code-integrity-$timestamp.reg")
    Export-RegistryKeySafely `
        -NativePath 'HKLM\SYSTEM\CurrentControlSet\Control\Lsa' `
        -Destination (Join-Path $BackupDirectory "lsa-$timestamp.reg")
)

$beforeStatus = Get-MpComputerStatus
$beforePreference = Get-MpPreference
$tamperProtected = [bool]$beforeStatus.IsTamperProtected

$managedAction = if ($Profile -eq 'Strict') { 'Enabled' } else { 'AuditMode' }
$controlledFolderAction = if (
    $EnableControlledFolderAccessBlock -or $Profile -eq 'Strict'
) {
    'Enabled'
}
else {
    'AuditMode'
}

$asr = Get-AsrDictionary -Preference $beforePreference
foreach ($rule in @($policy.attackSurfaceReduction.rules)) {
    $asr[[string]$rule.id] = $managedAction
}
$asrIds = @($asr.Keys | Sort-Object)
$asrActions = @(
    foreach ($id in $asrIds) {
        $asr[$id]
    }
)

if ($PSCmdlet.ShouldProcess(
        'Microsoft Defender Antivirus',
        "Apply HELIOS $Profile baseline"
    )) {
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
        -EnableControlledFolderAccess $controlledFolderAction `
        -AttackSurfaceReductionRules_Ids $asrIds `
        -AttackSurfaceReductionRules_Actions $asrActions
}

if ($PSCmdlet.ShouldProcess(
        'Windows Firewall',
        'Enable all profiles with blocked inbound and allowed outbound defaults'
    )) {
    Set-NetFirewallProfile `
        -Profile Domain, Private, Public `
        -Enabled True `
        -DefaultInboundAction Block `
        -DefaultOutboundAction Allow `
        -NotifyOnListen True `
        -LogBlocked True `
        -LogAllowed False
}

if ($PSCmdlet.ShouldProcess(
        'Windows vulnerable-driver blocklist',
        'Enable explicit policy value'
    )) {
    $ciPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Config'
    New-Item -Path $ciPath -Force | Out-Null
    New-ItemProperty `
        -Path $ciPath `
        -Name VulnerableDriverBlocklistEnable `
        -PropertyType DWord `
        -Value 1 `
        -Force | Out-Null
}

if ($EnableMemoryIntegrity) {
    if ($PSCmdlet.ShouldProcess(
            'Virtualization-based security and HVCI',
            'Enable without UEFI lock'
        )) {
        $deviceGuard = 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'
        $hvci = Join-Path $deviceGuard 'Scenarios\HypervisorEnforcedCodeIntegrity'
        New-Item -Path $deviceGuard -Force | Out-Null
        New-Item -Path $hvci -Force | Out-Null
        New-ItemProperty `
            -Path $deviceGuard `
            -Name EnableVirtualizationBasedSecurity `
            -PropertyType DWord `
            -Value 1 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $deviceGuard `
            -Name RequirePlatformSecurityFeatures `
            -PropertyType DWord `
            -Value 1 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $deviceGuard `
            -Name Locked `
            -PropertyType DWord `
            -Value 0 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $hvci `
            -Name Enabled `
            -PropertyType DWord `
            -Value 1 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $hvci `
            -Name Locked `
            -PropertyType DWord `
            -Value 0 `
            -Force | Out-Null
    }
}

if ($EnableCredentialGuard) {
    if ($PSCmdlet.ShouldProcess(
            'Credential Guard and LSA protection',
            'Enable without UEFI lock'
        )) {
        $deviceGuard = 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'
        $lsa = 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa'
        New-Item -Path $deviceGuard -Force | Out-Null
        New-Item -Path $lsa -Force | Out-Null
        New-ItemProperty `
            -Path $deviceGuard `
            -Name EnableVirtualizationBasedSecurity `
            -PropertyType DWord `
            -Value 1 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $deviceGuard `
            -Name RequirePlatformSecurityFeatures `
            -PropertyType DWord `
            -Value 1 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $lsa `
            -Name LsaCfgFlags `
            -PropertyType DWord `
            -Value 2 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $lsa `
            -Name RunAsPPL `
            -PropertyType DWord `
            -Value 2 `
            -Force | Out-Null
        New-ItemProperty `
            -Path $lsa `
            -Name RunAsPPLBoot `
            -PropertyType DWord `
            -Value 2 `
            -Force | Out-Null
    }
}

$afterStatus = Get-MpComputerStatus
$afterPreference = Get-MpPreference
$evidence = [ordered]@{
    schemaVersion = 2
    appliedUtc = (Get-Date).ToUniversalTime().ToString('o')
    computerName = $env:COMPUTERNAME
    profile = $Profile
    asrManagedAction = $managedAction
    controlledFolderAccess = $controlledFolderAction
    memoryIntegrityRequested = [bool]$EnableMemoryIntegrity
    credentialGuardRequested = [bool]$EnableCredentialGuard
    tamperProtectionDetectedBeforeApply = $tamperProtected
    backups = [ordered]@{
        defender = $defenderBackup
        firewall = $firewallBackup
        registry = $registryBackups
    }
    effectiveDefender = [ordered]@{
        realTimeProtectionEnabled = $afterStatus.RealTimeProtectionEnabled
        behaviorMonitorEnabled = $afterStatus.BehaviorMonitorEnabled
        ioavProtectionEnabled = $afterStatus.IoavProtectionEnabled
        signaturesOutOfDate = $afterStatus.DefenderSignaturesOutOfDate
        puaProtection = $afterPreference.PUAProtection
        networkProtection = $afterPreference.EnableNetworkProtection
        controlledFolderAccess = $afterPreference.EnableControlledFolderAccess
    }
    secureBootChanged = $false
    tpmChanged = $false
    bitLockerChanged = $false
    rebootInitiated = $false
    rebootRecommended = [bool](
        $EnableMemoryIntegrity -or $EnableCredentialGuard
    )
}

$evidencePath = Join-Path $EvidenceDirectory "security-baseline-$timestamp.json"
$evidence | ConvertTo-Json -Depth 12 |
    Set-Content -LiteralPath $evidencePath -Encoding UTF8
$evidence | ConvertTo-Json -Depth 12
Write-Host "HELIOS security baseline evidence: $evidencePath" -ForegroundColor Green
