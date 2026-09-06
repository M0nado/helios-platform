#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import urllib.error
import urllib.request
from pathlib import Path

WORKFLOWS = {
    "connector-readiness": ("connector-readiness.yml", {"environment", "source_sha"}),
    "azure-plan": ("azure-plan.yml", {"environment", "source_sha", "deploy_workloads", "broker_image", "worker_image"}),
    "azure-deploy": ("azure-deploy.yml", {"environment", "source_sha", "plan_run_id", "plan_sha256", "apply", "confirmation"}),
    "sharepoint-plan": ("sharepoint-governance-sync.yml", {"environment", "source_sha", "apply", "confirmation"}),
    "sharepoint-apply": ("sharepoint-governance-sync.yml", {"environment", "source_sha", "apply", "confirmation"}),
    "emergency-quarantine": ("emergency-quarantine.yml", {"connector_id", "reason", "confirmation"}),
    "release-evidence": ("release-evidence.yml", {"source_sha"}),
}
REPOSITORY = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")


def main() -> int:
    parser = argparse.ArgumentParser(description="Plan or dispatch one allowlisted HELIOS control-plane workflow.")
    parser.add_argument("--operation", required=True, choices=sorted(WORKFLOWS))
    parser.add_argument("--repository", required=True)
    parser.add_argument("--ref", required=True)
    parser.add_argument("--arguments", default="{}")
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--confirm", default="")
    args = parser.parse_args()
    if not REPOSITORY.fullmatch(args.repository):
        raise SystemExit("invalid repository")
    arguments = json.loads(args.arguments)
    if not isinstance(arguments, dict):
        raise SystemExit("--arguments must be a JSON object")
    workflow, allowed = WORKFLOWS[args.operation]
    unknown = sorted(set(arguments) - allowed)
    if unknown:
        raise SystemExit(f"unsupported inputs for {args.operation}: {', '.join(unknown)}")
    if args.operation == "sharepoint-plan":
        arguments.update({"apply": False, "confirmation": ""})
    if args.operation == "sharepoint-apply":
        arguments["apply"] = True
    plan = {
        "mode": "apply" if args.apply else "preview",
        "repository": args.repository,
        "ref": args.ref,
        "operation": args.operation,
        "workflow": workflow,
        "inputs": arguments,
    }
    canonical = json.dumps(plan, sort_keys=True, separators=(",", ":")).encode()
    plan["requestSha256"] = hashlib.sha256(canonical).hexdigest()
    if args.apply:
        if args.confirm != "DISPATCH HELIOS CONTROL PLANE":
            raise SystemExit("--apply requires --confirm 'DISPATCH HELIOS CONTROL PLANE'")
        token = os.getenv("GITHUB_TOKEN")
        if not token:
            raise SystemExit("GITHUB_TOKEN is required")
        url = f"https://api.github.com/repos/{args.repository}/actions/workflows/{workflow}/dispatches"
        request = urllib.request.Request(
            url,
            data=json.dumps({"ref": args.ref, "inputs": arguments}).encode(),
            method="POST",
            headers={
                "Authorization": f"Bearer {token}",
                "Accept": "application/vnd.github+json",
                "X-GitHub-Api-Version": "2022-11-28",
                "User-Agent": "helios-control-plane/2.0",
            },
        )
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                if response.status != 204:
                    raise RuntimeError(f"unexpected GitHub status {response.status}")
        except urllib.error.HTTPError as exc:
            details = exc.read().decode("utf-8", errors="replace")[:1000]
            raise SystemExit(f"GitHub dispatch failed ({exc.code}): {details}") from exc
        plan["dispatched"] = True
    else:
        plan["dispatched"] = False
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(plan, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(plan, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

