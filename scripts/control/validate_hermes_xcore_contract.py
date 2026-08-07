#!/usr/bin/env python3
"""Fail-closed validation for HELIOS Hermes/XCore unified v1 contracts."""

from __future__ import annotations

import argparse
import json
import sys
from collections.abc import Mapping
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ENVIRONMENT_CONTRACT = (
    REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_ENVIRONMENT_BINDINGS_V1.json"
)
DEFAULT_CAPABILITY_CONTRACT = (
    REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_CAPABILITY_BINDINGS_V1.json"
)
DEFAULT_EVENT_CONTRACT = (
    REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_EVENT_PROFILE_V1.json"
)
DEFAULT_APPROVAL_CONTRACT = (
    REPOSITORY_ROOT / "config" / "HELIOS_HERMES_XCORE_APPROVAL_GOVERNANCE_V1.json"
)

EXPECTED_ENVIRONMENTS = {"x-tier-dev", "x-tier-xcore", "x-tier-prod"}
EXPECTED_ENVIRONMENT_BINDINGS = {
    "x-tier-dev": {
        "tier": "development",
        "deploymentWorkflow": "helios-cloud-deploy.yml",
    },
    "x-tier-xcore": {
        "tier": "evaluation",
        "deploymentWorkflow": "helios-cloud-deploy.yml",
    },
    "x-tier-prod": {
        "tier": "production",
        "deploymentWorkflow": "helios-cloud-deploy.yml",
    },
}
EXPECTED_TRANSITIONS = {
    ("x-tier-dev", "x-tier-xcore"),
    ("x-tier-xcore", "x-tier-prod"),
}
EXPECTED_DEPLOYMENT_SURFACES = {"github-actions-workflow"}
EXPECTED_APPROVAL_SURFACES = {
    "github-protected-environment-reviewers",
    "azure-change-control",
}
NON_AUTHORITATIVE_SURFACES = {
    "teams",
    "slack",
    "outlook",
    "edge-extension",
    "microsoft-copilot",
    "copilot-studio",
}
ADVISORY_ONLY_SURFACES = {"hermes-router", "xcore-evaluator"}
EXPECTED_EVENT_REQUIRED_FIELDS = {
    "schemaVersion",
    "eventId",
    "correlationId",
    "source",
    "eventType",
    "repository",
    "environment",
    "dataClassification",
    "occurredAt",
    "actor",
    "links",
    "payload",
}


def _name_set(value: object) -> set[str]:
    if not isinstance(value, list):
        return set()
    names = {entry for entry in value if isinstance(entry, str)}
    return names if len(names) == len(value) else set()


def _mapping(value: object) -> Mapping[str, object] | None:
    return value if isinstance(value, Mapping) else None


def validate_environment_contract(contract: object) -> list[str]:
    errors: list[str] = []
    document = _mapping(contract)
    if document is None:
        return ["environment contract: must be a JSON object"]

    if (
        document.get("contractSchema")
        != "schemas/hermes-xcore-environment-bindings-v1.schema.json"
    ):
        errors.append("environment contract: contractSchema mismatch")
    if document.get("schemaVersion") != "1.0.0":
        errors.append("environment contract: schemaVersion must be 1.0.0")

    environments = document.get("environments")
    if not isinstance(environments, list):
        errors.append("environment contract: environments must be an array")
        environments = []
    names: set[str] = set()
    for index, entry in enumerate(environments):
        environment = _mapping(entry)
        if environment is None:
            errors.append(f"environment contract: environments[{index}] must be an object")
            continue
        name = environment.get("name")
        if not isinstance(name, str):
            errors.append(f"environment contract: environments[{index}].name must be a string")
            continue
        names.add(name)
        expected_binding = EXPECTED_ENVIRONMENT_BINDINGS.get(name)
        if expected_binding is not None:
            if environment.get("tier") != expected_binding["tier"]:
                errors.append(
                    "environment contract: "
                    f"environments[{index}] must bind {name} to tier "
                    f"{expected_binding['tier']}"
                )
            if environment.get("deploymentWorkflow") != expected_binding["deploymentWorkflow"]:
                errors.append(
                    "environment contract: "
                    f"environments[{index}] must bind {name} to deployment workflow "
                    f"{expected_binding['deploymentWorkflow']}"
                )
        if environment.get("approvalRequired") is not True:
            errors.append(
                f"environment contract: environments[{index}] must require approval"
            )
    if names != EXPECTED_ENVIRONMENTS:
        errors.append(
            "environment contract: canonical environments must be exactly "
            + ", ".join(sorted(EXPECTED_ENVIRONMENTS))
        )

    promotion_graph = document.get("promotionGraph")
    transition_set: set[tuple[str, str]] = set()
    if not isinstance(promotion_graph, list):
        errors.append("environment contract: promotionGraph must be an array")
        promotion_graph = []
    for index, entry in enumerate(promotion_graph):
        transition = _mapping(entry)
        if transition is None:
            errors.append(f"environment contract: promotionGraph[{index}] must be an object")
            continue
        source = transition.get("from")
        target = transition.get("to")
        if isinstance(source, str) and isinstance(target, str):
            transition_set.add((source, target))
        if transition.get("requiresExactArtifactDigest") is not True:
            errors.append(
                f"environment contract: promotionGraph[{index}] must require exact artifact digest"
            )
        if transition.get("requiresEvidenceLink") is not True:
            errors.append(
                f"environment contract: promotionGraph[{index}] must require evidence link"
            )
    if transition_set != EXPECTED_TRANSITIONS:
        errors.append(
            "environment contract: promotionGraph must define only "
            "x-tier-dev->x-tier-xcore and x-tier-xcore->x-tier-prod"
        )

    hotfix = _mapping(document.get("hotfix"))
    if hotfix is None:
        errors.append("environment contract: hotfix must be an object")
    else:
        if hotfix.get("targetEnvironment") != "x-tier-prod":
            errors.append("environment contract: hotfix targetEnvironment must be x-tier-prod")
        if hotfix.get("separateEnvironment") is not False:
            errors.append("environment contract: hotfix must not be a separate environment")
        if hotfix.get("requiresIncidentId") is not True:
            errors.append("environment contract: hotfix must require incident ID")
        if hotfix.get("requiresReason") is not True:
            errors.append("environment contract: hotfix must require reason")
        if hotfix.get("autoApprove") is not False:
            errors.append("environment contract: hotfix autoApprove must be false")
        if hotfix.get("autoDeploy") is not False:
            errors.append("environment contract: hotfix autoDeploy must be false")

    continuity = _mapping(document.get("immutableArtifactContinuity"))
    if continuity is None:
        errors.append("environment contract: immutableArtifactContinuity must be an object")
    else:
        if continuity.get("required") is not True:
            errors.append("environment contract: immutable artifact continuity must be required")
        if continuity.get("forbidRebuildAfterApproval") is not True:
            errors.append("environment contract: rebuild-after-approval must be forbidden")
        if continuity.get("requireTemplateAndRequestDigestMatch") is not True:
            errors.append(
                "environment contract: template/request digest match must be required"
            )

    return errors


def validate_capability_contract(contract: object) -> list[str]:
    errors: list[str] = []
    document = _mapping(contract)
    if document is None:
        return ["capability contract: must be a JSON object"]

    if (
        document.get("contractSchema")
        != "schemas/hermes-xcore-capability-bindings-v1.schema.json"
    ):
        errors.append("capability contract: contractSchema mismatch")
    if document.get("schemaVersion") != "1.0.0":
        errors.append("capability contract: schemaVersion must be 1.0.0")

    surfaces_value = document.get("surfaces")
    if not isinstance(surfaces_value, list):
        errors.append("capability contract: surfaces must be an array")
        surfaces_value = []
    surfaces: dict[str, Mapping[str, object]] = {}
    for index, entry in enumerate(surfaces_value):
        surface = _mapping(entry)
        if surface is None:
            errors.append(f"capability contract: surfaces[{index}] must be an object")
            continue
        name = surface.get("name")
        if not isinstance(name, str):
            errors.append(f"capability contract: surfaces[{index}].name must be a string")
            continue
        if name in surfaces:
            errors.append(
                f"capability contract: surfaces[{index}].name duplicates existing surface {name}"
            )
            continue
        surfaces[name] = surface

    deployment_surfaces = _name_set(document.get("authoritativeDeploymentSurfaces"))
    if deployment_surfaces != EXPECTED_DEPLOYMENT_SURFACES:
        errors.append(
            "capability contract: authoritativeDeploymentSurfaces must be exactly "
            "github-actions-workflow"
        )

    approval_surfaces = _name_set(document.get("authoritativeApprovalSurfaces"))
    if approval_surfaces != EXPECTED_APPROVAL_SURFACES:
        errors.append(
            "capability contract: authoritativeApprovalSurfaces must be exactly "
            "github-protected-environment-reviewers and azure-change-control"
        )

    non_authoritative = _name_set(document.get("nonAuthoritativeSurfaces"))
    if not NON_AUTHORITATIVE_SURFACES.issubset(non_authoritative):
        errors.append(
            "capability contract: nonAuthoritativeSurfaces must include collaboration surfaces"
        )

    advisory = _name_set(document.get("advisoryOnlySurfaces"))
    if not ADVISORY_ONLY_SURFACES.issubset(advisory):
        errors.append("capability contract: advisoryOnlySurfaces must include Hermes and XCore")

    for name in deployment_surfaces.union(approval_surfaces):
        if name not in surfaces:
            errors.append(f"capability contract: missing surface definition for {name}")

    for name, surface in surfaces.items():
        can_approve = surface.get("canApprove") is True
        can_deploy = surface.get("canDeploy") is True
        can_execute = surface.get("canExecuteApprovalWorkflow") is True
        if can_approve and name not in approval_surfaces:
            errors.append(
                "capability contract: "
                f"{name} approval authority is not declared in authoritativeApprovalSurfaces"
            )
        if (can_deploy or can_execute) and name not in deployment_surfaces:
            errors.append(
                "capability contract: "
                f"{name} deployment authority is not declared in authoritativeDeploymentSurfaces"
            )

    for name in NON_AUTHORITATIVE_SURFACES:
        surface = surfaces.get(name)
        if surface is None:
            errors.append(f"capability contract: missing surface definition for {name}")
            continue
        if surface.get("canApprove") is True:
            errors.append(f"capability contract: {name} must not approve deployment")
        if surface.get("canDeploy") is True:
            errors.append(f"capability contract: {name} must not deploy")
        if surface.get("canExecuteApprovalWorkflow") is True:
            errors.append(f"capability contract: {name} must not execute approval workflows")

    for name in ADVISORY_ONLY_SURFACES:
        surface = surfaces.get(name)
        if surface is None:
            errors.append(f"capability contract: missing advisory surface {name}")
            continue
        if (
            surface.get("canApprove") is True
            or surface.get("canDeploy") is True
            or surface.get("canExecuteApprovalWorkflow") is True
        ):
            errors.append(f"capability contract: advisory surface {name} gained authority")

    return errors


def validate_event_contract(contract: object) -> list[str]:
    errors: list[str] = []
    document = _mapping(contract)
    if document is None:
        return ["event profile contract: must be a JSON object"]

    if document.get("contractSchema") != "schemas/hermes-xcore-event-profile-v1.schema.json":
        errors.append("event profile contract: contractSchema mismatch")
    if document.get("schemaVersion") != "1.0.0":
        errors.append("event profile contract: schemaVersion must be 1.0.0")

    envelope = _mapping(document.get("eventEnvelope"))
    if envelope is None:
        errors.append("event profile contract: eventEnvelope must be an object")
    else:
        if envelope.get("correlationIdRequired") is not True:
            errors.append("event profile contract: correlationIdRequired must be true")
        if envelope.get("linksRequired") is not True:
            errors.append("event profile contract: linksRequired must be true")
        required_fields = _name_set(envelope.get("requiredFields"))
        for required in EXPECTED_EVENT_REQUIRED_FIELDS:
            if required not in required_fields:
                errors.append(f"event profile contract: requiredFields missing {required}")
        if "evidenceLinks" in required_fields:
            errors.append(
                "event profile contract: requiredFields must use links instead of evidenceLinks"
            )

    delivery = _mapping(document.get("delivery"))
    if delivery is None:
        errors.append("event profile contract: delivery must be an object")
        return errors
    if delivery.get("semantics") != "at-least-once":
        errors.append("event profile contract: delivery semantics must be at-least-once")

    idempotency = _mapping(delivery.get("idempotency"))
    if idempotency is None:
        errors.append("event profile contract: idempotency must be an object")
    else:
        if idempotency.get("required") is not True:
            errors.append("event profile contract: idempotency.required must be true")
        key_fields = _name_set(idempotency.get("keyFields"))
        for required in {"eventId", "correlationId", "source", "eventType"}:
            if required not in key_fields:
                errors.append(f"event profile contract: idempotency.keyFields missing {required}")

    replay = _mapping(delivery.get("replayProtection"))
    if replay is None:
        errors.append("event profile contract: replayProtection must be an object")
    else:
        if replay.get("required") is not True:
            errors.append("event profile contract: replayProtection.required must be true")
        max_age = replay.get("maxAgeMinutes")
        if not isinstance(max_age, int) or max_age < 1:
            errors.append("event profile contract: replayProtection.maxAgeMinutes must be >= 1")
        if replay.get("requireNonce") is not True:
            errors.append("event profile contract: replayProtection.requireNonce must be true")

    lease_fencing = _mapping(delivery.get("leaseFencing"))
    if lease_fencing is None:
        errors.append("event profile contract: leaseFencing must be an object")
    else:
        if lease_fencing.get("required") is not True:
            errors.append("event profile contract: leaseFencing.required must be true")
        if lease_fencing.get("compareAndSwapRequired") is not True:
            errors.append(
                "event profile contract: leaseFencing.compareAndSwapRequired must be true"
            )
        for field in ("leaseOwnerField", "leaseExpiryField", "etagField"):
            if not isinstance(lease_fencing.get(field), str) or not lease_fencing.get(field):
                errors.append(f"event profile contract: leaseFencing.{field} must be non-empty")

    return errors


def validate_approval_contract(contract: object) -> list[str]:
    errors: list[str] = []
    document = _mapping(contract)
    if document is None:
        return ["approval governance contract: must be a JSON object"]

    if (
        document.get("contractSchema")
        != "schemas/hermes-xcore-approval-governance-v1.schema.json"
    ):
        errors.append("approval governance contract: contractSchema mismatch")
    if document.get("schemaVersion") != "1.0.0":
        errors.append("approval governance contract: schemaVersion must be 1.0.0")

    authorities = _mapping(document.get("authorities"))
    if authorities is None:
        errors.append("approval governance contract: authorities must be an object")
    else:
        deployment_surfaces = _name_set(authorities.get("deploymentSurfaces"))
        if deployment_surfaces != EXPECTED_DEPLOYMENT_SURFACES:
            errors.append(
                "approval governance contract: deploymentSurfaces must be exactly github-actions-workflow"
            )
        approval_surfaces = _name_set(authorities.get("approvalSurfaces"))
        if approval_surfaces != EXPECTED_APPROVAL_SURFACES:
            errors.append(
                "approval governance contract: approvalSurfaces must be exactly "
                "github-protected-environment-reviewers and azure-change-control"
            )
        disallowed = _name_set(authorities.get("disallowedApprovalAndDeploymentSurfaces"))
        if not NON_AUTHORITATIVE_SURFACES.issubset(disallowed):
            errors.append(
                "approval governance contract: disallowed surfaces missing collaboration boundaries"
            )

    approvals = _mapping(document.get("approvals"))
    if approvals is None:
        errors.append("approval governance contract: approvals must be an object")
    else:
        min_approvals = _mapping(approvals.get("minApprovals"))
        if min_approvals is None:
            errors.append("approval governance contract: approvals.minApprovals must be an object")
        else:
            keys = set(min_approvals.keys())
            if keys != EXPECTED_ENVIRONMENTS:
                errors.append(
                    "approval governance contract: approvals.minApprovals must define x-tier-dev, x-tier-xcore, and x-tier-prod"
                )
            prod_approvals = min_approvals.get("x-tier-prod")
            if not isinstance(prod_approvals, int) or prod_approvals < 2:
                errors.append(
                    "approval governance contract: x-tier-prod requires at least two approvals"
                )
        if approvals.get("secondApprovalRequiredForProd") is not True:
            errors.append(
                "approval governance contract: secondApprovalRequiredForProd must be true"
            )

    hotfix = _mapping(document.get("hotfix"))
    if hotfix is None:
        errors.append("approval governance contract: hotfix must be an object")
    else:
        if hotfix.get("targetEnvironment") != "x-tier-prod":
            errors.append("approval governance contract: hotfix targetEnvironment must be x-tier-prod")
        if hotfix.get("requiresIncidentId") is not True:
            errors.append("approval governance contract: hotfix requiresIncidentId must be true")
        if hotfix.get("requiresReason") is not True:
            errors.append("approval governance contract: hotfix requiresReason must be true")
        if hotfix.get("autoApprove") is not False:
            errors.append("approval governance contract: hotfix autoApprove must be false")
        if hotfix.get("autoDeploy") is not False:
            errors.append("approval governance contract: hotfix autoDeploy must be false")

    continuity = _mapping(document.get("artifactContinuity"))
    if continuity is None:
        errors.append("approval governance contract: artifactContinuity must be an object")
    else:
        transitions_value = continuity.get("transitions")
        transition_set: set[tuple[str, str]] = set()
        if not isinstance(transitions_value, list):
            errors.append("approval governance contract: artifactContinuity.transitions must be an array")
            transitions_value = []
        for index, entry in enumerate(transitions_value):
            transition = _mapping(entry)
            if transition is None:
                errors.append(
                    f"approval governance contract: artifactContinuity.transitions[{index}] must be an object"
                )
                continue
            source = transition.get("from")
            target = transition.get("to")
            if isinstance(source, str) and isinstance(target, str):
                transition_set.add((source, target))
            if transition.get("requireExactDigest") is not True:
                errors.append(
                    f"approval governance contract: transitions[{index}] must require exact digest"
                )
        if transition_set != EXPECTED_TRANSITIONS:
            errors.append(
                "approval governance contract: artifact transitions must match dev->xcore->prod"
            )
        if continuity.get("forbidRebuildAfterApproval") is not True:
            errors.append(
                "approval governance contract: forbidRebuildAfterApproval must be true"
            )
        if continuity.get("requireTemplateAndRequestDigestMatch") is not True:
            errors.append(
                "approval governance contract: requireTemplateAndRequestDigestMatch must be true"
            )

    xcore = _mapping(document.get("xcorePromotionPolicy"))
    if xcore is None:
        errors.append("approval governance contract: xcorePromotionPolicy must be an object")
    else:
        if xcore.get("requireRecordedDigestFromEvaluation") is not True:
            errors.append(
                "approval governance contract: XCore must require recorded digest from evaluation"
            )
        if xcore.get("allowRebuiltArtifactPromotion") is not False:
            errors.append(
                "approval governance contract: XCore must reject rebuilt artifact promotion"
            )
        if xcore.get("requireEvidenceLink") is not True:
            errors.append("approval governance contract: XCore must require evidence links")

    return errors


def validate_contracts(
    environment_contract: object,
    capability_contract: object,
    event_contract: object,
    approval_contract: object,
) -> list[str]:
    errors: list[str] = []
    errors.extend(validate_environment_contract(environment_contract))
    errors.extend(validate_capability_contract(capability_contract))
    errors.extend(validate_event_contract(event_contract))
    errors.extend(validate_approval_contract(approval_contract))
    return errors


def _load_json(path: Path) -> tuple[object | None, list[str]]:
    try:
        return json.loads(path.read_text(encoding="utf-8")), []
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return None, [f"{path}: cannot load JSON ({exc})"]


def validate_files(
    environment_contract_path: Path,
    capability_contract_path: Path,
    event_contract_path: Path,
    approval_contract_path: Path,
) -> list[str]:
    errors: list[str] = []
    environment_contract, load_errors = _load_json(environment_contract_path)
    errors.extend(load_errors)
    capability_contract, load_errors = _load_json(capability_contract_path)
    errors.extend(load_errors)
    event_contract, load_errors = _load_json(event_contract_path)
    errors.extend(load_errors)
    approval_contract, load_errors = _load_json(approval_contract_path)
    errors.extend(load_errors)
    if errors:
        return errors
    errors.extend(
        validate_contracts(
            environment_contract,
            capability_contract,
            event_contract,
            approval_contract,
        )
    )
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--environment", type=Path, default=DEFAULT_ENVIRONMENT_CONTRACT)
    parser.add_argument("--capability", type=Path, default=DEFAULT_CAPABILITY_CONTRACT)
    parser.add_argument("--event", type=Path, default=DEFAULT_EVENT_CONTRACT)
    parser.add_argument("--approval", type=Path, default=DEFAULT_APPROVAL_CONTRACT)
    arguments = parser.parse_args(argv)

    errors = validate_files(
        arguments.environment.resolve(),
        arguments.capability.resolve(),
        arguments.event.resolve(),
        arguments.approval.resolve(),
    )
    if errors:
        print("Hermes/XCore contract validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("Hermes/XCore contracts valid.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
