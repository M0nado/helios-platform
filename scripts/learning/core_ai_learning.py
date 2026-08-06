#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt, collections
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/learning'
def load(path, default):
 p=ROOT/path
 return json.loads(p.read_text(encoding='utf-8')) if p.exists() else default
def main():
 final=load('reports/final-gate/final-gate.json', {'results':[],'failed':[]}); autofix=load('reports/autofix/autofix-plan.json', {'tasks':[]}); fleet=load('reports/fleet/fleet-deploy.json', {'deployableAgents':[]})
 failures=collections.Counter(r.get('id','unknown') for r in final.get('failed',[])); langs=collections.Counter(a.get('language','unknown') for a in fleet.get('deployableAgents',[]))
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'failureHotspots':dict(failures),'languageCoverage':dict(langs),'autofixTaskCount':len(autofix.get('tasks',[])),'recommendations':['fix C# core blockers first','run F# analytics report after contracts pass','run native benchmark before C++ integration','use Azure Warden before deploy']}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'core-ai-learning.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'core-ai-learning.md').write_text('# Core AI Learning\n\n'+'\n'.join(f"- {x}" for x in payload['recommendations'])+'\n')
 print(f"Wrote {(OUT/'core-ai-learning.md').relative_to(ROOT)}")
if __name__=='__main__': main()
