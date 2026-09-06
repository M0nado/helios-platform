# HELIOS Enterprise Automation Fabric 2.0

A governed event-and-evidence control plane joining **GitHub, Azure, Slack, Linear, SharePoint, and Microsoft Teams** without turning any single chat button into an invisible production backdoor.

## The operating model

```text
GitHub events / Slack actions / Linear webhooks
                 / Teams approvals
                         |
                         v
              HELIOS webhook ingress
        verify signature -> normalize -> enqueue
                         |
                         v
                 Azure Service Bus
                         |
                         v
              HELIOS delivery worker
         route -> redact -> dedupe -> publish
        /            |             |            \
     Slack         Linear        Teams       SharePoint
                                   |              |
                              human notices   evidence archive
```

GitHub remains the executable source of truth. Azure holds runtime identities, queues, state, and secrets. SharePoint is the immutable governance/evidence projection. Slack and Teams provide human interaction. Linear tracks work. Production deployment requires a protected GitHub environment, an unchanged plan hash, and an approved deployment workflow.

## Buildable recovery

This directory is the reconstructed, self-contained project. The original flat
recovery remains one directory above it for provenance. All collaboration
connectors remain disabled, Azure workload deployment defaults to false, and
the overlay installer refuses to overwrite repository conflicts.

## Start here

```powershell
py -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install -e ".[test]"

fabricctl validate --root .
fabricctl simulate --root . --fixture tests/fixtures/deployment-plan.json
fabricctl evidence --root . --fixture tests/fixtures/deployment-plan.json --output artifacts/generated
dotnet restore src/dotnet/HELIOS.Fabric.sln
dotnet build src/dotnet/HELIOS.Fabric.sln -c Release --warnaserror
dotnet test src/dotnet/HELIOS.Fabric.sln -c Release --no-build
docker build -f docker/broker.Dockerfile -t helios-fabric-broker:local .
docker build -f docker/worker.Dockerfile -t helios-fabric-worker:local .
```

On Linux/macOS:

```bash
python3 -m venv .venv
. .venv/bin/activate
python -m pip install -e '.[test]'
fabricctl validate --root .
pytest
```

## Repository overlay

Copy this package into a review branch of `M0nado/helios-platform`. The pack intentionally contains no tenant IDs, webhook URLs, tokens, private keys, or API keys.

```powershell
pwsh -NoProfile -File .\scripts\bootstrap\Install-HeliosFabricOverlay.ps1 `
  -PackRoot (Resolve-Path .) `
  -RepositoryRoot C:\src\helios-platform
```

The first run is preview-only. Add `-Apply -Confirm 'APPLY HELIOS AUTOMATION FABRIC'`
after reviewing the generated conflict report. Apply refuses every differing
destination file; those conflicts must be reconciled in review instead of
being overwritten.

## Deployment phases

1. **Validate**: schemas, YAML, JSON, signatures, routes, Bicep syntax in CI, C# build, PowerShell AST parsing.
2. **Foundation**: Azure resource group, identities, Key Vault, Service Bus, Storage, App Configuration, observability, ACR, Container Apps environment.
3. **Credentials**: tenant administrators add connector secrets to Key Vault and grant scoped Graph/SharePoint permissions.
4. **Broker**: build and deploy the C# ingress and worker images.
5. **Connector activation**: enable routes one connector at a time; health-check and rollback each activation.
6. **Production**: protected environment approval, exact what-if hash, blocker checks, then deployment.

## What is deliberately not automatic

- tenant-wide Graph consent
- SharePoint site permission grants
- Slack app installation and admin approval
- Linear OAuth authorization or webhook creation
- production GitHub environment approval
- secret creation, printing, or readback
- destructive Azure cleanup

Those are explicit administrative boundaries, not missing code.
