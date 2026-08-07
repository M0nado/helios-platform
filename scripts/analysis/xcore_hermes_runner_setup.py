#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LANGUAGE_PROFILES = ROOT / "config/aihub-language-skill-profiles.json"
MODULE_BLUEPRINT = ROOT / "config/aihub-module-blueprint.json"
OUT = ROOT / "reports/learning/xcore-hermes-runner-setup.json"
MD = ROOT / "reports/learning/xcore-hermes-runner-setup.md"


def load_json(path: Path) -> dict:
    return json.loads(path.read_text())


def index_profiles(profiles: list[dict]) -> dict[str, dict]:
    indexed: dict[str, dict] = {}
    for profile in profiles:
        indexed[profile["language"].lower()] = profile
    return indexed


def main() -> int:
    profile_cfg = load_json(LANGUAGE_PROFILES)
    module_cfg = load_json(MODULE_BLUEPRINT)
    profile_map = index_profiles(profile_cfg.get("profiles", []))

    csharp = profile_map.get("c#", {})
    cpp = profile_map.get("c++", {})
    python = profile_map.get("python", {})
    fsharp = profile_map.get("f#", {})

    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "principle": profile_cfg.get("principle"),
        "runnerTopology": {
            "workflowLane": "hermes-xcore-review",
            "bicepEntryPoint": "infra/azure/main.bicep",
            "runnerModule": "infra/azure/modules/xcore-hermes-runner.bicep",
            "serviceBusQueues": [
                "hermes-runner-requests",
                "xcore-evaluation-requests",
                "xcore-hermes-deadletter",
            ],
            "containerJobs": [
                "hermes-orchestrator-runner",
                "xcore-evaluator-runner",
            ],
        },
        "idealLanguageUtilization": {
            "csharp": {
                "focus": "WinUI 3 front end, API/core orchestration, typed contracts, and security/vault policy gating.",
                "skills": csharp.get("skills", []),
                "rankOn": csharp.get("rankOn", []),
            },
            "cpp": {
                "focus": "CPU/memory/GPU fast-path acceleration, low-overhead kernels, and security-sensitive native helpers.",
                "skills": cpp.get("skills", []),
                "rankOn": cpp.get("rankOn", []),
            },
            "fsharp": {
                "focus": "Scoring, prediction, ranking, and optimization that evaluate Hermes/XCore outcomes.",
                "skills": fsharp.get("skills", []),
                "rankOn": fsharp.get("rankOn", []),
            },
            "python": {
                "focus": "WSL2/Linux automation, AIHub and agent library integrations, and cross-provider adapters.",
                "skills": python.get("skills", []),
                "rankOn": python.get("rankOn", []),
            },
        },
        "moduleTargets": module_cfg.get("modules", []),
        "workflowHooks": [
            ".github/workflows/aihub-self-learning-growth.yml",
            ".github/workflows/branch-absorption-multicloud.yml",
            ".github/workflows/azure-infra.yml",
        ],
        "safetyGates": [
            "dry-run by default",
            "immutable container image digest requirement for runner jobs",
            "approval-gated deploy path only",
            "no secret values in repository parameters",
        ],
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + "\n")

    lines = [
        "# XCore/Hermes Runner Setup",
        "",
        f"Generated: `{payload['generatedUtc']}`",
        "",
        payload["principle"],
        "",
        "## Runner topology",
        f"- Workflow lane: `{payload['runnerTopology']['workflowLane']}`",
        f"- Bicep entry point: `{payload['runnerTopology']['bicepEntryPoint']}`",
        f"- Runner module: `{payload['runnerTopology']['runnerModule']}`",
        "- Service Bus queues:",
    ]
    lines += [f"  - `{queue}`" for queue in payload["runnerTopology"]["serviceBusQueues"]]
    lines += ["- Container jobs:"] + [f"  - `{job}`" for job in payload["runnerTopology"]["containerJobs"]]

    lines += [
        "",
        "## Ideal language utilization",
        "",
        "### C#",
        payload["idealLanguageUtilization"]["csharp"]["focus"],
        "",
        f"Skills: {', '.join(payload['idealLanguageUtilization']['csharp']['skills'])}",
        "",
        "### C++",
        payload["idealLanguageUtilization"]["cpp"]["focus"],
        "",
        f"Skills: {', '.join(payload['idealLanguageUtilization']['cpp']['skills'])}",
        "",
        "### F#",
        payload["idealLanguageUtilization"]["fsharp"]["focus"],
        "",
        f"Skills: {', '.join(payload['idealLanguageUtilization']['fsharp']['skills'])}",
        "",
        "### Python",
        payload["idealLanguageUtilization"]["python"]["focus"],
        "",
        f"Skills: {', '.join(payload['idealLanguageUtilization']['python']['skills'])}",
        "",
        "## Safety gates",
    ]
    lines += [f"- {gate}" for gate in payload["safetyGates"]]

    MD.write_text("\n".join(lines) + "\n")
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
