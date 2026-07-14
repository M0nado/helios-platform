# HELIOS Azure Integration Slice

This draft joins the Hermes/XCore boundary to a read-only Monado Control Center without copying an upstream Hermes runtime. It is a staging slice, not production authorization.

## Current capability

- `infra/`: internal VNet-bound Azure Container Apps, Premium ACR, Key Vault, Log Analytics, Application Insights, and a user-assigned managed identity.
- `services/control-api/`: a containerized status API and responsive Monado dashboard.
- `.github/workflows/helios-azure.yml`: Bicep compilation, container smoke tests, Azure what-if, protected OIDC deployment, and digest-pinned release.

The canonical port intentionally omits `azure.yaml` and a default parameters file. The protected GitHub workflow is the only promotion path; local Azure CLI use is limited to validation and explicit operator diagnostics.

The API currently exposes status reads only. Hermes reports `unbound` until a real adapter health check is wired. XCore uses an explicit standby adapter and cannot be enabled by an environment flag; it reports `safe-standby` until a real telemetry/training boundary is implemented. There is no training, merge, deletion, RBAC, notification, or USB execution endpoint.

## Trust boundaries

- Pull-request validation has `contents: read` only and cannot request an Azure token.
- No parallel `azd up` or default-parameter apply path is included.
- Deployment can run only through an explicit `deploy=true` dispatch of the reviewed `main` branch.
- Every Azure job also requires the canonical repository and a protected `main` ref. The jobs use the protected `azure-dev` environment and an Entra workload-identity federation subject of `repo:M0nado/helios-platform:environment:azure-dev`.
- The first deployment creates only the foundation. It does not create a Container Apps environment or expose a placeholder container.
- The image is built in ACR and released by immutable digest.
- Foundation and release plans contain sanitized property-level changes, source/template/parameter hashes, and immutable runtime bindings. Apply jobs consume those artifacts and reject Azure drift before creation.
- The Container Apps environment is created only during release, inside the canonical `helios-dev-hybrid-vnet/control-plane` subnet. The subnet must be at least `/27` and delegated to `Microsoft.App/environments`; environment and app ingress are hard-coded internal. The template has no public-ingress parameter.
- Any future public route must be added through the governed Front Door/WAF, private APIM, and Entra boundary.
- Key Vault has RBAC, soft delete, purge protection, and public-network access disabled. Runtime secret access is intentionally not granted until private networking and the adapter contract exist.

## Required `azure-dev` variables

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`

The resource group must already exist. Before enabling `azure-dev`, define and review a dedicated deployment role limited to the resource types and actions in this slice and scope it to that resource group. Resource-group Contributor is an upper bound for initial validation, not the final least-privilege design; subscription-wide Contributor or Owner is prohibited. The ACR pull role assignment must be pre-provisioned by a separately approved role-administration identity; the template never creates role assignments, and the workflow fails before release when exact-registry `AcrPull` is absent.

A separate subscription-level platform administrator must pre-register `Microsoft.App`, `Microsoft.ContainerRegistry`, `Microsoft.KeyVault`, `Microsoft.ManagedIdentity`, `Microsoft.Insights`, and `Microsoft.OperationalInsights`. The deployment identity is not allowed to register providers.

## Promotion blockers

- PR #174 must be merged and this branch rebased before review can leave draft.
- Shared Log Analytics and Key Vault ownership must be reconciled into the post-#174 canonical enterprise Bicep graph; this staging slice must not become a competing desired-state root.
- The canonical subnet delegation, ACR/Key Vault private endpoints and DNS, private APIM, Entra authorization, controlled egress, internal API verification, rollback evidence, SBOM, signing, and attestation are not complete here.
- The protected `azure-dev` environment, canonical federation credential, variables, required reviewers with self-review prevented, admin bypass disabled, main-only branch restriction, and custom deployment role must remain unconfigured until those controls are approved.

## Deployment sequence

1. Keep the canonical port in draft until validation and review are complete.
2. Configure the four environment variables, exact canonical federation subject, main-only deployment branch, required reviewers with self-review prevented, and disabled admin bypass on `azure-dev`.
3. Merge the reviewed commit to `main`.
4. Dispatch `HELIOS Azure` from `main` with `deploy=true`.
5. Review the sanitized property-level foundation plan and its source/template/parameter manifest, then separately approve the protected foundation apply job.
6. Have a separately approved identity grant `AcrPull` to the new control identity at the exact registry scope.
7. Confirm the canonical control-plane subnet is delegated, large enough, and governed for an internal Container Apps environment.
8. Review the digest-pinned, sanitized property-level release plan and manifest, then separately approve the protected release apply job.
9. Verify Azure activity logs, revision health, identity scope, internal API health, and rollback evidence before any promotion.

Production deployment remains disabled. The canonical network, policy, and runtime contracts in `M0nado/helios-platform` PR #174 take precedence when this slice is promoted.
