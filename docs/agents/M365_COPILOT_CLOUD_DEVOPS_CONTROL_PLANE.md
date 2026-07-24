# HELIOS Microsoft 365 Copilot, Edge, Cloud Shell, DevOps, and GitHub Control Plane

## Objective

Make the HELIOS platform easy to operate from normal Microsoft 365 Copilot while preserving strict ownership boundaries between Microsoft Graph, Azure, GitHub, Azure DevOps, Foundry, OpenAI, XCore, Hermes, AIHub, Teams, SharePoint, Slack, and Linear.

## User experience

The user interacts with one Copilot or HELIOS command surface. The orchestrator delegates the request to a bounded specialist and returns direct evidence links.

```text
Microsoft 365 Copilot / HELIOS GUI / ChatGPT App / CLI
                         |
                         v
              M365 Copilot Orchestrator
                         |
     +-------------------+-------------------+
     |                   |                   |
     v                   v                   v
Productivity         Cloud engineering    Intelligence
Outlook/Teams        Azure/Cloud Shell    XCore/Hermes
SharePoint/Drive     Azure DevOps          AIHub/OpenAI
Planner/Office       GitHub/Foundry        evaluations
```

## Microsoft 365 specialist pool

The normal Copilot front door may route to:

- Outlook drafting and explicitly approved sending;
- Teams channel posts, replies, and operational summaries;
- SharePoint governed runbooks and evidence publishing;
- OneDrive file operations;
- Planner tasks;
- Word, Excel, PowerPoint, and Loop document workflows;
- Copilot Studio solution validation and publication planning;
- Teams app and declarative-agent packages;
- narrowly scoped Graph actions.

## Edge workspace

The Edge workspace is a navigation and review surface containing approved links for:

- Microsoft 365 Copilot;
- Copilot Studio;
- Teams and SharePoint;
- Azure Portal and Cloud Shell;
- Azure AI Foundry;
- GitHub and Codespaces;
- Azure DevOps;
- Linear;
- HELIOS GUI, Hermes, and AIHub dashboards.

Browser-private data and credentials are outside the agent contract.

## Azure Cloud Shell and unified CLI

Cloud Shell provides an authenticated Azure command surface without requiring a local installation. The unified CLI routes commands to the correct specialist:

```text
helios tools status
helios azure account
helios azure what-if dev
helios foundry status
helios copilot validate
helios github status
helios devops inventory
helios xcore health
helios publish evidence
```

These are typed operations, not arbitrary command concatenation. Planning and read operations are the default. Writes, elevation, package installation, RBAC, production deployment, rollback, merges, and publication require approval.

## GitHub repository control

GitHub remains the engineering source of truth. The control subagent manages the reviewed hierarchy:

```text
Yolkster64 integration and fleet repositories
                 -> M0nado canonical repositories
                 -> reviewed enterprise release mirrors
```

The connected write scope in this environment is limited to:

- `M0nado/helios-platform`
- `Yolkster64/hermes-fleet-platforms`

Other repositories remain declared promotion targets and must be authorized before direct mutation. Promotion uses immutable source SHAs, pull requests, required checks, protected environments, and evidence.

## Azure DevOps

Azure DevOps is an enterprise companion, not a replacement for the canonical GitHub repository. Supported domains include:

- organizations and projects;
- repositories and mirrors;
- Boards;
- Pipelines and environments;
- Artifacts;
- service-connection planning;
- read-only MCP integration by default.

Pipeline execution, service-connection writes, environment changes, and repository mirror writes require approval.

## Foundry Agent Service

The Foundry subagent owns:

- project readiness;
- model deployment plans;
- Agent Service definitions;
- Entra Agent identity plans;
- evaluations and traces;
- approved tool connections;
- Copilot Studio and HELIOS handoffs.

It cannot publish a production agent, deploy a model, or add a new tool connection without approval.

## Security model

- GitHub Actions uses Entra workload identity federation.
- Azure-hosted agents use managed identities.
- Interactive Cloud Shell and developer setup use the signed-in user context.
- Microsoft Graph permissions are resource-scoped where possible.
- OpenAI uses an environment or Key Vault reference.
- No secret values are stored in registries or collaboration messages.
- Production remains blocked by GitHub Issue #162.

## Operational role count

```text
20 core enterprise agents
3 first-wave subagents: XCore, Microsoft Copilot, Azure Toolchain
8 cloud/productivity/DevOps subagents
31 governed operational roles total
```

## Definition of done

The extension is ready when the registries validate in CI, the M365 Copilot orchestrator can resolve all specialist destinations, Cloud Shell and CLI readiness checks succeed, GitHub and Azure DevOps boundaries are enforced, Foundry/XCore health checks are linked, and Teams, Slack, Linear, and SharePoint contain correlated status records.
