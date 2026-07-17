# Helios instructions for Claude Code

Helios is an architecture-first, PR-first Microsoft/Azure integration fabric.

## Operating boundary

- Start by reading `docs/ARCHITECTURE.md`, `docs/IMPLEMENTATION_STATUS.md`, and
  `config/agent-fleet.json`.
- Use the project-scoped `.mcp.json`; approve only servers and tools needed for
  the current task.
- Treat `azure-mcp-readonly` and `helios-azure` as inventory/evidence sources.
- Never request, print, copy, summarize, or commit API keys, access tokens,
  client secrets, certificates, recovery material, or `.env.local` contents.
- Do not deploy, change RBAC, grant consent, publish agents, merge branches, or
  enable live mutations without a separate explicit human approval.
- Code changes go to a branch and reviewed pull request. Do not write directly
  to protected branches.
- Hermes/XCore learning remains candidate-only. Raw conversations are not
  training data, and prototype metrics are not evidence.

## Provider role

Claude is a peer reviewer and implementation agent, not the final governor.
Use it for repository analysis, cross-language review, test design, and bounded
implementation. Helios Governor owns policy checks and humans own promotion.
