# HELIOS Azure Dev Live Connect

This slice reconciles the reviewed Azure concepts from PR #176 and the operator/app setup concepts from PR #177 onto fresh canonical `main` without importing either stale branch history.

It provides:

- a runnable PowerShell Azure CLI connector;
- browser or device-code Azure login plus an interactive subscription picker;
- optional secretless Entra application and GitHub OIDC federation;
- optional protected `azure-dev` GitHub environment configuration;
- environment variables for GitHub Actions OIDC;
- Bicep compilation and resource-group validation;
- a development-only Azure what-if operation;
- current Azure, GitHub, Linear, Slack, Foundry, Agent 365, Teams, and SharePoint connection metadata;
- authenticated read-only Azure/GitHub cloud preflight evidence;
- optional local AIHub, Hermes, and XCore health checks.

No deployment or secret creation is included.

## Fresh reconciliation source

```text
Canonical base: M0nado/helios-platform main
Base SHA:       b8d61e0bda11ed43aa559f2ae578dd535b6c4180
Azure source:   PR #176, reviewed concepts only
App/setup:      PR #177, reviewed concepts only
Branch:         integration/azure-dev-live-connect-v1
```

## 1. Open the operator shell

Use Azure Cloud Shell PowerShell when you want the operator session to run in
Azure. A workstation is optional; it is never part of the deployed runtime.

For a Windows workstation, install the required administration clients:

Open **PowerShell 7 as Administrator**:

```powershell
winget install --id Microsoft.AzureCLI --exact
winget install --id Microsoft.PowerShell --exact
winget install --id GitHub.cli --exact
winget install --id OpenJS.NodeJS.LTS --exact

az extension add --name azure-devops
```

Restart the terminal after installation if `az` or `gh` is not found.

## 2. Check out the reconciliation branch

```powershell
gh repo clone M0nado/helios-platform C:\src\helios-platform
Set-Location C:\src\helios-platform
git fetch origin
git checkout integration/azure-dev-live-connect-v1
```

## 3. Connect Azure and run an Azure what-if

Choose the login UX explicitly. When `-SubscriptionId` is omitted, the script lists enabled subscriptions in the selected tenant and prompts for the approved one. Pass `-SubscriptionId` to make automation non-interactive.

```powershell
pwsh -NoProfile -File .\integration\azure-dev-live-connect\scripts\Connect-HeliosAzureDev.ps1 `
  -LoginMode DeviceCode `
  -TenantId '<approved-tenant-id>' `
  -ResourceGroup 'rg-helios-dev' `
  -Location 'eastus2' `
  -CreateResourceGroup `
  -ResourceGroupConfirmation 'CREATE HELIOS AZURE DEV RESOURCE GROUP' `
  -RunWhatIf
```

Use `-LoginMode Browser` for browser-based Azure CLI authentication. The login mode is used only when the current Azure CLI session is not authenticated.

This command performs validation and what-if only. It writes evidence under:

```text
integration/azure-dev-live-connect/evidence/
```

## 4. Configure secretless GitHub OIDC

This creates or reuses one Entra application, creates its service principal, and adds the exact environment-bound federated credential:

```text
repo:M0nado/helios-platform:environment:azure-dev
```

Run:

```powershell
pwsh -NoProfile -File .\integration\azure-dev-live-connect\scripts\Connect-HeliosAzureDev.ps1 `
  -SubscriptionId '<approved-subscription-id>' `
  -TenantId '<approved-tenant-id>' `
  -ResourceGroup 'rg-helios-dev' `
  -ConfigureOidc `
  -OidcConfirmation 'CONFIGURE HELIOS AZURE DEV OIDC'
```

No client secret is generated.

The repository and environment are immutable for this connector: `M0nado/helios-platform` and `azure-dev`. An existing `github-azure-dev` federated credential must exactly match the canonical issuer, subject, and audience; a mismatch fails closed and is never overwritten.

## 5. Create the protected GitHub environment and variables

Authenticate GitHub CLI:

```powershell
gh auth login
```

A positive numeric GitHub user ID is mandatory. Environment configuration fails closed when `-RequiredReviewerId` is omitted or invalid. Self-review prevention and protected-branch deployment policy remain enabled.

```powershell
pwsh -NoProfile -File .\integration\azure-dev-live-connect\scripts\Connect-HeliosAzureDev.ps1 `
  -SubscriptionId '<approved-subscription-id>' `
  -TenantId '<approved-tenant-id>' `
  -ResourceGroup 'rg-helios-dev' `
  -ConfigureGitHubEnvironment `
  -RequiredReviewerId '<github-user-id>' `
  -GitHubEnvironmentConfirmation 'CONFIGURE HELIOS AZURE DEV ENVIRONMENT'
```

The script writes these non-secret environment variables:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
AZURE_RESOURCE_GROUP
```

## 6. Start and inspect connections

After the GitHub environment exists, run the authenticated read-only cloud
preflight. This avoids requiring that environment during the initial bootstrap.

```powershell
pwsh -NoProfile -File .\integration\azure-dev-live-connect\scripts\Start-HeliosConnections.ps1 `
  -CheckCloudConnections `
  -SubscriptionId '<approved-subscription-id>' `
  -TenantId '<approved-tenant-id>' `
  -ResourceGroup 'rg-helios-dev' `
  -CheckClaudeMcp `
  -CheckAzureDevOps `
  -CheckLocalServices
```

Connection registry:

```text
integration/azure-dev-live-connect/config/connections.json
```

Remote MCP OAuth is completed by the MCP client when the server is first used. The registry contains no OAuth tokens.

`-CheckCloudConnections` performs authenticated reads only: Azure account/context, resource-group lookup, resource inventory, GitHub repository lookup, and `azure-dev` environment lookup. It never requests an access token, changes RBAC, updates an environment, or invokes an MCP tool.

## 7. Run the protected GitHub what-if

After the reconciliation PR is reviewed and merged to protected `main`:

1. Open **Actions → HELIOS Azure Dev Live Connect**.
2. Select **Run workflow** on `main`.
3. Set `run_what_if=true`.
4. Approve the `azure-dev` environment request.
5. Download the `helios-azure-dev-what-if-<sha>` artifact.

The workflow requests an OIDC token and runs `az deployment group what-if`. It contains no Azure apply job.

## Azure DevOps defaults

```powershell
az devops configure --defaults `
  organization='https://dev.azure.com/<organization>' `
  project='<project>'

az devops project list --output table
```

## Safety boundary

This reconciliation does not:

- deploy the Bicep template;
- create Azure role assignments;
- grant Owner or subscription-wide Contributor;
- create client secrets;
- write Key Vault secrets;
- grant Microsoft Graph or Agent 365 consent;
- publish Copilot agents;
- merge itself;
- perform destructive workstation operations.

The Bicep foundation intentionally remains development-scoped and must be reviewed before any separate deployment implementation is added.
