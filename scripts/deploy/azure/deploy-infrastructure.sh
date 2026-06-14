#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="${HELIOS_RESOURCE_GROUP:-helios-platform-rg}"
LOCATION="${HELIOS_LOCATION:-eastus}"
TEMPLATE_FILE="${HELIOS_BICEP_TEMPLATE:-infra/main.bicep}"
DRY_RUN="${HELIOS_AZURE_DRY_RUN:-false}"

if [[ "$DRY_RUN" == "true" ]]; then
  echo "Azure infrastructure deployment dry-run enabled; skipping resource group and deployment writes."
  echo "Would create or update resource group: $RESOURCE_GROUP in $LOCATION"
  if [[ -f "$TEMPLATE_FILE" ]]; then
    echo "Would deploy Bicep template: $TEMPLATE_FILE"
  else
    echo "No Bicep template found at $TEMPLATE_FILE; dry-run validation completed."
  fi
  exit 0
fi

az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output json
if [[ -f "$TEMPLATE_FILE" ]]; then
  az deployment group create --resource-group "$RESOURCE_GROUP" --template-file "$TEMPLATE_FILE" --output json
else
  echo "No Bicep template found at $TEMPLATE_FILE; resource group validation completed."
fi
