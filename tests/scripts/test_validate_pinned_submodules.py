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
    def test_non_boolean_approval_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            manifest = Path(directory) / "approved-submodules.json"
            manifest.write_text(
                json.dumps({"approved": "false", "submodules": [{"path": "modules/example"}]}),
                encoding="utf-8",
            )
            original_manifest = VALIDATOR.MANIFEST
            VALIDATOR.MANIFEST = manifest
            try:
                output = io.StringIO()
                with redirect_stdout(output):
                    result = VALIDATOR.main()
            finally:
                VALIDATOR.MANIFEST = original_manifest

        self.assertEqual(2, result)
        self.assertIn("manifest is not approved", output.getvalue())


if __name__ == "__main__":
    unittest.main()
