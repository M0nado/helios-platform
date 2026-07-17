#!/usr/bin/env python3
"""Guarded HELIOS enterprise setup for Claude Code, Agent 365, and DevOps."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from typing import Any, Sequence

PLUGIN_ROOT = Path(__file__).resolve().parent.parent
REPO_ROOT = PLUGIN_ROOT.parents[1]
SETUP_PATH = PLUGIN_ROOT / "assets" / "enterprise-setup.json"
AGENT365_TEMPLATE = REPO_ROOT / "apps" / "helios-control" / "copilot" / "agent365" / "a365.config.template.json"
AGENT365_OUTPUT = REPO_ROOT / "apps" / "helios-control" / "copilot" / "agent365" / "a365.config.json"


class SetupError(RuntimeError):
    pass


def run(command: Sequence[str], *, capture: bool = False) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(list(command), check=False, text=True, capture_output=capture, shell=False)
    if result.returncode != 0:
        detail = (result.stderr or result.stdout or "command failed").strip()
        raise SetupError(f"{command[0]} failed: {detail}")
    return result


def load_setup() -> dict[str, Any]:
    payload = json.loads(SETUP_PATH.read_text(encoding="utf-8"))
    if payload.get("productionEnabled") is not False:
        raise SetupError("Production must remain disabled.")
    phases = payload.get("phases", [])
    if not phases or any(item.get("id") == "production" and item.get("operations") for item in phases):
        raise SetupError("Invalid production gate.")
    return payload


def require_tool(name: str) -> None:
    if not shutil.which(name):
        raise SetupError(f"Required tool is not installed: {name}")


def require_env(name: str) -> str:
    value = os.getenv(name, "").strip()
    if not value:
        raise SetupError(f"Set {name} in the current shell. Do not commit it.")
    return value


def operation_commands(operation: str) -> list[list[str]]:
    mapping: dict[str, list[list[str]]] = {
        "install-azure-cli": [["winget", "install", "Microsoft.AzureCLI"]],
        "install-azd": [["winget", "install", "Microsoft.Azd"]],
        "install-functions-tools": [["winget", "install", "Microsoft.Azure.FunctionsCoreTools"]],
        "install-powershell": [["winget", "install", "Microsoft.PowerShell"]],
        "install-github-cli": [["winget", "install", "GitHub.cli"]],
        "install-node-lts": [["winget", "install", "OpenJS.NodeJS.LTS"]],
        "install-m365-agents-toolkit": [["npm", "install", "-g", "@microsoft/m365agentstoolkit-cli"]],
        "install-claude-code": [["npm", "install", "-g", "@anthropic-ai/claude-code"]],
        "install-azure-devops-extension": [["az", "extension", "add", "--name", "azure-devops"]],
        "azure-interactive-login": [["az", "login"]],
        "azd-interactive-login": [["azd", "auth", "login"]],
        "github-interactive-login": [["gh", "auth", "login"]],
        "power-platform-interactive-login": [["pac", "auth", "create"]],
        "agent365-interactive-login": [["a365", "auth", "login"]],
        "validate-agent365-requirements": [["a365", "setup", "requirements", "-c", str(AGENT365_OUTPUT)]],
        "preview-agent365-setup": [["a365", "setup", "all", "-c", str(AGENT365_OUTPUT), "--dry-run"]],
        "validate-claude-mcp": [["claude", "mcp", "list"]],
        "validate-github": [["gh", "pr", "view", "177", "--repo", "M0nado/helios-platform"]],
        "validate-azure": [["az", "account", "show"]],
    }
    if operation == "set-azure-devops-defaults":
        organization = require_env("HELIOS_AZURE_DEVOPS_ORG")
        project = require_env("HELIOS_AZURE_DEVOPS_PROJECT")
        return [["az", "devops", "configure", "--defaults", f"organization={organization}", f"project={project}"]]
    if operation in {"validate-azure-devops-project", "validate-azure-devops"}:
        return [["az", "devops", "project", "list"]]
    if operation in {"generate-admin-handoff", "validate-collaboration-destinations"}:
        return []
    if operation not in mapping:
        raise SetupError(f"Unknown operation: {operation}")
    return mapping[operation]


def render_agent365_config() -> Path:
    values = {
        "AZURE_TENANT_ID": require_env("AZURE_TENANT_ID"),
        "AZURE_SUBSCRIPTION_ID": require_env("AZURE_SUBSCRIPTION_ID"),
        "AZURE_LOCATION": os.getenv("AZURE_LOCATION", "eastus2"),
        "HELIOS_AGENT_USER_PRINCIPAL_NAME": require_env("HELIOS_AGENT_USER_PRINCIPAL_NAME"),
        "HELIOS_AGENT_MANAGER_EMAIL": require_env("HELIOS_AGENT_MANAGER_EMAIL"),
    }
    text = AGENT365_TEMPLATE.read_text(encoding="utf-8")
    for name, value in values.items():
        text = text.replace("${" + name + "}", value)
    json.loads(text)
    AGENT365_OUTPUT.write_text(text, encoding="utf-8")
    return AGENT365_OUTPUT


def write_admin_handoff() -> Path:
    config_dir = str(AGENT365_OUTPUT.parent.resolve())
    payload = {
        "configDirectory": config_dir,
        "productionEnabled": False,
        "reviewRequired": True,
        "nextStep": "A Global Administrator reviews generated configuration and uses the Agent 365 admin handoff flow.",
        "toolingManifest": str((AGENT365_OUTPUT.parent / "ToolingManifest.json").resolve()),
    }
    path = AGENT365_OUTPUT.parent / "admin-handoff.json"
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return path


def print_plan(setup: dict[str, Any]) -> None:
    print(json.dumps(setup, indent=2))


def execute_phase(phase_id: str, confirmation: str) -> None:
    setup = load_setup()
    phase = next((item for item in setup["phases"] if item["id"] == phase_id), None)
    if phase is None:
        raise SetupError(f"Unknown phase: {phase_id}")
    if phase_id == "production":
        raise SetupError("Production is disabled behind GitHub Issue #162.")
    if confirmation != phase["confirmation"]:
        raise SetupError(f"Exact confirmation required: {phase['confirmation']}")

    if phase_id == "agent365-developer" and not AGENT365_OUTPUT.exists():
        render_agent365_config()

    for operation in phase["operations"]:
        if operation == "generate-admin-handoff":
            print(write_admin_handoff())
            continue
        if operation == "validate-collaboration-destinations":
            print(json.dumps({
                "linear": "Helios Integration Fabric",
                "slack": ["#helios-control-plane", "#helios-ops"],
                "teams": "Helios / Helios Ops",
                "sharePoint": "Helios/Governance",
            }, indent=2))
            continue
        for command in operation_commands(operation):
            require_tool(command[0])
            run(command)


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description="HELIOS enterprise setup controller")
    sub = root.add_subparsers(dest="command", required=True)
    sub.add_parser("plan")
    sub.add_parser("render-agent365")
    execute = sub.add_parser("execute")
    execute.add_argument("--phase", required=True)
    execute.add_argument("--confirm", required=True)
    return root


def main(argv: Sequence[str] | None = None) -> int:
    args = parser().parse_args(argv)
    try:
        if args.command == "plan":
            print_plan(load_setup())
        elif args.command == "render-agent365":
            print(render_agent365_config())
        else:
            execute_phase(args.phase, args.confirm)
        return 0
    except (OSError, json.JSONDecodeError, SetupError) as error:
        print(f"HELIOS Enterprise: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
