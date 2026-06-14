#!/usr/bin/env python3
"""Set up HELIOS GitHub project, wiki/pages dashboard, milestones, and graph data.

Plan mode writes a dashboard payload without mutating GitHub. Execute mode uses
`gh` with GITHUB_TOKEN/GH_TOKEN to create labels, milestones, a repository project
when possible, and a Pages-ready JSON dashboard artifact.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CONFIG = ROOT / "config" / "automation" / "submodule-consolidation.json"
DEFAULT_OUTPUT = ROOT / "artifacts" / "github-control-plane"


@dataclass(frozen=True)
class CommandResult:
    command: str
    returncode: int | None
    stdout: str
    stderr: str


def run(command: list[str], timeout: int = 120) -> CommandResult:
    try:
        completed = subprocess.run(command, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=timeout, check=False)
        return CommandResult(" ".join(command), completed.returncode, completed.stdout.strip(), completed.stderr.strip())
    except (FileNotFoundError, subprocess.TimeoutExpired) as exc:
        return CommandResult(" ".join(command), None, "", str(exc))


def load_config(path: Path) -> dict[str, object]:
    return json.loads(path.read_text(encoding="utf-8"))


def repo_name() -> str:
    env_repo = os.environ.get("GITHUB_REPOSITORY", "")
    if env_repo:
        return env_repo
    remote = run(["git", "config", "--get", "remote.origin.url"])
    text = remote.stdout
    if "github.com" in text:
        return text.rstrip(".git").split("github.com", 1)[1].strip(":/")
    return ""


def gh_available() -> bool:
    return run(["gh", "--version"]).returncode == 0 and bool(os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN"))


def upsert_label(repo: str, name: str, description: str) -> dict[str, object]:
    color = "5319e7" if name.startswith("ai") else "0e8a16"
    result = run(["gh", "label", "create", name, "--repo", repo, "--description", description, "--color", color, "--force"])
    return {"kind": "label", "name": name, "returncode": result.returncode, "stderr": result.stderr}


def upsert_milestone(repo: str, title: str) -> dict[str, object]:
    existing = run(["gh", "api", f"repos/{repo}/milestones", "--jq", f".[] | select(.title==\"{title}\") | .number"])
    if existing.returncode == 0 and existing.stdout:
        return {"kind": "milestone", "title": title, "status": "exists", "number": existing.stdout.splitlines()[0]}
    created = run(["gh", "api", f"repos/{repo}/milestones", "-f", f"title={title}", "-f", "state=open"])
    return {"kind": "milestone", "title": title, "status": "created" if created.returncode == 0 else "failed", "returncode": created.returncode, "stderr": created.stderr}


def create_project(repo: str, title: str) -> dict[str, object]:
    # Repository Projects v2 are owner scoped; use a best-effort gh project command.
    owner = repo.split("/", 1)[0] if "/" in repo else repo
    existing = run(["gh", "project", "list", "--owner", owner, "--format", "json", "--jq", f".projects[] | select(.title==\"{title}\") | .number"])
    if existing.returncode == 0 and existing.stdout:
        return {"kind": "project", "title": title, "status": "exists", "number": existing.stdout.splitlines()[0]}
    created = run(["gh", "project", "create", "--owner", owner, "--title", title])
    return {"kind": "project", "title": title, "status": "created" if created.returncode == 0 else "failed", "returncode": created.returncode, "stderr": created.stderr}


def build_dashboard(config: dict[str, object], actions: list[dict[str, object]], repo: str) -> dict[str, object]:
    lanes = config.get("submodule_lanes", [])
    priority = config.get("priority_repositories", [])
    return {
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "repository": repo,
        "automation_graph": {
            "nodes": [repo_item.get("name") for repo_item in priority] + [lane.get("lane") for lane in lanes],
            "edges": [{"from": repo_item.get("name"), "to": repo_item.get("lane"), "type": "promotes-unique-code"} for repo_item in priority],
        },
        "milestones": config.get("github_control_plane", {}).get("milestones", []),
        "labels": list(config.get("github_control_plane", {}).get("labels", {}).keys()),
        "actions": actions,
    }


def write_pages_dashboard(config: dict[str, object], dashboard: dict[str, object], output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir / "automation-dashboard.json").write_text(json.dumps(dashboard, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Automation Control Plane", "", f"Generated UTC: `{dashboard['generated_utc']}`", "", "## Graph nodes"]
    lines.extend(f"- `{node}`" for node in dashboard["automation_graph"]["nodes"] if node)
    lines.extend(["", "## Graph edges"])
    lines.extend(f"- `{edge['from']}` → `{edge['to']}` ({edge['type']})" for edge in dashboard["automation_graph"]["edges"])
    (output_dir / "automation-dashboard.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Set up HELIOS GitHub control plane assets")
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--execute", action="store_true")
    args = parser.parse_args()

    config_path = args.config if args.config.is_absolute() else ROOT / args.config
    output_dir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
    config = load_config(config_path)
    control = config.get("github_control_plane", {})
    repo = repo_name()
    can_execute = args.execute and repo and gh_available()
    actions: list[dict[str, object]] = []
    if can_execute:
        for name, description in control.get("labels", {}).items():
            actions.append(upsert_label(repo, str(name), str(description)))
        for title in control.get("milestones", []):
            actions.append(upsert_milestone(repo, str(title)))
        project_title = str(control.get("project_title", "HELIOS AI Automation Control Plane"))
        actions.append(create_project(repo, project_title))
    else:
        actions.append({"kind": "plan", "status": "not-executed", "reason": "missing --execute, gh, token, or repository context"})
    dashboard = build_dashboard(config, actions, repo)
    write_pages_dashboard(config, dashboard, output_dir)
    print(f"Repository: {repo or 'unknown'}")
    print(f"Executed: {can_execute}")
    print(f"Actions: {len(actions)}")
    return 1 if any(action.get("status") == "failed" for action in actions) else 0


if __name__ == "__main__":
    raise SystemExit(main())
