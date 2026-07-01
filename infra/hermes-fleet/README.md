# Azure deployment scaffold

This folder is a clean Azure infrastructure surface for `hermes-fleet-platforms`. It is intentionally separate from the repo's historical Azure notes under `azure-integration/` and `cloud-integration/`.

## What this stack deploys

- A dedicated virtual network with three subnets:
  - `app` for App Service regional VNet integration
  - `data` for Storage and Cosmos DB access controls
  - `compute` for an optional management VM
- Two Linux Web Apps on a shared App Service Plan:
  - API tier
  - Portal tier
- Storage Account with `data`, `logs`, and `backups` blob containers
- Cosmos DB SQL API account, database, and starter container
- Log Analytics workspace and Application Insights instance
- Optional Windows jumpbox VM for admin and troubleshooting workflows

## Files

- `main.bicep` - entrypoint for the full deployment
- `main.parameters.json` - sample parameter file
- `modules/network.bicep` - VNet and subnet topology
- `modules/data-stack.bicep` - Storage + Cosmos DB resources
- `modules/web-stack.bicep` - App Service Plan, Web Apps, App Insights, Log Analytics
- `modules/compute.bicep` - optional jumpbox VM

## Deployment example

```bash
az deployment group create \
  --resource-group <resource-group> \
  --template-file infra/azure/main.bicep \
  --parameters @infra/azure/main.parameters.json \
  --parameters vmAdminPassword='<secure-password>'
```

## Design assumptions

- Runtime matches the repo's active .NET 8 direction.
- Web apps use system-assigned managed identities so secrets do not need to be baked into code. The API identity is granted Storage Blob Data Contributor and Cosmos DB SQL built-in Data Contributor data-plane access during deployment.
- Data services are private by default and scoped to the `data` subnet.
- The VM is optional because the repo mixes desktop, agent, and service concerns; most environments should start without it.

## Connectivity guidance

1. Keep user ingress at the web tier and avoid direct public exposure for Storage and Cosmos DB.
2. Use the `app` subnet only for App Service integration because it is delegated to `Microsoft.Web/serverFarms`.
3. Keep the `data` subnet for service endpoints and later private endpoints if you want to harden further.
4. If the jumpbox VM is enabled, lock RDP source ranges before production use.
5. Verify the API web app identity keeps its generated Storage Blob Data Contributor and Cosmos DB SQL built-in Data Contributor assignments before enabling production traffic.

## Next hardening steps

- Replace service endpoints with private endpoints for Storage and Cosmos DB.
- Add Key Vault and move runtime settings to Key Vault references.
- Add deployment slots for the API and portal apps.
- Add Azure Front Door or Application Gateway if this becomes internet-facing.
- Add diagnostic settings for Storage, Cosmos DB, and App Service to centralize logs.
