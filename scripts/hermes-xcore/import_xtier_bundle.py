#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
from pathlib import Path


MARKER_RE = re.compile(r"^(FILE|MODULE):\s+(.+)$")


def clean_name(raw: str) -> str:
    name = raw.split("—", 1)[0].strip()
    name = name.split("(", 1)[0].strip()
    return name


def map_target(repo_root: Path, marker_kind: str, marker_value: str) -> Path:
    value = clean_name(marker_value)
    lowered = value.lower()

    if lowered.startswith(".wslconfig"):
        return repo_root / "config" / "x-tier" / "imported" / "wslconfig.mode-c-b.conf"

    if marker_kind == "MODULE":
        if not lowered.endswith(".py"):
            value = value + ".py"
        return repo_root / "python" / "x-tier" / "hermes_xcore" / "imported" / value

    # FILE mapping
    suffix = Path(value).suffix.lower()
    if suffix == ".ps1":
        return repo_root / "scripts" / "x-tier" / "imported" / Path(value).name
    if suffix == ".sql":
        return repo_root / "sql-learning" / "imported" / Path(value).name
    if suffix in {".json", ".yaml", ".yml", ".wsb"}:
        return repo_root / "config" / "x-tier" / "imported" / Path(value).name
    if Path(value).name.lower() == "requirements.txt":
        return repo_root / "python" / "x-tier" / "imported" / "requirements.txt"
    return repo_root / "docs" / "x-tier" / "imported" / Path(value).name


def extract_sections(lines: list[str]) -> list[tuple[str, str, list[str]]]:
    markers: list[tuple[int, str, str]] = []
    for i, line in enumerate(lines):
        m = MARKER_RE.match(line.strip())
        if m:
            markers.append((i, m.group(1), m.group(2)))

    sections: list[tuple[str, str, list[str]]] = []
    for idx, (start, kind, value) in enumerate(markers):
        end = markers[idx + 1][0] if idx + 1 < len(markers) else len(lines)
        body = lines[start + 1 : end]
        # Trim empty margins.
        while body and not body[0].strip():
            body.pop(0)
        while body and not body[-1].strip():
            body.pop()
        sections.append((kind, value, body))
    return sections


def main() -> int:
    parser = argparse.ArgumentParser(description="Import FILE/MODULE markers from pasted X-Tier bundle.")
    parser.add_argument("--source", required=True, help="Path to pasted text bundle.")
    parser.add_argument("--repo-root", default=".", help="Repository root path.")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    src = Path(args.source).resolve()
    text = src.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    sections = extract_sections(lines)

    # Persist raw source copy
    raw_dir = repo_root / "docs" / "x-tier" / "imported"
    raw_dir.mkdir(parents=True, exist_ok=True)
    raw_copy = raw_dir / src.name
    raw_copy.write_text(text, encoding="utf-8")

    written = 0
    for kind, value, body in sections:
        target = map_target(repo_root, kind, value)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text("\n".join(body).strip() + "\n", encoding="utf-8")
        written += 1
        print(f"[written] {target}")

    print(f"Imported {written} sections from {src}")
    print(f"Raw source saved at {raw_copy}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

