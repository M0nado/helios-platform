# XCore9 runtime matrix v1

This document defines the governed XCore9 runtime matrix for `monado/helios-control` and maps directly to `config/xcore9-runtime-matrix.v1.json`.

## Governance baseline

- Default mode is `local-windows`.
- Default execution mode is `validation-first` (`dry-run` policy surface).
- Production mutation is blocked unless a protected GitHub environment approval is present.
- Cross-tenant secret reuse and cross-mode token reuse are both forbidden.

## Runtime mode boundaries

| Mode | Identity boundary | Network boundary | Storage boundary | Tool access boundary | Secret boundary |
| --- | --- | --- | --- | --- | --- |
| `local-windows` | Interactive operator session, single-tenant-per-run, no cross-tenant token reuse | Loopback-only ingress, allowlisted egress, no public ingress | Local runtime/evidence directories, destructive disk operations denied | Dotnet/Pwsh/GH/Azure read and plan tools, local MCP read-only | Scope `xcore9-local-windows`, no plaintext repo secrets, no cross-mode reuse |
| `local-docker` | Container runtime identity gated by host operator, single-tenant-per-container-run | Loopback port mapping via Docker bridge, no public ingress | Ephemeral container runtime state plus governed evidence directory | Docker + planning tools only, local MCP read-only | Scope `xcore9-local-docker`, no plaintext repo secrets, no cross-mode reuse |
| `hybrid-windows-docker-fleet` | Split Windows + container principals, tenant boundary per runtime mode | Dual loopback endpoints (Windows + container), allowlisted egress, no public ingress | Shared governed runtime/evidence directories with mode isolation | Windows + Docker + governance tools, local MCP read-only | Scope `xcore9-hybrid-fleet`, explicit no cross-mode reuse |

## Deterministic startup and health contracts

| Mode | Startup command | Health contract |
| --- | --- | --- |
| `local-windows` | `pwsh ./monado/helios-control/scripts/Start-HeliosLocal.ps1` | `GET http://127.0.0.1:5080/health/ready` must return `200` |
| `local-docker` | Build and run `helios-connect:xcore9-local` with explicit `dry-run` env | `GET http://127.0.0.1:5081/health/ready` must return `200` |
| `hybrid-windows-docker-fleet` | `pwsh ./monado/helios-control/scripts/Invoke-XCore9RuntimeMatrixSmoke.ps1 -Mode hybrid-windows-docker-fleet` | Both `:5080/health/ready` and `:5081/health/ready` must return `200` |

## Immutable pinning and bounded runtime envelopes

| Mode | Pinning contract | Resource/concurrency ceiling |
| --- | --- | --- |
| `local-windows` | SHA-256 binary hash for `Helios.Connect.Api.dll` | 8 CPU, 16 GB RAM, 2 deep-learning jobs, 4 agent runs |
| `local-docker` | SHA-256 digest contract for `helios-connect:xcore9-local` image | 6 CPU, 12 GB RAM, 2 deep-learning jobs, 3 agent runs |
| `hybrid-windows-docker-fleet` | Combined binary-hash + image-digest contract | 12 CPU, 24 GB RAM, 3 deep-learning jobs, 6 agent runs |

## Rollback and failure-domain isolation

| Mode | Rollback strategy | Failure-domain isolation |
| --- | --- | --- |
| `local-windows` | Stop runtime and restart last known good binary hash | Local workstation process/session boundary |
| `local-docker` | Stop container and restart last approved image digest | Single container + volume scope |
| `hybrid-windows-docker-fleet` | Isolate/revert only the failing runtime component | Split Windows process domain and container domain |

## Enforced deny-list

All modes enforce the required baseline deny-list:

- `automatic-production-deploy`
- `automatic-rbac-change`
- `automatic-consent-grant`
- `automatic-merge`
- `cross-tenant-secret-reuse`
- `cross-mode-token-reuse`
- `unbounded-recursive-agents`
- `plaintext-secret-export`
- `bypass-protected-approval`

Mode-specific deny operations are additionally enforced in the manifest and CI validator.

## Smoke evidence links

- Evidence index: `monado/helios-control/docs/XCORE9_RUNTIME_SMOKE_EVIDENCE.md`
- Local Windows evidence:
  - `monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-windows-smoke.md`
  - `monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-windows-smoke.json`
- Local Docker evidence:
  - `monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-docker-smoke.md`
  - `monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-docker-smoke.json`
- Hybrid evidence:
  - `monado/helios-control/docs/evidence/xcore9-runtime-matrix/hybrid-windows-docker-fleet-smoke.md`
  - `monado/helios-control/docs/evidence/xcore9-runtime-matrix/hybrid-windows-docker-fleet-smoke.json`
