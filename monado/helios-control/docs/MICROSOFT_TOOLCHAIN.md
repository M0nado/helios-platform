# Microsoft developer and Copilot toolchain

Helios uses one reproducible developer lane across Codespaces, local Dev Drive,
GitHub Actions, and Azure DevOps. `config/microsoft-toolchain.json` is the inventory;
the devcontainer supplies the baseline tools; `azd` is limited to authentication
and environment inspection; tenant applications are registered separately
through Entra and solution pipelines.

## Merge boundaries

- Azure CLI and Bicep bootstrap and inspect Azure through interactive developer
  identity. Direct `azd provision` and `azd deploy` are blocked by
  `azure.yaml`; GitHub Actions uses OIDC for application promotion.
- Power Platform CLI moves versioned Copilot Studio solutions between environments.
- Microsoft 365 Agents Toolkit (`atk`) packages published agents for Copilot and Teams; new work does not use deprecated TeamsFx.
- GitHub CLI and Copilot work through branches and pull requests.
- Azure DevOps remote MCP is configured read-only for supported local VS Code/Visual Studio clients. Broader Foundry or Copilot Studio support is not assumed while the remote server remains preview-limited.
- The official Azure MCP server is pinned for local use, starts in namespace mode,
  and carries `--read-only` in every shared configuration.
- VS Code Insiders is the isolated local workbench. GitHub Copilot and Claude Code
  consume the same reviewed repository and MCP boundary but retain separate sign-in
  sessions and never exchange provider credentials.
- Azure Container Apps is the implemented hosting target. Its deployment output
  `connectorMcpUrl` resolves to `https://<container-app-host>/mcp`; the legacy
  Azure Functions `/runtime/webhooks/mcp` route is local compatibility only and
  is not the Microsoft agent toolbox endpoint.
- Foundry Hosted Agents are a later administrator-controlled target. No Foundry
  project, model deployment, hosted Hermes agent, or Agent 365 identity is
  created by the current connector.

## Authentication

Developer login is interactive. CI uses federated workload identity. Production
agents use managed identity. Key Vault is used only for services that cannot use
federation. PATs, client secrets, raw tokens, and recovery material never enter
source control or agent memory.

The deployed Bicep currently creates an empty, RBAC-enabled Key Vault but does
not grant the connector access to provider secrets or map any secret into the
Container App. Online OpenAI is therefore disabled and fails closed. Enabling it
requires an administrator-reviewed Key Vault secret/reference, managed-identity
RBAC, explicit model selection, and a new protected what-if/deployment; repository
scripts never accept or persist a plaintext provider key.

Run `scripts/Invoke-HeliosCliMatrix.ps1` to check independent CLI installations
in parallel. The command reports versions only; optional authentication probes
discard command output so tenant, subscription, and user details are not copied
into evidence. Installation remains the responsibility of the devcontainer or
approved enterprise software distribution.

## Provisioning order

1. Validate GitHub branch protection and CI.
2. Create Entra groups, workload identities, and least-privilege roles.
3. Run `scripts/Invoke-HeliosProvisionPreview.ps1`, Bicep lint, and the protected
   Azure what-if. `azd` does not preview or apply Helios infrastructure.
4. Approve the separate protected deployment job for identity, runtime, and observability resources.
5. After an administrator selects and approves a Foundry project, deployment,
   identity, and RBAC, register the deployed `connectorMcpUrl` toolbox. Its
   current tools are the three Azure inventory tools plus read-only HELIOS plan,
   proposal, run-status, and connector-status tools.
6. As a later vertical slice, deploy and evaluate Hermes/XCore agents in the
   approved development Foundry project.
7. Evaluate and trace before publishing to Microsoft 365 Copilot or Teams.
8. Export Copilot Studio solutions and promote through governed environments.

The local agent fleet is a coordination layer, not a new trust boundary. Azure
Scout is read-only, Integration Builder is confined to the worktree, XCore
Evaluator may write evidence only, and Helios Governor can reject a promotion
but cannot approve one. See `MULTI_AGENT_WORKBENCH.md`.

Published Foundry agents receive dedicated identities. Development RBAC does not
silently transfer to published versions; production permissions are reassigned
explicitly after publication and before traffic promotion.

APIM with a private Container Apps backend, Container Apps Jobs, Service Bus,
Data Lake, Azure AI Search, Foundry, and broader Cosmos learning stores are
target-architecture gates. The current Bicep implements only the serverless
Cosmos control-run container with a container-scoped managed-identity data role.
