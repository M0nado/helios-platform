import importlib.util
import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


class KnowledgeDiscoveryTests(unittest.TestCase):
    def test_llms_txt_is_generated_from_language_profiles(self):
        path = ROOT / "scripts" / "knowledge" / "generate_llms_txt.py"
        spec = importlib.util.spec_from_file_location("generate_llms_txt", path)
        module = importlib.util.module_from_spec(spec)
        assert spec.loader
        spec.loader.exec_module(module)
        self.assertEqual((ROOT / "llms.txt").read_text(encoding="utf-8"), module.render())

    def test_capability_backlog_has_fifty_unique_governed_items(self):
        data = json.loads(
            (ROOT / "config" / "capabilities" / "major-capabilities.v1.json").read_text(encoding="utf-8")
        )
        items = data["capabilities"]
        self.assertEqual(50, len(items))
        self.assertEqual(50, len({item["id"] for item in items}))
        self.assertTrue(all(item["checks"] for item in items))


if __name__ == "__main__":
    unittest.main()
