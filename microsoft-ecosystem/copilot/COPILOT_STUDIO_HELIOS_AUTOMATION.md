# Copilot Studio HELIOS Automation Setup

This runbook connects Copilot Studio to HELIOS local automation and Azure-hosted automation resources.

## Prerequisites

- Azure CLI installed and authenticated with `az login`.
- Permissions to create resource groups, managed identities, Key Vault, Storage, Log Analytics, Application Insights, Event Hubs, and Service Bus.
- Copilot Studio environment with permission to create custom actions/connectors.

## Bootstrap Azure and Local Configuration

Run a dry-run first:

```powershell
pwsh ./scripts/microsoft-enterprise/setup-helios-azure-automation.ps1 -EnvironmentName prod -WhatIf
```

Apply the setup:

```powershell
pwsh ./scripts/microsoft-enterprise/setup-helios-azure-automation.ps1 -EnvironmentName prod
```

The script writes `config/azure-automation.local.json` for local HELIOS components and `microsoft-ecosystem/copilot/copilot-studio-helios-actions.json` for Copilot Studio action mapping. A committed starter template is available at `microsoft-ecosystem/copilot/copilot-studio-helios-actions.template.json`.

## Copilot Studio Actions

Import or recreate the actions from `copilot-studio-helios-actions.json` after running the bootstrap script, or start from `copilot-studio-helios-actions.template.json` before Azure resource names are finalized:

| Action | Azure target | HELIOS purpose |
| --- | --- | --- |
| `submitAutomationJob` | Service Bus queue | Queue automation jobs for local or cloud workers. |
| `publishFleetEvent` | Event Hub | Send Hermes fleet telemetry and state changes. |
| `queryOperationalHealth` | Log Analytics | Let Copilot answer health and incident questions. |
| `retrieveSecretReference` | Key Vault | Resolve approved secret references without revealing raw secrets. |

## Integration Notes

- **helios-control / WinUI 3 / C#**: read `config/azure-automation.local.json` to display cloud status and submit jobs.
- **hermes-fleet-production / C++ / Python**: use Event Hubs for high-volume telemetry and Service Bus for work dispatch.
- **F# analytics / XCore specialist**: use Log Analytics and Application Insights names from the manifest for prediction and anomaly jobs.
- **Security**: assign the generated managed identity least-privilege RBAC only for the resources it must access.

## Verification

```powershell
az group show --name rg-helios-platform-prod
az servicebus queue show --resource-group rg-helios-platform-prod --namespace-name <serviceBusNamespace> --name helios-automation-jobs
az eventhubs eventhub show --resource-group rg-helios-platform-prod --namespace-name <eventHubNamespace> --name helios-automation-events
```
