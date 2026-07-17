# HELIOS Azure Setup Wizard

The wizard is a personal Microsoft 365/Teams tab and ordinary Edge web app served by the HELIOS control API at `/setup`. The same plan and upgrade capabilities are available to Copilot, ChatGPT, and Codex through authenticated MCP tools.

## Setup flow

1. Enter the Azure tenant ID, subscription ID, resource group, and environment.
2. Select **Generate Cloud Shell setup**.
3. Review and copy the generated PowerShell into Azure Cloud Shell.
4. Complete Microsoft device authentication in the browser.
5. The script verifies the selected Azure context, runs HELIOS `Diagnose`, then runs `Plan`.
6. Review the resulting ARM what-if file and SHA-256 digest.
7. Stop. Apply remains a separate protected workflow with fresh drift verification and typed confirmation.

The generated script contains identifiers but no credentials, tokens, secret values, role grants, resource deletion, or apply command. Input validation rejects shell metacharacters and control characters.

## Automatic upgrades

The **Upgrade agent** panel or `helios_propose_upgrade` MCP tool accepts a capability name and reason. It returns a deterministic proposal ID and required checks. Codex or Copilot can use that proposal to create a task branch, implement the scoped capability, run validation, and open a draft pull request.

Promotion is deliberately separate from proposal generation. Every upgrade requires schema validation, security guardrails, unit tests, integration tests, and protected review. Automatic apply, direct `main` writes, tenant-admin consent, and automatic merge remain disabled.

## Installation

Build the Microsoft app package with `scripts/New-HeliosTeamsPackage.ps1`, substitute the hosted base URL, Entra application ID, and Teams application ID, then upload the resulting package through the tenant administrator’s normal Microsoft 365 app-management process. The same hosted `/setup` URL works directly in Edge.
