# HELIOS.Installer

C#-first installer, USB wizard, recovery creator, and post-install orchestration module.

## Purpose

HELIOS.Installer owns safe planning and orchestration for:

- USB autoinstaller media
- Windows recovery / WinRE planning
- Defender Offline readiness
- partition planning
- locked-down first-user setup
- security baseline staging
- driver/software bundle planning
- DevDrive, WSL2, AIHub, and vault setup

## Rule

C# orchestrates. PowerShell adapts. Python is reference unless explicitly promoted.

## Planned services

```text
BootMediaPlanner
RecoveryMediaPlanner
PartitionLayoutPlanner
SecurityBaselinePlanner
DriverBundlePlanner
SoftwareBundlePlanner
AccountBootstrapPlanner
ExecutionEvidenceWriter
ExternalScriptRunner
ConfirmationGate
```

## Execution modes

```text
PlanOnly
DryRun
ApplyWithApproval
ElevatedApplyWithApproval
```

## Hard guardrails

- No disk formatting without exact disk identity.
- No offline scan reboot without explicit phrase.
- No BitLocker action without recovery-key evidence.
- No Azure deployment.
- No plaintext secrets.
- No silent driver installation.
- No profile elevation without SysAdmin/local approval.

## USB wizard stages

1. Preflight hardware/security inventory
2. Trust and isolation warning
3. USB target selection
4. Recovery plan
5. Partition plan
6. Profile/user plan
7. Security baseline plan
8. Driver/software bundle plan
9. AIHub/WSL2/DevDrive/vault plan
10. Final review
11. Execute or export plan
12. Evidence and rollback
