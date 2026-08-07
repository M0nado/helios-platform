import importlib.util
import io
import json
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[2] / "scripts/integrations/validate_pinned_submodules.py"
SPEC = importlib.util.spec_from_file_location("validate_pinned_submodules", SCRIPT)
VALIDATOR = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(VALIDATOR)


class PinnedSubmoduleApprovalTests(unittest.TestCase):
    def _entry(self, commit: str = "a" * 40, **overrides: str) -> dict[str, str]:
        entry = {
            "path": "modules/example",
            "url": "https://github.com/M0nado/example.git",
            "commit": commit,
            "evidenceUrl": "https://github.com/M0nado/helios-platform/issues/1",
            "ownershipDecision": "Approved by platform owner",
            "licenseDecision": "MIT allowed",
            "dependencySecurityReview": "No critical CVEs",
            "contractEvidence": "https://github.com/M0nado/helios-platform/actions/runs/1",
            "buildEvidence": "https://github.com/M0nado/helios-platform/actions/runs/2",
            "testEvidence": "https://github.com/M0nado/helios-platform/actions/runs/3",
        }
        entry.update(overrides)
        return entry

    def _write_schema(self, root: Path) -> Path:
        schema = {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "$id": VALIDATOR.EXPECTED_SCHEMA_ID,
            "type": "object",
            "required": ["approved", "submodules"],
            "properties": {
                "submodules": {
                    "items": {
                        "required": list(VALIDATOR.SUBMODULE_FIELDS),
                    }
                }
            },
        }
        schema_path = root / "config/integrations/approved-submodules.schema.json"
        schema_path.parent.mkdir(parents=True, exist_ok=True)
        schema_path.write_text(json.dumps(schema), encoding="utf-8")
        return schema_path

    def _run_validator(
        self,
        root: Path,
        manifest_payload: dict,
        git_outputs: dict[tuple[str, ...], str] | None = None,
        git_errors: dict[tuple[str, ...], str] | None = None,
    ) -> tuple[int, str]:
        manifest = root / "config/integrations/approved-submodules.json"
        manifest.parent.mkdir(parents=True, exist_ok=True)
        manifest.write_text(json.dumps(manifest_payload), encoding="utf-8")
        schema = self._write_schema(root)
        git_outputs = git_outputs or {}
        git_errors = git_errors or {}

        def fake_git(*args: str) -> str:
            key = tuple(args)
            if key in git_errors:
                raise VALIDATOR.GitCommandError(git_errors[key])
            if key in git_outputs:
                return git_outputs[key]
            raise AssertionError(f"Unexpected git command: {key}")

        original_root = VALIDATOR.ROOT
        original_manifest = VALIDATOR.MANIFEST
        original_schema = VALIDATOR.MANIFEST_SCHEMA
        original_git = VALIDATOR.git
        VALIDATOR.ROOT = root
        VALIDATOR.MANIFEST = manifest
        VALIDATOR.MANIFEST_SCHEMA = schema
        VALIDATOR.git = fake_git
        try:
            output = io.StringIO()
            with redirect_stdout(output):
                result = VALIDATOR.main()
        finally:
            VALIDATOR.ROOT = original_root
            VALIDATOR.MANIFEST = original_manifest
            VALIDATOR.MANIFEST_SCHEMA = original_schema
            VALIDATOR.git = original_git
        return result, output.getvalue()

    def _git_outputs(
        self,
        root: Path,
        entry: dict[str, str],
        *,
        configured_url: str | None = None,
        staged_commit: str | None = None,
        staged_mode: str = "160000",
        head_commit: str | None = None,
        extra_paths: str = "",
        extra_staged: str = "",
        status_output: str = "",
    ) -> dict[tuple[str, ...], str]:
        configured_url = configured_url or entry["url"]
        staged_commit = staged_commit or entry["commit"]
        head_commit = head_commit or entry["commit"]
        module = str(root / entry["path"])
        return {
            ("config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$"): (
                "submodule.sample.path modules/example" + extra_paths
            ),
            ("config", "-f", ".gitmodules", "submodule.sample.url"): configured_url,
            ("ls-files", "--stage"): f"{staged_mode} {staged_commit} 0\tmodules/example{extra_staged}",
            ("-C", module, "rev-parse", "HEAD"): head_commit,
            ("-C", module, "status", "--porcelain", "--untracked-files=all"): status_output,
            ("submodule", "foreach", "--recursive", "git fsck --full"): "",
        }

    def test_non_boolean_approval_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            result, output = self._run_validator(
                root, {"approved": "false", "submodules": [self._entry()]}
            )

        self.assertEqual(2, result)
        self.assertIn("approved must be boolean true", output)

    def test_missing_evidence_field_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            del entry["contractEvidence"]
            result, output = self._run_validator(root, {"approved": True, "submodules": [entry]})

        self.assertEqual(2, result)
        self.assertIn("missing required fields: contractEvidence", output)

    def test_valid_manifest_and_git_state_pass(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(root, entry),
            )

        self.assertEqual(0, result)
        self.assertIn("PASS: 1 approved gitlinks", output)

    def test_extra_unapproved_gitlink_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            outputs = self._git_outputs(
                root,
                entry,
                extra_paths="\nsubmodule.extra.path modules/extra",
            )
            outputs[("config", "-f", ".gitmodules", "submodule.extra.url")] = (
                "https://github.com/M0nado/extra.git"
            )
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=outputs,
            )

        self.assertEqual(1, result)
        self.assertIn("approved paths do not exactly match .gitmodules", output)

    def test_url_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(
                    root,
                    entry,
                    configured_url="https://github.com/M0nado/other.git",
                ),
            )

        self.assertEqual(1, result)
        self.assertIn("modules/example: URL differs from .gitmodules", output)

    def test_extra_indexed_gitlink_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(
                    root,
                    entry,
                    extra_staged=f"\n160000 {'d' * 40} 0\tmodules/rogue",
                ),
            )

        self.assertEqual(1, result)
        self.assertIn("indexed gitlink paths do not exactly match approved manifest", output)

    def test_gitlink_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(root, entry, staged_commit="b" * 40),
            )

        self.assertEqual(1, result)
        self.assertIn("modules/example: index is not mode 160000 at approved commit", output)

    def test_uninitialized_worktree_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(root, entry),
            )

        self.assertEqual(1, result)
        self.assertIn("modules/example: submodule worktree is not initialized", output)

    def test_checked_out_commit_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(root, entry, head_commit="c" * 40),
            )

        self.assertEqual(1, result)
        self.assertIn("modules/example: checked-out commit differs from approval", output)

    def test_dirty_worktree_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(root, entry, status_output=" M local-change\n"),
            )

        self.assertEqual(1, result)
        self.assertIn("modules/example: submodule worktree is dirty", output)

    def test_recursive_fsck_failure_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            entry = self._entry()
            module = root / entry["path"]
            module.mkdir(parents=True)
            (module / ".git").write_text("gitdir: ../../.git/modules/example\n", encoding="utf-8")
            result, output = self._run_validator(
                root,
                {"approved": True, "submodules": [entry]},
                git_outputs=self._git_outputs(root, entry),
                git_errors={
                    ("submodule", "foreach", "--recursive", "git fsck --full"): "object corrupted"
                },
            )

        self.assertEqual(1, result)
        self.assertIn("recursive object validation failed", output)


if __name__ == "__main__":
    unittest.main()
