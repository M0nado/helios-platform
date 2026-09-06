# HELIOS Windows Recovery and Security V2

## Purpose

This lane replaces the stale environment-repair and boot-security branches with a fresh-main, audit-first implementation. It separates standard-user diagnostics from administrator-only mutation and never treats source-code merge as permission to change a workstation.

## Safe order

```text
1. Audit environment variables and critical tools
2. Repair only the current process if command resolution is broken
3. Re-open PowerShell and verify absolute Windows tool paths
4. Audit boot and endpoint-security posture
5. Preview user or machine environment changes
6. Apply machine environment repair only from an elevated terminal
7. Preview the Defender/Firewall/ASR baseline with -WhatIf
8. Apply the reviewed baseline
9. Install startup audit and daily quick-scan tasks
10. Run Defender Offline only during an incident, with recovery evidence
```

## 1. Environment audit

No administrator rights are required:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\environment\Repair-HeliosEnvironment.ps1 `
  -Mode Audit
```

## 2. Current-process repair

This changes only the current PowerShell process and is the first response when `sfc`, `DISM`, `winmgmt`, `netsh`, or `pnputil` cannot be resolved:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\environment\Repair-HeliosEnvironment.ps1 `
  -Mode Process
```

The adapter verifies `winmgmt.exe` at the correct path:

```text
%SystemRoot%\System32\Wbem\winmgmt.exe
```

It does not run any system-repair command.

## 3. Current-user repair

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\environment\Repair-HeliosEnvironment.ps1 `
  -Mode User
```

Existing `PATH` entries are preserved. The WindowsApps path is appended only when missing. User `TEMP` and `TMP` resolve to the current user's local application-data directory.

## 4. Machine repair

Machine writes require elevation and an exact phrase:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\environment\Repair-HeliosEnvironment.ps1 `
  -Mode Machine `
  -Confirmation 'REPAIR HELIOS MACHINE ENVIRONMENT'
```

The script creates a JSON backup before writing. It never deletes existing `PATH` entries, invokes SFC/DISM, resets networking, changes drivers, modifies security policy, or reboots.

## 5. Boot-security posture

The posture collector is redacted and non-mutating:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Get-HeliosBootSecurityPosture.ps1
```

It records:

- Secure Boot and TPM readiness;
- Defender and Tamper Protection state;
- Firewall profiles;
- WinRE and BitLocker state;
- VBS, HVCI, Code Integrity, and LSA state;
- vulnerable-driver blocklist state;
- hashes—not plaintext—of startup command lines;
- event IDs and timestamps—not full event messages.

BitLocker recovery passwords and secret values are never written to evidence.

## 6. Preview and apply baseline

Preview:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Set-HeliosWindowsSecurityBaseline.ps1 `
  -Profile Audit `
  -Confirmation 'APPLY HELIOS WINDOWS SECURITY BASELINE' `
  -WhatIf
```

Apply the audit-first baseline:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Set-HeliosWindowsSecurityBaseline.ps1 `
  -Profile Audit `
  -Confirmation 'APPLY HELIOS WINDOWS SECURITY BASELINE'
```

The baseline backs up Defender preferences, Firewall policy, Device Guard, Code Integrity, and LSA state. Existing ASR rules are preserved and HELIOS-managed rules are merged.

Memory Integrity and Credential Guard remain explicit opt-ins:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Set-HeliosWindowsSecurityBaseline.ps1 `
  -Profile Balanced `
  -EnableMemoryIntegrity `
  -EnableCredentialGuard `
  -Confirmation 'APPLY HELIOS WINDOWS SECURITY BASELINE'
```

They are configured without UEFI lock to preserve a recovery path for incompatible drivers. The script does not change firmware Secure Boot or reset the TPM.

## 7. Install recurring tasks

The default task set is:

```text
At startup:  redacted posture audit
Daily:       Microsoft Defender quick scan
Weekly full: not installed unless explicitly selected
Offline:     never scheduled
```

Install:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Install-HeliosSecurityAuditTasks.ps1 `
  -Confirmation 'INSTALL HELIOS SECURITY TASKS'
```

Optional weekly full scan:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Install-HeliosSecurityAuditTasks.ps1 `
  -InstallWeeklyFullScan `
  -WeeklyFullScanDay Sunday `
  -WeeklyFullScanTime '03:00' `
  -Confirmation 'INSTALL HELIOS SECURITY TASKS'
```

## 8. Defender recovery

Readiness only:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Invoke-HeliosDefenderRecovery.ps1 `
  -Mode Readiness
```

Quick scan:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Invoke-HeliosDefenderRecovery.ps1 `
  -Mode QuickScan `
  -UpdateSignatures
```

Full scan:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Invoke-HeliosDefenderRecovery.ps1 `
  -Mode FullScan `
  -Confirmation 'RUN HELIOS FULL DEFENDER SCAN'
```

## 9. Defender Offline

Defender Offline is an incident-recovery action, not an on-boot loop. Before running it:

1. Save all work.
2. Verify WinRE.
3. Determine BitLocker protection state.
4. Verify the recovery key exists in an approved offline or enterprise escrow.
5. Create a secret-free receipt from `config/windows/bitlocker-recovery-evidence.template.json`.
6. Never place the recovery password itself in that receipt.

Run:

```powershell
pwsh -NoProfile -File `
  .\scripts\windows\security\v2\Invoke-HeliosDefenderRecovery.ps1 `
  -Mode OfflineScan `
  -EnableWinRE `
  -SuspendBitLockerForOneReboot `
  -RecoveryEvidencePath '.\recovery-evidence.local.json' `
  -Confirmation 'RUN HELIOS OFFLINE ROOTKIT SCAN'
```

The operation refuses to continue when BitLocker state is unknown. When protection is on, it permits at most one reboot of suspension and requires the secret-free recovery receipt. `Start-MpWDOScan` may restart the computer immediately.

## Prohibited behavior

This implementation contains no automatic disk formatting, partition mutation, TPM reset, Secure Boot mutation, permanent BitLocker disablement, broad Defender exclusions, automatic Defender Offline task, reboot loop, Azure deployment, or production enablement.
