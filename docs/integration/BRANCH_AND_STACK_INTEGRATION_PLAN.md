# HELIOS/Hermes Branch and Stack Integration Runbook

This runbook turns the "combine all HELIOS/Hermes branches" request into a repeatable workflow that can be executed whenever the branch refs or sibling repositories are present. The current execution environment exposes only the local `work` branch and no configured Git remote, so the repository cannot safely auto-merge unavailable `helios-control` or `hermes-fleet-production` refs. Instead, use the inventory/merge automation below to discover, order, merge, and validate those branches as soon as they are available.

## What the automation covers

- **Branch discovery**: local and remote refs containing `helios-control`, `hermes-fleet-production`, `helios`, or `hermes`.
- **Sibling repository discovery**: nearby repositories named like `helios-control` or `hermes-fleet-production`.
- **Stack lane inventory**:
  - C# / WinUI 3 frontend and platform services.
  - C++ performance/security backend and native integrations.
  - F# math, prediction, analytics, and parallel optimization projects.
  - Python AI Hub, Hermes XCore, and automation glue.
  - Azure/Bicep/PowerShell/shell deployment automation.
- **Ordered merge planning**: HELIOS control-plane refs first, Hermes production fleet refs second, then remaining HELIOS/Hermes branches.

## Inventory every branch and stack lane

Run a dry-run inventory before merging:

```bash
./scripts/integration/helios_branch_integrator.py \
  --repo . \
  --remote-url https://github.com/M0nado/helios-platform.git \
  --fetch \
  --search-root /Microsoft.NET \
  --report build/integration/helios-hermes-inventory.md \
  --json build/integration/helios-hermes-inventory.json
```

Review the generated report for branch refs, sibling repositories, language-lane coverage, and planned merge commands.

## Execute the ordered merge when refs exist

```bash
git switch -c integration/helios-hermes-stack
./scripts/integration/helios_branch_integrator.py \
  --repo . \
  --remote-url https://github.com/M0nado/helios-platform.git \
  --fetch \
  --search-root /Microsoft.NET \
  --execute \
  --report build/integration/helios-hermes-merge.md
```

The script uses `git merge --no-ff --no-edit` and stops on conflicts so they can be resolved intentionally. Do not enable `--execute` unless the working tree is clean and you are on an integration branch.

## Stack-specific optimization responsibilities

| Lane | Primary focus | Validation gate |
|---|---|---|
| C# / WinUI 3 | Operator frontend, App Service host, shared platform contracts | `dotnet build` / UI smoke checks |
| C++ | Native performance backend, system integration, security-sensitive boundaries | compiler warnings, benchmarks, security review |
| F# | Predictive math, analytics, scheduling, parallel optimization kernels | deterministic tests and model/benchmark snapshots |
| Python | AI Hub, Hermes XCore setup, branch/repo inventory, automation glue | `python3 -m py_compile`, dry-run automation reports |
| Azure | Azure CLI, Bicep, App Service package, smoke tests | `az account show`, `az bicep build`, deployment dry run |

## Azure CLI setup

For Linux/macOS/WSL deployment hosts, run:

```bash
./scripts/setup/setup-azure-cli.sh --login --install-bicep --subscription "$HELIOS_AZURE_SUBSCRIPTION_ID"
# CI/non-interactive hosts can use service-principal authentication:
./scripts/setup/setup-azure-cli.sh --service-principal --tenant "$HELIOS_AZURE_TENANT_ID" --install-bicep --subscription "$HELIOS_AZURE_SUBSCRIPTION_ID"
```

The setup helper detects an existing `az`, installs Azure CLI when needed, authenticates with either device-code or service-principal credentials, sets the active subscription, and installs/upgrades Bicep for `infrastructure/main.bicep` deployments.

## Required validation after each merge batch

```bash
git diff --check
python3 -m py_compile scripts/integration/helios_branch_integrator.py
bash -n scripts/setup/setup-azure-cli.sh
./scripts/integration/helios_branch_integrator.py --repo . --fetch --search-root /Microsoft.NET --report build/integration/post-merge-inventory.md
```

Add stack-specific build/test commands (`dotnet test`, C++ build, F# analytics tests, Azure deployment validation) as soon as those projects or remote branches are present in the checkout.

## Current checkout inventory

A generated inventory for this checkout is committed at [HELIOS_HERMES_INVENTORY.md](HELIOS_HERMES_INVENTORY.md). It records the currently discoverable branch refs, sibling repositories, and language/deployment lane counts so reviewers can see exactly what was available during this integration pass.
