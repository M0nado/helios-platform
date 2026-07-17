#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,subprocess,datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/autoconnect'
COMMANDS=['python3 scripts/github/connect_github.py verify','python3 scripts/github/setup_repository.py verify','python3 scripts/azure/bicep_report.py build','python3 scripts/integrations/helios_capability_setup.py verify','python3 scripts/automation/agent_runtime_matrix.py','python3 scripts/automation/llm_router_plan.py','python3 scripts/security/vault_readiness.py verify','python3 scripts/security/policy_gate.py','python3 scripts/github/conflict_forecast.py','python3 scripts/automation/autofix_loop.py plan','python3 scripts/automation/final_gate.py --report-only','python3 scripts/automation/render_operator_dashboard.py']
def run(c,execute):
 if not execute: return {'command':c,'dryRun':True,'exitCode':None}
 p=subprocess.run(c,cwd=ROOT,shell=True,text=True,capture_output=True); return {'command':c,'dryRun':False,'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['plan','verify','apply'],nargs='?',default='plan'); a=ap.parse_args(); res=[run(c,a.mode!='plan') for c in COMMANDS]; OUT.mkdir(parents=True,exist_ok=True)
 payload={'mode':a.mode,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'results':res}; (OUT/'autoconnect.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'autoconnect.md').write_text('# Autoconnect Setup\n\n'+'\n'.join(f"- {r['command']}: {'DRY' if r['dryRun'] else r['exitCode']}" for r in res)+'\n')
 print(f"Wrote {(OUT/'autoconnect.md').relative_to(ROOT)}"); return 1 if any(r.get('exitCode') not in (0,None) for r in res) else 0
if __name__=='__main__': raise SystemExit(main())
