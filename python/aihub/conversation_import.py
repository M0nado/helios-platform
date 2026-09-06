from __future__ import annotations

import argparse
from collections import Counter
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
from typing import Iterable, Sequence


ROLE_MARKERS = {
    "you said": "user",
    "user said": "user",
    "assistant said": "assistant",
    "chatgpt said": "assistant",
    "copilot said": "assistant",
    "claude said": "assistant",
}

COMPONENT_PATTERNS: dict[str, tuple[re.Pattern[str], ...]] = {
    "windows": tuple(re.compile(pattern, re.I) for pattern in (r"\bwindows\b", r"\bwinre\b", r"\bdism\b", r"\bdefender\b")),
    "storage": tuple(re.compile(pattern, re.I) for pattern in (r"\bvhdx\b", r"\bdev\s*drive\b", r"\bbitlocker\b", r"\bpartition\b")),
    "security": tuple(re.compile(pattern, re.I) for pattern in (r"\bfirewall\b", r"\bentra\b", r"\bpurview\b", r"\bquarantine\b", r"\brootkit\b")),
    "networking": tuple(re.compile(pattern, re.I) for pattern in (r"\bproxy\b", r"\bvpn\b", r"\bethernet\b", r"\bwifi\b", r"\boidc\b")),
    "ai-ml": tuple(re.compile(pattern, re.I) for pattern in (r"\baihub\b", r"\bllm\b", r"\bmodel\b", r"\btraining\b", r"\banomaly\b")),
    "runtime": tuple(re.compile(pattern, re.I) for pattern in (r"\bdocker\b", r"\bapi\b", r"\bmcp\b", r"\bhermes\b", r"\bxcore\b")),
    "azure": tuple(re.compile(pattern, re.I) for pattern in (r"\bazure\b", r"\bbicep\b", r"\bkey vault\b", r"\bfoundry\b")),
    "github": tuple(re.compile(pattern, re.I) for pattern in (r"\bgithub\b", r"\bpull request\b", r"\bworkflow\b", r"\bcodex\b")),
    "collaboration": tuple(re.compile(pattern, re.I) for pattern in (r"\bslack\b", r"\blinear\b", r"\bsharepoint\b", r"\bteams\b")),
    "ui": tuple(re.compile(pattern, re.I) for pattern in (r"\bwinui\b", r"\bgui\b", r"\bprofile wheel\b", r"\bmonado\b")),
}

_SECRET_PATTERNS: tuple[re.Pattern[str], ...] = (
    re.compile(r"sk-(?:proj-)?[A-Za-z0-9_-]{20,}"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{20,}"),
    re.compile(r"github_pat_[A-Za-z0-9_]{20,}"),
    re.compile(r"xox[baprs]-[A-Za-z0-9-]{20,}"),
    re.compile(r"(?i)(authorization\s*:\s*bearer\s+)[A-Za-z0-9._~+/-]{16,}=*"),
    re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----.*?-----END (?:RSA |EC |OPENSSH )?PRIVATE KEY-----", re.S),
)


@dataclass(frozen=True, slots=True)
class ConversationTurn:
    id: str
    source: str
    ordinal: int
    role: str
    text: str
    tags: tuple[str, ...]


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def redact_text(text: str) -> tuple[str, int]:
    redacted = text
    count = 0
    for pattern in _SECRET_PATTERNS:
        redacted, substitutions = pattern.subn("[REDACTED_SECRET]", redacted)
        count += substitutions
    return redacted, count


def classify(text: str) -> tuple[str, ...]:
    return tuple(
        tag
        for tag, patterns in COMPONENT_PATTERNS.items()
        if any(pattern.search(text) for pattern in patterns)
    )


def _turn_id(source: str, ordinal: int, role: str, text: str) -> str:
    material = f"{source}\0{ordinal}\0{role}\0{text}".encode("utf-8")
    return hashlib.sha256(material).hexdigest()[:20]


def parse_transcript(lines: Iterable[str], *, source: str) -> tuple[list[ConversationTurn], int]:
    turns: list[ConversationTurn] = []
    current_role: str | None = None
    current_lines: list[str] = []
    redaction_count = 0

    def flush() -> None:
        nonlocal current_role, current_lines, redaction_count
        if current_role is None:
            current_lines = []
            return
        raw_text = "\n".join(current_lines).strip()
        current_lines = []
        if not raw_text:
            current_role = None
            return
        text, substitutions = redact_text(raw_text)
        redaction_count += substitutions
        ordinal = len(turns) + 1
        turns.append(
            ConversationTurn(
                id=_turn_id(source, ordinal, current_role, text),
                source=source,
                ordinal=ordinal,
                role=current_role,
                text=text,
                tags=classify(text),
            )
        )
        current_role = None

    for raw in lines:
        line = raw.rstrip("\r\n")
        marker = ROLE_MARKERS.get(line.strip().lower())
        if marker:
            flush()
            current_role = marker
            continue
        if current_role is not None:
            current_lines.append(line)
    flush()
    return turns, redaction_count


def _normalized_summary(text: str) -> str:
    first = next((line.strip() for line in text.splitlines() if line.strip()), "")
    collapsed = re.sub(r"\s+", " ", first).strip()
    return collapsed[:240]


def _summary_key(summary: str) -> str:
    normalized = re.sub(r"[^a-z0-9]+", " ", summary.lower()).strip()
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:16]


def merge_transcript_payloads(
    sources: Sequence[tuple[str, Iterable[str]]],
) -> dict:
    all_turns: list[ConversationTurn] = []
    source_records: list[dict] = []
    total_redactions = 0

    for source_name, lines in sources:
        material = list(lines)
        source_bytes = "\n".join(line.rstrip("\r\n") for line in material).encode("utf-8")
        turns, redactions = parse_transcript(material, source=source_name)
        all_turns.extend(turns)
        total_redactions += redactions
        source_records.append(
            {
                "name": source_name,
                "sha256": hashlib.sha256(source_bytes).hexdigest(),
                "turnCount": len(turns),
                "secretRedactions": redactions,
            }
        )

    requirements: list[dict] = []
    milestones: list[dict] = []
    requirement_counts: Counter[str] = Counter()
    requirement_display: dict[str, str] = {}
    coverage: Counter[str] = Counter()

    for turn in all_turns:
        coverage.update(turn.tags)
        summary = _normalized_summary(turn.text)
        if not summary:
            continue
        if turn.role == "user":
            key = _summary_key(summary)
            requirement_counts[key] += 1
            requirement_display.setdefault(key, summary)
            requirements.append(
                {
                    "id": f"req-{turn.id}",
                    "source": turn.source,
                    "summary": summary,
                    "tags": list(turn.tags),
                }
            )
        elif turn.role == "assistant":
            milestones.append(
                {
                    "id": f"ms-{turn.id}",
                    "source": turn.source,
                    "summary": summary,
                    "tags": list(turn.tags),
                }
            )

    unique_requirements = [
        {
            "id": key,
            "summary": requirement_display[key],
            "count": count,
        }
        for key, count in sorted(
            requirement_counts.items(),
            key=lambda item: (-item[1], requirement_display[item[0]].lower()),
        )
    ]

    return {
        "schemaVersion": 2,
        "generatedUtc": utc_now(),
        "scope": "user-supplied-or-exported-transcript-text-only",
        "privateReasoningImported": False,
        "sourceConversationObjectsMerged": False,
        "sources": source_records,
        "summary": {
            "sourceCount": len(source_records),
            "turnCount": len(all_turns),
            "userTurnCount": sum(turn.role == "user" for turn in all_turns),
            "assistantTurnCount": sum(turn.role == "assistant" for turn in all_turns),
            "secretRedactions": total_redactions,
        },
        "componentCoverage": dict(sorted(coverage.items())),
        "requirements": requirements,
        "uniqueRequirements": unique_requirements,
        "milestones": milestones,
        "turns": [asdict(turn) for turn in all_turns],
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Merge exported HELIOS chat transcripts into a redacted control record."
    )
    parser.add_argument("--source", action="append", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    sources = [
        (path.name, path.read_text(encoding="utf-8", errors="replace").splitlines())
        for path in args.source
    ]
    payload = merge_transcript_payloads(sources)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "output": str(args.output),
                "sources": payload["summary"]["sourceCount"],
                "turns": payload["summary"]["turnCount"],
                "secretRedactions": payload["summary"]["secretRedactions"],
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
