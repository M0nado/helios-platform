from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path
import sys

SETUP_DIR = Path(__file__).resolve().parents[2] / "tools" / "helios-setup"
sys.path.insert(0, str(SETUP_DIR))

import helios_setup


class SetupTests(unittest.TestCase):
    def setUp(self) -> None:
        self.payload = {
            "schemaVersion": "1.0",
            "setupId": "test",
            "defaultMode": "plan",
            "productionEnabled": False,
            "productionBlockerIssue": 162,
            "phases": [
                {"id": phase_id, "description": phase_id, "approval": "test", "commands": []}
                for phase_id in [
                    "tools",
                    "developer-auth",
                    "context",
                    "agent365-developer",
                    "agent365-admin-handoff",
                    "azure-plan",
                    "connections",
                    "development-apply",
                    "production",
                ]
            ],
        }

    def test_manifest_validation(self) -> None:
        helios_setup.validate_manifest(self.payload)

    def test_production_requires_no_commands(self) -> None:
        self.payload["phases"][-1]["commands"] = ["az deployment group create"]
        with self.assertRaises(helios_setup.SetupError):
            helios_setup.validate_manifest(self.payload)

    def test_load_manifest_rejects_production_enabled(self) -> None:
        self.payload["productionEnabled"] = True
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "manifest.json"
            path.write_text(json.dumps(self.payload), encoding="utf-8")
            with self.assertRaises(helios_setup.SetupError):
                helios_setup.load_manifest(path)


if __name__ == "__main__":
    unittest.main()
