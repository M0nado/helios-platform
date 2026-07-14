"""Run deterministic policy cases; optionally smoke-test the live OpenAI agent."""

from __future__ import annotations

import argparse
import asyncio
import json
import os
import sys
from pathlib import Path

SERVICE_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SERVICE_ROOT))

from helios_deployment_agent.policy import evaluate_action  # noqa: E402

CASES_PATH = Path(__file__).with_name("cases.jsonl")


def load_cases() -> list[dict[str, object]]:
    return [
        json.loads(line)
        for line in CASES_PATH.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]


def run_offline(cases: list[dict[str, object]]) -> int:
    failures: list[str] = []
    for case in cases:
        verdict = evaluate_action(str(case["action"]), case.get("controls", {}))
        if verdict.decision.value != case["expected_decision"]:
            failures.append(str(case["id"]))
    if failures:
        print(f"offline policy eval failed: {', '.join(failures)}")
        return 1
    print(f"offline policy eval passed: {len(cases)} cases")
    return 0


async def run_live(cases: list[dict[str, object]]) -> int:
    if not os.getenv("OPENAI_API_KEY"):
        print("OPENAI_API_KEY is required for --live", file=sys.stderr)
        return 2
    from helios_deployment_agent.agent import run_plan

    manifest = {
        "repositories": ["M0nado/helios-platform"],
        "branches": ["main"],
        "languages": ["C#", "F#", "C++", "Python"],
        "proposed_actions": [str(case["action"]) for case in cases],
    }
    prompt = (
        "Produce a plan-only policy assessment. Do not execute anything. Manifest: "
        + json.dumps(manifest, sort_keys=True)
    )
    output = await run_plan(prompt)
    if not output.strip():
        print("live agent returned no output", file=sys.stderr)
        return 1
    print("live agent smoke passed")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--live", action="store_true")
    args = parser.parse_args()
    cases = load_cases()
    offline = run_offline(cases)
    if offline or not args.live:
        return offline
    return asyncio.run(run_live(cases))


if __name__ == "__main__":
    raise SystemExit(main())
