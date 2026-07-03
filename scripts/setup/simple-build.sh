#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
export PATH="$ROOT/.tools/dotnet:$ROOT/.tools/gh/bin:$ROOT/.tools/azcli-venv/bin:$PATH"
mode="${1:-quick}"
case "$mode" in
  absorb)
    python3 scripts/analysis/branch_absorption_multicloud_plan.py
    python3 scripts/agents/branch_test_autofix_plan.py
    python3 scripts/analysis/document_code_absorption_ranker.py
    python3 scripts/analysis/merge_prune_recommendations.py
    python3 scripts/analysis/complex_code_grading.py
    python3 scripts/analysis/simple_build_center.py
    python3 scripts/dashboard/generate-gui.py
    ;;
  notes)
    python3 scripts/analysis/module_submodule_organizer.py --mode plan
    python3 scripts/analysis/aihub_learning_feedback_loop.py
    python3 scripts/analysis/aihub_self_learning_notes.py
    python3 scripts/dashboard/generate-gui.py
    ;;
  organize)
    python3 scripts/analysis/complex_code_grading.py
    python3 scripts/analysis/branch_absorption_multicloud_plan.py
    python3 scripts/analysis/merge_prune_recommendations.py
    python3 scripts/agents/agent_fleet_control_catalog.py
    python3 scripts/analysis/module_submodule_organizer.py --mode plan
    python3 scripts/analysis/aihub_learning_feedback_loop.py
    python3 scripts/analysis/aihub_self_learning_notes.py
    python3 scripts/dashboard/generate-gui.py
    ;;
  organize-scaffold-plan)
    python3 scripts/analysis/module_submodule_organizer.py --mode mark-only
    python3 scripts/dashboard/generate-gui.py
    ;;
  organize-delete-plan)
    python3 scripts/analysis/complex_code_grading.py
    python3 scripts/analysis/module_submodule_organizer.py --mode mark-only
    python3 scripts/dashboard/generate-gui.py
    ;;
  organize-full)
    python3 scripts/analysis/complex_code_grading.py
    python3 scripts/analysis/branch_absorption_multicloud_plan.py
    python3 scripts/analysis/document_code_absorption_ranker.py
    python3 scripts/analysis/merge_prune_recommendations.py
    python3 scripts/agents/branch_test_autofix_plan.py
    python3 scripts/agents/agent_fleet_control_catalog.py
    python3 scripts/analysis/module_submodule_organizer.py --mode full
    python3 scripts/analysis/aihub_learning_feedback_loop.py
    python3 scripts/analysis/aihub_self_learning_notes.py
    python3 scripts/dashboard/generate-gui.py
    ;;
  learn)
    python3 scripts/analysis/complex_code_grading.py
    python3 scripts/analysis/branch_absorption_multicloud_plan.py
    python3 scripts/analysis/merge_prune_recommendations.py
    python3 scripts/agents/branch_test_autofix_plan.py
    python3 scripts/agents/agent_fleet_control_catalog.py
    python3 scripts/analysis/aihub_language_skill_profiles.py
    python3 scripts/analysis/module_submodule_organizer.py --mode plan
    python3 scripts/analysis/aihub_learning_feedback_loop.py
    python3 scripts/analysis/aihub_self_learning_notes.py
    python3 scripts/integrations/aihub_learning_knowledge_store.py
    python3 scripts/integrations/aihub_command_ide.py
    python3 scripts/dashboard/generate-gui.py
    ;;
  finish)
    python3 scripts/analysis/branch_absorption_multicloud_plan.py
    python3 scripts/agents/branch_test_autofix_plan.py
    python3 scripts/analysis/document_code_absorption_ranker.py
    python3 scripts/analysis/merge_prune_recommendations.py
    python3 scripts/analysis/complex_code_grading.py
    python3 scripts/agents/branch_fix_agents.py --max-branches 88
    python3 scripts/analysis/simple_build_center.py
    python3 scripts/apply/finish_readiness_apply.py
    python3 scripts/analysis/module_submodule_organizer.py --mode plan
    python3 scripts/analysis/aihub_learning_feedback_loop.py
    python3 scripts/analysis/aihub_self_learning_notes.py
    python3 scripts/integrations/aihub_learning_knowledge_store.py
    python3 scripts/integrations/aihub_command_ide.py
    python3 scripts/dashboard/generate-gui.py
    ;;
  save-run)
    python3 scripts/integrations/aihub_learning_knowledge_store.py
    python3 scripts/integrations/aihub_command_ide.py
    python3 scripts/dashboard/generate-gui.py
    scripts/setup/save-run-bundle.sh
    ;;
  exe)
    scripts/setup/build-run-exe.sh
    ;;
  exe-web)
    scripts/setup/build-run-exe.sh
    bundle="$(cat .run/latest-helios-exe.txt)"
    echo "Open $bundle/web-sandbox/index.html or run: $bundle/serve-web-sandbox.sh"
    ;;
  module)
    python3 -m py_compile scripts/analysis/aihub_module_blueprint.py
    python3 scripts/analysis/aihub_module_blueprint.py
    python3 -m json.tool config/aihub-module-blueprint.json >/dev/null
    dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --nologo
    python3 scripts/build_graph/build_graph.py run --node aihub-module-blueprint --max-workers 2
    python3 scripts/dashboard/generate-gui.py
    ;;
  quick)
    python3 scripts/build_graph/build_graph.py run --profile quick --changed-only --max-workers 2
    python3 scripts/dashboard/generate-gui.py
    ;;
  full)
    python3 scripts/build_graph/build_graph.py run --profile full --max-workers 2
    python3 scripts/dashboard/generate-gui.py
    ;;
  clean)
    rm -rf .build src/core/HELIOS.Platform.Contracts/bin src/core/HELIOS.Platform.Contracts/obj src/analytics/HELIOS.Analytics.FSharp/bin src/analytics/HELIOS.Analytics.FSharp/obj
    git status --short
    ;;
  *)
    echo "Usage: scripts/setup/simple-build.sh [module|quick|full|clean|absorb|notes|organize|organize-scaffold-plan|organize-delete-plan|organize-full|learn|finish|save-run|exe|exe-web]" >&2
    exit 2
    ;;
esac
