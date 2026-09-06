#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Any
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / "config" / "control" / "unified-control.v3.json"
OPERATIONS = ROOT / "config" / "control" / "operations.v3.json"
CURRENT_JSON = ROOT / "control" / "current.json"
CURRENT_MD = ROOT / "control" / "CURRENT.md"
EVENTS = ROOT / "control" / "events.jsonl"
ARTIFACTS = ROOT / "artifacts" / "control"


def load(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def now() -> str:
    return datetime.now(timezone.utc).isoformat()


def git_sha() -> str:
    for name in ("HELIOS_SOURCE_SHA", "GITHUB_HEAD_SHA", "GITHUB_SHA"):
        if os.getenv(name):
            return os.environ[name]
    try:
        return subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=ROOT,
            check=True,
            text=True,
            capture_output=True,
        ).stdout.strip()
    except Exception:
        return "unavailable"


def stable_key(payload: dict[str, Any]) -> str:
    canonical = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()
    return hashlib.sha256(canonical).hexdigest()


def build_record(stage: str, status: str, summary: str, next_action: str) -> dict[str, Any]:
    previous = load(CURRENT_JSON)
    correlation = previous.get("correlationId")
    if not correlation or correlation == "uninitialized":
        correlation = str(uuid4())
    return {
        "schemaVersion": "3.0",
        "correlationId": correlation,
        "runId": str(uuid4()),
        "sourceSha": git_sha(),
        "stage": stage,
        "status": status,
        "approvalState": "none",
        "productionEnabled": False,
        "summary": summary,
        "nextAction": next_action,
        "links": previous.get("links", {}),
        "updatedUtc": now(),
    }


def render(record: dict[str, Any]) -> str:
    links = "\n".join(f"- **{k}:** {v}" for k, v in sorted(record.get("links", {}).items()))
    return f"""# HELIOS Control Plane — Current Authority

```text
correlationId:  {record['correlationId']}
runId:          {record['runId']}
sourceSha:      {record['sourceSha']}
stage:          {record['stage']}
status:         {record['status']}
approvalState:  {record['approvalState']}
production:     {str(record['productionEnabled']).lower()}
updatedUtc:     {record['updatedUtc']}
```

## Summary

{record['summary']}

## Next action

{record['nextAction']}

## Links

{links or '- No live links recorded.'}

## Authority boundary

GitHub remains source and deployment authority. External surfaces mirror this record through the
existing HELIOS Fabric. Tenant and machine activation require separate approvals.
"""


def persist(record: dict[str, Any]) -> None:
    CURRENT_JSON.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    CURRENT_MD.write_text(render(record), encoding="utf-8")
    EVENTS.parent.mkdir(parents=True, exist_ok=True)
    with EVENTS.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(record, sort_keys=True, separators=(",", ":")) + "\n")


def build_event(record: dict[str, Any]) -> dict[str, Any]:
    event = {
        "schemaVersion": "1.0",
        "eventId": str(uuid4()),
        "correlationId": record["correlationId"],
        "causationId": record["runId"],
        "source": "github-unified-control-v3",
        "eventType": "helios.control.current.updated",
        "classification": "internal",
        "occurredAt": record["updatedUtc"],
        "sourceSha": record["sourceSha"],
        "stage": record["stage"],
        "status": record["status"],
        "approvalState": record["approvalState"],
        "productionEnabled": False,
        "redacted": True,
        "nextAction": record["nextAction"],
        "evidence": ["control/current.json", "control/CURRENT.md"],
    }
    event["idempotencyKey"] = stable_key({
        "schemaVersion": event["schemaVersion"],
        "correlationId": event["correlationId"],
        "causationId": event["causationId"],
        "eventType": event["eventType"],
        "sourceSha": event["sourceSha"],
        "stage": event["stage"],
    })
    return event


def validate() -> dict[str, Any]:
    errors: list[str] = []
    required = [
        CONFIG,
        OPERATIONS,
        CURRENT_JSON,
        CURRENT_MD,
        EVENTS,
        ROOT / "monado/helios-control/config/unified-control-v3.json",
        ROOT / "monado/helios-control/config/agent-core-policy.json",
        ROOT / "monado/helios-control/config/integrations.json",
        ROOT / "monado/helios-control/Helios.Connect.sln",
    ]
    for path in required:
        if not path.exists():
            errors.append(f"missing: {path.relative_to(ROOT)}")
    for path in [
        CONFIG,
        OPERATIONS,
        CURRENT_JSON,
        ROOT / "monado/helios-control/config/unified-control-v3.json",
        ROOT / "automation/control-plane/surface-contracts.v3.json",
        ROOT / "automation/openai/helios-control-tools.v3.json",
    ]:
        try:
            load(path)
        except Exception as exc:
            errors.append(f"invalid JSON {path.relative_to(ROOT)}: {exc}")
    if not errors:
        cfg = load(CONFIG)
        if cfg.get("defaultMode") != "plan-only":
            errors.append("defaultMode must be plan-only")
        if cfg.get("relay", {}).get("directProviderTokensAllowed") is not False:
            errors.append("direct provider tokens must be disabled")
        ops = load(OPERATIONS).get("operations", {})
        if any(v.get("deployment") is True for v in ops.values() if isinstance(v, dict)):
            errors.append("unified workflow may not contain a deployment operation")
        tools = load(ROOT / "automation/openai/helios-control-tools.v3.json").get("tools", [])
        if any(not t.get("readOnlyHint") for t in tools):
            errors.append("OpenAI/ALVIS tools in this layer must remain read-only")
    result = {"ok": not errors, "errors": errors, "checkedUtc": now()}
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    (ARTIFACTS / "unified-control-validation.json").write_text(
        json.dumps(result, indent=2) + "\n",
        encoding="utf-8",
    )
    return result


def plan_sync(record: dict[str, Any], bindings_path: Path) -> dict[str, Any]:
    bindings = load(bindings_path)
    surfaces = []
    for name in ("github", "slack", "linear", "sharePoint", "azureDevOps", "openAI"):
        item = bindings.get(name, {})
        surfaces.append({
            "surface": name,
            "enabled": bool(item.get("enabled")),
            "action": "update-existing-permanent-object" if item.get("enabled") else "disabled",
            "correlationId": record["correlationId"],
        })
    plan = {
        "schemaVersion": "3.0",
        "record": record,
        "surfaces": surfaces,
        "externalWrites": 0,
    }
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    (ARTIFACTS / "sync-plan.json").write_text(json.dumps(plan, indent=2) + "\n", encoding="utf-8")
    return plan


def azure_plan_request(record: dict[str, Any]) -> dict[str, Any]:
    request = {
        "schemaVersion": "3.0",
        "operation": "azure-development-what-if-request",
        "sourceSha": record["sourceSha"],
        "correlationId": record["correlationId"],
        "targetWorkflow": ".github/workflows/helios-cloud-deploy.yml",
        "targetEnvironment": "azure-dev",
        "deploymentRequested": False,
        "externalWrites": 0,
        "nextAction": "Review and dispatch the existing protected Azure workflow through its own approval boundary.",
    }
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    (ARTIFACTS / "azure-plan-request.json").write_text(
        json.dumps(request, indent=2) + "\n",
        encoding="utf-8",
    )
    return request


def main() -> int:
    parser = argparse.ArgumentParser(prog="helios-unified-control")
    parser.add_argument(
        "command",
        choices=[
            "status",
            "validate",
            "plan-sync",
            "emit-event",
            "connector-readiness",
            "azure-plan-request",
        ],
    )
    parser.add_argument("--bindings", default="config/control/surface-bindings.example.json")
    args = parser.parse_args()

    if args.command == "validate":
        result = validate()
        print(json.dumps(result, indent=2))
        return 0 if result["ok"] else 1

    record = build_record(
        stage=args.command,
        status="ready",
        summary="HELIOS Unified Control v3 generated one correlation-bound authority record for the existing control plane and Fabric.",
        next_action="Review evidence, then activate one connector through the existing signed relay and protected approval path.",
    )
    persist(record)

    if args.command == "status":
        print(render(record))
    elif args.command == "emit-event":
        event = build_event(record)
        ARTIFACTS.mkdir(parents=True, exist_ok=True)
        (ARTIFACTS / "relay-envelope.json").write_text(
            json.dumps(event, indent=2) + "\n",
            encoding="utf-8",
        )
        print(json.dumps(event, indent=2))
    elif args.command in {"plan-sync", "connector-readiness"}:
        print(json.dumps(plan_sync(record, ROOT / args.bindings), indent=2))
    elif args.command == "azure-plan-request":
        print(json.dumps(azure_plan_request(record), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
