import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


class DeliveryContractTests(unittest.TestCase):
    def test_delivery_validator(self):
        result = subprocess.run(
            ["python3", "eng/automation/validate_delivery.py"],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_profile_contains_no_secret_values(self):
        profile = json.loads(
            (ROOT / "config/integrations/identity-profile.json").read_text(encoding="utf-8")
        )
        serialized = json.dumps(profile).lower()
        for forbidden in ("clientsecret", "privatekey", "access_token", "pat"):
            self.assertNotIn(forbidden, serialized)


if __name__ == "__main__":
    unittest.main()
