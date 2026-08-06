# HELIOS Azure Enterprise Agent Fleet

## Goal

The fleet turns the Enterprise Deployment Manager into a governed multi-agent control plane. Each agent has a narrow operational domain, separate identity requirements, explicit denied actions, validation outputs, and approval boundaries.

The design intentionally avoids a single all-powerful automation principal.

## Control-plane topology

```text
GitHub issue / Linear work item / HELIOS GUI request
                         |
                         v
              Deployment Supervisor
                         |
        +----------------+----------------+
        |                |                |
        v                v                v
 Azure foundation   Enterprise APIs   Assurance plane
        |                |                |
 az/azd/Bicep       Graph/Slack/Linear  Validate/Rollback
 Entra/OIDC         Teams/SharePoint    Compliance/Telemetry
 Key Vault          Plugins/MCP         Evidence
 Foundry
 Container Apps
        |
        v
 Hermes + AIHub registration and evaluation
```

## Agent groups

### Azure foundation

| Agent | Primary responsibility | Default mode |
|---|---|---|
| Azure CLI and azd Bootstrap | Tooling, login state, tenant/subscription context, Bicep compilation | Read/validate |
| Bicep Infrastructure | Modules, parameters, outputs, what-if, drift | What-if only |
| Entra Identity | Managed identities, app registrations, RBAC plan | Plan only |
| GitHub OIDC | Federated credentials and protected environment readiness | Validate only |
| Key Vault and Secret Broker | Secret references, access tests, rotation metadata | Metadata only |
| Foundry Platform | Foundry project, model deployments, evaluations, agent endpoints | Plan/evaluate |
| Container Broker | Container Apps/Functions integration broker and revisions | Validate only |

### Enterprise connectors

| Agent | Destination | Write boundary |
|---|---|---|
| Microsoft Graph | Common Graph permission and connection layer | Admin consent required for new app permissions |
| Teams Operations | `Helios / Helios Ops` | Messages and approved channel operations |
| SharePoint Publisher | `Helios/Governance` and approved evidence libraries | No anonymous sharing or secrets |
| Linear Synchronizer | `Helios Integration Fabric` | Status/issues/comments; GitHub remains source of truth |
| Slack Operations | `#helios-control-plane` and approved operations channels | No secret posting or private search without consent |
| Custom Connection Manager | ChatGPT App/MCP/plugin manifests | Deny-by-default tool registration |

### Intelligence and runtime

| Agent | Responsibility | Restriction |
|---|---|---|
| Hermes Registration | Worker registration, routing, skills, evaluations | No production deployment permission |
| AIHub Registration | Endpoints, engine catalog, tasks, memory/telemetry | No raw memory publication |
| OpenAI Provider | Model calls, bounded tools, traces, evaluations | No API-key readback or implicit write tools |

### Assurance

| Agent | Responsibility |
|---|---|
| Validation and Observability | Synthetic checks, Application Insights correlation, scorecards |
| Rollback Controller | Revisions, parameter restoration, permission-revoke plan |
| Compliance Evidence | Hashes, workflow links, Azure correlation, RBAC/Graph summaries, SharePoint publishing |

## Deployment phases

1. **Bootstrap** — inspect tools and authentication without capturing credentials.
2. **Discover** — enumerate tenant, subscription, repository, project, and connector context.
3. **Plan** — resolve IaC outputs and produce a deployment graph.
4. **Validate** — compile Bicep, run tests, check permissions, and execute what-if.
5. **Approve** — protected GitHub environment approval for any production write.
6. **Apply** — execute only the approved immutable plan.
7. **Register** — connect broker, Foundry, Graph, Hermes, AIHub, Linear, Slack, Teams, and SharePoint.
8. **Verify** — send correlated synthetic events and query telemetry.
9. **Publish evidence** — versioned SharePoint and artifact-storage evidence.
10. **Rollback ready** — record an executable rollback point and permission-reversal plan.

## Azure scope

The fleet is designed to manage the following approved platform areas through IaC and scoped identities:

- Azure CLI, Azure Developer CLI, Bicep, and Functions Core Tools;
- resource groups and deterministic naming;
- Log Analytics and Application Insights;
- Storage and Data Lake;
- Key Vault and private endpoints;
- Cosmos DB;
- Azure Container Registry;
- Container Apps and Azure Functions;
- Service Bus and Event Grid/Event Hubs where the broker requires them;
- API Management;
- Azure AI Search;
- Azure AI Foundry projects, model deployments, evaluations, and agent endpoints;
- Entra managed identities, application registrations, workload identity federation, and least-privilege RBAC;
- private networking and private DNS;
- cost, drift, policy, and deployment evidence.

## Connector architecture

All external systems consume a normalized event envelope. Connector adapters must implement:

```text
authorize -> validate scopes -> health check -> receive/send event -> return evidence
```

A connection is not considered active merely because credentials exist. The agent must prove a scoped operation and return a correlation-linked result.

## OpenAI integration

The OpenAI Provider Agent is an implementation boundary, not a global administrator. It may:

- route requests to approved models;
- invoke explicitly registered tools;
- emit traces and evaluation results;
- package bounded Codex tasks;
- collaborate with Hermes routing and the HELIOS GUI.

It may not expose `OPENAI_API_KEY`, enable arbitrary shell execution, or grant itself new write tools. The secure Platform key flow uses the `HELIOS Codex` key name and stores only a reference in configuration.

## Custom ChatGPT App and MCP connections

Custom connections are described by `config/plugins/custom-connections.v1.json`. Each connection declares:

- owner and environment;
- authentication mode;
- allowed resources and tools;
- read/write classification;
- approval requirements;
- secret reference names;
- health-check operation;
- evidence destination.

Undeclared tools are unavailable. A new write-capable tool requires pull-request review and production app approval.

## Issue #162 gate

Production remains disabled until:

1. agent/binding configuration is covered by an integrity manifest;
2. canonical binding names consume actual Bicep outputs;
3. Cosmos DB and Container Apps module expectations match the IaC implementation;
4. a workflow compares the binding registry with what-if output;
5. the production approval environment is configured.

## Definition of done

The fleet is complete when all 20 agents pass schema validation, connector health tests are correlated end to end, the OpenAI provider uses a secure key reference, GitHub and Linear work remain linked, SharePoint evidence is versioned, and a production apply cannot occur without the protected approval gate.
