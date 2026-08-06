# Hermes/XCore Microsoft learning plane

This document describes the planned learning plane, not the deployed connector
slice. Hermes remains the intended orchestrator. After administrator approval,
Microsoft Foundry Agent Service can supply managed agent identity, hosting,
Responses API access, MCP tools, evaluation, tracing, versioning, and
distribution through Microsoft 365 Copilot and Teams.

## Controlled learning flow

1. `xcore-teacher` reads approved skill scores and generates a synthetic task.
2. `hermes-orchestrator` routes it to the appropriate local, VM, or hosted node.
3. Execution occurs in an isolated sandbox with bounded time, data, and tools.
4. `xcore-evaluator` scores quality, latency, safety, and regression evidence.
5. Results enter approved Cosmos DB, Azure AI Search, and Data Lake candidate
   stores after those services and their identities are deployed; none is
   provisioned by the current Bicep.
6. `helios-governor` rejects, quarantines, or proposes a versioned promotion PR.
7. A human-approved PR promotes instructions, routing policy, curriculum, or code.
8. Application Insights monitors the new version and a reviewed automation path
   proposes or triggers rollback on regression; automatic rollback is not
   implemented in the current slice.

This is feedback-driven improvement, not unrestricted recursive self-modification.
Copilot conversations are not silently harvested as training data. Only approved,
sanitized sources enter the learning loop.

## Microsoft applications

- Microsoft Foundry Agent Service: Hosted and prompt agents, Responses API, tools, memory, tracing, evaluations, optimizer, stable endpoints.
- Microsoft 365 Copilot: governed employee-facing access to published Hermes agents.
- Copilot Studio: low-code topics, connectors, approval flows, and business actions.
- Microsoft Teams: operational interaction and approval channel.
- Entra Agent Registry: inventory, identity, ownership, and lifecycle governance.
- Azure DevOps MCP: scoped work-item and pipeline context through the Foundry toolbox.
- SharePoint/Work IQ/Fabric IQ: governed knowledge sources, never unrestricted training feeds.
