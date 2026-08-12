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
        invalid = {"defaultDecision": "allow", "profiles": []}
        self.assertEqual("deny", MODULE.resolve_profile(invalid, "unknown.superuser"))
        self.assertEqual("deny", MODULE.resolve_profile([], "unknown.superuser"))

    def test_duplicate_profile_is_rejected(self):
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"].append(copy.deepcopy(invalid["profiles"][0]))
        self.assertTrue(any("duplicates" in error for error in MODULE.validate(invalid)))

    def test_mutation_without_idempotency_is_rejected(self):
        invalid = copy.deepcopy(self.registry)
        profile = next(p for p in invalid["profiles"] if p["id"] == "slack.post")
        profile["idempotency"] = {"required": True, "key": None, "replayWindow": None}
        self.assertTrue(any("idempotency.key" in error for error in MODULE.validate(invalid)))

    def test_malformed_documents_return_errors_without_crashing(self):
        self.assertEqual(["$ must be an object"], MODULE.validate([]))
        for field in ("scopes", "resources", "environments"):
            invalid = copy.deepcopy(self.registry)
            invalid["profiles"][0][field] = [{}]
            self.assertTrue(MODULE.validate(invalid), field)
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"].append([])
        self.assertTrue(any("must be an object" in error for error in MODULE.validate(invalid)))
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"][0]["approval"]["mode"] = []
        self.assertTrue(any("approval.mode" in error for error in MODULE.validate(invalid)))

    def test_closed_objects_and_unique_nested_arrays(self):
        invalid = copy.deepcopy(self.registry)
        invalid["unexpected"] = True
        self.assertTrue(any("unknown fields" in error for error in MODULE.validate(invalid)))
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"][0]["unexpected"] = True
        self.assertTrue(any("unknown fields" in error for error in MODULE.validate(invalid)))
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"][1]["approval"]["approvers"] *= 2
        self.assertTrue(any("approvers must not contain duplicates" in error for error in MODULE.validate(invalid)))
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"][0]["cleanup"]["actions"] *= 2
        self.assertTrue(any("actions must not contain duplicates" in error for error in MODULE.validate(invalid)))

    def test_duplicate_exact_matches_fail_closed(self):
        invalid = copy.deepcopy(self.registry)
        invalid["profiles"].append(copy.deepcopy(invalid["profiles"][0]))
        self.assertEqual("deny", MODULE.resolve_profile(invalid, "ai.inference"))


if __name__ == "__main__":
    unittest.main()
