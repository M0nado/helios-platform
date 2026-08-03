from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location(
    "validate_repository_integrity",
    ROOT / "scripts/integrations/validate_repository_integrity.py",
)
validator = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(validator)


class RepositoryIntegrityTests(unittest.TestCase):
    def setUp(self):
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.repository_map = self.root / "repositories.json"
        self.gitmodules = self.root / ".gitmodules"
        self.document = {
            "schemaVersion": "1.0",
            "controlPlane": "Example/control",
            "canonicalPlatform": "Example/platform",
            "repositories": [
                {
                    "name": "Example/control",
                    "role": "control-plane",
                    "authority": ["policy"],
                },
                {
                    "name": "Example/platform",
                    "role": "canonical-platform",
                    "authority": ["product"],
                },
                {
                    "name": "Example/module",
                    "role": "module",
                    "authority": ["feature"],
                },
            ],
        }
        self.gitmodules.write_text(
            '[submodule "modules/module"]\n'
            "\tpath = modules/module\n"
            "\turl = https://github.com/Example/module.git\n",
            encoding="utf-8",
        )

    def tearDown(self):
        self.temporary_directory.cleanup()

    def validate(self):
        self.repository_map.write_text(json.dumps(self.document), encoding="utf-8")
        return validator.validate(
            self.repository_map,
            self.gitmodules,
            tracked_gitlinks={"modules/module"},
        )

    def test_accepts_consistent_repository_metadata(self):
        self.assertEqual(self.validate(), [])

    def test_rejects_duplicate_repository_names_and_roles(self):
        duplicate = dict(self.document["repositories"][2])
        duplicate["name"] = "example/MODULE"
        self.document["repositories"].append(duplicate)

        errors = self.validate()

        self.assertTrue(any("duplicates 'example/MODULE'" in error for error in errors))
        self.assertTrue(any("role duplicates 'module'" in error for error in errors))

    def test_rejects_submodule_missing_from_repository_map(self):
        self.gitmodules.write_text(
            '[submodule "modules/unknown"]\n'
            "\tpath = modules/unknown\n"
            "\turl = git@github.com:Example/unknown.git\n",
            encoding="utf-8",
        )

        self.assertTrue(any("absent from" in error for error in self.validate()))

    def test_rejects_invalid_authority_and_canonical_role(self):
        self.document["repositories"][1]["authority"] = []
        self.document["repositories"][1]["role"] = "other"

        errors = self.validate()

        self.assertTrue(any("authority must contain" in error for error in errors))
        self.assertTrue(any("role 'canonical-platform'" in error for error in errors))

    def test_rejects_malformed_json(self):
        self.repository_map.write_text("{", encoding="utf-8")
        errors = validator.validate(self.repository_map, self.gitmodules)
        self.assertTrue(any("cannot read valid JSON" in error for error in errors))

    def test_rejects_declared_submodule_without_tracked_gitlink(self):
        self.repository_map.write_text(json.dumps(self.document), encoding="utf-8")

        errors = validator.validate(
            self.repository_map,
            self.gitmodules,
            tracked_gitlinks=set(),
        )

        self.assertTrue(any("is not a tracked gitlink" in error for error in errors))

    def test_rejects_tracked_gitlink_without_submodule_declaration(self):
        self.repository_map.write_text(json.dumps(self.document), encoding="utf-8")

        errors = validator.validate(
            self.repository_map,
            self.gitmodules,
            tracked_gitlinks={"modules/module", "modules/undeclared"},
        )

        self.assertTrue(any("is not declared as a submodule" in error for error in errors))

    def test_reads_mode_160000_entries_from_git_index(self):
        completed = mock.Mock(
            stdout=(
                "100644 aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa 0\t.gitmodules\0"
                "160000 bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb 0\tmodules/module\0"
            )
        )

        with mock.patch.object(validator.subprocess, "run", return_value=completed) as run:
            errors = self.validate_without_supplied_gitlinks()

        self.assertEqual(errors, [])
        run.assert_called_once_with(
            ["git", "-C", str(self.gitmodules.parent), "ls-files", "--stage", "-z"],
            check=True,
            capture_output=True,
            text=True,
        )

    def validate_without_supplied_gitlinks(self):
        self.repository_map.write_text(json.dumps(self.document), encoding="utf-8")
        return validator.validate(self.repository_map, self.gitmodules)


if __name__ == "__main__":
    unittest.main()
