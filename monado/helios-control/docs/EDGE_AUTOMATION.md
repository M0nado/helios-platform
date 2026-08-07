# HELIOS edge automation

This branch adds a governed integration layer between Microsoft Edge/Copilot or ChatGPT/Codex, the HELIOS control API, Azure CLI, GitHub, Linear, Slack, and SharePoint.

The design separates reasoning from mutation:

- REST and MCP generate deterministic, reviewable plans.
- Azure CLI performs diagnostics and produces deployment evidence.
- A protected GitHub environment controls production execution.
- Issue repair and upgrade requests produce proposal records only; branch and draft-PR executors remain disabled until implemented and reviewed.
- Edge starts a Cosmos-backed, resumable Diagnose → Plan → Save → Sync run.
- GitHub, Linear, Slack, and SharePoint receive normalized status through signed, idempotent relay bindings.

Relay HMAC covers the canonical tuple `unix timestamp + newline + idempotency key + newline + exact JSON body`. Receivers must verify that tuple before parsing, reject stale timestamps, and deduplicate the signed key; neither replay header can be substituted independently.

## Safety contract

Automatic operations are limited to discovery, diagnostics, plan generation, validation, and proposal records. MCP cannot create a branch or pull request, apply infrastructure, write secrets, write directly to `main`, or merge a repair.

Infrastructure apply is deliberately absent from the local CLI. The only supported deployment path is `.github/workflows/helios-cloud-deploy.yml`, which requires:

1. GitHub OIDC bound to the selected protected environment.
2. An immutable image built from the exact checked-out commit.
3. A full-resource ARM what-if and request artifact containing the source, template, image, environment, identity, and organization-tag bindings.
4. A second protected-environment approval.
5. A fresh what-if whose canonical hash still matches.
6. Explicit `mode=deploy` and `confirmDeployment=DEPLOY`.

Local `Apply` and `VaultSet` modes are retired. Vault mutation remains unavailable until a separate protected secret-owner workflow and verifiable approval artifact are implemented.

## Edge and OpenAI app surface

The Edge page starts the job through authenticated same-origin REST. The MCP endpoint exposes `azure_get_context`, `azure_list_resources`, `azure_list_foundry_resources`, `helios_plan_automation`, `helios_propose_upgrade`, `helios_get_run`, and `helios_list_connectors` as read-only tools for ChatGPT, Codex, and Copilot. They follow the OpenAI Apps tool model and explicitly declare non-destructive behavior. There is deliberately no MCP apply or run-start tool.

Reference documentation:

- https://developers.openai.com/plugins/build/mcp-server
- https://developers.openai.com/plugins/plan/tools
- https://developers.openai.com/plugins/build/auth

## Operator flow

From `monado/helios-control`, authenticate interactively and run diagnostics:

~~~powershell
az login --tenant <tenant-id>
./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Diagnose -TenantId <tenant-id> -SubscriptionId <subscription-id> -ResourceGroup <resource-group>
~~~

Generate a Bicep what-if plan and evidence digest. Plan resolves every azd placeholder with explicit non-secret values and requests `FullResourcePayloads`, so property-level changes are part of the approved hash:

~~~powershell
./scripts/Invoke-HeliosEdgeAutomation.ps1 -Mode Plan -TenantId <tenant-id> -SubscriptionId <subscription-id> -ResourceGroup <resource-group> -EnvironmentName x-tier-dev -ContainerImage <registry>/helios-connect@sha256:<digest> -ContainerRegistryName <registry> -EntraClientId <client-id> -AllowedPrincipalObjectId <object-id> -SourceCommitSha <git-sha>
~~~

The generated `request.json` is a handoff contract, not proof of approval. Reviewers use the protected GitHub workflow to build the immutable image, capture what-if evidence, approve a second environment gate, verify no drift, and deploy. The local helper has no apply or vault-write code path.

## Plan API

Authenticated callers can request a deterministic plan:

~~~json
{
  "intent": "repair-issue",
  "environment": "x-tier-dev",
  "target": "JOH-36",
  "connector": "all"
}
~~~

Supported intents are `provision-resources`, `rotate-secret`, `repair-issue`, `sync-release`, and `cleanup-owned-resources`. Supported connectors are `github`, `linear`, `slack`, `sharepoint`, `copilot`, `codex`, and `all`.

## Validation and rollout

The branch validation workflow parses the guardrail configuration, runs the .NET test suite, checks PowerShell syntax on Windows, and rejects prohibited commands such as secret readback or automatic PR merge. The existing cloud deployment workflow continues to compile Bicep and owns protected what-if/deploy execution.

This work is carried by PR #188 on current `main`. Require the Windows/.NET,
Bicep/cloud, Copilot package, and repository CI checks before merge; deployment
and deployment remain separate protected-environment decisions.
