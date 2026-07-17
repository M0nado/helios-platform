# HELIOS interactive autofix agent

This contract coordinates OpenAI Codex/ChatGPT, GitHub Copilot, and Microsoft 365 Copilot around one evidence-backed HELIOS plan. It does not grant any model independent Azure credentials.

## What is automatic

- Refresh repository, issue, connector, Azure Resource Graph, and activity-log context.
- Diagnose failures and produce a redacted evidence bundle.
- Create a task branch, apply a scoped code/configuration repair, run tests, and open a draft pull request.
- Prepare separate Cloud Shell work packets for inventory, identity readiness, deployment preview, and health verification.
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
4. Run read-only diagnostics in isolated Cloud Shell roles.
5. For code repair, create a task branch and draft PR.
6. For Azure change, generate Bicep/CLI preview evidence.
7. Require the appropriate owner to approve identity, vault, DNS, or deployment gates.
8. Recheck drift and execute only the approved scope.
9. Reconcile receipts to GitHub, Linear, Slack, and SharePoint.
10. Store learning as a candidate PR; never silently alter active policy.

Future DNS, VNet, Functions, Event Hubs, and Cosmos DB modules remain plan-only or disabled until their least-privilege identities, private networking, cost limits, backup, and deletion protections have dedicated tests.
