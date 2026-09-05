from __future__ import annotations

import sys
import unittest
from pathlib import Path


CONTRACTS_DIR = Path(__file__).resolve().parents[1]
if str(CONTRACTS_DIR) not in sys.path:
    sys.path.insert(0, str(CONTRACTS_DIR))

from validate_monadoblade_profile_bundle import find_secret_patterns  # noqa: E402


class ProfileBundleSecretPatternTests(unittest.TestCase):
    def test_storage_action_id_is_not_an_openai_key(self) -> None:
        self.assertEqual([], find_secret_patterns("disk-or-vhdx-apply-from-runtime"))

    def test_embedded_disk_prefix_is_not_an_openai_key(self) -> None:
        self.assertEqual([], find_secret_patterns("task-runner-disk-scan"))

    def test_project_key_shape_is_detected(self) -> None:
        value = "OPENAI_API_KEY=" + "sk-proj-" + ("A" * 32)
        self.assertIn("openai-api-key", find_secret_patterns(value))

    def test_legacy_key_shape_is_detected(self) -> None:
        value = "key=" + "sk-" + ("B" * 48)
        self.assertIn("openai-api-key", find_secret_patterns(value))

    def test_github_pat_shape_is_detected(self) -> None:
        value = "github_pat_" + ("C" * 40)
        self.assertIn("github-personal-access-token", find_secret_patterns(value))

    def test_assigned_secret_value_is_detected(self) -> None:
        value = 'client_secret="not-a-real-secret-value"'
        self.assertIn("assigned-secret-value", find_secret_patterns(value))


if __name__ == "__main__":
    unittest.main()
