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
  inventory) exec python3 scripts/analysis/repo_inventory.py "$@" ;;
  gaps) exec python3 scripts/analysis/hybrid_gap_analysis.py "$@" ;;
  prune-generated) exec python3 scripts/analysis/prune_generated_artifacts.py "$@" ;;
  branches) exec python3 scripts/analysis/branch_intelligence.py "$@" ;;
  build) exec python3 scripts/build_graph/build_graph.py "$@" ;;
  verify) exec python3 scripts/build_graph/build_graph.py run --changed-only --include-readiness "$@" ;;
  codex) exec python3 scripts/codex/generate-codex-tasks.py "$@" ;;
  recommendations) exec python3 scripts/analysis/merge_prune_recommendations.py "$@" ;;
  profiles) exec python3 scripts/integrations/cross_access_profiles.py "$@" ;;
  readiness) exec python3 scripts/integrations/readiness_score.py "$@" ;;
  apps) exec python3 scripts/integrations/app_automation.py "$@" ;;
  local-app) exec scripts/local-app/helios-local-app.sh "$@" ;;
  doctor) exec python3 scripts/control/doctor.py "$@" ;;
  validate) exec python3 scripts/control/validate_workflows.py "$@" ;;
  pr-update) exec python3 scripts/github/update-pr-from-reports.py "$@" ;;
  gui)
    python3 scripts/control/doctor.py
    python3 scripts/github/update-pr-from-reports.py --dry-run >/tmp/helios-pr-update-preview.md
    exec python3 scripts/dashboard/generate-gui.py "$@"
    ;;
  actions)
    python3 scripts/dashboard/generate-actions.py "$@"
    exec python3 scripts/dashboard/generate-gui.py "$@"
    ;;
  all)
    # Execution order is defined in config/execution-order.json.
    printf '1/15 HELIOS Command Center single entry point\n'
    python3 scripts/control/helios-control.py
    python3 scripts/analysis/repo_inventory.py
    python3 scripts/analysis/hybrid_gap_analysis.py
    printf '2/15 Unified secrets map\n'
    python3 scripts/integrations/check-connections.py
    python3 scripts/integrations/cross_access_profiles.py
    python3 scripts/integrations/readiness_score.py
    python3 scripts/integrations/app_automation.py
    python3 scripts/control/validate_workflows.py
    printf '3/15 GitHub inventory\n'
    python3 scripts/github/github-inventory.py
    printf '4/15 Azure inventory\n'
    python3 scripts/azure/azure-inventory.py
    printf '5/15 Control-plane permissions model\n'
    test -f docs/security/CONTROL_PLANE_PERMISSIONS.md
    printf '6/15 Build graph\n'
    python3 scripts/build_graph/build_graph.py
    printf '7/15 Branch merge/prune recommendations\n'
    python3 scripts/analysis/branch_intelligence.py
    python3 scripts/analysis/merge_prune_recommendations.py
    printf '8/15 Dashboard actions page\n'
    python3 scripts/dashboard/generate-actions.py
    python3 scripts/control/doctor.py
    python3 scripts/github/update-pr-from-reports.py --dry-run >/tmp/helios-pr-update-preview.md
    python3 scripts/dashboard/generate-gui.py
    printf '9/15 Codex task packet generation\n'
    python3 scripts/codex/generate-codex-tasks.py
    printf '10/15 Opt-in AI enrichment\n'
    python3 scripts/ai/enrich-ideas.py
    printf '11/15 GitHub desired-state manifest\n'
    test -f config/github-control.example.json
    printf '12/15 Azure desired-state manifest\n'
    test -f config/azure-control.example.json
    printf '13/15 Hosted GitHub workflow expansion\n'
    test -f .github/workflows/helios-control-plane.yml
    printf '14/15 Cloud Shell full setup\n'
    test -f scripts/cloudshell/helios-cloudshell.sh
    printf '15/15 Long-term org/enterprise apply-mode controls\n'
    test -f docs/security/CONTROL_PLANE_PERMISSIONS.md
    python3 scripts/graphs/generate_graphs.py
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
  inventory         Whole-project inventory
  gaps              Hybrid integration gap analysis
  prune-generated   Delete generated report/dashboard artifacts from the working tree
  branches          Branch intelligence reports
  build             Build graph report or runner
  verify            Run changed build-graph nodes plus full-stack readiness
  codex             Generate Codex task packets
  recommendations   Branch merge/prune recommendations
  profiles          Cross-access profile readiness
  readiness         Local/repo readiness score
  apps              App automation readiness for GitHub MCP, Slack, Linear, Copilot, ChatGPT, Claude, Azure
  local-app         Open the combined local app shell for HELIOS GUI, Copilot, ChatGPT, Claude, GitHub, Azure, Slack, Linear
  doctor            Setup doctor and fix hints
  validate          Validate GitHub workflow structure without requiring PyYAML
  pr-update         Generate or apply PR body from reports
  gui               Generate HTML GUI dashboard
  actions           Dashboard actions page
  all               Run the safe read-only/report generation pipeline
EOF
    ;;
esac
