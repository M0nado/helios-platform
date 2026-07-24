import importlib.util
import json
import unittest
from pathlib import Path


SCRIPT = Path(__file__).with_name("helios.py")
SPEC = importlib.util.spec_from_file_location("helios_cli", SCRIPT)
assert SPEC and SPEC.loader
HELIOS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HELIOS)


class HeliosCliTests(unittest.TestCase):
    def test_targets_are_canonical(self) -> None:
        targets = HELIOS.read_targets()
        self.assertEqual(
            targets["authorities"]["github"],
            "https://github.com/M0nado/helios-platform",
        )
        self.assertEqual(targets["state"]["azure"], "not-live")

    def test_plan_is_non_executing(self) -> None:
        plan = HELIOS.release_plan("azure-dev")
        self.assertEqual(plan["executionMode"], "plan-only")
        self.assertIn("Azure deployment approval", plan["administratorGates"])
        self.assertEqual(
            plan["federationSubject"],
            "repo:M0nado/helios-platform:environment:azure-dev",
        )

    def test_targets_file_is_json(self) -> None:
        json.loads(HELIOS.TARGETS_FILE.read_text(encoding="utf-8"))

    def test_invalid_environment_fails(self) -> None:
        with self.assertRaises(ValueError):
            HELIOS.release_plan("production-now")


if __name__ == "__main__":
    unittest.main()
