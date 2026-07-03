#!/usr/bin/env python3
from __future__ import annotations

import json
import py_compile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "reports" / "python-static"
EXCLUDE_PARTS = {".git", ".venv", ".tools", "__pycache__", "bin", "obj"}
SEARCH_ROOTS = [ROOT / "scripts"]


def discover() -> list[Path]:
    files: list[Path] = []
    for root in SEARCH_ROOTS:
        if not root.exists():
            continue
        for path in root.rglob("*.py"):
            if any(part in EXCLUDE_PARTS for part in path.relative_to(ROOT).parts):
                continue
            files.append(path)
    return sorted(files)


def main() -> int:
    results = []
    ok = True
    for path in discover():
        rel = path.relative_to(ROOT).as_posix()
        try:
            py_compile.compile(str(path), doraise=True)
            results.append({"path": rel, "ok": True})
        except py_compile.PyCompileError as exc:
            ok = False
            results.append({"path": rel, "ok": False, "error": str(exc)})
    OUT.mkdir(parents=True, exist_ok=True)
    payload = {"generatedUtc": datetime.now(timezone.utc).isoformat(), "ok": ok, "files": results}
    (OUT / "python-static.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n")
    lines = ["# Python Static Compile", "", f"Generated: `{payload['generatedUtc']}`", "", "| File | OK |", "| --- | --- |"]
    lines.extend(f"| `{item['path']}` | {item['ok']} |" for item in results)
    (OUT / "python-static.md").write_text("\n".join(lines) + "\n")
    print(f"Checked {len(results)} Python files")
    print(f"Wrote {(OUT / 'python-static.json').relative_to(ROOT)}")
    print(f"Wrote {(OUT / 'python-static.md').relative_to(ROOT)}")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
