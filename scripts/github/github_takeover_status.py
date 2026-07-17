#!/usr/bin/env python3
from __future__ import annotations
import json, os, shutil, subprocess, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/github-takeover'
def run(cmd):
 p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True); return {'command':' '.join(cmd),'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def main():
 checks=[{'name':'git','ok':bool(shutil.which('git'))},{'name':'gh','ok':bool(shutil.which('gh'))},{'name':'HELIOS_AUTOMATION_TOKEN','ok':bool(os.getenv('HELIOS_AUTOMATION_TOKEN')),'secretPrinted':False}]
 if shutil.which('git'): checks.append({'name':'remotes',**run(['git','remote','-v'])})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'checks':checks,'canTakeover':all(c.get('ok',c.get('exitCode')==0) for c in checks if c['name'] in {'git'})}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'status.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'status.md').write_text('# GitHub Takeover Status\n\n'+'\n'.join(f"- {c['name']}: {c.get('ok', c.get('exitCode')==0)}" for c in checks)+'\n')
 print(f"Wrote {(OUT/'status.md').relative_to(ROOT)}")
if __name__=='__main__': main()
