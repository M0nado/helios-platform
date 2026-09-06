from __future__ import annotations

import json
import unittest
from pathlib import Path

from tools.validation.validate_helios_mainline_authority import ContractError, validate


ROOT = Path(__file__).resolve().parents[2]
CONTRACT = ROOT / "config" / "governance" / "helios-mainline-authority.v1.json"


class HeliosMainlineAuthorityTests(unittest.TestCase):
    def load_contract(self) -> dict:
        return json.loads(CONTRACT.read_text(encoding="utf-8"))

    def test_canonical_contract_passes(self) -> None:
        result = validate(self.load_contract())
        self.assertEqual(result["status"], "passed")
        self.assertEqual(result["profiles"], 6)
        self.assertEqual(result["desktop"], "WinUI 3")
        self.assertFalse(result["azureApplyEnabled"])
        self.assertFalse(result["productionEnabled"])

    def test_direct_push_cannot_be_enabled(self) -> None:
        contract = self.load_contract()
        contract["mergePolicy"]["directPushToMain"] = True
        with self.assertRaises(ContractError):
            validate(contract)

    def test_wpf_cannot_be_enabled_in_active_product(self) -> None:
        contract = self.load_contract()
        contract["desktop"]["wpfAllowedInActiveProduct"] = True
        with self.assertRaises(ContractError):
            validate(contract)

    def test_profile_drift_is_rejected(self) -> None:
        contract = self.load_contract()
        contract["profiles"].append({"id": "recovery", "glyph": "復", "privileged": False})
        with self.assertRaises(ContractError):
            validate(contract)

    def test_public_aihub_bind_is_rejected(self) -> None:
        contract = self.load_contract()
        contract["aihub"]["defaultBind"] = "0.0.0.0"
        with self.assertRaises(ContractError):
            validate(contract)

    def test_azure_apply_cannot_be_enabled_by_source_merge(self) -> None:
        contract = self.load_contract()
        contract["azure"]["applyEnabled"] = True
        with self.assertRaises(ContractError):
            validate(contract)

    def test_exposed_openai_key_must_remain_revoked(self) -> None:
        contract = self.load_contract()
        contract["secrets"]["previouslyExposedOpenAIKeyStatus"] = "active"
        with self.assertRaises(ContractError):
            validate(contract)


if __name__ == "__main__":
    unittest.main()
