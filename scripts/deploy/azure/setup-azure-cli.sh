#!/usr/bin/env bash
set -euo pipefail

if command -v az >/dev/null 2>&1; then
  az version --output json
  exit 0
fi

case "$(uname -s)" in
  Linux)
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
    ;;
  Darwin)
    brew update
    brew install azure-cli
    ;;
  *)
    echo "Azure CLI auto-install is supported on Linux/macOS runners. Install manually for this OS." >&2
    exit 2
    ;;
esac

az version --output json
