#!/usr/bin/env python3
"""HELIOS branch consolidation, merge planning, and Azure CLI readiness automation.

This tool is intentionally stdlib-only so it can run before PowerShell, .NET, Azure CLI,
or Python package restore are configured. It uses MERGE_SOURCE_MANIFEST.yaml as the source
of truth for repository priority and conflict-ownership hints, then produces a deterministic
merge plan or executes guarded merges when explicitly requested.
"""

from __future__ import annotations

import argparse
import json
import platform
import shutil
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable, Sequence

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_MANIFEST = REPO_ROOT / "MERGE_SOURCE_MANIFEST.yaml"

AREA_HINTS = {
    "csharp_winui": [".cs", ".xaml", ".csproj", ".sln", ".slnx"],
    "cpp_backend": [".cpp", ".cxx", ".cc", ".hpp", ".h", ".vcxproj"],
    "fsharp_analytics": [".fs", ".fsi", ".fsx", ".fsproj"],
    "python_ai_hub": [".py", ".ipynb", "requirements.txt", "pyproject.toml"],
    "azure_github_automation": [".ps1", ".bicep", ".tf", ".yml", ".yaml"],
}

PRIORITY_NAME_HINTS = ("helios-control", "hermes-fleet-production")


@dataclass(slots=True)
class MergeSource:
    """A repository or branch input from the consolidation manifest."""

    source_id: str
    kind: str = "unknown"
    remote_name: str = ""
    remote_url_or_path: str = ""
    target_branch: str = ""
    source_branch: str = ""
    merge_order: int = 999
    consolidation_mode: str = "history"
    primary_areas: list[str] = field(default_factory=list)
    notes: str = ""

    @property
    def priority(self) -> int:
        if self.source_id in PRIORITY_NAME_HINTS:
            return -100 + self.merge_order
        return self.merge_order

    @property
    def ref_candidates(self) -> list[str]:
        candidates: list[str] = []
        if self.remote_name and self.source_branch:
            candidates.append(f"{self.remote_name}/{self.source_branch}")
        if self.source_branch:
            candidates.append(self.source_branch)
        if self.remote_name:
            candidates.append(self.remote_name)
        return candidates


def run_git(args: Sequence[str], *, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        check=check,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )


def require_git_repo() -> None:
    result = run_git(["rev-parse", "--show-toplevel"], check=False)
    if result.returncode != 0:
        raise SystemExit("This command must be run inside a git repository.")
    if Path(result.stdout.strip()).resolve() != REPO_ROOT:
        raise SystemExit(f"Unexpected repository root: {result.stdout.strip()}")


def parse_scalar(value: str) -> str:
    value = value.strip()
    if (value.startswith('"') and value.endswith('"')) or (value.startswith("'") and value.endswith("'")):
        return value[1:-1]
    return value


def parse_manifest(path: Path) -> list[MergeSource]:
    """Parse the limited YAML shape used by MERGE_SOURCE_MANIFEST.yaml without PyYAML."""
    if not path.exists():
        raise FileNotFoundError(path)

    sources: list[MergeSource] = []
    current: dict[str, object] | None = None
    list_key: str | None = None
    in_merge_sources = False

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        stripped = raw_line.strip()
        if not stripped or stripped.startswith("#"):
            continue

        indent = len(raw_line) - len(raw_line.lstrip(" "))
        if indent == 0:
            in_merge_sources = stripped == "merge_sources:"
            continue
        if not in_merge_sources:
            continue

        if indent == 2 and stripped.startswith("- "):
            if current:
                sources.append(source_from_dict(current))
            current = {}
            list_key = None
            item = stripped[2:]
            if ":" in item:
                key, value = item.split(":", 1)
                current[key.strip()] = parse_scalar(value)
            continue

        if current is None:
            continue

        if indent == 4 and stripped.endswith(":"):
            list_key = stripped[:-1]
            current[list_key] = []
            continue

        if indent >= 6 and stripped.startswith("- ") and list_key:
            value = parse_scalar(stripped[2:])
            current.setdefault(list_key, [])
            assert isinstance(current[list_key], list)
            current[list_key].append(value)
            continue

        if indent == 4 and ":" in stripped:
            key, value = stripped.split(":", 1)
            list_key = None
            current[key.strip()] = parse_scalar(value)

    if current:
        sources.append(source_from_dict(current))
    return sources


def source_from_dict(values: dict[str, object]) -> MergeSource:
    def as_int(value: object, default: int) -> int:
        try:
            return int(str(value).strip())
        except (TypeError, ValueError):
            return default

    def as_list(value: object) -> list[str]:
        if isinstance(value, list):
            return [str(item) for item in value]
        return []

    return MergeSource(
        source_id=str(values.get("id", "unknown")),
        kind=str(values.get("kind", "unknown")),
        remote_name=str(values.get("git_remote_name", "")),
        remote_url_or_path=str(values.get("git_remote_url_or_local_path", "")),
        target_branch=str(values.get("target_branch", "")),
        source_branch=str(values.get("source_branch", "")),
        merge_order=as_int(values.get("merge_order"), 999),
        consolidation_mode=str(values.get("consolidation_mode", "history")),
        primary_areas=as_list(values.get("primary_areas")),
        notes=str(values.get("notes", "")),
    )


def current_branch() -> str:
    result = run_git(["branch", "--show-current"])
    return result.stdout.strip() or "HEAD"


def has_clean_worktree() -> bool:
    result = run_git(["status", "--porcelain"])
    return result.stdout.strip() == ""


def configured_remotes() -> dict[str, str]:
    result = run_git(["remote", "-v"])
    remotes: dict[str, str] = {}
    for line in result.stdout.splitlines():
        parts = line.split()
        if len(parts) >= 2 and parts[0] not in remotes:
            remotes[parts[0]] = parts[1]
    return remotes


def branch_refs() -> list[str]:
    result = run_git(["for-each-ref", "--format=%(refname:short)", "refs/heads", "refs/remotes"])
    refs: list[str] = []
    for ref in result.stdout.splitlines():
        if ref.endswith("/HEAD"):
            continue
        refs.append(ref)
    return sorted(set(refs))


def ref_exists(ref: str) -> bool:
    result = run_git(["rev-parse", "--verify", "--quiet", ref], check=False)
    return result.returncode == 0


def changed_files_for_ref(ref: str, base: str) -> list[str]:
    merge_base = run_git(["merge-base", base, ref], check=False)
    if merge_base.returncode != 0:
        return []
    result = run_git(["diff", "--name-only", merge_base.stdout.strip(), ref], check=False)
    if result.returncode != 0:
        return []
    return [line for line in result.stdout.splitlines() if line]


def classify_files(paths: Iterable[str]) -> dict[str, int]:
    counts = {area: 0 for area in AREA_HINTS}
    for path in paths:
        lower_path = path.lower()
        for area, suffixes in AREA_HINTS.items():
            if any(lower_path.endswith(suffix.lower()) for suffix in suffixes):
                counts[area] += 1
    return {area: count for area, count in counts.items() if count}


def resolve_manifest_refs(sources: Sequence[MergeSource]) -> list[dict[str, object]]:
    refs = set(branch_refs())
    remotes = configured_remotes()
    plan: list[dict[str, object]] = []
    base = current_branch()

    for source in sorted(sources, key=lambda item: item.priority):
        resolved = next((candidate for candidate in source.ref_candidates if candidate in refs or ref_exists(candidate)), "")
        reachable = bool(resolved)
        files = changed_files_for_ref(resolved, base) if reachable else []
        plan.append(
            {
                "id": source.source_id,
                "kind": source.kind,
                "remote": source.remote_name,
                "remoteConfigured": source.remote_name in remotes,
                "remoteUrlOrPath": source.remote_url_or_path,
                "sourceBranch": source.source_branch,
                "resolvedRef": resolved,
                "reachable": reachable,
                "mergeOrder": source.merge_order,
                "consolidationMode": source.consolidation_mode,
                "primaryAreas": source.primary_areas,
                "changedFileCount": len(files),
                "changedAreaCounts": classify_files(files),
                "notes": source.notes,
            }
        )
    return plan


def discover_extra_branches(manifest_plan: Sequence[dict[str, object]]) -> list[dict[str, object]]:
    planned_refs = {str(item.get("resolvedRef")) for item in manifest_plan if item.get("resolvedRef")}
    base = current_branch()
    extras: list[dict[str, object]] = []
    for ref in branch_refs():
        if ref == base or ref in planned_refs:
            continue
        files = changed_files_for_ref(ref, base)
        extras.append(
            {
                "id": ref,
                "kind": "discovered_branch",
                "remote": ref.split("/", 1)[0] if "/" in ref else "",
                "resolvedRef": ref,
                "reachable": True,
                "mergeOrder": 500,
                "consolidationMode": "history",
                "primaryAreas": [],
                "changedFileCount": len(files),
                "changedAreaCounts": classify_files(files),
                "notes": "Discovered from local refs; not listed in MERGE_SOURCE_MANIFEST.yaml.",
            }
        )
    return sorted(extras, key=lambda item: str(item["resolvedRef"]))


def write_report(payload: dict[str, object], output: Path | None) -> None:
    text = json.dumps(payload, indent=2, sort_keys=True)
    if output:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(text + "\n", encoding="utf-8")
        print(f"Wrote report: {output}")
    else:
        print(text)


def print_plan_summary(items: Sequence[dict[str, object]]) -> None:
    print("\nHELIOS consolidation plan")
    print("=" * 27)
    for index, item in enumerate(items, start=1):
        status = "reachable" if item.get("reachable") else "missing"
        ref = item.get("resolvedRef") or f"{item.get('remote')}/{item.get('sourceBranch', '')}".strip("/")
        areas = ", ".join(item.get("primaryAreas") or []) or "unclassified"
        print(f"{index:02d}. {item['id']} [{status}] -> {ref} | areas: {areas}")
        if item.get("changedAreaCounts"):
            print(f"    changed areas: {item['changedAreaCounts']}")
        if item.get("remoteConfigured") is False and item.get("remote"):
            print(f"    remote not configured: git remote add {item['remote']} {item.get('remoteUrlOrPath')}")


def audit_command(args: argparse.Namespace) -> int:
    require_git_repo()
    sources = parse_manifest(args.manifest)
    manifest_plan = resolve_manifest_refs(sources)
    extras = discover_extra_branches(manifest_plan)
    all_items = manifest_plan + extras
    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "repositoryRoot": str(REPO_ROOT),
        "currentBranch": current_branch(),
        "cleanWorktree": has_clean_worktree(),
        "configuredRemotes": configured_remotes(),
        "manifest": str(args.manifest),
        "plan": all_items,
    }
    if not args.json_only:
        print_plan_summary(all_items)
    write_report(payload, args.output)
    return 0


def fetch_command(args: argparse.Namespace) -> int:
    require_git_repo()
    remotes = configured_remotes()
    sources = parse_manifest(args.manifest)
    exit_code = 0
    for source in sorted(sources, key=lambda item: item.priority):
        if not source.remote_name:
            continue
        if source.remote_name not in remotes:
            if args.add_missing_remotes:
                print(f"Adding remote {source.remote_name}: {source.remote_url_or_path}", flush=True)
                result = run_git(["remote", "add", source.remote_name, source.remote_url_or_path], check=False)
                if result.returncode != 0:
                    print(result.stderr, file=sys.stderr)
                    exit_code = result.returncode
                    continue
            else:
                print(f"Missing remote {source.remote_name}; rerun with --add-missing-remotes to add it.", flush=True)
                exit_code = 2
                continue
        print(f"Fetching {source.remote_name}...", flush=True)
        result = run_git(["fetch", "--prune", source.remote_name], check=False)
        if result.returncode != 0:
            print(result.stderr, file=sys.stderr)
            exit_code = result.returncode
    return exit_code


def merge_command(args: argparse.Namespace) -> int:
    require_git_repo()
    if args.execute and not has_clean_worktree():
        print("Refusing to merge with a dirty worktree. Commit/stash unrelated changes first.", file=sys.stderr)
        return 2

    sources = parse_manifest(args.manifest)
    plan = [item for item in resolve_manifest_refs(sources) + discover_extra_branches([]) if item.get("reachable")]
    if args.only:
        wanted = set(args.only)
        plan = [item for item in plan if item["id"] in wanted or item.get("resolvedRef") in wanted]
    if not plan:
        print("No reachable branch refs to merge.")
        return 0

    print_plan_summary(plan)
    if not args.execute:
        print("\nDry run only. Add --execute to run guarded git merge commands.")
        return 0

    run_git(["config", "rerere.enabled", "true"], check=False)
    for item in plan:
        ref = str(item["resolvedRef"])
        print(f"\nMerging {item['id']} from {ref}...")
        result = run_git(["merge", "--no-ff", "--no-commit", ref], check=False)
        if result.returncode != 0:
            print(result.stdout)
            print(result.stderr, file=sys.stderr)
            print("Merge stopped for conflict resolution. Resolve conflicts, commit, then rerun for remaining refs.", file=sys.stderr)
            return result.returncode
        if args.commit_each:
            message = f"Merge {item['id']} into HELIOS consolidation"
            commit = run_git(["commit", "-m", message], check=False)
            if commit.returncode != 0:
                print(commit.stdout)
                print(commit.stderr, file=sys.stderr)
                return commit.returncode
            print(commit.stdout.strip())
        else:
            print(f"Staged merge for {item['id']}; commit after reviewing accumulated changes.")
    return 0


def azure_status() -> dict[str, object]:
    az_path = shutil.which("az")
    status: dict[str, object] = {
        "platform": platform.platform(),
        "azFound": bool(az_path),
        "azPath": az_path or "",
        "installHints": azure_install_hints(),
    }
    if az_path:
        version = subprocess.run([az_path, "version", "--output", "json"], text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
        status["versionReturnCode"] = version.returncode
        status["version"] = json.loads(version.stdout) if version.returncode == 0 and version.stdout.strip().startswith("{") else version.stdout.strip()
        account = subprocess.run([az_path, "account", "show", "--output", "json"], text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
        status["authenticated"] = account.returncode == 0
        status["account"] = json.loads(account.stdout) if account.returncode == 0 and account.stdout.strip().startswith("{") else {}
    return status


def azure_install_hints() -> list[str]:
    system = platform.system().lower()
    if system == "windows":
        return [
            "winget install --exact --id Microsoft.AzureCLI",
            "az login --tenant <tenant-id>",
            "az account set --subscription <subscription-id>",
        ]
    if system == "darwin":
        return ["brew update && brew install azure-cli", "az login", "az account set --subscription <subscription-id>"]
    return [
        "curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash",
        "az login --use-device-code",
        "az account set --subscription <subscription-id>",
    ]


def azure_command(args: argparse.Namespace) -> int:
    status = azure_status()
    write_report(status, args.output)
    if args.login and status["azFound"]:
        login_args = [str(status["azPath"]), "login"]
        if args.tenant:
            login_args.extend(["--tenant", args.tenant])
        if args.use_device_code:
            login_args.append("--use-device-code")
        return subprocess.call(login_args)
    if args.login:
        print("Azure CLI is not installed; use one of the installHints first.", file=sys.stderr)
        return 2
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="HELIOS consolidation and setup automation")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST, help="Path to MERGE_SOURCE_MANIFEST.yaml")
    subparsers = parser.add_subparsers(dest="command", required=True)

    audit = subparsers.add_parser("audit", help="Inspect manifest sources, branches, remotes, and changed file areas")
    audit.add_argument("--output", type=Path, help="Write JSON audit report to this path")
    audit.add_argument("--json-only", action="store_true", help="Suppress human summary")
    audit.set_defaults(func=audit_command)

    fetch = subparsers.add_parser("fetch", help="Fetch manifest remotes in merge priority order")
    fetch.add_argument("--add-missing-remotes", action="store_true", help="Add remotes from the manifest before fetching")
    fetch.set_defaults(func=fetch_command)

    merge = subparsers.add_parser("merge", help="Plan or execute guarded merges")
    merge.add_argument("--execute", action="store_true", help="Actually run git merge; default is dry-run")
    merge.add_argument("--commit-each", action="store_true", help="Commit after every successful merge")
    merge.add_argument("--only", nargs="*", help="Limit merge to source ids or refs")
    merge.set_defaults(func=merge_command)

    azure = subparsers.add_parser("azure-check", help="Check Azure CLI installation/authentication and print setup hints")
    azure.add_argument("--output", type=Path, help="Write JSON Azure CLI status report to this path")
    azure.add_argument("--login", action="store_true", help="Run az login if Azure CLI is installed")
    azure.add_argument("--tenant", help="Tenant id for az login")
    azure.add_argument("--use-device-code", action="store_true", help="Use device-code auth for headless shells")
    azure.set_defaults(func=azure_command)
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return int(args.func(args))
    except FileNotFoundError as exc:
        print(f"Missing required file: {exc}", file=sys.stderr)
        return 2
    except subprocess.CalledProcessError as exc:
        print(exc.stdout)
        print(exc.stderr, file=sys.stderr)
        return exc.returncode


if __name__ == "__main__":
    raise SystemExit(main())
