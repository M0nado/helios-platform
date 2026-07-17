#!/usr/bin/env python3
"""Render HELIOS multi-LLM router and cross-optimization plan."""
from __future__ import annotations
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / "config" / "helios-llm-router.json"
OUT = ROOT / "reports" / "llm-router"

def main() -> int:
    data = json.loads(CONFIG.read_text(encoding="utf-8"))
    providers = []
    for provider in data.get("providerFamilies", []):
        env_ready = {name: bool(os.environ.get(name)) for name in provider.get("env", [])}
        readiness = 100 if all(env_ready.values()) or provider.get("type") == "local" else 50 if any(env_ready.values()) else 0
        providers.append({**provider, "envReady": env_ready, "readinessScore": readiness})
    profiles = []
    provider_weights = {p["id"]: p.get("defaultWeight", 0) + p["readinessScore"] for p in providers}
    for profile in data.get("agentModelProfiles", []):
        ranked = sorted(profile.get("preferredFamilies", []), key=lambda item: provider_weights.get(item, 0), reverse=True)
        profiles.append({**profile, "rankedFamilies": ranked})
    payload = {**data, "providerFamilies": providers, "agentModelProfiles": profiles}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "llm-router-plan.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Multi-LLM Router Plan", "", data.get("description", ""), "", "## Provider readiness", "", "| Provider | Type | Score | Roles |", "| --- | --- | ---: | --- |"]
    for provider in providers:
        lines.append(f"| `{provider['id']}` | {provider['type']} | {provider['readinessScore']} | {', '.join(provider.get('roles', []))} |")
    lines.extend(["", "## Agent model profiles", "", "| Agent | Parallelism | Ranked families | Roles |", "| --- | ---: | --- | --- |"])
    for profile in profiles:
        lines.append(f"| `{profile['agent']}` | {profile['parallelism']} | {', '.join(profile['rankedFamilies'])} | {', '.join(profile.get('primaryRoles', []))} |")
    lines.extend(["", "## Optimization policies", ""])
    for policy in data.get("optimizationPolicies", []):
        lines.append(f"- **{policy['id']}**: {policy['rule']}")
    (OUT / "llm-router-plan.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {(OUT / 'llm-router-plan.md').relative_to(ROOT)}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
