#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
export PATH="$ROOT/.tools/dotnet:$ROOT/.tools/gh/bin:$ROOT/.tools/azcli-venv/bin:$PATH"
profile="quick"
agents="0"
mode="auto"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --profile) profile="${2:-quick}"; shift 2 ;;
    --agents) agents="${2:-0}"; shift 2 ;;
    --mode) mode="${2:-auto}"; shift 2 ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done
scripts/setup/bootstrap-local-tools.sh
python3 scripts/agents/agent_fleet_control_catalog.py --agents "$agents" --mode "$mode"
python3 scripts/agents/branch_test_autofix_plan.py
python3 scripts/agents/branch_fix_agents.py --max-branches 88
python3 scripts/agents/agent_fleet_autopilot.py --agents "$agents" --mode "$mode"
python3 scripts/integrations/aihub_super_shell.py
python3 scripts/integrations/aihub_connectivity_guide.py
python3 scripts/analysis/legacy_algorithm_recovery.py
python3 scripts/integrations/aihub_live_flags.py
python3 scripts/integrations/aihub_learning_rules.py
python3 scripts/integrations/aihub_full_framework.py
python3 scripts/integrations/aihub_integration_graph.py
python3 scripts/integrations/aihub_unified_control_plane.py
python3 scripts/integrations/aihub_supershell_vault_wizard.py
python3 scripts/analysis/aihub_module_blueprint.py
python3 scripts/analysis/aihub_language_skill_profiles.py
python3 scripts/analysis/simple_build_center.py
python3 scripts/analysis/branch_absorption_multicloud_plan.py
python3 scripts/build_graph/build_graph.py run --profile "$profile" --max-workers 2
python3 scripts/dashboard/generate-gui.py
cat <<MSG
Agent runner easy setup complete.
Reports:
- reports/branch-agents/agent-fleet-control-catalog.md
- reports/branch-agents/branch-fix-agents.md
- reports/branch-agents/branch-test-autofix-plan.md
- reports/branch-agents/agent-fleet-autopilot.md
- reports/integrations/aihub-super-shell.md
- reports/integrations/aihub-connectivity-guide.md
- reports/integrations/aihub-full-framework.md
- reports/integrations/aihub-learning-rules.md
- reports/integrations/aihub-live-flags.md
- reports/integrations/aihub-integration-graph.md
- reports/integrations/aihub-unified-control-plane.md
- reports/integrations/aihub-supershell-vault-wizard.md
- reports/learning/aihub-module-blueprint.md
- reports/learning/aihub-language-skill-profiles.md
- reports/build-graph/simple-build-center.md
- reports/branch-intelligence/branch-absorption-multicloud-plan.md
- reports/learning/complex-code-grading.md
- reports/learning/module-submodule-organizer.md
- reports/learning/aihub-learning-feedback-loop.md
- reports/learning/legacy-algorithm-recovery.md
- reports/build-graph/latest.md
Manual live-control steps when ready:
- gh auth login
- az login
- review reports before any push/wiki/cloud mutation
MSG
