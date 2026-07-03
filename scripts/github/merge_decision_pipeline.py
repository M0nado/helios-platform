#!/usr/bin/env python3
"""Mixed-language merge decision pipeline: C# orchestration plan + F# analytics + C++ acceleration hooks + Python report glue."""
from __future__ import annotations
import json, subprocess, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/mass-integration'
def call(cmd):
 p=subprocess.run(cmd,cwd=ROOT,shell=True,text=True,capture_output=True); return {'command':cmd,'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def load(path,default):
 p=ROOT/path
 return json.loads(p.read_text(encoding='utf-8')) if p.exists() else default
def main():
 steps=[call('python3 scripts/github/language_aware_score.py'),call('python3 scripts/github/conflict_forecast.py')]
 language=load('reports/mass-integration/language-aware-score.json',{'candidates':[]}); conflict=load('reports/mass-integration/conflict-forecast.json',{'blocked':[]})
 blocked={b.get('branch') for b in conflict.get('blocked',[])}
 decisions=[]
 for c in language.get('candidates',[]):
  decisions.append({'branch':c['branch'],'score':c.get('languageAwareScore',0),'recommendedParty':c.get('recommendedParty'),'requiredChecks':c.get('requiredLanguageChecks',[]),'blocked':c['branch'] in blocked,'engines':['csharp-orchestration','fsharp-analytics','cpp-overlap-accelerator','python-report-glue']})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'steps':steps,'decisions':decisions}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'merge-decision-pipeline.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'merge-decision-pipeline.md').write_text('# Merge Decision Pipeline\n\n'+'\n'.join(f"- `{d['branch']}` score {d['score']} party {d['recommendedParty']} blocked={d['blocked']}" for d in decisions)+'\n')
 print(f"Wrote {(OUT/'merge-decision-pipeline.md').relative_to(ROOT)}")
 return 1 if any(s['exitCode'] not in (0,None) for s in steps) else 0
if __name__=='__main__': raise SystemExit(main())
