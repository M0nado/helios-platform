#!/usr/bin/env python3
"""HELIOS branch/module intelligence runner.

Generates branch rankings, module impact scores, idea impact reports, local CLI
connectivity checks, and a markdown dashboard. The script is intentionally safe:
it does not merge or delete branches. Remote configuration/fetching only happens
when explicitly requested.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import hashlib
import os
import shutil
import urllib.request
import subprocess
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_MANIFEST = ROOT / "docs" / "integration" / "remote-manifest.json"
DEFAULT_OUT = ROOT / "reports" / "branch-intelligence"


def run(cmd: list[str], check: bool = False) -> tuple[int, str, str]:
    proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True)
    if check and proc.returncode != 0:
        raise RuntimeError(f"Command failed ({proc.returncode}): {' '.join(cmd)}\n{proc.stderr}")
    return proc.returncode, proc.stdout.strip(), proc.stderr.strip()


def load_manifest(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def save_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def configure_remotes(manifest: dict[str, Any], apply: bool) -> list[dict[str, Any]]:
    _, existing_out, _ = run(["git", "remote"])
    existing = set(existing_out.splitlines()) if existing_out else set()
    actions: list[dict[str, Any]] = []
    for remote in manifest.get("remotes", []):
        name = remote.get("name", "")
        url = remote.get("url", "") or os.environ.get(remote.get("urlEnv", ""), "")
        enabled = bool(remote.get("enabled"))
        action = "skip"
        reason = "disabled" if not enabled else "missing url" if not url else "already exists" if name in existing else "add"
        if enabled and url and name not in existing:
            action = "add"
            if apply:
                code, _, err = run(["git", "remote", "add", name, url])
                reason = "added" if code == 0 else f"failed: {err}"
            else:
                reason = "dry-run add"
        actions.append({"name": name, "urlConfigured": bool(url), "enabled": enabled, "action": action, "result": reason})
    return actions


def fetch_remotes(apply: bool) -> dict[str, Any]:
    if not apply:
        return {"executed": False, "result": "dry-run"}
    code, out, err = run(["git", "fetch", "--all", "--prune", "--tags"])
    return {"executed": True, "exitCode": code, "stdout": out[-4000:], "stderr": err[-4000:]}


def branch_refs() -> list[dict[str, Any]]:
    fmt = "%(refname:short)|%(objectname:short)|%(committerdate:iso8601)|%(authorname)|%(subject)"
    code, out, _ = run(["git", "for-each-ref", f"--format={fmt}", "refs/heads", "refs/remotes"])
    if code != 0 or not out:
        return []
    refs = []
    for line in out.splitlines():
        parts = line.split("|", 4)
        if len(parts) == 5:
            refs.append({"name": parts[0], "commit": parts[1], "date": parts[2], "author": parts[3], "subject": parts[4]})
    return refs


def changed_files_for_ref(ref: str, target: str) -> list[str]:
    if ref == target:
        code, out, _ = run(["git", "show", "--name-only", "--format=", ref])
    else:
        code, base, _ = run(["git", "merge-base", target, ref])
        if code != 0 or not base:
            code, out, _ = run(["git", "show", "--name-only", "--format=", ref])
        else:
            code, out, _ = run(["git", "diff", "--name-only", base, ref])
    if code != 0 or not out:
        return []
    return sorted({line.strip() for line in out.splitlines() if line.strip()})


def module_for(path: str, weights: dict[str, int]) -> str:
    candidates = sorted(weights, key=len, reverse=True)
    for prefix in candidates:
        if path == prefix or path.startswith(prefix.rstrip("/") + "/"):
            return prefix
    return path.split("/", 1)[0] if "/" in path else "root"



def github_ci_status(branch: str) -> dict[str, Any]:
    """Best-effort GitHub Actions status lookup using gh when available."""
    if shutil.which("gh") is None:
        return {"available": False, "score": 0, "summary": "gh not found"}
    cmd = [
        "gh", "run", "list",
        "--branch", branch.split("/", 1)[-1],
        "--limit", "10",
        "--json", "conclusion,status,workflowName,createdAt,updatedAt",
    ]
    code, out, err = run(cmd)
    if code != 0 or not out:
        return {"available": True, "score": 0, "summary": err or "no runs"}
    try:
        runs = json.loads(out)
    except json.JSONDecodeError:
        return {"available": True, "score": 0, "summary": "unparseable gh output"}
    if not runs:
        return {"available": True, "score": 0, "summary": "no runs"}
    completed = [r for r in runs if r.get("status") == "completed"]
    successes = [r for r in completed if r.get("conclusion") == "success"]
    score = min(20, int((len(successes) / max(1, len(completed))) * 20)) if completed else 5
    latest = runs[0]
    return {
        "available": True,
        "score": score,
        "summary": f"{len(successes)}/{len(completed)} completed runs succeeded" if completed else "runs are not completed",
        "latestWorkflow": latest.get("workflowName"),
        "latestStatus": latest.get("status"),
        "latestConclusion": latest.get("conclusion"),
    }

def score_branch(ref: dict[str, Any], files: list[str], manifest: dict[str, Any]) -> dict[str, Any]:
    weights: dict[str, int] = manifest.get("moduleWeights", {})
    modules: dict[str, dict[str, Any]] = {}
    for file in files:
        module = module_for(file, weights)
        entry = modules.setdefault(module, {"files": 0, "weight": weights.get(module, 4), "paths": []})
        entry["files"] += 1
        if len(entry["paths"]) < 10:
            entry["paths"].append(file)
    module_impact = min(40, sum(entry["weight"] for entry in modules.values()))
    test_value = 12 if any(path.startswith("tests/") or "/Tests/" in path or path.endswith("Tests.fs") for path in files) else 0
    workflow_value = 10 if any(path.startswith(".github/workflows/") or path.endswith("azure-pipelines.yml") for path in files) else 0
    analytics_value = 8 if any(path.startswith("src/analytics") or "Analytics" in path for path in files) else 0
    heuristic_ci_closeness = 15 if len(files) <= 50 else 8 if len(files) <= 150 else 3
    live_ci = github_ci_status(ref["name"])
    ci_closeness = max(heuristic_ci_closeness, live_ci.get("score", 0))
    conflict_risk = min(20, len(files) // 25)
    total = max(0, min(100, module_impact + test_value + workflow_value + analytics_value + ci_closeness - conflict_risk))
    if total >= 85:
        action = "merge-first"
    elif total >= 70:
        action = "compare-selectively"
    elif total >= 50:
        action = "extract-ideas"
    elif total >= 30:
        action = "archive-review"
    else:
        action = "prune-candidate-after-review"
    return {**ref, "score": total, "recommendedAction": action, "fileCount": len(files), "modules": modules, "ci": live_ci, "heuristicCiCloseness": heuristic_ci_closeness}


def rank_branches(manifest: dict[str, Any], remote_names: list[str] | None = None) -> list[dict[str, Any]]:
    target = manifest.get("targetBranch", "work")
    ranked = []
    remote_prefixes = tuple(f"{name}/" for name in (remote_names or []))
    for ref in branch_refs():
        if remote_prefixes and not ref["name"].startswith(remote_prefixes):
            continue
        files = changed_files_for_ref(ref["name"], target)
        ranked.append(score_branch(ref, files, manifest))
    return sorted(ranked, key=lambda item: item["score"], reverse=True)


def extract_ideas(manifest: dict[str, Any], max_files: int = 250) -> list[dict[str, Any]]:
    keywords: dict[str, list[str]] = manifest.get("ideaKeywords", {})
    docs = [p for p in ROOT.rglob("*.md") if ".git" not in p.parts and "bin" not in p.parts and "obj" not in p.parts]
    docs += [p for p in ROOT.rglob("*.txt") if ".git" not in p.parts and "bin" not in p.parts and "obj" not in p.parts]
    ideas: list[dict[str, Any]] = []
    for path in sorted(docs)[:max_files]:
        rel = path.relative_to(ROOT).as_posix()
        try:
            lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
        except OSError:
            continue
        for idx, line in enumerate(lines, 1):
            normalized = line.lower()
            if not (line.startswith("#") or line.lstrip().startswith("-")):
                continue
            for category, words in keywords.items():
                if any(word.lower() in normalized for word in words):
                    module = rel.split("/", 1)[0]
                    impact = impact_for_category(category)
                    ideas.append({
                        "category": category,
                        "module": module,
                        "path": rel,
                        "line": idx,
                        "idea": line.strip().lstrip("#- ").strip(),
                        "recommendedAction": "merge-or-rewrite" if category in {"hermes", "fsharp-analytics", "github-automation"} else "save-and-score",
                        "bonusImpact": impact,
                    })
                    break
            if len(ideas) >= 400:
                return ideas
    return ideas


def impact_for_category(category: str) -> str:
    impacts = {
        "hermes": "Adds fleet agent telemetry and learning sources for distributed workload optimization.",
        "xcore": "Creates a native performance path for C++/XCore acceleration without blocking managed code.",
        "fsharp-analytics": "Uses existing F# math/prediction APIs to rank modules, branches, and fleet events.",
        "azure-automation": "Improves deployment repeatability and environment verification through Azure CLI automation.",
        "github-automation": "Improves branch pruning, PR selection, and dashboard freshness.",
        "aihub-openai": "Enables AI-assisted idea extraction and learning-record enrichment when credentials are present.",
    }
    return impacts.get(category, "Potential platform capability improvement after review.")



def dedupe_ideas(ideas: list[dict[str, Any]]) -> list[dict[str, Any]]:
    grouped: dict[str, dict[str, Any]] = {}
    for idea in ideas:
        normalized = " ".join(idea["idea"].lower().split())[:120]
        key = hashlib.sha256(f"{idea['category']}|{idea['module']}|{normalized}".encode("utf-8")).hexdigest()[:16]
        entry = grouped.setdefault(key, {**idea, "dedupeKey": key, "occurrences": 0, "sources": []})
        entry["occurrences"] += 1
        if len(entry["sources"]) < 8:
            entry["sources"].append(f"{idea['path']}:{idea['line']}")
    def score(item: dict[str, Any]) -> int:
        category_bonus = 20 if item["category"] in {"hermes", "xcore", "fsharp-analytics"} else 12
        return category_bonus + min(30, item["occurrences"] * 3)
    summary = sorted(grouped.values(), key=score, reverse=True)
    for item in summary:
        item["impactScore"] = score(item)
    return summary


def enrich_ideas(ideas: list[dict[str, Any]], enabled: bool) -> list[dict[str, Any]]:
    """Optional AI enrichment placeholder.

    The implementation is intentionally conservative: when enabled and credentials
    are present, it marks records as ready for enrichment but avoids sending repo
    content by default. This creates the stable schema before any external calls
    are allowed by policy.
    """
    configured = bool(os.environ.get("OPENAI_API_KEY") or (os.environ.get("AZURE_OPENAI_ENDPOINT") and os.environ.get("AZURE_OPENAI_API_KEY")))
    for idea in ideas:
        idea["aiEnrichment"] = {
            "requested": enabled,
            "configured": configured,
            "status": "ready-not-sent" if enabled and configured else "not-configured" if enabled else "disabled",
        }
    return ideas


def read_hermes_jsonl(paths: list[Path]) -> list[dict[str, Any]]:
    events: list[dict[str, Any]] = []
    for path in paths:
        if not path.exists():
            continue
        for line_no, line in enumerate(path.read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
            if not line.strip():
                continue
            try:
                data = json.loads(line)
            except json.JSONDecodeError:
                continue
            events.append({"path": str(path), "line": line_no, "event": data})
    return events


def hermes_ideas(events: list[dict[str, Any]]) -> list[dict[str, Any]]:
    ideas = []
    for item in events:
        event = item["event"]
        status = str(event.get("status", "unknown"))
        agent_type = str(event.get("agentType", event.get("type", "agent")))
        ideas.append({
            "category": "hermes",
            "module": "hermes-fleet",
            "path": item["path"],
            "line": item["line"],
            "idea": f"Hermes {agent_type} event recorded with status {status}",
            "recommendedAction": "merge-or-rewrite",
            "bonusImpact": impact_for_category("hermes"),
        })
    return ideas


def azure_readiness() -> dict[str, Any]:
    if shutil.which("az") is None:
        return {"available": False, "score": 0, "summary": "az not found"}
    code, out, err = run(["az", "account", "show"])
    if code != 0:
        return {"available": True, "authenticated": False, "score": 10, "summary": err.splitlines()[:3]}
    try:
        account = json.loads(out)
    except json.JSONDecodeError:
        return {"available": True, "authenticated": True, "score": 25, "summary": "account output not JSON"}
    score = 70
    if os.environ.get("AZURE_OPENAI_ENDPOINT") and os.environ.get("AZURE_OPENAI_API_KEY"):
        score += 20
    return {"available": True, "authenticated": True, "score": min(100, score), "subscription": account.get("name"), "tenantId": account.get("tenantId")}


def build_agent_queue(ranked: list[dict[str, Any]], ideas: list[dict[str, Any]]) -> list[dict[str, Any]]:
    queue: list[dict[str, Any]] = []
    for branch in ranked:
        for module, details in branch.get("modules", {}).items():
            queue.append({
                "taskType": branch["recommendedAction"],
                "branch": branch["name"],
                "module": module,
                "priorityScore": branch["score"] + details.get("weight", 0),
                "allowedPaths": details.get("paths", []),
                "blockedPaths": [".git", "bin", "obj"],
                "expectedOutput": "comparison notes, merge risk, tests to run, and idea extraction",
            })
    for idea in ideas[:50]:
        queue.append({
            "taskType": "idea-review",
            "branch": "knowledge-base",
            "module": idea.get("module"),
            "priorityScore": idea.get("impactScore", 10),
            "allowedPaths": idea.get("sources", [f"{idea.get('path')}:{idea.get('line')}"]),
            "blockedPaths": [".git", "bin", "obj"],
            "expectedOutput": idea.get("bonusImpact"),
        })
    return sorted(queue, key=lambda item: item["priorityScore"], reverse=True)


def analytics_metrics(ranked: list[dict[str, Any]], ideas: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "schema": "HELIOS.BranchIntelligence.AnalyticsMetrics.v1",
        "branchScores": [{"name": b["name"], "score": b["score"], "fileCount": b["fileCount"]} for b in ranked],
        "ideaScores": [{"key": i.get("dedupeKey"), "category": i["category"], "score": i.get("impactScore", 0), "occurrences": i.get("occurrences", 1)} for i in ideas],
        "intendedConsumer": "src/analytics/HELIOS.Analytics.FSharp via future HELIOS.RepositoryAnalytics tool",
    }

def check_tool(name: str, command: list[str]) -> dict[str, Any]:
    exists = shutil.which(command[0]) is not None
    if not exists:
        return {"name": name, "available": False, "authenticated": False, "detail": f"{command[0]} not found"}
    code, out, err = run(command)
    return {"name": name, "available": True, "authenticated": code == 0, "detail": (out or err).splitlines()[:3]}


def connectivity() -> dict[str, Any]:
    checks = [
        check_tool("git", ["git", "--version"]),
        check_tool("GitHub CLI", ["gh", "auth", "status"]),
        check_tool("Azure CLI", ["az", "account", "show"]),
        check_tool(".NET SDK", ["dotnet", "--version"]),
        check_tool("Python", [sys.executable, "--version"]),
    ]
    openai_key = bool(os.environ.get("OPENAI_API_KEY"))
    azure_openai = bool(os.environ.get("AZURE_OPENAI_ENDPOINT") and os.environ.get("AZURE_OPENAI_API_KEY"))
    return {
        "timestamp": dt.datetime.now(dt.UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "checks": checks,
        "openaiApiKeyConfigured": openai_key,
        "azureOpenAIConfigured": azure_openai,
        "azureReadiness": azure_readiness(),
        "notes": "This check never prints secret values. Configure credentials in the shell or CI secrets.",
    }


def markdown_table(headers: list[str], rows: list[list[Any]]) -> str:
    out = ["| " + " | ".join(headers) + " |", "|" + "|".join(["---"] * len(headers)) + "|"]
    for row in rows:
        out.append("| " + " | ".join(str(cell).replace("|", "\\|") for cell in row) + " |")
    return "\n".join(out)


def write_reports(out_dir: Path, ranked: list[dict[str, Any]], ideas: list[dict[str, Any]], idea_summary: list[dict[str, Any]], agent_queue: list[dict[str, Any]], conn: dict[str, Any], remote_actions: list[dict[str, Any]], fetch_result: dict[str, Any]) -> None:
    save_json(out_dir / "branch-ranking.json", ranked)
    save_json(out_dir / "idea-impact.json", ideas)
    save_json(out_dir / "idea-impact-summary.json", idea_summary)
    save_json(out_dir / "agent-work-queue.json", agent_queue)
    save_json(out_dir / "analytics-metrics.json", analytics_metrics(ranked, idea_summary))
    save_json(out_dir / "connectivity.json", conn)
    save_json(out_dir / "remote-actions.json", {"remoteActions": remote_actions, "fetch": fetch_result})

    branch_rows = [[b["name"], b["score"], b["recommendedAction"], b["fileCount"], ", ".join(list(b["modules"].keys())[:6])] for b in ranked]
    idea_rows = [[i["category"], i["module"], i.get("dedupeKey", ""), i.get("occurrences", 1), i["recommendedAction"], i["bonusImpact"]] for i in idea_summary[:50]]
    queue_rows = [[q["taskType"], q["branch"], q["module"], q["priorityScore"], q["expectedOutput"]] for q in agent_queue[:30]]
    conn_rows = [[c["name"], c["available"], c["authenticated"], c["detail"]] for c in conn["checks"]]
    remote_rows = [[r["name"], r["enabled"], r["urlConfigured"], r["action"], r["result"]] for r in remote_actions]

    full_idea_rows = [[i["category"], i["module"], f"{i['path']}:{i['line']}", i["recommendedAction"], i["bonusImpact"]] for i in ideas[:50]]
    write_text(out_dir / "branch-ranking.md", "# Branch Ranking\n\n" + markdown_table(["Branch", "Score", "Action", "Files", "Modules"], branch_rows) + "\n")
    write_text(out_dir / "idea-impact.md", "# Idea Impact\n\n" + markdown_table(["Category", "Module", "Source", "Action", "How it affects us"], full_idea_rows) + "\n")
    write_text(out_dir / "idea-impact-summary.md", "# Idea Impact Summary\n\n" + markdown_table(["Category", "Module", "Key", "Occurrences", "Action", "How it affects us"], idea_rows) + "\n")
    write_text(out_dir / "agent-work-queue.md", "# Agent Work Queue\n\n" + markdown_table(["Task", "Branch", "Module", "Priority", "Expected output"], queue_rows) + "\n")
    write_text(out_dir / "connectivity.md", "# Local/CI Connectivity\n\n" + markdown_table(["Tool", "Available", "Authenticated", "Detail"], conn_rows) + "\n")
    dashboard = "\n".join([
        "# HELIOS Branch Intelligence Dashboard",
        "",
        f"Generated: {conn['timestamp']}",
        "",
        "## Remote setup",
        markdown_table(["Remote", "Enabled", "URL configured", "Action", "Result"], remote_rows),
        "",
        "## Branch ranking",
        markdown_table(["Branch", "Score", "Action", "Files", "Modules"], branch_rows[:20]),
        "",
        "## Idea impact",
        markdown_table(["Category", "Module", "Key", "Occurrences", "Action", "How it affects us"], idea_rows[:20]),
        "",
        "## Agent work queue",
        markdown_table(["Task", "Branch", "Module", "Priority", "Expected output"], queue_rows[:20]),
        "",
        "## Connectivity",
        markdown_table(["Tool", "Available", "Authenticated", "Detail"], conn_rows),
        "",
        "## Safe merge policy",
        "1. Extract unique ideas before merging.",
        "2. Merge only high-value, low-risk branches.",
        "3. Archive stale ideas before pruning.",
        "4. Prune only reviewed, low-value, already-merged branches.",
        "5. Refresh this dashboard after each ranking run.",
        "",
    ])
    write_text(out_dir / "dashboard.md", dashboard)


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate HELIOS branch intelligence reports.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--configure-remotes", action="store_true", help="Add enabled remotes with configured URLs.")
    parser.add_argument("--fetch", "--fetch-remotes", dest="fetch", action="store_true", help="Run git fetch --all --prune --tags.")
    parser.add_argument("--remote", action="append", default=[], help="Limit branch ranking to one or more remote names.")
    parser.add_argument("--remote-inventory-only", action="store_true", help="Only inventory configured remote actions and matching remote branches.")
    parser.add_argument("--enrich-ideas", action="store_true", help="Mark ideas for optional AI enrichment when credentials are configured.")
    parser.add_argument("--hermes-jsonl", action="append", type=Path, default=[], help="Optional Hermes fleet JSONL event input.")
    args = parser.parse_args()

    manifest = load_manifest(args.manifest)
    remote_actions = configure_remotes(manifest, apply=args.configure_remotes)
    fetch_result = fetch_remotes(apply=args.fetch)
    ranked = rank_branches(manifest, args.remote)
    ideas = extract_ideas(manifest)
    hermes_events = read_hermes_jsonl(args.hermes_jsonl)
    ideas.extend(hermes_ideas(hermes_events))
    ideas = enrich_ideas(ideas, args.enrich_ideas)
    idea_summary = dedupe_ideas(ideas)
    agent_queue = build_agent_queue(ranked, idea_summary)
    conn = connectivity()
    write_reports(args.out, ranked, ideas, idea_summary, agent_queue, conn, remote_actions, fetch_result)
    print(f"Wrote branch intelligence reports to {args.out.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
