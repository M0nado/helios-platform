# HELIOS Hermes/XCore Unified Spec v1.0

**Status:** Proposed  
**Canonical repository:** `M0nado/helios-platform`  
**Issue:** #204

This specification defines the governed federation contract between HELIOS, Hermes, and XCore for environment bindings, approval boundaries, artifact continuity, and cross-system control-plane evidence.

## 1. Scope and dependency alignment

This spec is authored to close Issue #204 and align with dependent tracks:

- [#165](https://github.com/M0nado/helios-platform/issues/165)
- [#203](https://github.com/M0nado/helios-platform/issues/203)
- [#198](https://github.com/M0nado/helios-platform/issues/198)
- [#197](https://github.com/M0nado/helios-platform/issues/197)
- [#200](https://github.com/M0nado/helios-platform/issues/200)
- [#201](https://github.com/M0nado/helios-platform/issues/201)
- [#199](https://github.com/M0nado/helios-platform/issues/199)
- [#162](https://github.com/M0nado/helios-platform/issues/162)

Any dependency that changes privileged execution, tenant authority, or production deployment must remain admin-approved and rollback-documented.

Admin-gated prerequisites for any privileged rollout under this spec:

1. protected-environment reviewer policy is configured and verifiable;
2. tenant-bound OIDC federation is reviewed;
3. what-if evidence and immutable artifact digest are available;
4. rollback path is documented in the owning issue/PR evidence.

## 2. Canonical environment model

The only canonical deployment environments are:

1. `x-tier-dev`
2. `x-tier-xcore`
3. `x-tier-prod`

Legacy aliases (`dev/test/preview/prod`, `azure-dev/test/prod`) are non-authoritative and must not be emitted by new contracts.

Hotfix is **not** a fourth environment. Hotfix is an incident-bound expedited path that still targets `x-tier-prod` and preserves approval gates.

## 3. Authority and capability boundaries

Authoritative deployment and approval surfaces are constrained to governed workflow/runtime control points:

- Deployment execution: `github-actions-workflow`
- Deployment approval: `github-protected-environment-reviewers`, `azure-change-control`

The following surfaces are non-authoritative and must not approve or execute deployment:

- Teams
- Slack
- Outlook
- Edge extension surfaces
- Microsoft Copilot and Copilot Studio conversational surfaces

Hermes and XCore are advisory/evaluation systems. They can recommend, score, and reject, but they cannot bypass approval authority.

Inference constraints are advisory-only and fail closed: suggestions, ranking, or synthetic evaluation output cannot directly trigger deploy, approval, RBAC changes, or tenant-policy mutation.

## 4. Immutable artifact continuity

Promotion continuity is mandatory:

1. `x-tier-dev` -> `x-tier-xcore`
2. `x-tier-xcore` -> `x-tier-prod`

For each transition:

- exact immutable artifact digest continuity is required;
- request and template digests must match reviewed evidence;
- rebuild or artifact substitution after approval is forbidden.

XCore must reject promotion if the artifact digest is missing from evaluation evidence or if the promoted artifact was rebuilt.

## 5. Chaos and regression policy

Chaos/fault activity under this contract is bounded and non-authoritative:

1. chaos experiments must execute in `x-tier-dev` or `x-tier-xcore`, never directly in `x-tier-prod`;
2. any chaos-derived recommendation must pass the same approval and artifact-continuity gates before promotion;
3. XCore regression checks must fail closed and open reviewable evidence when drift or safety regression is detected.

## 6. Event/reliability contract

Cross-system federation events must use the normalized envelope and include:

- event ID and correlation ID,
- source, event type, and environment,
- data classification and actor identity,
- evidence links.

Delivery semantics are fail-closed:

- at-least-once delivery,
- idempotent processing keys,
- replay protection,
- lease fencing with compare-and-swap ownership checks.

## 7. Hotfix governance contract

Hotfix controls are mandatory:

1. target environment must be `x-tier-prod`;
2. incident ID and hotfix reason are required inputs;
3. hotfix cannot auto-approve;
4. hotfix cannot auto-deploy;
5. second approval remains required before apply.

## 8. Machine-readable contract set

| Contract area | Schema | Canonical contract |
| --- | --- | --- |
| Environment bindings | `schemas/hermes-xcore-environment-bindings-v1.schema.json` | `config/HELIOS_HERMES_XCORE_ENVIRONMENT_BINDINGS_V1.json` |
| Capability bindings | `schemas/hermes-xcore-capability-bindings-v1.schema.json` | `config/HELIOS_HERMES_XCORE_CAPABILITY_BINDINGS_V1.json` |
| Event profile | `schemas/hermes-xcore-event-profile-v1.schema.json` | `config/HELIOS_HERMES_XCORE_EVENT_PROFILE_V1.json` |
| Approval governance | `schemas/hermes-xcore-approval-governance-v1.schema.json` | `config/HELIOS_HERMES_XCORE_APPROVAL_GOVERNANCE_V1.json` |

## 9. Conformance requirements

All contract artifacts must pass:

1. JSON Schema Draft 2020-12 validation.
2. `scripts/control/validate_hermes_xcore_contract.py`.
3. `scripts/control/tests/test_validate_hermes_xcore_contract.py`.

Failure is fail-closed: any contract drift or weakened control invalidates the run.
