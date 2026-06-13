# Cross-Repo Consolidation Runbook

This repository cannot safely merge external HELIOS repositories until each source in
`MERGE_SOURCE_MANIFEST.yaml` is reachable and reviewed. The consolidation tooling is
therefore designed to fail closed and generate auditable reports before any Git remote,
submodule, subtree, or branch merge operation occurs.

## Priority order

1. `helios-control` — C# WinUI 3 front end, control plane, user experience.
2. `hermes-fleet-production` — F# analytics, prediction, fleet optimization, parallel math.
3. `helios-monado-blade` — C++ performance backend, native security, Hermes XCore hooks.
4. `helios-security-setup` — hardening and compliance.
5. `helios-ai-hub` / `helios-dev-ai-hub` — Python AI Hub integration and model orchestration.
6. `helios-build-agents` — GitHub Actions, Azure CLI, release automation, cloud provisioning.
7. Remaining GUI/software stack repositories from the manifest.

## Commands

```bash
# Online pre-merge gate. Fails if any external source is private/unreachable.
python3 scripts/github/prepare-consolidation.py --online --dry-run --write-gitmodules-preview --fail-on-unreachable

# CI/reporting mode. Produces reports but does not fail when private repos require credentials.
python3 scripts/github/prepare-consolidation.py --online --dry-run --write-gitmodules-preview --allow-unreachable

# Offline planning mode. Does not contact GitHub; useful for generating the .gitmodules preview from the manifest.
python3 scripts/github/prepare-consolidation.py --offline --dry-run --write-gitmodules-preview

# Backward-compatible wrapper for environments that use underscores in script names.
python3 scripts/github/prepare_consolidation.py --offline --dry-run --write-gitmodules-preview
```

## Generated artifacts

- `artifacts/consolidation/consolidation-report.json` records source ids, branches, modes, reachability, SHAs, and errors.
- `artifacts/consolidation/gitmodules.preview` is a review-only preview. Do not copy it to `.gitmodules` until all sources pass the online pre-merge gate.

## Apply mode

Only after the online gate passes, use:

```bash
python3 scripts/github/prepare-consolidation.py --online --apply --write-gitmodules-preview --fail-on-unreachable
```

`--apply` adds or updates Git remotes and fetches the configured branches. It still does **not** merge branches, add submodules, or perform subtree merges automatically.
