#!/usr/bin/env python3
from __future__ import annotations
import json, collections, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/learning'; LOG=OUT/'events.jsonl'
def main():
 events=[]
 if LOG.exists():
  events=[json.loads(line) for line in LOG.read_text(encoding='utf-8').splitlines() if line.strip()]
 by_task=collections.Counter(e.get('task','unknown') for e in events); failures=collections.Counter(e.get('language','unknown') for e in events if not e.get('success'))
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'eventCount':len(events),'taskCounts':dict(by_task),'failureLanguages':dict(failures),'recommendations':['prioritize repeated failure languages','route costly tasks to local/Hermes fallback when quality allows','raise agent XP after verified green runs']}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'summary.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'summary.md').write_text('# Learning Summary\n\n'+json.dumps(payload,indent=2)+'\n')
 print(f"Wrote {(OUT/'summary.md').relative_to(ROOT)}")
if __name__=='__main__': main()
