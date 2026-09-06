# HELIOS Single Control Workflow

HELIOS uses one normalized authority record rather than independent status stories.

```text
request → GitHub validation → current record → canonical event → Helios.Connect → projections
```

GitHub is canonical. Linear owns planning, Slack owns rapid operations conversation, SharePoint owns
durable governance/evidence, Azure DevOps mirrors validation, and OpenAI/ALVIS produces one bounded
digest. External systems cannot authorize deployment.

## Modes

- `status`: render the current record.
- `validate`: validate contracts and safety invariants.
- `plan-sync`: show which permanent objects would be updated; no external write.
- `emit-event`: build the relay envelope; no external write.
- `connector-readiness`: report bindings and missing authorization.
- `azure-plan-request`: produce a request for the existing protected what-if workflow.

All consequential operations remain in the existing HELIOS policy/approval runtime.
