---
name: helios-dev-cockpit
description: Set up, diagnose, validate, or explain the HELIOS Codespaces, VS Code, Microsoft Edge DevTools, Claude Code, Codex, Copilot, MCP, and Windows runner developer cockpit.
---

# HELIOS developer cockpit

Use this skill for developer environment setup, toolchain drift, project MCP
trust, Edge debugging, Claude/Codex/Copilot interoperability, and runner-lane
selection.

## Start

1. Read `AGENTS.md`, `.github/copilot-instructions.md`, `CLAUDE.md`, and
   `config/dev/toolchain-lock.json`.
2. Run the contract-only check before installing anything:

   ```bash
   bash scripts/dev/portable-validate.sh --contract-only
   ```

3. Prefer the checked-in `.devcontainer/devcontainer.json` for a clean
   environment. After creation, run:

   ```bash
   python3 scripts/dev/helios_dev_doctor.py --profile devcontainer
   ```

4. Use the root `.vscode/mcp.json` for VS Code/Copilot and root `.mcp.json` for
   Claude Code. Approve and authenticate only the servers required by the
   current task.

## Lane selection

- Codespaces/devcontainer: portable .NET, Python, C++20, Node, local dry-run
  MCP, Codex, Claude Code, Copilot, and private port forwarding.
- Windows hosted runner: WPF compilation, Windows-targeted validation, and
  hashed build artifacts. It is not an interactive desktop or device lab.
- Local Windows + Edge: render and inspect the setup wizard or MCP app through
  the checked-in `msedge` launch profiles.
- Device lab: disabled. USB writes, boot changes, sign-in changes, and driver
  operations remain under GitHub issue #171 and require separate approval.

## Connector boundary

- GitHub, Linear, and Slack use their standard remote project MCP endpoints and
  provider-specific interactive authorization.
- Azure MCP is read-only. VS Code may use Microsoft's remote Azure DevOps MCP
  with `X-MCP-Readonly: true`.
- Do not add the remote Azure DevOps MCP to Claude Code or Codex: Microsoft
  does not currently support those clients on the remote OAuth flow. The local
  package is a separate reviewed opt-in because it has no hard read-only mode.
- Use Azure MCP's read-only `foundryextensions` namespace for Foundry
  discovery. Do not auto-bind the hosted Foundry MCP preview; it exposes write
  tools and needs separate tenant, RBAC, and tool-approval review.
- Teams and SharePoint use the governed HELIOS Entra runtime and Foundry
  bindings. Do not invent direct project MCP endpoints for them.
- An Azure/Foundry/SharePoint connection requires confirmed tenant, project,
  connection ID, site URL, folder URL, and user-delegated consent. App-only
  SharePoint identity is not a substitute.

## Never do automatically

Do not deploy Azure, mutate RBAC, grant tenant consent, change GitHub Enterprise
or organization policy, publish Microsoft 365 apps, enable connector delivery,
enable self-hosted runners, merge, write USB media, or modify Windows boot or
sign-in. Report the exact administrator gate instead.
