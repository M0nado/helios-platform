#!/usr/bin/env python3
from __future__ import annotations
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "reports" / "branch-intelligence" / "idea-impact-summary.json"
OUT = ROOT / "reports" / "branch-intelligence" / "ai-enrichment.json"

ideas = []
if SRC.exists():
    payload = json.loads(SRC.read_text())
    ideas = payload if isinstance(payload, list) else payload.get("ideas", payload.get("items", []))

enabled = bool(os.environ.get("OPENAI_API_KEY") or os.environ.get("AZURE_OPENAI_API_KEY"))
summary = {
    "enabled": enabled,
    "mode": "ready" if enabled else "offline-placeholder",
    "providerHint": "OPENAI_API_KEY or AZURE_OPENAI_API_KEY",
    "ideaCount": len(ideas) if isinstance(ideas, list) else 0,
    "recommendation": "Use this report as the handoff point for safe AI enrichment; no prompt content or secrets are sent by this offline script."
}
OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n")
print(f"Wrote {OUT.relative_to(ROOT)}")
