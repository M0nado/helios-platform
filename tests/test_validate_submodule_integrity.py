from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

SCRIPT_PATH = (
    Path(__file__).resolve().parents[1] / "scripts" / "validate_submodule_integrity.py"
)


def write_fixture(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent(content).strip() + "\n", encoding="utf-8")


class ValidateSubmoduleIntegrityTests(unittest.TestCase):
    def run_validator(self, gitmodules_path: Path, repositories_path: Path, json_out: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                sys.executable,
                str(SCRIPT_PATH),
                "--gitmodules",
                str(gitmodules_path),
                "--repositories",
                str(repositories_path),
                "--json-out",
                str(json_out),
            ],
            capture_output=True,
            text=True,
            check=False,
        )

    def test_validator_succeeds_for_matching_repository_map(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            gitmodules_path = root / ".gitmodules"
            repositories_path = root / "config" / "integrations" / "repositories.json"
            json_out = root / "report" / "submodule-integrity.json"

            write_fixture(
                gitmodules_path,
                """
                [submodule "modules/helios-monado-blade"]
                    path = modules/helios-monado-blade
                    url = https://github.com/M0nado/helios-monado-blade.git
                    branch = main
                [submodule "modules/helios-ai-hub"]
                    path = modules/helios-ai-hub
                    url = git@github.com:M0nado/helios-ai-hub.git
                    branch = main
                """,
            )
            repositories_payload = {
                "repositories": [
                    {"name": "M0nado/helios-monado-blade"},
                    {"name": "M0nado/helios-ai-hub"},
                ]
            }
            repositories_path.parent.mkdir(parents=True, exist_ok=True)
            repositories_path.write_text(
                json.dumps(repositories_payload, indent=2) + "\n",
                encoding="utf-8",
            )

            result = self.run_validator(gitmodules_path, repositories_path, json_out)
            self.assertEqual(result.returncode, 0, msg=result.stderr)
            self.assertIn("Validated 2 submodules", result.stdout)
            report = json.loads(json_out.read_text(encoding="utf-8"))
            self.assertTrue(report["ok"])
            self.assertEqual(report["submoduleCount"], 2)
            self.assertEqual(report["errors"], [])

    def test_validator_fails_for_unknown_repository_reference(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            gitmodules_path = root / ".gitmodules"
            repositories_path = root / "config" / "integrations" / "repositories.json"
            json_out = root / "report" / "submodule-integrity.json"

            write_fixture(
                gitmodules_path,
                """
                [submodule "modules/example"]
                    path = modules/example
                    url = https://github.com/ExampleOrg/example.git
                    branch = main
                """,
            )
            repositories_payload = {
                "repositories": [{"name": "M0nado/helios-monado-blade"}]
            }
            repositories_path.parent.mkdir(parents=True, exist_ok=True)
            repositories_path.write_text(
                json.dumps(repositories_payload, indent=2) + "\n",
                encoding="utf-8",
            )

            result = self.run_validator(gitmodules_path, repositories_path, json_out)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("unknown repository", result.stderr.lower())
            report = json.loads(json_out.read_text(encoding="utf-8"))
            self.assertFalse(report["ok"])
            self.assertGreater(len(report["errors"]), 0)

    def test_validator_fails_for_duplicate_submodule_paths(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            gitmodules_path = root / ".gitmodules"
            repositories_path = root / "config" / "integrations" / "repositories.json"
            json_out = root / "report" / "submodule-integrity.json"

            write_fixture(
                gitmodules_path,
                """
                [submodule "modules/one"]
                    path = modules/shared
                    url = https://github.com/M0nado/helios-monado-blade.git
                    branch = main
                [submodule "modules/two"]
                    path = modules/shared
                    url = https://github.com/M0nado/helios-security-setup.git
                    branch = main
                """,
            )
            repositories_payload = {
                "repositories": [
                    {"name": "M0nado/helios-monado-blade"},
                    {"name": "M0nado/helios-security-setup"},
                ]
            }
            repositories_path.parent.mkdir(parents=True, exist_ok=True)
            repositories_path.write_text(
                json.dumps(repositories_payload, indent=2) + "\n",
                encoding="utf-8",
            )

            result = self.run_validator(gitmodules_path, repositories_path, json_out)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("duplicate submodule path", result.stderr.lower())
            report = json.loads(json_out.read_text(encoding="utf-8"))
            self.assertFalse(report["ok"])
            self.assertTrue(any("Duplicate submodule path" in error for error in report["errors"]))


if __name__ == "__main__":
    unittest.main()
