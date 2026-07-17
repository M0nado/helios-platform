# Helios integration implementation status

## Implemented in this change

- Buildable .NET 8 webhook/API project and tests.
- GitHub, Linear, and Slack HMAC verification in live mode.
- Slack replay-window enforcement, request bounds, JSON validation, and process-local duplicate rejection.
- Local-only, read-only MCP tools for Copilot Chat.
- Hardened Azure Bicep resources for managed identity, Key Vault, Container
  Apps, Log Analytics, and Application Insights, with an external immutable ACR
  binding. Service Bus, ADLS, Cosmos, AI Search, and APIM remain architecture
  milestones rather than deployable connector resources.
- OIDC Azure what-if workflow and separate Copilot package validation workflow.
- OpenAI Responses provider with explicit model selection and environment/Key Vault credential references.
- Microsoft identity, toolchain, Agent 365, Teams/Copilot package, and approval contracts.
- Project-scoped Claude Code and VS Code Insiders MCP manifests.
- Pinned, namespace-scoped Azure MCP with enforced read-only startup.
- Concurrent PowerShell and Bash CLI inventory checks with non-reporting auth probes.
- Four bounded local agent definitions and a provider-neutral dry-run fleet contract.
- An isolated VS Code Insiders profile launcher requiring explicit local confirmation.
- Cloud-runtime contract that removes workstation dependencies from production.
- Immutable-image Container Apps deployment, liveness/readiness probes, and a
  protected GitHub OIDC workflow with hashed what-if evidence, drift detection,
  and a distinct second deployment approval.
- Interactive Azure/Entra bootstrap that resolves immutable GitHub OIDC subjects,
  verifies reviewer/branch protection before trust, registers exact providers,
  and keeps routine CI free of role-assignment authority.
- Entra-protected, read-only remote MCP with RFC 9728 discovery, Origin checks,
  Streamable HTTP lifecycle handling, strict JSON-RPC errors, and live ARM
  inventory verification.

## Intentionally not enabled

- Teams, SharePoint, Foundry, and Copilot inbound webhooks fail closed until Entra JWT and validation-challenge middleware is configured.
- No remote MCP or Azure runtime is live until an administrator completes the
  Configure and Publish phases and a reviewer approves both cloud workflow gates.
- Remote MCP exposes only three read-only inventory tools; SSE server push and
  stateful MCP sessions are intentionally unsupported.
- `hermes.run_sandbox`, task generation, evaluation writes, and promotion tools are not exposed.
- Tenant-wide Graph consent, Copilot publication, Conditional Access changes, production RBAC, and Azure deployment require explicit administrator approval.
- No CLI, VS Code extension, Claude package, or tenant application is installed
  automatically by repository scripts.
- No Claude, Copilot, OpenAI, or Foundry provider session starts automatically.
- Service Bus workers, Cosmos idempotency, signed public webhook ingress,
  Foundry/Claude cloud adapters, APIM policies, and OpenTelemetry are specified
  but not yet implemented in the connector API.

## Attached Python source reality

The uploaded Hermes/XCore Python files are prototypes. The current training loop
depends on a missing `hermes_xcore` package and generates synthetic quality/latency
scores rather than training a model. VM orchestration and ML registry files are
metadata scaffolds. They must not write governed learning state until normalized
into a package with real sandbox execution, evidence, evaluation, lineage, and rollback.

## Next vertical slice

1. Green CI and Bicep validation.
2. Shared versioned event schemas with `M0nado/helios-platform`.
3. Service Bus persistence and Cosmos idempotency.
4. Real isolated worker evidence, marked synthetic until validated.
5. Foundry Hosted Agent binding to the verified Entra-authenticated remote MCP.
6. Evaluated, versioned publication to Microsoft 365 Copilot and Teams.

## Activation still required

An operator must install the declared toolchain, authenticate Azure CLI, `azd`,
GitHub CLI, VS Code Insiders/Copilot, and Claude Code independently, then review
the MCP trust prompts. An Azure administrator must still select the subscription,
resource group, Foundry project/deployment, Entra identities/RBAC, Copilot Studio
environment, Graph consent, and publication targets. These values are not guessed
or embedded in the bundle.
