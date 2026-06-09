# HELIOS Branch Consolidation, Automation, and Azure CLI Setup Guide

This guide turns `MERGE_SOURCE_MANIFEST.yaml` into an executable workflow for fixing,
automating, and merging HELIOS platform branches and sibling repositories. It prioritizes
`helios-control` and `hermes-fleet-production`, while still auditing every configured or
locally discovered branch.

## Recommended merge strategy

1. **Start from a clean integration branch.** Commit or stash unrelated work before merging.
2. **Audit everything before merging.** Run the automation in dry-run mode and review missing remotes,
   reachable refs, file-area classifications, and conflict-ownership hints.
3. **Fetch all source repositories in manifest order.** Add missing remotes from the manifest only when
   you are ready to make network calls.
4. **Merge in priority order.** `helios-control` owns C#/WinUI 3 front-end and control-plane decisions;
   `hermes-fleet-production` owns F# math, prediction, fleet analytics, and parallel-processing semantics.
5. **Keep native performance/security boundaries explicit.** C++ backend and Hermes XCore work should stay
   isolated behind documented interop contracts until benchmark and security validation pass.
6. **Automate Azure CLI readiness separately from source merges.** Validate `az` installation and login before
   cloud deployment scripts run.

## Automation commands

Audit the manifest, currently configured remotes, all local/remote branches, and language area impact:

```bash
python3 scripts/dev/helios_merge_automation.py audit --output reports/merge-audit.json
```

Add missing manifest remotes and fetch them in priority order:

```bash
python3 scripts/dev/helios_merge_automation.py fetch --add-missing-remotes
```

Preview guarded merge order without changing the repository:

```bash
python3 scripts/dev/helios_merge_automation.py merge
```

Execute guarded merges only after the audit is clean:

```bash
python3 scripts/dev/helios_merge_automation.py merge --execute --commit-each
```

Check Azure CLI installation/authentication and write a machine-readable setup report:

```bash
python3 scripts/dev/helios_merge_automation.py azure-check --output reports/azure-cli-status.json
```

For headless login after installing Azure CLI:

```bash
python3 scripts/dev/helios_merge_automation.py azure-check --login --use-device-code --tenant <tenant-id>
```

## Conflict ownership map

| Area | Primary owner | Typical files |
| --- | --- | --- |
| C#/WinUI 3 front end | `helios-control` | `.cs`, `.xaml`, `.csproj`, `.sln`, `.slnx` |
| C++ performance backend/security native paths | `helios-monado-blade` | `.cpp`, `.cxx`, `.cc`, `.hpp`, `.h`, `.vcxproj` |
| F# predictions/analytics/parallel math | `hermes-fleet-production` | `.fs`, `.fsi`, `.fsx`, `.fsproj` |
| Python AI Hub integration | `helios-ai-hub` | `.py`, notebooks, `requirements.txt`, `pyproject.toml` |
| Azure/GitHub automation | `helios-build-agents` | `.ps1`, `.bicep`, `.tf`, workflow `.yml`/`.yaml` |

## Safety gates

- The merge command refuses to execute when the worktree is dirty.
- The default merge mode is dry-run; `--execute` is required to change the repository.
- `git rerere` is enabled during executed merges so repeated conflict resolutions can be reused.
- Missing remotes are reported during audit and are only added when `fetch --add-missing-remotes` is used.
