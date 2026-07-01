# Agent Integration Playbook

This playbook turns ranked integration issues into safe, repeatable tasks for AI agents. Agents must preserve the stable build lanes and use targeted imports instead of bulk-merging upstream repositories.

## Guardrails

- Keep `HELIOS.Platform.slnx` as the protected stable lane.
- Keep `HELIOS.Legacy.Quarantine.slnx` as the staging lane for legacy/imported code.
- Do not move code into the stable lane until it builds, has tests or a test plan, and fits the architecture boundary.
- Do not import generated histories, `bin/`, `obj/`, stale workflows, contradictory status documents, checked-in secrets, or deploy scripts that bypass `what-if`.
- Preserve upstream provenance in planning docs when importing targeted code.

## Task: helios-control WinUI shell

:::task-stub{title="Import helios-control WinUI shell behind HELIOS contracts"}
Inspect the `helios-control` upstream once credentials or a local clone are available.

Import only the WinUI shell, pages, view models, and UI services into:

- `src/frontend/HELIOS.Control.WinUI/`

Any backend, Hermes, Azure, Python, or native calls must be extracted behind interfaces in:

- `src/core/HELIOS.Platform.Contracts/`
- `src/core/HELIOS.Platform.Orchestration/`

Do not import old workflows, generated docs, `bin/`, `obj/`, secrets, or deployment scripts.

After import, run:

- `dotnet restore HELIOS.Platform.slnx`
- `dotnet build HELIOS.Platform.slnx --configuration Release --no-restore`

Update:

- `docs/planning/helios-control-inventory.md`
- `docs/architecture/COMPONENT_MATRIX.md`
- `docs/status/verification-log.md`
:::

## Task: Hermes targeted refresh

:::task-stub{title="Refresh Hermes/XCore assets without bulk-merging history"}
Run upstream inventory and compare the fetched Hermes branches by subsystem.

Import reviewed assets only into:

- `src/integrations/Hermes/`
- `src/python/hermes_xcore/`
- `src/native/HELIOS.Performance/x-tier/`
- `infra/hermes-fleet/`
- `docs/integration/hermes-xcore/`

Reject generated bulk artifacts, contradictory status docs, stale workflows, hard-coded secrets, and deployment scripts that bypass Azure `what-if`.

After import, run the relevant .NET, Python, native, docs, security, or Bicep validation commands and update the verification log.
:::

## Task: RL offline replay

:::task-stub{title="Promote Hermes/XCore RL into offline replay and safe routing"}
Extend the Python Hermes/XCore adapter so fleet logs become auditable state/action/reward events.

Work in:

- `src/python/hermes_xcore/src/hermes_xcore/reinforcement_learning.py`
- `src/python/hermes_xcore/src/hermes_xcore/feature_pipeline.py`
- `src/python/hermes_xcore/src/hermes_xcore/routing_policy.py`
- `tests/python/`
- `tests/integration/`

Safety requirements:

- no destructive online actions
- deterministic fallback policy
- bounded exploration
- auditable reward history
- explicit C# contract checkpoint before orchestration promotion
:::

## Task: restore quarantined subsystem

:::task-stub{title="Restore one quarantined C# subsystem"}
Pick exactly one quarantined subsystem and re-enable it in a focused branch.

Recommended order:

1. contracts and DTOs
2. configuration
3. security
4. update system
5. cloud/storage after SQLite dependencies are normalized
6. API
7. advanced ML
8. advanced optimization
9. observability
10. plugins
11. Phase 10
12. Windows-specific UI/device code in Windows-only projects

For the chosen subsystem:

- unquarantine a minimal file set
- fix missing types and ambiguous interfaces
- move Windows-only APIs to a Windows-specific project where needed
- add or restore tests
- build `HELIOS.Legacy.Quarantine.slnx`
- only then consider promotion to `HELIOS.Platform.slnx`
:::

## Task: Azure gated foundation

:::task-stub{title="Promote Azure scaffold to gated deployable foundation"}
Keep deployment disabled until IaC validates.

Add or verify:

- Key Vault
- managed identities
- GitHub OIDC/federated identity
- storage
- monitoring
- App Insights
- least-privilege role assignments
- no password fallbacks
- no hard-coded secrets

Validation gate:

- `az bicep build --file infra/main.bicep`
- `az deployment group what-if --resource-group <rg> --template-file infra/main.bicep`
:::
