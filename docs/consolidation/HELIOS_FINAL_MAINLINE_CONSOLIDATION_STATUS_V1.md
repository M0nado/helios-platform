# HELIOS Final Mainline Consolidation Status V1

## Binding architecture

- Desktop framework: C#/.NET with WinUI 3 only.
- Canonical profile wheel: Core `核`, Developer `創`, Studio `響`, Gamer `迅`, AI/Server `智`, and local/offline Sysadmin `統`.
- Recovery and Quarantine are security workflows rather than user identities.
- Production AIHub entry points must be loopback-first, authenticated, bounded, rate-limited, atomic, and approval-gated.
- Legacy XTier and WinRE sources remain inert evidence unless promoted through typed and tested adapters.

## Retained foundations

- PR #182: Azure development CLI/OIDC foundation.
- PR #188: Azure/Edge runtime.
- PR #190: unified MCP/plugin control plane.
- PR #193: live OIDC subject resolution.
- PR #186: Windows environment repair and boot-security replacement lane.
- Old PRs #163 and #166 must not be merged independently after replacement verification.

## Locked operator artifacts

- `HELIOS_FINAL_MAINLINE_MERGE_OPERATOR_v1.zip`
- SHA-256: `4fd2e3dc194a07c3ab919794e510cabb8c7e3705e98e24ce9072ac3bc317d482`
- `HELIOS_MAINLINE_SYNC_PACKET_v1.zip`
- SHA-256: `2d9d2393d770fbfeb83dda71a9d2990bc2a8753631eaf81f4119b3536d20b7d5`

Verified local evidence:

- 4/4 source archives verified;
- source ZIP integrity passed;
- zero secret-pattern findings;
- 12 user-mode runtime tests passed;
- 6 security/profile/UI contract tests passed;
- 18 Python tests passed in total;
- Python compilation passed;
- active-source safety validation passed;
- canonical profile-wheel contract passed;
- WinUI 3-only contract passed;
- 145 overlay files generated;
- Azure apply disabled;
- production disabled;
- no OpenAI secret included.

## Required source-merge sequence

1. Start from fresh current `origin/main`.
2. Generate a file-by-file SHA-256 overlay plan.
3. Classify every path as new, identical, or conflict.
4. Review and explicitly allowlist each conflict.
5. Apply to a fresh consolidation branch and back up reviewed conflicts under `.git`.
6. Verify every copied file and run the complete test/safety suite.
7. Open a draft pull request.
8. Require native PowerShell AST parsing on `windows-latest` and all configured checks.
9. Require approved review on the unchanged exact head SHA.
10. Squash merge with expected-head enforcement.
11. Close superseded PRs only after file-level replacement equivalence is proven.

## Separate Azure boundary

The source merge does not authenticate Azure, create federation, assign RBAC, write Key Vault secrets, publish an image, execute `what-if`, deploy infrastructure, grant tenant consent, activate connectors, or enable production. Those remain separate administrator-approved operations with their own evidence and deployment approval.
