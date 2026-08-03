#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,os,shlex,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/apply/finish-readiness-apply.json'
MD=ROOT/'reports/apply/finish-readiness-apply.md'
SAFE_STEPS=[
    {'id':'bootstrap-tools','command':'scripts/setup/bootstrap-local-tools.sh','mutates':'local .tools only','apply':True},
    {'id':'secret-preflight','command':'python3 scripts/security/secret_preflight.py','mutates':'reports only','apply':True},
    {'id':'apply-gate-preflight','command':'python3 scripts/security/apply_gate_preflight.py','mutates':'reports only','apply':True},
    {'id':'hermes-fleet-readiness','command':'python3 scripts/agents/hermes_fleet_readiness.py','mutates':'reports only','apply':True},
    {'id':'azure-what-if-plan','command':'python3 scripts/azure/azure_what_if.py','mutates':'reports only; Azure what-if when authenticated','apply':True},
    {'id':'finish-readiness','command':'python3 scripts/integrations/readiness_score.py','mutates':'reports only','apply':True},
    {'id':'code-learning-atlas','command':'python3 scripts/analysis/code_learning_atlas.py','mutates':'reports only; code/domain learning atlas','apply':True},
    {'id':'language-role-optimizer','command':'python3 scripts/analysis/language_role_optimizer.py','mutates':'reports only; language/module placement strategy','apply':True},
    {'id':'language-decision-matrix','command':'python3 scripts/analysis/language_decision_matrix.py','mutates':'reports only; language placement variables','apply':True},
    {'id':'aihub-module-blueprint','command':'python3 scripts/analysis/aihub_module_blueprint.py','mutates':'reports only; GUI/USBWizard/module language blueprint','apply':True},
    {'id':'aihub-language-skill-profiles','command':'python3 scripts/analysis/aihub_language_skill_profiles.py','mutates':'reports only; language skill profiles and upgrade loop','apply':True},
    {'id':'module-submodule-test-matrix','command':'python3 scripts/analysis/module_submodule_test_matrix.py','mutates':'reports only; module/submodule test matrix','apply':True},
    {'id':'knowledge-absorption-engine','command':'python3 scripts/analysis/knowledge_absorption_engine.py','mutates':'reports only; today knowledge absorption backlog','apply':True},
    {'id':'control-plane-knowledge-matrix','command':'python3 scripts/analysis/control_plane_knowledge_matrix.py','mutates':'reports only; control plane knowledge map','apply':True},
    {'id':'document-code-absorption-ranker','command':'python3 scripts/analysis/document_code_absorption_ranker.py','mutates':'reports only; document/code absorption scoring','apply':True},
    {'id':'branch-absorption-multicloud-plan','command':'python3 scripts/analysis/branch_absorption_multicloud_plan.py','mutates':'reports only; branch absorption multicloud UI plan','apply':True},
    {'id':'language-engine-catalog','command':'python3 scripts/analysis/language_engine_catalog.py','mutates':'reports only; engine capabilities and synthesis','apply':True},
    {'id':'agent-language-framework','command':'python3 scripts/analysis/agent_language_framework.py','mutates':'reports only; agent/language/module branch framework','apply':True},
    {'id':'recover-missing-branch-work','command':'python3 scripts/analysis/recover_missing_branch_work.py','mutates':'reports only; missing branch recovery visibility','apply':True},
    {'id':'commit-window-unification','command':'python3 scripts/analysis/commit_window_unification.py','mutates':'reports only; commit window synthesis','apply':True},
    {'id':'super-branch-unification','command':'python3 scripts/analysis/super_branch_unification.py','mutates':'reports only; no branch merge/delete/push','apply':True},
    {'id':'deep-branch-code-score','command':'python3 scripts/analysis/deep_branch_code_score.py','mutates':'reports only; branch/code scoring','apply':True},
    {'id':'agent-specialization-matrix','command':'python3 scripts/agents/agent_specialization_matrix.py','mutates':'reports only; agent specialization map','apply':True},
    {'id':'agent-fleet-control-catalog','command':'python3 scripts/agents/agent_fleet_control_catalog.py','mutates':'reports only; agent fleet/provider/control catalog','apply':True},
    {'id':'agent-fleet-autopilot','command':'python3 scripts/agents/agent_fleet_autopilot.py','mutates':'reports only; smart agent/fleet unit plan','apply':True},
    {'id':'branch-test-autofix-plan','command':'python3 scripts/agents/branch_test_autofix_plan.py','mutates':'reports only; branch test/autofix plan','apply':True},
    {'id':'branch-fix-agents','command':'python3 scripts/agents/branch_fix_agents.py --max-branches 88','mutates':'reports only; branch fix packets','apply':True},
    {'id':'aihub-super-shell','command':'python3 scripts/integrations/aihub_super_shell.py','mutates':'reports only; cross-LLM AIHub super shell setup','apply':True},
    {'id':'aihub-connectivity-guide','command':'python3 scripts/integrations/aihub_connectivity_guide.py','mutates':'reports only; AIHub local/cloud ports and tunnels guide','apply':True},
    {'id':'legacy-algorithm-recovery','command':'python3 scripts/analysis/legacy_algorithm_recovery.py','mutates':'reports only; recover older algorithms and ideas','apply':True},
    {'id':'aihub-live-flags','command':'python3 scripts/integrations/aihub_live_flags.py','mutates':'reports only; live automation toggle guidance','apply':True},
    {'id':'aihub-learning-rules','command':'python3 scripts/integrations/aihub_learning_rules.py','mutates':'reports only; AIHub LLM/tool/agent learning rules','apply':True},
    {'id':'aihub-full-framework','command':'python3 scripts/integrations/aihub_full_framework.py','mutates':'reports only; full AIHub framework launch plan','apply':True},
    {'id':'aihub-integration-graph','command':'python3 scripts/integrations/aihub_integration_graph.py','mutates':'reports only; cross-language AIHub graph and GUI model','apply':True},
    {'id':'aihub-unified-control-plane','command':'python3 scripts/integrations/aihub_unified_control_plane.py','mutates':'reports only; unified AIHub/Hermes/XCore GUI model','apply':True},
    {'id':'aihub-supershell-vault-wizard','command':'python3 scripts/integrations/aihub_supershell_vault_wizard.py','mutates':'reports only; SuperShell vault/setup/module wizard','apply':True},
    {'id':'full-integration-matrix','command':'python3 scripts/integrations/full_integration_matrix.py','mutates':'reports only','apply':True},
    {'id':'complex-code-grading','command':'python3 scripts/analysis/complex_code_grading.py','mutates':'reports only; 30-variable keep/prune code grading','apply':True},
    {'id':'module-submodule-organizer','command':'python3 scripts/analysis/module_submodule_organizer.py','mutates':'reports only; automatic module/submodule placement tree','apply':True},
    {'id':'aihub-learning-feedback-loop','command':'python3 scripts/analysis/aihub_learning_feedback_loop.py','mutates':'reports only; integrated learning transfer loop','apply':True},
    {'id':'dashboard','command':'python3 scripts/dashboard/generate-gui.py','mutates':'status-site dashboard only','apply':True},
    {'id':'simple-build-center','command':'python3 scripts/analysis/simple_build_center.py','mutates':'reports only; concise build command buttons','apply':True},
    {'id':'build-graph-quick','command':'python3 scripts/build_graph/build_graph.py run --profile quick --changed-only --max-workers 4','mutates':'reports only','apply':True},
    {'id':'finish-tasks','command':'python3 scripts/apply/generate_finish_tasks.py','mutates':'reports only','apply':True},
]

def tool_path_env():
    tools=ROOT/'.tools'
    paths=[tools/'dotnet',tools/'gh/bin',tools/'azcli-venv/bin']
    env=os.environ.copy()
    env['PATH']=':'.join(str(p) for p in paths)+':'+env.get('PATH','')
    return env

def run_step(step, apply):
    result={'id':step['id'],'command':step['command'],'mutates':step['mutates'],'status':'planned','exitCode':None,'tail':[]}
    if not apply or not step.get('apply'):
        return result
    proc=subprocess.run(step['command'],cwd=ROOT,shell=True,text=True,capture_output=True,timeout=600,env=tool_path_env())
    result.update({'status':'passed' if proc.returncode==0 else 'failed','exitCode':proc.returncode,'tail':(proc.stdout+proc.stderr).splitlines()[-12:]})
    return result

def main():
    parser=argparse.ArgumentParser(description='Plan or apply safe finish-readiness automation steps.')
    parser.add_argument('--apply', action='store_true', help='execute the safe local/report-only steps')
    parser.add_argument('--json', action='store_true')
    args=parser.parse_args()
    results=[]
    for step in SAFE_STEPS:
        results.append(run_step(step,args.apply))
        if results[-1]['status']=='failed':
            break
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'mode':'apply' if args.apply else 'plan','ok':all(r['status'] in {'planned','passed'} for r in results),'steps':results,'manualSteps':['Run `az login` before live Azure what-if/deploy operations.','Set HELIOS_AZURE_RESOURCE_GROUP or pass --resource-group for the target Azure group.','Review reports/apply/finish-readiness-apply.md before any production mutation.']}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Finish Readiness Apply Plan','',f"Generated: `{payload['generatedUtc']}`",'',f"Mode: **{payload['mode']}**",f"Status: {'PASS' if payload['ok'] else 'FAIL'}",'', '| Step | Status | Command | Mutates |','| --- | --- | --- | --- |']
    lines += [f"| `{r['id']}` | {r['status']} | `{r['command']}` | {r['mutates']} |" for r in results]
    lines += ['','## Manual steps','']+[f"- {step}" for step in payload['manualSteps']]
    MD.write_text('\n'.join(lines)+'\n')
    if args.json: print(json.dumps(payload,indent=2))
    else:
        print(f"Finish apply {payload['mode']}: {'PASS' if payload['ok'] else 'FAIL'}")
        print(f"Wrote {OUT.relative_to(ROOT)}")
        print(f"Wrote {MD.relative_to(ROOT)}")
    return 0 if payload['ok'] else 1
if __name__=='__main__': sys.exit(main())
