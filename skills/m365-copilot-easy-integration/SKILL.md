---
name: m365-copilot-easy-integration
description: Route a normal Microsoft 365 Copilot request into bounded Outlook, Teams, SharePoint, OneDrive, Planner, Word, Excel, PowerPoint, Loop, Copilot Studio, Foundry, Edge, Cloud Shell, Azure DevOps, GitHub, and HELIOS specialist operations.
---

# Microsoft 365 Copilot Easy Integration

## Goal

Provide one simple Copilot-facing entry point while keeping every external effect inside a narrow specialist contract.

## Request flow

```text
Normal Microsoft 365 Copilot request
  -> classify intent
  -> resolve user, tenant, destination, and environment
  -> select one or more bounded specialists
  -> preview external effects
  -> request approval where required
  -> execute through the owning connector
  -> return links, correlation ID, and evidence
```

## Specialist routing

- Outlook: draft by default; send only on explicit request.
- Teams: resolve team/channel, then post or reply.
- SharePoint/OneDrive: read, publish, version, move, or permission-audit within approved sites and drives.
- Planner: list/create/update tasks; delete only with explicit approval.
- Word/Excel/PowerPoint/Loop: create or update through declared document tools; no macros or arbitrary embedded code.
- Copilot Studio: validate solution/environment, connection references, topics, actions, and publish package.
- Foundry: hand off model, agent, evaluation, trace, and tool-connection work to the Foundry Agent Service subagent.
- Edge: open approved workspaces and deep links; never expose private browser data.
- Azure Cloud Shell: discovery and what-if by default; apply remains approval-gated.
- Azure DevOps: read projects, boards, repos, pipelines, and artifacts; writes require scoped approval.
- GitHub: source of truth for code, issues, PRs, Actions, Codespaces, and promotion.
- XCore/Hermes/AIHub: route bounded intelligence tasks without granting cloud administrative rights.

## Easy user commands

The HELIOS GUI, Copilot, ChatGPT App, or CLI should accept requests such as:

```text
Set up the HELIOS developer tools.
Show Azure readiness.
Run a dev what-if.
Create a Teams deployment update.
Publish the release evidence to SharePoint.
Create the linked Linear work item.
Prepare a Copilot Studio agent package.
Open the HELIOS Edge workspace.
Check Foundry and XCore health.
Promote the validated commit through GitHub.
```

The orchestrator converts each request into a typed operation rather than free-form shell commands.

## Approval rules

Require explicit approval for:

- sending mail;
- deleting or externally sharing content;
- new Graph or OAuth scopes;
- Copilot or Teams production publication;
- Azure subscription selection, RBAC, apply, or rollback;
- Azure DevOps pipeline execution or service-connection changes;
- GitHub merge, protected-branch change, enterprise promotion, or archive;
- package/extension installation requiring elevation.

## Output

Return:

- interpreted intent;
- selected specialists;
- planned external effects;
- approval state;
- execution status;
- destination links;
- correlation ID;
- evidence and rollback references;
- blocked items with exact authorization needed.

## Prohibited

- generic unrestricted shell or HTTP tools;
- automatic admin consent;
- implicit production actions;
- credentials in prompts, logs, source, Teams, Slack, Linear, or SharePoint;
- treating Copilot as a tenant administrator;
- bypassing GitHub or Microsoft approval gates.
