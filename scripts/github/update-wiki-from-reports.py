#!/usr/bin/env python3
from pathlib import Path
import shutil
ROOT = Path(__file__).resolve().parents[2]
REPORTS = ROOT / "reports" / "branch-intelligence"
OUT = ROOT / "status-site" / "wiki-export"
OUT.mkdir(parents=True, exist_ok=True)
for src, dest in {
    "dashboard.md": "Branch-Intelligence.md",
    "branch-ranking.md": "Module-Ranking.md",
    "idea-impact-summary.md": "Idea-Impact.md",
    "agent-work-queue.md": "Agent-Work-Queue.md",
    "graphs.md": "Branch-Graphs.md",
}.items():
    path = REPORTS / src
    if path.exists():
        shutil.copyfile(path, OUT / dest)
print(f"Wrote wiki export to {OUT.relative_to(ROOT)}")
