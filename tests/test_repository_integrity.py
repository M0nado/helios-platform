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
        self.repository_map = self.root / "config" / "integrations" / "repositories.json"
        self.gitmodules = self.root / ".gitmodules"
        self.repository_map.parent.mkdir(parents=True, exist_ok=True)
        self.document = {
            "schemaVersion": "1.0",
            "controlPlane": "Example/control",
            "canonicalPlatform": "Example/platform",
            "repositories": [
                {
                    "name": "Example/control",
                    "role": "control-plane",
                    "integrationMode": "contract-only",
                    "authority": ["policy"],
                },
                {
                    "name": "Example/platform",
                    "role": "canonical-platform",
                    "integrationMode": "canonical",
                    "authority": ["product"],
                },
                {
                    "name": "Example/module",
                    "role": "module",
                    "integrationMode": "pinned-submodule",
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

    def validate(self, links: dict[str, str] | None = None):
        self.repository_map.write_text(json.dumps(self.document), encoding="utf-8")
        if links is None:
            links = {"modules/module": "a" * 40}
        with mock.patch.object(validator, "gitlinks", return_value=links):
            return validator.validate(self.root)

    def test_accepts_consistent_repository_metadata(self):
        self.assertEqual(self.validate(), [])

    def test_rejects_duplicate_repository_names(self):
        duplicate = dict(self.document["repositories"][2])
        duplicate["name"] = "example/MODULE"
        self.document["repositories"].append(duplicate)

        self.assertTrue(any("duplicate names" in error for error in self.validate()))

    def test_rejects_unsupported_integration_mode(self):
        self.document["repositories"][2]["integrationMode"] = "unsupported"

        self.assertTrue(any("integrationMode unsupported" in error for error in self.validate()))

    def test_rejects_unsupported_schema_version(self):
        self.document["schemaVersion"] = "2.0"

        self.assertTrue(any("unsupported schemaVersion" in error for error in self.validate()))

    def test_rejects_non_matching_canonical_entry(self):
        self.document["canonicalPlatform"] = "Example/other"

        self.assertTrue(
            any("does not match canonicalPlatform" in error for error in self.validate())
        )

    def test_rejects_unapproved_submodule_repository(self):
        self.gitmodules.write_text(
            '[submodule "modules/unknown"]\n'
            "\tpath = modules/unknown\n"
            "\turl = https://github.com/Example/unknown.git\n",
            encoding="utf-8",
        )

        self.assertTrue(any("unapproved .gitmodules repository" in error for error in self.validate()))

    def test_rejects_declared_submodule_without_gitlink(self):
        self.assertTrue(
            any(
                "declared submodule has no 160000 gitlink" in error
                for error in self.validate(links={})
            )
        )

    def test_rejects_orphan_gitlink_without_declaration(self):
        self.assertTrue(
            any(
                "orphan gitlink not declared in .gitmodules" in error
                for error in self.validate(
                    links={
                        "modules/module": "a" * 40,
                        "modules/orphan": "b" * 40,
                    }
                )
            )
        )

    def test_rejects_invalid_gitlink_sha(self):
        self.assertTrue(
            any(
                "gitlink has invalid commit SHA" in error
                for error in self.validate(links={"modules/module": "deadbeef"})
            )
        )

    def test_github_name_normalizes_canonical_url(self):
        self.assertEqual(
            validator.github_name("https://github.com/M0nado/helios-ai-hub.git"),
            "m0nado/helios-ai-hub",
        )


if __name__ == "__main__":
    unittest.main()
