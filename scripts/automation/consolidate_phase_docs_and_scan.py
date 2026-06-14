#!/usr/bin/env python3
"""Consolidate phased Markdown docs and produce an AI/performance/security scan.

The script is intentionally offline and deterministic so it can run in CI without
Azure, GitHub, OpenAI, or local LLM credentials. It does not delete the original
phase documents; instead it builds canonical roll-up documents that operators can
review before archiving or renaming source reports.
"""
from __future__ import annotations

import argparse
import hashlib
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PHASE_OUTPUT = ROOT / "docs" / "phases" / "CONSOLIDATED_PHASE_MARKDOWN.md"
DEFAULT_REVIEW_OUTPUT = ROOT / "docs" / "optimization" / "AI_PERFORMANCE_SECURITY_REVIEW.md"

SKIP_DIRS = {
    ".git",
    ".vs",
    ".vscode",
    "bin",
    "obj",
    "node_modules",
    "packages",
    "artifacts",
    ".pytest_cache",
    "__pycache__",
}

CODE_EXTENSIONS = {
    ".cs": "C#",
    ".xaml": "WinUI/WPF XAML",
    ".cpp": "C++",
    ".cxx": "C++",
    ".cc": "C++",
    ".h": "C/C++ Header",
    ".hpp": "C++ Header",
    ".fs": "F#",
    ".fsi": "F#",
    ".fsx": "F#",
    ".py": "Python",
    ".ps1": "PowerShell",
    ".psm1": "PowerShell",
    ".psd1": "PowerShell",
}

FOCUS_TERMS = (
    "helios-control",
    "hermes-fleet-production",
    "hermes",
    "fleet",
    "aihub",
    "ai hub",
    "llm",
    "xcore",
    "azure",
    "security",
    "parallel",
    "prediction",
    "analytics",
)

OPPORTUNITY_PATTERNS = [
    (
        "security",
        "Potential secret/credential handling",
        re.compile(r"(?i)\b(password|api[_-]?key|secret|credential|access[_-]?token|refresh[_-]?token|bearer)\b"),
        "Confirm values are indirections to environment variables, GitHub secrets, Azure Key Vault, or Windows Credential Manager/DPAPI.",
    ),
    (
        "performance",
        "Blocking wait or synchronous delay",
        re.compile(r"(Thread\.Sleep|\.Wait\(|\.Result\b|Task\.Delay\([^)]*\)\.Wait)"),
        "Prefer async/await with cancellation tokens, especially in UI, orchestration, and tests.",
    ),
    (
        "performance",
        "Unbounded or repeated materialization",
        re.compile(r"\.(ToList|ToArray)\(\)|RemoveAt\(0\)|OrderBy\("),
        "For hot paths, evaluate streaming, bounded queues/ring buffers, pooling, or indexed lookups.",
    ),
    (
        "ai/llm",
        "LLM/AI integration surface",
        re.compile(r"(?i)(openai|azure openai|ollama|llm|model|prompt|tokens|inference|ai hub|aihub)"),
        "Add routing, token budgets, redaction, model fallback, and offline-safe provider health checks.",
    ),
    (
        "azure",
        "Azure CLI/cloud integration surface",
        re.compile(r"(?i)(az\s+|azure|key vault|managed identity|subscription|tenant)"),
        "Gate mutating Azure work behind explicit modes; keep inventory/auth checks read-only by default.",
    ),
    (
        "parallelism",
        "Parallel/concurrency candidate",
        re.compile(r"(?i)(parallel|concurrent|semaphore|task\.run|thread|async|await)"),
        "Review lock granularity, cancellation, and data ownership before increasing parallelism.",
    ),
]


@dataclass(frozen=True)
class PhaseDoc:
    path: Path
    title: str
    phase_key: str
    byte_count: int
    line_count: int
    sha256: str


@dataclass(frozen=True)
class Finding:
    category: str
    label: str
    path: Path
    line: int
    excerpt: str
    recommendation: str


def should_skip(path: Path) -> bool:
    try:
        relative = path.relative_to(ROOT)
    except ValueError:
        relative = path
    generated_outputs = {
        DEFAULT_PHASE_OUTPUT.relative_to(ROOT),
        DEFAULT_REVIEW_OUTPUT.relative_to(ROOT),
    }
    return relative in generated_outputs or any(part in SKIP_DIRS for part in relative.parts)


def iter_files() -> Iterable[Path]:
    for path in ROOT.rglob("*"):
        if path.is_file() and not should_skip(path):
            yield path


def is_phase_markdown(path: Path) -> bool:
    relative = path.relative_to(ROOT).as_posix().lower()
    name = path.name.lower()
    generated_outputs = {
        DEFAULT_PHASE_OUTPUT.relative_to(ROOT).as_posix().lower(),
        DEFAULT_REVIEW_OUTPUT.relative_to(ROOT).as_posix().lower(),
    }
    if relative in generated_outputs:
        return False
    return path.suffix.lower() == ".md" and ("phase" in name or relative.startswith("phases/"))


def first_heading(text: str, fallback: str) -> str:
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("#"):
            return stripped.lstrip("#").strip() or fallback
    return fallback


def phase_sort_key(path: Path) -> tuple[int, str]:
    text = path.relative_to(ROOT).as_posix().lower()
    match = re.search(r"phase[_ -]?(\d+)", text)
    if match:
        return int(match.group(1)), text
    match = re.search(r"phases/(\d+)-", text)
    if match:
        return int(match.group(1)), text
    return 999, text


def collect_phase_docs() -> list[PhaseDoc]:
    docs: list[PhaseDoc] = []
    for path in iter_files():
        if not is_phase_markdown(path):
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        digest = hashlib.sha256(text.encode("utf-8", errors="replace")).hexdigest()
        phase_number, _ = phase_sort_key(path)
        phase_key = f"Phase {phase_number}" if phase_number != 999 else "Cross-phase"
        docs.append(
            PhaseDoc(
                path=path,
                title=first_heading(text, path.stem.replace("_", " ").title()),
                phase_key=phase_key,
                byte_count=path.stat().st_size,
                line_count=len(text.splitlines()),
                sha256=digest,
            )
        )
    return sorted(docs, key=lambda doc: phase_sort_key(doc.path))


def write_phase_consolidation(docs: list[PhaseDoc], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    generated = datetime.now(timezone.utc).replace(microsecond=0).isoformat()
    grouped: dict[str, list[PhaseDoc]] = defaultdict(list)
    for doc in docs:
        grouped[doc.phase_key].append(doc)

    lines = [
        "# Consolidated HELIOS Phase Markdown",
        "",
        f"Generated UTC: `{generated}`",
        "",
        "This generated roll-up combines all Markdown files whose name or path is phase-oriented. The original documents remain in place for historical traceability; this file is the single review surface for phase status, delivery notes, implementation reports, and quick references.",
        "",
        "## Consolidation summary",
        "",
        f"- Source documents: `{len(docs)}`",
        f"- Total source bytes: `{sum(doc.byte_count for doc in docs)}`",
        f"- Total source lines: `{sum(doc.line_count for doc in docs)}`",
        "- Scope: repository Markdown files matching `*phase*.md` plus Markdown under `phases/`.",
        "",
        "## Source document index",
        "",
        "| Phase group | Source | Lines | Bytes | SHA-256 |",
        "| --- | --- | ---: | ---: | --- |",
    ]
    for doc in docs:
        rel = doc.path.relative_to(ROOT).as_posix()
        lines.append(f"| {doc.phase_key} | `{rel}` | {doc.line_count} | {doc.byte_count} | `{doc.sha256[:12]}` |")

    lines.extend(["", "## Phase-group roll-up", ""])
    for phase_key in sorted(grouped, key=lambda key: (999 if key == "Cross-phase" else int(key.split()[1]), key)):
        lines.append(f"### {phase_key}")
        for doc in grouped[phase_key]:
            rel = doc.path.relative_to(ROOT).as_posix()
            lines.append(f"- `{rel}` — {doc.title} ({doc.line_count} lines)")
        lines.append("")

    lines.extend(["", "## Combined source content", ""])
    for index, doc in enumerate(docs, 1):
        rel = doc.path.relative_to(ROOT).as_posix()
        text = doc.path.read_text(encoding="utf-8", errors="replace").rstrip()
        lines.extend([
            "---",
            "",
            f"## {index}. {doc.title}",
            "",
            f"Source: `{rel}`  ",
            f"Phase group: `{doc.phase_key}`  ",
            f"SHA-256: `{doc.sha256}`",
            "",
            text,
            "",
        ])
    output.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def language_inventory(files: list[Path]) -> Counter[str]:
    counts: Counter[str] = Counter({language: 0 for language in sorted(set(CODE_EXTENSIONS.values()))})
    for path in files:
        language = CODE_EXTENSIONS.get(path.suffix.lower())
        if language:
            counts[language] += 1
    return counts


def redact_excerpt(text: str) -> str:
    """Redact likely secret values before writing findings into reports."""
    redacted = re.sub(
        r"(?i)(password|api[_-]?key|secret|credential|access[_-]?token|refresh[_-]?token|bearer)([\s:=\"']+)([^\s,;|)]+)",
        r"\1\2<redacted>",
        text,
    )
    redacted = re.sub(r"(?i)sk-[A-Za-z0-9_-]+", "sk-<redacted>", redacted)
    return redacted


def collect_findings(files: list[Path], max_per_label: int = 16) -> list[Finding]:
    findings: list[Finding] = []
    seen_per_label: Counter[str] = Counter()
    for path in files:
        if path.suffix.lower() not in CODE_EXTENSIONS and path.suffix.lower() not in {".json", ".yml", ".yaml"}:
            continue
        relative = path.relative_to(ROOT).as_posix()
        if relative == DEFAULT_PHASE_OUTPUT.relative_to(ROOT).as_posix():
            continue
        try:
            lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
        except OSError:
            continue
        for line_number, line in enumerate(lines, 1):
            stripped = line.strip()
            if not stripped or stripped.startswith("//") or stripped.startswith("#") and path.suffix.lower() != ".md":
                continue
            for category, label, pattern, recommendation in OPPORTUNITY_PATTERNS:
                key = f"{category}:{label}"
                if seen_per_label[key] >= max_per_label:
                    continue
                if pattern.search(line):
                    findings.append(
                        Finding(
                            category=category,
                            label=label,
                            path=path,
                            line=line_number,
                            excerpt=redact_excerpt(stripped)[:180].replace("|", "\\|"),
                            recommendation=recommendation,
                        )
                    )
                    seen_per_label[key] += 1
    return findings


def focus_inventory(files: list[Path]) -> dict[str, list[str]]:
    matches: dict[str, list[str]] = {term: [] for term in FOCUS_TERMS}
    for path in files:
        rel = path.relative_to(ROOT).as_posix()
        haystack = rel.lower()
        if path.suffix.lower() in {".md", ".cs", ".py", ".ps1", ".psm1", ".json", ".xaml"}:
            try:
                haystack += "\n" + path.read_text(encoding="utf-8", errors="replace").lower()[:20000]
            except OSError:
                pass
        for term in FOCUS_TERMS:
            if term in haystack and len(matches[term]) < 12:
                matches[term].append(rel)
    return {term: paths for term, paths in matches.items() if paths}


def write_review(files: list[Path], phase_docs: list[PhaseDoc], findings: list[Finding], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    generated = datetime.now(timezone.utc).replace(microsecond=0).isoformat()
    languages = language_inventory(files)
    focus = focus_inventory(files)
    category_counts = Counter(f.category for f in findings)

    lines = [
        "# HELIOS AI, Performance, Security, and Cloud Optimization Review",
        "",
        f"Generated UTC: `{generated}`",
        "",
        "This review is an offline repository scan intended to identify high-value follow-up work for AIHub/LLM routing, HELIOS control automation, Hermes fleet production readiness, C#/WinUI front-end stability, C++/F#/Python expansion points, Azure CLI setup, performance, and security.",
        "",
        "## Executive priorities",
        "",
        "1. Treat `docs/phases/CONSOLIDATED_PHASE_MARKDOWN.md` as the single phase-readiness surface before merging or archiving phased reports.",
        "2. Keep branch consolidation safe: inventory `helios-control` and `hermes-fleet-production` remotes/branches before merging, require PR checks, and do not run mutating Azure commands from the default scan path.",
        "3. Harden AIHub and LLM surfaces with secret redaction, provider health checks, token/cost budgets, local fallback, and per-provider telemetry.",
        "4. Convert blocking waits and polling sleeps in hot paths/tests to cancellable async flows; reserve parallelism increases for code paths with clear ownership boundaries.",
        "5. Move credential persistence from in-memory/example storage to Windows Credential Manager/DPAPI or Azure Key Vault-backed implementations before production use.",
        "",
        "## Repository coverage",
        "",
        f"- Files scanned: `{len(files)}`",
        f"- Phase Markdown sources consolidated: `{len(phase_docs)}`",
        "",
        "| Language/surface | Files |",
        "| --- | ---: |",
    ]
    for language, count in sorted(languages.items()):
        lines.append(f"| {language} | {count} |")

    lines.extend(["", "## Focus-area matches", ""])
    for term, paths in focus.items():
        lines.append(f"### `{term}`")
        for path in paths:
            lines.append(f"- `{path}`")
        lines.append("")

    lines.extend([
        "## Opportunity summary",
        "",
        "| Category | Findings sampled |",
        "| --- | ---: |",
    ])
    for category, count in sorted(category_counts.items()):
        lines.append(f"| {category} | {count} |")

    lines.extend(["", "## Sampled findings", "", "| Category | Location | Signal | Recommendation |", "| --- | --- | --- | --- |"])
    for finding in findings:
        rel = finding.path.relative_to(ROOT).as_posix()
        lines.append(f"| {finding.category} | `{rel}:{finding.line}` | {finding.excerpt} | {finding.recommendation} |")

    lines.extend([
        "",
        "## AIHub / LLM optimization backlog",
        "",
        "- Add a provider-router interface that records latency, token usage, cost, redaction status, local/cloud availability, and error class per request.",
        "- Add a deterministic local-model health check path for Ollama/WSL2/Hyper-V so AIHub can degrade gracefully without network credentials.",
        "- Add prompt and response redaction middleware before any cloud provider call; fail closed when high-confidence secrets are detected.",
        "- Persist provider budgets and model preferences in configuration rather than hard-coded UI seed data.",
        "- Create tests for fallback order: local Hermes fleet -> local LLM -> Azure OpenAI -> OpenAI/Anthropic/custom provider.",
        "",
        "## Performance and analytics backlog",
        "",
        "- Replace list-front removals (`RemoveAt(0)`) in time-series hot paths with a ring buffer or deque to avoid O(n) shifts.",
        "- Avoid repeated `ToList()` materialization inside aggregate calculations when one streaming pass can calculate min/max/sum/std-dev inputs.",
        "- Push cancellation tokens through long-running monitoring, orchestration, and AI-provider calls.",
        "- Use bounded channels for parallel telemetry ingestion before introducing additional worker tasks.",
        "- Reserve F# modules for immutable math/prediction kernels that can be unit-tested independently from UI and orchestration code.",
        "",
        "## Security and Azure CLI backlog",
        "",
        "- Keep read-only Azure readiness checks (`az account show`, version discovery) separate from deployment commands.",
        "- Store real secrets only in GitHub Actions secrets, Azure Key Vault, managed identity, Windows Credential Manager, or DPAPI-protected stores.",
        "- Redact `password`, `api_key`, `secret`, `credential`, and `token` values from generated reports.",
        "- Require explicit environment approvals before any workflow runs Azure deployment, branch merge automation, or AI-driven code writes.",
        "- Document tenant/subscription prerequisites without embedding tenant IDs, client secrets, or generated keys.",
    ])
    output.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Consolidate phase Markdown and scan HELIOS optimization opportunities")
    parser.add_argument("--phase-output", type=Path, default=DEFAULT_PHASE_OUTPUT)
    parser.add_argument("--review-output", type=Path, default=DEFAULT_REVIEW_OUTPUT)
    args = parser.parse_args()

    files = list(iter_files())
    phase_docs = collect_phase_docs()
    findings = collect_findings(files)

    phase_output = args.phase_output if args.phase_output.is_absolute() else ROOT / args.phase_output
    review_output = args.review_output if args.review_output.is_absolute() else ROOT / args.review_output
    write_phase_consolidation(phase_docs, phase_output)
    write_review(files, phase_docs, findings, review_output)

    def display_path(path: Path) -> str:
        try:
            return path.relative_to(ROOT).as_posix()
        except ValueError:
            return path.as_posix()

    print(f"Consolidated {len(phase_docs)} phase Markdown files into {display_path(phase_output)}")
    print(f"Wrote optimization review to {display_path(review_output)} with {len(findings)} sampled findings")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
