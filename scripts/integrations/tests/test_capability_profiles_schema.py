import json
import unittest
from pathlib import Path

from jsonschema import Draft202012Validator


ROOT = Path(__file__).resolve().parents[3]


class CapabilityProfileSchemaTests(unittest.TestCase):
    def test_schema_and_registry(self):
        schema = json.loads(
            (ROOT / "config/integrations/capability-profiles.schema.json").read_text(encoding="utf-8")
        )
        registry = json.loads(
            (ROOT / "config/integrations/capability-profiles.json").read_text(encoding="utf-8")
        )
        Draft202012Validator.check_schema(schema)
        errors = sorted(Draft202012Validator(schema).iter_errors(registry), key=lambda error: list(error.path))
        self.assertEqual([], [error.message for error in errors])


if __name__ == "__main__":
    unittest.main()
