# HELIOS Azure infrastructure

This directory contains the Azure infrastructure-as-code consumed by
`microsoft-ecosystem/.github/workflows/azure-deploy.yml`.

## Entry point

- `main.bicep` is the deployment template used by both staging and production.
- The workflow passes `environment=staging` for the staging resource group and
  `environment=production` for the production resource group.

## Provisioned resources

The template creates the core platform resources needed by the zip-deploy
workflow:

- Linux App Service plan
- Linux App Service configured for .NET 8 zip deployment
- User-assigned managed identity
- Key Vault with Azure RBAC enabled
- Storage account with public blob access and shared keys disabled
- Log Analytics workspace
- Application Insights linked to Log Analytics
- RBAC assignments for the managed identity to read Key Vault secrets and use
  blob storage

Resource names are environment-aware. Production uses the `prod` suffix so the
workflow's `helios-app-prod` deployment target matches the Bicep-generated App
Service name.
