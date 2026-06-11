# HELIOS GitHub automation setup

This repository uses guarded automation instead of blind cross-repository merges.

## Consolidation workflow

1. `MERGE_SOURCE_MANIFEST.yaml` is the source of truth for external repositories, merge order, owner domains, and consolidation mode.
2. `scripts/github/prepare-consolidation.py` verifies every source branch and records the exact SHA before any fetch or merge work.
3. The GitHub workflow runs in dry-run mode by default. Use `apply_remotes=true` only after the source-readiness report says `safe_to_merge: true`.

## CI workflow hygiene

Run this locally or in CI:

```bash
python3 scripts/github/validate-workflows.py
```

The validator catches stale action versions, missing file references, build-output caching, and `ContinueOnError=true` build masking.

## Azure CLI setup

GitHub-hosted deploy jobs should use OIDC via `azure/login` and should not store long-lived client secrets. The helper scripts are:

- `scripts/deploy/azure/setup-azure-cli.sh`
- `scripts/deploy/azure/verify-azure-cli.sh`

Required repository/environment variables and secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

The workflow must grant `id-token: write` for OIDC login.
