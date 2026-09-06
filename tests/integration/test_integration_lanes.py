import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
INSPECTOR = ROOT / "eng/integration-lanes/validate_release_artifacts.py"

class ReleaseArtifactTests(unittest.TestCase):
    def run_inspector(self, path: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run([sys.executable, str(INSPECTOR), str(path)], text=True, capture_output=True)

    def test_stable_artifact_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            artifact = Path(directory) / "helios-1.0.0.zip"
            with zipfile.ZipFile(artifact, "w") as archive:
                archive.writestr("HELIOS.Platform.dll", b"stable")
            self.assertEqual(0, self.run_inspector(artifact).returncode)

    def test_nested_preview_artifact_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            artifact = Path(directory) / "helios-1.0.0.nupkg"
            with zipfile.ZipFile(artifact, "w") as archive:
                archive.writestr("eng/integration-lanes/preview/sdk.dll", b"preview")
            result = self.run_inspector(artifact)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("Preview content", result.stdout)

if __name__ == "__main__":
    unittest.main()
