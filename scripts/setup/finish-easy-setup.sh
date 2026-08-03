#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

PROFILE="quick"
SERVE="false"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --profile) PROFILE="${2:-quick}"; shift 2 ;;
    --full) PROFILE="full"; shift ;;
    --serve) SERVE="true"; shift ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

scripts/setup/bootstrap-local-tools.sh
TOOLS_DIR="${HELIOS_TOOLS_DIR:-$ROOT_DIR/.tools}"
export PATH="$TOOLS_DIR/dotnet:$TOOLS_DIR/gh/bin:$TOOLS_DIR/azcli-venv/bin:$PATH"

python3 scripts/apply/finish_readiness_apply.py --apply
python3 scripts/apply/generate_finish_tasks.py
if [[ "$PROFILE" == "full" ]]; then
  python3 scripts/build_graph/build_graph.py run --profile "$PROFILE" --max-workers "${HELIOS_BUILD_GRAPH_WORKERS:-2}"
else
  python3 scripts/build_graph/build_graph.py run --profile "$PROFILE" --changed-only --max-workers "${HELIOS_BUILD_GRAPH_WORKERS:-4}"
fi
python3 scripts/integrations/readiness_score.py
python3 scripts/dashboard/generate-gui.py

cat <<NEXT

HELIOS easy setup complete.

Generated reports:
- reports/apply/finish-readiness-apply.md
- reports/apply/finish-tasks.md
- reports/build-graph/latest.md
- reports/integrations/finish-readiness.md
- status-site/index.html

Manual auth steps still required for live cloud operations:
- gh auth login
- az login
- export HELIOS_AZURE_RESOURCE_GROUP=helios-dev-rg
- python3 scripts/azure/azure_what_if.py
NEXT

if [[ "$SERVE" == "true" ]]; then
  exec python3 scripts/web/helios-web.py --no-rebuild
fi
