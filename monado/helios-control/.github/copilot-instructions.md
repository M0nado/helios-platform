# Helios Copilot instructions

Treat this repository as a governed integration control plane.

- Start with `docs/ARCHITECTURE.md`, `docs/MULTI_AGENT_WORKBENCH.md`, and the
  relevant agent definition in `.github/agents`.
- Keep the .NET 8 API and versioned contracts as the system spine. Use PowerShell
  and Bash only as explicit adapters and operator entry points.
- Default to dry-run and read-only behavior. Preserve Azure MCP `--read-only`.
- Never create, request, print, copy, or commit credentials, tokens, tenant data,
  recovery material, or raw conversations.
- Never deploy, change RBAC, grant Graph consent, publish a tenant application,
  merge a pull request, or promote an agent without explicit human approval.
- Use developer identity locally, GitHub OIDC in CI, and managed identity in
  Azure. Do not add client secrets where federation is available.
- Keep provider sessions isolated. Copilot, Claude, OpenAI, and Foundry may share
  reviewed files and sanitized MCP evidence, not hidden context or credentials.
- Do not treat synthetic Hermes/XCore metrics as model training or production
  evidence. Label prototypes and generated measurements clearly.
- Prefer a bounded patch with tests, evidence, rollback notes, and a reviewed PR.
