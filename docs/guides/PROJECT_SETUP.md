# HELIOS project setup and repository guide

This is the canonical contributor entry point for the HELIOS product repository.
It describes repository ownership, language boundaries, safe branch integration,
local validation, and approval-gated Azure onboarding.

## Repository ownership

| Repository | Owns |
| --- | --- |
| `M0nado/helios-platform` | Product architecture, Windows execution, platform services, deployment, and releases |
| `M0nado/Helios-Control-Center` | Operator GUI, status dashboards, and approved command surfaces |
| `Yolkster64/hermes-fleet-platforms` | Hermes/XCore workers and bounded fleet experiments |
| `Heli0s-Dynamics/adaptive-multibrain-bootstrap` | Shared policy, GitHub/Azure bootstrap, integration contracts, and evaluation |

The machine-readable authority map is
[`config/integrations/repositories.json`](../../config/integrations/repositories.json).
Cross-repository data must use the normalized
[`event-contract.schema.json`](../../config/integrations/event-contract.schema.json)
envelope. Do not copy a satellite repository into this repository as a second
source of truth; integrate it through a pinned package, contract, or reviewed
migration.

## Runtime and language boundaries

- **C# / .NET 8** is the primary orchestration and service layer. Windows-only
  code stays behind typed interfaces so contracts and service tests remain
  cross-platform.
- **WinUI 3 / XAML** is the supported Windows operator experience. The operator
  surface requests typed platform operations; it must not embed privileged
  shell commands.
- **C++** is reserved for measured native hot paths. Every native component
  needs a managed boundary, deterministic fallback, benchmark, and memory-safety
  review before becoming a required dependency.
- **F#** owns deterministic mathematics, prediction, ranking, and analytics.
  Keep I/O at the boundary and test numerical behavior, non-finite inputs, and
  reproducibility.
- **Python** connects AIHub, provider adapters, Hermes/XCore evaluation, and
  report tooling. Python does not become an alternate product control plane.

The principal source roots are `src/core`, `src/services`, `src/analytics`,
`src/native`, and `src/ai`. Tests belong under `tests` and should mirror the
source boundary they validate. Historical reports are evidence, not current
architecture.

## Workstation bootstrap

Use Windows 11 and PowerShell 7 for the full desktop product. Cross-platform
contract, service, F#, and Python validation can run in the dev container.

Required tools:

- Git and GitHub CLI (`gh`)
- .NET 8 SDK and the Windows App SDK workload for WinUI builds
- PowerShell 7
- Python 3.11 or later
- CMake and a supported MSVC toolchain for native Windows components
- Azure CLI and Bicep CLI for Azure validation
- Docker only for explicitly containerized services

Start with the repository doctor; it reports missing tools without deploying:

```bash
./helios.sh doctor
./helios.sh readiness
./helios.sh validate
```

Do not put tokens in `.env`, command arguments, generated reports, or tracked
configuration. Use GitHub workload identity federation and Azure Key Vault for
automation. Local interactive login is only for operator-led validation.

## Safe branch consolidation

“Merge every branch” is not a safe integration strategy. Old branches can be
superseded, contain leaked secrets, undo security fixes, or represent abandoned
experiments. Inventory and compare them before selecting changes:

```bash
python3 scripts/analysis/branch_intelligence.py \
  --manifest docs/integration/remote-manifest.json \
  --out reports/branch-intelligence
```

Remote configuration and network fetches are deliberately opt-in:

```bash
python3 scripts/analysis/branch_intelligence.py --configure-remotes --fetch
```

For each candidate, compare its merge base, unique commits, affected ownership
areas, CI evidence, and security impact. Port unique changes onto a new branch
from `main`; do not merge stale branches wholesale. One scoped issue and pull
request must carry the source branch, commit SHA, correlation ID, tests, and
rollback notes. Do not delete or rewrite a branch until its disposition is
reviewed.

The same process applies to Helios Control and Hermes fleet branches in their
owning repositories. Only event-contract and package integration changes belong
here.

## Build and validation lanes

Run the lane affected by a change, followed by contract validation. The build
graph is the repository-wide discovery entry point:

```bash
./helios.sh build
dotnet build HELIOS.Platform.slnx --configuration Release
dotnet test HELIOS.Platform.slnx --configuration Release --no-restore
python3 -m compileall -q scripts src tests
./helios.sh validate
```

Windows/WinUI and native builds must also run on a Windows worker with the
documented SDK and MSVC workload. A Linux-only result does not validate desktop
or native components. Performance changes require a before/after benchmark;
prediction changes require deterministic numerical tests; integration changes
must validate the normalized event envelope and preserve correlation IDs and
evidence links.

## Azure CLI and governed onboarding

Installing Azure CLI is not authorization to provision resources. First verify
the local toolchain:

```bash
az version
az bicep version
az account show --output table
```

If an operator explicitly authorizes an interactive session, authenticate with
the least-privileged tenant and subscription and verify the selected context:

```bash
az login
az account set --subscription '<subscription-id>'
az account show --output table
```

For CI/CD, configure workload identity federation rather than a client secret.
Store the non-secret client, tenant, and subscription identifiers as protected
environment variables, keep secrets in Key Vault, and grant the smallest role
at the narrowest resource scope.

All infrastructure changes follow this sequence:

1. Build and lint Bicep.
2. Validate against the target resource group.
3. Capture a full Resource Manager what-if artifact.
4. Review and approve that immutable evidence in a protected environment.
5. Re-run what-if and reject drift.
6. Apply only with explicit deployment confirmation and a documented rollback.

Use `.github/workflows/helios-cloud-deploy.yml` for the governed path. Do not
run ad-hoc `az deployment ... create` commands or bypass environment reviewers.
Tenant, Entra/RBAC, Key Vault, production, firewall, and device-security changes
always require explicit approval.

## Completion checklist

- Change is linked to a scoped issue and implemented on a feature branch.
- Ownership boundaries and the normalized event contract are preserved.
- C#, WinUI, C++, F#, and Python lanes affected by the change pass.
- Security, performance, and rollback effects are documented.
- Azure what-if evidence is reviewed when infrastructure changes.
- No credentials, logs, caches, databases, model weights, VHDX files, or build
  outputs are tracked.
- The pull request records correlation IDs and evidence links.
