#!/usr/bin/env python3
"""Render reviewable GitHub issue packets without mutating GitHub."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BACKLOG = ROOT / "config" / "capabilities" / "major-capabilities.v1.json"


def issue_body(item: dict, defaults: dict) -> str:
    dependencies = item["dependencies"] or ["None"]
    criteria = "\n".join(f"- [ ] {value}" for value in item["acceptanceCriteria"])
    checks = "\n".join(f"- [ ] `{value}`" for value in defaults["checks"])
    evidence = "\n".join(f"- [ ] {value}" for value in defaults["evidence"])
    return f"""## Outcome

{item['outcome']}

## Routing

- Capability: `{item['id']}`
- Category: `{item['category']}`
- Engine owner: `{item['owner']}`
- Priority: `P{item['priority']}`
- Safety gate: `{item['gate']}`
- Dependencies: {', '.join(f'`{value}`' for value in dependencies)}

## Acceptance criteria

{criteria}

## Required checks

{checks}

## Evidence

{evidence}

## Safety

No credentials or raw fleet evidence may be committed. Privileged Azure or Windows work must remain dry-run/what-if until the declared approval gate is satisfied, with rollback evidence attached.
"""


def render_packets(data: dict, category: str | None = None) -> list[tuple[str, str]]:
    items = [item for item in data["capabilities"] if category is None or item["category"] == category]
    return [
        (
            f"{item['id'].lower()}-{item['slug']}.md",
            f"# [{item['id']}] {item['outcome'].rstrip('.')}\n\n" + issue_body(item, data["defaults"]),
        )
        for item in items
    ]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--category", help="render only one category")
    parser.add_argument("--output-dir", type=Path, help="write packets here; otherwise print an index")
    args = parser.parse_args()
    data = json.loads(BACKLOG.read_text(encoding="utf-8"))
    categories = {item["category"] for item in data["capabilities"]}
    if args.category and args.category not in categories:
        parser.error(f"unknown category: {args.category}")
    packets = render_packets(data, args.category)
    if args.output_dir:
        args.output_dir.mkdir(parents=True, exist_ok=True)
        for name, body in packets:
            if not re.fullmatch(r"[a-z0-9-]+\.md", name):
                raise ValueError(f"unsafe output name: {name}")
            (args.output_dir / name).write_text(body, encoding="utf-8")
        print(f"wrote {len(packets)} issue packets to {args.output_dir}")
    else:
        for name, body in packets:
            print(f"{name}: {body.splitlines()[0].removeprefix('# ')}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
