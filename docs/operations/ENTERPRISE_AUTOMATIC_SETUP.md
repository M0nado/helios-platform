# HELIOS Enterprise Automatic Setup

## Goal

Provide one dry-run-first setup command for Claude Code, Microsoft 365 Copilot and Agent 365, Azure CLI and Cloud Shell, Azure DevOps, GitHub, Slack, Linear, SharePoint, Teams, Foundry, XCore, Hermes, AIHub, and OpenAI.

## One command

```powershell
py .\tools\helios-setup\helios_setup.py plan
```

After reviewing the generated plan:

```powershell
py .\tools\helios-setup\helios_setup.py execute --phase tools --confirm "INSTALL HELIOS TOOLS"
py .\tools\helios-setup\helios_setup.py execute --phase developer-auth --confirm "AUTHENTICATE HELIOS"
py .\tools\helios-setup\helios_setup.py execute --phase context --confirm "SELECT HELIOS TARGETS"
py .\tools\helios-setup\helios_setup.py execute --phase agent365-developer --confirm "PREPARE HELIOS AGENT 365"
py .\tools\helios-setup\helios_setup.py execute --phase azure-plan --confirm "RUN HELIOS DEV WHATIF"
py .\tools\helios-setup\helios_setup.py execute --phase connections --confirm "VALIDATE HELIOS CONNECTIONS"
```

No production command is exposed while GitHub Issue #162 is open.

## Environment-only target bindings

Set these in the current operator shell. Do not commit them:

```text
HELIOS_AZURE_SUBSCRIPTION_ID
HELIOS_AZURE_DEVOPS_ORG
HELIOS_AZURE_DEVOPS_PROJECT
HELIOS_AGENT365_CONFIG_DIR
AZURE_TENANT_ID
```

## Authentication split

The setup automates preparation but never invents credentials or approvals:

- Azure CLI and `azd`: interactive Azure developer identity
- GitHub: OAuth locally and OIDC in Actions
- Claude Code: Claude account or Anthropic Console OAuth
- Agent 365 and Copilot: developer blueprint first, Global Administrator consent handoff second
- Slack and Linear: connector-owned OAuth
- Teams, SharePoint, and OneDrive: Microsoft Graph or Agent 365 authorization
- Azure-hosted agents: managed identity
- provider credentials: Key Vault references only

## Claude Code

The repository includes:

- `CLAUDE.md` for project governance
- `.claude/settings.json` for safe default permissions
- `.mcp.json` for Azure, GitHub, Linear, Foundry, and Agent 365 MCP servers

Run:

```powershell
npm install -g @anthropic-ai/claude-code
claude doctor
claude mcp list
claude --permission-mode plan
```

Approve project MCP servers only after reviewing `.mcp.json`.

## Microsoft 365 Copilot and Agent 365

Developer flow:

```powershell
a365 setup requirements
a365 setup all --dry-run
```

Administrator handoff:

```powershell
a365 config display
a365 setup admin --config-dir <reviewed-config-directory>
```

Only approved MCP scopes should be granted. Teams, OneDrive and SharePoint, Mail, Calendar, and Microsoft 365 Copilot are declared separately so permissions remain auditable.

## Azure and Azure DevOps

```powershell
az login
az account list --output table
az account set --subscription <approved-subscription-id>
az extension add --name azure-devops
az devops configure --defaults organization=<approved-org> project=<approved-project>
az devops project list
```

Azure Cloud Shell is an authenticated operator surface. Local and CI automation still use the same manifests and approval rules.

## GitHub

Canonical source: `M0nado/helios-platform`.

Promotion sequence:

```text
Yolkster64 integration and fleet repositories
  -> M0nado canonical repositories
  -> reviewed Heli0s-Dynamics enterprise mirrors
```

Every promotion uses an immutable SHA and reviewed PR. No direct copying to protected branches.

## Live connector validation

The setup validates:

- GitHub issue, pull request, and workflow access
- Linear project and issue synchronization
- Slack operations-channel posting
- Teams Helios Ops posting
- SharePoint Governance publishing
- Azure DevOps project and pipeline visibility
- Foundry endpoint readiness
- XCore, Hermes, and AIHub health

## Safety

The setup never performs Azure Owner grants, tenant-wide Graph consent, unreviewed RBAC, credential export, automatic production publishing, destructive resource deletion, or production deployment without the protected approval gate.
