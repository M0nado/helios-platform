# HELIOS Control Fabric

One plan-first Codex plugin and one shared Remote MCP app for the governed
HELIOS integration plane.

It connects the approved operator experience to:

- the Entra-protected HELIOS Streamable HTTP MCP endpoint;
- the MCP Apps Monado Control Center at
  'ui://helios/control-center-v2.html';
- read-only Azure and Azure DevOps MCP tools;
- GitHub, Linear, and Slack MCP endpoints;
- Microsoft Foundry;
- Microsoft 365 Copilot, Copilot Studio, Teams, and SharePoint templates;
- deterministic GitHub OIDC, Azure Edge, DevOps sync, and runner contracts.

## Safe default

The plugin diagnoses, reads, searches, fetches, renders, and plans. It does not
silently deploy Azure, assign RBAC, grant tenant consent, merge pull requests,
publish Microsoft 365 apps, enable self-hosted runners, create a bidirectional
DevOps mirror, or activate external connector delivery.

`monado/helios-control/config/custom-mcp-connector-plane.json` is the explicit
tool contract for custom MCP connectors. Access is deny-by-default and
write/mutation tools remain disabled until reviewed expansion.

## Start

    python plugins/helios-control-fabric/scripts/helios.py doctor
    python plugins/helios-control-fabric/scripts/helios.py targets
    python plugins/helios-control-fabric/scripts/helios.py plan --environment azure-dev
    python plugins/helios-control-fabric/scripts/helios.py oidc --environment azure-dev
    python plugins/helios-control-fabric/scripts/helios.py fleet
    python plugins/helios-control-fabric/scripts/helios.py edge --environment azure-dev
    python plugins/helios-control-fabric/scripts/helios.py devops-sync
    python plugins/helios-control-fabric/scripts/helios.py runners

PowerShell:

    ./plugins/helios-control-fabric/scripts/helios.ps1 doctor
    ./plugins/helios-control-fabric/scripts/helios.ps1 oidc --environment azure-dev
    ./plugins/helios-control-fabric/scripts/helios.ps1 fleet

Every command above is read-only. Add '--json' for machine-readable output.
The `oidc` command requires an authenticated GitHub CLI session because it
reads the repository metadata and effective OIDC subject policy. It mirrors the
Azure onboarding wizard: immutable owner/repository IDs are used when GitHub
enables them, custom subject templates are rejected, and missing policy fields
fail closed rather than falling back to a guessed subject.
The `fleet` command resolves the registry from the repository checkout. If the
plugin is installed outside the repository tree, set `HELIOS_REPOSITORY_ROOT`
or `HELIOS_ENTERPRISE_FLEET_REGISTRY` to an explicit local source-controlled
registry path.

## Environment

Set these non-secret values after an administrator selects the tenant and
runtime:

- 'HELIOS_AZURE_CONNECTOR_URL' — approved HTTPS origin of the deployed HELIOS
  connector, without the '/mcp' suffix.
- 'AZURE_DEVOPS_ORGANIZATION' — Azure DevOps organization name.
- 'AZURE_TENANT_ID'
- 'AZURE_SUBSCRIPTION_ID'
- 'AZURE_RESOURCE_GROUP'
- 'AZURE_CLIENT_ID'

Authentication is interactive or workload-identity based. Do not place access
tokens, client secrets, relay keys, or Key Vault values in these files.

## Runtime contract

The same '<origin>/mcp' endpoint serves:

- standard 'search' and 'fetch' tools;
- the HELIOS configuration snapshot;
- the interactive Monado MCP Apps resource;
- read-only Azure and Foundry inventory;
- deterministic automation and upgrade proposals;
- saved control-run and connector-binding reads.

The Monado widget uses the MCP Apps bridge first and treats 'window.openai' as
an optional host enhancement. Tool content is rendered with DOM 'textContent';
unsafe 'innerHTML' is prohibited by CI.

## Release order

1. Land the current reviewed GitHub integration line.
2. Authenticate an Azure administrator and select the tenant/subscription.
3. Create least-privilege Entra identities and the exact protected-environment
   GitHub OIDC trust.
4. Publish an immutable ACR image by digest.
5. Review and approve the exact Azure 'what-if' evidence.
6. Approve deployment separately.
7. Complete the Front Door Premium and Container Apps Private Link cutover,
   including administrator approval of the private endpoint connection.
8. Bind and test connector relays in dry-run.
9. Register the same '/mcp' endpoint in ChatGPT, Microsoft 365 Copilot,
   Copilot Studio, Teams, and Foundry.
10. Promote the SharePoint setup guide into a team-owned Helios site.

See the audited control-center snapshot at
https://helios-control-center.thepatman64.chatgpt.site.
