import copy
import importlib.util
import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location(
    "validate_capability_profiles",
    ROOT / "scripts/integrations/validate_capability_profiles.py",
)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader
SPEC.loader.exec_module(MODULE)


class CapabilityProfileTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.registry = json.loads(
            (ROOT / "config/integrations/capability-profiles.json").read_text(encoding="utf-8")
        )

    def test_registry_is_valid(self):
        self.assertEqual([], MODULE.validate(self.registry))

    def test_unknown_profile_is_denied(self):
        self.assertEqual("deny", MODULE.resolve_profile(self.registry, "unknown.superuser"))

    def test_duplicate_profile_is_rejected(self):
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"].append(copy.deepcopy(invalid["profiles"][0]))
        self.assertTrue(any("duplicates" in error for error in MODULE.validate(invalid)))

    def test_mutation_without_idempotency_is_rejected(self):
        invalid = copy.deepcopy(self.registry)
        profile = next(p for p in invalid["profiles"] if p["id"] == "slack.post")
        profile["idempotency"] = {"required": True, "key": None, "replayWindow": None}
        self.assertTrue(any("idempotency.key" in error for error in MODULE.validate(invalid)))


if __name__ == "__main__":
    unittest.main()
