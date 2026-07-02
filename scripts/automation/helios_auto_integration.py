#!/usr/bin/env python3
"""HELIOS automatic AI, branch, and Azure automation integrator.

This utility is intentionally safe by default: it discovers branch, language,
Azure CLI, AIHub, HELIOS Control, and Hermes Fleet Production readiness without
mutating the working tree unless --execute is supplied for the supported actions.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

LANGUAGE_GLOBS = {
    "csharp": ("*.cs", "*.csproj"),
    "cpp": ("*.cpp", "*.cc", "*.cxx", "*.c", "*.h", "*.hpp", "*.vcxproj"),
    "fsharp": ("*.fs", "*.fsi", "*.fsx", "*.fsproj"),
    "python": ("*.py",),
    "xaml": ("*.xaml",),
}

SKIP_DIRS = {".git", "bin", "obj", ".vs", ".idea", "node_modules", "packages"}


@dataclass
class CommandResult:
    command: list[str]
    returncode: int
    stdout: str = ""
    stderr: str = ""


@dataclass
class IntegrationReport:
    generated_at_utc: str
    repository: str
    execute_mode: bool
    profiles: dict[str, Any] = field(default_factory=dict)
    branches: dict[str, Any] = field(default_factory=dict)
    languages: dict[str, Any] = field(default_factory=dict)
    azure_cli: dict[str, Any] = field(default_factory=dict)
    ai_hub: dict[str, Any] = field(default_factory=dict)
    recommendations: list[str] = field(default_factory=list)
    actions: list[str] = field(default_factory=list)


def run_command(command: list[str], cwd: Path) -> CommandResult:
    try:
        completed = subprocess.run(
            command,
            cwd=str(cwd),
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
        return CommandResult(command, completed.returncode, completed.stdout.strip(), completed.stderr.strip())
    except FileNotFoundError as exc:
        return CommandResult(command, 127, "", str(exc))


def load_profiles(repo_root: Path) -> dict[str, Any]:
    profile_path = repo_root / "config" / "automation" / "ai-automation-profiles.json"
    with profile_path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def discover_profiles(repo_root: Path, profile_config: dict[str, Any]) -> dict[str, Any]:
    discovered: dict[str, Any] = {}
    for name, profile in profile_config.get("profiles", {}).items():
        paths = profile.get("requiredPaths", [])
        path_status = {path: (repo_root / path).exists() for path in paths}
        discovered[name] = {
            "description": profile.get("description", ""),
            "focusAreas": profile.get("focusAreas", []),
            "requiredPathStatus": path_status,
            "coveragePercent": round((sum(path_status.values()) / max(len(path_status), 1)) * 100, 2),
            "azureCli": profile.get("azureCli", {"requiredCommands": [], "extensions": []}),
        }
    return discovered


def discover_branches(repo_root: Path, execute: bool, merge_all: bool, fetch: bool, profile_config: dict[str, Any]) -> dict[str, Any]:
    branch_report: dict[str, Any] = {"fetchAttempted": False, "current": None, "available": [], "mergePlan": []}
    if fetch:
        branch_report["fetchAttempted"] = True
        result = run_command(["git", "fetch", "--all", "--prune"], repo_root)
        branch_report["fetch"] = result.__dict__

    current = run_command(["git", "branch", "--show-current"], repo_root)
    branch_report["current"] = current.stdout or None

    branches = run_command(["git", "branch", "--all", "--format", "%(refname:short)"], repo_root)
    skip_patterns = tuple(profile_config.get("branchMerge", {}).get("skipPatterns", []))
    available = []
    for raw_branch in branches.stdout.splitlines():
        branch = raw_branch.strip()
        if not branch or branch == branch_report["current"] or branch.endswith("/HEAD"):
            continue
        if any(pattern and pattern in branch for pattern in skip_patterns):
            continue
        available.append(branch)
    branch_report["available"] = sorted(set(available))

    if merge_all:
        for branch in branch_report["available"]:
            action = {"branch": branch, "strategy": profile_config.get("branchMerge", {}).get("defaultStrategy", "no-commit")}
            if execute:
                merge = run_command(["git", "merge", "--no-commit", "--no-ff", branch], repo_root)
                action["result"] = merge.__dict__
                if merge.returncode != 0:
                    action["status"] = "blocked"
                    branch_report["mergePlan"].append(action)
                    break
                action["status"] = "merged-no-commit"
            else:
                action["status"] = "planned-dry-run"
            branch_report["mergePlan"].append(action)
    return branch_report


def iter_repo_files(repo_root: Path):
    for path in repo_root.rglob("*"):
        if not path.is_file():
            continue
        if any(part in SKIP_DIRS for part in path.parts):
            continue
        yield path


def discover_languages(repo_root: Path, profile_config: dict[str, Any]) -> dict[str, Any]:
    language_report: dict[str, Any] = {}
    all_files = list(iter_repo_files(repo_root))
    for language, patterns in LANGUAGE_GLOBS.items():
        matches = []
        suffixes = tuple(pattern.replace("*", "") for pattern in patterns)
        for path in all_files:
            if path.name.endswith(suffixes):
                matches.append(path.relative_to(repo_root).as_posix())
        sample = matches[:20]
        language_report[language] = {
            "role": profile_config.get("languageRoles", {}).get(language, ""),
            "count": len(matches),
            "sample": sample,
        }
    return language_report


def inspect_azure_cli(repo_root: Path, execute: bool, profile_config: dict[str, Any]) -> dict[str, Any]:
    az_path = shutil.which("az")
    report: dict[str, Any] = {
        "installed": az_path is not None,
        "path": az_path,
        "subscriptionEnvironmentVariable": "AZURE_SUBSCRIPTION_ID" in os.environ,
        "tenantEnvironmentVariable": "AZURE_TENANT_ID" in os.environ,
        "extensionPlan": [],
    }
    extensions = sorted({ext for profile in profile_config.get("profiles", {}).values() for ext in profile.get("azureCli", {}).get("extensions", [])})
    commands = sorted({cmd for profile in profile_config.get("profiles", {}).values() for cmd in profile.get("azureCli", {}).get("requiredCommands", [])})
    report["requiredCommands"] = commands
    for extension in extensions:
        item: dict[str, Any] = {"extension": extension}
        if az_path and execute:
            result = run_command(["az", "extension", "add", "--upgrade", "--name", extension], repo_root)
            item["result"] = result.__dict__
            item["status"] = "installed-or-upgraded" if result.returncode == 0 else "failed"
        else:
            item["status"] = "planned"
        report["extensionPlan"].append(item)

    if az_path and execute and os.environ.get("AZURE_SUBSCRIPTION_ID"):
        account = run_command(["az", "account", "set", "--subscription", os.environ["AZURE_SUBSCRIPTION_ID"]], repo_root)
        report["accountSet"] = account.__dict__
    return report


def inspect_ai_hub(repo_root: Path) -> dict[str, Any]:
    candidates = [
        "ai-integration/README.md",
        "ai-integration/ai-coordination/README.md",
        "config/ai-services/ai-services-config.json",
        "config/ai-services/service-weights.json",
        "scripts/ai-integration/config/.env.template",
        "scripts/ai-integration/ai-config-schema.json",
    ]
    return {
        "configuredPaths": {candidate: (repo_root / candidate).exists() for candidate in candidates},
        "apiKeyEnvironmentVariables": {
            "OPENAI_API_KEY": "OPENAI_API_KEY" in os.environ,
            "AZURE_OPENAI_ENDPOINT": "AZURE_OPENAI_ENDPOINT" in os.environ,
            "AZURE_OPENAI_API_KEY": "AZURE_OPENAI_API_KEY" in os.environ,
        },
    }


def build_recommendations(report: IntegrationReport) -> list[str]:
    recommendations: list[str] = []
    if not report.azure_cli.get("installed"):
        recommendations.append("Install Azure CLI before execute mode; dry-run reports can still be generated without it.")
    if not report.branches.get("available"):
        recommendations.append("No additional local or remote branches were discovered for automatic merge planning.")
    missing_languages = [name for name, payload in report.languages.items() if payload.get("count", 0) == 0]
    if missing_languages:
        recommendations.append(f"Add or link source roots for missing requested language surfaces: {', '.join(missing_languages)}.")
    for profile_name, profile in report.profiles.items():
        missing_paths = [path for path, exists in profile.get("requiredPathStatus", {}).items() if not exists]
        if missing_paths:
            recommendations.append(f"{profile_name}: create or connect missing paths: {', '.join(missing_paths)}.")
    if not any(report.ai_hub.get("apiKeyEnvironmentVariables", {}).values()):
        recommendations.append("Set OPENAI_API_KEY or Azure OpenAI environment variables before live AIHub automation.")
    return recommendations


def write_report(report: IntegrationReport, output: Path | None) -> None:
    payload = json.dumps(report.__dict__, indent=2, sort_keys=True)
    if output:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(payload + "\n", encoding="utf-8")
    print(payload)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="HELIOS automatic AI, branch, and Azure integration planner")
    parser.add_argument("--repo-root", type=Path, default=Path.cwd(), help="Repository root to inspect")
    parser.add_argument("--output", type=Path, help="Optional JSON report output path")
    parser.add_argument("--execute", action="store_true", help="Run supported mutating setup actions such as Azure extension install")
    parser.add_argument("--fetch", action="store_true", help="Fetch all git remotes before branch discovery")
    parser.add_argument("--merge-all", action="store_true", help="Plan or execute no-commit merges for all eligible discovered branches")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = args.repo_root.resolve()
    if not (repo_root / ".git").exists():
        print(f"error: {repo_root} is not a git repository root", file=sys.stderr)
        return 2

    profile_config = load_profiles(repo_root)
    report = IntegrationReport(
        generated_at_utc=datetime.now(timezone.utc).isoformat(),
        repository=str(repo_root),
        execute_mode=args.execute,
    )
    report.profiles = discover_profiles(repo_root, profile_config)
    report.branches = discover_branches(repo_root, args.execute, args.merge_all, args.fetch, profile_config)
    report.languages = discover_languages(repo_root, profile_config)
    report.azure_cli = inspect_azure_cli(repo_root, args.execute, profile_config)
    report.ai_hub = inspect_ai_hub(repo_root)
    report.actions = [
        "profile-discovery",
        "branch-discovery" + ("-merge-plan" if args.merge_all else ""),
        "language-surface-scan",
        "azure-cli-readiness",
        "aihub-readiness",
    ]
    report.recommendations = build_recommendations(report)
    write_report(report, args.output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
