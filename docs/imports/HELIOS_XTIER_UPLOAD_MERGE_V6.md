# HELIOS XTier / WinRE Upload Merge v6

## Decision

The uploaded material is absorbed into `M0nado/helios-platform` as a **legacy bridge**, not revived as an unrestricted mega-script.

The canonical split remains:

- C#/.NET: control plane, GUI, policy, state, and enterprise contracts.
- PowerShell: narrow Windows adapters with audit-first defaults and explicit elevation.
- Python: AIHub routing, engine catalog, report generation, and optional self-teaching sidecars.
- Bicep/Azure CLI: staged infrastructure with OIDC, what-if, approvals, and Key Vault.
- Legacy XTier/WinRE: inert evidence and source material until behavior is promoted into reviewed modules.

## Material absorbed

1. Full apps/drivers integration pack, including software catalog, hardware detection, profiles, partitions, security manifest, GUI architecture, and C# single-solution plan.
2. AIHub Python control server, CLI, engine catalog, registry, VM topology, security optimizer, transcript integrator, and training-loop references.
3. XTier local repair, TPM, M365, Purview, disk, Azure, hardening, DevStack, and combined runbook sources.
4. WinRE complete suite, deployment launcher, and GitHub/Azure/agentic-learning extension.
5. Original diagnostics and prior merge/design notes.

## Critical repair incorporated

The diagnostics show `sfc`, `DISM`, `winmgmt`, `netsh`, and `pnputil` were not found, while the master run stopped because it was not elevated. The v6 environment repair adapter therefore:

- uses a known Windows-root fallback;
- audits before writing;
- backs up environment state to JSON;
- preserves existing PATH entries;
- restores only missing Windows baseline entries;
- separates user and machine scopes;
- requires elevation for machine writes;
- verifies the exact System32 executables by absolute path;
- broadcasts the environment change and recommends reboot only when needed.

## Promotion rules

Legacy behavior may move into live HELIOS modules only when it has:

1. an explicit contract and owner;
2. report/dry-run mode;
3. preconditions and authorization checks;
4. rollback evidence;
5. tests;
6. no plaintext secrets;
7. no direct-to-main mutation;
8. separate approval for disk, BitLocker, Entra, Graph, Purview, RBAC, and production deployment.

## Immediate integration order

1. Merge the environment repair and audit adapters.
2. Merge the package-safe AIHub bridge and tests.
3. Keep raw sources inert under `legacy/`.
4. Map apps/drivers catalog data into current C# contracts.
5. Promote one legacy phase at a time into typed, testable HELIOS services.
6. Use issue #159 for the PowerShell/PATH blocker and keep PR #151 gated until required CI is clean.
