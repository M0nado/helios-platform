# HELIOS developer cockpit

The HELIOS developer cockpit is the first implementation slice of
[GitHub issue #194](https://github.com/M0nado/helios-platform/issues/194). It
provides one reproducible portable environment while keeping Windows desktop,
device-lab, tenant, and deployment privileges in separate lanes.

## Quick start

In GitHub Codespaces, choose **Create codespace** for this repository. In local
VS Code, install the recommended Dev Containers extension, clone the repository,
and run **Dev Containers: Reopen in Container**.

The post-create hook installs the locked user-space CLIs and runs:

```bash
python3 scripts/dev/helios_dev_doctor.py --profile devcontainer
```

Run the portable test slice with:

```bash
bash scripts/dev/portable-validate.sh
```

Run only configuration and guardrail checks on a machine that does not yet have
the toolchain:

```bash
bash scripts/dev/portable-validate.sh --contract-only
```

No bootstrap or validation command authenticates a provider or writes to a
cloud, tenant, repository, collaboration system, Windows boot configuration, or
physical device.

## Windows, Edge, and GUI

The devcontainer supports portable code and private forwarding of local port
`5080`. Microsoft Edge itself remains on the Windows UI side.

1. Open the repository in VS Code or VS Code connected to the Codespace.
2. Accept the recommended Microsoft Edge DevTools extension.
3. Press **F5** and choose either **HELIOS Setup Wizard in Microsoft Edge** or
   **HELIOS MCP App in Microsoft Edge**.
4. VS Code starts the local .NET connector with `HELIOS_EXECUTION_MODE=dry-run`
   and opens the private forwarded URL.

Hosted Windows CI compiles WPF and publishes build output with
`SHA256SUMS.txt`. It does not provide interactive rendering, boot verification,
sign-in verification, driver access, or physical USB access.

## Claude Code, Codex, Copilot, and MCP

- Claude Code reads root `CLAUDE.md` and root `.mcp.json`.
- VS Code and GitHub Copilot read `.vscode/mcp.json`.
- Codex reads `AGENTS.md`; the HELIOS Control Fabric plugin supplies the focused
  `helios-dev-cockpit` skill on plugin-capable surfaces.
- The local HELIOS MCP endpoint is `http://127.0.0.1:5080/mcp`.

Project MCP files contain no tokens. Each provider displays its own trust and
authentication flow. Approve only the servers needed for the current task.

| System | Developer-cockpit route | Default |
|---|---|---|
| GitHub | Standard remote GitHub MCP | Interactive auth |
| Linear | Standard remote Linear MCP | Interactive auth |
| Slack | Standard remote Slack MCP | Interactive auth |
| Azure | Pinned Azure MCP namespaces | Read-only |
| Azure DevOps in VS Code/Copilot | `Heli0sDev` remote project URL | Enforced read-only header |
| Azure DevOps in Claude/Codex | No default project binding | Local server requires separate write-risk review |
| Foundry discovery | Azure MCP `foundryextensions` namespace | Read-only |
| Hosted Foundry MCP | No default project binding | Separate tenant/RBAC and write-tool review |
| Teams | HELIOS Entra runtime/Microsoft package | Not directly bound |
| SharePoint | HELIOS Entra runtime or Foundry SharePoint tool | Not directly bound |

Teams and SharePoint intentionally have no invented direct MCP server entry.
Their runtime path needs the approved Entra application, delegated user
identity, exact tenant resource, and reviewed publication.

Microsoft’s remote Azure DevOps MCP currently authenticates only Visual Studio
Code and Visual Studio. The local `@azure-devops/mcp` package works with Claude
Code and Codex but exposes no hard read-only switch. It is therefore not
auto-bound in Claude's root `.mcp.json`; use the enforced read-only VS Code
route until a separate local write-risk decision is approved.

The hosted Foundry MCP preview also exposes both read and write tools. The
cockpit uses the integrity-locked local `azmcp` binary with `--read-only` and
the `foundryextensions` namespace for discovery; it does not auto-bind
`https://mcp.ai.azure.com`.

The official Microsoft 365 Agents Toolkit extension is recommended. Its
`@microsoft/m365agentstoolkit-cli` package is not installed automatically:
version `1.1.12` currently resolves to high-severity transitive npm advisories
without an upstream fix. A fixed release must pass the cockpit audit before the
CLI enters the lock.

## Foundry and SharePoint gate

For a Foundry agent to use SharePoint, an administrator must confirm:

- the Foundry project and SharePoint site are in the same tenant;
- the exact site URL and folder URL, not a copied browser-sharing URL;
- the project connection ID exposed to the runtime as
  `SHAREPOINT_PROJECT_CONNECTION_ID`;
- delegated user identity and consent; app-only identity is unsupported for
  this tool;
- one SharePoint tool binding per agent.

Keep these as deployment variables or Key Vault references. Do not commit them
as guessed values.

## Workstream boundaries

| Workstream | Owner | This cockpit does |
|---|---|---|
| Repository convergence | #149 / JOH-29 | Uses the canonical repository only |
| Developer environments/runners | #194 / JOH-42 | Implements the portable and hosted Windows lanes |
| Operator GUI | #172 / JOH-30 | Supplies local dry-run launch/debug hooks |
| Monado startup/sign-in visuals | #195 / JOH-43 | Supplies no Windows sign-in mutation |
| Guarded USB wizard | #171 / JOH-31 | Keeps the physical device lane disabled |
| Signed plugin SDK | #173 / JOH-32 | Adds one focused source skill; no marketplace publication |
| Azure/MCP activation | #192 / JOH-35/36 | Supplies read-only client config; no Azure action |
| Production integrity | #162 | Remains a production blocker |

## Administrator-only follow-ons

GitHub Enterprise account enrollment, enterprise-server registration,
organization ownership or policy changes, Entra consent, Azure RBAC, Foundry
publication, Teams app publication, SharePoint site promotion, self-hosted
runners, and device-lab activation are separate reviewed changes. The cockpit
diagnoses missing authority; it does not manufacture it.

References:

- [GitHub Codespaces dev containers](https://docs.github.com/en/codespaces/setting-up-your-project-for-codespaces/introduction-to-dev-containers)
- [Microsoft Edge DevTools for VS Code](https://learn.microsoft.com/en-us/microsoft-edge/visual-studio-code/microsoft-edge-devtools-extension)
- [Claude Code in VS Code](https://code.claude.com/docs/en/vs-code)
- [Claude Code MCP](https://code.claude.com/docs/en/mcp)
- [Codex MCP](https://developers.openai.com/codex/mcp)
- [OpenAI plugin skills](https://developers.openai.com/plugins/build/skills)
- [Foundry SharePoint tool](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/tools/sharepoint)
- [Remote Azure DevOps MCP setup](https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server)
- [Remote Azure DevOps MCP client compatibility](https://learn.microsoft.com/en-us/azure/devops/mcp-server/remote-mcp-server-troubleshooting)
