#!/usr/bin/env python3
"""Generate the public HELIOS AI/coding-engine discovery document."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PROFILE_PATH = ROOT / "config" / "aihub-language-skill-profiles.json"
OUTPUT_PATH = ROOT / "llms.txt"


def render(profile_path: Path = PROFILE_PATH) -> str:
    data = json.loads(profile_path.read_text(encoding="utf-8"))
    profiles = data["profiles"]
    if {item["language"] for item in profiles} != {"C#", "C++", "F#", "Python"}:
        raise ValueError("Language profiles must contain exactly C#, C++, F#, and Python")

    lines = [
        "# HELIOS / MonadoBlade",
        "",
        "> Governed, C#-first Windows control and AI orchestration platform.",
        "",
        "Canonical repository: M0nado/helios-platform",
        "Integration contract: /config/integrations/event-contract.schema.json",
        "Repository ownership: /config/integrations/repositories.json",
        "Agent communication: /docs/architecture/UNIFIED_AGENT_COMMUNICATION.md",
        "Capability backlog: /config/capabilities/major-capabilities.v1.json",
        "",
        "## Engine routing",
        "",
        data["principle"],
        "",
    ]
    for profile in profiles:
        lines.extend(
            [
                f"### {profile['language']}: {profile['role']}",
                "",
                profile["hundredWordGuide"],
                "",
                "Skills: " + ", ".join(profile["skills"]) + ".",
                "Route when optimizing: " + ", ".join(profile["rankOn"]) + ".",
                "",
            ]
        )

    lines.extend(
        [
            "## AI provider boundaries",
            "",
            "OpenAI API and Codex integrations belong behind AIHub provider adapters; use structured outputs, bounded tool permissions, correlation IDs, evaluation evidence, and secret references rather than embedded keys. Claude API and Claude Code use the same provider-neutral command/result envelope and cannot bypass approval gates. Hermes routes bounded work, XCore evaluates it, and Guardian blocks secrets, privileged Windows changes, tenant mutations, and production deployment without review.",
            "",
            "## Safe setup and operations",
            "",
            "Azure automation uses workload identity federation, managed identity, Key Vault, least privilege, Bicep validation, and what-if before apply. Windows disk, BitLocker, WDAC/AppLocker, firewall, Entra/RBAC, Intune, Purview, secret rotation, and production actions remain dry-run and approval gated. Never place credentials, raw fleet evidence, model weights, logs, caches, VHDX files, or local databases in Git.",
            "",
            "## Development checks",
            "",
            "Run `python3 scripts/knowledge/generate_llms_txt.py --check` to detect discovery-document drift, `python3 scripts/knowledge/validate_capabilities.py` to validate the consolidated 50-capability dependency graph, and `python3 scripts/knowledge/plan_capability_issues.py` to preview reviewable issue packets. Packet generation is local-only and never creates issues, branches, cloud resources, or privileged machine changes.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="fail if llms.txt is stale")
    args = parser.parse_args()
    expected = render()
    if args.check:
        if not OUTPUT_PATH.exists() or OUTPUT_PATH.read_text(encoding="utf-8") != expected:
            print("llms.txt is stale; run scripts/knowledge/generate_llms_txt.py")
            return 1
        print("llms.txt is current")
        return 0
    OUTPUT_PATH.write_text(expected, encoding="utf-8")
    print(f"wrote {OUTPUT_PATH.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
