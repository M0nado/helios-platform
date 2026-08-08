from __future__ import annotations

import copy
from pathlib import Path
import sys
import unittest


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts" / "contracts"))

from validate_monado_enterprise_v2 import (  # noqa: E402
    EXPECTED_PROFILES,
    load_contract_bundle,
    validate_contract_bundle,
    validate_experience_contract,
    validate_profiles_contract,
    validate_repository_map_contract,
    validate_storage_contract,
    validate_sync_contract,
)


class MonadoEnterpriseV2ValidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.bundle = load_contract_bundle(REPOSITORY_ROOT)

    def test_canonical_bundle_is_valid(self) -> None:
        self.assertEqual(validate_contract_bundle(REPOSITORY_ROOT), [])

    def test_profiles_fail_when_profile_is_missing(self) -> None:
        candidate = copy.deepcopy(self.bundle["profiles"])
        candidate["profiles"] = [profile for profile in candidate["profiles"] if profile["id"] != "core"]
        errors = validate_profiles_contract(candidate)
        self.assertTrue(any("profiles set mismatch" in error for error in errors))

    def test_profiles_fail_when_second_admin_is_added(self) -> None:
        candidate = copy.deepcopy(self.bundle["profiles"])
        target = next(profile for profile in candidate["profiles"] if profile["id"] == "sysops")
        target["administrator"] = True
        errors = validate_profiles_contract(candidate)
        self.assertTrue(any("exactly one administrator profile" in error for error in errors))

    def test_profiles_fail_when_duplicate_profile_id_exists(self) -> None:
        candidate = copy.deepcopy(self.bundle["profiles"])
        duplicate = copy.deepcopy(next(profile for profile in candidate["profiles"] if profile["id"] == "core"))
        candidate["profiles"][-1] = duplicate
        errors = validate_profiles_contract(candidate)
        self.assertTrue(any("duplicate profile ids" in error for error in errors))

    def test_profiles_fail_when_recovery_is_misclassified(self) -> None:
        candidate = copy.deepcopy(self.bundle["profiles"])
        candidate["states"] = [state for state in candidate["states"] if state["id"] != "recovery"]
        errors = validate_profiles_contract(candidate)
        self.assertTrue(any("states must be recovery and quarantine" in error for error in errors))

    def test_profiles_fail_when_state_entry_skips_approval(self) -> None:
        candidate = copy.deepcopy(self.bundle["profiles"])
        candidate["states"][0]["entryRequiresApproval"] = False
        errors = validate_profiles_contract(candidate)
        self.assertTrue(any("must require approval for entry" in error for error in errors))

    def test_storage_fails_when_core_cross_is_missing(self) -> None:
        candidate = copy.deepcopy(self.bundle["storage"])
        disk0 = next(disk for disk in candidate["disks"] if disk["id"] == "disk0")
        disk0["volumes"] = [volume for volume in disk0["volumes"] if volume.get("label") != "CORE_CROSS"]
        errors = validate_storage_contract(candidate)
        self.assertTrue(any("X: CORE_CROSS" in error for error in errors))

    def test_storage_fails_when_vault_is_automounted(self) -> None:
        candidate = copy.deepcopy(self.bundle["storage"])
        disk1 = next(disk for disk in candidate["disks"] if disk["id"] == "disk1")
        vault = next(virtual_disk for virtual_disk in disk1["virtualDisks"] if virtual_disk["preferredLetter"] == "V")
        vault["autoMount"] = True
        errors = validate_storage_contract(candidate)
        self.assertTrue(any("must never auto-mount" in error for error in errors))

    def test_storage_fails_when_dev_drive_contract_is_weakened(self) -> None:
        candidate = copy.deepcopy(self.bundle["storage"])
        disk1 = next(disk for disk in candidate["disks"] if disk["id"] == "disk1")
        dev_drive = next(virtual_disk for virtual_disk in disk1["virtualDisks"] if virtual_disk["preferredLetter"] == "D")
        dev_drive["type"] = "fixed"
        errors = validate_storage_contract(candidate)
        self.assertTrue(any("D: dev drive must be VHDX" in error for error in errors))

    def test_experience_fails_when_alvis_is_admin(self) -> None:
        candidate = copy.deepcopy(self.bundle["experience"])
        candidate["alvis"]["administrator"] = True
        errors = validate_experience_contract(candidate)
        self.assertTrue(any("must not be administrator" in error for error in errors))

    def test_experience_fails_when_profiles_are_incomplete(self) -> None:
        candidate = copy.deepcopy(self.bundle["experience"])
        candidate["profiles"].pop("core")
        errors = validate_experience_contract(candidate)
        self.assertTrue(any("must include all permanent profiles" in error for error in errors))

    def test_experience_fails_when_allowlist_contains_privileged_verb(self) -> None:
        candidate = copy.deepcopy(self.bundle["experience"])
        candidate["profiles"]["core"]["alvisToolBudget"]["allow"] = ["disk.apply"]
        errors = validate_experience_contract(candidate)
        self.assertTrue(any("contains privileged verb" in error for error in errors))

    def test_sync_fails_when_not_proposal_only(self) -> None:
        candidate = copy.deepcopy(self.bundle["synchronization"])
        candidate["executionMode"] = "apply"
        errors = validate_sync_contract(candidate)
        self.assertTrue(any("proposal-only" in error for error in errors))

    def test_sync_fails_when_azure_devops_is_not_readonly(self) -> None:
        candidate = copy.deepcopy(self.bundle["synchronization"])
        candidate["surfaces"]["azure-devops"]["readOnly"] = False
        errors = validate_sync_contract(candidate)
        self.assertTrue(any("azure-devops must be read-only" in error for error in errors))

    def test_sync_fails_when_required_fields_do_not_match_envelope_schema(self) -> None:
        candidate = copy.deepcopy(self.bundle["synchronization"])
        candidate["normalizedEnvelope"]["requiredFields"] = ["payload"]
        errors = validate_sync_contract(candidate)
        self.assertTrue(any("requiredFields must match" in error for error in errors))

    def test_sync_fails_when_privileged_route_skips_approval(self) -> None:
        candidate = copy.deepcopy(self.bundle["synchronization"])
        route = candidate["routes"][0]
        route["operation"] = "production-deployment"
        route["requiresApproval"] = False
        errors = validate_sync_contract(candidate)
        self.assertTrue(any("must require approval" in error for error in errors))

    def test_sync_fails_when_idempotency_inputs_are_not_canonical(self) -> None:
        candidate = copy.deepcopy(self.bundle["synchronization"])
        candidate["idempotency"]["inputFields"] = ["routeId"]
        errors = validate_sync_contract(candidate)
        self.assertTrue(any("inputFields must match canonical normalized key inputs" in error for error in errors))

    def test_repository_map_fails_when_canonical_repo_changes(self) -> None:
        candidate = copy.deepcopy(self.bundle["repositoryMap"])
        candidate["canonicalRepository"] = "example/not-canonical"
        errors = validate_repository_map_contract(candidate)
        self.assertTrue(any("canonicalRepository" in error for error in errors))

    def test_repository_map_fails_when_canonical_boundary_is_missing(self) -> None:
        candidate = copy.deepcopy(self.bundle["repositoryMap"])
        candidate["ownershipBoundaries"] = [
            entry
            for entry in candidate["ownershipBoundaries"]
            if entry.get("repository") != "M0nado/helios-platform"
        ]
        errors = validate_repository_map_contract(candidate)
        self.assertTrue(any("missing canonical entry for M0nado/helios-platform" in error for error in errors))

    def test_repository_map_fails_when_bootstrap_boundary_role_changes(self) -> None:
        candidate = copy.deepcopy(self.bundle["repositoryMap"])
        target = next(
            entry
            for entry in candidate["ownershipBoundaries"]
            if entry["repository"] == "Heli0s-Dynamics/adaptive-multibrain-bootstrap"
        )
        target["role"] = "untrusted-mirror"
        errors = validate_repository_map_contract(candidate)
        self.assertTrue(
            any(
                "ownershipBoundaries.Heli0s-Dynamics/adaptive-multibrain-bootstrap.role must be cross-repository-control-plane"
                in error
                for error in errors
            )
        )

    def test_expected_profile_set_is_stable(self) -> None:
        self.assertSetEqual(
            EXPECTED_PROFILES,
            {"core", "developer", "gamer", "studio", "personal", "sysops", "ai-server", "sysadmin"},
        )


if __name__ == "__main__":
    unittest.main()
