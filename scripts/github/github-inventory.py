#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/control-plane/github-inventory.json'; MD=ROOT/'reports/control-plane/github-inventory.md'
def run(cmd):
    if shutil.which(cmd[0]) is None: return {'ok':False,'available':False,'command':cmd,'detail':f'{cmd[0]} not found'}
    p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True,timeout=25)
    return {'ok':p.returncode==0,'available':True,'command':cmd,'exitCode':p.returncode,'detail':(p.stdout or p.stderr).strip().splitlines()[:20]}
report={'generatedUtc':datetime.now(timezone.utc).isoformat(),'safeByDefault':True,'repo':run(['gh','repo','view','--json','nameWithOwner,defaultBranchRef,url,visibility']),'auth':run(['gh','auth','status']),'workflows':run(['gh','workflow','list','--limit','50']),'runs':run(['gh','run','list','--limit','10']),'secrets':run(['gh','secret','list']),'variables':run(['gh','variable','list']),'environments':run(['gh','api','repos/{owner}/{repo}/environments']),'pages':run(['gh','api','repos/{owner}/{repo}/pages']),'branchProtection':run(['gh','api','repos/{owner}/{repo}/branches/main/protection']),'desiredState':'config/github-control.example.json'}
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(report,indent=2,sort_keys=True)+'\n')
lines=['# GitHub Inventory','',f"Generated: `{report['generatedUtc']}`",'', '| Area | Status | Detail |','| --- | --- | --- |']
for k,v in report.items():
    if isinstance(v,dict) and 'ok' in v: lines.append(f"| {k} | {'✅' if v.get('ok') else '⚠️'} | `{str(v.get('detail'))[:160]}` |")
MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
