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

[[ "${HELIOS_CONFIRM_APPLY:-}" == 'YES' ]] || {
  echo 'Refusing mutation. Set HELIOS_CONFIRM_APPLY=YES and re-run with --apply.' >&2
  exit 4
}


if [[ "$HELIOS_CONTAINER_IMAGE" == *@sha256:0000000000000000000000000000000000000000000000000000000000000000 ]]; then
  echo 'Replace HELIOS_CONTAINER_IMAGE with an approved immutable image reference before applying.' >&2
  exit 4
fi

az group create --name "$AZ_RESOURCE_GROUP" --location "$AZ_LOCATION" --output none

ensure_app() {
  local display_name="$1"
  local app_id
  app_id="$(az ad app list --display-name "$display_name" --query '[0].appId' --output tsv)"
  if [[ -z "$app_id" ]]; then
    app_id="$(az ad app create --display-name "$display_name" --sign-in-audience AzureADMyOrg --query appId --output tsv)"
  fi
  if [[ -z "$(az ad sp list --filter "appId eq '$app_id'" --query '[0].id' --output tsv)" ]]; then
    az ad sp create --id "$app_id" --output none
  fi
  printf '%s' "$app_id"
}

CONNECTOR_APP_ID="$(ensure_app "$HELIOS_CONNECTOR_APP_NAME")"
CONNECTOR_OBJECT_ID="$(az ad app list --filter "appId eq '$CONNECTOR_APP_ID'" --query '[0].id' --output tsv)"
if [[ "$(az ad app show --id "$CONNECTOR_OBJECT_ID" --query 'length(api.oauth2PermissionScopes)' --output tsv)" == '0' ]]; then
  SCOPE_ID="$(cat /proc/sys/kernel/random/uuid)"
  SCOPE_JSON="$(jq -nc --arg id "$SCOPE_ID" --arg admin "$SIGNED_IN_OBJECT_ID" '[{id:$id,adminConsentDescription:"Allow Helios clients to read governed Azure inventory.",adminConsentDisplayName:"Read Helios Azure inventory",isEnabled:true,type:"User",userConsentDescription:"Allow this client to read governed Helios Azure inventory.",userConsentDisplayName:"Read Helios Azure inventory",value:"user_impersonation"}]')"
  az ad app update --id "$CONNECTOR_OBJECT_ID" --identifier-uris "api://${CONNECTOR_APP_ID}" --set "api.oauth2PermissionScopes=${SCOPE_JSON}"
fi

GITHUB_APP_ID="$(ensure_app "$HELIOS_GITHUB_APP_NAME")"
GITHUB_OBJECT_ID="$(az ad app list --filter "appId eq '$GITHUB_APP_ID'" --query '[0].id' --output tsv)"
FEDERATED_NAME="github-${GITHUB_ENVIRONMENT}"
if [[ -z "$(az ad app federated-credential list --id "$GITHUB_OBJECT_ID" --query "[?name=='${FEDERATED_NAME}'].name | [0]" --output tsv)" ]]; then
  FEDERATED_FILE="$(mktemp)"
  trap 'rm -f "${FEDERATED_FILE:-}"' EXIT
  jq -n \
    --arg name "$FEDERATED_NAME" \
    --arg subject "repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENVIRONMENT}" \
    '{name:$name,issuer:"https://token.actions.githubusercontent.com",subject:$subject,audiences:["api://AzureADTokenExchange"]}' \
    > "$FEDERATED_FILE"
  az ad app federated-credential create --id "$GITHUB_OBJECT_ID" --parameters "@$FEDERATED_FILE" --output none
fi

for role_id in \
  'b24988ac-6180-42a0-ab88-20f7382dd24c'; do
  if [[ "$(az role assignment list --assignee "$GITHUB_APP_ID" --scope "$RG_SCOPE" --role "$role_id" --query 'length(@)' --output tsv)" == '0' ]]; then
    az role assignment create --assignee "$GITHUB_APP_ID" --role "$role_id" --scope "$RG_SCOPE" --output none
  fi
done

az deployment group what-if \
  --resource-group "$AZ_RESOURCE_GROUP" \
  --template-file "$TEMPLATE" \
  --parameters \
    environmentName="$HELIOS_ENVIRONMENT" \
    containerImage="$HELIOS_CONTAINER_IMAGE" \
    containerRegistryName="$HELIOS_CONTAINER_REGISTRY_NAME" \
    entraClientId="$CONNECTOR_APP_ID" \
    entraTenantId="$AZ_TENANT_ID" \
    allowedPrincipalObjectId="$SIGNED_IN_OBJECT_ID"

DEPLOYMENT_JSON="$(az deployment group create \
  --name "helios-connector-${HELIOS_ENVIRONMENT}" \
  --resource-group "$AZ_RESOURCE_GROUP" \
  --template-file "$TEMPLATE" \
  --parameters \
    environmentName="$HELIOS_ENVIRONMENT" \
    containerImage="$HELIOS_CONTAINER_IMAGE" \
    containerRegistryName="$HELIOS_CONTAINER_REGISTRY_NAME" \
    entraClientId="$CONNECTOR_APP_ID" \
    entraTenantId="$AZ_TENANT_ID" \
    allowedPrincipalObjectId="$SIGNED_IN_OBJECT_ID" \
  --output json)"

CONNECTOR_URL="$(jq -r '.properties.outputs.connectorUrl.value' <<<"$DEPLOYMENT_JSON")"
echo 'Helios connector deployed in dry-run/read-only mode.'
echo "Connector URL: ${CONNECTOR_URL}"
echo 'Set these as protected GitHub environment variables (identifiers, not API keys):'
echo "AZURE_CLIENT_ID=${GITHUB_APP_ID}"
echo "AZURE_TENANT_ID=${AZ_TENANT_ID}"
echo "AZURE_SUBSCRIPTION_ID=${AZ_SUBSCRIPTION_ID}"
echo "AZURE_RESOURCE_GROUP=${AZ_RESOURCE_GROUP}"
echo "HELIOS_ENTRA_CLIENT_ID=${CONNECTOR_APP_ID}"
echo "HELIOS_ALLOWED_PRINCIPAL_OBJECT_ID=${SIGNED_IN_OBJECT_ID}"
echo 'No OpenAI or Azure OpenAI key was created, retrieved, printed, or stored.'
