# HELIOS Platform Deployment Guide

This repository deploys HELIOS through a shared Bash entrypoint at `scripts/deploy/deploy-platform.sh`, with GitHub Actions and Azure Pipelines acting as the two supported CI/CD entrypoints.

## Deployment surfaces

- `scripts/deploy/deploy-platform.sh` is the canonical deployment entrypoint.
- `.github/workflows/deploy.yml` runs the shared script from GitHub Actions using Azure OIDC.
- `azure-pipelines.yml` runs the same shared script from Azure Pipelines through an Azure service connection.
- `deployment/main.bicep` is the resource-group-scoped Bicep template invoked by the script.
- `deployment/parameters/platform.parameters.example.json` is the example parameter file for optional overrides.

## Supported phases and targets

The shared deploy script accepts these phases:

- `preflight`
- `infrastructure`
- `container-apps`
- `aks`
- `integrations`
- `verification`
- `all`

Supported targets:

- `aca`
- `aks`
- `both`

`all` runs preflight, infrastructure, the selected platform targets, integrations, and verification.

## Required local prerequisites

- Azure CLI installed and authenticated.
- Bicep support available through `az bicep`.
- Python 3 available for JSON validation and deployment status artifact generation.
- A target Azure subscription with permission to create or update resources in the target resource group.

## Environment configuration

Copy `.env.template` to a local `.env` and populate the deployment values you need.

Core variables used by the deployment path:

```bash
export AZURE_SUBSCRIPTION_ID="00000000-0000-0000-0000-000000000000"
export AZURE_TENANT_ID="00000000-0000-0000-0000-000000000000"
export AZURE_CLIENT_ID="00000000-0000-0000-0000-000000000000"
export AZURE_CLIENT_SECRET="YOUR_AZURE_CLIENT_SECRET"
export HELIOS_RESOURCE_GROUP="rg-helios"
export HELIOS_LOCATION="eastus"
export HELIOS_ENVIRONMENT="prod"
export HELIOS_BASE_NAME="helios"
export HELIOS_DEPLOY_OBSERVABILITY=true
export HELIOS_PARAMETERS_FILE=""
export DEPLOYMENT_OUTPUT_DIR="./deployment-artifacts"
export SLACK_WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
export DEPLOYMENT_STATUS_WEBHOOK="https://example.com/helios/deployments"
export HUBSPOT_TOKEN="YOUR_HUBSPOT_PRIVATE_APP_TOKEN"
```

Notes:

- GitHub Actions uses OIDC for the deploy workflow and does not require `AZURE_CLIENT_SECRET` in that job.
- The cloud integration runtime still uses service principal auth, so keep `AZURE_CLIENT_SECRET` populated until the runtime config is migrated.
- The template still keeps the older `AZURE_RESOURCE_GROUP` and `AZURE_LOCATION` values for runtime compatibility. The deploy workflow uses `HELIOS_RESOURCE_GROUP` and `HELIOS_LOCATION`.

## Running the shared deployment script locally

Run a full deployment:

```bash
./scripts/deploy/deploy-platform.sh \
  --resource-group "$HELIOS_RESOURCE_GROUP" \
  --phase all \
  --target both \
  --location "$HELIOS_LOCATION" \
  --environment "$HELIOS_ENVIRONMENT" \
  --base-name "$HELIOS_BASE_NAME" \
  --what-if false
```

Run a what-if preview for Container Apps only:

```bash
PARAMETERS_FILE="deployment/parameters/platform.parameters.example.json" \
./scripts/deploy/deploy-platform.sh \
  --resource-group "$HELIOS_RESOURCE_GROUP" \
  --phase container-apps \
  --target aca \
  --location "$HELIOS_LOCATION" \
  --environment "$HELIOS_ENVIRONMENT" \
  --base-name "$HELIOS_BASE_NAME" \
  --what-if true
```

## Deployment artifacts and instant reporting

Each run writes artifacts under:

```text
deployment-artifacts/<environment>/<run-id>/
```

Per-run outputs include:

- `deploy.log`
- `deployment-summary.md`
- `deployment-status.json`
- per-deployment JSON or what-if log files
- `account.json`
- `bicep-version.txt`
- verification outputs when verification runs

Latest pointers are also published to:

- `deployment-artifacts/latest-summary.md`
- `deployment-artifacts/latest-status.json`
- `deployment-artifacts/latest-run-dir.txt`

If configured, the deploy script posts start and completion notifications to:

- `SLACK_WEBHOOK_URL`
- `DEPLOYMENT_STATUS_WEBHOOK`

Both notifications include status, phase, target, environment, resource group, run ID, source ref, commit SHA, and run URL when available.

## GitHub Actions deployment flow

The GitHub workflow supports both push-triggered deployment file changes and manual dispatch.

Key inputs:

- `phase`
- `target`
- `environment`
- `location`
- `resource_group`
- `base_name`
- `deploy_observability`
- `parameters_file`
- `what_if`

The workflow does the following:

1. Checks out the repository.
2. Installs Azure CLI.
3. Authenticates with Azure using `azure/login@v2` and OIDC.
4. Validates the deploy script, key JSON files, and `deployment/main.bicep`.
5. Executes the shared deploy script.
6. Uploads deployment assets and run artifacts.

Required GitHub secrets:

- `AZURE_SUBSCRIPTION_ID`
- `AZURE_TENANT_ID`
- `AZURE_CLIENT_ID`

Optional GitHub secrets:

- `SLACK_WEBHOOK_URL`
- `DEPLOYMENT_STATUS_WEBHOOK`

Useful GitHub variables:

- `HELIOS_RESOURCE_GROUP`
- `HELIOS_LOCATION`
- `HELIOS_BASE_NAME`
- `HELIOS_DEPLOY_OBSERVABILITY`
- `HELIOS_PARAMETERS_FILE`
- `HELIOS_CONTROL_PLANE_IMAGE`
- `HELIOS_HUBSPOT_SYNC_IMAGE`
- `HELIOS_AKS_IMAGE`
- `HELIOS_SLACK_CHANNEL`
- `HUBSPOT_BASE_URL`
- `HUBSPOT_TOKEN`

## Azure Pipelines deployment flow

`azure-pipelines.yml` exposes the same deployment shape through pipeline parameters.

Important parameters:

- `phase`
- `target`
- `environmentName`
- `location`
- `resourceGroup`
- `baseName`
- `deployObservability`
- `parametersFile`
- `whatIf`

The pipeline:

1. Validates the deploy script, JSON config files, and Bicep template.
2. Exports deployment metadata into environment variables.
3. Runs the shared deploy script through `AzureCLI@2`.
4. Uploads the generated deployment summary to the build summary.
5. Publishes deployment assets and deployment run artifacts.

Pipeline variables to set when posting is required:

- `slackWebhookUrl`
- `deploymentStatusWebhook`
- `hubspotToken`

## Rollback and recovery

This deployment path is incremental and ARM/Bicep-based; there is no single custom rollback script in this repository.

Recommended recovery pattern:

1. Use `deployment-artifacts/latest-summary.md` and `deployment-artifacts/latest-status.json` to identify the failed phase.
2. Review the matching per-run logs in `deployment-artifacts/<environment>/<run-id>/`.
3. Fix parameters, images, or secrets.
4. Re-run the affected phase or platform target.
5. If needed, use Azure deployment history from the target resource group to redeploy a prior known-good template input set.

## Verification checks

A healthy run should produce:

- a non-empty deployment summary
- a non-empty deployment status JSON file
- uploaded CI artifacts in GitHub Actions or Azure Pipelines
- verification outputs when `phase=verification` or `phase=all`

For local validation before committing deployment changes, run:

```bash
bash -n ./scripts/deploy/deploy-platform.sh
python3 - <<'PY'
import json
from pathlib import Path
for path in [
    Path('deployment/logicapps/azure-monitor-to-slack.definition.json'),
    Path('deployment/parameters/platform.parameters.example.json'),
    Path('cloud-integration/configs/azure.config.json'),
    Path('cloud-integration/configs/github.config.json'),
]:
    with path.open('r', encoding='utf-8') as handle:
        json.load(handle)
print('JSON validation complete')
PY
az bicep build --file ./deployment/main.bicep >/dev/null
```

## Related files

- `scripts/deploy/deploy-platform.sh`
- `.github/workflows/deploy.yml`
- `azure-pipelines.yml`
- `deployment/main.bicep`
- `deployment/parameters/platform.parameters.example.json`
- `cloud-integration/configs/azure.config.json`
- `cloud-integration/configs/github.config.json`
