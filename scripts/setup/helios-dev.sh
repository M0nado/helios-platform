#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

SERVE_DASHBOARD="false"
SERVE_STALE="false"
PROFILE="quick"
BUILD_GRAPH_ARGS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --serve|serve) SERVE_DASHBOARD="true"; shift ;;
    --serve-stale) SERVE_DASHBOARD="true"; SERVE_STALE="true"; shift ;;
    --profile) PROFILE="${2:-quick}"; shift 2 ;;
    --full) PROFILE="full"; shift ;;
    --changed-only) BUILD_GRAPH_ARGS+=("--changed-only"); shift ;;
    --tag) BUILD_GRAPH_ARGS+=("--tag" "${2:?missing tag}"); shift 2 ;;
    --max-workers) BUILD_GRAPH_ARGS+=("--max-workers" "${2:?missing worker count}"); shift 2 ;;
    *) BUILD_GRAPH_ARGS+=("$1"); shift ;;
  esac
done

scripts/setup/bootstrap-local-tools.sh
TOOLS_DIR="${HELIOS_TOOLS_DIR:-$ROOT_DIR/.tools}"
export PATH="$TOOLS_DIR/dotnet:$TOOLS_DIR/gh/bin:$TOOLS_DIR/azcli-venv/bin:$PATH"
mkdir -p reports/local-setup

{
  echo "# HELIOS Dev Setup Summary"
  echo
  echo "Generated: $(date -u +'%Y-%m-%dT%H:%M:%SZ')"
  echo
  echo "Profile: \`$PROFILE\`"
  echo
  echo "## Tool status"
  for tool in git gh az dotnet python3 cmake; do
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
  echo "## Build graph command"
  echo '```bash'
  printf 'python3 scripts/build_graph/build_graph.py run --profile %q' "$PROFILE"
  printf ' %q' "${BUILD_GRAPH_ARGS[@]}"
  echo
  echo '```'
} | tee reports/local-setup/helios-dev-summary.md

if python3 scripts/build_graph/build_graph.py run --profile "$PROFILE" "${BUILD_GRAPH_ARGS[@]}"; then
  BUILD_GRAPH_OK="true"
else
  BUILD_GRAPH_OK="false"
fi

if [[ "$SERVE_DASHBOARD" == "true" ]]; then
  if [[ "$BUILD_GRAPH_OK" == "true" || "$SERVE_STALE" == "true" ]]; then
    exec python3 scripts/web/helios-web.py --no-rebuild
  fi
  echo "Dashboard generation failed; rerun with --serve-stale to serve the previous dashboard." >&2
  exit 1
fi

[[ "$BUILD_GRAPH_OK" == "true" ]]
