from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

USE_RE = re.compile(
    r"(?P<prefix>\buses:\s*)(?P<repo>[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)@"
    r"(?P<ref>[^\s#]+)(?P<suffix>\s*(?:#.*)?)$",
    re.MULTILINE,
)
SHA_RE = re.compile(r"^[0-9a-fA-F]{40}$")


@dataclass(frozen=True)
class ActionReference:
    workflow: str
    repository: str
    current_ref: str
    line: int


@dataclass(frozen=True)
class ActionPin:
    repository: str
    source_ref: str
    commit_sha: str


def scan_workflows(root: Path) -> list[ActionReference]:
    result: list[ActionReference] = []
    workflow_root = root / ".github" / "workflows"
    for path in sorted([*workflow_root.glob("*.yml"), *workflow_root.glob("*.yaml")]):
        text = path.read_text(encoding="utf-8")
        for match in USE_RE.finditer(text):
            line = text.count("\n", 0, match.start()) + 1
            result.append(
                ActionReference(
                    workflow=str(path.relative_to(root)).replace("\\", "/"),
                    repository=match.group("repo"),
                    current_ref=match.group("ref"),
                    line=line,
                )
            )
    return result


def _api_json(url: str, token: str | None) -> dict:
    headers = {
        "Accept": "application/vnd.github+json",
        "User-Agent": "helios-action-pin-resolver/2.0",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")[:500]
        raise RuntimeError(f"GitHub API request failed ({exc.code}) for {url}: {body}") from exc
    except urllib.error.URLError as exc:
        raise RuntimeError(f"GitHub API request failed for {url}: {exc.reason}") from exc


def resolve_tag(repository: str, ref: str, token: str | None) -> str:
    if SHA_RE.fullmatch(ref):
        return ref.lower()
    quoted = urllib.parse.quote(ref, safe="")
    base = f"https://api.github.com/repos/{repository}"
    payload = _api_json(f"{base}/git/ref/tags/{quoted}", token)
    obj = payload.get("object", {})
    obj_type = obj.get("type")
    sha = str(obj.get("sha", ""))
    depth = 0
    while obj_type == "tag":
        depth += 1
        if depth > 5 or not SHA_RE.fullmatch(sha):
            raise RuntimeError(f"Could not safely dereference tag {repository}@{ref}")
        payload = _api_json(f"{base}/git/tags/{sha}", token)
        obj = payload.get("object", {})
        obj_type = obj.get("type")
        sha = str(obj.get("sha", ""))
    if obj_type != "commit" or not SHA_RE.fullmatch(sha):
        raise RuntimeError(f"{repository}@{ref} did not resolve to a 40-character commit SHA")
    return sha.lower()


def _load_policy(root: Path) -> dict:
    return json.loads((root / "config/github/action-policy.json").read_text(encoding="utf-8"))


def build_plan(root: Path, *, token: str | None, resolve: bool = True) -> dict:
    policy = _load_policy(root)
    approved = {key.lower(): value for key, value in policy["approvedMajorTags"].items()}
    forbidden = {value.lower() for value in policy["forbiddenRefs"]}
    references = scan_workflows(root)
    errors: list[str] = []
    unique: dict[tuple[str, str], ActionPin] = {}
    for item in references:
        repo_key = item.repository.lower()
        if item.current_ref.lower() in forbidden:
            errors.append(f"{item.workflow}:{item.line}: forbidden action ref {item.repository}@{item.current_ref}")
            continue
        if SHA_RE.fullmatch(item.current_ref):
            unique[(repo_key, item.current_ref)] = ActionPin(item.repository, item.current_ref, item.current_ref.lower())
            continue
        expected = approved.get(repo_key)
        if expected is None:
            errors.append(f"{item.workflow}:{item.line}: action {item.repository} is not approved")
            continue
        if item.current_ref != expected:
            errors.append(
                f"{item.workflow}:{item.line}: {item.repository}@{item.current_ref} is not approved; expected {expected}"
            )
            continue
        key = (repo_key, item.current_ref)
        if key not in unique:
            sha = resolve_tag(item.repository, item.current_ref, token) if resolve else "UNRESOLVED"
            unique[key] = ActionPin(item.repository, item.current_ref, sha)
    pins = sorted((asdict(value) for value in unique.values()), key=lambda item: item["repository"].lower())
    return {
        "version": "1.0",
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "mode": "resolved" if resolve else "offline-inventory",
        "workflowCount": len({item.workflow for item in references}),
        "referenceCount": len(references),
        "uniqueActionCount": len(pins),
        "errors": errors,
        "pins": pins,
    }


def apply_pins(root: Path, plan: dict) -> int:
    pins = {
        (item["repository"].lower(), item["source_ref"]): item["commit_sha"]
        for item in plan["pins"]
        if SHA_RE.fullmatch(item["commit_sha"])
    }
    changed = 0
    workflow_root = root / ".github" / "workflows"
    for path in sorted([*workflow_root.glob("*.yml"), *workflow_root.glob("*.yaml")]):
        text = path.read_text(encoding="utf-8")

        def replace(match: re.Match[str]) -> str:
            nonlocal changed
            repo = match.group("repo")
            source_ref = match.group("ref")
            sha = pins.get((repo.lower(), source_ref))
            if not sha or SHA_RE.fullmatch(source_ref):
                return match.group(0)
            changed += 1
            suffix = match.group("suffix")
            comment = suffix if suffix.strip().startswith("#") else f" # {source_ref}"
            return f"{match.group('prefix')}{repo}@{sha}{comment}"

        updated = USE_RE.sub(replace, text)
        if updated != text:
            path.write_text(updated, encoding="utf-8")
    return changed


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Resolve approved GitHub Action tags to reviewed full commit SHAs.")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--offline", action="store_true", help="Inventory references without contacting GitHub.")
    parser.add_argument("--apply", action="store_true", help="Rewrite workflows and write the lock file.")
    parser.add_argument("--confirm", default="")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args(list(argv) if argv is not None else None)

    root = args.root.resolve()
    if args.apply and args.confirm != "PIN HELIOS GITHUB ACTIONS":
        parser.error("--apply requires --confirm 'PIN HELIOS GITHUB ACTIONS'")
    token = os.getenv("GITHUB_TOKEN") or os.getenv("GH_TOKEN")
    plan = build_plan(root, token=token, resolve=not args.offline)
    if plan["errors"]:
        print(json.dumps(plan, indent=2), file=sys.stderr)
        return 2
    if args.apply:
        plan["rewrittenReferences"] = apply_pins(root, plan)
        lock_path = root / "config/github/action-pins.lock.json"
        lock_path.write_text(json.dumps(plan, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        plan["lockFile"] = str(lock_path.relative_to(root)).replace("\\", "/")
    output = json.dumps(plan, indent=2, sort_keys=True) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output, encoding="utf-8")
    print(output, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
