# HELIOS Mainline Consolidation V1

## Purpose

This document is the review boundary for consolidating the HELIOS/Monadoblade implementation onto the current `M0nado/helios-platform` `main` branch without reviving stale branches, conflicting profile models, WPF-era UI paths, unsafe prototype services, or unrestricted legacy automation.

## Canonical architecture

### Desktop

The active desktop implementation is **C#/.NET with WinUI 3 and Microsoft.UI.Xaml**. WPF, `System.Windows`, `PresentationFramework`, `PresentationCore`, and PowerShell-hosted WPF are prohibited in active product code. Historical WPF material may remain only under an explicitly inert `legacy/` or `reference/` path.

### Profiles

The canonical identity wheel contains exactly six profiles:

| Profile | Glyph | Boundary |
|---|---|---|
| Core | 核 | trusted baseline and shared control-plane services |
| Developer | 創 | standard-user development, builds, tests, and repository work |
| Studio | 響 | audio, media, rendering, and low-latency creative workflows |
| Gamer | 迅 | gaming and latency-sensitive performance policy |
| AI / Server | 智 | AIHub, containers, services, model workers, and telemetry |
| Sysadmin | 統 | strictly local/offline privileged maintenance |

Recovery and Quarantine are security workflows, not login identities.

## Runtime boundary

The production AIHub path must be:

- loopback-first by default;
- authenticated on every non-health route;
- bounded by request-size and rate limits;
- atomic for queue and state writes;
- proposal-only for VM, optimization, training, and administrative actions unless a separate approval contract authorizes execution;
- incapable of secret readback;
- incapable of silently enabling Azure or production.

The historical prototype server that binds to `0.0.0.0` is reference material only and must not be used as the production entry point.

## Windows recovery order

1. Audit process and machine environment variables.
2. Restore current-process command resolution first.
3. Verify `System32` and `System32\Wbem` tools by absolute path.
4. Require elevation before machine-level environment writes.
5. Run security posture collection before policy mutation.
6. Keep weekly full scans and Defender Offline explicit.
7. Require WinRE, BitLocker recovery evidence, and an exact confirmation before an offline scan.

No source merge formats disks, changes partitions, resets TPM, changes Secure Boot, disables BitLocker, or installs a reboot loop.

## Azure and identity boundary

The repository foundation may contain Bicep, OIDC, Entra, Key Vault, Container Apps, ACR, Foundry, Microsoft 365, and connector contracts. Repository merge does not mean those resources are live.

The activation sequence remains separate:

1. Resolve the live GitHub OIDC subject.
2. Protect `azure-dev`, build, and deploy environments.
3. Establish environment-bound workload federation.
4. Grant only resource-group or resource-scoped RBAC.
5. Bind managed identity to Key Vault and ACR.
6. Publish a digest-pinned image.
7. Run and preserve an exact development what-if.
8. Review the canonical plan and hash.
9. Request deployment through a separate protected environment.
10. Authorize ChatGPT and Microsoft tenant connectors through reviewed delegated scopes.

Production remains disabled until direct evidence proves each required authorization and approval.

## Source consolidation rules

- Always branch from current `origin/main`.
- Compare every incoming file by SHA-256.
- Classify each file as new, identical, or conflict.
- Block unreviewed conflicts.
- Back up approved conflicts under Git metadata, not tracked source.
- Never push directly to `main`.
- Require native PowerShell AST parsing on `windows-latest`.
- Require all configured required checks and an approved review on the exact head SHA.
- Merge with expected-head enforcement.
- Close superseded branches only after replacement equivalence is verified.

## Secret policy

No API key, OAuth token, client secret, certificate private key, authorization header, BitLocker recovery password, or Key Vault secret value may appear in source, pull-request text, CI logs, Slack, Linear, SharePoint, Google Drive, or chat. The OpenAI key previously pasted into chat is considered exposed and must remain revoked.

## Current source package evidence

The locally sealed final merge operator is identified by:

```text
HELIOS_FINAL_MAINLINE_MERGE_OPERATOR_v1.zip
SHA-256: 4fd2e3dc194a07c3ab919794e510cabb8c7e3705e98e24ce9072ac3bc317d482
```

Its reported validation state is:

```text
Locked source archives:             4 / 4 verified
Source ZIP integrity:               passed
Secret-pattern findings:            0
User-mode runtime tests:            12 passed
Security/profile/UI contract tests: 6 passed
Total Python tests:                 18 passed
Python compilation:                 passed
Active-source safety validation:    passed
Canonical profile-wheel contract:   passed
WinUI 3-only contract:              passed
Generated overlay files:            145
Azure apply enabled:                false
Production enabled:                 false
OpenAI secret included:             false
External writes performed:          0
```

These local results do not replace hosted validation on the exact target-repository commit.
