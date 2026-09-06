# HELIOS Windows Boot Security and Rootkit Recovery V2

## Operating model

The Windows security lane is split into explicit layers:

1. **Standard-user environment audit** — inspects deleted or damaged environment variables and verifies absolute Windows tool locations.
2. **Automatic startup posture audit** — reports Secure Boot, TPM, Defender, Firewall, WinRE, BitLocker, VBS/HVCI, driver-blocklist, startup persistence, and security-event metadata.
3. **Administrator-reviewed baseline** — backs up Defender, Firewall, Device Guard, Code Integrity, and LSA state, then applies only explicitly selected controls.
4. **Operator-confirmed incident recovery** — runs quick, full, or Microsoft Defender Offline scanning. Offline scanning is never scheduled automatically.

## Safe order

Run the environment audit first from ordinary PowerShell:

```powershell
pwsh -NoProfile -File .\scripts\windows\Repair-HeliosEnvironment.ps1
```

Apply user-scope restoration only after reviewing the JSON plan:

```powershell
pwsh -NoProfile -File .\scripts\windows\Repair-HeliosEnvironment.ps1 -Apply
```

Machine-scope restoration requires elevation and remains separate:

```powershell
pwsh -NoProfile -File .\scripts\windows\Repair-HeliosEnvironment.ps1 `
  -Apply `
  -IncludeMachine
```

Then collect the security posture without changing it:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Get-HeliosBootSecurityPosture.ps1
```

Preview the administrator baseline:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Set-HeliosWindowsSecurityBaseline.ps1 `
  -Profile Audit `
  -WhatIf
```

Apply audit-mode Defender/ASR controls and Firewall defaults:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Set-HeliosWindowsSecurityBaseline.ps1 `
  -Profile Audit
```

Compatibility-sensitive controls stay explicit:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Set-HeliosWindowsSecurityBaseline.ps1 `
  -Profile Balanced `
  -EnableVulnerableDriverBlocklist `
  -EnableMemoryIntegrity `
  -EnableCredentialGuard
```

Install the startup audit and daily quick scan:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Install-HeliosBootSecurityTasks.ps1
```

Add a weekly full scan only when deliberately requested:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Install-HeliosBootSecurityTasks.ps1 `
  -InstallWeeklyFullScan `
  -WeeklyFullScanDay Sunday `
  -WeeklyFullScanTime '03:00'
```

## Defender Offline rootkit recovery

Readiness is non-mutating:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Invoke-HeliosRootkitRecovery.ps1 `
  -Mode Readiness
```

The offline path fails closed when BitLocker state is unknown. When BitLocker protection is on, it requires a separate **non-secret recovery-evidence receipt** and explicit one-reboot suspension. The evidence receipt proves the operator verified recovery access; it must not contain the recovery password itself.

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Invoke-HeliosRootkitRecovery.ps1 `
  -Mode OfflineScan `
  -EnableWinRE `
  -SuspendBitLockerForOneReboot `
  -RecoveryEvidencePath 'C:\ApprovedEvidence\bitlocker-recovery-access-receipt.json' `
  -Confirmation 'RUN HELIOS OFFLINE ROOTKIT SCAN'
```

The final command intentionally restarts the computer into Microsoft Defender Offline. It does not create a recurring task or reboot loop.

## Firmware posture

HELIOS reports but does not blindly mutate firmware. The reviewed target posture is:

- UEFI enabled;
- Legacy/CSM disabled;
- TPM 2.0 enabled and ready;
- Secure Boot enabled after driver/recovery validation;
- Fast Boot disabled during diagnosis and recovery;
- current OEM BIOS, firmware, and Windows Secure Boot certificate updates.

## Hard boundaries

This lane never performs:

- disk formatting or partition mutation;
- TPM clearing;
- Secure Boot key reset;
- permanent BitLocker disablement;
- broad Defender exclusions;
- automatic offline scans;
- automatic reboot loops;
- Azure, Entra, RBAC, Graph, Key Vault, or production deployment operations.
