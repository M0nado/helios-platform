#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
from pathlib import Path

PATTERNS = [
    re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{30,}"),
    re.compile(r"xox[baprs]-[A-Za-z0-9-]{20,}"),
    re.compile(r"(?i)(?:client[_-]?secret|api[_-]?key|password)\s*[:=]\s*['\"][^$<{\s][^'\"]{11,}['\"]"),
]
IGNORED_PARTS = {".git", ".venv", "__pycache__", "bin", "obj", "artifacts"}


def main() -> int:
    parser = argparse.ArgumentParser(description="Reject recognizable credential values.")
    parser.add_argument("root", nargs="?", default=".")
    args = parser.parse_args()
    root = Path(args.root).resolve()
    findings: list[str] = []
    for path in root.rglob("*"):
        if not path.is_file() or any(part in IGNORED_PARTS for part in path.parts):
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError):
            continue
        for number, line in enumerate(text.splitlines(), 1):
            if any(pattern.search(line) for pattern in PATTERNS):
                findings.append(f"{path.relative_to(root)}:{number}")
    if findings:
        print("\n".join(findings))
        return 2
    print("No recognizable secret values found.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

