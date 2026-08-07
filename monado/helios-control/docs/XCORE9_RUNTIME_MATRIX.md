# XCore9 runtime matrix (Windows, Docker, hybrid)

The governed runtime matrix is versioned in
`config/xcore9-runtime-matrix.v1.json`.

## Modes

| Mode | Identity boundary | Artifact pinning | Failure domain | Default |
| --- | --- | --- | --- | --- |
| `local-windows` | interactive user or managed identity; no cross-tenant reuse | binary hash required | single workstation | yes |
| `local-docker` | local workload boundary; no cross-tenant reuse | binary hash + image digest required | single host container runtime |  |
| `hybrid-fleet` | per-node managed/workload identity; no cross-tenant reuse | binary hash + image digest required | per node + per replica |  |

Each mode carries explicit boundaries for identity, network, storage, tool
access, secrets, startup/health checks, rollback, and deny-list enforcement.

## Deterministic startup and health contract

Each profile defines deterministic startup commands and an explicit health probe.
Runtime startup remains validation-first and non-destructive by default.

## Deny-list enforcement

Every mode has an explicit deny-list and includes these mandatory prohibitions:

- production mutation without protected approval;
- cross-tenant secret/token reuse;
- unbounded self-expanding execution.

## Smoke evidence links

Use the following contract smoke commands and attach output in PR evidence:

1. Local Windows: `dotnet test monado/helios-control/Helios.Connect.sln --configuration Release`
2. Local Docker: verify immutable digest + run read-only health checks from the runtime matrix contract
3. Hybrid fleet: run profile signature/hash verification + read-only node health inventory

These are validation-only checks and do not grant deploy/apply authority.
