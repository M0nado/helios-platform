"""Deterministic safety policy for deployment-plan proposals.

The agent is plan-only. A policy verdict of ``allow_plan`` permits the agent to
describe a step; it never grants authority to execute that step.
"""

from __future__ import annotations

from dataclasses import asdict, dataclass
from enum import StrEnum
from typing import Any, Mapping

MAX_MANIFEST_BYTES = 262_144
SENSITIVE_KEY_PARTS = ("api_key", "password", "private_key", "secret", "token")


class Decision(StrEnum):
    ALLOW_PLAN = "allow_plan"
    REQUIRES_CONTROLS = "requires_controls"
    DENY = "deny"


@dataclass(frozen=True)
class PolicyVerdict:
    action: str
    decision: Decision
    risk: str
    required_controls: tuple[str, ...]
    missing_controls: tuple[str, ...]
    reason: str
    execution_permitted: bool = False

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


READ_ONLY_ACTIONS = frozenset(
    {
        "compare_branches",
        "create_report",
        "inspect_repository",
        "read_governance",
        "validate_build",
    }
)

CONTROLLED_ACTIONS: dict[str, tuple[str, tuple[str, ...]]] = {
    "create_draft_pr": ("moderate", ("human_approval",)),
    "post_status_notification": (
        "moderate",
        ("human_approval", "destination_resolved"),
    ),
    "merge_pull_request": (
        "high",
        ("human_approval", "required_checks_green", "rollback_plan"),
    ),
    "delete_branch": (
        "high",
        ("human_approval", "unique_delta_evidence", "rollback_plan"),
    ),
    "archive_repository": (
        "high",
        ("human_approval", "redirect_published", "unique_delta_evidence"),
    ),
    "deploy_staging": (
        "high",
        ("human_approval", "oidc_federation", "rollback_plan"),
    ),
    "deploy_production": (
        "critical",
        (
            "human_approval",
            "required_checks_green",
            "oidc_federation",
            "protected_environment",
            "rollback_plan",
        ),
    ),
    "modify_rbac": (
        "critical",
        ("human_approval", "least_privilege_review", "protected_environment"),
    ),
    "graph_write": (
        "high",
        ("human_approval", "destination_resolved", "idempotency_key"),
    ),
    "usb_write": (
        "critical",
        (
            "human_approval",
            "physical_presence",
            "verified_device_identity",
            "recovery_plan",
        ),
    ),
}

PROHIBITED_ACTIONS: dict[str, str] = {
    "bypass_branch_protection": "Branch protection and required review may not be bypassed.",
    "direct_production_apply": "Production changes must use the protected deployment workflow.",
    "echo_secret": "Secrets may not be printed, returned, logged, or copied into prompts.",
    "read_secret_value": "The planner may check configuration presence but never read back values.",
    "self_approve": "An agent cannot approve its own privileged action.",
}


def contains_sensitive_key(value: Any) -> bool:
    """Return true without reproducing a value when a sensitive field name is present."""

    if isinstance(value, dict):
        for key, nested in value.items():
            normalized = str(key).lower().replace("-", "_")
            if any(part in normalized for part in SENSITIVE_KEY_PARTS):
                return True
            if contains_sensitive_key(nested):
                return True
    elif isinstance(value, list):
        return any(contains_sensitive_key(item) for item in value)
    return False


def _normalize_action(action: str) -> str:
    return action.strip().lower().replace("-", "_").replace(" ", "_")


def _enabled_controls(context: Mapping[str, Any]) -> frozenset[str]:
    return frozenset(key for key, value in context.items() if value is True)


def evaluate_action(
    action: str,
    context: Mapping[str, Any] | None = None,
) -> PolicyVerdict:
    """Evaluate an action without performing it."""

    normalized = _normalize_action(action)
    controls = _enabled_controls(context or {})

    if normalized in PROHIBITED_ACTIONS:
        return PolicyVerdict(
            action=normalized,
            decision=Decision.DENY,
            risk="critical",
            required_controls=(),
            missing_controls=(),
            reason=PROHIBITED_ACTIONS[normalized],
        )

    if normalized in READ_ONLY_ACTIONS:
        return PolicyVerdict(
            action=normalized,
            decision=Decision.ALLOW_PLAN,
            risk="low",
            required_controls=(),
            missing_controls=(),
            reason="Read-only analysis may be included in a plan.",
        )

    controlled = CONTROLLED_ACTIONS.get(normalized)
    if controlled is None:
        return PolicyVerdict(
            action=normalized or "<empty>",
            decision=Decision.REQUIRES_CONTROLS,
            risk="unknown",
            required_controls=("human_approval", "documented_policy"),
            missing_controls=("human_approval", "documented_policy"),
            reason="Unknown actions default to review and cannot be executed by this service.",
        )

    risk, required = controlled
    missing = tuple(control for control in required if control not in controls)
    if missing:
        return PolicyVerdict(
            action=normalized,
            decision=Decision.REQUIRES_CONTROLS,
            risk=risk,
            required_controls=required,
            missing_controls=missing,
            reason="Required safeguards are missing; the proposal must remain blocked.",
        )

    return PolicyVerdict(
        action=normalized,
        decision=Decision.ALLOW_PLAN,
        risk=risk,
        required_controls=required,
        missing_controls=(),
        reason="Controls are recorded; planning is allowed, but execution remains out of scope.",
    )
