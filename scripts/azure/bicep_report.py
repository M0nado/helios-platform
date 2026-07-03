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


def run(cmd: list[str]) -> int:
    print("+ " + " ".join(cmd), flush=True)
    return subprocess.run(cmd, cwd=ROOT).returncode


def require_az() -> None:
    if shutil.which("az") is None:
        print("Azure CLI is required. Install it with scripts/setup/bootstrap-local-tools.sh or use the ubuntu-latest GitHub runner with azure/login.", file=sys.stderr)
        sys.exit(127)


def resource_group() -> str:
    rg = os.environ.get("HELIOS_AZURE_RESOURCE_GROUP") or os.environ.get("AZURE_RESOURCE_GROUP")
    if not rg:
        print("Set HELIOS_AZURE_RESOURCE_GROUP or AZURE_RESOURCE_GROUP for deployment validation/what-if.", file=sys.stderr)
        sys.exit(2)
    return rg


def location() -> str:
    return os.environ.get("HELIOS_AZURE_LOCATION") or os.environ.get("AZURE_LOCATION") or "eastus"


def emit_summary(action: str, exit_code: int) -> None:
    summary = {
        "action": action,
        "template": str(TEMPLATE.relative_to(ROOT)),
        "parameters": str(PARAMETERS.relative_to(ROOT)),
        "resourceGroup": os.environ.get("HELIOS_AZURE_RESOURCE_GROUP") or os.environ.get("AZURE_RESOURCE_GROUP"),
        "location": location(),
        "exitCode": exit_code,
    }
    print(json.dumps(summary, indent=2, sort_keys=True))


def main() -> int:
    parser = argparse.ArgumentParser(description="Run HELIOS Azure Bicep validation commands.")
    parser.add_argument("action", choices=("build", "validate", "what-if"))
    args = parser.parse_args()

    require_az()
    if not TEMPLATE.exists():
        print(f"Missing template: {TEMPLATE}", file=sys.stderr)
        return 1

    if args.action == "build":
        code = run(["az", "bicep", "build", "--file", str(TEMPLATE)])
    else:
        command = [
            "az", "deployment", "group", args.action,
            "--resource-group", resource_group(),
            "--template-file", str(TEMPLATE),
            "--parameters", str(PARAMETERS), f"location={location()}",
        ]
        code = run(command)

    emit_summary(args.action, code)
    return code


if __name__ == "__main__":
    sys.exit(main())
