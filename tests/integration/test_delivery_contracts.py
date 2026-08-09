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

    def test_deployment_unblock_sequence_is_ordered_and_fail_closed(self):
        policy = json.loads(
            (ROOT / "config/integrations/delivery-policy.json").read_text(encoding="utf-8")
        )
        deployment = policy["deployment"]
        self.assertTrue(deployment["failClosed"])
        self.assertEqual(
            deployment["unblockSequence"],
            [
                "exact-sha-build-and-tests",
                "reviewed-current-infrastructure-plan",
                "inspected-and-attested-immutable-artifact",
                "protected-least-privilege-identity",
                "independent-exact-sha-approval",
                "protected-merge",
                "immediate-preflight-revalidation",
                "digest-bound-deployment",
                "provider-observed-verification",
                "proven-rollback",
                "revocation-and-cleanup",
                "zero-blocker-terminal-proof",
            ],
        )


if __name__ == "__main__":
    unittest.main()
