# HELIOS unified plugin setup

HELIOS Control Fabric 0.6.0 is the operator entry point for Codex, ChatGPT,
Azure, Azure DevOps, GitHub, Linear, Slack, SharePoint, Teams, Foundry, and
Microsoft Copilot. One Entra-protected Streamable HTTP MCP endpoint exposes the
same governed tools and Monado MCP Apps UI to approved hosts.

## Current truth

- Control-center website:
  https://helios-control-center.thepatman64.chatgpt.site
- Canonical repository: 'M0nado/helios-platform'
- Runtime foundation: PR #188 merged at 'aebbd9bce5373509b91f311dcaf847948a7ae00d'
- Current plugin review line: PR #190
- Azure runtime: not live
- Validated direct runtime: Entra-protected Azure Container Apps
- Target edge: Azure Front Door Premium to Container Apps over Private Link
- Connector delivery: dry-run
- Azure DevOps: read-only, plan-first
- GitHub runners: hosted validation/release; self-hosted disabled
- Hermes learning: candidate-only
- XCore: standby

The website and MCP status tool are governed configuration snapshots. Neither is
a substitute for Azure deployment status and health evidence.

## Install locally

Open the repository marketplace at '.agents/plugins/marketplace.json' and
install 'helios-control-fabric'.

Run:

    python plugins/helios-control-fabric/scripts/helios.py doctor
    python plugins/helios-control-fabric/scripts/helios.py targets
    python plugins/helios-control-fabric/scripts/helios.py plan --environment x-tier-dev
    python plugins/helios-control-fabric/scripts/helios.py oidc --environment x-tier-dev
    python plugins/helios-control-fabric/scripts/helios.py edge --environment x-tier-dev
    python plugins/helios-control-fabric/scripts/helios.py devops-sync
    python plugins/helios-control-fabric/scripts/helios.py runners

These commands inspect files and local prerequisites only.

## Shared MCP app

After deployment, '<approved-origin>/mcp' provides:

- standard 'search' and 'fetch';
- 'helios_get_control_plane_status';
- 'helios_render_control_center';
- the 'ui://helios/control-center-v2.html' MCP Apps resource;
- Azure and Foundry inventory reads;
- deterministic plan and upgrade proposals;
- owner-scoped control-run reads;
- connector binding status.

The UI resource uses MIME type 'text/html;profile=mcp-app'. It communicates
through the MCP Apps bridge and uses 'window.openai' only as an additive ChatGPT
enhancement. CI rejects unsafe 'innerHTML'. Approved external links use the host
navigation API and an explicit HTTPS redirect allowlist.

The server exposes initialize, tool/resource discovery, and the declared UI
resource before account linking so ChatGPT and Copilot can discover the app.
Every tool descriptor declares its OAuth 'securitySchemes'; tool execution
requires the origin-bound Entra scope
'api://<approved-hostname>/<client-id>/access_as_user'. An unauthenticated or
incorrectly scoped tool call returns '_meta["mcp/www_authenticate"]' with the
RFC protected-resource challenge. Typed app tools declare 'outputSchema' and
return matching 'structuredContent'.

## Connect Azure and GitHub OIDC

1. Select the Azure tenant, subscription, resource group, and Foundry project.
2. Register the exact Entra application ID URI
   'api://<approved-hostname>/<client-id>' and delegated scope 'access_as_user'.
3. Verify the protected-resource metadata at
   '<approved-origin>/.well-known/oauth-protected-resource/mcp'.
4. Run 'scripts/Connect-HeliosAzureInteractive.ps1'.
5. Create the protected GitHub 'x-tier-dev' environment.
6. Grant workflow permission 'id-token: write' and 'contents: read'.
7. Configure issuer 'https://token.actions.githubusercontent.com', audience
   'api://AzureADTokenExchange', and exact subject
   'repo:M0nado/helios-platform:environment:x-tier-dev'.
8. Assign least-privilege RBAC at the reviewed scope.
9. Publish a digest-pinned immutable image.
10. Review the exact Bicep, parameter, image, and what-if hashes.
11. Approve deployment separately.

No client secret is required for the GitHub-to-Azure path.

## Add Azure Edge

The current validated endpoint is the Entra-protected Container Apps runtime.
Private edge cutover is a separate administrator-reviewed stage:

1. Register 'Microsoft.CDN' and 'Microsoft.App'.
2. Use a workload-profiles Container Apps environment in a custom VNet.
3. Reserve a delegated infrastructure subnet and separate private-endpoint
   subnet.
4. Deploy Azure Front Door Premium and its private-link origin.
5. Approve the private endpoint connection.
6. Validate TLS, Entra audience, origin restrictions, '/health/ready', and MCP
   initialize/tools/resources handshakes.
7. Disable direct public ingress only after the private route is proven.

## Connect ChatGPT and Codex

After '/health/ready' succeeds:

1. Set 'HELIOS_AZURE_CONNECTOR_URL' to the approved HTTPS origin.
2. Install or refresh the repository plugin.
3. Add the same '<origin>/mcp' endpoint as an internal ChatGPT app.
4. Complete Entra authorization for the exact origin-bound 'access_as_user'
   scope.
5. Verify public initialize/discovery, authenticated 'search' and 'fetch',
   resource read, Monado rendering, and the auth challenge on an unlinked tool
   call.

## Connect Microsoft 365 Copilot and Teams

The app package contains 'manifest.json', 'declarativeAgent.json', and
'ai-plugin.json'. Use Microsoft 365 Agents Toolkit 6.12 or later to provision an
Entra SSO or OAuth Plugin Vault registration, replace toolkit variables,
validate, sideload into development, and publish only after tenant review.

Copilot Studio and Foundry should bind the same approved Remote MCP endpoint,
pin the allowed HELIOS tools, and complete non-production evaluation before
publication.

## Connect Azure DevOps

The plugin binds 'https://mcp.dev.azure.com/<organization>' with
'X-MCP-Readonly: true' and toolsets 'repos,wit,pipelines,wiki'. GitHub remains
the release source of truth. The initial sync is GitHub-to-Azure-DevOps
plan-only; automatic writes and bidirectional merge remain disabled.

## Runner contract

Validation and protected release use GitHub-hosted runners. Optional
'helios-linux' and 'helios-windows' self-hosted groups are documented but
disabled until they are ephemeral, isolated, allow-listed, and bound to a
protected environment.

## Approval boundary

No repository file or plugin command grants tenant consent, assigns RBAC,
deploys Azure, approves a private endpoint, enables live connector relays,
merges a pull request, publishes an agent, or enables a runner automatically.
