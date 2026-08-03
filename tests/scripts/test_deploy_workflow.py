import re
import unittest
from pathlib import Path


class DeploymentWorkflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = Path(".github/workflows/deploy.yml").read_text(encoding="utf-8")

    def job(self, name):
        match = re.search(
            rf"^  {re.escape(name)}:\n(?P<body>.*?)(?=^  [a-z][a-z-]*:\n|\Z)",
            self.workflow,
            flags=re.MULTILINE | re.DOTALL,
        )
        self.assertIsNotNone(match, f"missing {name} job")
        return match.group("body")

    def test_manual_phases_include_every_prerequisite(self):
        expected_phases = {
            "infrastructure": [
                "infrastructure",
                "agents",
                "ai-services",
                "security",
                "monitoring",
                "verification",
                "all",
            ],
            "agents": [
                "agents",
                "ai-services",
                "security",
                "monitoring",
                "verification",
                "all",
            ],
            "ai-services": [
                "ai-services",
                "security",
                "monitoring",
                "verification",
                "all",
            ],
            "security": ["security", "monitoring", "verification", "all"],
            "monitoring": ["monitoring", "verification", "all"],
            "verification": ["verification", "all"],
        }

        for job, phases in expected_phases.items():
            with self.subTest(job=job):
                encoded_phases = ",".join(f'"{phase}"' for phase in phases)
                self.assertIn(f"fromJSON('[{encoded_phases}]')", self.job(job))

    def test_only_infrastructure_can_request_oidc_token(self):
        self.assertEqual(1, self.workflow.count("id-token: write"))
        self.assertIn("id-token: write", self.job("infrastructure"))

    def test_actions_are_pinned_to_commit_shas(self):
        action_references = re.findall(r"^\s+- uses: ([^\s#]+)", self.workflow, re.MULTILINE)
        self.assertGreater(len(action_references), 0)
        for reference in action_references:
            with self.subTest(reference=reference):
                self.assertRegex(reference, r"^[^@]+@[0-9a-f]{40}$")


if __name__ == "__main__":
    unittest.main()
