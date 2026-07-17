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
Azure trust is created.

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

## 3. Publish an immutable image in Azure

Publish uses Azure Container Registry Tasks (`az acr build`), so no local Docker
daemon or local runtime is required. A new ACR requires `CREATE CONTAINER
REGISTRY <name>`; source upload/build requires `PUBLISH HELIOS IMAGE`. The build
context is scanned for secret-shaped files and governed by `.dockerignore`.
Only a dedicated registry tagged for the exact Helios environment is accepted;
the template never creates, downgrades, or changes an existing registry.

Publish grants the runtime identity and GitHub OIDC principal only the registry
read role appropriate to the registry authorization mode (`AcrPull` for
RBAC-only, or `Container Registry Repository Reader` for ABAC). It reads and
records the registry's authentication-as-ARM policy but supports both modes and
never weakens a registry that requires the more restrictive ACR-scoped audience.

```powershell
pwsh -NoProfile -File ./scripts/Connect-HeliosAzureInteractive.ps1 `
  -Mode Publish `
  -EnvironmentName dev `
  -ResourceGroup rg-helios-dev `
  -ContainerRegistryName '<globally-unique-acr-name>'
```

Copy the emitted `HELIOS_CONTAINER_IMAGE=<registry>/helios-connect@sha256:...`
value. It is an immutable non-secret identifier.

## 4. Deploy the reviewed revision online

The recommended deployment surface is the `helios-cloud-deploy` GitHub Actions
workflow. Its preview job produces a hashed plan artifact. A separate deploy job
inside the same deploy-mode workflow waits for a second protected-environment
approval, verifies the artifact and its SHA-256, rechecks drift, and uses the
same immutable image digest. This keeps application execution and deployment
online and makes review evidence durable.

The operator wizard has no direct deployment mode. Dispatch the root
`helios-cloud-deploy` workflow with the emitted immutable image reference,
select `mode=deploy`, and enter `confirmDeployment=DEPLOY`. The workflow first
waits at the preview environment, validates the exact registry and digest,
produces and hashes canonical ARM what-if evidence, then waits at the separate
deploy environment before rechecking drift and applying that same revision.

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
