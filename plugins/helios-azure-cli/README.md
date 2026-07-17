# HELIOS Azure CLI plugin

An easy, guarded wrapper around Azure CLI for the canonical `M0nado/helios-platform` Azure development environment.

## Fast path

```bash
python3 scripts/helios_azure.py doctor
python3 scripts/helios_azure.py login --device-code
python3 scripts/helios_azure.py plan
python3 scripts/helios_azure.py what-if --template path/to/main.bicep
```

On Windows PowerShell:

```powershell
.\scripts\helios-azure.ps1 doctor
```

Nothing deploys by default. OIDC creation requires `--execute --confirm CONFIGURE-AZURE-DEV-OIDC`. Bicep deployment requires `--execute --confirm DEPLOY-AZURE-DEV`. Both mutating commands also refuse to continue until GitHub PRs #174 and #176 are merged.

The plugin never creates or stores a client secret. It uses interactive Azure CLI authentication for local administration and the exact GitHub environment federation subject for automation.

## Official references

- [Install Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Sign in interactively](https://learn.microsoft.com/en-us/cli/azure/authenticate-azure-cli-interactively)
- [Configure a federated identity credential](https://learn.microsoft.com/en-us/entra/workload-id/workload-identity-federation-create-trust)
- [Preview Bicep changes with what-if](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deploy-what-if)
