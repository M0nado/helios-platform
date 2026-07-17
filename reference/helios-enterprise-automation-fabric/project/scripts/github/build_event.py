#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def payload_value(value: str) -> dict[str, Any]:
    raw = Path(value[1:]).read_text(encoding="utf-8") if value.startswith("@") else value
    result = json.loads(raw)
    if not isinstance(result, dict):
        raise SystemExit("--payload must be a JSON object")
    return result


def without_none(value: dict[str, Any]) -> dict[str, Any]:
    return {key: item for key, item in value.items() if item is not None}


def canonical_projection(event: dict[str, Any]) -> dict[str, Any]:
    occurred = datetime.fromisoformat(event["occurredAt"].replace("Z", "+00:00"))
    projection: dict[str, Any] = {
        "specVersion": event["specVersion"],
        "eventId": event["eventId"],
        "correlationId": event["correlationId"],
        "eventType": event["eventType"],
        "source": event["source"],
        "subject": event["subject"],
        "occurredAtUnixMs": int(occurred.timestamp() * 1000),
        "environment": event["environment"],
        "severity": event["severity"],
        "sensitivity": event["sensitivity"],
        "summary": event["summary"],
        "provenance": without_none({
            key: event["provenance"].get(key)
            for key in ("repository", "commitSha", "ref", "workflow", "workflowRunId", "actor", "idempotencyKey")
        }),
        "payload": event["payload"],
        "requestedActions": event["requestedActions"],
        "links": event["links"],
    }
    if event.get("approval") is not None:
        projection["approval"] = event["approval"]
    return projection


def content_hash(event: dict[str, Any]) -> str:
    encoded = json.dumps(canonical_projection(event), sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description="Build a canonical, checksum-bound HELIOS event.")
    parser.add_argument("--event-type", required=True)
    parser.add_argument("--summary", required=True)
    parser.add_argument("--environment", required=True, choices=["local", "dev", "test", "stage", "prod", "global"])
    parser.add_argument("--severity", required=True, choices=["debug", "info", "notice", "warning", "error", "critical"])
    parser.add_argument("--actions", default="none")
    parser.add_argument("--payload", default="{}")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()

    payload = payload_value(args.payload)
    repository = os.getenv("GITHUB_REPOSITORY")
    commit_sha = os.getenv("GITHUB_SHA")
    run_id = os.getenv("GITHUB_RUN_ID")
    idempotency = f"github:{repository or 'local'}:{run_id or 'offline'}:{args.event_type}:{commit_sha or 'no-sha'}"
    event_id = str(uuid.uuid5(uuid.NAMESPACE_URL, idempotency))
    occurred = datetime.now(timezone.utc).isoformat(timespec="milliseconds")
    actions = list(dict.fromkeys(item.strip() for item in args.actions.split(",") if item.strip()))
    event: dict[str, Any] = {
        "specVersion": "1.0",
        "eventId": event_id,
        "correlationId": str(uuid.uuid5(uuid.NAMESPACE_URL, f"correlation:{idempotency}")),
        "eventType": args.event_type.lower(),
        "source": "github" if repository else "operator",
        "subject": repository or "offline HELIOS event",
        "occurredAt": occurred,
        "environment": args.environment,
        "severity": args.severity,
        "sensitivity": "confidential",
        "summary": args.summary,
        "provenance": {
            "repository": repository,
            "commitSha": commit_sha.lower() if commit_sha else None,
            "ref": os.getenv("GITHUB_REF"),
            "workflow": os.getenv("GITHUB_WORKFLOW"),
            "workflowRunId": run_id,
            "actor": os.getenv("GITHUB_ACTOR", "fabricctl"),
            "idempotencyKey": idempotency,
            "contentSha256": "",
        },
        "payload": payload,
        "requestedActions": actions or ["none"],
        "links": [],
    }
    event["provenance"]["contentSha256"] = content_hash(event)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(event, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output), "eventId": event_id, "contentSha256": event["provenance"]["contentSha256"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

