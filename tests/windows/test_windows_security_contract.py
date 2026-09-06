from __future__ import annotations

import copy
import json
import unittest
from pathlib import Path

from tools.validation.validate_windows_security_contract import (
    ContractError,
    validate_boot_policy,
    validate_environment_policy,
    validate_recovery_template,
    validate_repository,
)


ROOT = Path(__file__).resolve().parents[2]
ENVIRONMENT = ROOT / "config/windows/environment-baseline.v2.json"
BOOT_SECURITY = ROOT / "config/windows/boot-security.v2.json"
RECOVERY = ROOT / "config/windows/bitlocker-recovery-evidence.template.json"


class WindowsSecurityContractTests(unittest.TestCase):
    def load(self, path: Path) -> dict:
        return json.loads(path.read_text(encoding="utf-8"))

    def test_repository_contract_passes(self) -> None:
        result = validate_repository(ROOT)
        self.assertEqual(result["status"], "passed")
        self.assertEqual(result["scriptsScanned"], 5)
        self.assertFalse(result["offlineScanAutomatic"])
        self.assertFalse(result["weeklyFullScanDefault"])
        self.assertFalse(result["productionEnabled"])

    def test_environment_path_is_preserve_first(self) -> None:
        policy = self.load(ENVIRONMENT)
        policy["mode"] = "replace"
        with self.assertRaises(ContractError):
            validate_environment_policy(policy)

    def test_correct_wmi_executable_path_is_required(self) -> None:
        policy = self.load(ENVIRONMENT)
        for tool in policy["criticalTools"]:
            if tool["name"] == "winmgmt.exe":
                tool["relativePath"] = "System32\\winmgmt.exe"
        with self.assertRaises(ContractError):
            validate_environment_policy(policy)

    def test_machine_environment_confirmation_cannot_be_removed(self) -> None:
        policy = self.load(ENVIRONMENT)
        policy["machineWrite"]["confirmationPhrase"] = ""
        with self.assertRaises(ContractError):
            validate_environment_policy(policy)

    def test_offline_scan_cannot_be_automatic(self) -> None:
        policy = self.load(BOOT_SECURITY)
        policy["offlineRecovery"]["automatic"] = True
        with self.assertRaises(ContractError):
            validate_boot_policy(policy)

    def test_offline_scan_cannot_be_scheduled(self) -> None:
        policy = self.load(BOOT_SECURITY)
        policy["scheduledScans"]["offlineScanScheduled"] = True
        with self.assertRaises(ContractError):
            validate_boot_policy(policy)

    def test_weekly_full_scan_remains_opt_in(self) -> None:
        policy = self.load(BOOT_SECURITY)
        policy["scheduledScans"]["weeklyFullScanDefault"] = True
        with self.assertRaises(ContractError):
            validate_boot_policy(policy)

    def test_asr_ids_must_be_unique(self) -> None:
        policy = self.load(BOOT_SECURITY)
        policy["attackSurfaceReduction"]["rules"].append(
            copy.deepcopy(policy["attackSurfaceReduction"]["rules"][0])
        )
        with self.assertRaises(ContractError):
            validate_boot_policy(policy)

    def test_recovery_template_cannot_claim_escrow_is_verified(self) -> None:
        template = self.load(RECOVERY)
        template["recoveryKeyEscrowVerified"] = True
        with self.assertRaises(ContractError):
            validate_recovery_template(template)

    def test_recovery_template_cannot_contain_password(self) -> None:
        template = self.load(RECOVERY)
        template["containsRecoveryPassword"] = True
        with self.assertRaises(ContractError):
            validate_recovery_template(template)


if __name__ == "__main__":
    unittest.main()
