#!/usr/bin/env python3
from __future__ import annotations
import json,re,shutil
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; SRC=ROOT/'reports/branch-intelligence/agent-work-queue.json'; OUT=ROOT/'reports/codex/tasks'; IDX=ROOT/'reports/codex/task-index.md'
def slug(s): return re.sub(r'[^a-z0-9]+','-',str(s).lower()).strip('-')[:80] or 'task'
items=(json.loads(SRC.read_text()) if SRC.exists() else [])[:20]
if OUT.exists(): shutil.rmtree(OUT)
OUT.mkdir(parents=True,exist_ok=True); lines=['# Codex Task Packets','',f"Generated: `{datetime.now(timezone.utc).isoformat()}`",'']
for i,item in enumerate(items,1):
    name=f"{i:03d}-{slug(item.get('branch'))}-{slug(item.get('module'))}.md"; path=OUT/name
    body=f"""# Codex Task: {item.get('module')} from {item.get('branch')}

- Priority: {item.get('priorityScore')}
- Task type: {item.get('taskType')}
- Allowed paths: `{item.get('allowedPaths')}`
- Blocked paths: `{item.get('blockedPaths')}`
- Expected output: {item.get('expectedOutput')}
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
"""
    path.write_text(body); lines.append(f"- [{path.name}](tasks/{path.name})")
IDX.parent.mkdir(parents=True,exist_ok=True); IDX.write_text('\n'.join(lines)+'\n'); print(f'Wrote {IDX.relative_to(ROOT)}')
