# HELIOS Control Fabric

One Codex plugin for the governed HELIOS integration plane.

It connects the approved operator experience to:

- the Entra-protected HELIOS Streamable HTTP MCP endpoint;
- read-only Azure and Azure DevOps MCP tools;
- GitHub, Linear, and Slack MCP endpoints;
- Microsoft Foundry;
- the HELIOS Monado Control Center website;
- Microsoft 365 Copilot and Teams package templates.

## Safe default

The plugin diagnoses, reads, and plans. It does not silently deploy Azure,
assign RBAC, grant tenant consent, merge pull requests, publish Microsoft 365
apps, or activate external connector delivery.

## Start

```bash
python plugins/helios-control-fabric/scripts/helios.py doctor
python plugins/helios-control-fabric/scripts/helios.py targets
python plugins/helios-control-fabric/scripts/helios.py plan --environment azure-dev
```

PowerShell:

```powershell
./plugins/helios-control-fabric/scripts/helios.ps1 doctor
```

## Environment

Set these non-secret values after an administrator selects the tenant and
runtime:

- `HELIOS_AZURE_CONNECTOR_URL` — HTTPS origin of the deployed HELIOS connector.
- `AZURE_DEVOPS_ORGANIZATION` — Azure DevOps organization name.
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`

Authentication is interactive or workload-identity based. Do not place access
tokens, client secrets, relay keys, or Key Vault values in these files.

## Release order

1. Merge and validate the active HELIOS integration line.
2. Authenticate an Azure administrator and select the tenant/subscription.
3. Create least-privilege Entra identities and GitHub `azure-dev` OIDC trust.
4. Publish an immutable ACR image.
5. Review and approve the exact Azure `what-if` evidence.
6. Approve the deployment separately.
7. Bind and test connector relays in dry-run.
8. Register the same `/mcp` endpoint in ChatGPT, Microsoft 365 Copilot,
   Copilot Studio, Teams, and Foundry.

See the control center at
https://helios-control-center.thepatman64.chatgpt.site.
