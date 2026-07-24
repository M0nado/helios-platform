#!/usr/bin/env python3
"""Plan-first HELIOS setup helper.

Every command is read-only and deterministic. Cloud, repository, and
collaboration writes remain in reviewed protected-environment workflows.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
TARGETS_FILE = ASSETS / "connections.json"

TOOLS: tuple[tuple[str, tuple[str, ...], bool], ...] = (
    ("az", ("version",), True),
    ("gh", ("--version",), True),
    ("dotnet", ("--version",), True),
    ("pwsh", ("--version",), True),
    ("node", ("--version",), True),
    ("npm", ("--version",), True),
    ("azd", ("version",), False),
    ("docker", ("--version",), False),
    ("jq", ("--version",), False),
    ("pac", ("--version",), False),
    ("atk", ("--version",), False),
)

ENVIRONMENT_KEYS = (
    "HELIOS_AZURE_CONNECTOR_URL",
    "AZURE_DEVOPS_ORGANIZATION",
    "AZURE_TENANT_ID",
    "AZURE_SUBSCRIPTION_ID",
    "AZURE_RESOURCE_GROUP",
    "AZURE_CLIENT_ID",
)


def read_asset(name: str) -> dict[str, Any]:
    return json.loads((ASSETS / name).read_text(encoding="utf-8"))


def read_targets() -> dict[str, Any]:
    return read_asset("connections.json")


def first_line(value: str) -> str:
    return value.strip().splitlines()[0] if value.strip() else "available"


def check_tool(command: str, arguments: tuple[str, ...], required: bool) -> dict[str, Any]:
    path = shutil.which(command)
    result: dict[str, Any] = {
        "command": command,
        "required": required,
        "available": path is not None,
    }
    if path is None:
        return result
    try:
        process = subprocess.run(
            [path, *arguments],
            check=False,
            capture_output=True,
            text=True,
            timeout=8,
        )
        result["version"] = first_line(process.stdout or process.stderr)
        result["healthy"] = process.returncode == 0
    except (OSError, subprocess.TimeoutExpired):
        result["healthy"] = False
        result["version"] = "version check failed"
    return result


def doctor() -> dict[str, Any]:
    tools = [check_tool(*entry) for entry in TOOLS]
    environment = {
        key: {
            "configured": bool(os.environ.get(key)),
            "secret": False,
        }
        for key in ENVIRONMENT_KEYS
    }
    return {
        "mode": "read-only",
        "tools": tools,
        "environment": environment,
        "requiredToolsReady": all(
            tool["available"] and tool.get("healthy", False)
            for tool in tools
            if tool["required"]
        ),
        "cloudAuthenticated": "not-checked",
        "azureDeployed": False,
    }


def release_plan(environment: str) -> dict[str, Any]:
    if environment not in {"azure-dev", "azure-test", "azure-prod"}:
        raise ValueError("environment must be azure-dev, azure-test, or azure-prod")
    return {
        "environment": environment,
        "executionMode": "plan-only",
        "automatic": [
            "toolchain diagnosis",
            "identity and resource inventory",
            "Bicep compilation",
            "Azure what-if evidence generation",
            "test and security validation",
            "task branch and draft pull request",
            "dry-run connector receipts",
        ],
        "administratorGates": [
            "tenant authentication and consent",
            "least-privilege RBAC assignment",
            "immutable image publication",
            "what-if approval",
            "Azure deployment approval",
            "connector activation",
            "Microsoft 365 Copilot and Teams publication",
        ],
        "commands": [
            "pwsh ./monado/helios-control/scripts/Connect-HeliosAzureInteractive.ps1",
            "python plugins/helios-control-fabric/scripts/helios.py doctor --json",
            "python plugins/helios-control-fabric/scripts/helios.py oidc --environment "
            + environment
            + " --json",
            "python plugins/helios-control-fabric/scripts/helios.py edge --environment "
            + environment
            + " --json",
            "GitHub Actions: HELIOS Azure → what-if",
            "GitHub protected environment: " + environment + " → deployment approval",
        ],
        "federationSubject": (
            "repo:M0nado/helios-platform:environment:" + environment
        ),
    }


def oidc_contract(environment: str) -> dict[str, Any]:
    contract = read_asset("oidc.json")
    subjects = contract["subjects"]
    if environment not in subjects:
        raise ValueError("environment must be azure-dev, azure-test, or azure-prod")
    return {
        **contract,
        "environment": environment,
        "selectedSubject": subjects[environment],
        "configuredVariables": {
            key: bool(os.environ.get(key))
            for key in contract["requiredVariables"]
        },
        "executionMode": "plan-only",
    }


def devops_sync_plan() -> dict[str, Any]:
    plan = read_asset("devops-sync.json")
    return {
        **plan,
        "organizationConfigured": bool(os.environ.get("AZURE_DEVOPS_ORGANIZATION")),
        "executionMode": "plan-only",
    }


def runner_plan() -> dict[str, Any]:
    return {
        **read_asset("runner-topology.json"),
        "executionMode": "plan-only",
    }


def edge_plan(environment: str) -> dict[str, Any]:
    if environment not in {"azure-dev", "azure-test", "azure-prod"}:
        raise ValueError("environment must be azure-dev, azure-test, or azure-prod")
    return {
        **read_asset("edge-runtime.json"),
        "environment": environment,
        "executionMode": "plan-only",
    }


def print_human(payload: dict[str, Any]) -> None:
    if "tools" in payload:
        print("HELIOS doctor (read-only)")
        for tool in payload["tools"]:
            mark = "OK" if tool["available"] and tool.get("healthy", False) else (
                "OPTIONAL" if not tool["required"] else "MISSING"
            )
            print(f"  [{mark:8}] {tool['command']}")
        print("  Cloud authentication: not checked")
        print("  Azure runtime: not live")
    elif "authorities" in payload:
        print("HELIOS authorities")
        for name, value in payload["authorities"].items():
            print(f"  {name}: {value}")
    elif "selectedSubject" in payload:
        print(f"HELIOS OIDC contract ({payload['environment']})")
        print(f"  issuer: {payload['issuer']}")
        print(f"  audience: {payload['audience']}")
        print(f"  subject: {payload['selectedSubject']}")
        print("  No identity or RBAC mutation was performed.")
    elif "sourceOfTruth" in payload:
        print("HELIOS Azure DevOps sync plan")
        print(f"  source: {payload['sourceOfTruth']}")
        print(f"  direction: {payload['synchronization']['direction']}")
        print("  writes: disabled")
    elif "selfHosted" in payload:
        print("HELIOS runner topology")
        print(f"  validation: {payload['validation']['provider']}")
        print(f"  release environment: {payload['release']['environment']}")
        print("  self-hosted runners: disabled")
    elif "targetEdge" in payload:
        print(f"HELIOS Azure Edge plan ({payload['environment']})")
        print(f"  service: {payload['targetEdge']['service']}")
        print(f"  connectivity: {payload['targetEdge']['connectivity']}")
        print("  automatic apply: false")
    else:
        print(f"HELIOS {payload['environment']} release plan")
        for index, gate in enumerate(payload["administratorGates"], start=1):
            print(f"  GATE {index}: {gate}")
        print("  No cloud mutation was performed.")


def add_environment_argument(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--environment",
        default="azure-dev",
        choices=("azure-dev", "azure-test", "azure-prod"),
    )
    parser.add_argument("--json", action="store_true")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="HELIOS plan-first setup helper")
    subparsers = parser.add_subparsers(dest="command", required=True)
    doctor_parser = subparsers.add_parser("doctor", help="Check local prerequisites")
    doctor_parser.add_argument("--json", action="store_true")
    targets_parser = subparsers.add_parser("targets", help="Show canonical authorities")
    targets_parser.add_argument("--json", action="store_true")
    plan_parser = subparsers.add_parser("plan", help="Create a non-executing release plan")
    add_environment_argument(plan_parser)
    oidc_parser = subparsers.add_parser("oidc", help="Show the exact GitHub-to-Azure OIDC contract")
    add_environment_argument(oidc_parser)
    sync_parser = subparsers.add_parser("devops-sync", help="Show the read-only Azure DevOps sync plan")
    sync_parser.add_argument("--json", action="store_true")
    runner_parser = subparsers.add_parser("runners", help="Show the governed GitHub runner topology")
    runner_parser.add_argument("--json", action="store_true")
    edge_parser = subparsers.add_parser("edge", help="Show the Azure Front Door private-edge activation plan")
    add_environment_argument(edge_parser)
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    if args.command == "doctor":
        payload = doctor()
    elif args.command == "targets":
        payload = read_targets()
    elif args.command == "plan":
        payload = release_plan(args.environment)
    elif args.command == "oidc":
        payload = oidc_contract(args.environment)
    elif args.command == "devops-sync":
        payload = devops_sync_plan()
    elif args.command == "runners":
        payload = runner_plan()
    else:
        payload = edge_plan(args.environment)
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        print_human(payload)
    return 0


if __name__ == "__main__":
    sys.exit(main())
