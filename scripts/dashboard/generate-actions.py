#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'status-site/actions.md'
commands=[('Setup','./helios.sh setup'),('Dashboard','./helios.sh dashboard'),('Status','./helios.sh status'),('Everything safe','./helios.sh all'),('GitHub inventory','./helios.sh github'),('Azure inventory','./helios.sh azure'),('Build graph','./helios.sh build'),('Codex tasks','./helios.sh codex'),('Recommendations','./helios.sh recommendations'),('GitHub hosted workflow','gh workflow run helios-control-plane.yml -f publish_pages=true -f update_wiki=true')]
lines=['# HELIOS Dashboard Actions','','Copy/paste actions for local and hosted control.','']
for title,cmd in commands: lines += [f'## {title}','','```bash',cmd,'```','']
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text('\n'.join(lines)); print(f'Wrote {OUT.relative_to(ROOT)}')
