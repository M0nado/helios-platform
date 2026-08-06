#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/ai-providers'; CONFIG=ROOT/'config/helios-model-store.json'
def tier(v):
 return {'low':1,'cheap':1,'medium':2,'balanced':2,'high':3,'premium':3,'fast':1,'slow':3}.get(str(v).lower(),2)
def main():
 data=json.loads(CONFIG.read_text(encoding='utf-8')) if CONFIG.exists() else {'models':[]}; models=data.get('models', data if isinstance(data,list) else [])
 ranked=sorted([m for m in models if isinstance(m,dict)], key=lambda m:(tier(m.get('costTier')),tier(m.get('speedTier')),-tier(m.get('qualityTier'))))
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'cheapestAcceptable':ranked[0] if ranked else None,'fastestAcceptable':sorted(ranked,key=lambda m:tier(m.get('speedTier')))[0] if ranked else None,'highestConfidence':sorted(ranked,key=lambda m:tier(m.get('qualityTier')),reverse=True)[0] if ranked else None,'parallelCommitteePlan':ranked[:3]}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'cost-speed-optimizer.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'cost-speed-optimizer.md').write_text('# Model Cost/Speed Optimizer\n\n'+json.dumps(payload,indent=2)+'\n')
 print(f"Wrote {(OUT/'cost-speed-optimizer.md').relative_to(ROOT)}")
if __name__=='__main__': main()
