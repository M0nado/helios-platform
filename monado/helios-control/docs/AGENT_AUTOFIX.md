# HELIOS interactive autofix agent

This contract coordinates OpenAI Codex/ChatGPT, GitHub Copilot, and Microsoft 365 Copilot around one evidence-backed HELIOS plan. It does not grant any model independent Azure credentials.

## What is automatic

- Refresh repository, issue, connector, Azure Resource Graph, and activity-log context.
- Diagnose failures and produce a redacted evidence bundle.
- Start one idempotent Edge control run and resume its saved Cosmos state after a browser or process interruption.
- Propose a scoped code/configuration repair and its validation plan; branch and draft-PR execution remain disabled until an approved executor is implemented.
- Render inventory, identity-readiness, deployment-preview, and health-verification stages inside one operator-authenticated Cloud Shell session.
- Select a subscription deterministically from the tenant and target resource group, with an interactive fallback when the evidence is ambiguous.
- Persist canonical deployment-preview evidence in Cloud Shell storage so another authorized session can verify the same SHA-256 without implying separate agent identities.
- Enforce HELIOS ownership, environment, service, provisioner, repository, and organization tags through Bicep.
- Deliver normalized status through HTTPS relays with destination-specific HMAC signatures, idempotency keys, bounded retries, and saved receipts.
- Propose reusable knowledge updates from successful fixes.

## What stays approved

- Tenant-wide Entra consent, directory roles, and privileged Azure RBAC.
- Key Vault value creation or rotation.
- DNS registrar or zone delegation.
- ARM/Bicep apply, resource deletion, production changes, and pull-request merge.
- Promotion of learned rules into the active agent policy.

## Interactive sequence

1. Select a provider and issue or desired outcome.
2. Refresh grounded evidence; redact tokens, secrets, and personal data.
3. Generate a deterministic HELIOS plan and plan digest.
4. Run read-only diagnostics as labeled stages in the operator's authenticated Cloud Shell context.
5. For code repair, produce a proposal; an authorized repository workflow owns any branch or draft PR.
6. For Azure change, generate Bicep/CLI preview evidence.
7. Require the appropriate owner to approve identity, vault, DNS, or deployment gates.
8. Recheck drift and execute only the approved scope.
9. Reconcile receipts to GitHub, Linear, Slack, and SharePoint.
10. Store learning as a candidate PR; never silently alter active policy.

Cleanup follows the same contract. The agent may inventory resources tagged `helios-managed=true`, identify locks and shared dependencies, and prepare a complete-mode removal what-if. It must protect untagged, unknown, shared, or drifted resources. A protected workflow, fresh drift check, owner approval, and exact typed confirmation are mandatory before any removal.

Future DNS, VNet, Functions, Event Hubs, and broader Cosmos learning/data modules remain plan-only or disabled until their least-privilege identities, private networking, cost limits, backup, and deletion protections have dedicated tests. The narrow Cosmos control-run container is implemented separately.
