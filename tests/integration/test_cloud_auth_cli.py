import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
AUTH = ROOT / "scripts/setup/helios-auth"


class CloudAuthCliTests(unittest.TestCase):
    def test_help_exposes_review_gated_github_pr_command(self):
        result = subprocess.run(
            [str(AUTH), "--help"],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("github create-pr", result.stdout)
        self.assertIn("--publish", result.stdout)

    def test_github_pr_creation_requires_explicit_publish(self):
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8") as body:
            body.write("## Summary\n\nContract test.\n")
            body.flush()
            result = subprocess.run(
                [
                    str(AUTH),
                    "github",
                    "create-pr",
                    "--issue",
                    "123",
                    "--title",
                    "Contract test",
                    "--body-file",
                    body.name,
                ],
                cwd=ROOT,
                check=False,
                capture_output=True,
                text=True,
            )
        self.assertEqual(result.returncode, 2)
        self.assertIn("explicit --publish", result.stderr)


if __name__ == "__main__":
    unittest.main()
