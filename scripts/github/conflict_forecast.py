#!/usr/bin/env python3
"""Forecast branch conflict risk before mass integration applies merges."""
from __future__ import annotations
import json, subprocess, datetime as dt
from itertools import combinations
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/mass-integration'
PRIORITY=('helios-control','hermes-fleet-production')
HIGH_RISK_SUFFIX={'.csproj':12,'.fsproj':12,'.vcxproj':12,'.props':10,'.targets':10,'.sln':10,'.slnx':10,'.bicep':14,'.yml':9,'.yaml':9,'.json':5}
HIGH_RISK_PREFIX={'src/core/HELIOS.Platform.Contracts':18,'.github/workflows':14,'infra/azure':16,'config':8,'tools/helios.ps1':12,'tests':7}
def run(cmd):
 p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True); return p.returncode,p.stdout.strip()
def refs():
 code,out=run(['git','for-each-ref','--format=%(refname:short)','refs/remotes']); vals=[x for x in out.splitlines() if x and not x.endswith('/HEAD')] if code==0 else []
 return [x for x in vals if any(k in x.lower() for k in ('helios','hermes','he-'))]
def changed(ref,target='HEAD'):
 code,base=run(['git','merge-base',target,ref]); cmd=['git','diff','--name-only',base,ref] if code==0 and base else ['git','show','--name-only','--format=',ref]
 code,out=run(cmd); return sorted(set(out.splitlines())) if code==0 and out else []
def branch_meta(ref):
 code,out=run(['git','log','-1','--format=%ci|%h|%s',ref]); return out if code==0 else ''
def risk(files):
 score=0; reasons=[]; buckets={'projectFiles':0,'workflows':0,'infra':0,'contracts':0,'tests':0,'configs':0}
 for f in files:
  for suffix,pts in HIGH_RISK_SUFFIX.items():
   if f.endswith(suffix): score+=pts; reasons.append(f'suffix:{suffix}:{f}')
  for prefix,pts in HIGH_RISK_PREFIX.items():
   if f.startswith(prefix): score+=pts; reasons.append(f'path:{prefix}:{f}')
  if f.endswith(('.csproj','.fsproj','.vcxproj','.sln','.props','.targets')): buckets['projectFiles']+=1
  if f.startswith('.github/workflows'): buckets['workflows']+=1
  if f.startswith('infra/'): buckets['infra']+=1
  if 'Contracts' in f: buckets['contracts']+=1
  if f.startswith('tests') or '/tests/' in f: buckets['tests']+=1
  if f.startswith('config') or f.endswith(('.json','.yml','.yaml')): buckets['configs']+=1
 return score,reasons[:25],buckets
def main():
 branch_files={r:changed(r) for r in refs()}; branch_scores=[]
 for r,files in branch_files.items():
  score,reasons,buckets=risk(files); name=r.split('/',1)[-1]
  branch_scores.append({'branch':r,'shortName':name,'priorityCandidate':name in PRIORITY,'fileCount':len(files),'riskScore':score,'riskLevel':'high' if score>=80 else ('medium' if score>=35 else 'low'),'reasons':reasons,'buckets':buckets,'lastCommit':branch_meta(r),'sample':files[:40]})
 overlaps=[]
 for a,b in combinations(branch_files,2):
  shared=sorted(set(branch_files[a]) & set(branch_files[b]));
  if shared: overlaps.append({'branches':[a,b],'overlapCount':len(shared),'files':shared[:40]})
 payload={'schemaVersion':2,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'blockThreshold':80,'priorityBranches':[b for b in sorted(branch_scores,key=lambda x:(not x['priorityCandidate'],-x['riskScore'])) if b['priorityCandidate']],'branches':sorted(branch_scores,key=lambda x:(not x['priorityCandidate'],-x['riskScore'])),'overlaps':overlaps,'blocked':[b for b in branch_scores if b['riskScore']>=80]}
 OUT.mkdir(parents=True, exist_ok=True); (OUT/'conflict-forecast.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'priority-branches.json').write_text(json.dumps({'generatedUtc':payload['generatedUtc'],'priorityBranches':payload['priorityBranches']},indent=2,sort_keys=True)+'\n')
 lines=['# Conflict Forecast','',f"Block threshold: {payload['blockThreshold']}",'','| Branch | Priority | Risk | Level | Files |','| --- | --- | ---: | --- | ---: |']+[f"| `{x['branch']}` | {x['priorityCandidate']} | {x['riskScore']} | {x['riskLevel']} | {x['fileCount']} |" for x in payload['branches']]
 lines += ['','## Priority Branches']+[f"- `{x['branch']}`: {x['riskLevel']} risk, next command `python3 scripts/github/mass_integration.py score --no-fetch`" for x in payload['priorityBranches']]
 lines += ['','## Overlaps']+[f"- {o['branches'][0]} ↔ {o['branches'][1]}: {o['overlapCount']} files" for o in overlaps]
 (OUT/'conflict-forecast.md').write_text('\n'.join(lines)+'\n'); (OUT/'priority-branches.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'conflict-forecast.md').relative_to(ROOT)}")
 return 1 if payload['blocked'] else 0
if __name__=='__main__': raise SystemExit(main())
