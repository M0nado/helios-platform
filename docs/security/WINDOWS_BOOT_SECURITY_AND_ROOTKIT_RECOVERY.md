# HELIOS Windows boot security and rootkit recovery

## Operating model

The startup path is deliberately split into two layers:

1. **Automatic, non-destructive startup audit** — records Secure Boot, TPM, Defender, Firewall, WinRE, BitLocker, VBS/HVCI, vulnerable-driver protection, startup persistence, and recent Defender/Code Integrity events.
2. **Explicit remediation** — applies the reviewed baseline, enables compatibility-sensitive VBS controls only when requested, or starts Microsoft Defender Offline after an exact confirmation phrase.

A Defender Offline scan is never scheduled at every boot. It restarts the computer and runs outside the normal Windows kernel, so HELIOS keeps it as an operator-confirmed incident-recovery action.

## Files

- `Get-HeliosBootSecurityPosture.ps1` — report only.
- `Set-HeliosWindowsSecurityBaseline.ps1` — Defender, firewall, ASR, driver blocklist, optional HVCI/Credential Guard.
- `Install-HeliosBootSecurityTasks.ps1` — startup audit plus daily quick and weekly full scans.
- `Invoke-HeliosRootkitRecovery.ps1` — readiness, quick, full, and guarded offline scan.
- `config/security/windows-boot-security.v1.json` — machine-readable policy.

## Safe deployment order

Run from an elevated PowerShell session after cloning the reviewed branch.

```powershell
# 1. Audit only
pwsh -NoProfile -File .\scripts\windows\security\Get-HeliosBootSecurityPosture.ps1

# 2. Preview baseline changes
pwsh -NoProfile -File .\scripts\windows\security\Set-HeliosWindowsSecurityBaseline.ps1 -Profile Audit -WhatIf

# 3. Apply the audit-first baseline
pwsh -NoProfile -File .\scripts\windows\security\Set-HeliosWindowsSecurityBaseline.ps1 -Profile Audit

# 4. Install durable startup and scan tasks
pwsh -NoProfile -File .\scripts\windows\security\Install-HeliosBootSecurityTasks.ps1
```

Enable compatibility-sensitive protections only after reviewing driver state and recovery access:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Set-HeliosWindowsSecurityBaseline.ps1 `
  -Profile Balanced `
  -EnableMemoryIntegrity `
  -EnableCredentialGuard
```

## Rootkit recovery

Readiness check:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Invoke-HeliosRootkitRecovery.ps1 -Mode Readiness
```

Offline scan, after saving work and verifying the BitLocker recovery key:

```powershell
pwsh -NoProfile -File .\scripts\windows\security\Invoke-HeliosRootkitRecovery.ps1 `
  -Mode OfflineScan `
  -EnableWinRE `
  -SuspendBitLockerForOneReboot `
  -Confirmation 'RUN HELIOS OFFLINE ROOTKIT SCAN'
```

## Firmware settings

HELIOS reports but does not blindly change firmware. On the Razer Blade 18 target, the reviewed firmware posture is:

- UEFI mode enabled.
- Legacy/CSM disabled.
- TPM 2.0 / Security Device Support enabled.
- Secure Boot enabled after recovery and driver validation.
- Fast Boot disabled during diagnosis and recovery.
- Current OEM BIOS, firmware, and Windows Secure Boot certificate updates installed.

## Safety boundaries

- No automatic offline scan or reboot loop.
- No Secure Boot, TPM, or BitLocker reset.
- No broad Defender exclusions.
- No forced HVCI/UEFI lock without explicit opt-in.
- No raw security logs or secrets sent to AI services.
- OpenAI incident summarization is disabled by default and accepts only redacted operator-approved summaries.
