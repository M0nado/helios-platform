# Helios Connect

Helios Connect is the cloud-first integration control plane for GitHub, Linear,
Slack, Microsoft Teams, SharePoint, Microsoft Copilot/Foundry, Azure, and MCP.
Local processes are development and administration clients only; the governed
runtime is hosted in Azure.

It uses one normalized event envelope, an allowlisted router, idempotency keys,
and per-connector workers. Secrets never live in source control: local secrets
come from environment variables or .NET user-secrets; Azure secrets come from
Key Vault through managed identity.

## Development quick start

1. Copy `.env.example` to `.env` and fill only the connectors you need.
2. Install .NET 8 SDK.
3. Run `dotnet restore && dotnet test`.
4. Run `dotnet run --project src/Helios.Connect.Api`.
5. Point webhook providers to `https://<host>/webhooks/{provider}`.

The default profile is `dry-run`: incoming events are validated and logged but
no external writes occur. Set `HELIOS_EXECUTION_MODE=live` only after Key Vault,
signing secrets, and destination allowlists are configured.

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
confirmation, publishes the image with an ACR cloud build, repeats ARM what-if,
and keeps deployment as a separately confirmed phase.

The Bash compatibility script is preview-only. Its legacy mutation path is
retired so it cannot bypass GitHub environment verification or newer immutable
OIDC subject formats.

After deployment, `scripts/Test-HeliosCloudConnection.ps1` verifies HTTPS,
health, fail-closed Entra protection, connector context, MCP initialization, and
the exact read-only Azure tool inventory. Its optional token remains in memory
and is never printed.

## Online-only production target

`config/cloud-runtime.json` is the production contract: Azure hosts the API,
remote MCP, durable queues, agent jobs, state, evidence, and telemetry. Local
VS Code, Claude Code, and CLI processes are administration clients only and are
never a production runtime dependency.

The hosted rollout surface is the
[Helios Cloud Control Center](https://helios-cloud-control.thepatman64.chatgpt.site),
with execution tracked in
[Linear JOH-36](https://linear.app/641974/issue/JOH-36/deploy-helios-cloud-only-control-plane-and-online-visualization).

`.github/workflows/helios-cloud-deploy.yml` provides the protected OIDC path. It
accepts only immutable image digests from the configured dedicated registry,
publishes hashed ARM what-if evidence, and requires a separate protected deploy
approval bound to that evidence before applying. The workflow does not create
role assignments, grant consent, or populate Key Vault.

## Claude, VS Code Insiders, and local fleets

`docs/MULTI_AGENT_WORKBENCH.md` configures Claude Code, GitHub Copilot, Azure
MCP, the Helios local/cloud MCP endpoints, concurrent CLI checks, and four
governed local agents. Azure MCP is pinned and read-only. Provider credentials
remain isolated, and the fleet cannot deploy or merge automatically.
