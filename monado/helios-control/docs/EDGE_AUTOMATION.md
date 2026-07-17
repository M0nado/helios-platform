# HELIOS edge automation

This branch adds a governed integration layer between Microsoft Edge/Copilot or ChatGPT/Codex, the HELIOS control API, Azure CLI, GitHub, Linear, Slack, and SharePoint.

The design separates reasoning from mutation:

- REST and MCP generate deterministic, reviewable plans.
- Azure CLI performs diagnostics and produces deployment evidence.
- A protected GitHub environment controls production execution.
- GitHub branches and draft pull requests contain issue repairs.
- Edge starts a Cosmos-backed, resumable Diagnose → Plan → Save → Sync run.
- GitHub, Linear, Slack, and SharePoint receive normalized status through signed, idempotent relay bindings.

## Safety contract

Automatic operations are limited to discovery, diagnostics, plan generation, validation, and draft-PR preparation. MCP cannot apply infrastructure, write secrets, write directly to `main`, or merge a repair.

Infrastructure apply requires all of the following:

1. An authenticated Azure CLI context matching the requested tenant and subscription.
2. A previously saved plan and its SHA-256 digest.
3. A fresh what-if result that exactly matches that digest.
4. A protected GitHub environment for production.
5. The typed confirmation `APPLY HELIOS EDGE <ENVIRONMENT>`.

Vault updates use a secure prompt and a restricted temporary file. The secret is passed to `az keyvault secret set --file`, never supplied as a command-line value, never returned by the API, and never read back by the script.

## Edge and OpenAI app surface

The Edge page starts the job through authenticated same-origin REST. The MCP endpoint exposes `helios_plan_automation`, `helios_get_run`, and `helios_list_connectors` as read-only tools for ChatGPT, Codex, and Copilot. They follow the Apps SDK tool model and explicitly declare non-destructive behavior. There is deliberately no MCP apply or run-start tool.

Reference documentation:

- https://developers.openai.com/apps-sdk/build/mcp-server
- https://developers.openai.com/apps-sdk/plan/tools
- https://developers.openai.com/apps-sdk/build/auth

## Operator flow

From `monado/helios-control`, authenticate interactively and run diagnostics:

~~~powershell
az login --tenant <tenant-id>
./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Diagnose -TenantId <tenant-id> -SubscriptionId <subscription-id> -ResourceGroup <resource-group>
~~~

Generate a Bicep what-if plan and evidence digest:

~~~powershell
./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Plan -TenantId <tenant-id> -SubscriptionId <subscription-id> -ResourceGroup <resource-group> -EnvironmentName dev
~~~

After review, apply the exact approved plan. The script recomputes what-if and fails closed if the result changed:

~~~powershell
./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Apply -TenantId <tenant-id> -SubscriptionId <subscription-id> -ResourceGroup <resource-group> -EnvironmentName dev -ApprovedPlanFile ./evidence/helios-edge/dev/what-if.json -ApprovedPlanSha256 <sha256> -Confirmation "APPLY HELIOS EDGE DEV"
~~~

Set a Key Vault secret without exposing its value in shell history:

~~~powershell
./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode VaultSet -TenantId <tenant-id> -SubscriptionId <subscription-id> -ResourceGroup <resource-group> -KeyVaultName <vault-name> -SecretName <secret-name> -Confirmation "SET HELIOS VAULT SECRET <secret-name>"
~~~

## Plan API

Authenticated callers can request a deterministic plan:

~~~json
{
  "intent": "repair-issue",
  "environment": "dev",
  "target": "JOH-36",
  "connector": "all"
}
~~~

Supported intents are `provision-resources`, `rotate-secret`, `repair-issue`, and `sync-release`. Supported connectors are `github`, `linear`, `slack`, `sharepoint`, `copilot`, `codex`, and `all`.

## Validation and rollout

The branch validation workflow parses the guardrail configuration, runs the .NET test suite, checks PowerShell syntax on Windows, and rejects prohibited commands such as secret readback or automatic PR merge. The existing cloud deployment workflow continues to compile Bicep and owns protected what-if/deploy execution.

This work is carried by PR #188 on current `main`. Require the Windows/.NET,
Bicep/cloud, Copilot package, and repository CI checks before merge; deployment
and typed apply remain separate protected-environment decisions.
