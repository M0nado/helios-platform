#!/usr/bin/env bash
set -euo pipefail

# HELIOS Azure CLI bootstrap helper.
# Safe to run repeatedly on Debian/Ubuntu CI or deployment hosts.

if command -v az >/dev/null 2>&1; then
  az --version | head -n 1
  exit 0
fi

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required to install Azure CLI" >&2
  exit 1
fi

curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
az --version | head -n 1
cat <<'MSG'
Azure CLI installed. Next deployment steps:
  az login
  az account set --subscription <subscription-id>
MSG
