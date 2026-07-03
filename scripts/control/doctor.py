#!/usr/bin/env python3
from __future__ import annotations
import json, shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/control-plane/doctor.md'
fixes={
 'gh':'scripts/setup/bootstrap-local-tools.sh then gh auth login',
 'az':'scripts/setup/bootstrap-local-tools.sh then az login',
 'dotnet':'scripts/setup/bootstrap-local-tools.sh',
 'cmake':'Install CMake or use a GitHub-hosted runner with CMake available',
 'python3':'Install Python 3.12+ or use GitHub Actions setup-python',
 'git':'Install Git or use Codespaces/Cloud Shell',
 'ruby':'Optional: install Ruby for full YAML parse fallback; stdlib workflow validation still runs without PyYAML'
}
checks=['python3 scripts/control/validate_workflows.py','./helios.sh apps','./helios.sh readiness']
lines=['# HELIOS Doctor','','Runbook for getting local setup as close to 100% as possible.','','| Tool | Status | Fix |','| --- | --- | --- |']
for tool,fix in fixes.items(): lines.append(f"| `{tool}` | {'✅' if shutil.which(tool) else '⚠️'} | {fix} |")
lines += ['','## Automatic validation commands'] + [f'- `{cmd}`' for cmd in checks] + ['','## One-command path','','```bash','scripts/setup/helios-dev.sh --serve','```','','## PR update path','','```bash','python3 scripts/github/update-pr-from-reports.py --dry-run','python3 scripts/github/update-pr-from-reports.py --apply','```']
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}')
