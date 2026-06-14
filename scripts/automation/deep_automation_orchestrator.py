#!/usr/bin/env python3
"""HELIOS deep automation orchestrator.

Creates a deterministic repository, branch, GitHub Actions, Azure CLI, and AI hub
readiness report without requiring network access or secrets. The report is meant
for CI gates and local operators before enabling privileged automation.
"""
from __future__ import annotations

import argparse
import json
import os
import platform
import re
import shutil
import subprocess
from urllib.parse import urlsplit, urlunsplit
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = ROOT / "artifacts" / "automation"
SKIP_DIRS = {
    ".git",
    ".vs",
    ".vscode",
    "bin",
    "obj",
    "node_modules",
    "packages",
    ".pytest_cache",
    "__pycache__",
}
LANGUAGE_EXTENSIONS = {
    "csharp": {".cs", ".csproj", ".sln", ".slnx"},
    "winui_wpf_xaml": {".xaml"},
    "cpp": {".cpp", ".cxx", ".cc", ".h", ".hpp", ".vcxproj"},
    "fsharp": {".fs", ".fsi", ".fsx", ".fsproj"},
    "python": {".py", ".pyw", ".ipynb"},
    "powershell": {".ps1", ".psm1", ".psd1"},
    "github_actions": {".yml", ".yaml"},
    "javascript": {".js", ".mjs", ".cjs", ".ts", ".tsx"},
}
AI_CONFIG_FILES = [
    ROOT / "scripts" / "ai-services" / "ai-services-config.json",
    ROOT / "cloud-integration" / "configs" / "openai.config.json",
    ROOT / "cloud-integration" / "configs" / "azure-openai.config.json",
    ROOT / "cloud-integration" / "configs" / "github.config.json",
    ROOT / "cloud-integration" / "configs" / "copilot.config.json",
]
BRANCH_FOCUS_NAMES = ("helios-control", "hermes-fleet-production")
ORCHESTRATION_FOCUS_TERMS = (
    "helios",
    "hermes",
    "fleet",
    "control",
    "ai",
    "aihub",
    "automation",
    "workflow",
    "azure",
    "github",
    "xcore",
    "winui",
    "xaml",
    "csharp",
    "cpp",
    "fsharp",
    "analytics",
    "prediction",
    "parallel",
    "security",
    "performance",
)


@dataclass(frozen=True)
class CommandResult:
    command: str
    returncode: int | None
    stdout: str
    stderr: str


def run(command: list[str], cwd: Path = ROOT, timeout: int = 20) -> CommandResult:
    try:
        completed = subprocess.run(
            command,
            cwd=cwd,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=timeout,
            check=False,
        )
        return CommandResult(" ".join(command), completed.returncode, completed.stdout.strip(), completed.stderr.strip())
    except (FileNotFoundError, subprocess.TimeoutExpired) as exc:
        return CommandResult(" ".join(command), None, "", str(exc))


def iter_repo_files() -> Iterable[Path]:
    """Yield repository files while pruning generated/dependency trees early."""
    for current_root, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [dirname for dirname in dirnames if dirname not in SKIP_DIRS]
        current_path = Path(current_root)
        for filename in filenames:
            yield current_path / filename


def count_language_files(files: Iterable[Path]) -> dict[str, int]:
    counters: dict[str, int] = {name: 0 for name in LANGUAGE_EXTENSIONS}
    for path in files:
        suffix = path.suffix.lower()
        for language, extensions in LANGUAGE_EXTENSIONS.items():
            if suffix in extensions:
                if language == "github_actions" and ".github/workflows" not in path.as_posix():
                    continue
                counters[language] += 1
    return counters


def sanitize_remote_url(url: str) -> str:
    """Redact user-info from a git remote URL while preserving non-secret parts."""
    parsed = urlsplit(url)
    if not parsed.scheme or "@" not in parsed.netloc:
        return url

    host = parsed.netloc.rsplit("@", 1)[1]
    sanitized_netloc = f"[redacted]@{host}"
    return urlunsplit((parsed.scheme, sanitized_netloc, parsed.path, parsed.query, parsed.fragment))


def sanitize_remote_line(line: str) -> str:
    """Redact credentials embedded in URLs returned by `git remote -v`."""
    return re.sub(
        r"(?P<url>[A-Za-z][A-Za-z0-9+.-]*://[^\s]+)",
        lambda match: sanitize_remote_url(match.group("url")),
        line,
    )


def git_ref_inventory() -> dict[str, object]:
    """Return portable local/remote ref details without relying on git show-ref --remotes."""
    result = run([
        "git",
        "for-each-ref",
        "--format=%(refname:short)|%(objectname:short)|%(refname)",
        "refs/heads",
        "refs/remotes",
    ])
    refs: list[dict[str, str]] = []
    if result.returncode == 0:
        for line in result.stdout.splitlines():
            parts = line.split("|", 2)
            if len(parts) != 3:
                continue
            short_name, commit, full_name = parts
            ref_type = "remote" if full_name.startswith("refs/remotes/") else "local"
            refs.append({"name": short_name, "commit": commit, "type": ref_type})
    return {
        "command": result.command,
        "returncode": result.returncode,
        "refs": refs,
        "local_count": sum(1 for ref in refs if ref["type"] == "local"),
        "remote_count": sum(1 for ref in refs if ref["type"] == "remote"),
    }

def git_inventory() -> dict[str, object]:
    branches_raw = run(["git", "branch", "-a", "--no-color"]).stdout.splitlines()
    branches = [line.replace("*", " ").strip() for line in branches_raw if line.strip()]
    refs = git_ref_inventory()
    ref_names = [str(ref["name"]) for ref in refs["refs"]]
    status = run(["git", "status", "--short", "--branch"]).stdout
    remotes = [sanitize_remote_line(line) for line in run(["git", "remote", "-v"]).stdout.splitlines()]
    focus = {name: sorted({candidate for candidate in branches + ref_names if name in candidate}) for name in BRANCH_FOCUS_NAMES}
    missing_focus = [name for name, matches in focus.items() if not matches]
    return {
        "current_branch": run(["git", "branch", "--show-current"]).stdout,
        "status_short": status,
        "branches": branches,
        "refs": refs,
        "remotes": remotes,
        "focus_branch_matches": focus,
        "missing_focus_branches": missing_focus,
        "merge_guidance": [
            "Fetch remotes with full history before attempting branch consolidation.",
            "Merge protected/focus branches only through pull requests with CI, security, and deployment checks enabled.",
            "If helios-control or hermes-fleet-production are external repositories, add them as remotes or submodules before running this orchestrator again.",
        ],
    }


def github_actions_inventory() -> dict[str, object]:
    workflows = sorted((ROOT / ".github" / "workflows").glob("*.yml")) + sorted((ROOT / ".github" / "workflows").glob("*.yaml"))
    triggers: dict[str, list[str]] = {}
    for workflow in workflows:
        text = workflow.read_text(encoding="utf-8", errors="ignore")
        seen = []
        for marker in ("workflow_dispatch", "pull_request", "push", "schedule", "release"):
            if marker in text:
                seen.append(marker)
        triggers[workflow.name] = seen
    return {
        "workflow_count": len(workflows),
        "workflows": [workflow.relative_to(ROOT).as_posix() for workflow in workflows],
        "triggers": triggers,
    }


def ai_inventory() -> dict[str, object]:
    configs: dict[str, object] = {}
    env_names: set[str] = set()
    for config_path in AI_CONFIG_FILES:
        if not config_path.exists():
            configs[config_path.relative_to(ROOT).as_posix()] = {"exists": False}
            continue
        try:
            data = json.loads(config_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            configs[config_path.relative_to(ROOT).as_posix()] = {"exists": True, "valid_json": False, "error": str(exc)}
            continue
        serialized = json.dumps(data)
        for key in ("api_key_env", "api_key_environment_variable", "token_env", "secret_env"):
            def collect(value: object) -> None:
                if isinstance(value, dict):
                    for child_key, child_value in value.items():
                        if child_key == key and isinstance(child_value, str):
                            env_names.add(child_value)
                        collect(child_value)
                elif isinstance(value, list):
                    for child in value:
                        collect(child)
            collect(data)
        configs[config_path.relative_to(ROOT).as_posix()] = {
            "exists": True,
            "valid_json": True,
            "top_level_keys": sorted(data.keys()) if isinstance(data, dict) else [],
            "bytes": len(serialized),
        }
    env_status = {name: bool(os.environ.get(name)) for name in sorted(env_names)}
    return {"configs": configs, "declared_secret_env": env_status}


def azure_github_cli_inventory() -> dict[str, object]:
    tools = {
        "az": shutil.which("az"),
        "gh": shutil.which("gh"),
        "git": shutil.which("git"),
        "dotnet": shutil.which("dotnet"),
        "python3": shutil.which("python3") or shutil.which("python"),
    }
    versions = {}
    for name, executable in tools.items():
        if not executable:
            versions[name] = {"available": False}
            continue
        args = [executable, "--version"] if name != "dotnet" else [executable, "--info"]
        result = run(args, timeout=15)
        versions[name] = {"available": True, "path": executable, "returncode": result.returncode, "stdout_first_line": result.stdout.splitlines()[0] if result.stdout else ""}
    azure_context = run(["az", "account", "show", "--output", "json"], timeout=15) if tools["az"] else CommandResult("az account show --output json", None, "", "az not found")
    return {"tools": versions, "azure_account_available": azure_context.returncode == 0}


def component_inventory(files: list[Path]) -> dict[str, object]:
    directories = Counter(path.relative_to(ROOT).parts[0] for path in files if path.relative_to(ROOT).parts)
    focus_terms = defaultdict(list)
    for path in files:
        relative = path.relative_to(ROOT).as_posix()
        lowered = relative.lower()
        normalized = lowered.replace(".", "").replace("-", "").replace("_", "")
        for term in ORCHESTRATION_FOCUS_TERMS:
            if (term in lowered or term in normalized) and len(focus_terms[term]) < 40:
                focus_terms[term].append(relative)
    return {
        "top_level_file_counts": dict(directories.most_common()),
        "focus_term_samples": dict(sorted(focus_terms.items())),
    }


def build_report(args: argparse.Namespace) -> dict[str, object]:
    files = list(iter_repo_files())
    return {
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "repository": ROOT.as_posix(),
        "mode": args.mode,
        "host": {"platform": platform.platform(), "python": platform.python_version()},
        "git": git_inventory(),
        "languages": count_language_files(files),
        "components": component_inventory(files),
        "github_actions": github_actions_inventory(),
        "ai_hub": ai_inventory(),
        "azure_github_cli": azure_github_cli_inventory(),
        "recommended_workflow": [
            "1. Run inventory in dry-run mode on every PR and nightly schedule.",
            "2. Resolve missing focus remotes/branches before consolidation.",
            "3. Run CI matrix across C#, XAML/WinUI/WPF assets, C++, F#, Python, PowerShell, and GitHub Actions files.",
            "4. Require GitHub environment approvals before Azure deployment or AI-driven code changes.",
            "5. Store API keys only in GitHub Actions secrets or Azure Key Vault; never write secret values into reports.",
        ],
    }


def write_markdown(report: dict[str, object], path: Path) -> None:
    git = report["git"]
    languages = report["languages"]
    gha = report["github_actions"]
    components = report["components"]
    ai = report["ai_hub"]
    azure = report["azure_github_cli"]
    lines = [
        "# HELIOS Deep Automation Readiness Report",
        "",
        f"Generated UTC: `{report['generated_utc']}`",
        f"Mode: `{report['mode']}`",
        "",
        "## Branch and merge consolidation",
        f"- Current branch: `{git['current_branch']}`",
        f"- Branches discovered: `{len(git['branches'])}`",
        f"- Portable refs discovered: `{git['refs']['local_count']}` local / `{git['refs']['remote_count']}` remote",
        f"- Ref inventory command: `{git['refs']['command']}`",
        f"- Missing focus branches/remotes: `{', '.join(git['missing_focus_branches']) or 'none'}`",
        "",
        "## Language and component inventory",
        "| Area | Files |",
        "| --- | ---: |",
    ]
    for language, count in sorted(languages.items()):
        lines.append(f"| {language} | {count} |")
    focus_samples = components.get("focus_term_samples", {})
    lines.extend([
        "",
        "## HELIOS/Hermes specialization coverage",
        f"- Focus terms sampled: `{len(focus_samples)}`",
        "- Priority branch/remotes checked: `helios-control`, `hermes-fleet-production`",
    ])
    for term, samples in list(focus_samples.items())[:12]:
        preview = ", ".join(f"`{sample}`" for sample in samples[:3])
        lines.append(f"- `{term}` samples: {preview or 'none'}")
    lines.extend([
        "",
        "## GitHub workflow automation",
        f"- Workflow files discovered: `{gha['workflow_count']}`",
        "- Added orchestration gate can run locally, on PRs, nightly, or on demand.",
        "",
        "## AI hub and workflow automation",
        "| Config | Valid | Notes |",
        "| --- | --- | --- |",
    ])
    for config, details in ai["configs"].items():
        valid = details.get("valid_json", False) if details.get("exists") else False
        notes = "present" if details.get("exists") else "missing"
        if details.get("error"):
            notes = details["error"]
        lines.append(f"| `{config}` | `{valid}` | {notes} |")
    lines.extend([
        "",
        "## Azure and GitHub CLI readiness",
        "| Tool | Available | First line |",
        "| --- | --- | --- |",
    ])
    for tool, details in azure["tools"].items():
        first_line = str(details.get("stdout_first_line", "")).replace("|", "\\|")
        lines.append(f"| `{tool}` | `{details.get('available', False)}` | {first_line} |")
    lines.extend([
        f"- Azure account context available: `{azure['azure_account_available']}`",
        "",
        "## Recommended controlled rollout",
    ])
    lines.extend(f"- {item}" for item in report["recommended_workflow"])
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="HELIOS deep automation readiness orchestrator")
    parser.add_argument("--mode", choices=["inventory", "ci", "azure", "ai", "full"], default="full")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--fail-on-dirty", action="store_true", help="Fail when the working tree has uncommitted changes")
    args = parser.parse_args()

    output_dir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
    output_dir.mkdir(parents=True, exist_ok=True)
    report = build_report(args)
    json_path = output_dir / "deep-automation-report.json"
    md_path = output_dir / "deep-automation-report.md"
    json_path.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    write_markdown(report, md_path)

    def display_path(path: Path) -> str:
        try:
            return path.relative_to(ROOT).as_posix()
        except ValueError:
            return path.as_posix()

    print(f"Wrote {display_path(json_path)}")
    print(f"Wrote {display_path(md_path)}")

    status_lines = str(report["git"]["status_short"]).splitlines()
    dirty_lines = [line for line in status_lines if not line.startswith("##")]
    if args.fail_on_dirty and dirty_lines:
        print("Working tree has changes; failing because --fail-on-dirty was set.")
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
