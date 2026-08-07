import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest import mock


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]


def load_script(name):
    path = REPOSITORY_ROOT / "scripts" / "security" / name
    spec = importlib.util.spec_from_file_location(name.removesuffix(".py"), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class PreflightFileDiscoveryTests(unittest.TestCase):
    def setUp(self):
        self.secret = load_script("secret_preflight.py")
        self.apply_gate = load_script("apply_gate_preflight.py")
        self.git_failure = mock.patch.object(
            subprocess,
            "run",
            return_value=subprocess.CompletedProcess([], 128, "", "not a git repository"),
        )

    def test_packaged_manifest_is_used_without_git_metadata(self):
        with tempfile.TemporaryDirectory() as directory, self.git_failure:
            root = Path(directory)
            tracked = root / "scripts" / "check.sh"
            tracked.parent.mkdir()
            tracked.write_text("echo checked\n")
            (root / ".helios-tracked-files").write_text(
                "scripts/check.sh\n../outside.txt\n"
            )

            with mock.patch.object(self.secret, "ROOT", root):
                self.assertEqual(self.secret.tracked_files(), [tracked])
            with mock.patch.object(self.apply_gate, "ROOT", root):
                self.assertEqual(self.apply_gate.tracked_files(), [tracked])

    def test_snapshot_files_are_enumerated_when_manifest_is_missing(self):
        with tempfile.TemporaryDirectory() as directory, self.git_failure:
            root = Path(directory)
            secret_file = root / "config" / "settings.json"
            gate_file = root / "infra" / "deploy.bicep"
            secret_file.parent.mkdir()
            gate_file.parent.mkdir()
            secret_file.write_text("{}\n")
            gate_file.write_text("resource example 'test@1' = {}\n")

            with mock.patch.object(self.secret, "ROOT", root):
                self.assertIn(secret_file, self.secret.tracked_files())
            with mock.patch.object(self.apply_gate, "ROOT", root):
                self.assertEqual(self.apply_gate.tracked_files(), [gate_file])


if __name__ == "__main__":
    unittest.main()
