from __future__ import annotations

import copy
import json
import sys
import tempfile
import unittest
import shutil
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts" / "control"))

from validate_monado_enterprise_experience_v2 import (  # noqa: E402
    validate_contracts,
)


BASE = REPOSITORY_ROOT / "config" / "monadoblade" / "experience-fabric"


class MonadoExperienceFabricV2ValidationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.root_contract = json.loads(
            (BASE / "monado-enterprise-experience-fabric.v2.json").read_text(encoding="utf-8")
        )
        self.sync_contract = json.loads(
            (BASE / "synchronization.contract.v2.json").read_text(encoding="utf-8")
        )
        self.profile_catalog = json.loads(
            (BASE / "profile-catalog.v2.json").read_text(encoding="utf-8")
        )

    def test_canonical_contracts_are_valid(self) -> None:
        self.assertEqual(validate_contracts(BASE), [])

    def test_runtime_side_effects_fail_closed(self) -> None:
        candidate = copy.deepcopy(self.root_contract)
        candidate["execution"]["runtimeSideEffectsAllowed"] = True
        with tempfile.TemporaryDirectory() as temp_dir:
            target = Path(temp_dir) / "experience-fabric"
            shutil.copytree(BASE, target)
            path = target / "monado-enterprise-experience-fabric.v2.json"
            path.write_text(json.dumps(candidate, indent=2) + "\n", encoding="utf-8")
            errors = validate_contracts(target)
        self.assertTrue(any("runtime side effects must be disabled" in error for error in errors))

    def test_azure_devops_must_remain_read_only(self) -> None:
        candidate = copy.deepcopy(self.sync_contract)
        candidate["systems"]["azure-devops"]["mode"] = "write-enabled"
        with tempfile.TemporaryDirectory() as temp_dir:
            target = Path(temp_dir) / "experience-fabric"
            shutil.copytree(BASE, target)
            path = target / "synchronization.contract.v2.json"
            path.write_text(json.dumps(candidate, indent=2) + "\n", encoding="utf-8")
            errors = validate_contracts(target)
        self.assertTrue(any("azure-devops must remain read-only" in error for error in errors))

    def test_profile_catalog_must_keep_expected_set(self) -> None:
        candidate = copy.deepcopy(self.profile_catalog)
        candidate["profiles"] = [
            profile for profile in candidate["profiles"] if profile["id"] != "sysops"
        ]
        with tempfile.TemporaryDirectory() as temp_dir:
            target = Path(temp_dir) / "experience-fabric"
            shutil.copytree(BASE, target)
            path = target / "profile-catalog.v2.json"
            path.write_text(json.dumps(candidate, indent=2) + "\n", encoding="utf-8")
            errors = validate_contracts(target)
        self.assertTrue(any("profile set mismatch" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
