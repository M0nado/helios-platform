# HELIOS unified plugin setup

The HELIOS Control Fabric plugin is the operator entry point for Codex, Azure,
Azure DevOps, GitHub, Linear, Slack, Foundry, and the future Entra-protected
HELIOS MCP runtime. The same MCP endpoint is packaged as a Microsoft 365
Copilot declarative-agent action.

## Current truth

- Control-center website:
  https://helios-control-center.thepatman64.chatgpt.site
- Canonical repository: `M0nado/helios-platform`
- Active integration line: PR #188
- Azure runtime: not live
- Connector delivery: dry-run
- Hermes learning: candidate-only
- XCore: standby

## Install locally

Open the repository marketplace at `.agents/plugins/marketplace.json` and
install `helios-control-fabric`.

Run:

```bash
python plugins/helios-control-fabric/scripts/helios.py doctor
python plugins/helios-control-fabric/scripts/helios.py plan --environment azure-dev
```

## Connect Azure

1. Select the Azure tenant, subscription, resource group, and Foundry project.
2. Run `scripts/Connect-HeliosAzureInteractive.ps1`.
3. Create the protected GitHub `azure-dev` environment.
4. Use OIDC subject
   `repo:M0nado/helios-platform:environment:azure-dev`.
5. Publish an immutable image.
6. Review the exact Bicep and parameter hashes and approve `what-if`.
7. Approve deployment separately.

## Connect ChatGPT and Codex

After the connector is deployed and `/health/ready` succeeds:

1. Set `HELIOS_AZURE_CONNECTOR_URL` to the approved HTTPS origin.
2. Install or refresh the Codex repository plugin.
3. Add the same `<origin>/mcp` endpoint as an internal ChatGPT app.
4. Complete Entra authorization.

## Connect Microsoft 365 Copilot and Teams

The app package contains:

- `manifest.json`
- `declarativeAgent.json`
- `ai-plugin.json`

Use Microsoft 365 Agents Toolkit 6.12 or later to provision an Entra SSO or
OAuth Plugin Vault registration, replace the toolkit variables, validate the
package, sideload into a development environment, and publish only after tenant
administrator review.

## Connect Foundry

Create a governed Foundry agent and add the same Entra-protected MCP endpoint as
a tool. Pin the allowed HELIOS tools, evaluate them in a non-production project,
and publish a versioned agent only after evidence review.

## Connect Azure DevOps

The plugin binds the hosted remote MCP endpoint at
`https://mcp.dev.azure.com/<organization>` with read-only mode and the
`repos,wit,pipelines,wiki` toolsets. The Microsoft service is public preview and
currently requires a supported Entra-connected client.

## Approval boundary

No repository file or plugin command grants tenant consent, assigns RBAC,
deploys Azure, enables live connector relays, merges a pull request, or publishes
an agent automatically.
