from __future__ import annotations

import sys
import unittest
from pathlib import Path

SERVICE_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SERVICE_ROOT))

from helios_deployment_agent.policy import (  # noqa: E402
    Decision,
    contains_sensitive_key,
    contains_sensitive_text,
    evaluate_action,
    summarize_manifest,
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

    def test_sensitive_aliases_and_objective_shapes_are_rejected(self) -> None:
        self.assertTrue(contains_sensitive_key({"connectionString": "not-returned"}))
        self.assertTrue(contains_sensitive_key({"authorization": "not-returned"}))
        self.assertTrue(contains_sensitive_text("Authorization: " + "Bearer " + ("a" * 24)))
        self.assertTrue(contains_sensitive_text("api_key=abcdefghijklmnop"))

    def test_manifest_summary_never_reproduces_repository_or_branch_names(self) -> None:
        summary = summarize_manifest(
            {
                "repositories": ["private-owner/private-repository"],
                "branches": ["customer/private-feature"],
                "languages": ["Python", "unknown"],
                "proposed_actions": ["compare_branches", "invent-secret-action"],
            }
        )
        rendered = str(summary)
        self.assertNotIn("private-owner", rendered)
        self.assertNotIn("customer/private-feature", rendered)
        self.assertNotIn("invent-secret-action", rendered)
        self.assertEqual(["compare_branches"], summary["proposed_actions"])
        self.assertEqual(1, summary["unknown_action_count"])


if __name__ == "__main__":
    unittest.main()
