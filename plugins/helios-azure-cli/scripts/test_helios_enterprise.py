from __future__ import annotations

import os
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))

import helios_enterprise


class EnterpriseSetupTests(unittest.TestCase):
    def test_production_is_disabled(self) -> None:
        setup = helios_enterprise.load_setup()
        self.assertFalse(setup["productionEnabled"])
        production = next(item for item in setup["phases"] if item["id"] == "production")
        self.assertEqual([], production["operations"])

    def test_confirmation_is_required(self) -> None:
        with self.assertRaises(helios_enterprise.SetupError):
            helios_enterprise.execute_phase("connections", "wrong")

    def test_render_requires_environment(self) -> None:
        with patch.dict(os.environ, {}, clear=True):
            with self.assertRaises(helios_enterprise.SetupError):
                helios_enterprise.render_agent365_config()

    def test_operation_manifest_has_unique_phases(self) -> None:
        setup = helios_enterprise.load_setup()
        ids = [item["id"] for item in setup["phases"]]
        self.assertEqual(len(ids), len(set(ids)))

    def test_every_operation_is_supported(self) -> None:
        setup = helios_enterprise.load_setup()
        environment = {
            "HELIOS_AZURE_DEVOPS_ORG": "https://dev.azure.com/example",
            "HELIOS_AZURE_DEVOPS_PROJECT": "Helios",
        }
        with patch.dict(os.environ, environment, clear=False):
            for phase in setup["phases"]:
                for operation in phase["operations"]:
                    with self.subTest(operation=operation):
                        helios_enterprise.operation_commands(operation)


if __name__ == "__main__":
    unittest.main()
