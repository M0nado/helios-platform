#!/usr/bin/env python3
from __future__ import annotations
import argparse
import json
import os
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "reports" / "control-plane"
SUMMARY_JSON = OUT_DIR / "control-summary.json"
SUMMARY_MD = OUT_DIR / "control-summary.md"


def run(cmd: list[str], timeout: int = 30) -> dict[str, Any]:
    if shutil.which(cmd[0]) is None:
        return {"command": cmd, "available": False, "ok": False, "detail": f"{cmd[0]} not found"}
    try:
        proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True, timeout=timeout)
        text = (proc.stdout or proc.stderr).strip()
        return {
            "command": cmd,
            "available": True,
            "ok": proc.returncode == 0,
            "exitCode": proc.returncode,
            "detail": text.splitlines()[:8],
        }
    except subprocess.TimeoutExpired:
        return {"command": cmd, "available": True, "ok": False, "detail": "timed out"}


def env_flags(names: list[str]) -> dict[str, bool]:
    return {name: bool(os.environ.get(name)) for name in names}


def github_status() -> dict[str, Any]:
    return {
        "auth": run(["gh", "auth", "status"]),
        "repo": run(["gh", "repo", "view", "--json", "nameWithOwner,defaultBranchRef,url"], timeout=20),
        "workflows": run(["gh", "workflow", "list", "--limit", "25"], timeout=20),
        "secrets": run(["gh", "secret", "list"], timeout=20),
        "safeCommands": [
            "gh auth login",
            "gh workflow run helios-control-plane.yml -f publish_pages=false -f update_wiki=true",
            "gh run list --limit 10",
        ],
    }


def azure_status() -> dict[str, Any]:
    return {
        "account": run(["az", "account", "show"], timeout=20),
        "bicep": run(["az", "bicep", "version"], timeout=20),
        "groups": run(["az", "group", "list", "--query", "[].{name:name,location:location}", "-o", "table"], timeout=20),
        "safeCommands": [
            "az login",
            "az group create --name <resource-group> --location <region>",
            "az deployment group what-if --resource-group <resource-group> --template-file infra/azure/main.bicep --parameters @infra/azure/parameters/dev.json",
            "scripts/azure/sync-keyvault-secrets.sh --vault <vault-name> --dry-run",
        ],
    }


def ai_status() -> dict[str, Any]:
    return {
        "openai": env_flags(["OPENAI_API_KEY"]),
        "azureOpenAI": env_flags(["AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_API_KEY"]),
        "claude": env_flags(["ANTHROPIC_API_KEY"]),
        "codex": {
            "cli": run(["codex", "--version"], timeout=10),
            "homeSet": bool(os.environ.get("CODEX_HOME")),
        },
        "microsoft365": env_flags(["M365_TENANT_ID", "M365_CLIENT_ID"]),
        "slack": env_flags(["SLACK_WEBHOOK_URL"]),
        "safeCommands": [
            "python3 scripts/ai/enrich-ideas.py",
            "python3 scripts/integrations/check-connections.py",
            "python3 scripts/control/helios-control.py ai",
            "scripts/azure/sync-keyvault-secrets.sh --vault <vault-name> --dry-run",
        ],
        "notes": "This status check never prints secret values and does not call OpenAI, Azure OpenAI, Slack, or Microsoft 365 APIs.",
    }


def local_status() -> dict[str, Any]:
    return {
        "tools": {name: shutil.which(name) for name in ["git", "gh", "az", "dotnet", "python3", "cmake", "codex", "claude"]},
        "paths": {path: (ROOT / path).exists() for path in [
            "scripts/setup/helios-dev.sh",
            "scripts/web/helios-web.py",
            "scripts/analysis/branch_intelligence.py",
            "scripts/ai/enrich-ideas.py",
            "infra/azure/main.bicep",
            ".github/workflows/helios-control-plane.yml",
            "status-site/index.md",
            "config/execution-order.json",
            "docs/integration/VISUAL_STUDIO_MAUI_SETUP.md",
            "docs/architecture/AZURE_HYBRID_ARCHITECTURE.md",
        ]},
        "safeCommands": [
            "scripts/setup/helios-dev.sh --serve",
            "curl -X POST http://127.0.0.1:8787/rebuild",
        ],
    }


def collect(scope: str) -> dict[str, Any]:
    report: dict[str, Any] = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "scope": scope,
        "local": local_status(),
    }
    if scope in {"all", "github"}:
        report["github"] = github_status()
    if scope in {"all", "azure"}:
        report["azure"] = azure_status()
    if scope in {"all", "ai"}:
        report["ai"] = ai_status()
    return report


def status_icon(value: Any) -> str:
    if isinstance(value, dict) and value.get("ok") is True:
        return "✅"
    if isinstance(value, dict) and value.get("available") is False:
        return "❌"
    if isinstance(value, dict) and value.get("ok") is False:
        return "⚠️"
    return "ℹ️"


def write_report(report: dict[str, Any]) -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    SUMMARY_JSON.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n")
    lines = ["# HELIOS Control Plane Summary", "", f"Generated: `{report['generatedUtc']}`", "", "## Local quick start", "", "```bash", "scripts/setup/helios-dev.sh --serve", "```", ""]
    for section in ["github", "azure", "ai"]:
        if section not in report:
            continue
        lines.extend([f"## {section.title()} Control", ""])
        for key, value in report[section].items():
            if key == "safeCommands":
                lines.append("### Safe commands")
                lines.append("")
                for command in value:
                    lines.append(f"- `{command}`")
                lines.append("")
            elif isinstance(value, dict) and ("ok" in value or "available" in value):
                lines.append(f"- {status_icon(value)} **{key}**: {value.get('detail')}")
            elif isinstance(value, dict):
                ready = sum(1 for item in value.values() if item)
                total = len(value)
                lines.append(f"- ℹ️ **{key}**: {ready}/{total} configured")
        lines.append("")
    SUMMARY_MD.write_text("\n".join(lines).rstrip() + "\n")


def main() -> int:
    parser = argparse.ArgumentParser(description="HELIOS GitHub/Azure/AI/Codex control-plane status and command guide.")
    parser.add_argument("scope", nargs="?", default="all", choices=["all", "github", "azure", "ai"], help="Control-plane area to inspect.")
    parser.add_argument("--json", action="store_true", help="Print JSON report to stdout.")
    args = parser.parse_args()
    report = collect(args.scope)
    write_report(report)
    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print(f"Wrote {SUMMARY_JSON.relative_to(ROOT)}")
        print(f"Wrote {SUMMARY_MD.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
