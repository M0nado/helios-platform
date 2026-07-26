from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "scripts/dev/helios_dev_doctor.py"
SPEC = importlib.util.spec_from_file_location("helios_dev_doctor", SCRIPT)
assert SPEC and SPEC.loader
DOCTOR = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = DOCTOR
SPEC.loader.exec_module(DOCTOR)


class VersionTests(unittest.TestCase):
    def test_extracts_plain_prefixed_and_prerelease_versions(self) -> None:
        self.assertEqual(DOCTOR.extract_version("Python 3.12.13"), "3.12.13")
        self.assertEqual(DOCTOR.extract_version("node v24.18.0"), "24.18.0")
        self.assertEqual(
            DOCTOR.extract_version("Azure MCP 3.0.0-beta.29"),
            "3.0.0-beta.29",
        )

    def test_exact_version_match_is_fail_closed(self) -> None:
        self.assertTrue(DOCTOR.version_matches("2.96.0", "2.96.0"))
        self.assertFalse(DOCTOR.version_matches("2.95.0", "2.96.0"))
        self.assertFalse(
            DOCTOR.version_matches("3.0.0-beta.28", "3.0.0-beta.29")
        )

    def test_major_minor_and_minimum_comparisons(self) -> None:
        self.assertTrue(
            DOCTOR.version_matches(
                "3.12.13",
                "3.12",
                match="major-minor",
            )
        )
        self.assertFalse(
            DOCTOR.version_matches(
                "3.13.0",
                "3.12",
                match="major-minor",
            )
        )
        self.assertTrue(
            DOCTOR.version_matches("3.30.1", "3.28.0", minimum=True)
        )
        self.assertFalse(
            DOCTOR.version_matches("1.10.0", "1.11.0", minimum=True)
        )


class ContractTests(unittest.TestCase):
    def test_repository_contract_is_consistent(self) -> None:
        checks = DOCTOR.check_contract(ROOT)
        failures = [check for check in checks if check.status == "fail"]
        self.assertEqual(
            failures,
            [],
            "\n".join(f"{item.name}: {item.detail}" for item in failures),
        )

    def test_contract_profile_performs_no_tool_or_cloud_probe(self) -> None:
        report = DOCTOR.build_report("contract", ROOT)
        self.assertEqual(report["status"], "ready")
        self.assertFalse(report["cloudAuthenticationChecked"])
        self.assertEqual(report["mutationsPerformed"], 0)
        self.assertFalse(
            any(item["name"].startswith("tool ") for item in report["checks"])
        )


if __name__ == "__main__":
    unittest.main()
