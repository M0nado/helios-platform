# Helios Connect

Helios Connect is the cloud-first integration control plane for GitHub, Linear,
Slack, Microsoft Teams, SharePoint, Microsoft Copilot/Foundry, Azure, and MCP.
Local processes are development and administration clients only; the governed
runtime is hosted in Azure.

The implemented slice validates signed webhooks, emits a normalized event
envelope, exposes read-only MCP inventory, and rejects duplicate deliveries with
a bounded process-local cache. Durable routing, per-connector workers, Service
Bus, Cosmos DB idempotency, and dead-letter replay remain target architecture.
Secrets never live in source control: local secrets come from environment
variables or .NET user-secrets; approved Azure secrets will come from Key Vault
through managed identity after their separate administrator gate.

## Development quick start

1. Copy `.env.example` to the project-root `.env.local` and fill only the
   connectors you need. `Start-HeliosLocal.ps1` never reads a parent-workspace
   environment file.
2. Install .NET 8 SDK.
3. Run `dotnet restore && dotnet test`.
4. Run `pwsh ./scripts/Start-HeliosLocal.ps1`.
5. Point webhook providers to `https://<host>/webhooks/{provider}`.

The default profile is `dry-run`: every incoming webhook still requires a valid
provider signature, while no external writes occur. Replay identifiers are kept
in a bounded, expiring in-process cache; they do not survive restart or coordinate
across replicas. Set `HELIOS_EXECUTION_MODE=live` only after durable idempotency,
Key Vault references, signing secrets, outbound workers, and destination
allowlists are implemented and reviewed. Providers without an implemented
verifier remain fail-closed.

See `docs/ARCHITECTURE.md`, `docs/CONNECTION_RUNBOOK.md`, and
`config/integrations.json`.

## Azure connector

The project includes an Azure-deployable, Entra-protected read-only connector
for discovering the configured subscription/resource group and Foundry-related
resources. It exposes REST inventory routes and an MCP endpoint, uses a
user-assigned managed identity with resource-group Reader, and contains no Azure
mutation tools.

Start with `docs/AZURE_INTERACTIVE_ONBOARDING.md`. The primary Cloud Shell entry
point is `scripts/Connect-HeliosAzureInteractive.ps1`; it selects the Azure
context interactively, configures secretless OIDC/RBAC only after exact
confirmation, and dispatches the protected online workflow. That workflow
builds the exact checked-out GitHub commit in ACR, resolves its immutable digest,
captures ARM what-if evidence, and requires a second approval before deployment.
The operator script has no local image-build or application-apply path.

The Bash compatibility script is preview-only. Its legacy mutation path is
retired so it cannot bypass GitHub environment verification or newer immutable
OIDC subject formats.

After deployment, `scripts/Test-HeliosCloudConnection.ps1` verifies HTTPS,
health, fail-closed Entra protection, connector context, MCP initialization, and
the exact read-only Azure tool inventory. Its optional token remains in memory
and is never printed.

The Microsoft 365 package stays staged until tenant approval. Run
`pwsh ./scripts/New-HeliosTeamsPackage.ps1` to materialize and verify the
versioned 192x192 color and 32x32 transparent outline icons before packaging.
Its SSO resource URI matches the Entra app registration created by the wizard;
the repository workflow validates the manifest, icon dimensions, and policy
routes without publishing to Teams or Copilot.

## Online-only production target

`config/cloud-runtime.json` distinguishes the implemented hosted connector slice
from the target production architecture. The current Bicep hosts the API, remote
MCP, Key Vault shell, and telemetry in Azure. Durable queues, agent jobs, state,
evidence stores, APIM/private ingress, and Foundry agents are explicitly marked
unimplemented. Local VS Code, Claude Code, and CLI processes are administration
clients only and are never a production runtime dependency.

Set `HELIOS_CLOUD_RUNTIME_ONLY=true` in Azure. The local-development MCP route
is mapped only when `HELIOS_LOCAL_RUNTIME_ALLOWED=true` and cloud-only mode is
not enabled, so conflicting settings fail closed. Cloud MCP clients must provide
an explicit `HELIOS_AZURE_CONNECTOR_URL`; there is no localhost fallback. The
optional `helios-local-dev` client uses `HELIOS_LOCAL_MCP_URL` explicitly.

The hosted rollout surface is the
[Helios Cloud Control Center](https://helios-cloud-control.thepatman64.chatgpt.site),
with execution tracked in
[Linear JOH-36](https://linear.app/641974/issue/JOH-36/deploy-helios-cloud-only-control-plane-and-online-visualization).

`.github/workflows/helios-cloud-deploy.yml` provides the protected OIDC path. It
verifies a clean checkout at `GITHUB_SHA`, builds that source in the configured
dedicated registry, resolves the immutable digest, publishes hashed ARM what-if
evidence, and requires a separate protected deploy approval bound to that
evidence before applying. The workflow does not create role assignments, grant
consent, or populate Key Vault.

## Claude, VS Code Insiders, and local fleets

`docs/MULTI_AGENT_WORKBENCH.md` configures Claude Code, GitHub Copilot, Azure
MCP, the Helios local/cloud MCP endpoints, concurrent CLI checks, and four
governed local agents. Azure MCP is pinned and read-only. Provider credentials
remain isolated, and the fleet cannot deploy or merge automatically. VS Code's
cloud connector prompt has no local default; its separately named
`helios-local-dev` server is an explicit development-only choice.
