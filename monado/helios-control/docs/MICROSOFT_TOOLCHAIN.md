# Microsoft developer and Copilot toolchain

Helios uses one reproducible developer lane across Codespaces, local Dev Drive,
GitHub Actions, and Azure DevOps. `config/microsoft-toolchain.json` is the inventory;
the devcontainer supplies the baseline tools; `azd` previews the same hardened
Bicep used by the protected GitHub deployment workflow; tenant applications are registered separately through Entra and
solution pipelines.

## Merge boundaries

- Azure CLI and Bicep bootstrap and inspect Azure through interactive developer
  identity; `azd` is preview-only. GitHub Actions uses OIDC for application
  promotion.
- Power Platform CLI moves versioned Copilot Studio solutions between environments.
- Microsoft 365 Agents Toolkit (`atk`) packages published agents for Copilot and Teams; new work does not use deprecated TeamsFx.
- GitHub CLI and Copilot work through branches and pull requests.
- Azure DevOps remote MCP is configured read-only for supported local VS Code/Visual Studio clients. Broader Foundry or Copilot Studio support is not assumed while the remote server remains preview-limited.
- The official Azure MCP server is pinned for local use, starts in namespace mode,
  and carries `--read-only` in every shared configuration.
- VS Code Insiders is the isolated local workbench. GitHub Copilot and Claude Code
  consume the same reviewed repository and MCP boundary but retain separate sign-in
  sessions and never exchange provider credentials.
- Azure Functions exposes the Helios MCP endpoint at `/runtime/webhooks/mcp`.
- Foundry Hosted Agents run Hermes orchestration code with dedicated Entra identities.

## Authentication

Developer login is interactive. CI uses federated workload identity. Production
agents use managed identity. Key Vault is used only for services that cannot use
federation. PATs, client secrets, raw tokens, and recovery material never enter
source control or agent memory.

Run `scripts/Invoke-HeliosCliMatrix.ps1` to check independent CLI installations
in parallel. The command reports versions only; optional authentication probes
discard command output so tenant, subscription, and user details are not copied
into evidence. Installation remains the responsibility of the devcontainer or
approved enterprise software distribution.

## Provisioning order

1. Validate GitHub branch protection and CI.
2. Create Entra groups, workload identities, and least-privilege roles.
3. Run `azd provision --preview`, Bicep lint, and the protected Azure what-if.
4. Approve the separate protected deployment job for identity, runtime, and observability resources.
5. Register the Foundry toolbox and Helios MCP endpoint.
6. Deploy Hermes/XCore agents into a development Foundry project.
7. Evaluate and trace before publishing to Microsoft 365 Copilot or Teams.
8. Export Copilot Studio solutions and promote through governed environments.

The local agent fleet is a coordination layer, not a new trust boundary. Azure
Scout is read-only, Integration Builder is confined to the worktree, XCore
Evaluator may write evidence only, and Helios Governor can reject a promotion
but cannot approve one. See `MULTI_AGENT_WORKBENCH.md`.

Published Foundry agents receive dedicated identities. Development RBAC does not
silently transfer to published versions; production permissions are reassigned
explicitly after publication and before traffic promotion.
