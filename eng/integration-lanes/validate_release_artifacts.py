#!/usr/bin/env python3
"""Reject preview lane files and prerelease-named content from release output."""
from __future__ import annotations

import argparse
import re
import tarfile
import zipfile
from pathlib import Path

FORBIDDEN = re.compile(r"(?:^|[._/-])(preview|prerelease|alpha|beta|rc)(?:[._/-]|\d|$)", re.I)

def inspect_name(name: str, failures: list[str]) -> None:
    normalized = name.replace("\\", "/")
    if "eng/integration-lanes/preview/" in normalized or FORBIDDEN.search(normalized):
        failures.append(normalized)

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("artifact", type=Path)
    args = parser.parse_args()
    if not args.artifact.exists():
        parser.error(f"artifact path does not exist: {args.artifact}")
    files = [args.artifact] if args.artifact.is_file() else [path for path in args.artifact.rglob("*") if path.is_file()]
    failures: list[str] = []
    for path in files:
        inspect_name(path.relative_to(args.artifact.parent).as_posix(), failures)
        try:
            if zipfile.is_zipfile(path):
                with zipfile.ZipFile(path) as archive:
                    for name in archive.namelist():
                        inspect_name(f"{path.name}:{name}", failures)
            elif tarfile.is_tarfile(path):
                with tarfile.open(path) as archive:
                    for member in archive.getmembers():
                        inspect_name(f"{path.name}:{member.name}", failures)
        except (OSError, tarfile.TarError, zipfile.BadZipFile) as error:
            failures.append(f"uninspectable archive {path}: {error}")
    if failures:
        print("Preview content found in release artifact:\n" + "\n".join(sorted(set(failures))))
        return 1
    print(f"Release artifact inspection passed for {args.artifact}.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
