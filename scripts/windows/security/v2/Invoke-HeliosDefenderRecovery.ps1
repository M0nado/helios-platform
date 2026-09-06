#Requires -Version 7.0
<#
.SYNOPSIS
    Runs guarded Microsoft Defender scan and offline-recovery operations.

.DESCRIPTION
    Readiness is the default and is non-mutating. QuickScan and FullScan are
    explicit. OfflineScan requires administrator elevation, WinRE readiness,
    known BitLocker state, secret-free recovery-evidence verification, optional
    one-reboot BitLocker suspension, and the exact confirmation phrase.

    No scan is scheduled by this script. OfflineScan can restart the computer.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [ValidateSet('Readiness', 'QuickScan', 'FullScan', 'OfflineScan')]
    [string]$Mode = 'Readiness',

    [string]$PolicyPath = (
        Join-Path (
            (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
        ) 'config\windows\boot-security.v2.json'
    ),

    [string]$EvidenceDirectory = (
        Join-Path $env:ProgramData 'HELIOS\Security\Reports'
    ),

    [string]$StateDirectory = (
        Join-Path $env:ProgramData 'HELIOS\Security\State'
    ),

    [string]$RecoveryEvidencePath,
    [switch]$EnableWinRE,
    [switch]$SuspendBitLockerForOneReboot,
    [switch]$UpdateSignatures,
    [string]$Confirmation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )
}

function Get-WinReState {
    param([Parameter(Mandatory)][string]$ReagentcPath)

    $raw = (& $ReagentcPath /info 2>&1 | Out-String)
    return [ordered]@{
        enabled = $raw -match 'Windows RE status:\s+Enabled'
        exitCode = $LASTEXITCODE
        statusLine = @(
            $raw -split "`r?`n" |
                Where-Object { $_ -match 'Windows RE status:' }
        ) | Select-Object -First 1
        rawOutputIncluded = $false
    }
}

function Get-BitLockerState {
    if (-not (Get-Command Get-BitLockerVolume -ErrorAction SilentlyContinue)) {
        return [ordered]@{
            known = $false
            protectionStatus = 'Unknown'
            volumeStatus = 'Unknown'
            mountPoint = $env:SystemDrive
            recoveryPasswordIncluded = $false
        }
    }

    try {
        $volume = Get-BitLockerVolume -MountPoint $env:SystemDrive
        return [ordered]@{
            known = $true
            protectionStatus = [string]$volume.ProtectionStatus
            volumeStatus = [string]$volume.VolumeStatus
            mountPoint = [string]$volume.MountPoint
            encryptionPercentage = $volume.EncryptionPercentage
            recoveryPasswordIncluded = $false
        }
    }
    catch {
        return [ordered]@{
            known = $false
            protectionStatus = 'Unknown'
            volumeStatus = 'Unknown'
            mountPoint = $env:SystemDrive
            error = $_.Exception.Message
            recoveryPasswordIncluded = $false
        }
    }
}

function Read-RecoveryEvidence {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Recovery evidence does not exist: $Path"
    }
    $evidence = Get-Content -LiteralPath $Path -Raw |
        ConvertFrom-Json -Depth 10

    if ($evidence.schemaVersion -ne 1) {
        throw 'Recovery evidence schemaVersion must be 1.'
    }
    if (-not $evidence.recoveryKeyEscrowVerified) {
        throw 'Recovery evidence does not confirm escrow verification.'
    }
    if ($evidence.containsRecoveryPassword) {
        throw 'Recovery evidence must never contain a BitLocker recovery password.'
    }
    if ([string]::IsNullOrWhiteSpace([string]$evidence.evidenceLocation)) {
        throw 'Recovery evidence must identify an approved escrow location.'
    }
    if ([string]$evidence.volume -ne [string]$env:SystemDrive) {
        throw "Recovery evidence volume does not match $env:SystemDrive."
    }

    return [ordered]@{
        schemaVersion = 1
        volume = [string]$evidence.volume
        recoveryKeyEscrowVerified = $true
        verifiedUtc = [string]$evidence.verifiedUtc
        verifiedBy = [string]$evidence.verifiedBy
        evidenceLocation = [string]$evidence.evidenceLocation
        containsRecoveryPassword = $false
        sourceSha256 = (
            Get-FileHash -LiteralPath $Path -Algorithm SHA256
        ).Hash.ToLowerInvariant()
    }
}

if (-not $IsWindows) {
    throw 'HELIOS Defender recovery can run only on Windows.'
}
if (-not (Test-Path -LiteralPath $PolicyPath -PathType Leaf)) {
    throw "Boot-security policy not found: $PolicyPath"
}

$policy = Get-Content -LiteralPath $PolicyPath -Raw |
    ConvertFrom-Json -Depth 20
if ($policy.schemaVersion -ne 2) {
    throw "Unsupported boot-security policy schema: $($policy.schemaVersion)"
}
if ($policy.offlineRecovery.automatic -or
    $policy.startup.automaticOfflineScan -or
    $policy.scheduledScans.offlineScanScheduled) {
    throw 'Policy rejected: offline recovery cannot be automatic or scheduled.'
}

$windowsRoot = if ($env:SystemRoot) { $env:SystemRoot } else { 'C:\Windows' }
$reagentc = Join-Path $windowsRoot 'System32\reagentc.exe'
if (-not (Test-Path -LiteralPath $reagentc -PathType Leaf)) {
    throw "Windows Recovery Environment utility is missing: $reagentc"
}

$winRe = Get-WinReState -ReagentcPath $reagentc
$bitLocker = Get-BitLockerState
$defender = if (Get-Command Get-MpComputerStatus -ErrorAction SilentlyContinue) {
    Get-MpComputerStatus
}
else {
    $null
}

$readiness = [ordered]@{
    schemaVersion = 2
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    computerName = $env:COMPUTERNAME
    mode = $Mode
    isAdministrator = Test-IsAdministrator
    defender = [ordered]@{
        available = [bool]$defender
        antivirusEnabled = if ($defender) {
            [bool]$defender.AntivirusEnabled
        }
        else {
            $false
        }
        realTimeProtectionEnabled = if ($defender) {
            [bool]$defender.RealTimeProtectionEnabled
        }
        else {
            $false
        }
        signaturesOutOfDate = if ($defender) {
            [bool]$defender.DefenderSignaturesOutOfDate
        }
        else {
            $null
        }
    }
    winRe = $winRe
    bitLocker = $bitLocker
    exactOfflineConfirmation = [string]$policy.offlineRecovery.confirmationPhrase
    executionStarted = $false
    rebootInitiated = $false
}

New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $StateDirectory -Force | Out-Null
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$readinessPath = Join-Path $EvidenceDirectory "defender-recovery-readiness-$timestamp.json"
$readiness | ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $readinessPath -Encoding UTF8

if ($Mode -eq 'Readiness') {
    $readiness | ConvertTo-Json -Depth 10
    Write-Host "HELIOS recovery readiness: $readinessPath" -ForegroundColor Green
    return
}

if (-not (Test-IsAdministrator)) {
    throw "$Mode requires an elevated PowerShell session."
}
if (-not $defender) {
    throw 'Microsoft Defender Antivirus status is unavailable.'
}
if (-not (Get-Command Start-MpScan -ErrorAction SilentlyContinue)) {
    throw 'Start-MpScan is unavailable.'
}

if ($UpdateSignatures) {
    if (-not (Get-Command Update-MpSignature -ErrorAction SilentlyContinue)) {
        throw 'Update-MpSignature is unavailable.'
    }
    if ($PSCmdlet.ShouldProcess('Microsoft Defender signatures', 'Update')) {
        Update-MpSignature
    }
}

switch ($Mode) {
    'QuickScan' {
        if ($PSCmdlet.ShouldProcess('Microsoft Defender', 'Run quick scan')) {
            Start-MpScan -ScanType QuickScan
        }
    }
    'FullScan' {
        if ($Confirmation -cne 'RUN HELIOS FULL DEFENDER SCAN') {
            throw 'Exact confirmation required: RUN HELIOS FULL DEFENDER SCAN'
        }
        if ($PSCmdlet.ShouldProcess('Microsoft Defender', 'Run full scan')) {
            Start-MpScan -ScanType FullScan
        }
    }
    'OfflineScan' {
        $offlineConfirmation = [string]$policy.offlineRecovery.confirmationPhrase
        if ($Confirmation -cne $offlineConfirmation) {
            throw "Exact confirmation required: $offlineConfirmation"
        }
        if (-not (Get-Command Start-MpWDOScan -ErrorAction SilentlyContinue)) {
            throw 'Start-MpWDOScan is unavailable.'
        }
        if (-not $bitLocker.known) {
            throw 'Offline scan blocked: BitLocker protection state is unknown.'
        }

        if (-not $winRe.enabled) {
            if (-not $EnableWinRE) {
                throw 'WinRE is disabled. Re-run with -EnableWinRE after reviewing recovery configuration.'
            }
            if ($PSCmdlet.ShouldProcess('Windows Recovery Environment', 'Enable')) {
                & $reagentc /enable | Out-Host
                if ($LASTEXITCODE -ne 0) {
                    throw 'reagentc /enable failed.'
                }
                $winRe = Get-WinReState -ReagentcPath $reagentc
                if (-not $winRe.enabled) {
                    throw 'WinRE did not report Enabled after reagentc /enable.'
                }
            }
        }

        $recoveryEvidence = $null
        if ($bitLocker.protectionStatus -eq 'On') {
            if (-not $SuspendBitLockerForOneReboot) {
                throw 'BitLocker is protected. Explicit one-reboot suspension is required.'
            }
            if ([string]::IsNullOrWhiteSpace($RecoveryEvidencePath)) {
                throw 'BitLocker is protected. A secret-free recovery-evidence receipt is required.'
            }
            $recoveryEvidence = Read-RecoveryEvidence -Path $RecoveryEvidencePath
            if (-not (Get-Command Suspend-BitLocker -ErrorAction SilentlyContinue)) {
                throw 'Suspend-BitLocker is unavailable.'
            }
            if ($PSCmdlet.ShouldProcess(
                    $env:SystemDrive,
                    'Suspend BitLocker protection for one reboot'
                )) {
                Suspend-BitLocker `
                    -MountPoint $env:SystemDrive `
                    -RebootCount 1 | Out-Null
            }
        }

        $marker = [ordered]@{
            schemaVersion = 2
            requestedUtc = (Get-Date).ToUniversalTime().ToString('o')
            computerName = $env:COMPUTERNAME
            operation = 'MicrosoftDefenderOffline'
            state = 'queued-for-defender'
            expectedEventId = 2030
            winReEnabled = $winRe.enabled
            bitLockerProtectionStatus = $bitLocker.protectionStatus
            bitLockerSuspendedForReboots = if (
                $bitLocker.protectionStatus -eq 'On'
            ) {
                1
            }
            else {
                0
            }
            recoveryEvidence = $recoveryEvidence
            recoveryPasswordIncluded = $false
            automatic = $false
            rebootInitiated = $false
        }
        $markerPath = Join-Path $StateDirectory 'offline-scan-pending.json'
        $marker | ConvertTo-Json -Depth 10 |
            Set-Content -LiteralPath $markerPath -Encoding UTF8

        if ($PSCmdlet.ShouldProcess(
                $env:COMPUTERNAME,
                'Restart into Microsoft Defender Offline'
            )) {
            Start-MpWDOScan
        }
    }
}

$result = [ordered]@{
    schemaVersion = 2
    completedUtc = (Get-Date).ToUniversalTime().ToString('o')
    mode = $Mode
    readinessEvidence = $readinessPath
    executionRequested = $true
    offlineScanAutomatic = $false
    secureBootChanged = $false
    tpmChanged = $false
    diskLayoutChanged = $false
    productionChanged = $false
}
$resultPath = Join-Path $EvidenceDirectory "defender-recovery-$($Mode.ToLowerInvariant())-$timestamp.json"
$result | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $resultPath -Encoding UTF8
$result | ConvertTo-Json -Depth 8
Write-Host "HELIOS Defender recovery evidence: $resultPath" -ForegroundColor Green
