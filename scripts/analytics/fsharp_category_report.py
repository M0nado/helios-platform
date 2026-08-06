#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/analytics'
CATEGORIES=[('merge-risk-formulas','src/analytics/HELIOS.Analytics.FSharp/Merge/MergeScoring.fs'),('model-ranking-formulas','scripts/automation/model_cost_speed_optimizer.py'),('agent-xp-formulas','scripts/learning/agent_xp.py'),('branch-priority-formulas','scripts/github/language_aware_score.py'),('native-overlap-baseline','src/native/HELIOS.Native.Performance/include/helios/merge_analysis.hpp')]
def main():
 rows=[{'category':c,'path':p,'present':(ROOT/p).exists()} for c,p in CATEGORIES]
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'categories':rows}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'fsharp-test-categories.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'fsharp-test-categories.md').write_text('# F# Analytics Test Categories\n\n'+'\n'.join(f"- {r['category']}: {r['path']} present={r['present']}" for r in rows)+'\n')
 print(f"Wrote {(OUT/'fsharp-test-categories.md').relative_to(ROOT)}")
if __name__=='__main__': main()
