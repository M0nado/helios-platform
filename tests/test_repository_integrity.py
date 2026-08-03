import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/integrations/validate_repository_integrity.py"
SPEC = importlib.util.spec_from_file_location("repository_integrity", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


def run_git(root: Path, *args: str) -> None:
    subprocess.run(["git", *args], cwd=root, check=True, capture_output=True)


class RepositoryIntegrityTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.repository = Path(self.temporary_directory.name)
        (self.repository / "config/integrations").mkdir(parents=True)
        self.write_registry(
            {
                "repositories": [
                    {
                        "name": "M0nado/example",
                        "role": "example",
                        "integrationMode": "pinned-submodule",
                        "authority": ["tests"],
                    }
                ]
            }
        )
        (self.repository / ".gitmodules").write_text(
            '[submodule "modules/example"]\n'
            "\tpath = modules/example\n"
            "\turl = https://github.com/M0nado/example.git\n",
            encoding="utf-8",
        )
        run_git(self.repository, "init")
        run_git(
            self.repository,
            "update-index",
            "--add",
            "--cacheinfo",
            "160000," + "1" * 40 + ",modules/example",
        )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def write_registry(self, value: object) -> None:
        (self.repository / "config/integrations/repositories.json").write_text(
            json.dumps(value), encoding="utf-8"
        )

    def test_github_name_normalizes_canonical_url(self) -> None:
        self.assertEqual(
            MODULE.github_name("https://github.com/M0nado/helios-ai-hub.git"),
            "m0nado/helios-ai-hub",
        )

    def test_valid_repository_has_no_errors(self) -> None:
        self.assertEqual(MODULE.validate(self.repository), [])

    def test_missing_gitlink_is_reported(self) -> None:
        run_git(self.repository, "update-index", "--force-remove", "modules/example")
        self.assertEqual(
            MODULE.validate(self.repository),
            ["declared submodule has no 160000 gitlink: modules/example"],
        )

    def test_bootstrap_supports_an_empty_index(self) -> None:
        run_git(self.repository, "update-index", "--force-remove", "modules/example")
        self.assertEqual(MODULE.validate_bootstrap(self.repository), [])

    def test_bootstrap_switches_to_full_validation(self) -> None:
        run_git(
            self.repository,
            "update-index",
            "--add",
            "--cacheinfo",
            "160000," + "2" * 40 + ",modules/orphan",
        )
        self.assertIn(
            "orphan 160000 gitlink: modules/orphan",
            MODULE.validate_bootstrap(self.repository),
        )

    def test_malformed_files_are_reported(self) -> None:
        gitmodules = self.repository / ".gitmodules"
        registry = self.repository / "config/integrations/repositories.json"
        original_gitmodules = gitmodules.read_text(encoding="utf-8")
        original_registry = registry.read_text(encoding="utf-8")
        with self.subTest(file="gitmodules"):
            gitmodules.write_text("[broken\n", encoding="utf-8")
            self.assertTrue(MODULE.validate(self.repository, require_gitlinks=False)[0].startswith("cannot read .gitmodules:"))
            gitmodules.write_text(original_gitmodules, encoding="utf-8")
        with self.subTest(file="registry"):
            registry.write_text("{", encoding="utf-8")
            self.assertTrue(MODULE.validate(self.repository, require_gitlinks=False)[0].startswith("cannot read repository registry:"))
            registry.write_text(original_registry, encoding="utf-8")

    def test_registry_structure_is_validated(self) -> None:
        cases = [
            ([], "$: expected object"),
            ({}, "$.repositories: expected array"),
            ({"repositories": [None]}, "$.repositories[0]: expected object"),
            ({"repositories": [{}]}, "$.repositories[0].name: expected nonempty string"),
            ({"repositories": [{"name": "x", "role": "x", "integrationMode": "bad", "authority": []}]}, "$.repositories[0].integrationMode: unsupported value"),
        ]
        for registry, message in cases:
            with self.subTest(message=message):
                self.write_registry(registry)
                self.assertIn(message, MODULE.validate(self.repository, require_gitlinks=False))

    def test_unmerged_index_entry_is_rejected(self) -> None:
        output = "160000 " + "1" * 40 + " 2\tmodules/example\n"
        with mock.patch.object(MODULE.subprocess, "run", return_value=SimpleNamespace(stdout=output)):
            with self.assertRaisesRegex(ValueError, "unmerged index entry at stage 2"):
                MODULE.gitlinks(self.repository)

    def test_unsafe_paths_are_reported(self) -> None:
        original = (self.repository / ".gitmodules").read_text(encoding="utf-8")
        for path in ("../example", "/modules/example", "example"):
            with self.subTest(path=path):
                (self.repository / ".gitmodules").write_text(original.replace("modules/example", path), encoding="utf-8")
                self.assertIn(
                    f"unsafe submodule path: {path!r}",
                    MODULE.validate(self.repository, require_gitlinks=False),
                )


if __name__ == "__main__":
    unittest.main()
