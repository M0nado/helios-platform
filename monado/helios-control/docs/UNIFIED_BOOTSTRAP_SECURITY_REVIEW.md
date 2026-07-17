# Unified Bootstrap Security Review

The original all-in-one draft was not adopted unchanged.

## Replaced behaviors

- Removed the editable `OPENAI_API_KEY` configuration value.
- Removed Azure OpenAI account-key retrieval and key echo/transport.
- Replaced subscription-wide Contributor with resource-group-scoped Contributor
  for the GitHub deployment identity. The hardened cloud workflow and
  `connector.bicep` do not grant roles. An authenticated operator separately
  grants Reader to the connector identity and the registry's least-privileged
  pull role to the connector and GitHub identities.
- Replaced branch-only OIDC trust with an exact protected-environment subject.
  The wizard resolves immutable owner/repository IDs when enabled, rejects
  unsupported custom subject templates, and verifies the environment's required
  reviewer and exact deployment branch before creating trust. The deployment
  uses ordinary jobs, so it does not depend on the reusable-workflow-only
  `job_workflow_ref` claim.
- Separated the connector API app from the GitHub deployment app.
- Replaced the Python function labeled as MCP with the actual .NET JSON-RPC MCP
  endpoint already covered by tests.
- Replaced push-to-main live deployment with pull-request validation plus a
  manually dispatched, protected workflow. The workflow captures and hashes a
  property-complete canonical ARM what-if. Its evidence also binds the compiled
  template hash and every deployment parameter; a separate protected-environment
  approval revalidates the artifact, image digest, template, parameters, and
  what-if before deployment. Only the redacted what-if copy is uploaded.
- Retired every direct local application apply path. The interactive wizard
  separates Plan, Configure, and Publish and requires exact confirmations
  before each Azure or GitHub bootstrap mutation. Only the protected GitHub
  workflow can build the deployable image or deploy the application.
- Added Entra allowlisting, Container Apps authentication, managed identity,
  operator-owned resource-group Reader and ACR pull assignments, immutable ACR
  image verification, OAuth resource metadata, and a read-only MCP surface.

## Explicitly unresolved

- Azure/Foundry model selection and deployment require region, quota, and cost
  decisions.
- External OpenAI secret transfer is not automated. Store it through an approved
  Key Vault operator path only when the provider is enabled.
- Graph consent, Copilot Studio publication, Agent 365 registration, and
  production activation remain separate administrator approvals.
- The GitHub environment must have required reviewers before it is considered a
  production-quality approval boundary.
