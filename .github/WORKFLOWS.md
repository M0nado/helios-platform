# GitHub Actions Workflows (Build Scope)

This document describes the build-focused workflows hardened for issue #228:

1. `multi-repo-sync.yml`
2. `component-version-check.yml`
3. `build-all-modules.yml`
4. `build-variant-test.yml`
5. `code-registry-update.yml`
6. `status-dashboard.yml`

## Workflow map

| Workflow | Primary purpose | Automatic trigger | Manual controls |
| --- | --- | --- | --- |
| `multi-repo-sync.yml` | Validate and optionally sync submodule pointers | Push on `.gitmodules`/integration-map/workflow changes | `apply_changes=false` (default dry-run), `target_ref` |
| `component-version-check.yml` | Validate semantic version values in `.csproj`, `package.json`, and `version.json` | PR/push when version-bearing manifests change | `strict=true`, `verbose=false` |
| `build-all-modules.yml` | Build/test discovered solution targets and optional Node module targets | Push/PR (non-doc changes) | `clean_build=false`, `include_node_targets=true` |
| `build-variant-test.yml` | Execute variant test matrix across Ubuntu/Windows | Push/PR on source/test/workflow changes | `variants=all` or comma list |
| `code-registry-update.yml` | Build compressed code registry artifacts from real source roots | Push to `main` on source/workflow changes | `apply_changes=false`, `compression_level=6` |
| `status-dashboard.yml` | Build live workflow health dashboard from GitHub Actions API data | Hourly schedule + workflow file push | `workflow_dispatch` |

## Detailed behavior

### `multi-repo-sync.yml`

- Always runs **integrity audit** before any sync action.
- Uses `scripts/validate_submodule_integrity.py` to validate:
  - `.gitmodules` structure
  - repository URL mapping against `config/integrations/repositories.json`
- Sync mode is **manual** and defaults to dry-run (`apply_changes=false`).
- Commit/push only occurs when:
  - event is `workflow_dispatch`
  - `apply_changes=true`
- Produces audit and sync artifacts for traceability.

### `component-version-check.yml`

- Discovers version-bearing manifests from:
  - `**/*.csproj`
  - `**/package.json`
  - `**/version.json`
- Extracts versions from:
  - `<Version>`, `<PackageVersion>`, `<AssemblyVersion>` in `.csproj`
  - `version` field in JSON manifests
- Validates semantic version format and emits:
  - run summary table (optional verbose mode)
  - JSON report artifact (`component-version-report.json`)
- Strict mode controls pass/fail behavior.

### `build-all-modules.yml`

- Discovers `.sln` and `.slnx` targets from repository-owned paths.
- Builds .NET targets with:
  - .NET 8 SDK
  - NuGet cache (unless `clean_build=true`)
  - restore → build → test sequence per target
- Optionally builds Node targets (currently the managed plugin path) when present.
- Fails only when discovered targets fail; reports when no targets are found.

### `build-variant-test.yml`

- Defines four variants:
  - `development` (Debug)
  - `staging` (Release)
  - `production` (Release)
  - `test` (Release + coverage collection)
- Runs each variant across:
  - `ubuntu-latest`
  - `windows-latest`
- Auto-discovers `*Tests*.csproj` under `tests/`, `src/`, and `monado/`.
- Produces per-variant artifacts and a consolidated comparison summary.

### `code-registry-update.yml`

- Builds registry from real source roots:
  - `src/`
  - `scripts/`
  - `monado/`
  - `plugins/`
- Produces:
  - `.registry/snippets.json`
  - `.registry/index.json`
  - compressed archive outputs
  - compression report JSON
- Publishing to git is explicitly gated:
  - manual dispatch only
  - `apply_changes=true`
  - `main` branch only

### `status-dashboard.yml`

- Collects live run data from GitHub Actions REST APIs for monitored workflows.
- Computes sampled success rates from recent completed runs.
- Publishes:
  - `status-report.json`
  - `status/dashboard.md`
- Appends dashboard markdown directly to run summary.

## Safe defaults and guardrails

- Write operations default to **off** (`apply_changes=false`).
- Manual sync/publish actions are isolated from automatic push events.
- Validation-heavy steps fail closed for bad manifests/mappings.
- Artifacts are emitted for each major workflow for auditing.

## Local validation commands

```bash
# Validate submodule mapping contract
python scripts/validate_submodule_integrity.py \
  --gitmodules .gitmodules \
  --repositories config/integrations/repositories.json

# Validate workflow YAML structure (repo utility)
python scripts/control/validate_workflows.py
```

## Operational note

If a workflow needs privileged or mutating behavior, keep it behind explicit workflow-dispatch inputs and document its rollback path in the same pull request.
