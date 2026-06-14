#!/usr/bin/env python3
"""Triage old bug issues and optionally automate verified fix PRs.

Default mode is read-only: it inventories old GitHub issues labeled as bugs or
optimization work and writes a report. Execution mode requires an operator-provided
HELIOS_AI_FIX_COMMAND so this repository can plug in a preferred AI coding agent
without committing provider secrets or hard-coded model choices.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_LABELS = "bug,performance,optimization"


@dataclass
class Issue:
    number: int
    title: str
    url: str
    created_at: str
    labels: list[str] = field(default_factory=list)
    body: str = ""

    @property
    def safe_slug(self) -> str:
        slug = re.sub(r"[^a-z0-9]+", "-", self.title.lower()).strip("-")[:48]
        return slug or "issue"


@dataclass(frozen=True)
class CommandResult:
    command: str
    returncode: int
    stdout: str
    stderr: str


def run(command: list[str], *, env: dict[str, str] | None = None, timeout: int | None = None) -> CommandResult:
    completed = subprocess.run(
        command,
        cwd=ROOT,
        env=env,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=timeout,
        check=False,
    )
    return CommandResult(" ".join(command), completed.returncode, completed.stdout.strip(), completed.stderr.strip())


def run_shell(command: str, *, env: dict[str, str], timeout: int | None = None) -> CommandResult:
    completed = subprocess.run(
        command,
        cwd=ROOT,
        env=env,
        shell=True,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=timeout,
        check=False,
    )
    return CommandResult(command, completed.returncode, completed.stdout.strip(), completed.stderr.strip())


def parse_issue(raw: dict[str, object]) -> Issue:
    labels = []
    for label in raw.get("labels", []) or []:
        if isinstance(label, dict):
            labels.append(str(label.get("name", "")))
        else:
            labels.append(str(label))
    return Issue(
        number=int(raw["number"]),
        title=str(raw.get("title", "")),
        url=str(raw.get("url", "")),
        created_at=str(raw.get("createdAt", "")),
        labels=[label for label in labels if label],
        body=str(raw.get("body", "") or ""),
    )


def list_issues(labels: str, age_days: int, limit: int) -> tuple[list[Issue], str]:
    if not os.environ.get("GH_TOKEN") and not os.environ.get("GITHUB_TOKEN"):
        return [], "GH_TOKEN/GITHUB_TOKEN is not set; issue inventory skipped."
    cutoff = (datetime.now(timezone.utc) - timedelta(days=age_days)).date().isoformat()
    label_terms = [f"label:{label.strip()}" for label in labels.split(",") if label.strip()]
    label_query = " OR ".join(label_terms)
    search = f"is:issue is:open created:<={cutoff} ({label_query})" if label_query else f"is:issue is:open created:<={cutoff}"
    result = run([
        "gh", "issue", "list",
        "--search", search,
        "--limit", str(limit),
        "--json", "number,title,url,createdAt,labels,body",
    ])
    if result.returncode != 0:
        return [], result.stderr or result.stdout or "gh issue list failed"
    try:
        data = json.loads(result.stdout or "[]")
    except json.JSONDecodeError as exc:
        return [], f"Unable to parse gh issue output: {exc}"
    return [parse_issue(item) for item in data], ""


def has_changes() -> bool:
    return bool(run(["git", "status", "--short"]).stdout.strip())


def current_branch() -> str:
    return run(["git", "branch", "--show-current"]).stdout.strip()


def execute_issue(issue: Issue, args: argparse.Namespace) -> dict[str, object]:
    command = os.environ.get("HELIOS_AI_FIX_COMMAND", "").strip()
    if not command:
        return {"issue": issue.number, "status": "skipped", "reason": "HELIOS_AI_FIX_COMMAND is not configured"}

    base_branch = args.base_branch
    branch = f"ai/bugfix-{issue.number}-{issue.safe_slug}"
    env = os.environ.copy()
    env.update({
        "HELIOS_ISSUE_NUMBER": str(issue.number),
        "HELIOS_ISSUE_TITLE": issue.title,
        "HELIOS_ISSUE_URL": issue.url,
        "HELIOS_ISSUE_BODY": issue.body,
        "HELIOS_BASE_BRANCH": base_branch,
        "HELIOS_WORK_BRANCH": branch,
    })

    steps: list[dict[str, object]] = []

    def clean_failed(result: dict[str, object]) -> dict[str, object]:
        run(["git", "reset", "--hard"], env=env)
        run(["git", "clean", "-fd"], env=env)
        run(["git", "checkout", "-"], env=env)
        return result

    for git_command in (["git", "fetch", "origin", base_branch], ["git", "checkout", f"origin/{base_branch}"], ["git", "checkout", "-B", branch]):
        result = run(git_command, env=env)
        steps.append({"command": result.command, "returncode": result.returncode})
        if result.returncode != 0:
            return clean_failed({"issue": issue.number, "status": "failed", "stage": "git-setup", "steps": steps, "stderr": result.stderr})

    fix = run_shell(command, env=env, timeout=args.fix_timeout_seconds)
    steps.append({"command": "HELIOS_AI_FIX_COMMAND", "returncode": fix.returncode})
    if fix.returncode != 0:
        return clean_failed({"issue": issue.number, "status": "failed", "stage": "fix-command", "steps": steps, "stderr": fix.stderr[-4000:]})
    if not has_changes():
        return clean_failed({"issue": issue.number, "status": "no-change", "steps": steps})

    test = run_shell(args.test_command, env=env, timeout=args.test_timeout_seconds)
    steps.append({"command": args.test_command, "returncode": test.returncode})
    if test.returncode != 0:
        return clean_failed({"issue": issue.number, "status": "failed", "stage": "tests", "steps": steps, "stderr": test.stderr[-4000:]})

    commit_message = f"Fix issue #{issue.number}: {issue.title[:72]}"
    run(["git", "add", "-A"], env=env)
    commit = run(["git", "commit", "-m", commit_message], env=env)
    steps.append({"command": "git commit", "returncode": commit.returncode})
    if commit.returncode != 0:
        return clean_failed({"issue": issue.number, "status": "failed", "stage": "commit", "steps": steps, "stderr": commit.stderr})

    push = run(["git", "push", "--set-upstream", "origin", branch], env=env)
    steps.append({"command": "git push", "returncode": push.returncode})
    if push.returncode != 0:
        return clean_failed({"issue": issue.number, "status": "failed", "stage": "push", "steps": steps, "stderr": push.stderr})

    pr_body = f"Automated candidate fix for {issue.url}.\n\nValidation command: `{args.test_command}`\n"
    pr = run(["gh", "pr", "create", "--base", base_branch, "--head", branch, "--title", commit_message, "--body", pr_body], env=env)
    steps.append({"command": "gh pr create", "returncode": pr.returncode})
    if pr.returncode != 0:
        return clean_failed({"issue": issue.number, "status": "failed", "stage": "pr-create", "steps": steps, "stderr": pr.stderr})

    result = {"issue": issue.number, "status": "pr-created", "steps": steps, "pr_url": pr.stdout.strip()}
    if args.enable_automerge:
        merge = run(["gh", "pr", "merge", "--auto", "--squash", "--delete-branch"], env=env)
        result["automerge"] = {"returncode": merge.returncode, "stderr": merge.stderr}
    return result


def write_reports(issues: list[Issue], results: list[dict[str, object]], note: str, output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    payload = {
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "current_branch": current_branch(),
        "note": note,
        "issues": [issue.__dict__ for issue in issues],
        "results": results,
    }
    (output_dir / "ai-issue-backlog.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS AI Bugfix and Optimization Backlog", "", f"Generated UTC: `{payload['generated_utc']}`", f"Current branch: `{payload['current_branch']}`"]
    if note:
        lines.extend(["", f"> {note}"])
    lines.extend(["", "## Candidate issues"])
    if issues:
        lines.append("| Issue | Labels | Created | Title |")
        lines.append("| ---: | --- | --- | --- |")
        for issue in issues:
            lines.append(f"| [#{issue.number}]({issue.url}) | {', '.join(issue.labels)} | {issue.created_at} | {issue.title} |")
    else:
        lines.append("- No candidate issues found or GitHub issue access is unavailable.")
    lines.extend(["", "## Automation results"])
    if results:
        lines.append("| Issue | Status | Detail |")
        lines.append("| ---: | --- | --- |")
        for result in results:
            lines.append(f"| #{result.get('issue')} | {result.get('status')} | {result.get('stage') or result.get('reason') or result.get('pr_url') or ''} |")
    else:
        lines.append("- Read-only inventory mode; no fix commands were executed.")
    (output_dir / "ai-issue-backlog.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="HELIOS old-bug AI automation runner")
    parser.add_argument("--labels", default=DEFAULT_LABELS)
    parser.add_argument("--age-days", type=int, default=30)
    parser.add_argument("--max-issues", type=int, default=3)
    parser.add_argument("--base-branch", default=os.environ.get("GITHUB_BASE_REF") or "main")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "artifacts" / "ai-issue-automation")
    parser.add_argument("--execute", action="store_true")
    parser.add_argument("--enable-automerge", action="store_true")
    parser.add_argument("--test-command", default="python3 -m compileall scripts/automation")
    parser.add_argument("--fix-timeout-seconds", type=int, default=1800)
    parser.add_argument("--test-timeout-seconds", type=int, default=1200)
    args = parser.parse_args()

    issues, note = list_issues(args.labels, args.age_days, args.max_issues)
    results = [execute_issue(issue, args) for issue in issues] if args.execute else []
    output_dir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
    write_reports(issues, results, note, output_dir)
    print(f"Candidate issues: {len(issues)}")
    print(f"Executed fixes: {len(results)}")
    return 1 if any(result.get("status") == "failed" for result in results) else 0


if __name__ == "__main__":
    raise SystemExit(main())
