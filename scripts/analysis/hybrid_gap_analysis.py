#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; ROAD=ROOT/'config/hybrid-integration-roadmap.json'; OUT=ROOT/'reports/project-inventory/hybrid-gap-analysis.json'; MD=ROOT/'reports/project-inventory/hybrid-gap-analysis.md'
road=json.loads(ROAD.read_text())['domains']
items=[]
for d in road:
    existing=[]; missing=[]
    for pattern in d['paths']:
        matches=list(ROOT.glob(pattern)) if any(ch in pattern for ch in '*?[]') else [ROOT/pattern]
        if any(m.exists() for m in matches): existing.append(pattern)
        else: missing.append(pattern)
    readiness=round(100*len(existing)/max(1,len(d['paths'])))
    items.append({'domain':d['name'],'priority':d['priority'],'readiness':readiness,'existing':existing,'missing':missing,'next':d['next']})
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'items':items}
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
lines=['# Hybrid Integration Gap Analysis','',f"Generated: `{payload['generatedUtc']}`",'','| Domain | Priority | Readiness | Next |','| --- | ---: | ---: | --- |']
for i in sorted(items,key=lambda x:(-x['priority'],x['readiness'])): lines.append(f"| {i['domain']} | {i['priority']} | {i['readiness']} | {i['next']} |")
MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
