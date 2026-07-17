# HELIOS Azure Dev Live Connect

This slice reconciles the reviewed Azure concepts from PR #176 and the operator/app setup concepts from PR #177 onto fresh canonical `main` without importing either stale branch history.

It provides:

- a runnable PowerShell Azure CLI connector;
- explicit subscription and tenant selection;
- optional secretless Entra application and GitHub OIDC federation;
- optional protected `azure-dev` GitHub environment configuration;
- environment variables for GitHub Actions OIDC;
- Bicep compilation and resource-group validation;
- a development-only Azure what-if operation;
- current Azure, GitHub, Linear, Slack, Foundry, Agent 365, Teams, and SharePoint connection metadata;
- local AIHub, Hermes, and XCore health checks.

No deployment or secret creation is included.

## Fresh reconciliation source

```text
Canonical base: M0nado/helios-platform main
Base SHA:       b8d61e0bda11ed43aa559f2ae578dd535b6c4180
Azure source:   PR #176, reviewed concepts only
App/setup:      PR #177, reviewed concepts only
Branch:         integration/azure-dev-live-connect-v1
```

## 1. Install the required local tools

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

## 3. Connect Azure and run a local what-if

Replace the subscription and tenant IDs with the values shown by `az account list`.

```powershell
az login --use-device-code
az account list --output table

pwsh -NoProfile -File .\integration\azure-dev-live-connect\scripts\Connect-HeliosAzureDev.ps1 `
  -SubscriptionId '<approved-subscription-id>' `
  -TenantId '<approved-tenant-id>' `
  -ResourceGroup 'rg-helios-dev' `
  -Location 'eastus2' `
  -CreateResourceGroup `
  -ResourceGroupConfirmation 'CREATE HELIOS AZURE DEV RESOURCE GROUP' `
  -RunWhatIf
```

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

## 5. Create the protected GitHub environment and variables

Authenticate GitHub CLI:

```powershell
gh auth login
```

A required reviewer can be supplied as the numeric GitHub user ID. Omitting it still creates the environment with protected-branch deployment policy and self-review prevention, but a reviewer should be added before promotion.

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

```powershell
pwsh -NoProfile -File .\integration\azure-dev-live-connect\scripts\Start-HeliosConnections.ps1 `
  -CheckClaudeMcp `
  -CheckAzureDevOps `
  -CheckLocalServices
```

Connection registry:

```text
integration/azure-dev-live-connect/config/connections.json
```

Remote MCP OAuth is completed by the MCP client when the server is first used. The registry contains no OAuth tokens.

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
