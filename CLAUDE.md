# HELIOS / Monadoblade Claude Code Instructions

## Source of truth

- Canonical runtime repository: `M0nado/helios-platform`
- Fleet adapter repository: `Yolkster64/hermes-fleet-platforms`
- Integration laboratory: `Yolkster64/monado-blade`
- Promotion targets: `M0nado/Helios-Control-Center`, `M0nado/helios-platform`, and reviewed `Heli0s-Dynamics` mirrors

Do not create competing repositories or bypass the reviewed promotion chain.

## Architecture

HELIOS is C#/.NET-first. PowerShell is a controlled Windows and tooling adapter. Python is used for AIHub and XCore reference services and safe CLI utilities. Bicep owns Azure infrastructure. JSON and YAML own declarative state.

## Required workflow

1. Start from a linked GitHub or Linear issue.
2. Inspect the relevant registry and skill contract.
3. Work on a feature branch.
4. Run targeted validation before broad validation.
5. Open or update a pull request.
6. Preserve correlation IDs, evidence links, and rollback notes.
7. Never silently merge, deploy, grant consent, change RBAC, publish an agent, rotate credentials, or perform a destructive operation.

## Permission mode

Default to planning and read-only inspection. Explicit approval is required for elevated installation, Azure subscription selection, Entra or federated identity changes, RBAC changes, Microsoft consent, credential writes, Copilot publication, GitHub merges, enterprise promotion, Azure deployment, and rollback.

Never use permission-skipping modes in HELIOS automation.

## Credentials

Do not print, commit, summarize, or persist raw provider, Azure, GitHub, Slack, Linear, or Microsoft credentials. Use interactive OAuth, workload identity federation, managed identity, connector-owned OAuth, or Key Vault references.

## Claude Code and MCP

Project MCP configuration lives in `.mcp.json`. Approve only reviewed project servers. Azure, GitHub, Linear, Foundry, and Agent 365 tools must operate with the signed-in user or approved workload identity and remain scoped to the requested environment.

Use Claude Code for architecture review, code changes, test generation, issue implementation, and evidence summaries. Claude Code is not an Azure, Entra, or Microsoft 365 administrator and cannot bypass tenant controls.

## Production gate

Production remains disabled while GitHub Issue `M0nado/helios-platform#162` is open. Even after closure, production requires the protected GitHub environment, immutable plan evidence, and explicit approval.
