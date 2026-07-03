#!/usr/bin/env python3
"""Azure Bicep validation helper for local and CI runs."""
from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TEMPLATE = ROOT / "infra" / "azure" / "main.bicep"
PARAMETERS = ROOT / "infra" / "azure" / "parameters" / "dev.json"


def run(cmd: list[str], *, check: bool = False, quiet: bool = False) -> subprocess.CompletedProcess[str]:
    if not quiet:
        print("+ " + " ".join(cmd), flush=True)
    return subprocess.run(
        cmd,
        cwd=ROOT,
        check=check,
        text=True,
        stdout=subprocess.PIPE if quiet else None,
        stderr=subprocess.PIPE if quiet else None,
    )


def require_az() -> None:
    if shutil.which("az") is None:
        print(
            "Azure CLI is required. Install it with scripts/setup/bootstrap-local-tools.sh "
            "or use the ubuntu-latest GitHub runner with azure/login.",
            file=sys.stderr,
        )
        sys.exit(127)


def configured_resource_group() -> str | None:
    return os.environ.get("HELIOS_AZURE_RESOURCE_GROUP") or os.environ.get("AZURE_RESOURCE_GROUP")


def is_authenticated() -> bool:
    account = run(["az", "account", "show", "--only-show-errors"], quiet=True)
    return account.returncode == 0


def location() -> str:
    return os.environ.get("HELIOS_AZURE_LOCATION") or os.environ.get("AZURE_LOCATION") or "eastus"


def emit_summary(action: str, exit_code: int, *, mode: str, message: str | None = None) -> None:
    summary = {
        "action": action,
        "exitCode": exit_code,
        "location": location(),
        "mode": mode,
        "parameters": str(PARAMETERS.relative_to(ROOT)),
        "resourceGroup": configured_resource_group(),
        "template": str(TEMPLATE.relative_to(ROOT)),
    }
    if message:
        summary["message"] = message
    print(json.dumps(summary, indent=2, sort_keys=True))


def build_template(action: str) -> int:
    code = run(["az", "bicep", "build", "--file", str(TEMPLATE)]).returncode
    emit_summary(action, code, mode="bicep-build")
    return code


def deployment_command(action: str, resource_group: str) -> list[str]:
    return [
        "az", "deployment", "group", action,
        "--resource-group", resource_group,
        "--template-file", str(TEMPLATE),
        "--parameters", str(PARAMETERS), f"location={location()}",
    ]


def main() -> int:
    parser = argparse.ArgumentParser(description="Run HELIOS Azure Bicep validation commands.")
    parser.add_argument("action", choices=("build", "validate", "what-if"))
    parser.add_argument(
        "--strict-online",
        action="store_true",
        help="Fail validate/what-if when Azure authentication or a resource group is unavailable.",
    )
    args = parser.parse_args()

    require_az()
    if not TEMPLATE.exists():
        print(f"Missing template: {TEMPLATE}", file=sys.stderr)
        return 1

    if args.action == "build":
        return build_template(args.action)

    resource_group = configured_resource_group()
    authenticated = is_authenticated()
    if not resource_group or not authenticated:
        missing = []
        if not resource_group:
            missing.append("HELIOS_AZURE_RESOURCE_GROUP or AZURE_RESOURCE_GROUP")
        if not authenticated:
            missing.append("Azure authentication")
        message = "Online deployment validation skipped; missing " + " and ".join(missing) + ". Offline Bicep build was run instead."
        if args.strict_online:
            print(message, file=sys.stderr)
            emit_summary(args.action, 2, mode="online-required", message=message)
            return 2
        code = run(["az", "bicep", "build", "--file", str(TEMPLATE)]).returncode
        emit_summary(args.action, code, mode="offline-bicep-build", message=message)
        return code

    code = run(deployment_command(args.action, resource_group)).returncode
    emit_summary(args.action, code, mode="online-deployment")
    return code


if __name__ == "__main__":
    sys.exit(main())
