# Quick Reference: Build Workflows

## Trigger summary

| Workflow | Automatic trigger | Manual inputs |
| --- | --- | --- |
| `multi-repo-sync.yml` | Push changes to submodule/integration mapping files | `apply_changes`, `target_ref` |
| `component-version-check.yml` | Push/PR changes to version-bearing manifests | `strict`, `verbose` |
| `build-all-modules.yml` | Push/PR non-doc code changes | `clean_build`, `include_node_targets` |
| `build-variant-test.yml` | Push/PR source and tests | `variants` |
| `code-registry-update.yml` | Push to `main` on source/workflow changes | `apply_changes`, `compression_level` |
| `status-dashboard.yml` | Hourly schedule and workflow-file push | none |

## Common `gh` commands

```bash
# Manual workflow runs
gh workflow run multi-repo-sync.yml -f apply_changes=false
gh workflow run component-version-check.yml -f strict=true -f verbose=true
gh workflow run build-all-modules.yml -f clean_build=true
gh workflow run build-variant-test.yml -f variants=development,test
gh workflow run code-registry-update.yml -f apply_changes=false -f compression_level=6
gh workflow run status-dashboard.yml

# View recent run list
gh run list --limit 20

# Inspect one run
gh run view <RUN_ID>
gh run view <RUN_ID> --log
```

## Safe mutation flow

1. Run mutating workflows in dry-run mode first (`apply_changes=false`).
2. Download and review artifacts.
3. Re-run with `apply_changes=true` only after validation.

## Artifact expectations

| Workflow | Key artifacts |
| --- | --- |
| `multi-repo-sync.yml` | `submodule-integrity-audit`, `submodule-sync-report` |
| `component-version-check.yml` | `component-version-report` |
| `build-all-modules.yml` | `dotnet-results-*`, `node-results-*` |
| `build-variant-test.yml` | `variant-<name>-<os>` |
| `code-registry-update.yml` | `code-registry` |
| `status-dashboard.yml` | `status-report`, `status-dashboard` |

## Fast troubleshooting

- **Version check fails**
  - Inspect `component-version-report` for missing/invalid semver values.
  - Confirm `.csproj` includes `<Version>` or `<PackageVersion>`.

- **Build-all-modules finds no targets**
  - Confirm `.sln` / `.slnx` files are present within repository-owned paths.
  - Check target discovery summary in the run output.

- **Variant tests skip unexpectedly**
  - Ensure test projects match `*Tests*.csproj` and are under `tests/`, `src/`, or `monado/`.

- **Submodule sync cannot push**
  - Confirm run was `workflow_dispatch` with `apply_changes=true`.
  - Confirm branch permissions allow push via `GITHUB_TOKEN`.

- **Registry publish does not commit**
  - Publish is gated to manual dispatch on `main` with `apply_changes=true`.
  - No commit is created when staged registry artifacts are unchanged.

## Local preflight checks

```bash
python scripts/validate_submodule_integrity.py \
  --gitmodules .gitmodules \
  --repositories config/integrations/repositories.json

python scripts/control/validate_workflows.py
```
