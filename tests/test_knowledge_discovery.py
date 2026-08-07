import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


class KnowledgeDiscoveryTests(unittest.TestCase):
    @staticmethod
    def load_module(name):
        path = ROOT / "scripts" / "knowledge" / f"{name}.py"
        spec = importlib.util.spec_from_file_location(name, path)
        module = importlib.util.module_from_spec(spec)
        assert spec.loader
        spec.loader.exec_module(module)
        return module

    def test_llms_txt_is_generated_from_language_profiles(self):
        module = self.load_module("generate_llms_txt")
        self.assertEqual((ROOT / "llms.txt").read_text(encoding="utf-8"), module.render())

    def test_capability_backlog_has_fifty_unique_governed_items(self):
        data = json.loads(
            (ROOT / "config" / "capabilities" / "major-capabilities.v1.json").read_text(encoding="utf-8")
        )
        items = data["capabilities"]
        self.assertEqual(50, len(items))
        self.assertEqual(50, len({item["id"] for item in items}))
        self.assertTrue(all(len(item["acceptanceCriteria"]) >= 2 for item in items))

    def test_capability_graph_is_valid_and_acyclic(self):
        validator = self.load_module("validate_capabilities")
        data = json.loads(validator.PATH.read_text(encoding="utf-8"))
        self.assertEqual([], validator.validate(data))
        data["capabilities"][0]["dependencies"] = [data["capabilities"][0]["id"]]
        self.assertIn("cannot depend on itself", "\n".join(validator.validate(data)))

    def test_issue_packets_are_safe_reviewable_markdown(self):
        planner = self.load_module("plan_capability_issues")
        data = json.loads(planner.BACKLOG.read_text(encoding="utf-8"))
        packets = planner.render_packets(data, "azure")
        self.assertEqual(5, len(packets))
        self.assertTrue(all("## Acceptance criteria" in body for _, body in packets))
        self.assertTrue(all("Safety gate:" in body for _, body in packets))
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / packets[0][0]
            output.write_text(packets[0][1], encoding="utf-8")
            self.assertTrue(output.is_file())


if __name__ == "__main__":
    unittest.main()
