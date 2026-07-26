# HELIOS instructions for Claude Code

HELIOS is an architecture-first, pull-request-first integration platform.

## Start here

1. Read `AGENTS.md`, `.github/copilot-instructions.md`, and
   `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`.
2. Use the repository-scoped `.mcp.json`. Approve only the servers needed for
   the current task.
3. Run `python3 scripts/dev/helios_dev_doctor.py --profile devcontainer`
   before implementation.
4. Keep changes on a task branch and use the owning GitHub issue and Linear
   item for acceptance.

## Provider and security boundary

- Claude is a peer implementer and reviewer, not the deployment authority.
- `azure-mcp-readonly` is an evidence source only.
- Azure DevOps is intentionally absent from Claude's project MCP profile.
  Microsoft's remote server does not currently authenticate non-Microsoft
  clients, and the local package has no hard read-only switch. Use the
  read-only VS Code profile or request a separate reviewed local connection.
- Hosted Foundry MCP is intentionally absent because its preview toolset
  includes writes. Use the read-only Azure MCP `foundryextensions` namespace
  for discovery until Foundry tenant/RBAC and tool approvals are reviewed.
- GitHub, Linear, and Slack project servers require their own interactive
  account authorization. Never copy tokens into repository files or prompts.
- Teams and SharePoint are reached through the governed HELIOS/Entra runtime
  and Microsoft Foundry configuration. Do not invent direct MCP endpoints.
- Never deploy Azure, change RBAC, grant tenant consent, publish agents, enable
  connector delivery, enable self-hosted runners, merge, write USB media, or
  modify Windows boot or sign-in without the separate human gate.
- Never request, print, summarize, or commit API keys, access tokens, client
  secrets, certificates, recovery material, or `.env.local` contents.
- Raw conversations are not training data. Hermes/XCore changes remain
  candidate-only until reviewed evidence is promoted.
