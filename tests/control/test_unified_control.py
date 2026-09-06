from __future__ import annotations

import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "unified_control",
    ROOT / "scripts/control/unified_control.py",
)
assert SPEC and SPEC.loader
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class UnifiedControlTests(unittest.TestCase):
    def test_plan_only_default(self) -> None:
        cfg = json.loads((ROOT / "config/control/unified-control.v3.json").read_text())
        self.assertEqual(cfg["defaultMode"], "plan-only")
        self.assertFalse(cfg["relay"]["directProviderTokensAllowed"])

    def test_only_existing_fabric_delivers(self) -> None:
        bridge = json.loads(
            (ROOT / "monado/helios-control/config/unified-control-v3.json").read_text()
        )
        self.assertEqual(bridge["runtime"]["controlPlane"], "Helios.Connect")
        self.assertTrue(bridge["delivery"]["signedRelayRequired"])
        self.assertFalse(bridge["delivery"]["directProviderCredentials"])

    def test_openai_tools_are_read_only(self) -> None:
        tools = json.loads(
            (ROOT / "automation/openai/helios-control-tools.v3.json").read_text()
        )["tools"]
        self.assertTrue(tools)
        self.assertTrue(all(t["readOnlyHint"] for t in tools))

    def test_event_is_idempotent_and_nonproduction(self) -> None:
        record = MODULE.build_record("test", "ready", "summary", "next")
        event = MODULE.build_event(record)
        self.assertFalse(event["productionEnabled"])
        self.assertEqual(len(event["idempotencyKey"]), 64)
        self.assertEqual(event["eventType"], "helios.control.current.updated")

    def test_exact_source_sha_precedence(self) -> None:
        import os
        old = os.environ.get("HELIOS_SOURCE_SHA")
        try:
            os.environ["HELIOS_SOURCE_SHA"] = "a" * 40
            self.assertEqual(MODULE.git_sha(), "a" * 40)
        finally:
            if old is None:
                os.environ.pop("HELIOS_SOURCE_SHA", None)
            else:
                os.environ["HELIOS_SOURCE_SHA"] = old


if __name__ == "__main__":
    unittest.main()
