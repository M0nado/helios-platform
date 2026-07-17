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
  signature/validation boundary and fails closed in every execution mode.
- An application-level check requires the Easy Auth principal header.
- The user-assigned managed identity receives operator-bootstrapped Reader on
  the deployment resource group and template-owned Cosmos Data Contributor only
  on the `control-runs` container.
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

Direct `azd provision` and `azd deploy` are blocked by `azure.yaml`; `azd` is an
authentication and environment-inspection client only. Use
`scripts/Invoke-HeliosProvisionPreview.ps1` for a non-mutating local ARM what-if
or the protected GitHub workflow for reviewed preview and deployment. The
deployment identity's `AZURE_CLIENT_ID` remains separate from the connector app
registration. `infra/main.bicep` is only a wrapper around the hardened
`connector.bicep`; it cannot create a registry or grant Azure management roles.
The connector module owns only its container-scoped Cosmos SQL data role.

These identifiers are not secrets. Do not place app secrets, tokens, or passwords
in the parameter file.

For a reviewed Edge/custom-DNS front door, set `publicBaseUrl` to the canonical
HTTPS origin and preserve that host through the proxy. The default remains the
Container Apps FQDN. Custom DNS, TLS certificates, Entra redirect URIs, and
private-origin networking are separate what-if and administrator gates.

## Preview and deploy

The recommended end-to-end operator path is
`scripts/Connect-HeliosAzureInteractive.ps1`, documented in
`AZURE_INTERACTIVE_ONBOARDING.md`. It can authenticate through Azure CLI,
select the target, configure scoped secretless identity, publish with an ACR
cloud build from the exact checked-out commit, and run what-if inside the
protected workflow. Application deployment occurs only through the two
protected GitHub workflow approvals.

### Cloud Shell PowerShell

Use `Connect-HeliosAzureInteractive.ps1` for all identity and registry
bootstrap. It resolves the repository's current immutable OIDC subject,
verifies the reviewer and branch policy before creating trust, gives CI only
resource-group Contributor plus the registry writer role needed for the
commit-bound ACR build, and creates the runtime identity and its
Reader/registry-read grants under the administrator session. The Entra
credential matches the exact protected-environment subject. Required-reviewer
and exact deployment-branch policies gate every job that can receive its OIDC
token; ordinary jobs do not emit the reusable-workflow-only `job_workflow_ref`.

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

The protected workflow builds the exact clean `GITHUB_SHA` into the dedicated
Azure Container Registry named by `containerRegistryName`, then passes only its
resolved digest to Bicep. The interactive Publish phase establishes and
revalidates this trust and authorization invariant. The Bicep template
references but never mutates that registry.

Preview compiles the template with the pinned Bicep version, canonicalizes it,
and binds its SHA-256 plus every resolved deployment parameter into the evidence
manifest. What-if uses `FullResourcePayloads`; a property-level canonical hash
is re-created immediately before deploy. Only the redacted review copy is
uploaded to GitHub.

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
