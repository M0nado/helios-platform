# HELIOS Azure Infrastructure

Bicep templates for dashboard/report storage and observability.

## Hermes/XCore runner baseline

`infra/azure/main.bicep` now supports an optional Hermes/XCore runner slice:

- user-assigned managed identity for runner workloads;
- Service Bus namespace with Hermes/XCore request queues plus dead-letter queue;
- private artifact storage container for runner evidence;
- optional manual-trigger Container Apps Jobs for Hermes orchestration and XCore evaluation.

Enable it with:

- `enableHermesXcoreRunner=true` for baseline resources;
- `enableHermesXcoreRunnerJobs=true` to add manual jobs;
- immutable `hermesRunnerImage` and `xcoreRunnerImage` digests for job containers.

The default parameter file keeps this disabled, preserving current behavior.

## Validate locally

```bash
az bicep build --file infra/azure/main.bicep
az deployment group validate \
  --resource-group <resource-group> \
  --template-file infra/azure/main.bicep \
  --parameters infra/azure/parameters/dev.json
```

Deployment should be gated through `.github/workflows/azure-infra.yml`.
