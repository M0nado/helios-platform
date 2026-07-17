# HELIOS Repository and Module Map

This document defines the canonical ownership boundaries after the governed polyglot foundation merged through PR #174.

## Canonical repository roles

| Repository | Role | Accepts |
|---|---|---|
| `M0nado/helios-platform` | Canonical product/runtime monorepo | Product code, local services, policy contracts, required CI, guarded Windows adapters, Azure infrastructure plans |
| `M0nado/Helios-Control-Center` | Operator and promotion control surface | Operator UX, promotion evidence, release orchestration, status visualization |
| `Yolkster64/monado-blade` | Integration laboratory | Imported packs, experimental modules, compatibility tests, dry runs |
| `Yolkster64/hermes-fleet-platforms` | Hermes/XCore runtime laboratory | Routing, training, worker adapters, fleet evaluation |
| `Heli0s-Dynamics/adaptive-multibrain-bootstrap` | Cross-repository governance | Shared contracts, bootstrap policy, organization automation |
| `Heli0s-Dynamics/helios-platform` | Reviewed enterprise mirror | Release candidates promoted from immutable canonical commits |

## Canonical monorepo modules

```text
apps/
  control-center/       Interactive operator surfaces
  helios-control/       Shared ChatGPT/Copilot MCP application

src/
  core/                 Stable contracts and domain types
  services/             Long-running .NET services and brokers
  agents/               Typed governed agent registries and loaders
  tools/                Diagnostics, migration and repository utilities

python/
  aihub/                AIHub orchestration, routing and evaluation
  integrations/         Provider adapters with bounded contracts

native/
  security/             C++ hot-path security and runtime checks
  optimization/         Native performance helpers

analytics/
  fsharp/               Scoring, recommendation and telemetry analysis

scripts/
  windows/              Guarded Windows adapters; report-first by default
  validation/           Repository-wide deterministic validators
  github/               PR, evidence and promotion helpers
  azure/                Plan/what-if helpers; no implicit deployment

infra/
  bicep/                Canonical Azure modules and environment composition

config/
  agents/               Agent definitions
  plugins/              Connection/tool registries
  security/             Security policy manifests
  profiles/             Workstation/profile contracts
  schemas/              Machine-enforced JSON schemas

tests/
  unit/                 Language-specific unit tests
  integration/          Cross-module contract tests
  policy/               Negative and fail-closed tests

legacy/
  diagnostics/          Preserved evidence
  inert/                Disabled source snapshots; never runtime-imported
```

## Dependency direction

```text
apps
  -> services / agents
      -> core contracts

python, native, analytics
  -> core-compatible schemas and generated contracts

scripts
  -> public module CLIs and manifests
  -X-> private implementation internals

infra
  -> published runtime contracts and image identities
  -X-> local developer secrets
```

Circular dependencies between applications, services and provider adapters are prohibited. Cross-language boundaries must use versioned JSON schemas, HTTP contracts, generated bindings or stable process interfaces.

## Submodule policy

Git submodules are allowed only for independently released components that satisfy all of the following:

1. Separate release cadence and ownership.
2. Immutable commit pinning.
3. Reproducible offline or authenticated restore.
4. Independent CI and security scanning.
5. No required secret material in the submodule URL.
6. A fallback build that fails clearly when the submodule is absent.

Hermes/XCore and enterprise governance should normally remain separate repositories connected through versioned contracts, not embedded as mutable submodules. Imported third-party source should prefer package managers or vendored checksum-locked archives over submodules.

## Current consolidation sequence

1. Canonical foundation: PR #174 merged.
2. Port the guarded Windows environment repair from PR #163 onto current `main`.
3. Port the Windows boot-security lane from PR #166 onto current `main`.
4. Rebase and reconcile Azure runtime PR #176.
5. Reconcile the shared ChatGPT/Copilot application PR #177 after the Azure runtime.
6. Review agent/control-plane PR #167 against the merged contracts and remove duplicate definitions.
7. Close or archive superseded stacked PRs only after replacement commits are verified.

## Merge rules

- No direct-to-main automation.
- Every merge is pinned to the reviewed head SHA.
- Required CI must be green.
- Generated evidence identifies source SHA, workflow run, dependency locks and artifact digests.
- Azure, tenant, RBAC, Graph, BitLocker, disk and production mutations remain separate approvals.
