#!/usr/bin/env python3
"""Preview or create the canonical HELIOS repository-organization backlog."""

from __future__ import annotations

import argparse
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BACKLOG = ROOT / "config" / "repository-backlog.json"


def command(arguments: list[str], *, capture: bool = False) -> str:
    result = subprocess.run(
        arguments, check=True, text=True, capture_output=capture
    )
    return result.stdout.strip() if capture else ""


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true", help="Create missing GitHub issues")
    parser.add_argument("--repo", help="OWNER/REPO; defaults to the gh CLI repository")
    args = parser.parse_args()

    document = json.loads(BACKLOG.read_text(encoding="utf-8"))
    tasks = document["tasks"]
    if [task["priority"] for task in tasks] != list(range(1, 16)):
        raise SystemExit("Backlog priorities must be unique and contiguous from 1 to 15")

    repo_args = ["--repo", args.repo] if args.repo else []
    for task in tasks:
        marker = f"[{task['id']}]"
        title = f"{marker} {task['title']}"
        print(f"{task['priority']:02d}. {title} -> {task['lane']}")
        if not args.apply:
            continue

        existing = command(
            ["gh", "issue", "list", *repo_args, "--state", "all", "--search", marker,
             "--json", "number", "--jq", "length"], capture=TrueWhen a different issue, body, or comment merely references a backlog marker such as [ORG-001], this broad search can return that issue and make int(existing) nonzero, causing the script to skip creation even when the canonical backlog issue is absent. The CLI documents --search as a general issue-search query, and GitHub's [issue-search documentation](https://docs.github.com/en/search-github/searching-on-github/searching-issues-and-pull-requests) explains that unqualified terms search issue content rather than exact titles; retrieve candidate titles and compare the complete generated title exactly before declaring the task present.
        )
        if int(existing):
            print("    already exists")
            continue
        body = (
            f"Delivery lane: `{task['lane']}`\n\n"
            f"## Acceptance criteria\n\n- [ ] {task['acceptance']}\n\n"
            "## Completion gates\n\n"
            "- [ ] Affected builds/tests and integration contracts pass.\n"
            "- [ ] Security and rollback impact is documented.\n"
            "- [ ] Evidence links and correlation IDs are preserved.\n"
        )
        command(["gh", "issue", "create", *repo_args, "--title", title,
                 "--body", body, "--label", "repository-reset"])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
