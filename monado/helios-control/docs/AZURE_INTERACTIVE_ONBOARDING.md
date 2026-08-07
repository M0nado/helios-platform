# Helios interactive Azure onboarding

This is the operator handoff for the cloud-only Helios connector. Run it from
Azure Cloud Shell PowerShell (recommended) or PowerShell 7 with Azure CLI. The
operator client may close after the protected deployment; Azure Container Apps, managed
identity, ACR, Key Vault, and Application Insights remain online.

The wizard never accepts an OpenAI, Azure OpenAI, Anthropic, GitHub, or
Microsoft 365 secret. Azure CLI and GitHub CLI own their interactive sign-in
sessions. CI uses GitHub OIDC, and the deployed connector uses managed identity.

## 1. Inspect the target

Plan is the non-mutating default. It signs in if necessary, presents enabled
tenants/subscriptions and resource groups, builds Bicep, and runs ARM validation
and what-if. Supply an existing ACR name or select one interactively.

```powershell
pwsh -NoProfile -File ./scripts/Connect-HeliosAzureInteractive.ps1 `
  -Mode Plan `
  -EnvironmentName dev `
  -UseDeviceCode
```

## 2. Configure identity and protected CI

Configure can create the selected resource group, two secretless Entra
applications, the exact GitHub environment-bound federation, the runtime
managed identity, and non-secret GitHub environment variables. It resolves the
repository's current GitHub OIDC policy, including immutable owner/repository
IDs, and refuses custom templates rather than guessing. The reviewer-protected
environment and its exact deployment branch are read back and verified before
Azure trust is created. The Entra federated credential matches the exact
environment subject. The deployment workflow uses ordinary jobs rather than a
reusable `workflow_call`, so it intentionally does not require GitHub's
reusable-workflow-only `job_workflow_ref` claim. The environment's required
reviewer and exact deployment-branch policy remain mandatory gates.

The interactive administrator applies `helios-environment=<environment>` to
the selected resource group without replacing other tags and refuses to
reclassify a group already bound to another environment. It grants CI only
Contributor at the selected resource-group scope. It separately grants the runtime identity Reader and
registers only the seven resource providers listed by the wizard; provider
registration has its own `REGISTER HELIOS PROVIDERS` confirmation. CI never
receives Owner or role-assignment authority. Configuration requires the exact
phrase `CONFIGURE HELIOS AZURE`; resource-group creation has a separate exact
confirmation, and a positive numeric GitHub reviewer ID is mandatory.

```powershell
pwsh -NoProfile -File ./scripts/Connect-HeliosAzureInteractive.ps1 `
  -Mode Configure `
  -EnvironmentName dev `
  -ResourceGroup rg-helios-dev `
  -ContainerRegistryName '<globally-unique-acr-name>' `
  -RequiredReviewerId '<github-user-id>' `
  -GitHubDeploymentBranch main
```

This phase runs what-if with an all-zero preview placeholder when no image
exists. It does not deploy an application. The connector API is single-tenant,
uses v2 access tokens, exposes exactly `access_as_user`, and preauthorizes
the Microsoft Azure CLI public client for that scope so the verifier can obtain
a token without storing a secret. The Entra Application ID URI stays
`api://<client-id>`; do not rebind it to hostnames.

## 3. Prepare and dispatch the protected cloud build

Publish does not build from the operator workstation. It revalidates the exact
GitHub reviewer/branch policy, environment-bound OIDC credential, resource-group
Contributor scope, runtime Reader role, and registry roles, then dispatches the
protected workflow. That workflow verifies a clean checkout at `GITHUB_SHA`,
uses Azure Container Registry Tasks (`az acr build`) on that exact source, and
resolves the resulting immutable digest. A new ACR still requires the separate
`CREATE CONTAINER REGISTRY <name>` confirmation.

Publish grants the runtime identity the authorization-mode-appropriate read role
(`AcrPull` or `Container Registry Repository Reader`) and the workflow identity
the corresponding write role (`AcrPush` or `Container Registry Repository
Writer`). It reads the registry's authentication-as-ARM policy, supports both
modes, and never weakens the policy.

```powershell
pwsh -NoProfile -File ./scripts/Connect-HeliosAzureInteractive.ps1 `
  -Mode Publish `
  -EnvironmentName dev `
  -ResourceGroup rg-helios-dev `
  -ContainerRegistryName '<globally-unique-acr-name>' `
  -RequiredReviewerId '<github-user-id>'
```

The operator script emits no user-supplied image reference. The protected run
records the immutable image reference and source SHA in its what-if evidence.

## 4. Deploy the reviewed revision online

The recommended deployment surface is the `helios-cloud-deploy` GitHub Actions
workflow. A `mode=what-if` run produces one immutable evidence artifact for the
approved commit/environment. A later `mode=deploy` run requires
`reviewedRunId=<what-if-run-id>` plus `confirmDeployment=DEPLOY`, waits for a
second protected-environment approval, downloads that exact artifact, verifies
its hashes, rechecks drift, and applies the same immutable image digest.

The evidence contract contains the exact source SHA, canonical compiled-template
SHA-256, deployment scope, and every resolved Bicep parameter. ARM what-if uses
`FullResourcePayloads`, so property-level changes are reviewable and participate
in the drift hash. Both the review-safe redacted payload and the full canonical
payload are artifacted and hash-bound in the request manifest.

The operator wizard has no direct deployment mode. Run the protected workflow
in two stages on the approved branch:

1. `mode=what-if` to produce the reviewed evidence artifact.
2. `mode=deploy`, `reviewedRunId=<what-if-run-id>`, and
   `confirmDeployment=DEPLOY` to consume that exact reviewed artifact.

Re-running `-Mode Configure` remains idempotent for policy revalidation and
GitHub/Azure binding checks, but it does not change the Entra Application ID
URI convention.

Use GitHub Actions → `helios-cloud-deploy` → **Run workflow**. Direct local
`az deployment group create`, `azd provision`, and `azd deploy` are not Helios
promotion paths and are intentionally absent from the operator scripts and
`azure.yaml` service definition.

The connector is deployed in cloud-only, `dry-run`, read-only inventory mode.
This does not enable tenant-wide writes, Graph consent, Foundry model access,
Agent 365 publication, or Copilot Studio publication.

## 5. Verify the live cloud connector

First verify the anonymous health and fail-closed boundary. Add
`-InteractiveAuth` to acquire an Entra token through Azure CLI in memory and
verify connector context plus MCP initialization and its exact eleven-tool
discovery/read-only/plan-only inventory. The verifier confirms anonymous
`tools/list` discovery stays public, anonymous `tools/call` returns the OAuth
challenge tool result, and authenticated read-only calls complete through
managed identity/RBAC.

```powershell
pwsh -NoProfile -File ./scripts/Test-HeliosCloudConnection.ps1 `
  -ConnectorUrl 'https://<container-app-host>' `
  -EntraClientId '<connector-app-client-id>' `
  -TenantId '<tenant-id>' `
  -InteractiveAuth
```

Administrator authentication and permission to create Entra objects, scoped
role assignments, the protected GitHub environment, and Azure resources are
still required. The wizard turns those approvals into an auditable sequence; it
does not bypass them.
