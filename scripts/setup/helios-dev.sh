#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

scripts/setup/bootstrap-local-tools.sh
TOOLS_DIR="${HELIOS_TOOLS_DIR:-$ROOT_DIR/.tools}"
export PATH="$TOOLS_DIR/dotnet:$TOOLS_DIR/gh/bin:$TOOLS_DIR/azcli-venv/bin:$PATH"
mkdir -p reports/local-setup

{
  echo "# HELIOS Dev Setup Summary"
  echo
  echo "Generated: $(date -u +'%Y-%m-%dT%H:%M:%SZ')"
  echo
  echo "## Tool status"
  for tool in git gh az dotnet python3; do
    if command -v "$tool" >/dev/null 2>&1; then
      echo "- ✅ $tool: $($tool --version 2>&1 | head -1)"
    else
      echo "- ❌ $tool: not found"
    fi
  done
  echo
  echo "## Authentication"
  gh auth status >/tmp/helios-gh-auth.txt 2>&1 && echo "- ✅ GitHub CLI authenticated" || echo "- ⚠️ GitHub CLI needs: gh auth login"
  az account show >/tmp/helios-az-auth.json 2>&1 && echo "- ✅ Azure CLI authenticated" || echo "- ⚠️ Azure CLI needs: az login"
  echo
  echo "## Next commands"
  echo '```bash'
  echo 'python3 scripts/analysis/branch_intelligence.py'
  echo 'dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'
  echo '```'
} | tee reports/local-setup/helios-dev-summary.md

python3 scripts/analysis/branch_intelligence.py
