# Helios multi-agent workbench

Helios uses one provider-neutral MCP boundary across VS Code Insiders, GitHub
Copilot, Claude Code, OpenAI Responses, Microsoft Foundry, and the local .NET
control plane. Providers do not share credentials or hidden conversation state.
They exchange only reviewed files, MCP results, and evidence artifacts.

## Workbench surfaces

- `HeliosControl.code-workspace` is the VS Code Insiders workbench.
- `.vscode/mcp.json` configures VS Code/Copilot.
- `.mcp.json` configures Claude Code at project scope.
- `.github/agents` defines four scoped agents that VS Code discovers.
- `config/agent-fleet.json` defines provider preference, concurrency, workflow,
  and forbidden actions.
- `config/cli-matrix.json` is the source of truth for concurrent CLI checks.

## Start safely

```powershell
# Show the fleet without starting providers.
pwsh ./scripts/Start-HeliosLocalFleet.ps1 -Mode Plan

# Check tool versions and authentication concurrently; command output is discarded
# for auth probes so tokens and account details are not reported.
pwsh ./scripts/Invoke-HeliosCliMatrix.ps1 -CheckAuthentication

# Open the isolated VS Code Insiders profile after explicit local confirmation.
$env:HELIOS_CONFIRM_LOCAL_WORKBENCH = 'YES'
pwsh ./scripts/Start-HeliosLocalFleet.ps1 -Mode OpenWorkbench
```

On WSL or Cloud Shell, run `bash scripts/invoke-helios-cli-matrix.sh`. Add
`--include-network-tools` only when downloading the pinned Azure MCP package is
acceptable.

## Claude Code

Install the pinned CLI through an approved Node toolchain, authenticate with
Claude Code's own account flow, open this repository, and review the project MCP
trust prompt. The repository does not request or store an Anthropic key.

```bash
npm install --global @anthropic-ai/claude-code@2.1.212
claude mcp list
claude
```

Use Claude for repository analysis, bounded implementation, and peer review.
It must not deploy, merge, modify RBAC, grant consent, publish agents, or consume
raw conversations as training data.

## Azure local and cloud split

- Local: `azure-mcp-readonly` reuses Azure CLI/`azd` credentials and exposes only
  selected namespaces with `--read-only`.
- Sanitized cloud: `helios-azure` calls the Entra-protected connector and returns
  only resource ID, name, type, and location.
- Deployment: GitHub OIDC and reviewed Bicep what-if remain the only CI path.
- Runtime: managed identity replaces developer credentials in Azure.

The local Azure MCP process is intentionally more capable than the sanitized
connector, so it remains read-only and interactive. Never remove `--read-only`
from shared project configuration. A separate, explicitly approved profile is
required for mutations.

## Fleet flow

1. Azure Scout gathers read-only resource, quota, pricing, monitoring, and
   Foundry evidence.
2. Integration Builder makes bounded local changes and tests them.
3. XCore Evaluator checks evidence and rejects prototype or fabricated metrics.
4. Helios Governor verifies security and promotion gates.
5. A human approves a pull request. No agent can auto-deploy or auto-merge.
