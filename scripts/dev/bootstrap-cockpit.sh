#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
NODE_TOOLS_DIR="${REPO_ROOT}/.devcontainer"
HELIOS_NPM_CACHE="${NODE_TOOLS_DIR}/.cache/npm"
HELIOS_DOTNET_BUNDLE_DIR="${NODE_TOOLS_DIR}/.cache/dotnet-bundle"

mkdir -p \
  "${HELIOS_NPM_CACHE}" \
  "${HELIOS_DOTNET_BUNDLE_DIR}"
export DOTNET_BUNDLE_EXTRACT_BASE_DIR="${HELIOS_DOTNET_BUNDLE_DIR}"

echo "Installing locked user-space cockpit tools"
npm ci \
  --prefix "${NODE_TOOLS_DIR}" \
  --cache "${HELIOS_NPM_CACHE}" \
  --no-audit \
  --no-fund

export PATH="${NODE_TOOLS_DIR}/node_modules/.bin:${PATH}"

echo "Validating the HELIOS developer cockpit"
python3 "${REPO_ROOT}/scripts/dev/helios_dev_doctor.py" \
  --profile devcontainer

echo
echo "Cockpit ready. Authentication remains interactive and provider-specific."
echo "Use 'gh auth login', 'az login', 'claude', or the VS Code MCP trust UI only when needed."
