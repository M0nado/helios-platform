#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
REPORTS = ROOT / "reports" / "branch-intelligence"
OUT = REPORTS / "graphs.md"

def load(name):
    path = REPORTS / name
    return json.loads(path.read_text()) if path.exists() else []
branches = load("branch-ranking.json")
ideas = load("idea-impact-summary.json")[:25]
queue = load("agent-work-queue.json")[:25]
lines = ["# Branch Intelligence Graphs", "", "## Branch to module", "", "```mermaid", "graph TD"]
for branch in branches[:20]:
    bid = branch["name"].replace("/", "_").replace("-", "_")
    lines.append(f"  {bid}[\"{branch['name']} ({branch['score']})\"]")
    for module in branch.get("modules", {}):
        mid = module.replace("/", "_").replace(".", "_").replace("-", "_")
        lines.append(f"  {bid} --> {mid}[\"{module}\"]")
lines += ["```", "", "## Idea categories", "", "```mermaid", "graph LR"]
for idea in ideas:
    cat = idea["category"].replace("-", "_")
    mod = str(idea["module"]).replace("/", "_").replace(".", "_").replace("-", "_")
    lines.append(f"  {cat}[\"{idea['category']}\"] --> {mod}[\"{idea['module']}\"]")
lines += ["```", "", "## Agent queue", "", "```mermaid", "graph TD"]
for i, item in enumerate(queue, 1):
    lines.append(f"  Q{i}[\"{item['taskType']} {item['priorityScore']}\"] --> M{i}[\"{item['module']}\"]")
lines.append("```")
OUT.write_text("\n".join(lines) + "\n")
print(f"Wrote {OUT.relative_to(ROOT)}")
