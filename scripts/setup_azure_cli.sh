#!/usr/bin/env bash
# Idempotent Azure CLI bootstrap for HELIOS/HERMES Linux or WSL operators.
set -euo pipefail

EXTENSIONS=(azure-devops ml ssh resource-graph)

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI is not installed. Install it from https://aka.ms/InstallAzureCLIDeb before rerunning." >&2
  exit 1
fi

az version --output table
for extension in "${EXTENSIONS[@]}"; do
  if az extension show --name "$extension" >/dev/null 2>&1; then
    echo "✓ Azure CLI extension already installed: $extension"
  else
    echo "Installing Azure CLI extension: $extension"
    az extension add --name "$extension" --only-show-errors
  fi
done

if az account show >/dev/null 2>&1; then
  az account show --query "{name:name, tenantId:tenantId, user:user.name}" --output table
else
  echo "Azure CLI is ready. Run 'az login' or 'az login --use-device-code' to authenticate."
fi
