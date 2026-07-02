#!/usr/bin/env python3
"""Validate HELIOS/HERMES specialist workstation readiness.

The checker is intentionally non-destructive by default. It reports Git branch and
remote state plus tool availability for Azure, .NET/WinUI, Python AIHub,
PowerShell automation, and C++/CMake work. Remote inference checks explicit
environment/config, GitHub CLI, and historical GitHub merge subjects. Azure login/default mutation is only
performed when explicit flags are supplied.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
from dataclasses import asdict, dataclass
from typing import Iterable, Sequence


DEFAULT_FOCUS_BRANCHES = ("helios-control", "hermes-fleet-production")


@dataclass(frozen=True)
class CheckResult:
    name: str
    status: str
    detail: str = ""
    remediation: str = ""


def run_text(args: Sequence[str], *, check: bool = False) -> tuple[int, str]:
    completed = subprocess.run(
        args,
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )
    output = completed.stdout.strip()
    if check and completed.returncode != 0:
        raise RuntimeError(f"{' '.join(args)} failed: {output}")
    return completed.returncode, output


def command_version(command: str) -> str:
    code, output = run_text([command, "--version"])
    if code != 0 or not output:
        return f"{command} found, but --version did not return cleanly."
    return output.splitlines()[0]



def infer_remote_url() -> str | None:
    explicit = os.environ.get("HELIOS_REMOTE_URL") or os.environ.get("REMOTE_URL")
    if explicit:
        return explicit

    repository = os.environ.get("GITHUB_REPOSITORY")
    if repository:
        server = os.environ.get("GITHUB_SERVER_URL", "https://github.com").rstrip("/")
        return f"{server}/{repository}.git"

    code, configured_repo = run_text(["git", "config", "--get", "github.repository"])
    if code == 0 and configured_repo:
        return f"https://github.com/{configured_repo}.git"

    if shutil.which("gh") is not None:
        code, gh_url = run_text(["gh", "repo", "view", "--json", "url", "--jq", ".url"])
        if code == 0 and gh_url:
            return f"{gh_url.rstrip('/')}.git"

    code, subjects = run_text(["git", "log", "--format=%s", "--max-count=200"])
    if code == 0:
        for subject in subjects.splitlines():
            match = re.search(r"from\s+([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)(?::|$)", subject)
            if match:
                return f"https://github.com/{match.group(1)}.git"

    return None


def ensure_remote(remote_name: str, remote_url: str | None, *, auto_setup: bool) -> CheckResult:
    if not auto_setup:
        return CheckResult(
            "Remote setup",
            "Warn",
            "Automatic remote setup not requested.",
            "Pass --auto-setup-remote with --remote-url, HELIOS_REMOTE_URL, REMOTE_URL, or GITHUB_REPOSITORY.",
        )

    resolved_url = remote_url or infer_remote_url()
    if not resolved_url:
        return CheckResult(
            "Remote setup",
            "Warn",
            "No remote URL could be inferred.",
            "Set HELIOS_REMOTE_URL or pass --remote-url before pushing.",
        )

    existing_code, existing_url = run_text(["git", "remote", "get-url", remote_name])
    if existing_code == 0 and existing_url:
        if existing_url == resolved_url:
            return CheckResult("Remote setup", "Pass", f"{remote_name} already points to {resolved_url}.")
        run_text(["git", "remote", "set-url", remote_name, resolved_url], check=True)
        return CheckResult("Remote setup", "Pass", f"Updated {remote_name} to {resolved_url}.")

    run_text(["git", "remote", "add", remote_name, resolved_url], check=True)
    return CheckResult("Remote setup", "Pass", f"Added {remote_name}: {resolved_url}.")


def push_current_branch(remote_name: str, *, push: bool) -> CheckResult:
    if not push:
        return CheckResult("Git push", "Warn", "Push not requested.", "Pass --push-current to push the current branch after remote setup.")

    _, current_branch = run_text(["git", "rev-parse", "--abbrev-ref", "HEAD"], check=True)
    code, output = run_text(["git", "push", "-u", remote_name, current_branch])
    if code == 0:
        return CheckResult("Git push", "Pass", output or f"Pushed {current_branch} to {remote_name}.")
    return CheckResult("Git push", "Fail", output, "Verify remote URL, credentials, and branch permissions.")

def check_git(focus_branches: Iterable[str]) -> list[CheckResult]:
    if shutil.which("git") is None:
        return [CheckResult("Git", "Fail", "git is not available.", "Install Git and rerun from the repository root.")]

    results: list[CheckResult] = []
    _, repo_root = run_text(["git", "rev-parse", "--show-toplevel"], check=True)
    _, current_branch = run_text(["git", "rev-parse", "--abbrev-ref", "HEAD"], check=True)
    _, status = run_text(["git", "status", "--short"], check=True)
    _, remotes = run_text(["git", "remote"], check=True)
    _, branches = run_text(["git", "branch", "--all", "--no-color"], check=True)

    results.append(CheckResult("Repository", "Pass", f"Root: {repo_root}; branch: {current_branch}"))
    results.append(CheckResult(
        "Working tree",
        "Pass" if not status else "Warn",
        status,
        "Commit or stash local changes before branch integration.",
    ))
    results.append(CheckResult(
        "Git remotes",
        "Pass" if remotes else "Warn",
        remotes,
        "Add an origin/upstream remote before fetching, merging, or pushing.",
    ))

    branch_lines = {line.strip().lstrip("* ").strip() for line in branches.splitlines() if line.strip()}
    for branch in focus_branches:
        present = any(ref == branch or ref.endswith(f"/{branch}") for ref in branch_lines)
        results.append(CheckResult(
            f"Focus branch: {branch}",
            "Pass" if present else "Warn",
            "Branch ref found." if present else "Branch ref not found locally.",
            "Fetch remotes or create the branch before attempting merges.",
        ))

    return results


def check_tools() -> list[CheckResult]:
    tool_specs = (
        ("Azure CLI", "az", "Install Azure CLI 2.53+ from Microsoft Learn."),
        (".NET SDK", "dotnet", "Install the .NET SDK required by HELIOS projects."),
        ("Python", "python", "Install Python 3.11+ for AIHub integration and analytics tooling."),
        ("PowerShell", "pwsh", "Install PowerShell 7+ for cross-platform automation."),
        ("CMake", "cmake", "Install CMake for C++ performance backend builds."),
    )
    results: list[CheckResult] = []
    for name, command, remediation in tool_specs:
        if shutil.which(command) is None:
            results.append(CheckResult(name, "Warn", f"{command} not found.", remediation))
        else:
            results.append(CheckResult(name, "Pass", command_version(command)))
    return results


def configure_azure(args: argparse.Namespace) -> list[CheckResult]:
    results: list[CheckResult] = []
    if shutil.which("az") is None:
        return results

    if args.login_azure:
        run_text(["az", "login"], check=True)

    code, account = run_text(["az", "account", "show", "--output", "json"])
    if code == 0 and account:
        try:
            parsed = json.loads(account)
            user = parsed.get("user", {}).get("name", "unknown")
            subscription = parsed.get("name", "unknown")
            results.append(CheckResult("Azure account", "Pass", f"Signed in as {user}; subscription: {subscription}"))
        except json.JSONDecodeError:
            results.append(CheckResult("Azure account", "Warn", "Azure CLI returned non-JSON account output."))
    else:
        results.append(CheckResult(
            "Azure account",
            "Warn",
            "Azure CLI is installed but not signed in.",
            "Run scripts/setup/setup_specialist_environment.py --login-azure.",
        ))

    if args.configure_defaults:
        if args.azure_subscription:
            run_text(["az", "account", "set", "--subscription", args.azure_subscription], check=True)
        run_text(["az", "configure", "--defaults", f"location={args.azure_location}"], check=True)

    return results


def emit_table(results: Sequence[CheckResult]) -> None:
    widths = {
        "name": max([len("Name"), *(len(item.name) for item in results)]),
        "status": max([len("Status"), *(len(item.status) for item in results)]),
    }
    print(f"{'Name'.ljust(widths['name'])}  {'Status'.ljust(widths['status'])}  Detail")
    print(f"{'-' * widths['name']}  {'-' * widths['status']}  ------")
    for item in results:
        detail = item.detail if not item.remediation else f"{item.detail} Remediation: {item.remediation}"
        print(f"{item.name.ljust(widths['name'])}  {item.status.ljust(widths['status'])}  {detail}")


def parse_args(argv: Sequence[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate HELIOS/HERMES specialist workstation readiness.")
    parser.add_argument("--focus-branch", action="append", dest="focus_branches", help="Branch ref to prioritize; repeatable.")
    parser.add_argument("--login-azure", action="store_true", help="Run az login before checking account state.")
    parser.add_argument("--configure-defaults", action="store_true", help="Configure Azure CLI subscription/location defaults.")
    parser.add_argument("--azure-subscription", help="Azure subscription name or id to set when --configure-defaults is used.")
    parser.add_argument("--azure-location", default="eastus", help="Azure default location to set when --configure-defaults is used.")
    parser.add_argument("--json", action="store_true", help="Emit check results as JSON.")
    parser.add_argument("--remote-name", default="origin", help="Git remote name to create/update when automatic remote setup is requested.")
    parser.add_argument("--remote-url", help="Git remote URL to create/update. Defaults to HELIOS_REMOTE_URL, REMOTE_URL, or GITHUB_REPOSITORY inference.")
    parser.add_argument("--auto-setup-remote", action="store_true", help="Create or update the configured Git remote before pushing.")
    parser.add_argument("--push-current", action="store_true", help="Push the current branch with upstream tracking after remote setup.")
    return parser.parse_args(argv)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    focus_branches = tuple(args.focus_branches or DEFAULT_FOCUS_BRANCHES)
    results = [*check_git(focus_branches), *check_tools(), *configure_azure(args)]
    if args.auto_setup_remote or args.push_current:
        results.append(ensure_remote(args.remote_name, args.remote_url, auto_setup=args.auto_setup_remote or args.push_current))
        if results[-1].status == "Pass":
            results.append(push_current_branch(args.remote_name, push=args.push_current))

    if args.json:
        print(json.dumps([asdict(item) for item in results], indent=2))
    else:
        emit_table(results)

    return 1 if any(item.status == "Fail" for item in results) else 0


if __name__ == "__main__":
    raise SystemExit(main())
