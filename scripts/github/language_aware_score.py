#!/usr/bin/env python3
"""Score merge candidates using C#/F#/C++/Python/Bicep/GitHub ownership."""
from __future__ import annotations
import json, subprocess, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OWN=ROOT/'config/helios-language-ownership.json'; SCORE=ROOT/'reports/mass-integration/mass-integration-score.json'; OUT=ROOT/'reports/mass-integration'
def run(cmd):
 p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True); return p.returncode,p.stdout.strip()
def changed(branch):
 code,base=run(['git','merge-base','HEAD',branch]); cmd=['git','diff','--name-only',base,branch] if code==0 and base else ['git','show','--name-only','--format=',branch]
 code,out=run(cmd); return [x for x in out.splitlines() if x] if code==0 else []
def impacts(files, ownership):
 result={}
 for lang,info in ownership.items():
  count=sum(1 for f in files for prefix in info.get('ownedPaths',[]) if f==prefix or f.startswith(prefix.rstrip('/')+'/'))
  if count:
   result[lang]={'fileCount':count,'score':count*int(info.get('mergeWeight',1)),'ownerAgent':info.get('ownerAgent'),'requiredChecks':info.get('requiredChecks',[])}
 return result
def formation(langs):
 if {'csharp','fsharp','cpp'} <= set(langs): return 'full-merge-raid'
 if 'csharp' in langs: return 'core-stability-party'
 if 'fsharp' in langs: return 'analytics-party'
 if 'cpp' in langs: return 'performance-party'
 if 'bicep' in langs: return 'cloud-setup-party'
 return 'aihub-party'
def main():
 ownership=json.loads(OWN.read_text(encoding='utf-8'))['languages']
 base=json.loads(SCORE.read_text(encoding='utf-8')) if SCORE.exists() else {'candidates':[]}
 candidates=[]
 for c in base.get('candidates',[]):
  files=c.get('changedFilesSample') or changed(c['branch']); impact=impacts(files,ownership); langs=list(impact)
  required=sorted({check for i in impact.values() for check in i.get('requiredChecks',[])})
  candidates.append({**c,'languageImpact':impact,'languageAwareScore':int(c.get('score',0))+sum(i['score'] for i in impact.values()),'requiredLanguageChecks':required,'recommendedParty':formation(langs),'pythonGlueOnly':'python' in langs and len(langs)==1})
 candidates.sort(key=lambda x:x['languageAwareScore'],reverse=True)
 for i,c in enumerate(candidates,1): c['languageAwareOrder']=i
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'candidates':candidates,'ownershipSource':'config/helios-language-ownership.json'}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'language-aware-score.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# Language-Aware Merge Score','','| Order | Branch | Score | Party | Languages | Required Checks |','| ---: | --- | ---: | --- | --- | --- |']
 for c in candidates: lines.append(f"| {c['languageAwareOrder']} | `{c['branch']}` | {c['languageAwareScore']} | {c['recommendedParty']} | {', '.join(c['languageImpact'].keys())} | {', '.join(c['requiredLanguageChecks'])} |")
 (OUT/'language-aware-score.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'language-aware-score.md').relative_to(ROOT)}")
if __name__=='__main__': main()
