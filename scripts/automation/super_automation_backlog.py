#!/usr/bin/env python3
"""Rank HELIOS super-automation backlog items and render a report."""
from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / "config" / "helios-super-automation-backlog.json"
OUT = ROOT / "reports" / "super-automation"

def main() -> int:
    data = json.loads(CONFIG.read_text(encoding="utf-8"))
    items = []
    for item in data.get("items", []):
        priority = int(item.get("impact", 0)) * 2 - int(item.get("difficulty", 0))
        items.append({**item, "priorityScore": priority})
    items.sort(key=lambda x: x["priorityScore"], reverse=True)
    OUT.mkdir(parents=True, exist_ok=True)
    payload = {"version": data.get("version"), "description": data.get("description"), "items": items}
    (OUT / "super-automation-backlog.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Super Automation Backlog", "", data.get("description", ""), "", "| Rank | Item | Priority | Impact | Difficulty | Domains |", "| ---: | --- | ---: | ---: | ---: | --- |"]
    for rank, item in enumerate(items, 1):
        lines.append(f"| {rank} | {item['title']} | {item['priorityScore']} | {item['impact']} | {item['difficulty']} | {', '.join(item.get('domains', []))} |")
    lines.extend(["", "## Implementation seeds", ""])
    for item in items:
        lines.append(f"### {item['title']}")
        lines.append("")
        lines.append(f"- Adds: {', '.join(item.get('adds', []))}")
        lines.append(f"- Start in: {', '.join(item.get('firstFiles', []))}")
        lines.append("")
    (OUT / "super-automation-backlog.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {(OUT / 'super-automation-backlog.md').relative_to(ROOT)}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
