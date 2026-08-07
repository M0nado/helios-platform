#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUT_DIR = ROOT / "reports" / "branch-intelligence"
JSON_NAME = "codex-pr-merge-trains.json"
MD_NAME = "codex-pr-merge-trains.md"

TRAIN_ORDER = ["contracts-policy", "app-runtime", "infra-workflow"]
TRAIN_PRIORITY = {name: index for index, name in enumerate(TRAIN_ORDER)}

DOMAIN_RULES = {
    "contracts-policy": [
        "config/integrations/",
        "config/policy",
        "config/agent",
        "event-contract.schema.json",
        "docs/governance/",
        "agents.md",
        ".github/copilot-instructions.md",
        "monado/helios-control/config/",
    ],
    "app-runtime": [
        "src/",
        "tests/",
        ".csproj",
        ".fsproj",
        ".sln",
        ".slnx",
        "monado/helios-control/src/",
    ],
    "infra-workflow": [
        ".github/workflows/",
        "infra/",
        "scripts/",
        "azure-pipelines.yml",
        "monado/helios-control/infra/",
        "plugins/",
    ],
}

CHECK_SUCCESS = {"SUCCESS", "NEUTRAL", "SKIPPED"}
CHECK_FAILURE = {"FAILURE", "TIMED_OUT", "CANCELLED", "ACTION_REQUIRED", "STARTUP_FAILURE", "STALE"}
STATUS_SUCCESS = {"SUCCESS"}
STATUS_FAILURE = {"FAILURE", "ERROR"}


def run(cmd: list[str], timeout: int = 60) -> str:
    proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True, timeout=timeout)
    if proc.returncode != 0:
        detail = (proc.stderr or proc.stdout).strip()
        raise RuntimeError(f"Command failed ({proc.returncode}): {' '.join(cmd)}\n{detail}")
    return proc.stdout.strip()


def gh_json(args: list[str], timeout: int = 60) -> Any:
    output = run(["gh", *args], timeout=timeout)
    if not output:
        return None
    return json.loads(output)


def parse_iso(value: str | None) -> datetime:
    if not value:
        return datetime.fromtimestamp(0, tz=timezone.utc)
    try:
        if value.endswith("Z"):
            return datetime.fromisoformat(value.replace("Z", "+00:00"))
        return datetime.fromisoformat(value)
    except ValueError:
        return datetime.fromtimestamp(0, tz=timezone.utc)


def slug(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")


def parse_number_list(raw: str) -> list[int]:
    if not raw.strip():
        return []
    out: list[int] = []
    for token in raw.split(","):
        text = token.strip()
        if not text:
            continue
        if not text.isdigit():
            raise ValueError(f"Invalid PR number: {text}")
        out.append(int(text))
    return sorted(set(out))


def extract_issue_pr_numbers(body: str, issue_number: int) -> list[int]:
    seen = set()
    for match in re.findall(r"#(\d+)", body or ""):
        number = int(match)
        if number != issue_number:
            seen.add(number)
    return sorted(seen)


def discover_repo(repo: str) -> tuple[str, str]:
    if repo:
        info = gh_json(["repo", "view", repo, "--json", "nameWithOwner,defaultBranchRef"])
    else:
        info = gh_json(["repo", "view", "--json", "nameWithOwner,defaultBranchRef"])
    name = info.get("nameWithOwner")
    default_branch = info.get("defaultBranchRef", {}).get("name") or "main"
    if not name:
        raise RuntimeError("Unable to resolve repository from gh.")
    return str(name), str(default_branch)


def load_issue_context(repo: str, issue_number: int | None) -> dict[str, Any]:
    if issue_number is None:
        return {"number": None, "title": "", "url": "", "candidateNumbers": []}
    issue = gh_json(["issue", "view", str(issue_number), "--repo", repo, "--json", "number,title,url,body"])
    candidate_numbers = extract_issue_pr_numbers(issue.get("body", ""), issue_number)
    return {
        "number": issue.get("number"),
        "title": issue.get("title", ""),
        "url": issue.get("url", ""),
        "candidateNumbers": candidate_numbers,
    }


def load_open_pr_summaries(repo: str, limit: int) -> list[dict[str, Any]]:
    fields = ",".join(
        [
            "number",
            "title",
            "headRefName",
            "baseRefName",
            "isDraft",
            "mergeStateStatus",
            "mergeable",
            "author",
            "createdAt",
            "updatedAt",
            "changedFiles",
            "additions",
            "deletions",
            "url",
        ]
    )
    rows = gh_json(["pr", "list", "--repo", repo, "--state", "open", "--limit", str(limit), "--json", fields]) or []
    return [row for row in rows if isinstance(row, dict)]


def include_pr(summary: dict[str, Any], issue_numbers: set[int], head_prefixes: tuple[str, ...], explicit_numbers: set[int]) -> bool:
    number = int(summary.get("number", 0))
    head = str(summary.get("headRefName", "")).lower()
    author = str((summary.get("author") or {}).get("login", "")).lower()
    title = str(summary.get("title", "")).lower()
    if number in explicit_numbers:
        return True
    if number in issue_numbers:
        return True
    if any(head.startswith(prefix) for prefix in head_prefixes):
        return True
    if "copilot" in author or "codex" in author:
        return True
    if "codex" in title or "copilot" in title:
        return True
    return False


def load_pr_detail(repo: str, number: int) -> dict[str, Any]:
    fields = ",".join(
        [
            "number",
            "title",
            "headRefName",
            "baseRefName",
            "isDraft",
            "mergeStateStatus",
            "mergeable",
            "reviewDecision",
            "statusCheckRollup",
            "changedFiles",
            "additions",
            "deletions",
            "files",
            "commits",
            "author",
            "createdAt",
            "updatedAt",
            "url",
        ]
    )
    detail = gh_json(["pr", "view", str(number), "--repo", repo, "--json", fields], timeout=90) or {}
    if not isinstance(detail, dict):
        raise RuntimeError(f"Unexpected PR detail payload for #{number}")
    return detail


def ownership_surfaces(paths: list[str]) -> list[str]:
    roots = sorted({path.split("/", 1)[0] if "/" in path else path for path in paths})
    return roots


def domain_scores(paths: list[str]) -> dict[str, int]:
    scores: Counter[str] = Counter()
    for raw_path in paths:
        path = raw_path.lower()
        for domain, needles in DOMAIN_RULES.items():
            if any(needle in path for needle in needles):
                scores[domain] += 1
    return dict(sorted(scores.items()))


def primary_domain(paths: list[str], scores: dict[str, int]) -> str:
    if not paths:
        return "contracts-policy"
    if not scores:
        if any(path.startswith("src/") or path.startswith("tests/") for path in paths):
            return "app-runtime"
        if any(path.startswith("config/") or path.startswith("docs/") for path in paths):
            return "contracts-policy"
        return "infra-workflow"
    ranked = sorted(scores.items(), key=lambda item: (-item[1], TRAIN_PRIORITY.get(item[0], 99), item[0]))
    return ranked[0][0]


def check_display_name(entry: dict[str, Any]) -> str:
    typename = entry.get("__typename")
    if typename == "CheckRun":
        wf = str(entry.get("workflowName", "")).strip()
        name = str(entry.get("name", "")).strip() or "unnamed-checkrun"
        return f"{wf} / {name}" if wf else name
    return str(entry.get("context", "status-context")).strip() or "status-context"


def summarize_checks(entries: list[dict[str, Any]]) -> dict[str, Any]:
    passed: list[str] = []
    failed: list[str] = []
    pending: list[str] = []
    required_names: set[str] = set()

    for entry in entries:
        name = check_display_name(entry)
        required_names.add(name)
        typename = entry.get("__typename")
        if typename == "CheckRun":
            status = str(entry.get("status", "")).upper()
            conclusion = str(entry.get("conclusion", "")).upper()
            if status != "COMPLETED":
                pending.append(name)
            elif conclusion in CHECK_SUCCESS:
                passed.append(name)
            elif conclusion in CHECK_FAILURE:
                failed.append(name)
            else:
                pending.append(name)
            continue
        state = str(entry.get("state", "")).upper()
        if state in STATUS_SUCCESS:
            passed.append(name)
        elif state in STATUS_FAILURE:
            failed.append(name)
        else:
            pending.append(name)

    if failed:
        state = "failing"
    elif pending:
        state = "pending"
    elif passed:
        state = "passed"
    else:
        state = "not-configured"

    return {
        "state": state,
        "total": len(required_names),
        "passedCount": len(set(passed)),
        "failedCount": len(set(failed)),
        "pendingCount": len(set(pending)),
        "required": sorted(required_names),
        "failing": sorted(set(failed)),
        "pending": sorted(set(pending)),
    }


def gate_status(pr: dict[str, Any]) -> dict[str, Any]:
    checks = pr["checks"]
    mergeable = str(pr.get("mergeable", ""))
    merge_state = str(pr.get("mergeStateStatus", ""))
    review_decision = str(pr.get("reviewDecision", "") or "")
    checks_satisfied = checks["state"] == "passed"
    approvals_satisfied = review_decision == "APPROVED"
    mergeability_satisfied = mergeable == "MERGEABLE" and merge_state not in {"DIRTY", "UNKNOWN"}
    return {
        "checksSatisfied": checks_satisfied,
        "approvalsSatisfied": approvals_satisfied,
        "mergeabilitySatisfied": mergeability_satisfied,
        "mergeReady": checks_satisfied and approvals_satisfied and mergeability_satisfied,
    }


def build_overlap(prs: list[dict[str, Any]]) -> tuple[list[dict[str, Any]], dict[int, list[dict[str, Any]]]]:
    rows: list[dict[str, Any]] = []
    by_pr: dict[int, list[dict[str, Any]]] = defaultdict(list)
    for index, left in enumerate(prs):
        left_paths = set(left["filePaths"])
        for right in prs[index + 1 :]:
            right_paths = set(right["filePaths"])
            inter = sorted(left_paths & right_paths)
            if not inter:
                continue
            union_count = len(left_paths | right_paths) or 1
            jaccard = round(len(inter) / union_count, 4)
            pair = {
                "left": left["number"],
                "right": right["number"],
                "overlapCount": len(inter),
                "jaccard": jaccard,
                "sharedSurfaces": ownership_surfaces(inter),
                "samplePaths": inter[:12],
            }
            rows.append(pair)
            by_pr[left["number"]].append({"pr": right["number"], "overlapCount": len(inter), "jaccard": jaccard})
            by_pr[right["number"]].append({"pr": left["number"], "overlapCount": len(inter), "jaccard": jaccard})
    rows.sort(key=lambda row: (-row["overlapCount"], -row["jaccard"], row["left"], row["right"]))
    for pr_number, overlaps in by_pr.items():
        overlaps.sort(key=lambda row: (-row["overlapCount"], -row["jaccard"], row["pr"]))
        by_pr[pr_number] = overlaps
    return rows, by_pr


def detect_superseded(prs: list[dict[str, Any]], overlap_rows: list[dict[str, Any]]) -> dict[int, dict[str, Any]]:
    superseded: dict[int, dict[str, Any]] = {}
    indexed = {pr["number"]: pr for pr in prs}

    for pr in prs:
        if int(pr.get("changedFiles") or 0) == 0:
            superseded[pr["number"]] = {
                "by": None,
                "reason": "No effective diff against the base branch.",
            }

    for pair in overlap_rows:
        if pair["jaccard"] < 0.85 or pair["overlapCount"] < 3:
            continue
        left = indexed[pair["left"]]
        right = indexed[pair["right"]]
        if left["train"] != right["train"]:
            continue
        left_time = parse_iso(left.get("updatedAt"))
        right_time = parse_iso(right.get("updatedAt"))
        if left_time <= right_time:
            older, newer = left, right
        else:
            older, newer = right, left
        if older["number"] in superseded:
            continue
        superseded[older["number"]] = {
            "by": newer["number"],
            "reason": f"High overlap with PR #{newer['number']} (jaccard {pair['jaccard']}).",
        }
    return superseded


def disposition_for(pr: dict[str, Any], superseded: dict[int, dict[str, Any]]) -> tuple[str, str]:
    number = pr["number"]
    if number in superseded:
        marker = superseded[number]
        if marker["by"] is None:
            return "supersede", marker["reason"]
        return "supersede", f"{marker['reason']} Prefer PR #{marker['by']}."

    mergeable = str(pr.get("mergeable", ""))
    merge_state = str(pr.get("mergeStateStatus", ""))
    title = str(pr.get("title", "")).upper()
    if pr.get("isDraft") or "[WIP]" in title:
        return "defer", "Draft/WIP PR should not enter a merge-ready train yet."
    if mergeable == "CONFLICTING" or merge_state == "DIRTY":
        return "defer", "Conflicting branch state requires manual rebase/conflict resolution."
    return "merge-train", "Candidate for canonical train integration."


def topo_order(items: list[dict[str, Any]], dependency_map: dict[int, list[int]]) -> list[int]:
    numbers = sorted(pr["number"] for pr in items)
    members = set(numbers)
    incoming = {number: 0 for number in numbers}
    outgoing: dict[int, set[int]] = {number: set() for number in numbers}

    for child in numbers:
        deps = [dep for dep in dependency_map.get(child, []) if dep in members]
        for dep in deps:
            if child not in outgoing[dep]:
                outgoing[dep].add(child)
                incoming[child] += 1

    queue = sorted([number for number, count in incoming.items() if count == 0])
    ordered: list[int] = []
    while queue:
        current = queue.pop(0)
        ordered.append(current)
        for target in sorted(outgoing[current]):
            incoming[target] -= 1
            if incoming[target] == 0:
                queue.append(target)
                queue.sort()

    if len(ordered) != len(numbers):
        missing = sorted(set(numbers) - set(ordered))
        ordered.extend(missing)
    return ordered


def build_trains(
    repo: str,
    issue_number: int | None,
    default_branch: str,
    prs: list[dict[str, Any]],
    dependency_map: dict[int, list[int]],
) -> list[dict[str, Any]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for pr in prs:
        grouped[pr["train"]].append(pr)

    trains: list[dict[str, Any]] = []
    for train_name in TRAIN_ORDER:
        items = sorted(grouped.get(train_name, []), key=lambda row: row["number"])
        if not items:
            continue
        ordered_numbers = topo_order(items, dependency_map)
        by_number = {pr["number"]: pr for pr in items}
        ordered = [by_number[number] for number in ordered_numbers if number in by_number]
        suffix = f"issue-{issue_number}-{train_name}" if issue_number is not None else f"{train_name}-{datetime.now().date().isoformat()}"
        branch = f"train/{slug(suffix)}"
        required_checks = sorted({name for pr in ordered for name in pr["checks"]["required"]})
        merge_candidates = [pr["number"] for pr in ordered if pr["disposition"] == "merge-train"]
        deferred = [pr["number"] for pr in ordered if pr["disposition"] == "defer"]
        superseded = [pr["number"] for pr in ordered if pr["disposition"] == "supersede"]
        external_deps = sorted(
            {
                dep
                for pr in ordered
                for dep in dependency_map.get(pr["number"], [])
                if dep not in {member["number"] for member in ordered}
            }
        )
        commands = [
            f"git checkout {default_branch}",
            "git pull --ff-only",
            f"git checkout -b {branch}",
        ]
        for pr in ordered:
            if pr["disposition"] != "merge-train":
                continue
            commands.append(f"# PR #{pr['number']} {pr['title']}")
            commands.append(f"git fetch origin pull/{pr['number']}/head:pr-{pr['number']}")
            if pr["commitOids"]:
                commands.append("git cherry-pick " + " ".join(pr["commitOids"]))
            else:
                commands.append(f"# no commit oids returned for PR #{pr['number']}")
        commands.append(
            f"gh pr create --repo {repo} --base {default_branch} --head {branch} "
            f"--title \"Issue #{issue_number or 'N/A'} train: {train_name}\" --draft"
        )
        trains.append(
            {
                "name": train_name,
                "canonicalBranch": branch,
                "targetPullRequestTitle": f"Issue #{issue_number or 'N/A'} train: {train_name}",
                "mergeOrder": [pr["number"] for pr in ordered],
                "mergeCandidates": merge_candidates,
                "deferred": deferred,
                "superseded": superseded,
                "externalDependencies": external_deps,
                "requiredChecks": required_checks,
                "commands": commands,
            }
        )
    return trains


def ownership_conflicts(prs: list[dict[str, Any]], overlap_rows: list[dict[str, Any]]) -> list[dict[str, Any]]:
    indexed = {pr["number"]: pr for pr in prs}
    conflicts: list[dict[str, Any]] = []
    for row in overlap_rows:
        left = indexed[row["left"]]
        right = indexed[row["right"]]
        if left["train"] == right["train"]:
            continue
        if left["disposition"] == "supersede" or right["disposition"] == "supersede":
            continue
        conflicts.append(
            {
                "left": row["left"],
                "right": row["right"],
                "leftTrain": left["train"],
                "rightTrain": right["train"],
                "overlapCount": row["overlapCount"],
                "sharedSurfaces": row["sharedSurfaces"],
                "samplePaths": row["samplePaths"][:8],
            }
        )
    conflicts.sort(key=lambda row: (-row["overlapCount"], row["left"], row["right"]))
    return conflicts


def markdown_table(headers: list[str], rows: list[list[Any]]) -> str:
    output = ["| " + " | ".join(headers) + " |", "| " + " | ".join(["---"] * len(headers)) + " |"]
    for row in rows:
        rendered = [str(cell).replace("|", "\\|").replace("\n", " ") for cell in row]
        output.append("| " + " | ".join(rendered) + " |")
    return "\n".join(output)


def render_markdown(payload: dict[str, Any]) -> str:
    lines: list[str] = [
        "# Codex PR Merge Trains",
        "",
        f"Generated: `{payload['generatedUtc']}`",
        "",
        payload["safeMode"],
        "",
        f"Repository: `{payload['repository']}`",
    ]
    issue = payload.get("issue", {})
    if issue.get("number") is not None:
        lines.append(f"Issue: [#{issue['number']}]({issue.get('url')}) - {issue.get('title', '')}")
    lines.extend(
        [
            "",
            "## Triage coverage",
            "",
            markdown_table(
                ["Total candidates", "Merge-train", "Supersede", "Defer", "Untriaged"],
                [
                    [
                        payload["triage"]["totalCandidates"],
                        payload["triage"]["mergeTrainCount"],
                        payload["triage"]["supersedeCount"],
                        payload["triage"]["deferCount"],
                        payload["triage"]["untriagedCount"],
                    ]
                ],
            ),
            "",
        ]
    )

    pr_rows: list[list[Any]] = []
    for pr in payload["pullRequests"]:
        deps = ", ".join(f"#{dep}" for dep in pr["dependencies"]) if pr["dependencies"] else ""
        checks = f"{pr['checks']['state']} ({pr['checks']['failedCount']} fail, {pr['checks']['pendingCount']} pending)"
        pr_rows.append(
            [
                f"[#{pr['number']}]({pr['url']})",
                pr["train"],
                pr["disposition"],
                pr["mergeStateStatus"],
                pr["reviewDecision"] or "none",
                checks,
                pr["changedFiles"],
                deps,
                pr["dispositionRationale"],
            ]
        )
    lines.extend(
        [
            "## PR disposition matrix",
            "",
            markdown_table(
                ["PR", "Train", "Disposition", "Merge state", "Review", "Checks", "Files", "Dependencies", "Rationale"],
                pr_rows,
            ),
            "",
        ]
    )

    overlap_rows = [
        [f"#{row['left']}", f"#{row['right']}", row["overlapCount"], row["jaccard"], ", ".join(row["sharedSurfaces"][:6])]
        for row in payload["overlapMatrix"][:30]
    ]
    if overlap_rows:
        lines.extend(
            [
                "## Top overlap pairs",
                "",
                markdown_table(["PR A", "PR B", "Shared files", "Jaccard", "Shared surfaces"], overlap_rows),
                "",
            ]
        )

    lines.extend(["## Train plans", ""])
    for train in payload["trains"]:
        lines.extend(
            [
                f"### {train['name']}",
                "",
                f"- Canonical branch: `{train['canonicalBranch']}`",
                f"- Merge order: {', '.join('#' + str(number) for number in train['mergeOrder']) if train['mergeOrder'] else 'none'}",
                f"- Merge candidates: {', '.join('#' + str(number) for number in train['mergeCandidates']) if train['mergeCandidates'] else 'none'}",
                f"- Deferred: {', '.join('#' + str(number) for number in train['deferred']) if train['deferred'] else 'none'}",
                f"- Superseded: {', '.join('#' + str(number) for number in train['superseded']) if train['superseded'] else 'none'}",
                f"- Required checks: {', '.join(train['requiredChecks']) if train['requiredChecks'] else 'none'}",
            ]
        )
        if train["externalDependencies"]:
            lines.append(f"- External dependencies: {', '.join('#' + str(dep) for dep in train['externalDependencies'])}")
        lines.extend(["", "Suggested commands:", "", "```bash"])
        lines.extend(train["commands"])
        lines.extend(["```", ""])

    conflicts = payload.get("ownershipConflicts", [])
    if conflicts:
        lines.extend(
            [
                "## Cross-train ownership conflicts",
                "",
                markdown_table(
                    ["PR A", "PR B", "Train A", "Train B", "Shared files", "Shared surfaces"],
                    [
                        [
                            f"#{row['left']}",
                            f"#{row['right']}",
                            row["leftTrain"],
                            row["rightTrain"],
                            row["overlapCount"],
                            ", ".join(row["sharedSurfaces"]),
                        ]
                        for row in conflicts[:30]
                    ],
                ),
                "",
            ]
        )
    else:
        lines.extend(["## Cross-train ownership conflicts", "", "- No cross-train overlap conflicts were detected among non-superseded PRs.", ""])

    lines.extend(
        [
            "## Acceptance checks",
            "",
            f"- Every candidate PR triaged: **{payload['acceptance']['allTriaged']}**",
            f"- Ownership conflicts remaining: **{payload['acceptance']['ownershipConflicts']}**",
            f"- Merge-ready candidates (all gates satisfied): **{payload['acceptance']['mergeReadyCandidates']}**",
            "",
        ]
    )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Build overlap matrix and merge-train recommendations for open Codex/Copilot PR streams."
    )
    parser.add_argument("--repo", default="", help="owner/repo, defaults to current gh repo")
    parser.add_argument("--issue", type=int, default=None, help="optional issue number used for candidate extraction and naming")
    parser.add_argument("--pr-numbers", default="", help="optional comma-separated PR numbers to force include")
    parser.add_argument("--limit", type=int, default=200, help="max open PRs to inspect")
    parser.add_argument(
        "--head-prefixes",
        default="codex/,copilot/,agent/",
        help="comma-separated head branch prefixes for automatic candidate selection",
    )
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT_DIR, help="output directory for generated reports")
    parser.add_argument("--base-branch", default="", help="override default branch for train command generation")
    args = parser.parse_args()

    if shutil.which("gh") is None:
        raise SystemExit("gh not found. Install GitHub CLI and authenticate before running this script.")

    repo, discovered_default = discover_repo(args.repo)
    default_branch = args.base_branch.strip() or discovered_default
    issue = load_issue_context(repo, args.issue)
    issue_numbers = set(int(number) for number in issue.get("candidateNumbers", []))
    explicit_numbers = set(parse_number_list(args.pr_numbers))
    head_prefixes = tuple(prefix.strip().lower() for prefix in args.head_prefixes.split(",") if prefix.strip())

    summaries = load_open_pr_summaries(repo, args.limit)
    candidates = [
        summary
        for summary in summaries
        if include_pr(summary, issue_numbers=issue_numbers, head_prefixes=head_prefixes, explicit_numbers=explicit_numbers)
    ]
    candidates.sort(key=lambda row: int(row.get("number", 0)))

    details: list[dict[str, Any]] = []
    for row in candidates:
        details.append(load_pr_detail(repo, int(row["number"])))

    prepared: list[dict[str, Any]] = []
    for detail in details:
        files = sorted({entry.get("path", "") for entry in detail.get("files", []) if entry.get("path")})
        scores = domain_scores(files)
        domain = primary_domain(files, scores)
        checks = summarize_checks(detail.get("statusCheckRollup", []))
        commits = detail.get("commits", []) or []
        prepared.append(
            {
                "number": int(detail.get("number")),
                "title": detail.get("title", ""),
                "url": detail.get("url", ""),
                "headRefName": detail.get("headRefName", ""),
                "baseRefName": detail.get("baseRefName", ""),
                "isDraft": bool(detail.get("isDraft")),
                "mergeable": detail.get("mergeable", ""),
                "mergeStateStatus": detail.get("mergeStateStatus", ""),
                "reviewDecision": detail.get("reviewDecision", "") or "",
                "changedFiles": int(detail.get("changedFiles") or 0),
                "additions": int(detail.get("additions") or 0),
                "deletions": int(detail.get("deletions") or 0),
                "author": (detail.get("author") or {}).get("login", ""),
                "createdAt": detail.get("createdAt", ""),
                "updatedAt": detail.get("updatedAt", ""),
                "filePaths": files,
                "ownershipSurfaces": ownership_surfaces(files),
                "domainScores": scores,
                "primaryDomain": domain,
                "train": domain,
                "checks": checks,
                "commitOids": [commit.get("oid", "") for commit in commits if commit.get("oid")],
                "commitHeadlines": [commit.get("messageHeadline", "") for commit in commits if commit.get("messageHeadline")][:12],
            }
        )

    head_to_number = {pr["headRefName"]: pr["number"] for pr in prepared if pr.get("headRefName")}
    dependency_map: dict[int, list[int]] = {}
    for pr in prepared:
        deps: list[int] = []
        base_ref = pr.get("baseRefName", "")
        if base_ref in head_to_number:
            deps.append(head_to_number[base_ref])
        dependency_map[pr["number"]] = sorted(set(deps))
        pr["dependencies"] = dependency_map[pr["number"]]

    overlap_rows, overlaps_by_pr = build_overlap(prepared)
    superseded = detect_superseded(prepared, overlap_rows)

    for pr in prepared:
        pr["overlaps"] = overlaps_by_pr.get(pr["number"], [])[:12]
        disposition, rationale = disposition_for(pr, superseded)
        gates = gate_status(pr)
        blockers: list[str] = []
        if not gates["checksSatisfied"]:
            blockers.append("checks")
        if not gates["approvalsSatisfied"]:
            blockers.append("approvals")
        if not gates["mergeabilitySatisfied"]:
            blockers.append("mergeability")
        if blockers and disposition == "merge-train":
            rationale = f"{rationale} Blocking gates: {', '.join(blockers)}."
        pr["gateStatus"] = gates
        pr["disposition"] = disposition
        pr["dispositionRationale"] = rationale

    trains = build_trains(repo, args.issue, default_branch, prepared, dependency_map)
    conflicts = ownership_conflicts(prepared, overlap_rows)

    triage = {
        "totalCandidates": len(prepared),
        "mergeTrainCount": sum(1 for pr in prepared if pr["disposition"] == "merge-train"),
        "supersedeCount": sum(1 for pr in prepared if pr["disposition"] == "supersede"),
        "deferCount": sum(1 for pr in prepared if pr["disposition"] == "defer"),
    }
    triage["untriagedCount"] = triage["totalCandidates"] - (
        triage["mergeTrainCount"] + triage["supersedeCount"] + triage["deferCount"]
    )

    acceptance = {
        "allTriaged": triage["untriagedCount"] == 0,
        "ownershipConflicts": len(conflicts),
        "mergeReadyCandidates": sum(1 for pr in prepared if pr["disposition"] == "merge-train" and pr["gateStatus"]["mergeReady"]),
    }

    payload = {
        "schemaVersion": "1.0",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "safeMode": "Report-only: no PR is merged, closed, rebased, deleted, or force-updated by this script.",
        "repository": repo,
        "defaultBranch": default_branch,
        "issue": issue,
        "candidateSelection": {
            "headPrefixes": list(head_prefixes),
            "explicitPrNumbers": sorted(explicit_numbers),
            "issueCandidateNumbers": sorted(issue_numbers),
        },
        "triage": triage,
        "acceptance": acceptance,
        "pullRequests": sorted(prepared, key=lambda pr: pr["number"]),
        "overlapMatrix": overlap_rows,
        "trains": trains,
        "ownershipConflicts": conflicts,
    }

    out_dir = args.out
    out_dir.mkdir(parents=True, exist_ok=True)
    json_path = out_dir / JSON_NAME
    md_path = out_dir / MD_NAME
    json_path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    md_path.write_text(render_markdown(payload), encoding="utf-8")
    print(f"Wrote {json_path.relative_to(ROOT)}")
    print(f"Wrote {md_path.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
