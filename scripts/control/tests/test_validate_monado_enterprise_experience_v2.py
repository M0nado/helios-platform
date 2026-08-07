from __future__ import annotations

import copy
import json
import xml.etree.ElementTree as ET
import sys
import tempfile
import unittest
import shutil
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts" / "control"))

from validate_monado_enterprise_experience_v2 import (  # noqa: E402
    validate_contracts,
    validate_profile_xml,
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
        self.storage_contract = json.loads(
            (BASE / "storage.contract.v2.json").read_text(encoding="utf-8")
        )
        self.alvis_budget = json.loads(
            (BASE / "alvis-tool-budgets.v2.json").read_text(encoding="utf-8")
        )
        self.openai_schema = json.loads(
            (BASE / "openai-proposal.schema.v2.json").read_text(encoding="utf-8")
        )

    def _validate_with_override(self, file_name: str, payload: dict) -> list[str]:
        with tempfile.TemporaryDirectory() as temp_dir:
            target = Path(temp_dir) / "experience-fabric"
            shutil.copytree(BASE, target)
            path = target / file_name
            path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
            return validate_contracts(target)

    def test_canonical_contracts_are_valid(self) -> None:
        self.assertEqual(validate_contracts(BASE), [])

    def test_runtime_side_effects_fail_closed(self) -> None:
        candidate = copy.deepcopy(self.root_contract)
        candidate["execution"]["runtimeSideEffectsAllowed"] = True
        errors = self._validate_with_override("monado-enterprise-experience-fabric.v2.json", candidate)
        self.assertTrue(any("runtime side effects must be disabled" in error for error in errors))

    def test_azure_devops_must_remain_read_only(self) -> None:
        candidate = copy.deepcopy(self.sync_contract)
        candidate["systems"]["azure-devops"]["mode"] = "read-only-and-write-enabled"
        errors = self._validate_with_override("synchronization.contract.v2.json", candidate)
        self.assertTrue(any("approved read-only mirror mode" in error for error in errors))

    def test_profile_catalog_must_keep_expected_set(self) -> None:
        candidate = copy.deepcopy(self.profile_catalog)
        candidate["profiles"] = [
            profile for profile in candidate["profiles"] if profile["id"] != "sysops"
        ]
        errors = self._validate_with_override("profile-catalog.v2.json", candidate)
        self.assertTrue(any("profile set mismatch" in error for error in errors))

    def test_profile_catalog_rejects_additional_administrator(self) -> None:
        candidate = copy.deepcopy(self.profile_catalog)
        sysops = next(profile for profile in candidate["profiles"] if profile["id"] == "sysops")
        sysops["administrator"] = True
        errors = self._validate_with_override("profile-catalog.v2.json", candidate)
        self.assertTrue(any("sole administrator profile" in error for error in errors))

    def test_profile_catalog_rejects_duplicate_profile_entries(self) -> None:
        candidate = copy.deepcopy(self.profile_catalog)
        duplicate = copy.deepcopy(next(profile for profile in candidate["profiles"] if profile["id"] == "developer"))
        candidate["profiles"].append(duplicate)
        errors = self._validate_with_override("profile-catalog.v2.json", candidate)
        self.assertTrue(any("must not duplicate profile IDs" in error for error in errors))

    def test_root_contract_references_must_target_expected_contract_file(self) -> None:
        candidate = copy.deepcopy(self.root_contract)
        candidate["contracts"]["storage"] = "config/monadoblade/experience-fabric/profile-catalog.v2.json"
        errors = self._validate_with_override("monado-enterprise-experience-fabric.v2.json", candidate)
        self.assertTrue(any("root contracts.storage must target storage.contract.v2.json" in error for error in errors))

    def test_profile_catalog_default_profile_must_remain_personal(self) -> None:
        candidate = copy.deepcopy(self.profile_catalog)
        candidate["defaultProfile"] = "sysadmin"
        errors = self._validate_with_override("profile-catalog.v2.json", candidate)
        self.assertTrue(any("defaultProfile must remain personal" in error for error in errors))

    def test_sync_envelope_requires_links_and_classification_fields(self) -> None:
        candidate = copy.deepcopy(self.sync_contract)
        required_fields = candidate["envelope"]["requiredFields"]
        candidate["envelope"]["requiredFields"] = [field for field in required_fields if field not in ("links", "dataClassification")]
        errors = self._validate_with_override("synchronization.contract.v2.json", candidate)
        self.assertTrue(any("missing required normalized fields" in error for error in errors))

    def test_sync_requires_strict_idempotency_definition(self) -> None:
        candidate = copy.deepcopy(self.sync_contract)
        candidate["idempotency"]["algorithm"] = "sha1"
        errors = self._validate_with_override("synchronization.contract.v2.json", candidate)
        self.assertTrue(any("idempotency algorithm must be sha256" in error for error in errors))

    def test_storage_guardrails_must_reject_runtime_disk_mutation(self) -> None:
        candidate = copy.deepcopy(self.storage_contract)
        candidate["guardrails"]["denyDirectDiskMutationFromRuntime"] = False
        errors = self._validate_with_override("storage.contract.v2.json", candidate)
        self.assertTrue(any("denyDirectDiskMutationFromRuntime" in error for error in errors))

    def test_storage_vault_kind_must_remain_bitlocker_encrypted(self) -> None:
        candidate = copy.deepcopy(self.storage_contract)
        vault = next(item for item in candidate["topology"]["disk1"]["vhdx"] if item["id"] == "vault")
        vault["kind"] = "dynamic-plaintext"
        errors = self._validate_with_override("storage.contract.v2.json", candidate)
        self.assertTrue(any("dynamic-bitlocker-encrypted" in error for error in errors))

    def test_alvis_tool_budget_must_be_positive(self) -> None:
        candidate = copy.deepcopy(self.alvis_budget)
        candidate["profiles"]["core"]["maxToolCallsPerPlan"] = 0
        errors = self._validate_with_override("alvis-tool-budgets.v2.json", candidate)
        self.assertTrue(any("maxToolCallsPerPlan must be a positive integer" in error for error in errors))

    def test_alvis_denied_operations_must_include_full_safe_set(self) -> None:
        candidate = copy.deepcopy(self.alvis_budget)
        candidate["deniedOperations"] = [
            op for op in candidate["deniedOperations"] if op != "raw-secret-readback"
        ]
        errors = self._validate_with_override("alvis-tool-budgets.v2.json", candidate)
        self.assertTrue(any("must include the full protected operation set" in error for error in errors))

    def test_openai_schema_must_enforce_privileged_approval_guard(self) -> None:
        candidate = copy.deepcopy(self.openai_schema)
        candidate["allOf"] = [{"then": {}}]
        errors = self._validate_with_override("openai-proposal.schema.v2.json", candidate)
        self.assertTrue(any("must enforce approval.required=true" in error for error in errors))

    def test_profile_xml_requires_positive_integer_tool_budget(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            target = Path(temp_dir) / "experience-fabric"
            shutil.copytree(BASE, target)
            xml_path = target / "xml" / "developer.profile.v2.xml"
            tree = ET.parse(xml_path)
            root = tree.getroot()
            namespace = "{https://helios-platform.dev/schemas/monado-profile-v2}"
            node = root.find(f"{namespace}AlvisMaxToolCallsPerPlan")
            self.assertIsNotNone(node)
            node.text = "not-a-number"
            tree.write(xml_path, encoding="utf-8", xml_declaration=True)
            errors = validate_profile_xml(target)
        self.assertTrue(any("AlvisMaxToolCallsPerPlan must be a positive integer" in error for error in errors))

    def test_profile_xml_filename_and_profile_id_must_match(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            target = Path(temp_dir) / "experience-fabric"
            shutil.copytree(BASE, target)
            xml_path = target / "xml" / "developer.profile.v2.xml"
            tree = ET.parse(xml_path)
            root = tree.getroot()
            root.set("profileId", "sysadmin")
            tree.write(xml_path, encoding="utf-8", xml_declaration=True)
            errors = validate_profile_xml(target)
        self.assertTrue(any("must match filename-derived id 'developer'" in error for error in errors))

    def test_profile_xml_tool_budget_must_match_alvis_budget(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            target = Path(temp_dir) / "experience-fabric"
            shutil.copytree(BASE, target)
            xml_path = target / "xml" / "developer.profile.v2.xml"
            tree = ET.parse(xml_path)
            root = tree.getroot()
            namespace = "{https://helios-platform.dev/schemas/monado-profile-v2}"
            node = root.find(f"{namespace}AlvisMaxToolCallsPerPlan")
            self.assertIsNotNone(node)
            node.text = "1"
            tree.write(xml_path, encoding="utf-8", xml_declaration=True)
            errors = validate_profile_xml(target)
        self.assertTrue(any("must match ALVIS profile budget" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
