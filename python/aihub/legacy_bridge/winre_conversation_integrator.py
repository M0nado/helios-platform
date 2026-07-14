from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


COMPONENT_PATTERNS: dict[str, list[str]] = {
    "winre": [r"\bwinre\b", r"\breagentc\b", r"\bdism\b"],
    "vhdx": [r"\bvhdx\b", r"\bdev\s*drive\b", r"\bbitlocker\b", r"\brefs\b"],
    "security": [r"\bfirewall\b", r"\bentra\b", r"\bpurview\b", r"\bquarantine\b"],
    "networking": [r"\bport\b", r"\bproxy\b", r"\bvpn\b", r"\bethernet\b", r"\bwifi\b"],
    "ai_ml": [r"\bai\b", r"\bml\b", r"\bllm\b", r"\bmeta[- ]learning\b", r"\banomaly\b"],
    "runtime": [r"\bdocker\b", r"\bgui\b", r"\bapi\b", r"\bweb app\b", r"\bhermes\b", r"\baihub\b"],
    "optimization": [r"\bcpu\b", r"\bram\b", r"\bgpu\b", r"\bcompression\b", r"\bthroughput\b"],
}


def classify(text: str) -> list[str]:
    lowered = text.lower()
    return [tag for tag, patterns in COMPONENT_PATTERNS.items() if any(re.search(pattern, lowered) for pattern in patterns)]


def parse_transcript(lines: list[str]) -> dict:
    turns: list[dict] = []
    current_role: str | None = None
    current_lines: list[str] = []

    def flush() -> None:
        nonlocal current_role, current_lines
        if not current_role:
            return
        text = "\n".join(current_lines).strip()
        if text:
            turns.append({"role": current_role, "text": text, "tags": classify(text)})
        current_role = None
        current_lines = []

    for raw in lines:
        line = raw.rstrip()
        if line == "You said":
            flush()
            current_role = "user"
            continue
        if line == "Copilot said":
            flush()
            current_role = "assistant"
            continue
        if current_role:
            current_lines.append(line)
    flush()

    user_turns = [turn for turn in turns if turn["role"] == "user"]
    assistant_turns = [turn for turn in turns if turn["role"] == "assistant"]
    requirements = [
        {"id": f"req-{index:03d}", "summary": turn["text"].splitlines()[0][:200].strip(), "tags": turn["tags"]}
        for index, turn in enumerate(user_turns, start=1)
    ]
    milestones = [
        {"id": f"ms-{index:03d}", "summary": turn["text"].splitlines()[0][:200], "tags": turn["tags"]}
        for index, turn in enumerate(assistant_turns, start=1)
    ]
    component_coverage = {key: 0 for key in COMPONENT_PATTERNS}
    for turn in turns:
        for tag in turn["tags"]:
            component_coverage[tag] += 1

    return {
        "summary": {
            "total_turns": len(turns),
            "user_turns": len(user_turns),
            "assistant_turns": len(assistant_turns),
            "line_count": len(lines),
        },
        "component_coverage": component_coverage,
        "requirements": requirements,
        "milestones": milestones,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Convert a WinRE transcript into AIHub integration artifacts.")
    parser.add_argument("--source", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    source = Path(args.source)
    output = Path(args.output)
    payload = parse_transcript(source.read_text(encoding="utf-8", errors="replace").splitlines())
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"Wrote: {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
