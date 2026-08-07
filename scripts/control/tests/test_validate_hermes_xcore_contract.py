from __future__ import annotations

import copy
import json
import sys
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts" / "control"))

from validate_hermes_xcore_contract import validate_contracts  # noqa: E402


ENVIRONMENT_CONTRACT = (
    REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_ENVIRONMENT_BINDINGS_V1.json"
)
CAPABILITY_CONTRACT = (
    REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_CAPABILITY_BINDINGS_V1.json"
)
EVENT_CONTRACT = REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_EVENT_PROFILE_V1.json"
APPROVAL_CONTRACT = (
    REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_APPROVAL_GOVERNANCE_V1.json"
)


class HermesXcoreContractValidationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.environment = json.loads(ENVIRONMENT_CONTRACT.read_text(encoding="utf-8"))
        self.capability = json.loads(CAPABILITY_CONTRACT.read_text(encoding="utf-8"))
        self.event = json.loads(EVENT_CONTRACT.read_text(encoding="utf-8"))
        self.approval = json.loads(APPROVAL_CONTRACT.read_text(encoding="utf-8"))

    def assert_has_error(
        self,
        environment: dict,
        capability: dict,
        event: dict,
        approval: dict,
        text: str,
    ) -> None:
        errors = validate_contracts(environment, capability, event, approval)
        self.assertTrue(
            any(text in error for error in errors),
            f"expected an error containing {text!r}; got {errors!r}",
        )

    def test_canonical_contracts_are_valid(self) -> None:
        self.assertEqual(
            validate_contracts(
                self.environment,
                self.capability,
                self.event,
                self.approval,
            ),
            [],
        )

    def test_missing_canonical_environment_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.environment)
        candidate["environments"] = [
            entry for entry in candidate["environments"] if entry["name"] != "x-tier-xcore"
        ]
        self.assert_has_error(
            candidate,
            self.capability,
            self.event,
            self.approval,
            "canonical environments must be exactly",
        )

    def test_environment_tier_binding_mismatch_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.environment)
        candidate["environments"][0]["tier"] = "production"
        self.assert_has_error(
            candidate,
            self.capability,
            self.event,
            self.approval,
            "must bind x-tier-dev to tier development",
        )

    def test_non_authoritative_surface_deploy_permission_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.capability)
        teams_surface = next(
            entry for entry in candidate["surfaces"] if entry["name"] == "teams"
        )
        teams_surface["canDeploy"] = True
        self.assert_has_error(
            self.environment,
            candidate,
            self.event,
            self.approval,
            "teams must not deploy",
        )

    def test_undeclared_surface_authority_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.capability)
        candidate["surfaces"].append(
            {
                "name": "rogue-surface",
                "canRequest": True,
                "canApprove": False,
                "canDeploy": True,
                "canExecuteApprovalWorkflow": True,
                "notes": "Unexpected deployment surface.",
            }
        )
        self.assert_has_error(
            self.environment,
            candidate,
            self.event,
            self.approval,
            "rogue-surface deployment authority is not declared",
        )

    def test_duplicate_surface_name_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.capability)
        candidate["surfaces"].append(copy.deepcopy(candidate["surfaces"][0]))
        self.assert_has_error(
            self.environment,
            candidate,
            self.event,
            self.approval,
            "duplicates existing surface",
        )

    def test_hotfix_auto_deploy_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.approval)
        candidate["hotfix"]["autoDeploy"] = True
        self.assert_has_error(
            self.environment,
            self.capability,
            self.event,
            candidate,
            "hotfix autoDeploy must be false",
        )

    def test_xcore_rebuilt_artifact_promotion_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.approval)
        candidate["xcorePromotionPolicy"]["allowRebuiltArtifactPromotion"] = True
        self.assert_has_error(
            self.environment,
            self.capability,
            self.event,
            candidate,
            "reject rebuilt artifact promotion",
        )

    def test_delivery_without_idempotency_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.event)
        candidate["delivery"]["idempotency"]["required"] = False
        self.assert_has_error(
            self.environment,
            self.capability,
            candidate,
            self.approval,
            "idempotency.required must be true",
        )

    def test_event_profile_rejects_legacy_evidence_links_field(self) -> None:
        candidate = copy.deepcopy(self.event)
        required_fields = candidate["eventEnvelope"]["requiredFields"]
        required_fields.remove("links")
        required_fields.append("evidenceLinks")
        self.assert_has_error(
            self.environment,
            self.capability,
            candidate,
            self.approval,
            "use links instead of evidenceLinks",
        )

    def test_event_profile_requires_payload_field(self) -> None:
        candidate = copy.deepcopy(self.event)
        candidate["eventEnvelope"]["requiredFields"].remove("payload")
        self.assert_has_error(
            self.environment,
            self.capability,
            candidate,
            self.approval,
            "requiredFields missing payload",
        )


if __name__ == "__main__":
    unittest.main()
