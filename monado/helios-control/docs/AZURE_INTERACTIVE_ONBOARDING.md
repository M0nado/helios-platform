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

The interactive administrator grants CI only Contributor at the selected
resource-group scope. It separately grants the runtime identity Reader and
registers only the six resource providers listed by the wizard; provider
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
uses v2 access tokens, exposes exactly `user_impersonation`, and preauthorizes
the Microsoft Azure CLI public client for that scope so the verifier can obtain
a token without storing a secret.

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
  -ContainerRegistryName '<globally-unique-acr-name>'
```

The operator script emits no user-supplied image reference. The protected run
records the immutable image reference and source SHA in its what-if evidence.

## 4. Deploy the reviewed revision online

The recommended deployment surface is the `helios-cloud-deploy` GitHub Actions
workflow. Its preview job produces a hashed plan artifact. A separate deploy job
inside the same deploy-mode workflow waits for a second protected-environment
approval, verifies the artifact and its SHA-256, rechecks drift, and uses the
same immutable image digest. This keeps application execution and deployment
online and makes review evidence durable.

The evidence contract contains the exact source SHA, canonical compiled-template
SHA-256, deployment scope, and every resolved Bicep parameter. ARM what-if uses
`FullResourcePayloads`, so property-level changes are reviewable and participate
in the drift hash. Only a redacted copy is uploaded; the full canonical payload
is hashed on the ephemeral runner and is never published as an artifact.

The operator wizard has no direct deployment mode. After reviewing the initial
what-if run, dispatch the root `helios-cloud-deploy` workflow on the approved
branch with `mode=deploy` and `confirmDeployment=DEPLOY`. The workflow first
waits at the preview environment, builds the exact selected commit and records
its registry digest, produces and hashes canonical ARM what-if evidence, then
waits at the separate deploy approval before rechecking drift and applying that
same revision.

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
verify connector context plus MCP initialization and its exact three-tool
read-only allowlist. The verifier also sends the initialized notification and
executes a real read-only ARM inventory call, so broken managed identity/RBAC
fails verification rather than appearing ready.

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
