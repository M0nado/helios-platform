# HELIOS Branch and Repository Integration Runbook

This runbook defines the safe process for combining HELIOS branches and sibling repositories such as `helios-control` and `hermes-fleet-production` when they are present in the workspace.

## Current workspace discovery

In this checkout, only the `helios-platform` Git repository is present and the only local branch is `work`. No Git remotes are configured, so there are no remote branches available to merge in this environment.

## Full read/write session bootstrap

Use the Azure CLI bootstrap script to prepare an operator shell for full read/write HELIOS work:

```powershell
pwsh ./scripts/devops/setup-full-access-azure-cli.ps1 -UseDeviceCode -PersistToUserProfile
```

On Linux/macOS or in environments without PowerShell, use the Bash equivalent:

```bash
./scripts/devops/setup-full-access-azure-cli.sh --use-device-code --persist-env
```

The script sets these session variables:

- `HELIOS_SESSION_ACCESS_MODE=full-read-write`
- `HELIOS_REPO_WRITE_ENABLED=true`
- `HELIOS_BRANCH_INTEGRATION_MODE=merge-with-review`
- `HELIOS_AZURE_SUBSCRIPTION_ID` and `HELIOS_AZURE_TENANT_ID` from the selected Azure CLI account

## Safe merge sequence

Do not blindly merge every branch at once. Use this sequence for every repository:

1. Inventory repositories and branches: `python3 ./scripts/devops/helios_repo_integrator.py --workspace-root .. --mode inventory --output json`.
2. Build a merge plan: `python3 ./scripts/devops/helios_repo_integrator.py --workspace-root .. --mode plan --output json`.
3. Fetch all remotes: `git fetch --all --prune --tags`.
4. Record the starting point: `git status --short` and `git branch --all --no-color`.
5. Create an integration branch from the intended base branch.
6. Merge one branch at a time with `--no-ff` so each integration point is auditable.
7. Resolve conflicts in the smallest related file set possible.
8. Run language-specific checks before continuing:
   - C# / WinUI 3: `dotnet build` and relevant test projects.
   - C++ performance backend: native build plus benchmarks where available.
   - F# analytics/math: `dotnet test` for F# projects.
   - Python AIHub integration: `python -m pytest` when tests are present.
9. Commit each successful branch integration before merging the next branch.
10. Push the integration branch and open a pull request for review.

## Priority review areas

When sibling repositories are available, inspect these areas first:

- `helios-control`: control-plane APIs, WinUI 3 surfaces, security policy enforcement, and Azure orchestration hooks.
- `hermes-fleet-production`: fleet rollout safety, telemetry, performance-sensitive C++ services, and production deployment automation.
- `helios-platform`: AIHub integrations, Azure CLI setup, session policy, security hardening, F# analytics, and parallel optimization paths.

## Conflict policy

Prefer source-of-truth ownership over timestamp-based choices:

- Security policy wins over convenience defaults.
- Production deployment safety wins over local demo behavior.
- Performance backend changes must include benchmark or profiling evidence.
- AIHub and cloud changes must avoid committing secrets or tenant-specific identifiers.
