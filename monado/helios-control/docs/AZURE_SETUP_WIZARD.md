# HELIOS Azure Setup Wizard

The wizard is a personal Microsoft 365/Teams tab and ordinary Edge web app served by the HELIOS control API at `/setup`. The default **Run** view is a one-button control surface; the Cloud Shell generator is retained for first-time tenant bootstrap. Copilot, ChatGPT, and Codex can read run and connector status through authenticated MCP tools.

## One-button run

Select **Run HELIOS now** to create an idempotent, Cosmos-backed job. The hosted managed identity verifies the configured Azure boundary, inventories non-secret resource metadata, creates the deterministic plan, and saves its SHA-256 digest plus connector receipts. Cosmos ETag leases let another replica reclaim queued or expired work after restart; a failed run can also be resumed explicitly.

Connector cards show only whether a binding exists. Relay URLs and HMAC values are never returned to the browser or MCP. Outbound delivery remains `dry-run` until an administrator injects each relay URL and 32-byte-or-longer HMAC secret from Key Vault and changes `HELIOS_CONNECTOR_DELIVERY_MODE` to `live` through a reviewed deployment.

The terminal state for any plan containing mutation is `awaiting-approval`. The run endpoint, browser, and MCP expose no apply method. Azure apply still requires the protected workflow, reviewed what-if hash, fresh drift match, reviewer approval, and exact typed confirmation.

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

The Bicep entry point accepts organization tags and enforces reserved `helios-managed`, service, environment, owner, provisioner, and repository tags on managed resources. The Edge `cleanup-owned-resources` intent is only an orchestration plan describing mandatory tag, lock, dependency, and complete-mode what-if checks; it does not execute those checks or remove anything. The protected operator workflow must produce the real removal what-if, protect unknown, untagged, shared, or drifted resources, and receive owner review plus typed confirmation before removal.

## Automatic upgrades

The **Upgrade agent** panel or `helios_propose_upgrade` MCP tool accepts a capability name and reason. It returns a deterministic proposal ID and required checks. Codex or Copilot can use that proposal to create a task branch, implement the scoped capability, run validation, and open a draft pull request.

Promotion is deliberately separate from proposal generation. Every upgrade requires schema validation, security guardrails, unit tests, integration tests, and protected review. Automatic apply, direct `main` writes, tenant-admin consent, and automatic merge remain disabled.

## Installation

Build the Microsoft app package with `scripts/New-HeliosTeamsPackage.ps1`, substitute the hosted base URL, Entra application ID, and Teams application ID, then upload the resulting package through the tenant administrator’s normal Microsoft 365 app-management process. The same hosted `/setup` URL works directly in Edge.
