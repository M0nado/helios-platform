#!/usr/bin/env python3
from __future__ import annotations
import json,collections
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/learning'; LOG=OUT/'events.jsonl'
def main():
 events=[json.loads(x) for x in LOG.read_text().splitlines()] if LOG.exists() else [] ; by=collections.Counter(e.get('model','unknown') for e in events); OUT.mkdir(parents=True,exist_ok=True)
 payload={'events':len(events),'byModel':dict(by)}; (OUT/'summary.json').write_text(json.dumps(payload,indent=2)+'\n'); (OUT/'summary.md').write_text('# Learning Summary\n\n'+json.dumps(payload,indent=2)+'\n'); print(f"Wrote {(OUT/'summary.md').relative_to(ROOT)}")
if __name__=='__main__': main()
