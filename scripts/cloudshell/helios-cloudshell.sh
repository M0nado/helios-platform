#!/usr/bin/env bash
set -euo pipefail

# Azure Cloud Shell / Codespaces bootstrap for HELIOS control-plane operations.
# Assumes git, gh, az, python3 and dotnet are available or installable in the host image.

ROOT_DIR="${HELIOS_ROOT:-$(pwd)}"
cd "$ROOT_DIR"

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI is required. In Cloud Shell, install gh or use a Codespace image with gh preinstalled." >&2
  exit 1
fi
if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI is required. Azure Cloud Shell includes az by default." >&2
  exit 1
fi

mkdir -p reports/cloudshell
{
  echo "# HELIOS Cloud Shell Readiness"
  echo
  echo "Generated: $(date -u +'%Y-%m-%dT%H:%M:%SZ')"
  echo
  gh auth status >/tmp/helios-cloudshell-gh.txt 2>&1 && echo "- ✅ GitHub authenticated" || echo "- ⚠️ Run: gh auth login"
  az account show >/tmp/helios-cloudshell-az.json 2>&1 && echo "- ✅ Azure authenticated" || echo "- ⚠️ Run: az login"
  echo "- Python: $(python3 --version 2>&1)"
  command -v dotnet >/dev/null 2>&1 && echo "- .NET: $(dotnet --version)" || echo "- ⚠️ dotnet not found"
  echo
  echo "## Suggested next steps"
  echo '```bash'
  echo 'python3 scripts/integrations/check-connections.py'
  echo 'python3 scripts/analysis/branch_intelligence.py --fetch'
  echo 'python3 scripts/graphs/generate_graphs.py'
  echo 'az bicep build --file infra/azure/main.bicep'
  echo '```'
} | tee reports/cloudshell/readiness.md

python3 scripts/integrations/check-connections.py
python3 scripts/control/helios-control.py
python3 scripts/github/github-inventory.py
python3 scripts/azure/azure-inventory.py
python3 scripts/analysis/branch_intelligence.py
python3 scripts/analysis/merge_prune_recommendations.py
python3 scripts/build_graph/build_graph.py
python3 scripts/codex/generate-codex-tasks.py
python3 scripts/dashboard/generate-actions.py
python3 scripts/graphs/generate_graphs.py
