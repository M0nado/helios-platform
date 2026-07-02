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
import os
import shutil
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
        url = remote.get("url", "")
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
    ci_closeness = 15 if len(files) <= 50 else 8 if len(files) <= 150 else 3
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
    return {**ref, "score": total, "recommendedAction": action, "fileCount": len(files), "modules": modules}


def rank_branches(manifest: dict[str, Any]) -> list[dict[str, Any]]:
    target = manifest.get("targetBranch", "work")
    ranked = []
    for ref in branch_refs():
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
        "notes": "This check never prints secret values. Configure credentials in the shell or CI secrets.",
    }


def markdown_table(headers: list[str], rows: list[list[Any]]) -> str:
    out = ["| " + " | ".join(headers) + " |", "|" + "|".join(["---"] * len(headers)) + "|"]
    for row in rows:
        out.append("| " + " | ".join(str(cell).replace("|", "\\|") for cell in row) + " |")
    return "\n".join(out)


def write_reports(out_dir: Path, ranked: list[dict[str, Any]], ideas: list[dict[str, Any]], conn: dict[str, Any], remote_actions: list[dict[str, Any]], fetch_result: dict[str, Any]) -> None:
    save_json(out_dir / "branch-ranking.json", ranked)
    save_json(out_dir / "idea-impact.json", ideas)
    save_json(out_dir / "connectivity.json", conn)
    save_json(out_dir / "remote-actions.json", {"remoteActions": remote_actions, "fetch": fetch_result})

    branch_rows = [[b["name"], b["score"], b["recommendedAction"], b["fileCount"], ", ".join(list(b["modules"].keys())[:6])] for b in ranked]
    idea_rows = [[i["category"], i["module"], f"{i['path']}:{i['line']}", i["recommendedAction"], i["bonusImpact"]] for i in ideas[:50]]
    conn_rows = [[c["name"], c["available"], c["authenticated"], c["detail"]] for c in conn["checks"]]
    remote_rows = [[r["name"], r["enabled"], r["urlConfigured"], r["action"], r["result"]] for r in remote_actions]

    write_text(out_dir / "branch-ranking.md", "# Branch Ranking\n\n" + markdown_table(["Branch", "Score", "Action", "Files", "Modules"], branch_rows) + "\n")
    write_text(out_dir / "idea-impact.md", "# Idea Impact\n\n" + markdown_table(["Category", "Module", "Source", "Action", "How it affects us"], idea_rows) + "\n")
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
        markdown_table(["Category", "Module", "Source", "Action", "How it affects us"], idea_rows[:20]),
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
    parser.add_argument("--fetch", action="store_true", help="Run git fetch --all --prune --tags.")
    args = parser.parse_args()

    manifest = load_manifest(args.manifest)
    remote_actions = configure_remotes(manifest, apply=args.configure_remotes)
    fetch_result = fetch_remotes(apply=args.fetch)
    ranked = rank_branches(manifest)
    ideas = extract_ideas(manifest)
    conn = connectivity()
    write_reports(args.out, ranked, ideas, conn, remote_actions, fetch_result)
    print(f"Wrote branch intelligence reports to {args.out.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
