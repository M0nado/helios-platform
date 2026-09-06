#!/usr/bin/env python3
"""Validate authentication, fleet, CI parity, and protected-delivery contracts."""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

DEPLOYMENT_UNBLOCK_SEQUENCE = [
    "exact-sha-build-and-tests",
    "reviewed-current-infrastructure-plan",
    "inspected-and-attested-immutable-artifact",
    "protected-least-privilege-identity",
    "independent-exact-sha-approval",
    "protected-merge",
    "immediate-preflight-revalidation",
    "digest-bound-deployment",
    "provider-observed-verification",
    "proven-rollback",
    "revocation-and-cleanup",
    "zero-blocker-terminal-proof",
]

DEPLOYMENT_INVALIDATION_INPUTS = {
    "source-sha",
    "infrastructure-plan",
    "artifact-digest",
    "identity",
    "approval",
    "target-environment",
    "policy",
    "capability",
    "rollback-state",
}


def load(path: str) -> dict:
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def main() -> None:
    identity = load("config/integrations/identity-profile.json")
    fleet = load("config/integrations/fleet-agents.json")
    policy = load("config/integrations/delivery-policy.json")
    event_schema = load("config/integrations/event-contract.schema.json")
    workflow = (ROOT / ".github/workflows/reusable-azure-auth.yml").read_text(encoding="utf-8")
    pipeline = (ROOT / "azure-pipelines.yml").read_text(encoding="utf-8")

    require(identity["schemaVersion"] == "1.0", "identity profile version must be 1.0")
    require(identity["github"]["environment"] == "azure-dev", "protected environment must be azure-dev")
    require("id-token: write" in workflow, "reusable workflow must request OIDC")
    require("permissions:\n  contents: read" in workflow, "workflow must default to read-only contents")
    require("client-secret" not in workflow.lower(), "client secrets are forbidden")
    require("HELIOS_AZDO_SERVICE_CONNECTION" in pipeline, "pipeline must use its federated service connection")
    for stage in ("Validate", "WhatIf", "DeployDevelopment"):
        require(re.search(rf"stage:\s*{stage}\b", pipeline), f"pipeline is missing {stage}")

    sources = set(event_schema["properties"]["source"]["enum"])
    ids = set()
    for agent in fleet["agents"]:
        require(agent["id"] not in ids, f"duplicate fleet agent: {agent['id']}")
        ids.add(agent["id"])
        require(agent["requiresReview"] is True, f"fleet agent must be review gated: {agent['id']}")
        source = agent["publishes"][0].split(".", 1)[0]
        require(source in sources, f"fleet source is absent from event contract: {source}")
        require(not any(value.endswith(":write") for value in agent["permissions"] if value.startswith("contents")), "fleet contents write is forbidden")

    require(policy["automation"]["autoMerge"] is False, "automatic protected merge is forbidden")
    require(policy["merge"]["previewArtifactsAllowed"] is False, "preview artifacts cannot enter stable release")
    require(policy["merge"]["credentialsAllowed"] is False, "credentials cannot enter artifacts")
    deployment = policy["deployment"]
    require(deployment["failClosed"] is True, "deployment gates must fail closed")
    require(
        deployment["unblockSequence"] == DEPLOYMENT_UNBLOCK_SEQUENCE,
        "deployment gates are missing or out of order",
    )
    require(
        set(deployment["invalidateOnChange"]) == DEPLOYMENT_INVALIDATION_INPUTS,
        "deployment evidence invalidation inputs are incomplete",
    )
    print("delivery contracts valid")


if __name__ == "__main__":
    main()
