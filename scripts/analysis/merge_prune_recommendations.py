#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; SRC=ROOT/'reports/branch-intelligence/branch-ranking.json'; OUT=ROOT/'reports/branch-intelligence/merge-prune-recommendations.json'; MD=ROOT/'reports/branch-intelligence/merge-prune-recommendations.md'
branches=json.loads(SRC.read_text()) if SRC.exists() else []
recs=[]
for b in branches:
    score=b.get('score',0); files=b.get('fileCount',0); ci=b.get('ci',{}).get('score',0); action=b.get('recommendedAction','review')
    if score>=80 and ci>=50: rec='merge-candidate'
    elif score>=50: rec='extract-ideas-only'
    elif files==0: rec='prune-after-review'
    else: rec='archive'
    recs.append({'branch':b.get('name'),'score':score,'ciScore':ci,'fileCount':files,'currentAction':action,'recommendation':rec,'reason':'Safe recommendation only; no branch is deleted or merged automatically.'})
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'recommendations':recs}
OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
lines=['# Branch Merge / Prune Recommendations','','No branches are merged or deleted by this report.','','| Branch | Score | CI | Files | Recommendation |','| --- | ---: | ---: | ---: | --- |']+[f"| `{r['branch']}` | {r['score']} | {r['ciScore']} | {r['fileCount']} | {r['recommendation']} |" for r in recs]
MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
