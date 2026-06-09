#!/usr/bin/env python3
"""Plan and optionally execute HELIOS submodule branch consolidation.

The consolidation path is intentionally deterministic: inventory first, fetch,
choose one focus branch, merge newest branches, classify unique code into lanes,
validate, push, and only then request PR auto-merge. Missing repository URLs stay
as report items so scheduled jobs can safely run in plan mode.
"""
from __future__ import annotations

import argparse
import configparser
import json
import os
import re
import subprocess
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CONFIG = ROOT / "config" / "automation" / "submodule-consolidation.json"
DEFAULT_OUTPUT = ROOT / "artifacts" / "submodule-consolidation"
SKIP_DIRS = {".git", "bin", "obj", "node_modules", "packages", "__pycache__", ".pytest_cache", ".tools"}
IGNORED_DUPLICATE_NAMES = {"readme.md", "index.md", "status.json", "license", "license.txt"}


@dataclass(frozen=True)
class CommandResult:
    command: str
    returncode: int | None
    stdout: str
    stderr: str


def run(command: list[str], cwd: Path = ROOT, timeout: int = 120) -> CommandResult:
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


def slug(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-") or "module"


def load_config(path: Path) -> dict[str, object]:
    return json.loads(path.read_text(encoding="utf-8"))


def gitmodules_inventory() -> list[dict[str, object]]:
    gitmodules = ROOT / ".gitmodules"
    if not gitmodules.exists():
        return []
    parser = configparser.ConfigParser()
    parser.read_string(gitmodules.read_text(encoding="utf-8"))
    modules = []
    for section in parser.sections():
        if not section.startswith("submodule "):
            continue
        name = section.split('"', 2)[1] if '"' in section else section.replace("submodule ", "")
        modules.append({
            "name": name,
            "path": parser.get(section, "path", fallback=""),
            "url": parser.get(section, "url", fallback=""),
            "target_branch": parser.get(section, "branch", fallback="main"),
            "source_branches": [parser.get(section, "branch", fallback="main")],
            "source": ".gitmodules",
        })
    return modules


def configured_repositories(config: dict[str, object]) -> list[dict[str, object]]:
    repositories: list[dict[str, object]] = []
    for raw in config.get("priority_repositories", []):
        repo = dict(raw)
        url_env = str(repo.get("url_env", ""))
        repo["url"] = os.environ.get(url_env, repo.get("url", "")) if url_env else repo.get("url", "")
        repo["source"] = "priority"
        repositories.append(repo)
    return repositories


def iter_repo_files() -> Iterable[Path]:
    for path in ROOT.rglob("*"):
        if path.is_dir():
            continue
        relative_parts = path.relative_to(ROOT).parts
        if any(part in SKIP_DIRS for part in relative_parts):
            continue
        yield path


def lane_for_file(relative_path: str, suffix: str, lanes: list[dict[str, object]]) -> str:
    for lane in lanes:
        suffixes = set(lane.get("languages", []))
        prefixes = tuple(lane.get("canonical_paths", []))
        if suffix in suffixes and (not prefixes or relative_path.startswith(prefixes)):
            return str(lane.get("lane"))
    for lane in lanes:
        if suffix in set(lane.get("languages", [])):
            return str(lane.get("lane"))
    return "unassigned"


def redundancy_inventory(config: dict[str, object]) -> dict[str, object]:
    files = list(iter_repo_files())
    by_name: dict[str, list[str]] = defaultdict(list)
    lane_counts: dict[str, int] = Counter()
    lane_examples: dict[str, list[str]] = defaultdict(list)
    promotion_candidates: dict[str, list[dict[str, str]]] = defaultdict(list)
    lanes = list(config.get("submodule_lanes", []))
    for path in files:
        rel = path.relative_to(ROOT).as_posix()
        by_name[path.name.lower()].append(rel)
        lane_name = lane_for_file(rel, path.suffix.lower(), lanes)
        if lane_name != "unassigned":
            lane_counts[lane_name] += 1
            if len(lane_examples[lane_name]) < 20:
                lane_examples[lane_name].append(rel)
            canonical = next((lane for lane in lanes if lane.get("lane") == lane_name), {})
            prefixes = tuple(canonical.get("canonical_paths", []))
            if prefixes and not rel.startswith(prefixes) and len(promotion_candidates[lane_name]) < 40:
                promotion_candidates[lane_name].append({
                    "source": rel,
                    "target_lane": lane_name,
                    "target_roots": ", ".join(prefixes),
                    "action": "promote unique code into the owning lane before removing duplicate copies",
                })
    duplicate_candidates = {
        name: sorted(paths)[:12]
        for name, paths in by_name.items()
        if len(paths) > 1 and name not in IGNORED_DUPLICATE_NAMES
    }
    return {
        "lane_file_counts": dict(sorted(lane_counts.items())),
        "lane_examples": dict(lane_examples),
        "duplicate_name_candidates": dict(sorted(duplicate_candidates.items())[:120]),
        "promotion_candidates": dict(promotion_candidates),
    }


def desired_focus_branch(pattern: str, module_name: str, work_item: str) -> str:
    return pattern.format(module_slug=slug(module_name), work_item=slug(work_item))


def remote_branch_inventory(module_path: Path, stale_days: int) -> dict[str, object]:
    fetch = run(["git", "fetch", "--all", "--prune"], cwd=module_path, timeout=600)
    if fetch.returncode != 0:
        return {"available": False, "error": fetch.stderr or fetch.stdout}
    result = run(["git", "for-each-ref", "--sort=-committerdate", "--format=%(refname:short)|%(committerdate:iso8601)|%(authorname)", "refs/remotes"], cwd=module_path)
    branches: list[dict[str, str]] = []
    stale: list[dict[str, str]] = []
    now = datetime.now(timezone.utc)
    for line in result.stdout.splitlines():
        if not line or line.endswith("/HEAD"):
            continue
        branch, date_text, author = (line.split("|", 2) + ["", ""])[:3]
        item = {"branch": branch, "committerdate": date_text, "author": author}
        branches.append(item)
        try:
            normalized = date_text.replace(" ", "T", 1)
            commit_date = datetime.fromisoformat(normalized)
            if commit_date.tzinfo is None:
                commit_date = commit_date.replace(tzinfo=timezone.utc)
            if (now - commit_date).days >= stale_days:
                stale.append(item)
        except ValueError:
            continue
    return {"available": True, "branches": branches[:80], "stale_branches": stale[:80]}


def plan_repository(repo: dict[str, object], pattern: str, work_item: str, policy: dict[str, object] | None = None) -> dict[str, object]:
    policy = policy or {}
    name = str(repo.get("name", ""))
    path = str(repo.get("path", ""))
    target_branch = str(repo.get("target_branch", "main"))
    source_branches = [str(branch) for branch in repo.get("source_branches", [target_branch])]
    focus_branch = desired_focus_branch(pattern, name, work_item)
    return {
        "name": name,
        "path": path,
        "url_present": bool(repo.get("url")),
        "target_branch": target_branch,
        "source_branches": source_branches,
        "focus_branch": focus_branch,
        "lane": repo.get("lane", "external-submodule"),
        "focus": repo.get("focus", ""),
        "stale_branch_days": int(repo.get("stale_branch_days", policy.get("stale_branch_days", 120))),
        "operation_order": policy.get("operation_order", []),
        "promotion_rules": repo.get("promotion_rules", []),
        "actions": [
            f"ensure {path} is initialized from a configured URL" if repo.get("url") else f"configure URL or secret for {name}",
            f"fetch all refs and inspect stale branch history before merge",
            f"checkout {focus_branch} from {target_branch}",
            "merge source branches one at a time, newest first, stopping on conflicts",
            "classify unique files and promote them into the canonical submodule lane",
            "validate, push, open PR, and enable auto-merge only after required checks pass",
        ],
    }


def ensure_module(repo: dict[str, object], module_path: Path, steps: list[dict[str, object]]) -> bool:
    if module_path.exists():
        return True
    module_path.parent.mkdir(parents=True, exist_ok=True)
    clone = run(["git", "clone", str(repo["url"]), str(module_path)], timeout=600)
    steps.append({"command": clone.command, "returncode": clone.returncode})
    return clone.returncode == 0


def repository_selector(repo: dict[str, object]) -> str:
    url = str(repo.get("url", ""))
    match = re.search(r"github\.com[:/]([^/]+)/([^/.]+)(?:\.git)?$", url)
    return f"{match.group(1)}/{match.group(2)}" if match else ""


def create_or_update_pr(repo: dict[str, object], focus_branch: str, target_branch: str, title: str, body: str, cwd: Path) -> dict[str, object]:
    selector = repository_selector(repo)
    command = ["gh", "pr", "create", "--base", target_branch, "--head", focus_branch, "--title", title, "--body", body]
    if selector:
        command[2:2] = ["--repo", selector]
    pr = run(command, cwd=cwd, timeout=120)
    if pr.returncode == 0:
        return {"status": "created", "url": pr.stdout, "returncode": pr.returncode}
    existing = run(["gh", "pr", "list", "--head", focus_branch, "--base", target_branch, "--json", "url", "--jq", ".[0].url"], cwd=cwd, timeout=120)
    if existing.returncode == 0 and existing.stdout:
        return {"status": "existing", "url": existing.stdout, "returncode": 0}
    return {"status": "failed", "returncode": pr.returncode, "stderr": pr.stderr}


def execute_repository(repo: dict[str, object], pattern: str, work_item: str, validation_command: str, policy: dict[str, object]) -> dict[str, object]:
    plan = plan_repository(repo, pattern, work_item, policy)
    if not repo.get("url"):
        plan.update({"status": "skipped", "reason": "repository URL is not configured"})
        return plan
    module_path = ROOT / str(repo.get("path"))
    focus_branch = str(plan["focus_branch"])
    target_branch = str(repo.get("target_branch", "main"))
    source_branches = [str(branch) for branch in repo.get("source_branches", [target_branch])]
    steps: list[dict[str, object]] = []

    if not ensure_module(repo, module_path, steps):
        plan.update({"status": "failed", "stage": "clone", "steps": steps})
        return plan

    remote_inventory = remote_branch_inventory(module_path, int(plan["stale_branch_days"]))
    plan["remote_branch_inventory"] = remote_inventory
    for command in (["git", "checkout", target_branch], ["git", "pull", "--ff-only", "origin", target_branch], ["git", "checkout", "-B", focus_branch]):
        result = run(command, cwd=module_path, timeout=600)
        steps.append({"command": result.command, "returncode": result.returncode})
        if result.returncode != 0:
            plan.update({"status": "failed", "stage": "prepare", "steps": steps, "stderr": result.stderr})
            return plan

    merged: list[str] = []
    for branch in source_branches:
        if branch == target_branch:
            continue
        merge = run(["git", "merge", "--no-edit", f"origin/{branch}"], cwd=module_path, timeout=600)
        steps.append({"command": merge.command, "returncode": merge.returncode})
        if merge.returncode != 0:
            run(["git", "merge", "--abort"], cwd=module_path)
            plan.update({"status": "conflict", "stage": f"merge {branch}", "steps": steps, "stderr": merge.stderr})
            return plan
        merged.append(branch)

    if validation_command:
        validation = run(["bash", "-lc", validation_command], timeout=1200)
        steps.append({"command": validation.command, "returncode": validation.returncode})
        if validation.returncode != 0:
            plan.update({"status": "failed", "stage": "validation", "steps": steps, "stderr": validation.stderr})
            return plan

    push = run(["git", "push", "--set-upstream", "origin", focus_branch], cwd=module_path, timeout=600)
    steps.append({"command": push.command, "returncode": push.returncode})
    if push.returncode != 0:
        plan.update({"status": "failed", "stage": "push", "steps": steps, "stderr": push.stderr})
        return plan

    pr_result: dict[str, object] = {"status": "not-requested"}
    if repo.get("create_pull_request", True):
        body = "\n".join([
            f"Automated submodule consolidation for `{repo.get('name')}`.",
            f"Focus branch: `{focus_branch}`",
            f"Merged source branches: `{', '.join(merged) or 'none'}`",
            "Validation must pass before GitHub auto-merge can land this branch.",
        ])
        pr_result = create_or_update_pr(repo, focus_branch, target_branch, f"Consolidate {repo.get('name')} via {work_item}", body, module_path)
        if repo.get("automerge", True) and pr_result.get("url"):
            auto = run(["gh", "pr", "merge", str(pr_result["url"]), "--auto", "--squash", "--delete-branch"], cwd=module_path, timeout=120)
            pr_result["automerge"] = {"returncode": auto.returncode, "stderr": auto.stderr}
    plan.update({"status": "pushed", "merged_source_branches": merged, "steps": steps, "pull_request": pr_result})
    return plan


def build_report(config: dict[str, object], work_item: str, execute: bool, validation_command: str) -> dict[str, object]:
    policy = dict(config.get("policy", {}))
    pattern = str(policy.get("focus_branch_pattern", "ai/submodule/{module_slug}/{work_item}"))
    repos = configured_repositories(config)
    existing_modules = gitmodules_inventory()
    module_plans = [plan_repository(repo, pattern, work_item, policy) for repo in repos]
    module_plans.extend(plan_repository(module, pattern, work_item, policy) for module in existing_modules)
    execution = [execute_repository(repo, pattern, work_item, validation_command, policy) for repo in repos] if execute else []
    return {
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "mode": "execute" if execute else "plan",
        "policy": policy,
        "github_control_plane": config.get("github_control_plane", {}),
        "priority_repositories": module_plans[: len(repos)],
        "gitmodules": existing_modules,
        "all_module_plans": module_plans,
        "execution": execution,
        "redundancy": redundancy_inventory(config),
    }


def write_reports(report: dict[str, object], output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir / "submodule-consolidation-report.json").write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = [
        "# HELIOS Submodule Branch Consolidation Report",
        "",
        f"Generated UTC: `{report['generated_utc']}`",
        f"Mode: `{report['mode']}`",
        "",
        "## Merge pattern",
        f"- Focus branch pattern: `{report['policy'].get('focus_branch_pattern')}`",
        f"- Promotion branch: `{report['policy'].get('promotion_branch')}`",
        f"- Operation order: `{', '.join(report['policy'].get('operation_order', []))}`",
        f"- Merge strategy: {report['policy'].get('merge_strategy')}",
        f"- Cleanup strategy: {report['policy'].get('cleanup_strategy')}",
        "",
        "## Priority repositories",
        "| Repository | Path | URL configured | Focus branch | Lane | Stale days |",
        "| --- | --- | --- | --- | --- | ---: |",
    ]
    for plan in report["priority_repositories"]:
        lines.append(f"| {plan['name']} | `{plan['path']}` | `{plan['url_present']}` | `{plan['focus_branch']}` | {plan['lane']} | {plan['stale_branch_days']} |")
    lines.extend(["", "## Redundancy and lane inventory", "| Lane | Files | Promotion candidates |", "| --- | ---: | ---: |"])
    promotions = report["redundancy"].get("promotion_candidates", {})
    for lane, count in report["redundancy"]["lane_file_counts"].items():
        lines.append(f"| {lane} | {count} | {len(promotions.get(lane, []))} |")
    duplicate_count = len(report["redundancy"]["duplicate_name_candidates"])
    lines.extend(["", f"Duplicate-name cleanup candidates discovered: `{duplicate_count}`"])
    if report["execution"]:
        lines.extend(["", "## Execution results", "| Repository | Status | Stage | PR |", "| --- | --- | --- | --- |"])
        for result in report["execution"]:
            pr = result.get("pull_request", {}) if isinstance(result.get("pull_request"), dict) else {}
            lines.append(f"| {result['name']} | {result.get('status')} | {result.get('stage', '')} | {pr.get('url', '')} |")
    (output_dir / "submodule-consolidation-report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Plan or execute HELIOS submodule branch consolidation")
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--work-item", default=os.environ.get("HELIOS_WORK_ITEM", "continuous-consolidation"))
    parser.add_argument("--execute", action="store_true")
    parser.add_argument("--validation-command", default="python3 -m compileall scripts/automation")
    args = parser.parse_args()

    config_path = args.config if args.config.is_absolute() else ROOT / args.config
    output_dir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
    report = build_report(load_config(config_path), args.work_item, args.execute, args.validation_command)
    write_reports(report, output_dir)
    failures = [item for item in report["execution"] if item.get("status") in {"failed", "conflict"}]
    print(f"Mode: {report['mode']}")
    print(f"Priority repositories: {len(report['priority_repositories'])}")
    print(f"Execution failures: {len(failures)}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
