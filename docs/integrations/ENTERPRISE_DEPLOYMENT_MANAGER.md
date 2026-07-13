# HELIOS Enterprise Deployment Manager

## Purpose

The HELIOS Enterprise Deployment Manager is the first-class provisioning, registration, validation, rollback, and compliance subsystem for the HELIOS / MonadoBlade platform.

It replaces disconnected connector tasks with one governed deployment plane.

## Ownership

The deployment manager owns:

- Azure provisioning
- Microsoft Entra integration
- Azure Key Vault
- Azure AI Foundry
- GitHub Actions OIDC federation
- Microsoft Teams integration
- SharePoint publishing
- OneDrive integration
- Microsoft Graph integration
- Linear synchronization
- Slack integration
- Hermes registration
- AIHub registration
- Deployment validation
- Rollback
- Compliance reporting

## Canonical resources

Production resource names:

- Resource group: `rg-helios-prod`
- Key Vault: `kv-helios-core`
- Cosmos DB account/database role: `cosmos-helios-memory`
- Storage account logical name: `stheliosartifacts`
- Container Apps environment: `cae-helios`
- Log Analytics workspace: `log-helios`

Development and test environments must use equivalent environment suffixes and must not share production identities or secrets.

## Architecture

```text
GitHub Actions
  -> OIDC / workload identity federation
  -> HELIOS Enterprise Deployment Manager
  -> Azure resources and integration broker
  -> Microsoft Graph / Linear / Slack
  -> Hermes and AIHub registration
  -> validation, evidence, rollback, compliance
```

## Subsystems

### Provisioning

- Bicep and `azd` templates
- environment-specific parameters
- what-if validation before apply
- deterministic naming and tags
- private networking where required

### Identity

- Entra application registrations or managed identities
- GitHub workload identity federation
- least-privilege RBAC
- no long-lived Azure client secret in GitHub

### Secrets

- connector credentials stored in Key Vault
- workload identities retrieve secrets at runtime
- secret rotation events logged and auditable
- no raw secrets in source, Actions logs, Slack, Teams, or SharePoint

### Integration broker

The broker runs in Azure Functions or Azure Container Apps and handles normalized events for:

- GitHub
- Linear
- Slack
- Teams
- SharePoint
- OneDrive
- Microsoft Graph
- Hermes
- AIHub

### Validation

Every deployment must validate:

- resource existence and configuration
- OIDC token exchange
- managed identity and RBAC
- Key Vault access
- broker health
- Graph permission readiness
- Slack and Teams alert delivery
- Linear issue synchronization
- SharePoint release publishing
- Hermes and AIHub registration
- Application Insights correlation

### Rollback

Rollback is a first-class workflow, not an emergency note.

- retain previous deployment parameters
- support Bicep what-if comparison
- version broker container/function releases
- preserve release evidence and correlation IDs
- revoke newly granted permissions when rollback requires it
- never destroy persistent data stores without an explicit separate approval

### Compliance reporting

Generate a signed evidence bundle containing:

- deployment ID and commit SHA
- GitHub workflow run
- Azure correlation IDs
- resource inventory
- identity and RBAC summary
- Graph permissions granted
- Key Vault access policy/RBAC summary
- validation results
- rollback point
- release notes
- artifact SHA-256 manifest

Publish approved evidence to SharePoint. Store machine-readable copies in `stheliosartifacts`.

## Permission boundaries

The system may automate provisioning after authorization, but must not bypass:

- Azure subscription authorization
- Entra admin consent
- Microsoft Graph admin consent
- Teams and SharePoint ownership
- Linear workspace authorization
- Slack workspace authorization
- GitHub environment approval for production

Broad Azure Owner or tenant-wide Graph permissions are prohibited unless a separately reviewed exception documents necessity and expiry.

## Deployment states

```text
Unconfigured
Authorized
Planned
Validated
Deploying
Registered
Verified
Compliant
RollbackReady
Failed
RolledBack
```

## Acceptance criteria

1. `main` deployments use GitHub OIDC and no stored Azure client secret.
2. `rg-helios-prod`, `kv-helios-core`, `cosmos-helios-memory`, `stheliosartifacts`, `cae-helios`, and `log-helios` are provisioned from IaC.
3. Teams, SharePoint, OneDrive, Graph, Linear, and Slack connectors are registered through Key Vault-backed configuration.
4. Hermes and AIHub registration are health-checked and correlated with the deployment ID.
5. Validation and rollback workflows are executable from GitHub Actions and the HELIOS GUI.
6. Compliance evidence is generated and published only after successful validation.
7. Production deployment requires a GitHub environment approval gate.
