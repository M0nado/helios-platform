# HELIOS Azure Infrastructure

Bicep templates for dashboard/report storage and observability.

## Validate locally

Install Azure CLI first. On Ubuntu containers you can run Microsoft's Debian installer or use `scripts/setup/bootstrap-local-tools.sh` when you need a repo-local, non-root toolchain. GitHub-hosted `ubuntu-latest` runners include Azure CLI; the CI workflow still prints `az --version` and `az bicep version` before building templates so runner-image drift is visible.

```bash
az --version
az bicep version
az bicep build --file infra/azure/main.bicep
```

For deployment validation and what-if reports, authenticate and provide a target resource group. Without credentials or a resource group, the helper still exits successfully after an offline Bicep build and records `mode: offline-bicep-build`; CI uses `--strict-online` so missing Azure configuration fails protected deployment validation.

```bash
az login
export HELIOS_AZURE_RESOURCE_GROUP=<resource-group>
export HELIOS_AZURE_LOCATION=eastus
python3 scripts/azure/bicep_report.py validate
python3 scripts/azure/bicep_report.py what-if
```

Use strict mode when an online Azure validation is required:

```bash
python3 scripts/azure/bicep_report.py validate --strict-online
python3 scripts/azure/bicep_report.py what-if --strict-online
```

`bicep_report.py` emits a small JSON summary for local logs and CI artifacts. Deployment should be gated through `.github/workflows/azure-infra.yml`.
