#!/usr/bin/env python3
"""Generate a deterministic AI-review-ready CI report for HELIOS pull requests.

The report intentionally avoids calling external AI services directly. It prepares
high-signal context, risk findings, and language-specific review prompts that can
be consumed by GitHub Copilot, OpenAI-backed reviewers, or human reviewers.
"""
from __future__ import annotations

import argparse
import json
import subprocess
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LANGUAGE_BY_SUFFIX = {
    ".cs": "C# / WinUI front end",
    ".xaml": "WinUI/WPF XAML",
    ".cpp": "C++ performance back end",
    ".cxx": "C++ performance back end",
    ".cc": "C++ performance back end",
    ".h": "C++ performance back end",
    ".hpp": "C++ performance back end",
    ".fs": "F# analytics/predictions",
    ".fsx": "F# analytics/predictions",
    ".py": "Python AIHub/integration",
    ".ps1": "PowerShell automation",
    ".psm1": "PowerShell automation",
    ".yml": "GitHub Actions / YAML",
    ".yaml": "GitHub Actions / YAML",
}
RISK_PATTERNS = {
    "secret-handling": ["password", "api_key", "apikey", "secret", "token"],
    "shell-execution": ["Process.Start", "subprocess.", "os.system", "Start-Process", "cmd.exe", "powershell -"],
    "unsafe-parsing": ["eval(", "exec(", "pickle.load", "yaml.load("],
    "destructive-operation": ["Remove-Item", "rm -rf", "DeleteFile", "Directory.Delete"],
    "network-boundary": ["HttpClient", "requests.", "Invoke-RestMethod", "curl ", "wget "],
}
FOCUS_TERMS = ("helios-control", "hermes-fleet-production", "hermes", "fleet", "xcore", "aihub", "azure")


@dataclass(frozen=True)
class CommandResult:
    command: str
    returncode: int
    stdout: str
    stderr: str


def run(command: list[str]) -> CommandResult:
    completed = subprocess.run(command, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    return CommandResult(" ".join(command), completed.returncode, completed.stdout.strip(), completed.stderr.strip())


def changed_files(base_ref: str) -> list[Path]:
    merge_base = run(["git", "merge-base", "HEAD", base_ref])
    if merge_base.returncode != 0 or not merge_base.stdout:
        diff = run(["git", "diff", "--name-only", "HEAD~1", "HEAD"])
    else:
        diff = run(["git", "diff", "--name-only", f"{merge_base.stdout}...HEAD"])
    files = []
    for line in diff.stdout.splitlines():
        path = ROOT / line
        if path.exists() and path.is_file():
            files.append(path)
    return files


def classify(path: Path) -> str:
    suffix = path.suffix.lower()
    if suffix in (".yml", ".yaml") and ".github/workflows" not in path.as_posix():
        return "YAML/configuration"
    return LANGUAGE_BY_SUFFIX.get(suffix, "Other")


def scan_file(path: Path) -> list[dict[str, object]]:
    findings: list[dict[str, object]] = []
    try:
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    except OSError as exc:
        return [{"category": "read-error", "line": 0, "pattern": str(exc)}]
    for line_number, line in enumerate(lines, start=1):
        lowered = line.lower()
        for category, patterns in RISK_PATTERNS.items():
            for pattern in patterns:
                if pattern.lower() in lowered:
                    findings.append({
                        "category": category,
                        "file": path.relative_to(ROOT).as_posix(),
                        "line": line_number,
                        "pattern": pattern,
                    })
    return findings


def build_report(base_ref: str) -> dict[str, object]:
    files = changed_files(base_ref)
    language_counts = Counter(classify(path) for path in files)
    findings = []
    focus_files = []
    for path in files:
        rel = path.relative_to(ROOT).as_posix()
        if any(term in rel.lower() for term in FOCUS_TERMS):
            focus_files.append(rel)
        findings.extend(scan_file(path))
    return {
        "base_ref": base_ref,
        "changed_files": [path.relative_to(ROOT).as_posix() for path in files],
        "language_counts": dict(language_counts),
        "focus_files": focus_files,
        "risk_findings": findings,
        "review_prompts": [
            "Verify C# and WinUI changes for dispatcher/thread affinity, binding correctness, accessibility, and nullability.",
            "Verify C++ back-end changes for bounds checks, lifetime ownership, allocator behavior, and hot-path regressions.",
            "Verify F# analytics changes for deterministic math, numerical stability, and parallel collection safety.",
            "Verify Python AIHub changes for prompt-injection boundaries, secret handling, retries, and offline failure modes.",
            "Verify GitHub/Azure automation changes for least-privilege permissions, protected branches, and environment gates.",
        ],
    }


def write_markdown(report: dict[str, object], path: Path) -> None:
    lines = [
        "# HELIOS AI CI Code Review Report",
        "",
        f"Base reference: `{report['base_ref']}`",
        f"Changed files: `{len(report['changed_files'])}`",
        "",
        "## Language coverage",
        "| Area | Files |",
        "| --- | ---: |",
    ]
    for language, count in sorted(report["language_counts"].items()):
        lines.append(f"| {language} | {count} |")
    lines.extend(["", "## Focus areas"])
    if report["focus_files"]:
        lines.extend(f"- `{file}`" for file in report["focus_files"])
    else:
        lines.append("- No HELIOS/Hermes/XCore focus files changed in this diff.")
    lines.extend(["", "## Risk findings"])
    if report["risk_findings"]:
        lines.append("| Category | File | Line | Pattern |")
        lines.append("| --- | --- | ---: | --- |")
        for finding in report["risk_findings"][:200]:
            lines.append(f"| {finding['category']} | `{finding.get('file', '')}` | {finding.get('line', 0)} | `{finding.get('pattern', '')}` |")
    else:
        lines.append("- No configured high-risk patterns found in changed files.")
    lines.extend(["", "## AI reviewer prompt pack"])
    lines.extend(f"- {prompt}" for prompt in report["review_prompts"])
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate HELIOS AI CI review context")
    parser.add_argument("--base-ref", default="origin/main")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "artifacts" / "ai-review")
    args = parser.parse_args()
    output_dir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
    output_dir.mkdir(parents=True, exist_ok=True)
    report = build_report(args.base_ref)
    (output_dir / "ai-review-report.json").write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    write_markdown(report, output_dir / "ai-review-report.md")
    print(f"Changed files: {len(report['changed_files'])}")
    print(f"Risk findings: {len(report['risk_findings'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
