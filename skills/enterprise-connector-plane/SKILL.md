---
name: enterprise-connector-plane
description: Register and validate HELIOS Microsoft Graph, Teams, SharePoint, OneDrive, Linear, Slack, Hermes, AIHub, and custom MCP/ChatGPT App connections.
---

# Enterprise Connector Plane

## Trigger

Use this skill when connecting HELIOS to Microsoft 365, collaboration systems, work tracking, agent runtimes, or custom tool surfaces.

## Principle

A connector is active only after authorization, least-privilege scope validation, a successful bounded health operation, and correlation-linked evidence. Possession of credentials alone is not sufficient.

## Connection sequence

1. Identify the destination owner, tenant/workspace, environment, and source issue.
2. Resolve the exact destination container: team/channel, SharePoint site/folder, Linear project/team, Slack channel, Hermes/AIHub endpoint, or custom app resource.
3. Declare read and write tools separately.
4. Validate OAuth/managed-identity scopes and admin-consent requirements.
5. Store only secret references; never include values in manifests or events.
6. Run a read-only health test.
7. Run one approved synthetic write where applicable.
8. Capture the destination link, operation result, correlation ID, and timestamp.
9. Register the connection in the HELIOS registry.
10. Publish a concise operational update.

## Destination contracts

### Microsoft Graph

- deny tenant-wide permissions by default;
- prefer selected-site or resource-scoped access;
- separate delegated setup identity from application runtime identity;
- record every granted permission and consent source.

### Teams

Canonical destination: `Helios / Helios Ops`.

Allowed default operations:

- post deployment and incident messages;
- reply to existing operational threads;
- resolve teams/channels;
- create a channel only when explicitly approved.

### SharePoint and OneDrive

Canonical governed path: `Helios/Governance` on the approved John More Microsoft 365 site.

Allowed default operations:

- create governed folders;
- upload versioned runbooks and evidence;
- inspect permissions and versions;
- create organization-scoped view links only when requested.

Never publish raw secrets, private agent memory, or unreviewed logs.

### Linear

Canonical project: `Helios Integration Fabric`, team `John` (`JOH`).

- GitHub remains the engineering source of truth;
- create linked implementation issues and status updates;
- preserve source URLs and correlation IDs;
- do not delete history or silently close GitHub issues.

### Slack

Canonical channels:

- `#helios-control-plane` for control-plane status;
- `#helios-ops` for private operational coordination.

- do not post secrets;
- do not search private channels or DMs without consent;
- prefer threads for repeated event updates.

### Hermes and AIHub

- register health, routing, task, engine, memory, trace, and evaluation endpoints;
- deny production deployment or identity privileges;
- publish normalized outcomes, not raw private memory;
- route repeated failures to a GitHub/Linear issue.

### Custom ChatGPT App and MCP

- every tool requires a stable name, JSON schema, auth mode, read/write classification, and human-readable effect;
- write tools require explicit approval semantics and idempotency behavior;
- tools must expose bounded resources rather than generic arbitrary API access;
- undeclared tools are unavailable;
- production app publishing requires review and environment approval.

## Output

Return a connection record containing:

```json
{
  "connectionId": "string",
  "system": "teams",
  "environment": "dev",
  "authMode": "oauth-or-managed-identity",
  "scopes": [],
  "toolsEnabled": [],
  "healthStatus": "healthy",
  "syntheticWrite": {
    "status": "succeeded",
    "url": "https://..."
  },
  "correlationId": "uuid",
  "secretReferences": [],
  "approvals": [],
  "evidence": []
}
```

## Prohibited

- broad unreviewed OAuth scopes;
- connector secrets in source control;
- arbitrary generic HTTP write tools;
- cross-tenant connections without explicit owner authorization;
- external/anonymous SharePoint links by default;
- Slack private-data search without consent;
- allowing Hermes, AIHub, or OpenAI agents to bypass platform approvals.
