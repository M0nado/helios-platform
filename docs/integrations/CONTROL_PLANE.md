# HELIOS Collaboration and Automation Control Plane

## Canonical systems

| System | Responsibility |
|---|---|
| GitHub | Source, architecture, issues, pull requests, Actions, releases |
| Linear | Sprint planning, estimates, execution status |
| Slack | Fast collaboration, CI/security/deployment alerts |
| Microsoft Teams | Microsoft 365 collaboration, meetings, operational threads |
| SharePoint | Governed runbooks, policies, release evidence, architecture records |
| Azure | Infrastructure, Entra identity, Key Vault, Functions, Foundry, telemetry |
| Hermes | Agent routing, evaluation, learning, task automation |

## Repositories

- Canonical platform: `M0nado/helios-platform`
- Companion agent fleet: `Yolkster64/hermes-fleet-platforms`

Satellite repositories must not become competing sources of truth. They should be archived, mirrored, or represented as modules/packages consumed by the canonical platform.

## Event model

All integrations should consume a normalized event envelope:

```json
{
  "source": "github",
  "eventType": "pull_request.closed",
  "repository": "M0nado/helios-platform",
  "entityId": "123",
  "correlationId": "github-run-or-delivery-id",
  "environment": "development",
  "occurredAt": "UTC timestamp",
  "links": []
}
```

Use an Azure Function or Container App as the integration broker. Authenticate GitHub Actions to Azure with workload identity federation. Store third-party secrets in Azure Key Vault.

## Workflow

1. Approved GitHub issues carrying `linear-sync` are mirrored into Linear.
2. Linear workflow status is reflected back through labels/comments, without replacing GitHub history.
3. Pull requests trigger build, test, dependency, secret, and security checks.
4. Failed control-plane workflows alert Slack and Teams.
5. Merged releases generate release notes and evidence bundles.
6. Release evidence is published to SharePoint through Microsoft Graph.
7. Approved deployments use Azure environments and explicit production gates.
8. Hermes evaluates task routing, build outcomes, repeated failures, and operational signals.

## Required Azure resources

- Entra application registrations or managed identities
- Workload identity federation for GitHub Actions
- Azure Key Vault
- Azure Function or Container App integration broker
- Storage account for durable event/audit payloads
- Application Insights and Log Analytics
- Azure AI Foundry project and model deployments when enabled
- Private endpoints and VNet integration where required

## Microsoft 365 integration

Use Microsoft Graph with least-privilege application permissions:

- Teams: channel alerts and deployment cards
- SharePoint: release evidence and runbook publishing
- OneDrive: user-controlled drafts or portable exports only
- Purview: labels and governance applied to published artifacts

Admin consent must be explicit. Do not grant broad tenant permissions to desktop scripts.

## Safety gates

The following always require approval and rollback evidence:

- disk formatting or partition changes;
- BitLocker/VHDX key changes;
- WDAC/AppLocker policy enforcement;
- firewall lockdown or process termination;
- production Azure deployment;
- identity/RBAC changes;
- secret rotation;
- publishing sensitive evidence outside the approved SharePoint location.

## Initial status mapping

| GitHub/PR state | Linear state | Alert behavior |
|---|---|---|
| Planned issue | Backlog | None |
| `in-progress` label | In Progress | Optional Slack update |
| PR open/ready | In Review | Slack review notice |
| CI failure | In Review / Blocked | Slack + Teams alert |
| PR merged | Done | Release channel update |
| Production deployment failed | Incident | Slack + Teams urgent alert |
