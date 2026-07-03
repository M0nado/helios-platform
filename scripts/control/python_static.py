#!/usr/bin/env python3
from __future__ import annotations

import json
import py_compile
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SEARCH_ROOTS = ("scripts", "tools")
EXCLUDED_DIRS = {"__pycache__", ".venv", ".tools", "bin", "obj"}
OUT_DIR = ROOT / "reports" / "python-static"
JSON_OUT = OUT_DIR / "python-static.json"
MD_OUT = OUT_DIR / "python-static.md"


def is_excluded(path: Path) -> bool:
    try:
        rel = path.relative_to(ROOT)
    except ValueError:
        rel = path
    return any(part in EXCLUDED_DIRS for part in rel.parts)


def discover_python_files() -> list[Path]:
    files: list[Path] = []
    for root_name in SEARCH_ROOTS:
        root = ROOT / root_name
        if not root.exists():
            continue
        for path in root.rglob("*.py"):
            if path.is_file() and not is_excluded(path):
                files.append(path)
    return sorted(files, key=lambda p: p.relative_to(ROOT).as_posix())


def compile_file(path: Path) -> dict[str, object]:
    rel = path.relative_to(ROOT).as_posix()
    try:
        py_compile.compile(str(path), doraise=True, quiet=1)
    except py_compile.PyCompileError as exc:
        return {"path": rel, "status": "fail", "error": exc.msg}
    except Exception as exc:  # pragma: no cover - defensive filesystem/runtime failure reporting
        return {"path": rel, "status": "fail", "error": f"{type(exc).__name__}: {exc}"}
    return {"path": rel, "status": "pass"}


def write_reports(results: list[dict[str, object]]) -> None:
    generated = datetime.now(timezone.utc).isoformat()
    failed = [result for result in results if result["status"] != "pass"]
    payload = {
        "generatedUtc": generated,
        "searchRoots": list(SEARCH_ROOTS),
        "excludedDirectories": sorted(EXCLUDED_DIRS),
        "fileCount": len(results),
        "passed": len(results) - len(failed),
        "failed": len(failed),
        "results": results,
    }
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    JSON_OUT.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    lines = [
        "# Python Static Compile Report",
        "",
        f"Generated: `{generated}`",
        "",
        f"Files compiled: **{payload['fileCount']}**",
        f"Passed: **{payload['passed']}**",
        f"Failed: **{payload['failed']}**",
        "",
        "## Scope",
        "",
        "Search roots: " + ", ".join(f"`{root}`" for root in SEARCH_ROOTS),
        "Excluded directories: " + ", ".join(f"`{item}`" for item in sorted(EXCLUDED_DIRS)),
        "",
        "## Results",
        "",
        "| Status | File | Error |",
        "| --- | --- | --- |",
    ]
    for result in results:
        status = "✅" if result["status"] == "pass" else "❌"
        error = str(result.get("error", "")).replace("|", "\\|").replace("\n", "<br>")
        lines.append(f"| {status} | `{result['path']}` | {error} |")
    MD_OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    results = [compile_file(path) for path in discover_python_files()]
    write_reports(results)
    print(f"Wrote {JSON_OUT.relative_to(ROOT)}")
    print(f"Wrote {MD_OUT.relative_to(ROOT)}")
    failed = sum(1 for result in results if result["status"] != "pass")
    if failed:
        print(f"Python compile check failed for {failed} file(s)", file=sys.stderr)
        return 1
    print(f"Python compile check passed for {len(results)} file(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
