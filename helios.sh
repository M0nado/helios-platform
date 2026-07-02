#!/usr/bin/env bash
set -euo pipefail
cmd="${1:-help}"
shift || true
case "$cmd" in
  setup) exec scripts/setup/helios-dev.sh "$@" ;;
  dashboard|serve) exec scripts/setup/helios-dev.sh --serve "$@" ;;
  status) exec python3 scripts/control/helios-control.py "$@" ;;
  github) exec python3 scripts/github/github-inventory.py "$@" ;;
  azure) exec python3 scripts/azure/azure-inventory.py "$@" ;;
  ai) exec python3 scripts/ai/enrich-ideas.py "$@" ;;
  branches) exec python3 scripts/analysis/branch_intelligence.py "$@" ;;
  build) exec python3 scripts/build/build-graph.py "$@" ;;
  codex) exec python3 scripts/codex/generate-codex-tasks.py "$@" ;;
  recommendations) exec python3 scripts/analysis/merge_prune_recommendations.py "$@" ;;
  actions) exec python3 scripts/dashboard/generate-actions.py "$@" ;;
  all)
    python3 scripts/integrations/check-connections.py
    python3 scripts/github/github-inventory.py
    python3 scripts/azure/azure-inventory.py
    python3 scripts/control/helios-control.py
    python3 scripts/analysis/branch_intelligence.py
    python3 scripts/analysis/merge_prune_recommendations.py
    python3 scripts/graphs/generate_graphs.py
    python3 scripts/build/build-graph.py
    python3 scripts/codex/generate-codex-tasks.py
    python3 scripts/dashboard/generate-actions.py
    python3 scripts/ai/enrich-ideas.py
    ;;
  help|*)
    cat <<'EOF'
HELIOS Command Center

Usage: ./helios.sh <command>

Commands:
  setup             Bootstrap local tools and reports
  dashboard|serve   Start local dashboard
  status            GitHub/Azure/AI/Codex control summary
  github            GitHub inventory report
  azure             Azure inventory report
  ai                Safe AI enrichment readiness
  branches          Branch intelligence reports
  build             Build graph report or runner
  codex             Generate Codex task packets
  recommendations   Branch merge/prune recommendations
  actions           Dashboard actions page
  all               Run the safe read-only/report generation pipeline
EOF
    ;;
esac
