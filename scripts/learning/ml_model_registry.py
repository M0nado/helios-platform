#!/usr/bin/env python3
"""Render ML model registry for merge prediction and agent/model selection."""
from __future__ import annotations
import json, collections, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CONFIG=ROOT/'config/helios-ml-models.json'; OUT=ROOT/'reports/learning'
def main():
 data=json.loads(CONFIG.read_text(encoding='utf-8')); models=data.get('models',[]); by_lang=collections.Counter(m.get('ownerLanguage','unknown') for m in models); by_maturity=collections.Counter(m.get('maturity','unknown') for m in models)
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'models':models,'byOwnerLanguage':dict(by_lang),'byMaturity':dict(by_maturity),'recommendedNext':'promote merge-risk-heuristic to F# active engine, then train merge-risk-logistic from events.jsonl'}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'ml-models.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# HELIOS ML Model Registry','','| Model | Type | Owner | Maturity | Metric |','| --- | --- | --- | --- | --- |']
 for m in models: lines.append(f"| {m['id']} | {m['type']} | {m['ownerLanguage']} | {m['maturity']} | {m['metric']} |")
 (OUT/'ml-models.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'ml-models.md').relative_to(ROOT)}")
if __name__=='__main__': main()
