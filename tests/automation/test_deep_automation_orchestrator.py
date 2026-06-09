import importlib.util
import sys
import tempfile
import unittest
from argparse import Namespace
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "automation" / "deep_automation_orchestrator.py"

spec = importlib.util.spec_from_file_location("deep_automation_orchestrator", SCRIPT)
orchestrator = importlib.util.module_from_spec(spec)
assert spec.loader is not None
sys.modules[spec.name] = orchestrator
spec.loader.exec_module(orchestrator)


class DeepAutomationOrchestratorTests(unittest.TestCase):
    def test_dotnet_inventory_discovers_build_candidates(self) -> None:
        inventory = orchestrator.dotnet_inventory()

        self.assertGreater(inventory["project_count"], 0)
        self.assertIn(
            "src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj",
            inventory["recommended_restore_build_order"],
        )
        self.assertIn(
            "src/core/HELIOS.Platform/HELIOS.Platform.csproj",
            inventory["recommended_restore_build_order"],
        )
        self.assertIn("dotnet build", inventory["linux_safe_build_command"])

    def test_automerge_readiness_blocks_missing_focus_branches(self) -> None:
        git = {"missing_focus_branches": ["helios-control"]}
        github_actions = {
            "workflows": [f".github/workflows/{name}" for name in orchestrator.AUTOMERGE_REQUIRED_WORKFLOWS],
            "triggers": {
                name: list(orchestrator.AUTOMERGE_REQUIRED_TRIGGERS)
                for name in orchestrator.AUTOMERGE_REQUIRED_WORKFLOWS
            },
        }
        dotnet = {"recommended_restore_build_order": ["src/core/HELIOS.Platform/HELIOS.Platform.csproj"]}

        readiness = orchestrator.automerge_readiness_inventory(git, github_actions, dotnet)

        self.assertFalse(readiness["safe_automerge_enabled"])
        self.assertIn("focus branches/remotes are missing", readiness["blockers"])

    def test_build_report_contains_ci_and_automerge_sections(self) -> None:
        report = orchestrator.build_report(Namespace(mode="ci"))

        self.assertIn("dotnet_build_test", report)
        self.assertIn("automerge_readiness", report)
        self.assertIn("azure_github_cli", report)

        with tempfile.TemporaryDirectory() as temp_dir:
            output = Path(temp_dir) / "report.md"
            orchestrator.write_markdown(report, output)
            markdown = output.read_text(encoding="utf-8")

        self.assertIn("## .NET build and test plan", markdown)
        self.assertIn("Safe automerge ready", markdown)


if __name__ == "__main__":
    unittest.main()
