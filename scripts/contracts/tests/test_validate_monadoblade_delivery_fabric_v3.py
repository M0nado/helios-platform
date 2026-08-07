from __future__ import annotations

import copy
from pathlib import Path
import sys
import unittest


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts" / "contracts"))

from validate_monadoblade_delivery_fabric_v3 import (  # noqa: E402
    EXPECTED_LIBRARY_SURFACES,
    EXPECTED_PROFILES,
    load_contract_bundle,
    validate_contract_bundle,
    validate_experience_contract,
    validate_libraries_contract,
    validate_migration_map_contract,
    validate_profiles_contract,
    validate_projection_contract,
    validate_storage_contract,
)


class MonadobladeDeliveryFabricV3ValidationTests(unittest.TestCase):
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

    def test_experience_fails_when_shell_runs_pre_auth(self) -> None:
        candidate = copy.deepcopy(self.bundle["experience"])
        candidate["postAuthShell"]["runsAfterWindowsAuthentication"] = False
        errors = validate_experience_contract(candidate)
        self.assertTrue(any("must run after Windows authentication" in error for error in errors))

    def test_experience_fails_when_alvis_executor_tools_are_enabled(self) -> None:
        candidate = copy.deepcopy(self.bundle["experience"])
        candidate["alvis"]["executorToolsAllowed"] = True
        errors = validate_experience_contract(candidate)
        self.assertTrue(any("executor tools must be disabled" in error for error in errors))

    def test_storage_fails_when_usb_apply_route_is_enabled(self) -> None:
        candidate = copy.deepcopy(self.bundle["storage"])
        candidate["usbWizardRoutes"]["applyRouteEnabled"] = True
        errors = validate_storage_contract(candidate)
        self.assertTrue(any("usb apply route must be disabled" in error for error in errors))

    def test_projection_fails_when_trigger_execution_is_enabled(self) -> None:
        candidate = copy.deepcopy(self.bundle["integrationProjection"])
        candidate["executionTriggersAllowed"] = True
        errors = validate_projection_contract(candidate)
        self.assertTrue(any("executionTriggersAllowed must be false" in error for error in errors))

    def test_libraries_fail_when_surface_is_missing(self) -> None:
        candidate = copy.deepcopy(self.bundle["libraries"])
        candidate["libraries"] = [library for library in candidate["libraries"] if library["surface"] != "wyvern"]
        errors = validate_libraries_contract(candidate)
        self.assertTrue(any("library surfaces mismatch" in error for error in errors))

    def test_migration_map_fails_when_v1_profile_is_missing(self) -> None:
        candidate = copy.deepcopy(self.bundle["migrationMap"])
        candidate["mappings"] = [
            mapping
            for mapping in candidate["mappings"]
            if not (mapping["sourceVersion"] == "v1" and mapping["sourceProfileId"] == "server-background")
        ]
        errors = validate_migration_map_contract(candidate)
        self.assertTrue(any("must cover all required v1 profile IDs" in error for error in errors))

    def test_expected_profile_set_is_stable(self) -> None:
        self.assertSetEqual(
            EXPECTED_PROFILES,
            {"core", "developer", "studio", "gamer", "ai-server", "sysadmin"},
        )

    def test_expected_library_surfaces_are_stable(self) -> None:
        self.assertSetEqual(
            EXPECTED_LIBRARY_SURFACES,
            {
                "policy",
                "evidence",
                "control-client",
                "shellkit",
                "renderer",
                "chroma",
                "wyvern",
                "usb-device-broker-requests",
            },
        )


if __name__ == "__main__":
    unittest.main()
