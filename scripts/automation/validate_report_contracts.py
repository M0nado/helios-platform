#!/usr/bin/env python3
"""Validate report JSON against HELIOS typed contract expectations."""
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/contracts'
CHECKS={
 'reports/final-gate/final-gate.json':['status','mergeReady','results'],
 'reports/mass-integration/language-aware-score.json':['candidates','ownershipSource'],
 'reports/mass-integration/merge-decision-pipeline.json':['steps','decisions'],
 'reports/language-ownership/language-ownership.json':['languages','decisionOrder'],
}
def main():
 results=[]
 for rel,fields in CHECKS.items():
  path=ROOT/rel
  if not path.exists():
   results.append({'report':rel,'present':False,'missing':fields}); continue
  data=json.loads(path.read_text(encoding='utf-8')); missing=[f for f in fields if f not in data]
  results.append({'report':rel,'present':True,'missing':missing,'passed':not missing})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'results':results,'passed':all(r.get('present') and not r.get('missing') for r in results)}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'report-contracts.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'report-contracts.md').write_text('# Report Contract Validation\n\n'+'\n'.join(f"- {r['report']}: present={r['present']} missing={r['missing']}" for r in results)+'\n')
 print(f"Wrote {(OUT/'report-contracts.md').relative_to(ROOT)}")
 return 0 if payload['passed'] else 1
if __name__=='__main__': raise SystemExit(main())
