#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="${HELIOS_RESOURCE_GROUP:-helios-platform-rg}"
LOCATION="${HELIOS_LOCATION:-eastus}"
TEMPLATE_FILE="${HELIOS_BICEP_TEMPLATE:-infra/main.bicep}"

az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output json
if [[ -f "$TEMPLATE_FILE" ]]; then
  az deployment group create --resource-group "$RESOURCE_GROUP" --template-file "$TEMPLATE_FILE" --output json
else
  echo "No Bicep template found at $TEMPLATE_FILE; resource group validation completed."
fi
