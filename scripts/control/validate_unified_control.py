#!/usr/bin/env python3
from __future__ import annotations

import json
import py_compile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
errors: list[str] = []

for path in [
    ROOT / "config/control/unified-control.v3.json",
    ROOT / "config/control/operations.v3.json",
    ROOT / "config/control/surface-bindings.example.json",
    ROOT / "monado/helios-control/config/unified-control-v3.json",
    ROOT / "automation/control-plane/surface-contracts.v3.json",
    ROOT / "automation/openai/helios-control-tools.v3.json",
    ROOT / "control/current.json",
]:
    try:
        json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"{path.relative_to(ROOT)}: {exc}")

for path in [ROOT / "scripts/control/unified_control.py", Path(__file__)]:
    try:
        py_compile.compile(str(path), doraise=True)
    except Exception as exc:
        errors.append(f"{path.relative_to(ROOT)}: {exc}")

print(json.dumps({"ok": not errors, "errors": errors}, indent=2))
raise SystemExit(1 if errors else 0)
