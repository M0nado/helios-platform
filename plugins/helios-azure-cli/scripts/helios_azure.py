#!/usr/bin/env python3
"""Guarded Azure CLI wizard for the HELIOS development environment."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from typing import Any, Sequence


PLUGIN_ROOT = Path(__file__).resolve().parent.parent
TARGETS_PATH = PLUGIN_ROOT / "assets" / "helios-targets.json"
OIDC_CONFIRMATION = "CONFIGURE-AZURE-DEV-OIDC"
DEPLOY_CONFIRMATION = "DEPLOY-AZURE-DEV"


class HeliosAzureError(RuntimeError):
    """A user-actionable, non-secret CLI error."""


def load_targets() -> dict[str, Any]:
    return json.loads(TARGETS_PATH.read_text(encoding="utf-8"))


def executable(name: str) -> str:
    resolved = shutil.which(name)
    if not resolved:
        raise HeliosAzureError(f"Required command is not installed: {name}")
    return resolved


def run(command: Sequence[str], *, capture: bool = True, check: bool = True) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(
        list(command),
        check=False,
        text=True,
        capture_output=capture,
        shell=False,
    )
    if check and result.returncode != 0:
        detail = (result.stderr or result.stdout or "command failed").strip()
        raise HeliosAzureError(f"{command[0]} command failed: {detail}")
    return result


def run_json(command: Sequence[str]) -> Any:
    result = run(command)
    try:
        return json.loads(result.stdout or "null")
    except json.JSONDecodeError as error:
        raise HeliosAzureError(f"Expected JSON from {command[0]}") from error


def az_account() -> dict[str, Any] | None:
    if not shutil.which("az"):
        return None
    result = run(
        ["az", "account", "show", "--query", "{id:id,name:name,tenantId:tenantId,userType:user.type}", "--output", "json"],
        check=False,
    )
    if result.returncode != 0:
        return None
    return json.loads(result.stdout)


def github_gate_status(targets: dict[str, Any]) -> list[dict[str, Any]]:
    repository = targets["github"]["repository"]
    statuses: list[dict[str, Any]] = []
    if not shutil.which("gh"):
        return [{"number": number, "merged": None, "reason": "gh is not installed"} for number in targets["github"]["requiredMergedPullRequests"]]
    auth = run(["gh", "auth", "status", "--hostname", "github.com"], check=False)
    if auth.returncode != 0:
        return [{"number": number, "merged": None, "reason": "gh is not authenticated"} for number in targets["github"]["requiredMergedPullRequests"]]
    for number in targets["github"]["requiredMergedPullRequests"]:
        result = run(
            ["gh", "pr", "view", str(number), "--repo", repository, "--json", "state,isDraft,mergedAt,url"],
            check=False,
        )
        if result.returncode != 0:
            statuses.append({"number": number, "merged": None, "reason": "PR status unavailable"})
            continue
        data = json.loads(result.stdout)
        statuses.append(
            {
                "number": number,
                "merged": bool(data.get("mergedAt")),
                "state": data.get("state"),
                "draft": data.get("isDraft"),
                "url": data.get("url"),
            }
        )
    return statuses


def require_gates(targets: dict[str, Any]) -> None:
    gates = github_gate_status(targets)
    if not gates or any(item.get("merged") is not True for item in gates):
        summary = ", ".join(f"#{item['number']}={item.get('merged')}" for item in gates)
        raise HeliosAzureError(f"Azure mutation is blocked until required GitHub PRs are merged ({summary}).")


def require_confirmation(execute: bool, supplied: str | None, expected: str) -> None:
    if not execute or supplied != expected:
        raise HeliosAzureError(f"Mutation blocked. Pass --execute --confirm {expected} only after explicit approval.")


def doctor(_args: argparse.Namespace, targets: dict[str, Any]) -> None:
    az_path = shutil.which("az")
    gh_path = shutil.which("gh")
    az_version = None
    if az_path:
        version_data = run_json(["az", "version"])
        az_version = version_data.get("azure-cli")
    account = az_account()
    required = targets["azure"]["requiredVariables"]
    report = {
        "azureCli": {"installed": bool(az_path), "version": az_version},
        "githubCli": {"installed": bool(gh_path)},
        "azureLogin": {"authenticated": account is not None, "account": account},
        "environmentVariables": {name: bool(os.getenv(name, "").strip()) for name in required},
        "deployEnabled": os.getenv("AZURE_DEPLOY_ENABLED", "false").lower() == "true",
        "githubGates": github_gate_status(targets),
        "canonicalSubject": targets["azure"]["federationSubject"],
        "linear": targets["linear"],
        "slack": targets["slack"],
    }
    print(json.dumps(report, indent=2))


def login(args: argparse.Namespace, _targets: dict[str, Any]) -> None:
    executable("az")
    command = ["az", "login", "--output", "none"]
    if args.device_code:
        command.insert(2, "--use-device-code")
    if args.tenant:
        command.extend(["--tenant", args.tenant])
    run(command, capture=False)
    account = az_account()
    if account is None:
        raise HeliosAzureError("Azure login completed but no active subscription is available.")
    print(json.dumps({"authenticated": True, "account": account}, indent=2))


def plan(args: argparse.Namespace, targets: dict[str, Any]) -> None:
    azure = targets["azure"]
    account = az_account()
    subscription = args.subscription or os.getenv("AZURE_SUBSCRIPTION_ID") or (account or {}).get("id")
    tenant = args.tenant or os.getenv("AZURE_TENANT_ID") or (account or {}).get("tenantId")
    resource_group = args.resource_group or os.getenv("AZURE_RESOURCE_GROUP") or azure["defaultResourceGroup"]
    report = {
        "mode": "plan-only",
        "willMutateAzure": False,
        "repository": targets["github"]["repository"],
        "environment": azure["environment"],
        "subscriptionId": subscription,
        "tenantId": tenant,
        "resourceGroup": resource_group,
        "location": args.location or azure["defaultLocation"],
        "federation": {
            "issuer": azure["issuer"],
            "subject": azure["federationSubject"],
            "audience": azure["audience"],
        },
        "githubGates": github_gate_status(targets),
        "mutatingConfirmations": {
            "configureOidc": OIDC_CONFIRMATION,
            "deploy": DEPLOY_CONFIRMATION,
        },
        "coordination": {"linear": targets["linear"], "slack": targets["slack"]},
    }
    print(json.dumps(report, indent=2))


def what_if(args: argparse.Namespace, targets: dict[str, Any]) -> None:
    executable("az")
    if az_account() is None:
        raise HeliosAzureError("Azure CLI is not authenticated. Run the login command first.")
    template = Path(args.template).expanduser().resolve()
    if not template.is_file():
        raise HeliosAzureError(f"Bicep template not found: {template}")
    resource_group = args.resource_group or os.getenv("AZURE_RESOURCE_GROUP") or targets["azure"]["defaultResourceGroup"]
    run(["az", "bicep", "build", "--file", str(template), "--stdout"], check=True)
    command = [
        "az", "deployment", "group", "what-if",
        "--resource-group", resource_group,
        "--template-file", str(template),
    ]
    if args.parameters:
        parameters = Path(args.parameters).expanduser().resolve()
        if not parameters.is_file():
            raise HeliosAzureError(f"Parameters file not found: {parameters}")
        command.extend(["--parameters", f"@{parameters}"])
    run(command, capture=False)


def configure_oidc(args: argparse.Namespace, targets: dict[str, Any]) -> None:
    require_confirmation(args.execute, args.confirm, OIDC_CONFIRMATION)
    require_gates(targets)
    executable("az")
    account = az_account()
    if account is None:
        raise HeliosAzureError("Azure CLI is not authenticated. Run the login command first.")
    azure = targets["azure"]
    app_name = args.app_name or azure["appDisplayName"]
    apps = run_json([
        "az", "ad", "app", "list", "--display-name", app_name,
        "--query", "[0].{appId:appId,id:id}", "--output", "json",
    ])
    app = apps
    if not app:
        app = run_json([
            "az", "ad", "app", "create", "--display-name", app_name,
            "--query", "{appId:appId,id:id}", "--output", "json",
        ])
    client_id = app["appId"]
    app_object_id = app["id"]
    principals = run_json([
        "az", "ad", "sp", "list", "--filter", f"appId eq '{client_id}'",
        "--query", "[0].{id:id,appId:appId}", "--output", "json",
    ])
    if not principals:
        principals = run_json([
            "az", "ad", "sp", "create", "--id", client_id,
            "--query", "{id:id,appId:appId}", "--output", "json",
        ])
    credential_name = "github-azure-dev"
    existing = run_json([
        "az", "ad", "app", "federated-credential", "list", "--id", app_object_id,
        "--query", f"[?name=='{credential_name}'] | [0]", "--output", "json",
    ])
    if not existing:
        payload = {
            "name": credential_name,
            "issuer": azure["issuer"],
            "subject": azure["federationSubject"],
            "audiences": [azure["audience"]],
            "description": "Canonical HELIOS GitHub Actions azure-dev environment",
        }
        with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".json", delete=False) as handle:
            json.dump(payload, handle)
            payload_path = Path(handle.name)
        try:
            run([
                "az", "ad", "app", "federated-credential", "create", "--id", app_object_id,
                "--parameters", f"@{payload_path}", "--output", "none",
            ])
        finally:
            payload_path.unlink(missing_ok=True)
    print(json.dumps({
        "configured": True,
        "clientId": client_id,
        "tenantId": account["tenantId"],
        "subscriptionId": account["id"],
        "servicePrincipalObjectId": principals["id"],
        "federationSubject": azure["federationSubject"],
        "rbac": "Not assigned automatically; apply the reviewed least-privilege role separately.",
    }, indent=2))


def deploy(args: argparse.Namespace, targets: dict[str, Any]) -> None:
    require_confirmation(args.execute, args.confirm, DEPLOY_CONFIRMATION)
    require_gates(targets)
    if os.getenv("AZURE_DEPLOY_ENABLED", "false").lower() != "true":
        raise HeliosAzureError("Deployment is disabled. AZURE_DEPLOY_ENABLED must be explicitly set to true after approval.")
    executable("az")
    if az_account() is None:
        raise HeliosAzureError("Azure CLI is not authenticated. Run the login command first.")
    template = Path(args.template).expanduser().resolve()
    if not template.is_file():
        raise HeliosAzureError(f"Bicep template not found: {template}")
    resource_group = args.resource_group or os.getenv("AZURE_RESOURCE_GROUP") or targets["azure"]["defaultResourceGroup"]
    if args.create_resource_group:
        run(["az", "group", "create", "--name", resource_group, "--location", args.location or targets["azure"]["defaultLocation"], "--output", "none"])
    command = [
        "az", "deployment", "group", "create",
        "--resource-group", resource_group,
        "--template-file", str(template),
        "--confirm-with-what-if",
    ]
    if args.parameters:
        parameters = Path(args.parameters).expanduser().resolve()
        if not parameters.is_file():
            raise HeliosAzureError(f"Parameters file not found: {parameters}")
        command.extend(["--parameters", f"@{parameters}"])
    run(command, capture=False)


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description="Safe HELIOS Azure CLI wizard")
    subparsers = root.add_subparsers(dest="command", required=True)

    subparsers.add_parser("doctor", help="Check Azure CLI, GitHub CLI, login, variables, gates, and canonical targets")

    login_parser = subparsers.add_parser("login", help="Authenticate interactively with Azure CLI")
    login_parser.add_argument("--device-code", action="store_true")
    login_parser.add_argument("--tenant")

    plan_parser = subparsers.add_parser("plan", help="Print the canonical, non-mutating HELIOS Azure plan")
    plan_parser.add_argument("--subscription")
    plan_parser.add_argument("--tenant")
    plan_parser.add_argument("--resource-group")
    plan_parser.add_argument("--location")

    what_if_parser = subparsers.add_parser("what-if", help="Compile Bicep and preview Azure changes")
    what_if_parser.add_argument("--template", required=True)
    what_if_parser.add_argument("--parameters")
    what_if_parser.add_argument("--resource-group")

    oidc_parser = subparsers.add_parser("configure-oidc", help="Create the secretless Entra app and exact GitHub federated credential")
    oidc_parser.add_argument("--app-name")
    oidc_parser.add_argument("--execute", action="store_true")
    oidc_parser.add_argument("--confirm")

    deploy_parser = subparsers.add_parser("deploy", help="Deploy Bicep only after all gates and a second what-if confirmation")
    deploy_parser.add_argument("--template", required=True)
    deploy_parser.add_argument("--parameters")
    deploy_parser.add_argument("--resource-group")
    deploy_parser.add_argument("--location")
    deploy_parser.add_argument("--create-resource-group", action="store_true")
    deploy_parser.add_argument("--execute", action="store_true")
    deploy_parser.add_argument("--confirm")
    return root


def main(argv: Sequence[str] | None = None) -> int:
    args = parser().parse_args(argv)
    targets = load_targets()
    handlers = {
        "doctor": doctor,
        "login": login,
        "plan": plan,
        "what-if": what_if,
        "configure-oidc": configure_oidc,
        "deploy": deploy,
    }
    try:
        handlers[args.command](args, targets)
        return 0
    except HeliosAzureError as error:
        print(f"HELIOS Azure: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
