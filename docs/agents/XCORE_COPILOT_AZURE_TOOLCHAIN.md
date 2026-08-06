# XCore, Microsoft Copilot, and Azure Toolchain Extension

## Scope

This extension adds three explicit subagents to the 20-agent HELIOS Enterprise Deployment Manager fleet:

- `xcore-runtime`
- `microsoft-copilot`
- `azure-toolchain`

They remain subordinate to the governed fleet and do not gain independent production, identity, secret, or tenant-administration authority.

## XCore setup

XCore is the adaptive specialist runtime behind Hermes and AIHub.

```text
HELIOS Deployment Supervisor
             |
             v
     Hermes Registration
             |
             v
        XCore Runtime
      /       |       \
 Docker      WSL2    Hyper-V
 gateway     trainer  isolation
      \       |       /
       AIHub task/engine APIs
```

### Responsibilities

- contextual-bandit and specialist routing;
- bounded task generation and dispatch;
- worker and capability registration;
- engine-catalog consumption and recommendations;
- development training cycles;
- task history, reinforcement-memory, and vector-memory contracts;
- evaluation, reflection, telemetry, and resource recommendations.

### Safety boundary

XCore cannot deploy Azure resources, modify Entra/RBAC/Graph, read secrets, publish raw private memory, self-enable engines, or bypass approval gates.

The uploaded XCore/Hermes reference already models a route-execute-score-reflect loop with reinforcement and vector-memory writes. The existing AIHub service also exposes health, task, fleet, engine, security, and knowledge endpoints that become the XCore connection surface.

## Microsoft Copilot subagent

The Microsoft Copilot subagent coordinates:

- Microsoft 365 Agents Toolkit projects;
- Copilot Studio solutions and environments;
- Power Platform CLI authentication and solution lifecycle;
- Teams app and agent packages;
- declarative-agent validation;
- narrowly scoped Microsoft Graph actions;
- approved Foundry agent handoffs;
- HELIOS GUI readiness, approval, and publish state.

### Publish path

```text
Feature branch
  -> validate agent and Teams manifests
  -> export development solution/package
  -> Graph and action-contract tests
  -> review permissions and connection references
  -> protected approval
  -> publish to approved Teams/M365/Copilot Studio target
  -> evidence and rollback package
```

No tenant-wide publish, automatic admin consent, broad mailbox access, plaintext connection secrets, or production agent publication is permitted without approval.

## Azure and Microsoft tool setup

The manifest `config/toolchains/azure-microsoft-copilot-toolchain.v1.json` defines the approved Windows development stack:

- Azure CLI;
- Azure Developer CLI (`azd`);
- Bicep CLI;
- Azure Functions Core Tools;
- Power Platform CLI;
- Microsoft 365 Agents Toolkit;
- GitHub CLI;
- PowerShell 7;
- .NET 8 SDK;
- Docker Desktop;
- Node.js LTS;
- Visual Studio Code.

### Tool setup state machine

```text
Preflight
  -> PATH repair and dependency detection
  -> version and package-source inventory
  -> installation plan
  -> explicit elevation/upgrade approval
  -> approved install or repair
  -> terminal restart boundary
  -> version verification
  -> authentication handoff
  -> Bicep/C#/registry validation
  -> evidence report
```

Installation and login are separated. The toolchain subagent cannot authenticate accounts silently, choose a subscription, capture credentials, weaken security controls, or deploy production infrastructure.

## OpenAI handoff

The OpenAI Provider Agent uses the Agents SDK pattern of agents, tools, handoffs, guardrails, MCP integration, human approval, and tracing. XCore can be exposed as a bounded MCP toolset or specialist handoff, while Microsoft Copilot remains a separate Microsoft-native channel and policy surface.

The secure OpenAI key reference remains:

```text
OPENAI_API_KEY
OPENAI_PROJECT_ID
OPENAI_ORG_ID
```

The governed key name is `HELIOS Codex`; no raw key belongs in GitHub, Linear, Slack, Teams, SharePoint, AIHub memory, or agent traces.

## Validation totals

The fleet now validates:

- 20 core agents;
- 3 explicit subagents;
- 23 total operational roles;
- 11 original custom connections;
- 3 supplemental XCore/Copilot/toolchain connections;
- 12 required Azure and Microsoft development tools.

Production remains disabled while GitHub Issue #162 is open.
