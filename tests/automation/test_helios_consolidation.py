import importlib.util
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

MODULE_PATH = Path(__file__).resolve().parents[2] / "scripts" / "automation" / "helios_consolidation.py"
spec = importlib.util.spec_from_file_location("helios_consolidation", MODULE_PATH)
helios = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = helios
assert spec.loader is not None
spec.loader.exec_module(helios)


class ManifestValidationTests(unittest.TestCase):
    def make_source(self, **overrides):
        data = {
            "id": "helios-platform",
            "kind": "current_repository",
            "remote": "origin",
            "url": "https://example.invalid/repo.git",
            "branch": "work",
            "order": 0,
            "mode": "history",
            "path": ".",
            "areas": ("integration_baseline",),
        }
        data.update(overrides)
        return helios.Source(**data)

    def test_validate_manifest_accepts_known_priority_sources(self):
        source = self.make_source()
        helios.validate_manifest({"priority_sources": [source.id]}, [source])

    def test_validate_manifest_rejects_duplicate_ids_and_unknown_areas(self):
        source = self.make_source(areas=("unknown_area",))
        duplicate = self.make_source(order=1)
        with self.assertRaises(helios.CommandError) as context:
            helios.validate_manifest({}, [source, duplicate])
        message = str(context.exception)
        self.assertIn("duplicate source id", message)
        self.assertIn("unknown area", message)

    def test_build_test_commands_discovers_dotnet_projects(self):
        with TemporaryDirectory() as temp_dir:
            repo = Path(temp_dir)
            (repo / "HELIOS.Platform.csproj").write_text("<Project />", encoding="utf-8")
            commands = helios.build_test_commands(repo)
        self.assertIn("dotnet restore HELIOS.Platform.csproj -p:Configuration=Release", commands)
        self.assertIn("dotnet build HELIOS.Platform.csproj --no-restore -m --configuration Release", commands)
        self.assertIn("dotnet test HELIOS.Platform.csproj --no-build --configuration Release --verbosity normal", commands)


if __name__ == "__main__":
    unittest.main()
