# HELIOS Azure Setup Wizard

The wizard is a personal Microsoft 365/Teams tab and ordinary Edge web app served by the HELIOS control API at `/setup`. The same plan and upgrade capabilities are available to Copilot, ChatGPT, and Codex through authenticated MCP tools.

## Setup flow

1. Enter the Azure tenant ID, resource group, and environment. Subscription ID is optional.
2. Select **Generate Cloud Shell setup**.
3. Review and copy the generated PowerShell into Azure Cloud Shell.
4. Complete Microsoft device authentication in the browser.
5. If subscription ID is omitted, the script selects the single enabled subscription containing the resource group, uses the only enabled subscription when unambiguous, or asks you to choose from a tenant-scoped table.
6. The script verifies the selected Azure context, runs HELIOS `Diagnose`, then runs `Plan`.
7. Cloud Shell keeps the canonical plan, request envelope, and SHA-256 beneath `$HOME/clouddrive/helios-evidence` so separate shell sessions can use the same reviewed evidence.
8. The response also identifies four isolated work packets: inventory, identity readiness, deployment preview, and health verification. Each packet has a narrow read-only role; it does not distribute Azure credentials.
9. Review the resulting ARM what-if file and SHA-256 digest.
10. Stop. Apply remains a separate protected workflow with fresh drift verification and typed confirmation.

The generated script contains identifiers but no credentials, tokens, secret values, role grants, resource deletion, or apply command. Input validation rejects shell metacharacters and control characters.

## Resource governance and cleanup

The Bicep entry point accepts organization tags and enforces reserved `helios-managed`, service, environment, owner, provisioner, and repository tags on managed resources. The `cleanup-owned-resources` intent is plan-only: it inventories only `helios-managed=true` resources, detects locks and shared dependencies, and produces a complete-mode removal what-if. Unknown, untagged, shared, or drifted resources are protected. Removal can run only later through the protected workflow after owners review the exact digest and provide typed confirmation.

## Automatic upgrades

The **Upgrade agent** panel or `helios_propose_upgrade` MCP tool accepts a capability name and reason. It returns a deterministic proposal ID and required checks. Codex or Copilot can use that proposal to create a task branch, implement the scoped capability, run validation, and open a draft pull request.

Promotion is deliberately separate from proposal generation. Every upgrade requires schema validation, security guardrails, unit tests, integration tests, and protected review. Automatic apply, direct `main` writes, tenant-admin consent, and automatic merge remain disabled.

## Installation

Build the Microsoft app package with `scripts/New-HeliosTeamsPackage.ps1`, substitute the hosted base URL, Entra application ID, and Teams application ID, then upload the resulting package through the tenant administrator’s normal Microsoft 365 app-management process. The same hosted `/setup` URL works directly in Edge.
