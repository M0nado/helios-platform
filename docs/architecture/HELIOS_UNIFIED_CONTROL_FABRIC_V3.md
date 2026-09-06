# HELIOS Unified Control Fabric v3

## Integration decision

Unified Control v3 extends the existing `Helios.Connect` control plane and Integration Fabric. It
is not a new broker. GitHub produces one current authority record and one canonical event envelope;
the existing signed, idempotent relay is the only route to Slack, Linear, SharePoint, Azure DevOps,
and OpenAI/ALVIS.

```text
User / ALVIS request
        ↓
GitHub PR or bounded workflow dispatch
        ↓
repository validation + current record
        ↓
helios.control.current.updated envelope
        ↓
existing Helios.Connect lifecycle and policy
        ↓
existing signed relay / future Service Bus spine
        ↓
per-surface projection + durable receipts
```

## What is absorbed

- `control/current.json` and `control/CURRENT.md` become the single readable/machine authority.
- Append-only event and decision ledgers retain correlation history.
- One surface-binding contract prevents duplicate master threads/issues/documents.
- OpenAI/ALVIS receives read/plan tools only.
- Azure plan requests point to the existing protected cloud workflow; this workflow cannot deploy.
- Existing `agent-core-policy.json`, `integrations.json`, control-run runtime, Cosmos leases,
  signature rules, approvals, and deployment gates remain authoritative.

## What is intentionally not absorbed

- direct Slack, Linear, SharePoint, Azure DevOps, or OpenAI provider tokens in GitHub Actions;
- a second event broker;
- a second Azure deployment implementation;
- automatic merge, tenant consent, machine mutation, or production activation;
- duplicate profile, storage, USB, or AIHub implementations already versioned in the canonical repo.

## Activation order

1. Merge after complete GitHub CI and review.
2. Bind the existing GitHub master issue.
3. Bind the existing Slack control-plane thread.
4. Bind the Linear master issue/project.
5. Bind the SharePoint current-authority document.
6. Bind the Azure DevOps validation work item.
7. Bind OpenAI/ALVIS to the four read/plan tools.
8. Enable one signed relay destination per reviewed registry change.
9. Run correlated replay/idempotency tests.
10. Run protected Azure development what-if through the existing workflow.
11. Request deployment separately.
