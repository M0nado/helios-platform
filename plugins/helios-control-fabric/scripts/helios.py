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
from typing import Any, Callable


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

SUPPORTED_ENVIRONMENTS = ("azure-dev", "azure-test", "azure-prod")
GITHUB_API_VERSION = "2026-03-10"
GitHubApiReader = Callable[[str], dict[str, Any]]


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


def validate_environment(environment: str) -> None:
    if environment not in SUPPORTED_ENVIRONMENTS:
        raise ValueError("environment must be azure-dev, azure-test, or azure-prod")


def run_gh_api(endpoint: str) -> dict[str, Any]:
    gh = shutil.which("gh")
    if gh is None:
        raise RuntimeError(
            "GitHub CLI is required to resolve the effective OIDC subject. "
            "Install gh and authenticate with `gh auth login`."
        )
    try:
        process = subprocess.run(
            [
                gh,
                "api",
                "--method",
                "GET",
                "--header",
                f"X-GitHub-Api-Version: {GITHUB_API_VERSION}",
                endpoint,
            ],
            check=False,
            capture_output=True,
            text=True,
            timeout=15,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise RuntimeError(
            f"GitHub API read could not complete for {endpoint}."
        ) from error
    if process.returncode != 0:
        detail = first_line(process.stderr or process.stdout)
        raise RuntimeError(f"GitHub API read failed for {endpoint}: {detail}")
    try:
        payload = json.loads(process.stdout)
    except json.JSONDecodeError as error:
        raise RuntimeError(
            f"GitHub API returned invalid JSON for {endpoint}."
        ) from error
    if not isinstance(payload, dict):
        raise RuntimeError(
            f"GitHub API returned an unexpected payload for {endpoint}."
        )
    return payload


def required_text(value: Any, field: str) -> str:
    text = str(value or "").strip()
    if not text:
        raise RuntimeError(
            f"GitHub did not return {field}; refusing to guess the OIDC subject."
        )
    return text


def resolve_github_oidc_trust(
    repository: str,
    environment: str,
    api_reader: GitHubApiReader | None = None,
) -> dict[str, Any]:
    validate_environment(environment)
    if repository.count("/") != 1:
        raise ValueError("repository must use owner/name format")

    reader = api_reader or run_gh_api
    repository_info = reader(f"repos/{repository}")
    oidc_policy = reader(f"repos/{repository}/actions/oidc/customization/sub")

    if oidc_policy.get("use_default") is not True:
        raise RuntimeError(
            f"Repository '{repository}' uses a customized OIDC subject template. "
            "HELIOS will not infer or overwrite it."
        )
    if "use_immutable_subject" not in oidc_policy:
        raise RuntimeError(
            "GitHub did not return use_immutable_subject; refusing to guess "
            "the OIDC subject format."
        )

    owner = repository_info.get("owner")
    if not isinstance(owner, dict):
        raise RuntimeError(
            "GitHub did not return canonical repository-owner metadata; "
            "refusing to guess the OIDC subject."
        )
    canonical_owner = required_text(owner.get("login"), "the canonical owner name")
    owner_id = required_text(owner.get("id"), "the immutable owner ID")
    canonical_repository = required_text(
        repository_info.get("name"),
        "the canonical repository name",
    )
    repository_id = required_text(
        repository_info.get("id"),
        "the immutable repository ID",
    )

    use_immutable_subject = bool(oidc_policy["use_immutable_subject"])
    if use_immutable_subject:
        repository_segment = (
            f"{canonical_owner}@{owner_id}/"
            f"{canonical_repository}@{repository_id}"
        )
    else:
        repository_segment = f"{canonical_owner}/{canonical_repository}"

    return {
        "subject": f"repo:{repository_segment}:environment:{environment}",
        "canonicalRepository": f"{canonical_owner}/{canonical_repository}",
        "repositoryId": repository_id,
        "ownerId": owner_id,
        "useImmutableSubject": use_immutable_subject,
        "policyEndpoint": (
            f"repos/{repository}/actions/oidc/customization/sub"
        ),
        "apiVersion": GITHUB_API_VERSION,
    }


def release_plan(environment: str) -> dict[str, Any]:
    validate_environment(environment)
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
            "python plugins/helios-control-fabric/scripts/helios.py all --environment "
            + environment
            + " --json",
            "GitHub Actions: HELIOS Azure → what-if",
            "GitHub protected environment: " + environment + " → deployment approval",
        ],
        "federationSubjectResolution": (
            "Run the authenticated `oidc` command; HELIOS resolves GitHub's "
            "effective default/immutable subject policy and fails closed."
        ),
    }


def oidc_contract(
    environment: str,
    api_reader: GitHubApiReader | None = None,
) -> dict[str, Any]:
    validate_environment(environment)
    contract = read_asset("oidc.json")
    resolution = resolve_github_oidc_trust(
        contract["repository"],
        environment,
        api_reader=api_reader,
    )
    return {
        **contract,
        "environment": environment,
        "selectedSubject": resolution["subject"],
        "resolvedRepository": resolution["canonicalRepository"],
        "repositoryId": resolution["repositoryId"],
        "ownerId": resolution["ownerId"],
        "useImmutableSubject": resolution["useImmutableSubject"],
        "resolutionEvidence": {
            "policyEndpoint": resolution["policyEndpoint"],
            "apiVersion": resolution["apiVersion"],
        },
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
    validate_environment(environment)
    return {
        **read_asset("edge-runtime.json"),
        "environment": environment,
        "executionMode": "plan-only",
    }


def full_setup(environment: str, skip_oidc: bool = False) -> dict[str, Any]:
    validate_environment(environment)
    doctor_result = doctor()
    required_tools_ready = bool(doctor_result.get("requiredToolsReady"))
    oidc_required = not skip_oidc
    oidc_ready = not oidc_required

    steps: dict[str, Any] = {
        "doctor": doctor_result,
        "targets": read_targets(),
        "plan": release_plan(environment),
        "edge": edge_plan(environment),
        "devopsSync": devops_sync_plan(),
        "runners": runner_plan(),
    }
    blocked: list[dict[str, str]] = []

    if oidc_required:
        try:
            steps["oidc"] = oidc_contract(environment)
            oidc_ready = True
        except RuntimeError as error:
            steps["oidc"] = {
                "executionMode": "blocked",
                "reason": str(error),
            }
            blocked.append({"step": "oidc", "reason": str(error)})
            oidc_ready = False
    else:
        steps["oidc"] = {
            "executionMode": "skipped",
            "reason": "Skipped by --skip-oidc.",
        }

    return {
        "mode": "read-only",
        "command": "all",
        "environment": environment,
        "ready": required_tools_ready and oidc_ready and not blocked,
        "requiredToolsReady": required_tools_ready,
        "oidcRequired": oidc_required,
        "oidcReady": oidc_ready,
        "blocked": blocked,
        "steps": steps,
    }


def print_human(payload: dict[str, Any]) -> None:
    if payload.get("command") == "all" and "steps" in payload:
        print(f"HELIOS full setup ({payload['environment']})")
        print(
            "  required tools: "
            + ("ready" if payload.get("requiredToolsReady") else "missing")
        )
        if payload.get("oidcRequired", False):
            print(
                "  oidc contract: "
                + ("ready" if payload.get("oidcReady") else "blocked")
            )
        else:
            print("  oidc contract: skipped (--skip-oidc)")
        print("  targets/plan/edge/devops-sync/runners: prepared")
        for blocked in payload.get("blocked", []):
            print(f"  blocked {blocked['step']}: {blocked['reason']}")
        print(
            "  overall ready: "
            + ("true" if payload.get("ready", False) else "false")
        )
        print("  No cloud mutation was performed.")
    elif "tools" in payload:
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
        print(f"  immutable subject: {str(payload['useImmutableSubject']).lower()}")
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
    all_parser = subparsers.add_parser("all", help="Run the full read-only setup sequence")
    add_environment_argument(all_parser)
    all_parser.add_argument(
        "--skip-oidc",
        action="store_true",
        help="Skip live GitHub OIDC subject resolution when gh authentication is unavailable",
    )
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
    try:
        if args.command == "all":
            payload = full_setup(args.environment, skip_oidc=args.skip_oidc)
        elif args.command == "doctor":
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
    except (RuntimeError, ValueError) as error:
        print(f"HELIOS: {error}", file=sys.stderr)
        return 2
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        print_human(payload)
    if args.command == "all" and not payload.get("ready", False):
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
