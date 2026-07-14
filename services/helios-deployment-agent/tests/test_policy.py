from __future__ import annotations

import sys
import unittest
from pathlib import Path

SERVICE_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SERVICE_ROOT))

from helios_deployment_agent.policy import (  # noqa: E402
    Decision,
    contains_sensitive_key,
    evaluate_action,
)


class PolicyTests(unittest.TestCase):
    def test_read_only_action_can_be_planned(self) -> None:
        verdict = evaluate_action("compare-branches")
        self.assertEqual(Decision.ALLOW_PLAN, verdict.decision)
        self.assertFalse(verdict.execution_permitted)

    def test_merge_requires_all_controls(self) -> None:
        verdict = evaluate_action("merge_pull_request", {"human_approval": True})
        self.assertEqual(Decision.REQUIRES_CONTROLS, verdict.decision)
        self.assertEqual(("required_checks_green", "rollback_plan"), verdict.missing_controls)

    def test_production_plan_requires_protected_oidc_path(self) -> None:
        controls = {
            "human_approval": True,
            "required_checks_green": True,
            "oidc_federation": True,
            "protected_environment": True,
            "rollback_plan": True,
        }
        verdict = evaluate_action("deploy_production", controls)
        self.assertEqual(Decision.ALLOW_PLAN, verdict.decision)
        self.assertFalse(verdict.execution_permitted)

    def test_secret_readback_is_never_planned(self) -> None:
        verdict = evaluate_action("read_secret_value", {"human_approval": True})
        self.assertEqual(Decision.DENY, verdict.decision)

    def test_unknown_action_defaults_to_review(self) -> None:
        verdict = evaluate_action("invent_new_cloud_admin_mode")
        self.assertEqual(Decision.REQUIRES_CONTROLS, verdict.decision)
        self.assertIn("documented_policy", verdict.missing_controls)

    def test_nested_sensitive_field_is_detected_before_prompting(self) -> None:
        self.assertTrue(contains_sensitive_key({"azure": {"client_secret": "not-returned"}}))
        self.assertFalse(contains_sensitive_key({"azure": {"client_id": "non-secret"}}))


if __name__ == "__main__":
    unittest.main()
