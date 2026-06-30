#!/usr/bin/env python3
"""Consolidate phase markdown and generate an offline optimization/security scan."""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PHASE_OUTPUT = ROOT / "docs/phases/CONSOLIDATED_PHASE_MARKDOWN.md"
DEFAULT_REVIEW_OUTPUT = ROOT / "docs/optimization/AI_PERFORMANCE_SECURITY_REVIEW.md"
GENERATED_RELATIVE = {
    "docs/phases/CONSOLIDATED_PHASE_MARKDOWN.md",
    "docs/optimization/AI_PERFORMANCE_SECURITY_REVIEW.md",
}
SKIP_DIRS = {".git", "bin", "obj", "build", "dist", "node_modules", ".vs", ".idea", "__pycache__"}
TEXT_SUFFIXES = {
    ".cs": "C#", ".csproj": "C# project", ".sln": "Solution", ".xaml": "WinUI/WPF XAML",
    ".cpp": "C++", ".cc": "C++", ".cxx": "C++", ".h": "C/C++ header", ".hpp": "C++ header",
    ".fs": "F#", ".fsproj": "F# project", ".py": "Python", ".ps1": "PowerShell",
    ".bicep": "Bicep", ".yml": "YAML", ".yaml": "YAML", ".json": "JSON", ".md": "Markdown",
    ".txt": "Text", ".props": "MSBuild", ".targets": "MSBuild", ".xml": "XML",
}
FOCUS_TERMS = ["helios-control", "hermes-fleet-production", "hermes", "aihub", "llm", "xcore", "azure cli"]
PATTERNS = {
    "security": [r"secret\s*[=:]", r"password\s*[=:]", r"token\s*[=:]", r"apikey|api_key", r"credential", r"redact"],
    "performance": [r"performance", r"optimi[sz]", r"cache", r"ring\s*buffer", r"latency", r"throughput"],
    "ai_llm": [r"\baihub\b", r"\bllm\b", r"openai", r"copilot", r"model routing", r"hermes"],
    "azure": [r"\baz\s+", r"azure cli", r"az webapp", r"bicep", r"key vault", r"managed identity"],
    "parallelism": [r"parallel", r"concurrent", r"Task\.WhenAll", r"CancellationToken", r"async", r"thread"],
}
BACKLOG = [
    ("P0", "Secret redaction", "Route every scan/log excerpt through redact_excerpt and add tests for password/token/key formats."),
    ("P0", "Azure guardrails", "Wrap Azure CLI usage with dry-run, subscription/resource-group confirmation, managed identity defaults, and deny-by-default production prompts."),
    ("P1", "AIHub/LLM routing", "Centralize provider selection, local/offline fallback, timeout budgets, and per-provider telemetry for HELIOS/Hermes workflows."),
    ("P1", "Async/cancellation", "Require CancellationToken propagation for long-running C#, F#, C++, and Python orchestration paths."),
    ("P1", "Ring buffers", "Use bounded ring buffers for hot telemetry/log streams to protect UI and agents from unbounded growth."),
    ("P2", "Parallel execution", "Document safe parallel branches, shared-state ownership, and deterministic merge/review gates before increasing concurrency."),
]

@dataclass(frozen=True)
class Finding:
    category: str
    path: str
    line: int
    excerpt: str


def rel(path: Path) -> str:
    try:
        return path.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def display(path: Path) -> str:
    return rel(path) if path.resolve().is_relative_to(ROOT) else path.as_posix()


def iter_files() -> Iterable[Path]:
    for path in sorted(ROOT.rglob("*"), key=lambda p: rel(p).lower()):
        if path.is_dir():
            continue
        parts = set(path.relative_to(ROOT).parts[:-1])
        if parts & SKIP_DIRS:
            continue
        if rel(path) in GENERATED_RELATIVE:
            continue
        if path.suffix.lower() in TEXT_SUFFIXES:
            yield path


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return ""


def redact_excerpt(text: str) -> str:
    patterns = [
        (r"(?i)(password|passwd|pwd)\s*([:=])\s*[\"']?([^\s,'\";]+)[\"']?", r"\1\2[REDACTED]"),
        (r"(?i)(secret|token|api[_-]?key|client[_-]?secret)\s*([:=])\s*[\"']?([^\s,'\";]+)[\"']?", r"\1\2[REDACTED]"),
        (r"sk-[A-Za-z0-9_-]{6,}", "sk-[REDACTED]"),
        (r"(?i)(Bearer\s+)[A-Za-z0-9._~+/=-]{12,}", r"\1[REDACTED]"),
        (r"(?i)(DefaultEndpointsProtocol=https;AccountName=)[^;]+(;AccountKey=)[^;]+", r"\1[REDACTED]\2[REDACTED]"),
    ]
    redacted = text.strip().replace("|", "\\|")
    for pattern, repl in patterns:
        redacted = re.sub(pattern, repl, redacted)
    return redacted[:220]


def phase_files() -> list[Path]:
    files = []
    for path in iter_files():
        name = rel(path).lower()
        if path.suffix.lower() == ".md" and ("phase" in name or "stream" in name or "hermes" in name):
            files.append(path)
    return files


def write_phase_output(output: Path) -> None:
    files = phase_files()
    lines = ["# Consolidated Phase Markdown", "", "Generated deterministically from repository phase, stream, and Hermes markdown files.", "", f"Source files: {len(files)}", ""]
    for path in files:
        text = read_text(path).strip()
        digest = hashlib.sha256(text.encode("utf-8", errors="replace")).hexdigest()[:12]
        lines += ["---", "", f"## {rel(path)}", "", f"SHA256: `{digest}`", ""]
        lines.append(text if text else "_Empty file._")
        lines.append("")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def scan_repo() -> tuple[Counter, dict[str, list[Finding]], list[Finding]]:
    language_counts: Counter[str] = Counter()
    categorized: dict[str, list[Finding]] = defaultdict(list)
    focus: list[Finding] = []
    compiled = {k: [re.compile(p, re.I) for p in pats] for k, pats in PATTERNS.items()}
    focus_re = re.compile("|".join(re.escape(t) for t in FOCUS_TERMS), re.I)
    for path in iter_files():
        language_counts[TEXT_SUFFIXES[path.suffix.lower()]] += 1
        for idx, line in enumerate(read_text(path).splitlines(), 1):
            if focus_re.search(line) and len(focus) < 80:
                focus.append(Finding("focus", rel(path), idx, redact_excerpt(line)))
            for category, regexes in compiled.items():
                if len(categorized[category]) >= 80:
                    continue
                if any(rx.search(line) for rx in regexes):
                    categorized[category].append(Finding(category, rel(path), idx, redact_excerpt(line)))
    return language_counts, categorized, focus


def table_findings(findings: list[Finding], limit: int = 20) -> list[str]:
    rows = ["| File | Line | Excerpt |", "| --- | ---: | --- |"]
    for item in findings[:limit]:
        rows.append(f"| `{item.path}` | {item.line} | {item.excerpt} |")
    if len(rows) == 2:
        rows.append("| _None found_ |  |  |")
    return rows


def write_review_output(output: Path, phase_output: Path = DEFAULT_PHASE_OUTPUT) -> None:
    language_counts, categorized, focus = scan_repo()
    data = {
        "language_counts": dict(sorted(language_counts.items())),
        "finding_counts": {k: len(v) for k, v in sorted(categorized.items())},
        "focus_count": len(focus),
        "backlog": BACKLOG,
    }
    lines = [
        "# AI, Performance, Azure, and Security Review", "",
        "Offline deterministic scan; no cloud credentials or network calls are required.", "",
        f"Phase roll-up output: `{display(phase_output)}`", "",
        "## Language inventory", "", "| Language | Files |", "| --- | ---: |",
    ]
    for language, count in sorted(language_counts.items()):
        lines.append(f"| {language} | {count} |")
    lines += ["", "## Focus-term samples", ""] + table_findings(focus, 25)
    for category in sorted(PATTERNS):
        lines += ["", f"## {category.replace('_', '/').title()} opportunities", ""] + table_findings(categorized[category], 25)
    lines += ["", "## Prioritized backlog", "", "| Priority | Area | Action |", "| --- | --- | --- |"]
    for priority, area, action in BACKLOG:
        lines.append(f"| {priority} | {area} | {action} |")
    lines += ["", "## Machine-readable summary", "", "```json", json.dumps(data, indent=2, sort_keys=True), "```", ""]
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--phase-output", type=Path, default=DEFAULT_PHASE_OUTPUT)
    parser.add_argument("--review-output", type=Path, default=DEFAULT_REVIEW_OUTPUT)
    args = parser.parse_args()
    write_phase_output(args.phase_output)
    write_review_output(args.review_output, args.phase_output)
    print(f"wrote {display(args.phase_output)}")
    print(f"wrote {display(args.review_output)}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
