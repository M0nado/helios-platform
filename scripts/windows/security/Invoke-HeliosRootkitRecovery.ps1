#Requires -Version 5.1
<#
.SYNOPSIS
    Runs guarded Microsoft Defender scans, including an explicit offline rootkit scan.

.DESCRIPTION
    Readiness is the default and performs no mutation. OfflineScan requires an exact
    confirmation phrase because it can suspend BitLocker for one reboot and restart
    the computer into Microsoft Defender Offline.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [ValidateSet('Readiness','QuickScan','FullScan','OfflineScan')]
    [string]$Mode = 'Readiness',
    [string]$Confirmation,
    [switch]$EnableWinRE,
    [switch]$SuspendBitLockerForOneReboot,
    [string]$StateDirectory = "$env:ProgramData\HELIOS\Security\State"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$requiredConfirmation = 'RUN HELIOS OFFLINE ROOTKIT SCAN'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    throw 'Run this script from an elevated PowerShell session.'
}

New-Item -ItemType Directory -Path $StateDirectory -Force | Out-Null
$systemRoot = if ($env:SystemRoot) { $env:SystemRoot } else { 'C:\Windows' }
$reagentc = Join-Path $systemRoot 'System32\reagentc.exe'
$winReInfo = (& $reagentc /info 2>&1 | Out-String).Trim()
$winReEnabled = $winReInfo -match 'Windows RE status:\s+Enabled'
$defender = Get-MpComputerStatus
$bitLocker = $null
if (Get-Command Get-BitLockerVolume -ErrorAction SilentlyContinue) {
    $bitLocker = Get-BitLockerVolume -MountPoint $env:SystemDrive
}

$readiness = [ordered]@{
    GeneratedUtc = (Get-Date).ToUniversalTime().ToString('o')
    Mode = $Mode
    DefenderAntivirusEnabled = [bool]$defender.AntivirusEnabled
    DefenderRealtimeProtectionEnabled = [bool]$defender.RealTimeProtectionEnabled
    DefenderSignaturesOutOfDate = [bool]$defender.DefenderSignaturesOutOfDate
    WinREEnabled = [bool]$winReEnabled
    BitLockerProtectionStatus = if ($bitLocker) { [string]$bitLocker.ProtectionStatus } else { 'unknown' }
    ExactOfflineConfirmation = $requiredConfirmation
}

if ($Mode -eq 'Readiness') {
    $readiness | ConvertTo-Json -Depth 6
    return
}

if ($PSCmdlet.ShouldProcess('Microsoft Defender signatures', 'Update')) {
    Update-MpSignature
}

switch ($Mode) {
    'QuickScan' {
        if ($PSCmdlet.ShouldProcess('Microsoft Defender', 'Run quick scan')) {
            Start-MpScan -ScanType QuickScan
        }
    }
    'FullScan' {
        if ($PSCmdlet.ShouldProcess('Microsoft Defender', 'Run full scan')) {
            Start-MpScan -ScanType FullScan
        }
    }
    'OfflineScan' {
        if ($Confirmation -cne $requiredConfirmation) {
            throw "Offline scan blocked. Pass -Confirmation '$requiredConfirmation'."
        }

        if (-not $winReEnabled) {
            if (-not $EnableWinRE) {
                throw 'Windows Recovery Environment is disabled. Re-run with -EnableWinRE after reviewing the recovery configuration.'
            }
            if ($PSCmdlet.ShouldProcess('Windows Recovery Environment', 'Enable')) {
                & $reagentc /enable | Out-Host
                $winReInfo = (& $reagentc /info 2>&1 | Out-String).Trim()
                if ($winReInfo -notmatch 'Windows RE status:\s+Enabled') {
                    throw 'WinRE did not report Enabled after reagentc /enable.'
                }
            }
        }

        if ($bitLocker -and [string]$bitLocker.ProtectionStatus -eq 'On') {
            if (-not $SuspendBitLockerForOneReboot) {
                throw 'BitLocker protection is on. Re-run with -SuspendBitLockerForOneReboot or suspend it manually and retain the recovery key.'
            }
            if ($PSCmdlet.ShouldProcess($env:SystemDrive, 'Suspend BitLocker for one reboot')) {
                Suspend-BitLocker -MountPoint $env:SystemDrive -RebootCount 1 | Out-Null
            }
        }

        $marker = [ordered]@{
            RequestedUtc = (Get-Date).ToUniversalTime().ToString('o')
            ComputerName = $env:COMPUTERNAME
            Mode = 'MicrosoftDefenderOffline'
            ExpectedEventId = 2030
            State = 'queued'
        }
        $markerPath = Join-Path $StateDirectory 'offline-scan-pending.json'
        $marker | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $markerPath -Encoding UTF8

        if ($PSCmdlet.ShouldProcess($env:COMPUTERNAME, 'Restart into Microsoft Defender Offline')) {
            Start-MpWDOScan
        }
    }
}
