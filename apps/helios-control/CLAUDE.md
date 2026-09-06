# HELIOS Control App — Claude Code Instructions

This app is the shared Streamable HTTP MCP surface for ChatGPT and Microsoft Copilot. It must reuse the canonical HELIOS control plane rather than create a second administrator or connector stack.

## Default behavior

- Plan and inspect before editing.
- Keep the app read-only unless a tool contract and approval gate explicitly allow a write.
- Use the repository's Azure CLI plugin and enterprise setup manifest for cloud operations.
- Preserve correlation IDs, evidence URLs, and rollback notes.
- Keep production disabled while `M0nado/helios-platform#162` is open.

## Claude Code

Use the reviewed project MCP servers from `.mcp.json`. Claude Code may implement, test, and review the app and plugin. It may not grant tenant permissions, select an Azure subscription implicitly, change RBAC, publish Copilot agents, merge promotion PRs, expose credentials, or deploy production.

## Validation

Run the app checks, smoke test, Docker build, plugin unit tests, and setup-controller tests before requesting review.
