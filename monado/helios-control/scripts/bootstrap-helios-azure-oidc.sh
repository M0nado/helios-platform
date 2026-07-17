#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

# Preview-only legacy shim. Any --apply request exits before authentication or mutation.
APPLY=false
if [[ "${1:-}" == "--apply" ]]; then APPLY=true; fi

if [[ "$APPLY" == true ]]; then
  echo 'The legacy mutation path is retired because it cannot verify GitHub environment protection or immutable OIDC subjects.' >&2
  echo 'Use: pwsh ./scripts/Connect-HeliosAzureInteractive.ps1 -Mode Configure' >&2
  exit 2
fi

: "${AZ_SUBSCRIPTION_ID:?Set AZ_SUBSCRIPTION_ID}"
: "${AZ_RESOURCE_GROUP:=helios-rg}"
: "${AZ_LOCATION:=eastus2}"
: "${HELIOS_ENVIRONMENT:=dev}"
: "${HELIOS_CONTAINER_IMAGE:=heliosplaceholderacr.azurecr.io/helios-connect@sha256:0000000000000000000000000000000000000000000000000000000000000000}"
: "${GITHUB_ORG:=M0nado}"
: "${GITHUB_REPO:=helios-platform}"
: "${GITHUB_ENVIRONMENT:=azure-dev}"
: "${HELIOS_CONNECTOR_APP_NAME:=helios-azure-connector}"
: "${HELIOS_GITHUB_APP_NAME:=helios-github-oidc}"

for command_name in az jq; do
  command -v "$command_name" >/dev/null || { echo "Missing required command: $command_name" >&2; exit 2; }
done

if [[ ! "$HELIOS_CONTAINER_IMAGE" =~ ^([a-zA-Z0-9][a-zA-Z0-9-]{4,49})\.azurecr\.io/.+@sha256:[0-9a-fA-F]{64}$ ]]; then
  echo 'HELIOS_CONTAINER_IMAGE must be an immutable Azure Container Registry reference.' >&2
  exit 2
fi
HELIOS_CONTAINER_REGISTRY_NAME="${HELIOS_CONTAINER_REGISTRY_NAME:-${BASH_REMATCH[1]}}"

az account show --output none 2>/dev/null || {
  echo 'Authenticate first with: az login --use-device-code' >&2
  exit 3
}
az account set --subscription "$AZ_SUBSCRIPTION_ID"
AZ_TENANT_ID="$(az account show --query tenantId --output tsv)"
SIGNED_IN_OBJECT_ID="${HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID:-$(az ad signed-in-user show --query id --output tsv)}"
RG_SCOPE="/subscriptions/${AZ_SUBSCRIPTION_ID}/resourceGroups/${AZ_RESOURCE_GROUP}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEMPLATE="${ROOT_DIR}/infra/connector.bicep"

echo 'Helios Azure connector plan'
echo "  subscription: ${AZ_SUBSCRIPTION_ID}"
echo "  resource group: ${AZ_RESOURCE_GROUP} (${AZ_LOCATION})"
echo "  GitHub trust: repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENVIRONMENT}"
echo '  connector access: Entra allowlist + resource-group Reader managed identity'
echo '  Key Vault: RBAC enabled; no secret values inserted by this script'

if [[ "$APPLY" != true ]]; then
  if [[ "$(az group exists --name "$AZ_RESOURCE_GROUP")" == 'true' ]] \
      && [[ -n "${HELIOS_ENTRA_CLIENT_ID:-}" ]]; then
    az deployment group what-if \
      --resource-group "$AZ_RESOURCE_GROUP" \
      --template-file "$TEMPLATE" \
      --parameters \
        environmentName="$HELIOS_ENVIRONMENT" \
        containerImage="$HELIOS_CONTAINER_IMAGE" \
        containerRegistryName="$HELIOS_CONTAINER_REGISTRY_NAME" \
        allowPreviewPlaceholder=true \
        entraClientId="$HELIOS_ENTRA_CLIENT_ID" \
        entraTenantId="$AZ_TENANT_ID" \
        allowedPrincipalObjectId="$SIGNED_IN_OBJECT_ID"
  else
    echo 'Preview only: create/select the resource group and set HELIOS_ENTRA_CLIENT_ID to run ARM what-if.'
  fi
  echo 'No Azure or Entra resources were changed. After review, use Connect-HeliosAzureInteractive.ps1 -Mode Configure; legacy --apply is retired.'
  exit 0
fi
