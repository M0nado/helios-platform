# Hermes/XCore Unified Governed Specification v1

Status: proposed contract

Owner: Heli0s Admin

Change path: reviewed pull request

Runtime authority: protected deployment environments and accountable human approvers

## Purpose

This specification converts the Hermes/XCore design corpus into a bounded implementation contract. It defines what can be built now without turning an agent, chat channel, model score, or incident label into production authority.

The machine-readable sources are:

- `contracts/hermes-xcore/v1/system.contract.json`
- `contracts/hermes-xcore/v1/event.schema.json`

## Operating boundary

Hermes is a policy evaluator and decision-record producer. It may preflight, simulate, deny, request approval, and recommend remediation. It cannot approve its own request, impersonate a person, execute a deployment, execute a rollback, or disable a safety control.

Heli0s Admin is an accountable human role. It may exercise authorized administration and protected-environment approval, but it cannot bypass immutable evidence, artifact provenance, workload identity separation, or protected-production controls.

GitHub is the source, review, release, and protected-environment authority. Azure is the gated infrastructure target. Azure DevOps is a read-only discovery and synchronization target until a separately reviewed authority model is approved. SharePoint stores governance and evidence. Slack provides notifications and acknowledgements. Linear tracks work and incidents. OpenAI provides model capability; model output is advisory rather than authority.

## Environments

| Identifier | Purpose | Code generation | Chaos | Approval |
| --- | --- | --- | --- | --- |
| `x-tier-dev` | Build, validate, and propose | Proposal only | Disabled | Artifact binding required |
| `x-tier-xcore` | Isolated resilience experiments | Restricted proposal | Explicitly opted-in, seeded, bounded, reversible | Protected approval |
| `x-tier-prod` | Stable production delivery | Forbidden | Disabled | Two accountable humans, separation of duties |

`hotfix` is an expedited workflow targeting `x-tier-prod`; it is not an automatically privileged environment. An S0 classification alone does not authorize a deployment or rollback.

## Promotion contract

Promotion from XCore to production requires the immutable source revision, SHA-256 artifact identity, provenance, tests, security results, the XCore experiment record, a deployment plan, what-if evidence, and an unexpired human approval bound to those exact bytes and parameters.

Predictions remain advisory. A prediction must declare units, time window, sample size, model version, minimum evidence, and an explicit unknown state. Missing evidence fails closed.

## Runtime

The runtime is event driven and assumes at-least-once delivery. Every event therefore carries an idempotency key, correlation and causation identifiers, attempt count, and bounded hop count.

The finite lifecycle is:

`INIT → PRECHECK → PLAN → AWAIT_APPROVAL → EXECUTE → VERIFY → NOTIFY → COMPLETE`

Failures terminate in `FAILED` or, only after an authorized and verified rollback, `ROLLED_BACK`. Approval timeout terminates as `FAILED`; it never becomes implicit approval.

## XCore chaos boundary

An experiment is permitted only against isolated XCore resources after explicit opt-in. It must identify an owner and deterministic seed, cap duration, cost, and affected resources, define abort conditions, prohibit secret mutation and production reachability, and prove cleanup.

Random faults against a shared tenant, production, hotfix, third-party accounts, or approval channels are outside this contract.

## Integration authority

| Integration | Permitted role | Not authoritative for |
| --- | --- | --- |
| GitHub | Source, review, release, protected approvals | Azure tenant administration |
| Azure | Federated deployment target and runtime | Source review |
| Azure DevOps | Read-only discovery and synchronization | Source or release authority |
| SharePoint | Governance and evidence records | Deployment approval |
| Slack | Notification and acknowledgement | Deployment approval or execution |
| Linear | Work and incident tracking | Deployment approval or execution |
| OpenAI | Model-provider capability | Policy, human approval, or execution |

## Security requirements

- Use OIDC federated workload identities and live effective-subject resolution.
- Use a distinct least-privilege identity per environment; workload identities must not receive Owner.
- Keep raw provider keys out of source, events, logs, Slack, Linear, and SharePoint.
- Resolve OpenAI secrets at runtime through Azure Key Vault using managed identity.
- Record actor identity, UTC timestamps, artifact provenance, and append-only decision evidence.
- Bind approvals to the exact artifact digest, deployment parameters, and what-if output.

## Non-goals

Version 1 does not deploy Azure resources, create production credentials, change tenant RBAC, install software on hosts, mutate WinRE or boot state, invoke training endpoints, or import process-local control servers. Those actions require their own reviewed implementation and evidence lanes.
