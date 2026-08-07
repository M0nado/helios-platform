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
2. Local Docker: `docker image inspect <immutableDigest>`, then `docker run --rm --read-only --detach --name helios-connect-local --publish 127.0.0.1:8080:8080 <immutableDigest>`, then `docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}no-healthcheck{{end}}' helios-connect-local`
3. Hybrid fleet: `pwsh -File monado/helios-control/scripts/Start-HeliosLocalFleet.ps1 -Mode Plan`, then `pwsh -Command "$p = Start-Process dotnet -ArgumentList @('run','--project','monado/helios-control/src/Helios.Connect.Api/Helios.Connect.Api.csproj','--configuration','Release','--no-build','--','--urls','http://127.0.0.1:8080') -PassThru; Start-Sleep -Seconds 15; if ($p.HasExited) { throw 'Helios Connect API exited before hybrid health checks.' }"`, then `pwsh -File monado/helios-control/scripts/Start-HeliosLocalFleet.ps1 -Mode Status`, and `dotnet test monado/helios-control/Helios.Connect.sln --configuration Release --filter "FullyQualifiedName~XCore9GovernanceTests"`

These are validation-only checks and do not grant deploy/apply authority.
