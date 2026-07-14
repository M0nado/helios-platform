# HELIOS Azure deployment and DevOps automation

This document binds the same Bicep deployment contract to GitHub Actions and Azure DevOps.

## Deployment contract

- Canonical template: `infra/bicep/enterprise/main.bicep`
- Environments: `dev`, `test`, `prod`
- Authentication: workload identity federation / OIDC only
- Secret-based service principals: prohibited
- RBAC scope: target resource group, not subscription-wide
- Mandatory sequence: validate → what-if → protected approval → deploy → evidence
- Production: disabled until the production environment owner approves the run

## GitHub Actions binding

Workflow: `.github/workflows/enterprise-bicep.yml`

Create these protected environments:

- `helios-dev-what-if`
- `helios-test-what-if`
- `helios-prod-what-if`
- `helios-dev-deploy`
- `helios-test-deploy`
- `helios-prod-deploy`

For each environment configure OIDC identity references:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Configure environment variable:

- `AZURE_RESOURCE_GROUP`

Deploy jobs require the self-hosted runner labels:

```text
self-hosted
linux
x64
helios-azure
```

The first authorized run must use:

```text
environment_name=dev
apply=false
```

Review the uploaded `helios-dev-what-if` artifact before any apply run.

## Azure DevOps binding

Pipeline: `azure-pipelines.yml`

Create three Azure Resource Manager service connections using workload identity federation:

- `helios-azure-wif-dev`
- `helios-azure-wif-test`
- `helios-azure-wif-prod`

Grant each identity only the minimum role required on its target resource group:

- `rg-helios-dev`
- `rg-helios-test`
- `rg-helios-prod`

Recommended initial role:

```text
Contributor on the environment resource group only
```

Key Vault data-plane access must be assigned separately and only to runtime managed identities that require it. The pipeline identity should not receive secret-read permissions.

Create Azure DevOps environments:

- `helios-dev-what-if`
- `helios-test-what-if`
- `helios-prod-what-if`
- `helios-dev-deploy`
- `helios-test-deploy`
- `helios-prod-deploy`

Attach manual approvals and checks to every `*-deploy` environment. Require additional reviewers for production.

Create or register the private deployment pool:

```text
helios-azure
```

The deploy agent must run Linux and have Azure CLI available. Validation and what-if remain Microsoft-hosted.

The first Azure DevOps run must use:

```text
environmentName=dev
apply=false
```

Review the `helios-dev-what-if` artifact before setting `apply=true`.

## Resource-group prerequisites

The pipelines deliberately require the target resource group to exist before what-if. Bootstrap resource groups outside the deployment pipeline under administrator review, then bind the federated identities at resource-group scope.

Expected resource groups:

```text
rg-helios-dev
rg-helios-test
rg-helios-prod
```

Default region in Azure DevOps is `eastus2`. Change `azureLocation` only through a reviewed pull request.

## Evidence and rollback

Every what-if and deployment publishes a JSON artifact. Preserve artifacts for audit and rollback analysis.

A failed what-if blocks deployment. A skipped approval blocks deployment. An absent service connection, OIDC binding, resource group, protected environment, or runner blocks deployment.

No pipeline may create credentials, print tokens, grant subscription-wide RBAC, read back Key Vault secrets, or bypass environment approvals.
