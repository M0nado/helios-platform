#!/usr/bin/env python3
"""Forecast branch conflict risk before mass integration applies merges."""
from __future__ import annotations
import json, subprocess
from itertools import combinations
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/mass-integration'
HIGH_RISK_SUFFIX={'.csproj','.fsproj','.sln','.slnx','.bicep','.yml','.yaml','.json'}
HIGH_RISK_PREFIX=('src/core/HELIOS.Platform.Contracts','.github/workflows','infra/azure','config','tools/helios.ps1')
def run(cmd):
    p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True); return p.returncode,p.stdout.strip()
def refs():
    code,out=run(['git','for-each-ref','--format=%(refname:short)','refs/remotes']);
    return [x for x in out.splitlines() if x and not x.endswith('/HEAD') and ('helios' in x.lower() or 'hermes' in x.lower() or 'he-' in x.lower())] if code==0 else []
def changed(ref,target='HEAD'):
    code,base=run(['git','merge-base',target,ref]); cmd=['git','diff','--name-only',base,ref] if code==0 and base else ['git','show','--name-only','--format=',ref]
    code,out=run(cmd); return sorted(set(out.splitlines())) if code==0 and out else []
def risk(files):
    score=0; reasons=[]
    for f in files:
        if f.endswith(tuple(HIGH_RISK_SUFFIX)): score+=8; reasons.append(f'high-risk-suffix:{f}')
        if f.startswith(HIGH_RISK_PREFIX): score+=12; reasons.append(f'high-risk-path:{f}')
    return score,reasons[:20]
def main():
    data=[]; branch_files={r:changed(r) for r in refs()}
    for r,files in branch_files.items():
        score,reasons=risk(files); data.append({'branch':r,'fileCount':len(files),'riskScore':score,'reasons':reasons,'sample':files[:30]})
    overlaps=[]
    for a,b in combinations(branch_files,2):
        shared=sorted(set(branch_files[a]) & set(branch_files[b]));
        if shared: overlaps.append({'branches':[a,b],'overlapCount':len(shared),'files':shared[:30]})
    OUT.mkdir(parents=True, exist_ok=True); payload={'branches':sorted(data,key=lambda x:x['riskScore'],reverse=True),'overlaps':overlaps}
    (OUT/'conflict-forecast.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
    lines=['# Conflict Forecast','','| Branch | Risk | Files |','| --- | ---: | ---: |']+[f"| `{x['branch']}` | {x['riskScore']} | {x['fileCount']} |" for x in payload['branches']]
    lines += ['','## Overlaps']+[f"- {o['branches'][0]} ↔ {o['branches'][1]}: {o['overlapCount']} files" for o in overlaps]
    (OUT/'conflict-forecast.md').write_text('\n'.join(lines)+'\n'); print(f"Wrote {(OUT/'conflict-forecast.md').relative_to(ROOT)}")
if __name__=='__main__': main()
