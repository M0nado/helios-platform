# Deployment Workflow - deploy.yml

## Overview

`.github/workflows/deploy.yml` is the GitHub Actions entrypoint for HELIOS Azure deployments. It wraps the shared script at `scripts/deploy/deploy-platform.sh`, validates deployment assets before execution, authenticates to Azure with OIDC, and uploads both deployment inputs and run artifacts.

## Triggers

The workflow runs on:

- pushes to `main` that touch deployment-related files
- manual `workflow_dispatch`

Tracked push paths:

- `.github/workflows/deploy.yml`
- `deployment/**`
- `scripts/deploy/**`
- `cloud-integration/configs/**`

## Permissions and concurrency

The workflow uses these permissions:

```yaml
permissions:
  id-token: write
  contents: read
```

Concurrency is grouped by environment so overlapping deploys for the same environment do not race:

```yaml
concurrency:
  group: helios-deploy-${{ github.event.inputs.environment || 'prod' }}
  cancel-in-progress: false
```

## Manual inputs

`workflow_dispatch` exposes these inputs:

- `phase`
- `target`
- `environment`
- `location`
- `resource_group`
- `base_name`
- `deploy_observability`
- `parameters_file`
- `what_if`

Valid `phase` values:

- `preflight`
- `infrastructure`
- `container-apps`
- `aks`
- `integrations`
- `verification`
- `all`

Valid `target` values:

- `aca`
- `aks`
- `both`

## Authentication model

The workflow uses `azure/login@v2` with repository OIDC federation.

Required secrets:

- `AZURE_SUBSCRIPTION_ID`
- `AZURE_TENANT_ID`
- `AZURE_CLIENT_ID`

Optional secrets for outbound posting:

- `SLACK_WEBHOOK_URL`
- `DEPLOYMENT_STATUS_WEBHOOK`

This deploy workflow does not use a JSON `AZURE_CREDENTIALS` secret.

## Environment variables passed into the script

The workflow maps GitHub inputs, variables, and secrets into the shared deploy script environment.

Important values:

- `RESOURCE_GROUP`
- `LOCATION`
- `ENVIRONMENT_NAME`
- `BASE_NAME`
- `DEPLOY_OBSERVABILITY`
- `PARAMETERS_FILE`
- `DEPLOYMENT_OUTPUT_DIR`
- `RUN_ID`
- `RUN_URL`
- `SOURCE_REF`
- `COMMIT_SHA`
- `SLACK_CHANNEL`
- `CONTROL_PLANE_IMAGE`
- `HUBSPOT_SYNC_IMAGE`
- `AKS_IMAGE`
- `SLACK_WEBHOOK_URL`
- `DEPLOYMENT_STATUS_WEBHOOK`

## Workflow steps

The deploy job performs the following sequence:

1. Check out the repository.
2. Install Azure CLI through `azure/setup-azure-cli@v2`.
3. Authenticate to Azure using OIDC.
4. Validate the shared deploy script syntax.
5. Validate key JSON configuration files.
6. Build `deployment/main.bicep` to catch template errors early.
7. Ensure `scripts/deploy/deploy-platform.sh` is executable.
8. Execute the shared deployment script.
9. Upload deployment assets.
10. Upload deployment run artifacts.

## Validation gate

Before the workflow deploys, it checks:

- `scripts/deploy/deploy-platform.sh` with `bash -n`
- `deployment/logicapps/azure-monitor-to-slack.definition.json`
- `deployment/parameters/platform.parameters.example.json`
- `cloud-integration/configs/azure.config.json`
- `cloud-integration/configs/github.config.json`
- `deployment/main.bicep` with `az bicep build`

## Deployment reporting and posting

The shared script writes deployment artifacts to `deployment-artifacts/` and the workflow uploads that directory as the `helios-deployment-run` artifact.

The script also writes:

- `latest-summary.md`
- `latest-status.json`
- `latest-run-dir.txt`

When webhook secrets are configured, the workflow execution triggers start and finish notifications through the shared script.

Notification payloads include:

- status
- phase
- target
- environment
- resource group
- location
- run ID
- run URL
- source ref
- commit SHA
- current phase

## Artifact outputs

The workflow uploads two artifacts on every run:

- `helios-deployment-assets`
- `helios-deployment-run`

`helios-deployment-assets` captures the deploy template, config, and script inputs.

`helios-deployment-run` captures the generated logs, summary, status JSON, and verification outputs.

## Troubleshooting

### OIDC login fails

Check:

- the repository or environment is configured for Azure workload identity federation
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` match the federated app registration
- the workflow still has `id-token: write`

### Validation step fails

Check the file called out in the validation output:

- shell syntax failures come from `scripts/deploy/deploy-platform.sh`
- JSON parse failures come from one of the validated config files
- Bicep build failures come from `deployment/main.bicep` or one of its modules

### Deployment runs but no notifications arrive

Check:

- `SLACK_WEBHOOK_URL` and `DEPLOYMENT_STATUS_WEBHOOK` are configured
- the endpoint accepts JSON `POST` requests
- the generated `deployment-summary.md` shows the run actually reached the notification stage

### Deployment artifact missing

Check whether the run failed before `deployment-artifacts/` was created. The workflow uses `if: always()` for uploads, but the directory still has to exist for files to be present.

## Related files

- `scripts/deploy/deploy-platform.sh`
- `docs/DEPLOYMENT.md`
- `azure-pipelines.yml`
- `deployment/main.bicep`
- `deployment/parameters/platform.parameters.example.json`
- `cloud-integration/configs/azure.config.json`
- `cloud-integration/configs/github.config.json`
