#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

CHANNELS = ["helios-control-plane", "helios-ops", "helios-incidents", "helios-releases"]


def call(token: str, method: str, payload: dict | None = None) -> dict:
    data = None if payload is None else json.dumps(payload).encode()
    request = urllib.request.Request(
        f"https://slack.com/api/{method}",
        data=data,
        method="POST",
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json; charset=utf-8"},
    )
    with urllib.request.urlopen(request, timeout=20) as response:
        result = json.loads(response.read())
    if not result.get("ok"):
        raise RuntimeError(f"Slack {method} failed: {result.get('error', 'unknown')}")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Idempotently create HELIOS Slack channels using a separately approved bootstrap token.")
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--confirmation", default="")
    parser.add_argument("--output", default="config/slack/runtime-bindings.local.json")
    args = parser.parse_args()
    if not args.apply:
        print(json.dumps({"mode": "preview", "channels": CHANNELS, "requiredEnvironment": "SLACK_BOOTSTRAP_TOKEN"}, indent=2))
        return 0
    if args.confirmation != "CREATE HELIOS SLACK CHANNELS":
        raise SystemExit("--confirmation 'CREATE HELIOS SLACK CHANNELS' is required")
    token = os.getenv("SLACK_BOOTSTRAP_TOKEN")
    if not token:
        raise SystemExit("SLACK_BOOTSTRAP_TOKEN is required")
    auth = call(token, "auth.test")
    existing: dict[str, str] = {}
    cursor = ""
    while True:
        query = urllib.parse.urlencode({"limit": 200, "cursor": cursor, "exclude_archived": "true"})
        request = urllib.request.Request(f"https://slack.com/api/conversations.list?{query}", headers={"Authorization": f"Bearer {token}"})
        with urllib.request.urlopen(request, timeout=20) as response:
            page = json.loads(response.read())
        if not page.get("ok"): raise RuntimeError(page.get("error"))
        existing.update({item["name"]: item["id"] for item in page.get("channels", [])})
        cursor = page.get("response_metadata", {}).get("next_cursor", "")
        if not cursor: break
    bindings = {}
    for name in CHANNELS:
        channel_id = existing.get(name)
        if not channel_id:
            channel_id = call(token, "conversations.create", {"name": name, "is_private": False})["channel"]["id"]
        bindings[name] = channel_id
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps({"workspace": auth.get("team"), "workspaceId": auth.get("team_id"), "channels": bindings}, indent=2) + "\n")
    print(json.dumps({"createdOrResolved": len(bindings), "output": str(output)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
