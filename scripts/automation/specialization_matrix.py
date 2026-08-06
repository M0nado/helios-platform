#!/usr/bin/env python3
"""Render HELIOS specialization and sub-specialization instructions."""
from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / "config" / "helios-specializations.json"
OUT = ROOT / "reports" / "specializations"

def main() -> int:
    data = json.loads(CONFIG.read_text(encoding="utf-8"))
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "specialization-matrix.json").write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Specialization Matrix", "", data.get("description", ""), "", "## Learning loop", ""]
    loop = data.get("learningLoop", {})
    for key in ["collect", "score", "adapt"]:
        lines.append(f"- **{key.title()}**: {', '.join(loop.get(key, []))}")
    lines.append("")
    for spec in data.get("specializations", []):
        lines.append(f"## {spec['title']}")
        lines.append("")
        lines.append(f"- Primary agent: `{spec['primaryAgent']}`")
        lines.append(f"- Sub-specialties: {', '.join(spec.get('subSpecialties', []))}")
        lines.append(f"- Preferred models: {', '.join(spec.get('preferredModels', []))}")
        lines.append("- Instructions:")
        for instruction in spec.get("instructions", []):
            lines.append(f"  - {instruction}")
        lines.append(f"- Learning signals: {', '.join(spec.get('learningSignals', []))}")
        lines.append("")
    (OUT / "specialization-matrix.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {(OUT / 'specialization-matrix.md').relative_to(ROOT)}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
