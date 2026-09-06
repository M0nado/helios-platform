# Helios multi-agent workbench

HELIOS uses one governed control plane across Claude Code, Codex, ChatGPT,
GitHub Copilot, GitHub CLI and website, Azure CLI, Azure Developer CLI,
Azure DevOps, and the Monado operator UI. Clients exchange reviewed files,
normalized events, MCP results, and redacted evidence, not credentials or
hidden conversation state. Configuration does not prove live connectivity.

## Existing owning surfaces

- `HeliosControl.code-workspace` and `.vscode/mcp.json`: VS Code/Copilot.
- `.mcp.json` and `CLAUDE.md`: Claude Code project configuration and rules.
- `config/codex-mcp.example.toml`: disabled-by-default Codex HTTP example.
- `../../plugins/helios-control-fabric`: existing Codex plugin, MCP registry,
  skills, and governed CLI. Do not create another competing broker.
- `config/agent-fleet.json`: provider roles, bounded concurrency, and workflow.
- `config/cli-matrix.json`: version and noninteractive authentication probes,
  including Codex and the Azure DevOps CLI extension.
- `../../config/integrations/event-contract.schema.json`: shared event envelope.

The GitHub source, issue, branch, review, CI, and protected-environment record
is authoritative. Linear tracks dependencies and outcomes. Slack coordinates
operators. SharePoint stores approved runbooks and evidence. None of those
collaboration surfaces grants deployment or secret-management authority.

## Client authorization matrix

| Surface | Authorization and role | Readiness evidence |
|---|---|---|
| GitHub website | User browser sign-in; inspect issues, PRs, checks and approvals | Current immutable PR head and required checks |
| GitHub CLI | `gh auth login --web`; provider-owned credential store | Exit status of `gh auth status`; never print a token |
| Claude Code | Claude account sign-in plus project MCP trust | CLI version, trusted server discovery, then a read-only HELIOS call |
| Codex | `codex login` or device authorization; independent MCP OAuth | `codex login status`, approved MCP server and read-only tool proof |
| ChatGPT | Install/authorize the deployed HELIOS MCP app with its own OAuth flow | Entra-protected HTTPS endpoint, callback/PKCE and tool proof |
| Azure CLI / azd | Approved tenant/subscription and provider-managed user login | Read-only identity/context checks; no automatic subscription guess |
| Azure DevOps | Organization/project-scoped authorization; separate pipeline federation | Extension presence is not authentication; a scoped project read is needed |
| Foundry / cloud runtime | Reviewed deployment and workload/managed identity | Resource and model-specific health, identity, quota and evaluation proof |
| Local AIHub | Explicit local-development mode; authenticated loopback service | Real health and policy tests; no public unauthenticated prototype |

OpenAI API credentials belong only to the backend provider. They are not
GitHub credentials, Azure credentials, Claude credentials, or a replacement
for ChatGPT app OAuth. Store them outside tracked source or in approved Key
Vault bindings; do not put values in these profiles, logs, chats or evidence.

## Safe starting point

From this `monado/helios-control` directory:

```powershell
pwsh ./scripts/Start-HeliosLocalFleet.ps1 -Mode Plan
pwsh ./scripts/Invoke-HeliosCliMatrix.ps1 -CheckAuthentication
```

These are readiness operations, not proof that all clients are connected.
The existing legacy matrix runners report tool and authentication data
separately; a tool-only `ready` field must not be interpreted as end-to-end
readiness. Timeout/concurrency behavior must be validated before unattended
use. Do not run downloaded tools merely because they appear in a registry.

Review `config/codex-mcp.example.toml`, replace its non-secret endpoint with
the approved deployed HELIOS HTTPS origin, and merge it into a trusted
`.codex/config.toml`. It remains disabled until server and OAuth review.
Do not overwrite an existing user or project Codex configuration.

## Azure DevOps compatibility boundary

The official remote Azure DevOps MCP endpoint uses Microsoft Entra client
requirements that are not interchangeable across hosts. Keep its reviewed
VS Code profile read-only (`X-MCP-Readonly: true`) with selected toolsets.
Do not copy that profile into Codex and label it authenticated: Microsoft's
current documentation directs unsupported clients such as Codex to the local
Azure DevOps MCP server. Claude Code remote use requires a separately approved
Entra public client and callback configuration. No client registration or
consent is performed by this workbench.

A local Azure DevOps MCP package requires an exact reviewed version and its
own provider login. In CI, use the approved workload-identity service
connection, not a PAT copied from another client. Azure DevOps is a validation
and evidence lane, not a second automatic deployment authority.

## Bounded multi-model cooperation

Use an evidence packet with a correlation ID, source SHA, scope, allowed
paths/tools, data classification, acceptance tests, time and cost limits,
and rollback expectations. Keep specialist worktrees separate.

1. Azure Scout collects read-only resource and readiness evidence.
2. A selected implementation agent (Codex, Claude Code or Copilot) makes a
   bounded patch and runs the specified tests.
3. A different reviewer checks the patch and test evidence when available.
4. XCore evaluates measured outcomes, not self-reported confidence or synthetic
   training scores. Hermes routing changes remain candidates until evaluated.
5. HELIOS policy validation runs and a human reviews the PR.

Multiple models agreeing is not sufficient evidence of correctness or improved
performance. Compare quality, latency and measured provider usage against a
single-agent baseline. Bound handoffs, parallelism, retries and total tokens.
A budget rejection or data-residency restriction must not be bypassed through
fallback to another provider. There are no automatic production deployments,
merges, RBAC changes, consent grants, or raw-conversation training.

## Azure and storage integration

Retain the existing C# control plane and WinUI/Monado GUI; Python supplies
AIHub evaluation adapters, with C++/F# in their existing specialized lanes.
Do not change Dev Drive, partitions, accounts, vaults or VM state during
client onboarding. Their reviewed manifests remain a separate local workflow.

Key Vault holds approved durable provider secrets; runtime identities access
only their assigned resources. Cosmos DB is the intended state/metadata lane,
Blob/Data Lake holds artifacts and datasets, and Service Bus transports durable
work. These are architecture targets, not claims of deployed services.
Bicep validation, exact what-if evidence, and separate protected approval
remain required before any Azure provisioning.

## Source references

- Codex MCP: https://developers.openai.com/codex/mcp
- ChatGPT app authentication: https://developers.openai.com/apps-sdk/build/auth
- Claude Code MCP: https://code.claude.com/docs/en/mcp
- Azure DevOps remote MCP: https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server?view=azure-devops
- Local Azure DevOps MCP: https://github.com/microsoft/azure-devops-mcp
- GitHub CLI authentication: https://cli.github.com/manual/gh_auth_login

References describe product interfaces. Actual tenant/client readiness requires
fresh authenticated verification and a redacted correlated receipt.
