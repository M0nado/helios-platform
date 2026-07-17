#!/usr/bin/env python3
"""Set up GitHub repository automation for HELIOS mass integration.

The script verifies or applies repository-level settings needed by the no-review
runner path: labels, auto-merge-capable repository settings, optional branch
protection, and workflow presence. Mutating GitHub calls require --apply and a
usable gh authentication context.
"""
from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CONFIG = ROOT / "config" / "helios-github-setup.json"
DEFAULT_OUT = ROOT / "reports" / "github-setup"


def run(cmd: list[str], stdin: str | None = None) -> dict[str, Any]:
    proc = subprocess.run(cmd, cwd=ROOT, text=True, input=stdin, capture_output=True)
    return {"command": " ".join(cmd), "exitCode": proc.returncode, "stdout": proc.stdout[-3000:], "stderr": proc.stderr[-3000:]}


def load_config(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def gh_available() -> bool:
    return shutil.which("gh") is not None


def repo_name() -> str:
    if os.environ.get("GITHUB_REPOSITORY"):
        return os.environ["GITHUB_REPOSITORY"]
    result = run(["gh", "repo", "view", "--json", "nameWithOwner", "--jq", ".nameWithOwner"])
    return result["stdout"].strip() if result["exitCode"] == 0 else ""


def workflow_status(config: dict[str, Any]) -> dict[str, bool]:
    return {path: (ROOT / path).exists() for path in config.get("workflows", [])}


def secret_status(config: dict[str, Any]) -> dict[str, bool]:
    names = config.get("requiredSecrets", []) + config.get("optionalSecrets", [])
    return {name: bool(os.environ.get(name)) for name in names}


def apply_repository_settings(repo: str, config: dict[str, Any]) -> list[dict[str, Any]]:
    settings = config.get("repository", {})
    payload = {
        "allow_auto_merge": bool(settings.get("autoMerge", True)),
        "delete_branch_on_merge": bool(settings.get("deleteBranchOnMerge", False)),
        "allow_squash_merge": bool(settings.get("squashMerge", True)),
        "allow_merge_commit": bool(settings.get("mergeCommit", True)),
        "allow_rebase_merge": bool(settings.get("rebaseMerge", True)),
    }
    return [run(["gh", "api", f"repos/{repo}", "--method", "PATCH", "--input", "-"], stdin=json.dumps(payload)) | {"payload": payload}]


def apply_labels(config: dict[str, Any]) -> list[dict[str, Any]]:
    results = []
    for label in config.get("labels", []):
        create = run([
            "gh", "label", "create", label["name"],
            "--color", label.get("color", "ededed"),
            "--description", label.get("description", ""),
        ])
        if create["exitCode"] != 0:
            edit = run([
                "gh", "label", "edit", label["name"],
                "--color", label.get("color", "ededed"),
                "--description", label.get("description", ""),
            ])
            results.append({"label": label["name"], "create": create, "edit": edit})
        else:
            results.append({"label": label["name"], "create": create})
    return results


def apply_branch_protection(repo: str, config: dict[str, Any]) -> dict[str, Any]:
    protection = config.get("branchProtection", {})
    if not protection.get("enabled", False):
        return {"enabled": False, "result": "skipped"}
    branch = protection.get("branch", "main")
    payload = {
        "required_status_checks": {
            "strict": True,
            "contexts": protection.get("requiredStatusChecks", []),
        },
        "enforce_admins": bool(protection.get("enforceAdmins", False)),
        "required_pull_request_reviews": {
            "required_approving_review_count": int(protection.get("requiredApprovingReviewCount", 0))
        },
        "restrictions": None,
    }
    result = run(["gh", "api", f"repos/{repo}/branches/{branch}/protection", "--method", "PUT", "--input", "-"], stdin=json.dumps(payload))
    result["payload"] = payload
    return {"enabled": True, "branch": branch, "result": result}


def write_reports(out_dir: Path, payload: dict[str, Any]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "github-setup.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS GitHub Repository Setup", "", f"Apply mode: {payload['apply']}", f"Repository: {payload.get('repository') or '-'}", "", "## Workflows", ""]
    for path, present in payload["workflows"].items():
        lines.append(f"- {'✅' if present else '❌'} `{path}`")
    lines.extend(["", "## Secrets / token environment", ""])
    for name, present in payload["secrets"].items():
        lines.append(f"- {'✅' if present else '⚠️'} `{name}`")
    lines.extend(["", "## Actions", ""])
    for action in payload.get("actions", []):
        lines.append(f"- `{action.get('command', action.get('label', 'action'))}` → {action.get('exitCode', action.get('result', 'recorded'))}")
    (out_dir / "github-setup.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["verify", "setup"], nargs="?", default="verify")
    parser.add_argument("--config", default=str(DEFAULT_CONFIG))
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    apply = args.apply or args.mode == "setup"
    config = load_config(Path(args.config))
    repo = repo_name() if gh_available() else os.environ.get("GITHUB_REPOSITORY", "")
    actions: list[dict[str, Any]] = []
    if apply:
        if not gh_available():
            actions.append({"result": "gh CLI unavailable; cannot apply repository settings"})
        elif not repo:
            actions.append({"result": "repository could not be resolved"})
        else:
            actions.extend(apply_repository_settings(repo, config))
            actions.extend(apply_labels(config))
            actions.append(apply_branch_protection(repo, config))
    payload = {
        "apply": apply,
        "repository": repo,
        "ghAvailable": gh_available(),
        "workflows": workflow_status(config),
        "secrets": secret_status(config),
        "actions": actions,
    }
    write_reports(Path(args.out), payload)
    print(f"Wrote {(Path(args.out) / 'github-setup.md').relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
