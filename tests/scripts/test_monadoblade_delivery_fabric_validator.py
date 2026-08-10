from __future__ import annotations

import copy
import importlib.util
import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "monadoblade_validator",
    ROOT / "scripts/validation/validate_monadoblade_delivery_fabric.py",
)
validator = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(validator)


def load(relative_path: str) -> dict:
    return json.loads((ROOT / relative_path).read_text(encoding="utf-8"))


class MonadobladeDeliveryFabricValidatorTests(unittest.TestCase):
    def test_rejects_insufficient_distinct_sysadmin_factors(self) -> None:
        profiles = load("config/profiles/monadoblade-profiles.v2.json")
        sysadmin = next(item for item in profiles["identities"] if item["id"] == "sysadmin")
        sysadmin["activation"]["allowedFactors"] = ["security-key", " SECURITY-KEY "]

        with self.assertRaisesRegex(validator.ContractError, "distinct, nonblank"):
            validator.validate_identities(profiles)

    def test_rejects_scalar_engine_effects(self) -> None:
        registry = load("config/aihub/monadoblade-engine-registry.v1.json")
        registry["engines"][0]["effects"] = "execute"

        with self.assertRaisesRegex(validator.ContractError, "list of strings"):
            validator.validate_engine_registry(registry)

    def test_rejects_gui_storage_apply(self) -> None:
        storage = load("config/storage/monadoblade-storage-plan-template.v2.json")
        storage["apply"]["availableFromGui"] = True

        with self.assertRaisesRegex(validator.ContractError, "GUI cannot apply"):
            validator.validate_storage(storage)

    def test_rejects_any_renderer_budget_drift(self) -> None:
        environment = load("config/experience/monadoblade-living-environments.v1.json")
        changed = copy.deepcopy(environment)
        changed["performance"]["qualityTiers"]["balanced"]["updatesPerSecond"] = 60

        with self.assertRaisesRegex(validator.ContractError, "exactly match"):
            validator.validate_environment(changed)


if __name__ == "__main__":
    unittest.main()
