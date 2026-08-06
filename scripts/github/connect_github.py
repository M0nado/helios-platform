#!/usr/bin/env python3
"""Verify/apply GitHub auto-connect readiness without printing secrets."""
from __future__ import annotations
import argparse, json, os, shutil, subprocess, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/github-connect'
def run(cmd):
 p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True); return {'command':' '.join(cmd),'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['plan','verify','apply'],nargs='?',default='verify'); a=ap.parse_args(); checks=[]
 checks.append({'name':'git','ok':bool(shutil.which('git'))})
 if shutil.which('git'): checks += [{'name':'remotes',**run(['git','remote','-v'])},{'name':'default-branch',**run(['git','symbolic-ref','refs/remotes/origin/HEAD'])}]
 checks.append({'name':'HELIOS_AUTOMATION_TOKEN','ok':bool(os.getenv('HELIOS_AUTOMATION_TOKEN')),'secretPrinted':False})
 checks.append({'name':'gh','ok':bool(shutil.which('gh'))})
 if shutil.which('gh') and a.mode!='plan': checks.append({'name':'gh-auth',**run(['gh','auth','status'])})
 payload={'mode':a.mode,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'checks':checks,'applySupported':a.mode=='apply'}; OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'github-connect.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'github-connect.md').write_text('# GitHub Connect\n\n'+'\n'.join(f"- {c['name']}: {c.get('ok', c.get('exitCode')==0)}" for c in checks)+'\n')
 print(f"Wrote {(OUT/'github-connect.md').relative_to(ROOT)}")
 return 1 if any(c.get('ok') is False for c in checks if c['name'] in {'git'}) else 0
if __name__=='__main__': raise SystemExit(main())
