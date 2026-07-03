# HELIOS Azure Hybrid Architecture

This repo now includes a safe foundation for hybrid Azure architecture that can support local workstations, GitHub-hosted runners, Codespaces, Cloud Shell, future self-hosted runners, and private Azure resources.

## Foundation

- Resource group deployment entry: `infra/azure/main.bicep`
- Storage/reporting: `infra/azure/modules/storage.bicep`
- Observability: `infra/azure/modules/observability.bicep`
- Secrets: `infra/azure/modules/keyvault.bicep`
- Hybrid network landing zone: `infra/azure/modules/network.bicep`

## Intended topology

1. GitHub Actions and Cloud Shell run read-only/report-generation workflows by default.
2. Azure Key Vault stores provider keys and remote URLs without committing values.
3. A dedicated virtual network provides future placement for private endpoints, self-hosted runners, API gateways, and hybrid connectors.
4. Storage and Log Analytics provide dashboard/report hosting and telemetry foundations.
5. Future private connectivity can layer VPN/ExpressRoute, private endpoints, container apps, app services, or AKS without changing the local command center contract.

## Safe validation commands

```bash
az deployment group what-if --resource-group <resource-group> --template-file infra/azure/main.bicep --parameters @infra/azure/parameters/dev.json
az bicep build --file infra/azure/main.bicep
```
