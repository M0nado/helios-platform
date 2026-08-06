from __future__ import annotations

import copy
import json
import sys
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts" / "control"))

from validate_automation_routes import validate_registry  # noqa: E402


REGISTRY_PATH = REPOSITORY_ROOT / "config" / "HELIOS_AUTOMATION_ROUTES_V1.json"


class AutomationRouteValidationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.registry = json.loads(REGISTRY_PATH.read_text(encoding="utf-8"))

    def assert_has_error(self, registry: dict, text: str) -> None:
        errors = validate_registry(registry)
        self.assertTrue(
            any(text in error for error in errors),
            f"expected an error containing {text!r}; got {errors!r}",
        )

    def test_canonical_registry_is_valid(self) -> None:
        self.assertEqual(validate_registry(self.registry), [])

    def test_unknown_trigger_provider_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["routes"][0]["trigger"]["provider"] = "unregistered"
        self.assert_has_error(candidate, "unknown provider")

    def test_unknown_operation_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["routes"][0]["steps"][0]["operation"] = "unknown.execute"
        self.assert_has_error(candidate, "unknown operation")

    def test_unknown_approval_classes_fail_closed(self) -> None:
        route_candidate = copy.deepcopy(self.registry)
        route_candidate["routes"][0]["approvalClass"] = 5
        self.assert_has_error(route_candidate, "unknown approval class")

        step_candidate = copy.deepcopy(self.registry)
        step_candidate["routes"][0]["steps"][0]["approvalClass"] = -1
        self.assert_has_error(step_candidate, "unknown approval class")

    def test_enabled_route_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["routes"][0]["enabled"] = True
        self.assert_has_error(candidate, "every route must remain false")

    def test_provider_cycle_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["routes"].extend(
            [
                {
                    "id": "linear-to-broker-cycle-test-v1",
                    "enabled": False,
                    "dryRun": True,
                    "approvalClass": 0,
                    "trigger": {"provider": "linear", "event": "test"},
                    "steps": [
                        {
                            "order": 1,
                            "operation": "broker.normalize-and-persist",
                            "target": "azure-service-bus-broker",
                            "approvalClass": 0,
                        }
                    ],
                },
                {
                    "id": "broker-to-linear-cycle-test-v1",
                    "enabled": False,
                    "dryRun": True,
                    "approvalClass": 0,
                    "trigger": {"provider": "azure", "event": "test"},
                    "steps": [
                        {
                            "order": 1,
                            "operation": "linear.issue.update",
                            "target": "linear.integrationFabric",
                            "approvalClass": 0,
                        }
                    ],
                },
            ]
        )
        self.assert_has_error(candidate, "contains a cycle")

    def test_self_target_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["routes"][0]["trigger"]["provider"] = "linear"
        self.assert_has_error(candidate, "self-targeting route is prohibited")

    def test_unregistered_placeholder_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["routes"][0]["steps"][1]["template"] += " {secrets.token}"
        self.assert_has_error(candidate, "unregistered placeholder")

    def test_shell_syntax_and_mentions_fail_closed(self) -> None:
        shell_candidate = copy.deepcopy(self.registry)
        shell_candidate["routes"][0]["steps"][1]["template"] = "Result: $(id)"
        self.assert_has_error(shell_candidate, "unsafe template syntax")

        mention_candidate = copy.deepcopy(self.registry)
        mention_candidate["routes"][0]["steps"][2]["template"] = "@channel blocked"
        self.assert_has_error(mention_candidate, "mention syntax is prohibited")

    def test_unallowlisted_url_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["routes"][0]["steps"][1]["template"] = (
            "[trusted-looking label](https://example.net/run)"
        )
        self.assert_has_error(candidate, "URL host is not allowlisted")

    def test_unicode_control_and_newline_injection_fail_closed(self) -> None:
        bidi_candidate = copy.deepcopy(self.registry)
        bidi_candidate["routes"][0]["steps"][1]["template"] = "safe\u202eexe.txt"
        self.assert_has_error(bidi_candidate, "bidirectional control character")

        newline_candidate = copy.deepcopy(self.registry)
        newline_candidate["routes"][0]["steps"][1]["template"] = "safe\r\nforged"
        self.assert_has_error(newline_candidate, "carriage returns are prohibited")

    def test_public_destination_metadata_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["logicalDestinations"]["slack.controlPlane"]["channel"] = "private-name"
        self.assert_has_error(candidate, "public destination metadata is prohibited")

    def test_missing_reopened_subscription_fails_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        pull_request_route = next(
            route for route in candidate["routes"] if route["trigger"]["event"] == "pull_request"
        )
        pull_request_route["trigger"]["actions"].remove("reopened")
        self.assert_has_error(candidate, "must subscribe to reopened")

    def test_raw_idempotency_inputs_fail_closed(self) -> None:
        candidate = copy.deepcopy(self.registry)
        candidate["untrustedTemplatePolicy"]["idempotency"]["rawUntrustedTextIncluded"] = True
        self.assert_has_error(candidate, "normalized SHA-256 policy is required")


if __name__ == "__main__":
    unittest.main()
