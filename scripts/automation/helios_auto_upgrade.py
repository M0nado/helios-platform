#!/usr/bin/env python3
"""HELIOS deep auto-upgrade orchestrator.

Generates a single upgrade plan and local/hybrid GUI that connects automation
runners, autofix checks, mass merge/PR, Azure/M365/GitHub/OpenAI/Codex/Slack/MCP
providers, Hermes XCore agents, and learning/token optimization loops.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import shutil
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CONFIG = ROOT / "config" / "helios-auto-upgrade.json"
DEFAULT_OUT = ROOT / "reports" / "auto-upgrade"


def run(command: str, execute: bool = False) -> dict[str, Any]:
    if not execute:
        return {"command": command, "dryRun": True, "exitCode": None, "stdout": "", "stderr": ""}
    proc = subprocess.run(command, cwd=ROOT, shell=True, text=True, capture_output=True)
    return {"command": command, "dryRun": False, "exitCode": proc.returncode, "stdout": proc.stdout[-2000:], "stderr": proc.stderr[-2000:]}


def load_json(path: Path, fallback: Any) -> Any:
    if not path.exists():
        return fallback
    return json.loads(path.read_text(encoding="utf-8"))


def command_available(name: str) -> bool:
    return shutil.which(name) is not None


def collect_state(config: dict[str, Any]) -> dict[str, Any]:
    tools = {name: command_available(name) for name in ["git", "gh", "az", "pwsh", "dotnet", "cmake", "python3", "node"]}
    env = {name: bool(os.environ.get(name)) for name in [
        "GITHUB_TOKEN", "HELIOS_AUTOMATION_TOKEN", "AZURE_SUBSCRIPTION_ID", "AZURE_TENANT_ID",
        "OPENAI_API_KEY", "AZURE_OPENAI_API_KEY", "SLACK_BOT_TOKEN", "MCP_SERVER_URL",
        "HELIOS_HERMES_XCORE_ENABLED"
    ]}
    return {
        "generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "tools": tools,
        "environment": env,
        "agents": load_json(ROOT / "config" / "helios-agents.json", {}).get("agents", []),
        "capabilities": load_json(ROOT / "config" / "helios-capabilities.json", {}).get("capabilities", []),
        "phases": config.get("phases", []),
        "specialties": config.get("specialties", []),
    }


def readiness_for_phase(phase: dict[str, Any], state: dict[str, Any]) -> dict[str, Any]:
    agent_names = {agent.get("name") for agent in state.get("agents", [])}
    missing_agents = [agent for agent in phase.get("agents", []) if agent not in agent_names]
    provider_count = len(phase.get("providers", []))
    strategy_count = len(phase.get("strategies", []))
    score = 100 - len(missing_agents) * 20
    if provider_count:
        score += min(20, provider_count * 2)
    if strategy_count:
        score += min(20, strategy_count * 3)
    score = max(0, min(100, score))
    return {**phase, "readinessScore": score, "missingAgents": missing_agents}


def write_reports(out_dir: Path, payload: dict[str, Any]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "auto-upgrade-plan.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Auto Upgrade Plan", "", f"Generated: {payload['generatedUtc']}", "", "## Phases", "", "| Phase | Score | Agents | Commands |", "| --- | ---: | --- | --- |"]
    for phase in payload["phases"]:
        lines.append(f"| {phase['title']} | {phase['readinessScore']} | {', '.join(phase.get('agents', []))} | {len(phase.get('commands', []))} |")
    lines.extend(["", "## Specialties", ""])
    for specialty in payload.get("specialties", []):
        lines.append(f"- `{specialty}`")
    (out_dir / "auto-upgrade-plan.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_gui(out_dir: Path, payload: dict[str, Any]) -> None:
    gui_dir = out_dir / "gui"
    gui_dir.mkdir(parents=True, exist_ok=True)
    (gui_dir / "automation-state.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    cards = []
    for phase in payload["phases"]:
        cards.append(f"""
        <section class=\"card\">
          <h2>{phase['title']}</h2>
          <p class=\"score\">Readiness: {phase['readinessScore']}%</p>
          <p><strong>Agents:</strong> {', '.join(phase.get('agents', [])) or 'none'}</p>
          <p><strong>Outputs:</strong> {', '.join(phase.get('outputs', [])) or 'none'}</p>
        </section>""")
    html = f"""<!doctype html>
<html lang=\"en\">
<head>
  <meta charset=\"utf-8\">
  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">
  <title>HELIOS Hermes XCore Automation GUI</title>
  <style>
    body {{ font-family: Segoe UI, system-ui, sans-serif; margin: 0; background: #08111f; color: #e7f0ff; }}
    header {{ padding: 2rem; background: linear-gradient(135deg, #162d57, #5319e7); }}
    main {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1rem; padding: 1rem; }}
    .card {{ background: #101d33; border: 1px solid #27446f; border-radius: 16px; padding: 1rem; box-shadow: 0 10px 30px rgba(0,0,0,.25); }}
    .score {{ color: #7ee787; font-weight: 700; }}
    code {{ color: #9cdcfe; }}
  </style>
</head>
<body>
  <header>
    <h1>HELIOS Hermes XCore Automation GUI</h1>
    <p>Local/hybrid dashboard for runners, autofix, mass merge, Azure/M365/GitHub/OpenAI/Codex/Slack/MCP providers, specialized agents, and learning optimization.</p>
    <p>Generated: <code>{payload['generatedUtc']}</code></p>
  </header>
  <main>
    {''.join(cards)}
  </main>
</body>
</html>
"""
    (gui_dir / "index.html").write_text(html, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["plan", "verify", "gui", "apply"], nargs="?", default="plan")
    parser.add_argument("--config", default=str(DEFAULT_CONFIG))
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    args = parser.parse_args()
    config = load_json(Path(args.config), {})
    state = collect_state(config)
    phases = [readiness_for_phase(phase, state) for phase in config.get("phases", [])]
    payload = {**state, "phases": phases, "mode": args.mode}
    out_dir = Path(args.out)
    write_reports(out_dir, payload)
    if args.mode in {"gui", "apply"}:
        write_gui(out_dir, payload)
    if args.mode == "apply":
        results = []
        for phase in phases:
            for command in phase.get("commands", []):
                results.append(run(command, execute=True))
        payload["applyResults"] = results
        write_reports(out_dir, payload)
        write_gui(out_dir, payload)
    print(f"Wrote {(out_dir / 'auto-upgrade-plan.md').relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
