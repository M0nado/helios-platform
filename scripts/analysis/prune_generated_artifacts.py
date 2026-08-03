#!/usr/bin/env python3
from __future__ import annotations
import shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
TARGETS=[ROOT/'reports/branch-intelligence',ROOT/'reports/build-graph',ROOT/'reports/codex',ROOT/'reports/control-plane',ROOT/'reports/integrations',ROOT/'reports/project-inventory',ROOT/'status-site/wiki-export']
FILES=[ROOT/'status-site/actions.md',ROOT/'status-site/index.html',ROOT/'.github/PULL_REQUEST_BODY.md']
removed=[]
for t in TARGETS:
    if t.exists(): shutil.rmtree(t); removed.append(str(t.relative_to(ROOT)))
for f in FILES:
    if f.exists(): f.unlink(); removed.append(str(f.relative_to(ROOT)))
(ROOT/'reports').mkdir(exist_ok=True)
readme=ROOT/'reports/README.md'
if not readme.exists(): readme.write_text('# HELIOS Generated Reports\n\nRun `./helios.sh all` to regenerate reports.\n')
print('Removed generated artifacts:' if removed else 'No generated artifacts to remove.')
for item in removed: print(f'- {item}')
