# HELIOS Azure Infrastructure

Bicep templates for dashboard/report storage and observability.

## Validate locally

```bash
az bicep build --file infra/azure/main.bicep
az deployment group validate \
  --resource-group <resource-group> \
  --template-file infra/azure/main.bicep \
  --parameters infra/azure/parameters/dev.json
```

Deployment should be gated through `.github/workflows/azure-infra.yml`.
