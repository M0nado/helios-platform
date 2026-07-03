#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, subprocess, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/deep-setup'
COMMANDS=['python3 scripts/github/github_takeover_status.py','python3 scripts/security/vault_readiness.py verify','python3 scripts/microsoft365/copilot_m365_readiness.py','python3 scripts/automation/model_store_report.py','python3 scripts/automation/hermes_xcore_model_setup.py','python3 scripts/learning/agent_xp.py','python3 scripts/learning/agent_party.py','python3 scripts/automation/helios_store.py','python3 scripts/automation/render_operator_dashboard.py']
def run(c,execute):
 if not execute: return {'command':c,'dryRun':True,'exitCode':None}
 p=subprocess.run(c,cwd=ROOT,shell=True,text=True,capture_output=True); return {'command':c,'dryRun':False,'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['plan','verify'],nargs='?',default='plan'); a=ap.parse_args(); res=[run(c,a.mode=='verify') for c in COMMANDS]
 OUT.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'mode':a.mode,'results':res}; (OUT/'deep-setup-all.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'deep-setup-all.md').write_text('# Deep Setup All\n\n'+'\n'.join(f"- {r['command']}: {'DRY' if r['dryRun'] else r['exitCode']}" for r in res)+'\n')
 print(f"Wrote {(OUT/'deep-setup-all.md').relative_to(ROOT)}"); return 1 if any(r.get('exitCode') not in (0,None) for r in res) else 0
if __name__=='__main__': raise SystemExit(main())
