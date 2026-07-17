#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import urllib.request
from pathlib import Path

ENDPOINT = "https://api.linear.app/graphql"


def graphql(token: str, query: str, variables: dict | None = None) -> dict:
    request = urllib.request.Request(
        ENDPOINT,
        data=json.dumps({"query": query, "variables": variables or {}}).encode(),
        method="POST",
        headers={"Authorization": token, "Content-Type": "application/json", "User-Agent": "helios-linear-bootstrap/2.0"},
    )
    with urllib.request.urlopen(request, timeout=20) as response:
        result = json.loads(response.read())
    if result.get("errors"):
        raise RuntimeError(json.dumps(result["errors"]))
    return result["data"]


def main() -> int:
    parser = argparse.ArgumentParser(description="Resolve HELIOS Linear IDs and optionally register the signed webhook.")
    parser.add_argument("--project-name", default="Helios Integration Fabric")
    parser.add_argument("--webhook-url", default=os.getenv("HELIOS_LINEAR_WEBHOOK_URL"))
    parser.add_argument("--apply-webhook", action="store_true")
    parser.add_argument("--confirmation", default="")
    parser.add_argument("--output", default="config/linear/runtime-map.local.json")
    args = parser.parse_args()
    token = os.getenv("LINEAR_API_KEY")
    if not token:
        print(json.dumps({"mode": "preview", "project": args.project_name, "webhookConfigured": bool(args.webhook_url), "requiredEnvironment": "LINEAR_API_KEY"}, indent=2))
        return 0
    data = graphql(token, "query HeliosDiscover { viewer { id name } teams { nodes { id key name } } projects(first: 100) { nodes { id name state } } }")
    project = next((item for item in data["projects"]["nodes"] if item["name"] == args.project_name), None)
    if project is None:
        raise SystemExit(f"Linear project '{args.project_name}' was not found; create or rename it before enabling synchronization.")
    output_data = {"viewer": data["viewer"], "teams": data["teams"]["nodes"], "project": project}
    if args.apply_webhook:
        if args.confirmation != "CREATE HELIOS LINEAR WEBHOOK":
            raise SystemExit("--confirmation 'CREATE HELIOS LINEAR WEBHOOK' is required")
        if not args.webhook_url:
            raise SystemExit("--webhook-url or HELIOS_LINEAR_WEBHOOK_URL is required")
        team_id = data["teams"]["nodes"][0]["id"]
        mutation = "mutation CreateHeliosWebhook($input: WebhookCreateInput!) { webhookCreate(input: $input) { success webhook { id url enabled resourceTypes } } }"
        created = graphql(token, mutation, {"input": {"url": args.webhook_url, "teamId": team_id, "resourceTypes": ["Issue", "Project"], "allPublicTeams": False}})
        output_data["webhook"] = created["webhookCreate"]
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(output_data, indent=2) + "\n")
    print(json.dumps({"projectId": project["id"], "teams": len(output_data["teams"]), "output": str(output)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
