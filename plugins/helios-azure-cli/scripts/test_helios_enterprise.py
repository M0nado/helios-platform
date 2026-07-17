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


if __name__ == "__main__":
    unittest.main()
