# Helios Azure Connector Deployment

The Helios Azure Connector is a read-only Azure Resource Manager inventory API
and MCP server. It is intended for Azure Container Apps, Copilot-compatible
clients, and governed agent workflows.

## Security boundary

- The Container Apps auth sidecar validates bearer tokens and sanitizes
  platform principal headers on every route; the application returns
  route-specific `401` responses unless an allowed Entra principal is injected.
- `/health`, `/health/live`, `/health/ready`, the OpenAPI document, and OAuth
  protected-resource metadata are public discovery routes. Connector REST and
  remote MCP routes require Entra; provider webhook ingress remains a separate
  signature/validation boundary and fails closed when live mode is enabled.
- An application-level check requires the Easy Auth principal header.
- The user-assigned managed identity receives Reader only on the deployment resource group.
- Returned inventory is limited to resource ID, name, type, and location.
- No deploy, delete, role-assignment, consent, secret-read, or publication tool exists.
- Production remains `dry-run`; deploying the connector does not unlock Helios mutations.

## Azure prerequisites

1. An existing Azure subscription and resource group.
2. An Entra single-tenant app registration exposing a delegated
   `user_impersonation` scope.
3. The app registration client ID and tenant ID.
4. At least one allowed user or service-principal object ID.
5. An interactive administrator allowed to register the required providers and
   create the narrow bootstrap role assignments.
6. A reviewer-protected GitHub environment and a dedicated, Helios-tagged ACR.

For `azd`, set `HELIOS_ENTRA_CLIENT_ID` and
`HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID` in the selected `azd` environment. The
deployment identity's `AZURE_CLIENT_ID` remains separate from the connector app
registration.

These identifiers are not secrets. Do not place app secrets, tokens, or passwords
in the parameter file.

## Preview and deploy

The recommended end-to-end operator path is
`scripts/Connect-HeliosAzureInteractive.ps1`, documented in
`AZURE_INTERACTIVE_ONBOARDING.md`. It can authenticate through Azure CLI,
select the target, configure scoped secretless identity, publish with an ACR
cloud build, run what-if, deploy only after a second confirmation, and verify
the resulting immutable registry binding.

### Cloud Shell PowerShell

Use `Connect-HeliosAzureInteractive.ps1` for all identity and registry
bootstrap. It resolves the repository's current immutable OIDC subject,
verifies the reviewer and branch policy before creating trust, gives CI only
resource-group Contributor plus registry read, and creates the runtime identity
and its Reader/registry-read grants under the administrator session.

```powershell
pwsh -NoProfile -File ./scripts/Connect-HeliosAzureInteractive.ps1 `
  -Mode Configure `
  -EnvironmentName dev `
  -ResourceGroup rg-helios-dev `
  -ContainerRegistryName '<globally-unique-acr-name>' `
  -RequiredReviewerId '<github-user-id>'
```

The wizard never accepts an OpenAI key, never retrieves Azure OpenAI account
keys, and never prints secret values. The Key Vault is created empty with RBAC
enabled and a deny-by-default network ACL. Select a Foundry/Azure OpenAI project,
model deployment, and identity assignment separately after confirming regional
availability and quota.

The immutable image must be in the dedicated Azure Container Registry named by
`containerRegistryName` in the same Helios resource group. This guarantees that
the authorization-mode-appropriate registry read assignment protects the exact
registry used by the Container App. The interactive Publish phase establishes
this invariant. The Bicep template references but never mutates that registry.

The Bash compatibility script is preview-only. Its legacy `--apply` path is
retired and fails closed with directions to the interactive wizard.

### PowerShell

From an authenticated PowerShell 7 session:

```powershell
./scripts/Deploy-HeliosAzureConnector.ps1 `
  -SubscriptionId '<subscription-id>' `
  -ResourceGroup '<resource-group>' `
  -EntraClientId '<app-client-id>' `
  -EntraTenantId '<tenant-id>' `
  -AllowedPrincipalObjectId '<admin-object-id>' `
  -ImageReference '<registry>/helios-connect@sha256:<64-hex-digest>'
```

The default execution runs ARM `what-if` only. Its direct `-Apply` path is
retired; deploy through the reviewer-gated `helios-cloud-deploy` workflow, which
binds deployment to a hashed plan and immutable image.

## Connect clients

- MCP endpoint: `https://<container-app-host>/mcp`
- REST inventory: `https://<container-app-host>/connector/resources`
- Foundry inventory: `https://<container-app-host>/connector/foundry`
- OpenAPI: `https://<container-app-host>/openapi/v1.json`

For Power Platform/Copilot Studio, replace the placeholders in
`connector/helios-azure-connector.openapi.yaml`, then import it as a custom
connector and configure the Entra OAuth connection. For an MCP client, start from
`connector/mcp-manifest.example.json` and use an access token whose audience is
the connector app registration.

## Verification

1. Confirm `/health`, `/health/live`, and `/health/ready` return `200` without authentication.
2. Confirm `/connector/context` returns `401` without a token.
3. Sign in as an allowed principal and confirm the context reports
   `read-only-resource-group`.
4. Confirm resource inventory contains no properties, keys, connection strings,
   tags, or secret values.
5. Confirm MCP `tools/list` exposes only the three inventory tools.

Run the checks with `scripts/Test-HeliosCloudConnection.ps1`; add
`-InteractiveAuth` for the authenticated context and MCP checks. The access
token is retained only in memory and is never written to the report.
