import importlib.util
import json
import io
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock
from contextlib import redirect_stdout


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
                self.assertEqual(self.secret.tracked_files(), ([tracked], "manifest"))
            with mock.patch.object(self.apply_gate, "ROOT", root):
                self.assertEqual(self.apply_gate.tracked_files(), ([tracked], "manifest"))

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
                self.assertIn(secret_file, self.secret.tracked_files()[0])
            with mock.patch.object(self.apply_gate, "ROOT", root):
                self.assertEqual(self.apply_gate.tracked_files(), ([gate_file], "filesystem"))

    def run_preflight(self, module, root):
        report = root / "report.json"
        markdown = root / "report.md"
        with mock.patch.multiple(module, ROOT=root, OUT=report, MD=markdown, ALLOWLIST=root / "missing.json"), \
             mock.patch.object(sys, "argv", ["preflight"]), redirect_stdout(io.StringIO()) as output:
            result = module.main()
        return result, json.loads(report.read_text()), output.getvalue()

    def test_clean_manifest_scan_reports_positive_coverage(self):
        with tempfile.TemporaryDirectory() as directory, self.git_failure:
            root = Path(directory)
            path = root / "scripts" / "safe.sh"
            path.parent.mkdir()
            path.write_text("echo safe\n")
            (root / ".helios-tracked-files").write_text("scripts/safe.sh\n")

            for module in (self.secret, self.apply_gate):
                result, report, output = self.run_preflight(module, root)
                self.assertEqual(result, 0)
                self.assertTrue(report["ok"])
                self.assertEqual(report["discoverySource"], "manifest")
                self.assertEqual(report["scannedFileCount"], 1)
                self.assertIn("1 files scanned via manifest", output)

    def test_empty_manifest_cannot_pass(self):
        with tempfile.TemporaryDirectory() as directory, self.git_failure:
            root = Path(directory)
            (root / ".helios-tracked-files").write_text("")

            for module in (self.secret, self.apply_gate):
                result, report, output = self.run_preflight(module, root)
                self.assertEqual(result, 1)
                self.assertFalse(report["ok"])
                self.assertEqual(report["scannedFileCount"], 0)
                self.assertIn("ERROR", output)


if __name__ == "__main__":
    unittest.main()
