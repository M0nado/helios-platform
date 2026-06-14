#!/usr/bin/env bash
set -euo pipefail

if command -v az >/dev/null 2>&1; then
  az version --output json
  exit 0
fi

case "${RUNNER_OS:-$(uname -s)}" in
  Linux|linux*)
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
    ;;
  macOS|Darwin*)
    brew update
    brew install azure-cli
    ;;
  Windows*)
    pwsh -NoProfile -Command "Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'"
    ;;
  *)
    echo "Unsupported OS for automatic Azure CLI installation" >&2
    exit 1
    ;;
esac

az version --output json
