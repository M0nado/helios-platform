#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT_JSON = ROOT / "reports/learning/dual-repo-learning-preflight.json"
OUT_MD = ROOT / "reports/learning/dual-repo-learning-preflight.md"
TRACKED_EXTS = {".cs", ".fs", ".cpp", ".h", ".py", ".md", ".yml", ".yaml", ".json", ".bicep", ".sh", ".ps1"}


def git_files(repo: Path) -> list[str]:
    result = subprocess.run(
        ["git", "ls-files"],
        cwd=repo,
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode != 0:
        return []
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def summarize_repo(repo: Path) -> dict[str, object]:
    files = git_files(repo)
    ext_counts: Counter[str] = Counter()
    tracked = 0
    for file in files:
        ext = Path(file).suffix.lower()
        if ext in TRACKED_EXTS:
            tracked += 1
            ext_counts[ext] += 1
    return {
        "path": str(repo),
        "totalFiles": len(files),
        "trackedLearningFiles": tracked,
        "extensionCounts": dict(ext_counts.most_common()),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate dual-repository pre-absorption learning report.")
    parser.add_argument("--primary", required=True, help="Primary repository copy path (mutable branch source).")
    parser.add_argument("--secondary", required=True, help="Secondary repository copy path (read-only learning source).")
    args = parser.parse_args()

    primary = Path(args.primary).resolve()
    secondary = Path(args.secondary).resolve()

    primary_files = set(git_files(primary))
    secondary_files = set(git_files(secondary))
    shared = sorted(primary_files & secondary_files)
    primary_only = sorted(primary_files - secondary_files)
    secondary_only = sorted(secondary_files - primary_files)

    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "policy": {
            "mutationTarget": str(primary),
            "learningOnlySource": str(secondary),
            "note": "Primary copy is the only mutation target; secondary copy is read-only for cross-repository learning context.",
        },
        "primarySummary": summarize_repo(primary),
        "secondarySummary": summarize_repo(secondary),
        "comparison": {
            "sharedFileCount": len(shared),
            "primaryOnlyCount": len(primary_only),
            "secondaryOnlyCount": len(secondary_only),
            "sharedSample": shared[:50],
            "primaryOnlySample": primary_only[:50],
            "secondaryOnlySample": secondary_only[:50],
        },
    }

    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    OUT_JSON.write_text(json.dumps(payload, indent=2) + "\n")

    lines = [
        "# Dual Repository Learning Preflight",
        "",
        f"Generated: `{payload['generatedUtc']}`",
        "",
        "## Policy",
        f"- Mutation target: `{payload['policy']['mutationTarget']}`",
        f"- Learning-only source: `{payload['policy']['learningOnlySource']}`",
        f"- {payload['policy']['note']}",
        "",
        "## Comparison summary",
        f"- Shared files: **{payload['comparison']['sharedFileCount']}**",
        f"- Primary-only files: **{payload['comparison']['primaryOnlyCount']}**",
        f"- Secondary-only files: **{payload['comparison']['secondaryOnlyCount']}**",
        "",
        "## Shared file sample",
    ]
    lines += [f"- `{path}`" for path in payload["comparison"]["sharedSample"][:25]]
    lines += ["", "## Primary-only file sample"]
    lines += [f"- `{path}`" for path in payload["comparison"]["primaryOnlySample"][:25]]
    lines += ["", "## Secondary-only file sample"]
    lines += [f"- `{path}`" for path in payload["comparison"]["secondaryOnlySample"][:25]]

    OUT_MD.write_text("\n".join(lines) + "\n")
    print(f"Wrote {OUT_JSON.relative_to(ROOT)}")
    print(f"Wrote {OUT_MD.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
