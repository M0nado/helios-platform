# Monado enterprise experience fabric v2

## Scope

This document defines the canonical v2 integration fabric for issue #206.  
All contracts are owned in `M0nado/helios-platform` and remain **proposal-only** for privileged/destructive operations.

## System spine

1. Canonical contract map: `config/monadoblade/experience-fabric/*.v2.json`
2. Typed enforcement: C# contracts + F# scoring + C++ read-only extraction
3. Contract validation: dependency-free Python validator + CI workflow
4. Information architecture: README + architecture docs + Pages source links

## Ownership and non-duplication boundaries

- Canonical platform (`M0nado/helios-platform`): contracts, policy, routing, docs, validation.
- Dedicated GUI implementation: issue scopes #172/#195 (linked, not duplicated here).
- Dedicated USB Wizard: issue scope #171 (linked, not duplicated here).
- AIHub/ALVIS: governed adapter and bounded assistant behavior; never autonomous administrator.
- Azure DevOps remains read-only until separately approved write identity.

## Profile and storage model

- Permanent profile catalog: `core`, `developer`, `gamer`, `studio`, `personal`, `sysops`, `ai-server`, `sysadmin`.
- Recovery and Quarantine are states, not permanent profiles.
- Airgap remains an overlay.
- Storage contract v2 defines:
  - Disk 0 (`C`, `R`, physical `X: CORE_CROSS`)
  - Disk 1 domains volume with ACL-separated roots
  - Dynamic `D:` ReFS Dev Drive VHDX
  - Dynamic encrypted `V:` Vault VHDX, never auto-mounted
  - unresolved exact-size lock and no runtime mutation

## USB and boot handoff

USB/boot implementation stays in the dedicated USB module path.  
The v2 canonical contract only defines handoff boundaries and approval expectations:

- contract-level references and requirements,
- no physical media writes from runtime automation,
- proposal + approval + evidence + rollback for privileged transitions.

## Synchronization and external systems

The v2 synchronization contract enforces:

- idempotent route keys;
- normalized event envelope fields (including correlation and evidence links);
- GitHub as engineering authority;
- Linear as work authority;
- Slack/Teams as bounded checkpoint surfaces;
- SharePoint as governed evidence store;
- Azure DevOps as read-only mirror until explicit approval identity exists.

## CODEOWNERS-ready path map

Path ownership entries are published in:

- `config/monadoblade/experience-fabric/repository-ownership.contract.v2.json`

These entries are designed to be copied directly into a future `CODEOWNERS` file without changing canonical source locations.

## Contribution and fork strategy

1. Open/associate a scoped issue.
2. Branch from `main` and keep changes in `M0nado/helios-platform`.
3. Keep GUI/USB implementation in their dedicated modules; consume contracts by reference.
4. Run v2 validators and focused language checks before PR submission.
5. Publish immutable evidence links in PR/SharePoint records when applicable.
