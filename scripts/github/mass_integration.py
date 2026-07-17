#!/usr/bin/env python3
"""Automated HELIOS/Hermes mass integration scorer and PR orchestrator.

Modes:
  score  - fetch refs, score candidate branches/submodules, and write reports.
  plan   - score and write an ordered merge plan.
  branch - create/reset the integration branch and merge candidates in order.
  pr     - push integration branch and open/update a pull request with gh.
  merge  - request auto-merge for the pull request with gh.

The script is designed for GitHub runners and local shells. It does not require
manual review when a token with sufficient permissions is provided, but it still
honors branch protection and required checks enforced by GitHub.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CONFIG = ROOT / "config" / "helios-mass-integration.json"
DEFAULT_REPORT_DIR = ROOT / "reports" / "mass-integration"


def run(cmd: list[str], check: bool = False) -> tuple[int, str, str]:
    proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True)
    if check and proc.returncode != 0:
        raise RuntimeError(f"Command failed ({proc.returncode}): {' '.join(cmd)}\n{proc.stderr}")
    return proc.returncode, proc.stdout.strip(), proc.stderr.strip()


def load_config(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def timestamp() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat()


def current_branch() -> str:
    code, out, _ = run(["git", "branch", "--show-current"])
    return out if code == 0 else ""


def ensure_git_identity() -> None:
    run(["git", "config", "user.name", os.environ.get("GIT_AUTHOR_NAME", "HELIOS Automation")])
    run(["git", "config", "user.email", os.environ.get("GIT_AUTHOR_EMAIL", "helios-automation@users.noreply.github.com")])


def fetch_all() -> dict[str, Any]:
    code, out, err = run(["git", "fetch", "--all", "--prune", "--tags"])
    return {"command": "git fetch --all --prune --tags", "exitCode": code, "stdout": out[-4000:], "stderr": err[-4000:]}


def sync_submodules(apply: bool) -> dict[str, Any]:
    gitmodules = ROOT / ".gitmodules"
    if not gitmodules.exists():
        return {"present": False, "actions": []}
    actions = []
    for cmd in (["git", "submodule", "sync", "--recursive"], ["git", "submodule", "update", "--init", "--recursive", "--remote"]):
        if apply:
            code, out, err = run(cmd)
            actions.append({"command": " ".join(cmd), "exitCode": code, "stdout": out[-2000:], "stderr": err[-2000:]})
        else:
            actions.append({"command": " ".join(cmd), "exitCode": None, "dryRun": True})
    return {"present": True, "actions": actions}


def remote_branches() -> list[str]:
    code, out, _ = run(["git", "for-each-ref", "--format=%(refname:short)", "refs/remotes"])
    if code != 0 or not out:
        return []
    return [line.strip() for line in out.splitlines() if line.strip() and not line.endswith("/HEAD")]


def candidate_branches(config: dict[str, Any]) -> list[str]:
    includes = [p.lower() for p in config.get("branchNamePatterns", [])]
    excludes = [p.lower() for p in config.get("excludeBranchPatterns", [])]
    candidates = []
    for ref in remote_branches():
        short = ref.split("/", 1)[-1]
        lowered = short.lower()
        if any(pattern.lower() in lowered for pattern in excludes):
            continue
        if any(pattern in lowered for pattern in includes):
            candidates.append(ref)
    priority = []
    for wanted in config.get("priorityBranches", []):
        for ref in candidates:
            if ref.endswith("/" + wanted) and ref not in priority:
                priority.append(ref)
    rest = sorted([ref for ref in candidates if ref not in priority])
    return priority + rest


def merge_base(target_ref: str, candidate_ref: str) -> str | None:
    code, out, _ = run(["git", "merge-base", target_ref, candidate_ref])
    return out if code == 0 and out else None


def changed_files(target_ref: str, candidate_ref: str) -> list[str]:
    base = merge_base(target_ref, candidate_ref)
    cmd = ["git", "diff", "--name-only", base, candidate_ref] if base else ["git", "show", "--name-only", "--format=", candidate_ref]
    code, out, _ = run(cmd)
    if code != 0 or not out:
        return []
    return sorted({line.strip() for line in out.splitlines() if line.strip()})


def branch_age_days(ref: str) -> int:
    code, out, _ = run(["git", "log", "-1", "--format=%ct", ref])
    if code != 0 or not out:
        return 9999
    committed = dt.datetime.fromtimestamp(int(out), tz=dt.timezone.utc)
    return max(0, (dt.datetime.now(dt.timezone.utc) - committed).days)


def score_candidate(ref: str, files: list[str], config: dict[str, Any]) -> dict[str, Any]:
    weights = config.get("scoreWeights", {})
    path_weights = config.get("pathWeights", {})
    short = ref.split("/", 1)[-1]
    score = 0
    reasons: list[str] = []
    if short in config.get("priorityBranches", []):
        score += int(weights.get("priorityBranch", 0))
        reasons.append("priority branch")
    if "helios" in short.lower():
        score += int(weights.get("heliosName", 0)); reasons.append("helios branch name")
    if "hermes" in short.lower():
        score += int(weights.get("hermesName", 0)); reasons.append("hermes branch name")
    categories: dict[str, int] = {}
    for path in files:
        for prefix, category in path_weights.items():
            if path == prefix or path.startswith(prefix.rstrip("/") + "/"):
                categories[category] = categories.get(category, 0) + 1
                break
    for category in sorted(categories):
        score += int(weights.get(category, 0))
        reasons.append(f"{category}:{categories[category]}")
    if len(files) > 250:
        score -= int(weights.get("largeChangePenalty", 0))
        reasons.append("large-change penalty")
    age = branch_age_days(ref)
    freshness = max(0, 30 - min(age, 30))
    score += freshness
    return {
        "branch": ref,
        "shortName": short,
        "score": score,
        "fileCount": len(files),
        "changedFilesSample": files[:25],
        "categories": categories,
        "ageDays": age,
        "reasons": reasons,
        "recommendedOrder": 0
    }


def score_all(config: dict[str, Any], report_dir: Path, fetch: bool = True) -> dict[str, Any]:
    fetch_result = fetch_all() if fetch else {"executed": False}
    target_ref = f"origin/{config.get('targetBranch', 'main')}"
    if run(["git", "rev-parse", "--verify", "--quiet", target_ref])[0] != 0:
        target_ref = config.get("targetBranch", "main")
    candidates = []
    for ref in candidate_branches(config):
        files = changed_files(target_ref, ref)
        candidates.append(score_candidate(ref, files, config))
    candidates.sort(key=lambda item: item["score"], reverse=True)
    for idx, item in enumerate(candidates, 1):
        item["recommendedOrder"] = idx
    payload = {
        "generatedUtc": timestamp(),
        "targetRef": target_ref,
        "integrationBranch": config.get("integrationBranch"),
        "fetch": fetch_result,
        "submodules": sync_submodules(apply=False),
        "candidates": candidates,
    }
    write_json(report_dir / "mass-integration-score.json", payload)
    lines = ["# HELIOS Mass Integration Score", "", f"Generated: {payload['generatedUtc']}", "", "| Order | Branch | Score | Files | Reasons |", "| ---: | --- | ---: | ---: | --- |"]
    for item in candidates:
        lines.append(f"| {item['recommendedOrder']} | `{item['branch']}` | {item['score']} | {item['fileCount']} | {', '.join(item['reasons'])} |")
    write_text(report_dir / "mass-integration-score.md", "\n".join(lines) + "\n")
    return payload


def validate_commands(config: dict[str, Any]) -> list[dict[str, Any]]:
    results = []
    for command in config.get("validationCommands", []):
        code, out, err = run(command.split())
        results.append({"command": command, "exitCode": code, "stdout": out[-2000:], "stderr": err[-2000:]})
    return results


def create_integration_branch(config: dict[str, Any], scored: dict[str, Any], report_dir: Path, apply: bool) -> dict[str, Any]:
    ensure_git_identity()
    integration_branch = config.get("integrationBranch", "integration/helios-hermes-xcore-auto")
    target_branch = config.get("targetBranch", "main")
    target_ref = scored.get("targetRef") or f"origin/{target_branch}"
    actions = []
    if apply:
        run(["git", "checkout", "-B", integration_branch, target_ref], check=True)
        actions.append({"command": f"git checkout -B {integration_branch} {target_ref}", "exitCode": 0})
        submodule_result = sync_submodules(apply=True)
    else:
        actions.append({"command": f"git checkout -B {integration_branch} {target_ref}", "dryRun": True})
        submodule_result = sync_submodules(apply=False)
    merged = []
    failed = []
    for candidate in scored.get("candidates", []):
        branch = candidate["branch"]
        if apply:
            code, out, err = run(["git", "merge", "--no-ff", "--no-edit", branch])
            actions.append({"command": f"git merge --no-ff --no-edit {branch}", "exitCode": code, "stdout": out[-2000:], "stderr": err[-2000:]})
            if code == 0:
                merged.append(branch)
            else:
                run(["git", "merge", "--abort"])
                failed.append({"branch": branch, "error": err or out})
        else:
            actions.append({"command": f"git merge --no-ff --no-edit {branch}", "dryRun": True})
    validation = validate_commands(config) if apply else []
    result = {
        "generatedUtc": timestamp(),
        "integrationBranch": integration_branch,
        "targetBranch": target_branch,
        "apply": apply,
        "actions": actions,
        "submodules": submodule_result,
        "merged": merged,
        "failed": failed,
        "validation": validation,
    }
    write_json(report_dir / "mass-integration-branch.json", result)
    return result


def gh_available() -> bool:
    return shutil.which("gh") is not None


def open_pr(config: dict[str, Any], report_dir: Path, apply: bool) -> dict[str, Any]:
    pr_cfg = config.get("pullRequest", {})
    integration_branch = config.get("integrationBranch", "integration/helios-hermes-xcore-auto")
    target_branch = config.get("targetBranch", "main")
    body_path = report_dir / "mass-integration-score.md"
    if not gh_available():
        result = {"apply": apply, "status": "skipped", "reason": "gh CLI not found"}
        write_json(report_dir / "mass-integration-pr.json", result)
        return result
    commands = []
    if apply:
        push_cmd = ["git", "push", "--set-upstream", "origin", integration_branch, "--force-with-lease"]
        code, out, err = run(push_cmd)
        commands.append({"command": " ".join(push_cmd), "exitCode": code, "stdout": out[-2000:], "stderr": err[-2000:]})
        labels = []
        for label in pr_cfg.get("labels", []):
            labels.extend(["--label", label])
        create_cmd = ["gh", "pr", "create", "--base", target_branch, "--head", integration_branch, "--title", pr_cfg.get("title", "Automated HELIOS integration"), "--body-file", str(body_path), *labels]
        code, out, err = run(create_cmd)
        commands.append({"command": " ".join(create_cmd), "exitCode": code, "stdout": out[-2000:], "stderr": err[-2000:]})
    else:
        commands.append({"command": f"git push --set-upstream origin {integration_branch} --force-with-lease", "dryRun": True})
        commands.append({"command": f"gh pr create --base {target_branch} --head {integration_branch}", "dryRun": True})
    result = {"apply": apply, "commands": commands}
    write_json(report_dir / "mass-integration-pr.json", result)
    return result


def request_auto_merge(config: dict[str, Any], report_dir: Path, apply: bool) -> dict[str, Any]:
    if not gh_available():
        result = {"apply": apply, "status": "skipped", "reason": "gh CLI not found"}
        write_json(report_dir / "mass-integration-auto-merge.json", result)
        return result
    method = config.get("pullRequest", {}).get("autoMergeMethod", "squash")
    integration_branch = config.get("integrationBranch", "integration/helios-hermes-xcore-auto")
    cmd = ["gh", "pr", "merge", integration_branch, "--auto", f"--{method}"]
    delete_branch = bool(config.get("pullRequest", {}).get("deleteBranch", False))
    if delete_branch:
        cmd.append("--delete-branch")
    if apply:
        code, out, err = run(cmd)
        result = {"apply": True, "command": " ".join(cmd), "exitCode": code, "stdout": out[-2000:], "stderr": err[-2000:]}
    else:
        result = {"apply": False, "command": " ".join(cmd), "dryRun": True}
    write_json(report_dir / "mass-integration-auto-merge.json", result)
    return result


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["score", "plan", "branch", "pr", "merge", "all"], help="mass integration mode")
    parser.add_argument("--config", default=str(DEFAULT_CONFIG))
    parser.add_argument("--report-dir", default=str(DEFAULT_REPORT_DIR))
    parser.add_argument("--apply", action="store_true", help="execute mutating git/gh actions")
    parser.add_argument("--no-fetch", action="store_true", help="skip git fetch")
    args = parser.parse_args()

    config = load_config(Path(args.config))
    report_dir = Path(args.report_dir)
    report_dir.mkdir(parents=True, exist_ok=True)

    scored = score_all(config, report_dir, fetch=not args.no_fetch)
    if args.mode in {"branch", "all"}:
        create_integration_branch(config, scored, report_dir, apply=args.apply)
    if args.mode in {"pr", "all"}:
        open_pr(config, report_dir, apply=args.apply)
    if args.mode in {"merge", "all"}:
        request_auto_merge(config, report_dir, apply=args.apply)
    print(f"Wrote reports to {report_dir.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
