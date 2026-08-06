import copy
import json
import unittest
from pathlib import Path

from scripts.integrations.validate_slack_znc_config import validate


class SlackZncConfigurationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.config = json.loads(
            Path("config/integrations/slack-znc.example.json").read_text(encoding="utf-8")
        )

    def test_example_is_safe(self):
        self.assertEqual([], validate(self.config))

    def test_rejects_slack_as_execution_source(self):
        config = copy.deepcopy(self.config)
        config["slack"]["inboundExecutionAllowed"] = True
        self.assertIn("Slack must never be an execution source", validate(config))

    def test_rejects_embedded_slack_token(self):
        config = copy.deepcopy(self.config)
        config["slack"]["tokenSecretName"] = "xoxb-secret"
        self.assertTrue(any("tokenSecretName" in error for error in validate(config)))


if __name__ == "__main__":
    unittest.main()
