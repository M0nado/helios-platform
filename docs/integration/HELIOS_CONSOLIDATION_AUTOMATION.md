# HELIOS consolidation automation

This guide documents the safest way to combine the HELIOS platform branches and source repositories without losing ownership boundaries across C#/WinUI 3, C/C++, F#, Python, security, and Azure/GitHub automation work.

## Goals

- Make every source repository explicit before merging.
- Prioritize `helios-control` for C#/WinUI 3 front-end and control-plane conflicts.
- Prioritize `hermes-fleet-production` for F# fleet analytics, prediction math, and parallel workloads.
- Preserve C/C++ performance and native-security ownership in `helios-monado-blade`.
- Preserve Python AI Hub ownership in `helios-ai-hub` and experimentation boundaries in `helios-dev-ai-hub`.
- Preserve Azure CLI, GitHub Actions, and cloud deployment automation ownership in `helios-build-agents`.
- Make Azure CLI setup repeatable instead of depending on manual workstation state.

## Files added for automation

| File | Purpose |
| --- | --- |
| `scripts/automation/consolidation-sources.json` | Machine-readable source manifest derived from `MERGE_SOURCE_MANIFEST.yaml`. |
| `scripts/automation/helios_consolidation.py` | Plan-first consolidation runner for remotes, fetches, subtree/submodule commands, branch planning, language scans, and Azure CLI checks. |
| `docs/integration/HELIOS_CONSOLIDATION_EXECUTION_PLAN.md` | Generated execution plan produced by the runner. |

## Recommended workflow

Run the automation in dry-run mode first:

```bash
python3 scripts/automation/helios_consolidation.py --allow-untracked --include-current-branches --scan --setup-azure-cli
```

When you intentionally want to refresh the generated execution plan, pass `--write-plan` during a dry run (or provide a custom output path):

```bash
python3 scripts/automation/helios_consolidation.py --allow-untracked --write-plan
python3 scripts/automation/helios_consolidation.py --allow-untracked --write-plan /tmp/helios-plan.md
```

After reviewing the commands and confirming credentials/network access, execute only the source you want to integrate:

```bash
python3 scripts/automation/helios_consolidation.py --source helios-control --apply --allow-untracked
python3 scripts/automation/helios_consolidation.py --source hermes-fleet-production --apply --allow-untracked
```

Then continue through the remaining sources in manifest order. The script intentionally keeps current-repository branch merges plan-only, because local and remote branch conflict ownership must be reviewed before running `git merge`.

## Azure CLI setup

Validate Azure CLI state without changing the workstation:

```bash
python3 scripts/automation/helios_consolidation.py --setup-azure-cli
```

Install Azure CLI only after reviewing the installer command for the current operating system:

```bash
python3 scripts/automation/helios_consolidation.py --setup-azure-cli --install-azure-cli --apply
```

After Azure CLI is installed, authenticate with the correct tenant/subscription for the environment:

```bash
az login --tenant <tenant-id>
az account set --subscription <subscription-id>
az account show --output table
```

## Conflict ownership rules

| Area | Authoritative source |
| --- | --- |
| C#/WinUI 3 UI, XAML, MVVM, service wiring, shell/control-plane UX | `helios-control` |
| F# analytics, prediction models, fleet math, parallel analytics semantics | `hermes-fleet-production` |
| C/C++ performance backend, native interop, native security, Hermes XCore specialist hooks | `helios-monado-blade` |
| Security hardening and compliance posture | `helios-security-setup` |
| Python AI Hub integration, model orchestration, agent connectors | `helios-ai-hub` |
| Azure CLI, GitHub Actions, CI/CD, provisioning, release automation | `helios-build-agents` |

## Safety notes

- Commit or stash tracked changes before running with `--apply`.
- Use `--source` to integrate one repository at a time.
- Resolve each subtree/submodule result before starting the next source.
- Keep generated secrets, Azure credentials, and local `.env` files out of Git.
- Run domain tests after each integrated source: C#/WinUI build tests, C++ native tests, F# analytics tests, Python AI Hub tests, and Azure/GitHub automation dry-runs.
