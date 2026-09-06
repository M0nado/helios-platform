#Requires -Version 7.0
<#
.SYNOPSIS
    Collects a redacted, non-mutating HELIOS Windows boot-security posture report.

.DESCRIPTION
    Records Secure Boot, TPM, Defender, Firewall, WinRE, BitLocker, VBS/HVCI,
    vulnerable-driver protection, LSA state, startup persistence metadata, and
    recent Defender/Code Integrity event IDs. It never changes settings.

    Potentially sensitive command lines, event messages, BitLocker recovery
    passwords, API tokens, and secret values are not written to the report.
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = (
        Join-Path $env:LOCALAPPDATA 'HELIOS\Evidence\Security'
    ),
    [ValidateRange(1, 720)]
    [int]$EventLookbackHours = 24,
    [switch]$FailOnCritical
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw 'HELIOS boot-security posture collection can run only on Windows.'
}

function Invoke-HeliosSafeCheck {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Operation
    )

    try {
        return [ordered]@{
            name = $Name
            success = $true
            value = & $Operation
            error = $null
        }
    }
    catch {
        return [ordered]@{
            name = $Name
            success = $false
            value = $null
            error = $_.Exception.Message
        }
    }
}

function Get-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )
}

function Get-CommandHash {
    param([AllowNull()][string]$Command)

    if ([string]::IsNullOrWhiteSpace($Command)) {
        return $null
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes($Command)
    try {
        return [Convert]::ToHexString(
            [Security.Cryptography.SHA256]::HashData($bytes)
        ).ToLowerInvariant()
    }
    finally {
        [Array]::Clear($bytes, 0, $bytes.Length)
    }
}

$windowsRoot = if ($env:SystemRoot) { $env:SystemRoot } else { 'C:\Windows' }
$system32 = Join-Path $windowsRoot 'System32'
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$checks = [ordered]@{}
$checks.operatingSystem = Invoke-HeliosSafeCheck -Name 'OperatingSystem' -Operation {
    Get-CimInstance Win32_OperatingSystem |
        Select-Object Caption, Version, BuildNumber, OSArchitecture, LastBootUpTime
}
$checks.secureBoot = Invoke-HeliosSafeCheck -Name 'SecureBoot' -Operation {
    if (-not (Get-Command Confirm-SecureBootUEFI -ErrorAction SilentlyContinue)) {
        throw 'Confirm-SecureBootUEFI is unavailable.'
    }
    [ordered]@{ enabled = [bool](Confirm-SecureBootUEFI) }
}
$checks.tpm = Invoke-HeliosSafeCheck -Name 'TPM' -Operation {
    if (-not (Get-Command Get-Tpm -ErrorAction SilentlyContinue)) {
        throw 'Get-Tpm is unavailable.'
    }
    Get-Tpm |
        Select-Object TpmPresent, TpmReady, TpmEnabled, TpmActivated,
            ManagedAuthLevel, ManufacturerIdTxt, ManufacturerVersion
}
$checks.deviceGuard = Invoke-HeliosSafeCheck -Name 'DeviceGuard' -Operation {
    Get-CimInstance `
        -Namespace 'root\Microsoft\Windows\DeviceGuard' `
        -ClassName Win32_DeviceGuard |
        Select-Object VirtualizationBasedSecurityStatus,
            SecurityServicesConfigured, SecurityServicesRunning,
            AvailableSecurityProperties, RequiredSecurityProperties,
            CodeIntegrityPolicyEnforcementStatus,
            UsermodeCodeIntegrityPolicyEnforcementStatus
}
$checks.defenderStatus = Invoke-HeliosSafeCheck -Name 'DefenderStatus' -Operation {
    if (-not (Get-Command Get-MpComputerStatus -ErrorAction SilentlyContinue)) {
        throw 'Get-MpComputerStatus is unavailable.'
    }
    Get-MpComputerStatus |
        Select-Object AMServiceEnabled, AntispywareEnabled, AntivirusEnabled,
            BehaviorMonitorEnabled, IoavProtectionEnabled, NISEnabled,
            OnAccessProtectionEnabled, RealTimeProtectionEnabled,
            IsTamperProtected, AntivirusSignatureVersion,
            AntivirusSignatureLastUpdated, QuickScanAge, FullScanAge,
            DefenderSignaturesOutOfDate
}
$checks.defenderPreferences = Invoke-HeliosSafeCheck -Name 'DefenderPreferences' -Operation {
    if (-not (Get-Command Get-MpPreference -ErrorAction SilentlyContinue)) {
        throw 'Get-MpPreference is unavailable.'
    }
    Get-MpPreference |
        Select-Object PUAProtection, EnableNetworkProtection,
            EnableControlledFolderAccess, MAPSReporting, SubmitSamplesConsent,
            CloudBlockLevel, DisableArchiveScanning, DisableBehaviorMonitoring,
            DisableIOAVProtection, DisableRealtimeMonitoring,
            DisableRemovableDriveScanning, DisableScriptScanning,
            AttackSurfaceReductionRules_Ids, AttackSurfaceReductionRules_Actions
}
$checks.firewall = Invoke-HeliosSafeCheck -Name 'Firewall' -Operation {
    if (-not (Get-Command Get-NetFirewallProfile -ErrorAction SilentlyContinue)) {
        throw 'Get-NetFirewallProfile is unavailable.'
    }
    Get-NetFirewallProfile |
        Select-Object Name, Enabled, DefaultInboundAction, DefaultOutboundAction,
            NotifyOnListen, LogBlocked, LogAllowed, LogFileName,
            LogMaxSizeKilobytes
}
$checks.winRe = Invoke-HeliosSafeCheck -Name 'WinRE' -Operation {
    $reagentc = Join-Path $system32 'reagentc.exe'
    if (-not (Test-Path -LiteralPath $reagentc -PathType Leaf)) {
        throw "Missing $reagentc"
    }
    $raw = (& $reagentc /info 2>&1 | Out-String)
    [ordered]@{
        enabled = $raw -match 'Windows RE status:\s+Enabled'
        exitCode = $LASTEXITCODE
        # Do not persist the recovery image path from reagentc output.
        statusLine = @(
            $raw -split "`r?`n" |
                Where-Object { $_ -match 'Windows RE status:' }
        ) | Select-Object -First 1
    }
}
$checks.bitLocker = Invoke-HeliosSafeCheck -Name 'BitLocker' -Operation {
    if (-not (Get-Command Get-BitLockerVolume -ErrorAction SilentlyContinue)) {
        throw 'Get-BitLockerVolume is unavailable.'
    }
    $volume = Get-BitLockerVolume -MountPoint $env:SystemDrive
    [ordered]@{
        mountPoint = $volume.MountPoint
        volumeStatus = [string]$volume.VolumeStatus
        protectionStatus = [string]$volume.ProtectionStatus
        encryptionPercentage = $volume.EncryptionPercentage
        encryptionMethod = [string]$volume.EncryptionMethod
        autoUnlockEnabled = $volume.AutoUnlockEnabled
        keyProtectors = @(
            foreach ($protector in @($volume.KeyProtector)) {
                [ordered]@{
                    keyProtectorId = [string]$protector.KeyProtectorId
                    keyProtectorType = [string]$protector.KeyProtectorType
                    autoUnlockProtector = $protector.AutoUnlockProtector
                }
            }
        )
        recoveryPasswordIncluded = $false
    }
}
$checks.bootConfiguration = Invoke-HeliosSafeCheck -Name 'BootConfiguration' -Operation {
    $bcdedit = Join-Path $system32 'bcdedit.exe'
    if (-not (Test-Path -LiteralPath $bcdedit -PathType Leaf)) {
        throw "Missing $bcdedit"
    }
    $raw = (& $bcdedit /enum '{current}' 2>&1 | Out-String)
    [ordered]@{
        exitCode = $LASTEXITCODE
        recoveryEnabled = $raw -match '(?im)^recoveryenabled\s+Yes\s*$'
        bootStatusPolicy = @(
            $raw -split "`r?`n" |
                Where-Object { $_ -match '(?i)^bootstatuspolicy\s+' }
        ) | Select-Object -First 1
        noIntegrityChecks = $raw -match '(?im)^nointegritychecks\s+Yes\s*$'
        testSigning = $raw -match '(?im)^testsigning\s+Yes\s*$'
        rawOutputIncluded = $false
    }
}
$checks.vulnerableDriverBlocklist = Invoke-HeliosSafeCheck -Name 'VulnerableDriverBlocklist' -Operation {
    $path = 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Config'
    $item = Get-ItemProperty `
        -LiteralPath $path `
        -Name VulnerableDriverBlocklistEnable `
        -ErrorAction SilentlyContinue
    $value = if ($item) { $item.VulnerableDriverBlocklistEnable } else { $null }
    [ordered]@{
        registryValue = $value
        explicitlyEnabled = $value -eq 1
    }
}
$checks.lsaProtection = Invoke-HeliosSafeCheck -Name 'LsaProtection' -Operation {
    $path = 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa'
    $item = Get-ItemProperty -LiteralPath $path
    [ordered]@{
        runAsPpl = $item.RunAsPPL
        runAsPplBoot = $item.RunAsPPLBoot
        lsaCfgFlags = $item.LsaCfgFlags
    }
}
$checks.startupPersistence = Invoke-HeliosSafeCheck -Name 'StartupPersistence' -Operation {
    @(
        foreach ($entry in @(Get-CimInstance Win32_StartupCommand)) {
            [ordered]@{
                name = [string]$entry.Name
                location = [string]$entry.Location
                user = [string]$entry.User
                commandSha256 = Get-CommandHash -Command ([string]$entry.Command)
                commandIncluded = $false
            }
        }
    )
}
$checks.recentDefenderEvents = Invoke-HeliosSafeCheck -Name 'DefenderEvents' -Operation {
    $start = (Get-Date).AddHours(-1 * $EventLookbackHours)
    @(
        Get-WinEvent -FilterHashtable @{
            LogName = 'Microsoft-Windows-Windows Defender/Operational'
            StartTime = $start
        } -MaxEvents 150 |
            Select-Object TimeCreated, Id, Level, LevelDisplayName,
                ProviderName, RecordId
    )
}
$checks.recentCodeIntegrityEvents = Invoke-HeliosSafeCheck -Name 'CodeIntegrityEvents' -Operation {
    $start = (Get-Date).AddHours(-1 * $EventLookbackHours)
    @(
        Get-WinEvent -FilterHashtable @{
            LogName = 'Microsoft-Windows-CodeIntegrity/Operational'
            StartTime = $start
        } -MaxEvents 150 |
            Select-Object TimeCreated, Id, Level, LevelDisplayName,
                ProviderName, RecordId
    )
}

$findings = [Collections.Generic.List[object]]::new()
function Add-HeliosFinding {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('critical', 'high', 'medium', 'info')]
        [string]$Severity,
        [Parameter(Mandatory)][string]$Code,
        [Parameter(Mandatory)][string]$Message
    )
    $findings.Add([ordered]@{
        severity = $Severity
        code = $Code
        message = $Message
    })
}

if ($checks.secureBoot.success -and -not $checks.secureBoot.value.enabled) {
    Add-HeliosFinding critical 'secure-boot-disabled' 'Secure Boot is disabled.'
}
if ($checks.tpm.success -and (
        -not $checks.tpm.value.TpmPresent -or
        -not $checks.tpm.value.TpmReady
    )) {
    Add-HeliosFinding critical 'tpm-not-ready' 'TPM is absent or not ready.'
}
if ($checks.defenderStatus.success -and
    -not $checks.defenderStatus.value.RealTimeProtectionEnabled) {
    Add-HeliosFinding critical 'defender-realtime-disabled' 'Defender real-time protection is disabled.'
}
if ($checks.defenderStatus.success -and
    $checks.defenderStatus.value.DefenderSignaturesOutOfDate) {
    Add-HeliosFinding high 'defender-signatures-outdated' 'Defender signatures are out of date.'
}
if ($checks.deviceGuard.success -and
    $checks.deviceGuard.value.SecurityServicesRunning -notcontains 2) {
    Add-HeliosFinding high 'memory-integrity-not-running' 'Memory integrity is not reported as running.'
}
if ($checks.vulnerableDriverBlocklist.success -and
    -not $checks.vulnerableDriverBlocklist.value.explicitlyEnabled) {
    Add-HeliosFinding high 'driver-blocklist-not-explicit' 'The vulnerable-driver blocklist is not explicitly enabled.'
}
if ($checks.firewall.success) {
    foreach ($profile in @($checks.firewall.value)) {
        if (-not $profile.Enabled) {
            Add-HeliosFinding critical `
                "firewall-$($profile.Name)-disabled" `
                "Windows Firewall profile $($profile.Name) is disabled."
        }
    }
}
if ($checks.bootConfiguration.success -and
    $checks.bootConfiguration.value.noIntegrityChecks) {
    Add-HeliosFinding critical 'no-integrity-checks' 'Boot configuration disables integrity checks.'
}
if ($checks.bootConfiguration.success -and
    $checks.bootConfiguration.value.testSigning) {
    Add-HeliosFinding high 'test-signing-enabled' 'Boot configuration has test signing enabled.'
}

$criticalCount = @(
    $findings | Where-Object severity -eq 'critical'
).Count
$highCount = @(
    $findings | Where-Object severity -eq 'high'
).Count

$report = [ordered]@{
    schemaVersion = 2
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    computerName = $env:COMPUTERNAME
    isAdministrator = Get-IsAdministrator
    eventLookbackHours = $EventLookbackHours
    summary = [ordered]@{
        critical = $criticalCount
        high = $highCount
        totalFindings = $findings.Count
        overall = if ($criticalCount -gt 0) {
            'critical'
        }
        elseif ($highCount -gt 0) {
            'attention'
        }
        else {
            'healthy'
        }
    }
    findings = @($findings)
    checks = $checks
    redaction = [ordered]@{
        commandLinesIncluded = $false
        eventMessagesIncluded = $false
        bitLockerRecoveryPasswordsIncluded = $false
        secretValuesIncluded = $false
    }
    mutationPerformed = $false
}

$reportPath = Join-Path $OutputDirectory "boot-security-posture-$timestamp.json"
$latestPath = Join-Path $OutputDirectory 'boot-security-posture-latest.json'
$report | ConvertTo-Json -Depth 14 |
    Set-Content -LiteralPath $reportPath -Encoding UTF8
$report | ConvertTo-Json -Depth 14 |
    Set-Content -LiteralPath $latestPath -Encoding UTF8

$report | ConvertTo-Json -Depth 14
Write-Host "HELIOS boot-security evidence: $reportPath" -ForegroundColor Green
if ($FailOnCritical -and $criticalCount -gt 0) {
    exit 2
}
