#!/usr/bin/env python3
"""HELIOS branch/repository consolidation and Azure CLI automation.

The script is deliberately plan-first: it validates prerequisites and prints an
ordered merge/submodule/subtree plan by default. Use --apply to execute the safe
setup/fetch/submodule/subtree commands after reviewing the plan.
"""

from __future__ import annotations

import argparse
import json
import os
import platform
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence


LANGUAGE_EXTENSIONS = {
    "csharp_winui": {".cs", ".xaml", ".csproj", ".sln"},
    "cpp_backend": {".c", ".cc", ".cpp", ".cxx", ".h", ".hpp", ".vcxproj"},
    "fsharp_analytics": {".fs", ".fsx", ".fsi", ".fsproj"},
    "python_ai_hub": {".py", ".ipynb", ".toml", ".requirements", ".txt"},
    "azure_github_automation": {".ps1", ".psm1", ".yml", ".yaml", ".json", ".bicep"},
}


@dataclass(frozen=True)
class Source:
    id: str
    kind: str
    remote: str
    url: str
    branch: str
    order: int
    mode: str
    path: str
    areas: tuple[str, ...]

    @classmethod
    def from_json(cls, item: dict[str, object]) -> "Source":
        return cls(
            id=str(item["id"]),
            kind=str(item["kind"]),
            remote=str(item["remote"]),
            url=str(item["url"]),
            branch=str(item["branch"]),
            order=int(item["order"]),
            mode=str(item["mode"]),
            path=str(item["path"]),
            areas=tuple(str(area) for area in item.get("areas", [])),
        )


class CommandError(RuntimeError):
    pass


def run(cmd: Sequence[str], cwd: Path, apply: bool, check: bool = True) -> subprocess.CompletedProcess[str]:
    printable = " ".join(cmd)
    if not apply:
        print(f"DRY-RUN: {printable}")
        return subprocess.CompletedProcess(cmd, 0, "", "")
    print(f"RUN: {printable}")
    result = subprocess.run(cmd, cwd=cwd, text=True, capture_output=True, check=False)
    if result.stdout:
        print(result.stdout.rstrip())
    if result.stderr:
        print(result.stderr.rstrip(), file=sys.stderr)
    if check and result.returncode != 0:
        raise CommandError(f"Command failed ({result.returncode}): {printable}")
    return result


def capture(cmd: Sequence[str], cwd: Path, check: bool = True) -> str:
    result = subprocess.run(cmd, cwd=cwd, text=True, capture_output=True, check=False)
    if check and result.returncode != 0:
        raise CommandError(result.stderr.strip() or f"Command failed: {' '.join(cmd)}")
    return result.stdout.strip()


def load_sources(manifest: Path) -> tuple[dict[str, object], list[Source]]:
    data = json.loads(manifest.read_text(encoding="utf-8"))
    sources = [Source.from_json(item) for item in data["sources"]]
    return data, sorted(sources, key=lambda source: source.order)


def assert_git_repo(repo: Path) -> None:
    inside = capture(["git", "rev-parse", "--is-inside-work-tree"], repo)
    if inside.lower() != "true":
        raise CommandError(f"Not a git work tree: {repo}")


def require_clean_tree(repo: Path, allow_untracked: bool) -> None:
    status = capture(["git", "status", "--porcelain"], repo)
    if not status:
        return
    blocking = []
    for line in status.splitlines():
        if allow_untracked and line.startswith("?? "):
            continue
        blocking.append(line)
    if blocking:
        details = "\n".join(blocking)
        raise CommandError(f"Working tree has tracked changes; commit or stash first:\n{details}")
    print("NOTICE: working tree has only untracked files; continuing because --allow-untracked is enabled.")


def ensure_remote(repo: Path, source: Source, apply: bool) -> None:
    remotes = capture(["git", "remote"], repo, check=False).splitlines()
    if source.remote in remotes:
        run(["git", "remote", "set-url", source.remote, source.url], repo, apply)
    else:
        run(["git", "remote", "add", source.remote, source.url], repo, apply)


def fetch_source(repo: Path, source: Source, apply: bool) -> None:
    ensure_remote(repo, source, apply)
    run(["git", "fetch", "--prune", source.remote, source.branch], repo, apply)


def path_exists(repo: Path, source: Source) -> bool:
    return (repo / source.path).exists()


def subtree_command(repo: Path, source: Source) -> list[str]:
    action = "pull" if path_exists(repo, source) else "add"
    return [
        "git",
        "subtree",
        action,
        f"--prefix={source.path}",
        source.remote,
        source.branch,
        "--squash",
    ]


def submodule_command(repo: Path, source: Source) -> list[str]:
    if path_exists(repo, source):
        return ["git", "submodule", "update", "--init", "--remote", "--", source.path]
    return ["git", "submodule", "add", "-b", source.branch, source.url, source.path]


def history_merge_command(source: Source) -> list[str]:
    return [
        "git",
        "merge",
        "--no-ff",
        "--no-commit",
        "--allow-unrelated-histories",
        f"{source.remote}/{source.branch}",
    ]


def plan_source(repo: Path, source: Source, apply: bool) -> None:
    if source.kind == "current_repository":
        print(f"SKIP: {source.id} is the integration baseline ({source.branch}).")
        return

    print(f"\n== {source.order}. {source.id} [{source.mode}] ==")
    print(f"Areas: {', '.join(source.areas)}")
    fetch_source(repo, source, apply)

    if source.mode == "subtree":
        run(subtree_command(repo, source), repo, apply)
    elif source.mode == "submodule":
        run(submodule_command(repo, source), repo, apply)
    elif source.mode == "history":
        run(history_merge_command(source), repo, apply)
    else:
        raise CommandError(f"Unknown consolidation mode for {source.id}: {source.mode}")


def list_current_repo_branches(repo: Path, target_branch: str) -> list[str]:
    output = capture(["git", "branch", "--all", "--format=%(refname:short)"], repo)
    branches = []
    for raw in output.splitlines():
        branch = raw.strip()
        if not branch or branch == target_branch or branch.endswith(f"/{target_branch}"):
            continue
        if branch.startswith("remotes/") and branch.endswith("/HEAD"):
            continue
        branches.append(branch)
    return sorted(set(branches))


def plan_current_repo_branch_merges(repo: Path, target_branch: str, apply: bool) -> None:
    branches = list_current_repo_branches(repo, target_branch)
    if not branches:
        print("No additional local/remote branches were found to merge into the target branch.")
        return
    print("\n== Current repository branch merge plan ==")
    for branch in branches:
        run(["git", "merge", "--no-ff", "--no-commit", branch], repo, apply=False)
    if apply:
        print("Branch merges are intentionally plan-only. Run each displayed merge after reviewing branch ownership and conflicts.")


def count_languages(root: Path) -> dict[str, int]:
    counts = {key: 0 for key in LANGUAGE_EXTENSIONS}
    if not root.exists():
        return counts
    ignored_parts = {".git", "bin", "obj", "node_modules", ".venv", "venv", "__pycache__"}
    for current, dirs, files in os.walk(root):
        dirs[:] = [name for name in dirs if name not in ignored_parts]
        for file_name in files:
            suffix = Path(file_name).suffix.lower()
            for language, extensions in LANGUAGE_EXTENSIONS.items():
                if suffix in extensions or file_name.lower() in extensions:
                    counts[language] += 1
    return counts


def print_language_scan(repo: Path, sources: Iterable[Source]) -> None:
    print("\n== Language/domain scan ==")
    for source in sources:
        root = repo if source.kind == "current_repository" else repo / source.path
        counts = count_languages(root)
        summary = ", ".join(f"{name}={count}" for name, count in counts.items() if count)
        print(f"{source.id}: {summary or 'no scanned files present yet'}")


def azure_cli_install_command() -> list[str] | None:
    system = platform.system().lower()
    if system == "windows":
        if shutil.which("winget"):
            return ["winget", "install", "--exact", "--id", "Microsoft.AzureCLI", "--accept-package-agreements", "--accept-source-agreements"]
        if shutil.which("choco"):
            return ["choco", "install", "azure-cli", "-y"]
    if system == "darwin" and shutil.which("brew"):
        return ["bash", "-lc", "brew update && brew install azure-cli"]
    if system == "linux":
        return ["bash", "-lc", "curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"]
    return None


def setup_azure_cli(repo: Path, apply: bool, install: bool) -> None:
    print("\n== Azure CLI setup ==")
    if shutil.which("az"):
        version = capture(["az", "version", "--output", "json"], repo, check=False)
        print(f"Azure CLI is available: {version[:300]}")
    elif install:
        command = azure_cli_install_command()
        if not command:
            raise CommandError("No supported Azure CLI installer was found for this OS/package manager.")
        run(command, repo, apply)
    else:
        print("Azure CLI is not installed. Re-run with --install-azure-cli --apply after reviewing installer requirements.")

    run(["az", "extension", "add", "--name", "azure-devops", "--upgrade"], repo, apply and shutil.which("az") is not None, check=False)
    run(["az", "account", "show", "--output", "table"], repo, apply and shutil.which("az") is not None, check=False)


def write_plan(repo: Path, sources: list[Source], output: Path) -> None:
    lines = [
        "# HELIOS Consolidation Execution Plan",
        "",
        "This generated plan orders branch/repository consolidation by the source manifest.",
        "Review conflicts before running commands with `--apply`.",
        "",
        "## Ordered sources",
        "",
    ]
    for source in sources:
        lines.append(f"{source.order}. **{source.id}** — mode `{source.mode}`, branch `{source.branch}`, path `{source.path}`, areas `{', '.join(source.areas)}`")
    lines.extend([
        "",
        "## Safety gates",
        "",
        "- Keep `helios-control` authoritative for C#/WinUI 3 shell and control-plane conflicts.",
        "- Keep `hermes-fleet-production` authoritative for F# prediction, fleet analytics, and parallel math conflicts.",
        "- Keep `helios-monado-blade` authoritative for C/C++ performance backend and native security conflicts.",
        "- Keep `helios-ai-hub` authoritative for Python AI Hub integration conflicts.",
        "- Keep `helios-build-agents` authoritative for Azure CLI, GitHub Actions, CI/CD, and cloud automation conflicts.",
    ])
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    try:
        display_path = output.relative_to(repo)
    except ValueError:
        display_path = output
    print(f"Wrote plan: {display_path}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Plan and automate HELIOS branch/source consolidation.")
    parser.add_argument("--manifest", default="scripts/automation/consolidation-sources.json", help="JSON consolidation source manifest.")
    parser.add_argument("--target-branch", default=None, help="Integration branch name; defaults to manifest default_target_branch.")
    parser.add_argument("--source", action="append", help="Limit to one or more source ids.")
    parser.add_argument("--apply", action="store_true", help="Execute remote/fetch/subtree/submodule/Azure commands. Default is dry-run.")
    parser.add_argument("--allow-untracked", action="store_true", help="Allow untracked files while blocking tracked modifications.")
    parser.add_argument("--include-current-branches", action="store_true", help="Print a plan for merging every other branch in the current repository.")
    parser.add_argument("--scan", action="store_true", help="Scan available source directories for C#, C/C++, F#, Python, and automation assets.")
    parser.add_argument("--setup-azure-cli", action="store_true", help="Validate Azure CLI and Azure DevOps extension setup.")
    parser.add_argument("--install-azure-cli", action="store_true", help="Install Azure CLI when missing; requires --setup-azure-cli and --apply to execute.")
    parser.add_argument(
        "--write-plan",
        nargs="?",
        const="docs/integration/HELIOS_CONSOLIDATION_EXECUTION_PLAN.md",
        default=None,
        metavar="PATH",
        help="Write a generated markdown plan to PATH, or to docs/integration/HELIOS_CONSOLIDATION_EXECUTION_PLAN.md when no PATH is provided.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo = Path.cwd()
    manifest_path = repo / args.manifest
    data, sources = load_sources(manifest_path)
    target_branch = args.target_branch or str(data.get("default_target_branch", "work"))

    if args.source:
        selected = set(args.source)
        sources = [source for source in sources if source.id in selected]
        missing = selected.difference(source.id for source in sources)
        if missing:
            raise CommandError(f"Unknown source id(s): {', '.join(sorted(missing))}")

    assert_git_repo(repo)
    require_clean_tree(repo, allow_untracked=args.allow_untracked or not args.apply)

    current_branch = capture(["git", "branch", "--show-current"], repo)
    if current_branch != target_branch:
        print(f"WARNING: current branch is {current_branch!r}; target branch is {target_branch!r}.")

    if args.write_plan:
        write_plan(repo, sources, repo / args.write_plan)

    if args.include_current_branches:
        plan_current_repo_branch_merges(repo, target_branch, args.apply)

    print("\n== Ordered consolidation plan ==")
    for source in sources:
        plan_source(repo, source, args.apply)

    if args.scan:
        print_language_scan(repo, sources)

    if args.setup_azure_cli:
        setup_azure_cli(repo, args.apply, args.install_azure_cli)

    print("\nComplete. Review git status and resolve any conflicts before committing merge results.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except CommandError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
