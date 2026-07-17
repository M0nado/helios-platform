# HELIOS XTier / WinRE Upload Merge v6

## Decision

This pull request absorbs only the verified Windows diagnostics and the promoted,
audit-first repair and security adapters. The uploaded XTier, WinRE, AIHub, apps,
and drivers sources were inventoried as **legacy source material**; they are not
committed or activated by this pull request.

The canonical split remains:

- C#/.NET: control plane, GUI, policy, state, and enterprise contracts.
- PowerShell: narrow Windows adapters with audit-first defaults and explicit elevation.
- Python: AIHub routing, engine catalog, report generation, and optional self-teaching sidecars.
- Bicep/Azure CLI: staged infrastructure with OIDC, what-if, approvals, and Key Vault.
- Legacy XTier/WinRE: inert evidence and source material until behavior is promoted into reviewed modules.

## Material absorbed in this pull request

1. Normalized copies of the local-repair and master diagnostic logs.
2. The audit-first Windows environment repair adapter.
3. Windows boot-security policy, recovery scripts, tests, and operator guidance.

## Material reviewed but not absorbed

1. The apps/drivers integration pack and its software, hardware, profile,
   partition, security, GUI, and solution-design material.
2. The AIHub Python control server, CLI, engine catalog, model registry, VM
   topology, security optimizer, transcript integrator, and training-loop
   references.
3. The raw XTier/WinRE, Microsoft 365, Purview, disk, Azure, hardening, DevStack,
   and deployment sources.
4. `.reboot_needed`, which is stale machine state rather than portable source.
5. `part13_v4-FINAL.txt.crdownload`, which is an incomplete browser download and
   must not be executed or committed.

The Python control server is local-only reference code until it has server-side
authentication, authorization, bounded input handling, durable state, and tests.
The remaining pack belongs in a new, focused pull request from current `main`,
with checksums, typed contracts, schemas, and tests before any behavior is
promoted.

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

1. Complete and review this Windows repair/security pull request (#186).
2. Repair the Azure connector validation failures in #183; keep apply disabled.
3. Use #184 as the current-main architecture anchor for Platform2 storage,
   profiles, and install routing.
4. Salvage only the still-relevant app/plugin pieces from stale #177 into a new,
   focused branch; do not merge the stale branch wholesale.
5. Map the apps/drivers catalog into current typed C# contracts and promote one
   legacy phase at a time with schemas, tests, dry-run behavior, and rollback
   evidence.
6. Keep the raw upload outside the live runtime until each promoted component
   passes its own review and validation gate.
