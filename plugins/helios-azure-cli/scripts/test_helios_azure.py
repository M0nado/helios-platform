import importlib.util
from pathlib import Path
import unittest


MODULE_PATH = Path(__file__).with_name("helios_azure.py")
SPEC = importlib.util.spec_from_file_location("helios_azure", MODULE_PATH)
assert SPEC and SPEC.loader
helios_azure = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(helios_azure)


class HeliosAzureTests(unittest.TestCase):
    def test_canonical_targets(self):
        targets = helios_azure.load_targets()
        self.assertEqual(targets["github"]["repository"], "M0nado/helios-platform")
        self.assertEqual(targets["linear"]["projectName"], "Helios Integration Fabric")
        self.assertEqual(targets["linear"]["implementationIssue"], "JOH-35")
        self.assertEqual(targets["slack"]["controlPlane"]["channelId"], "C0BHWDBHG1W")
        self.assertEqual(targets["slack"]["announcements"]["channelId"], "C0B8PU2RGLF")
        self.assertEqual(
            targets["azure"]["federationSubject"],
            "repo:M0nado/helios-platform:environment:azure-dev",
        )

    def test_mutation_guard_rejects_missing_execute(self):
        with self.assertRaises(helios_azure.HeliosAzureError):
            helios_azure.require_confirmation(False, helios_azure.OIDC_CONFIRMATION, helios_azure.OIDC_CONFIRMATION)

    def test_mutation_guard_rejects_wrong_phrase(self):
        with self.assertRaises(helios_azure.HeliosAzureError):
            helios_azure.require_confirmation(True, "wrong", helios_azure.DEPLOY_CONFIRMATION)

    def test_mutation_guard_accepts_exact_phrase(self):
        helios_azure.require_confirmation(True, helios_azure.DEPLOY_CONFIRMATION, helios_azure.DEPLOY_CONFIRMATION)


if __name__ == "__main__":
    unittest.main()
