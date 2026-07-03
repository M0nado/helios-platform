#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, subprocess, datetime as dt, shlex
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/deep-setup'
COMMANDS=['python3 scripts/automation/unitary_ai_system.py','python3 scripts/github/github_takeover_status.py','python3 scripts/github/language_aware_score.py','python3 scripts/github/merge_decision_pipeline.py','python3 scripts/analytics/fsharp_category_report.py','python3 scripts/automation/validate_report_contracts.py','python3 scripts/automation/language_required_checks.py','python3 scripts/browser/edge_mode_readiness.py','python3 scripts/security/vault_readiness.py verify','python3 scripts/microsoft365/copilot_m365_readiness.py','python3 scripts/automation/language_ownership_report.py','python3 scripts/automation/language_optimization_matrix.py','python3 scripts/learning/ml_model_registry.py','python3 scripts/learning/party_formations.py','python3 scripts/automation/provider_health.py','python3 scripts/automation/model_cost_speed_optimizer.py','python3 scripts/automation/model_store_report.py','python3 scripts/automation/hermes_xcore_model_setup.py','python3 scripts/learning/agent_xp.py','python3 scripts/learning/agent_party.py','python3 scripts/learning/fleet_deploy.py','python3 scripts/learning/core_ai_learning.py','python3 scripts/automation/code_fix_center.py','python3 scripts/automation/helios_store.py','python3 scripts/learning/summarize_learning.py','python3 scripts/automation/gui_runner_bridge.py list','python3 scripts/automation/render_operator_dashboard.py']
def describe(c):
 first=shlex.split(c)[1] if c.startswith('python3 ') and len(shlex.split(c))>1 else c.split()[0]
 return {'command':c,'script':first,'reportFamily':first.replace('scripts/','reports/').replace('.py',''),'purpose':first.split('/')[-1].replace('.py','').replace('_',' ')}
def run(c,execute):
 base=describe(c)
 if not execute: return {**base,'dryRun':True,'exitCode':None,'planned':True}
 p=subprocess.run(c,cwd=ROOT,shell=True,text=True,capture_output=True); return {**base,'dryRun':False,'planned':False,'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['plan','verify'],nargs='?',default='plan'); a=ap.parse_args(); res=[run(c,a.mode=='verify') for c in COMMANDS]
 OUT.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'mode':a.mode,'results':res}; (OUT/'deep-setup-all.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'deep-setup-all.md').write_text('# Deep Setup All\n\n'+'\n'.join(f"- {r['command']}: {'DRY' if r['dryRun'] else r['exitCode']}" for r in res)+'\n')
 print(f"Wrote {(OUT/'deep-setup-all.md').relative_to(ROOT)}"); return 1 if any(r.get('exitCode') not in (0,None) for r in res) else 0
if __name__=='__main__': raise SystemExit(main())
