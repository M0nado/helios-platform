# Azure CLI and Hugging Face Binding — v5

## Goal

Bind the HELIOS Enterprise Deployment Manager to an authorized Azure subscription and the Hugging Face repository `yolkster1/helios-control` without storing long-lived secrets in source control.

## Azure CLI sequence

1. Install or verify Azure CLI and Azure Developer CLI.
2. Authenticate interactively for initial setup or use GitHub workload identity federation in Actions.
3. Select the approved subscription explicitly.
4. Run Bicep/`azd` validation and Azure what-if before any apply operation.
5. Provision in dependency order:
   - `rg-helios-prod`
   - `log-helios`
   - `stheliosartifacts`
   - `kv-helios-core`
   - `cosmos-helios-memory`
   - `cae-helios`
6. Create managed identities and least-privilege role assignments.
7. Configure GitHub OIDC environment variables and production approval gates.
8. Record Azure correlation IDs, resource inventory, and rollback metadata.

## Hugging Face binding

Target: `yolkster1/helios-control`

The connected Hugging Face integration currently cannot create repositories. Creation must be performed once from an authenticated Hugging Face CLI or web session. After creation:

1. Store the Hugging Face token in `kv-helios-core`.
2. Grant only the repository scope required by the deployment job.
3. Register repository metadata in `config/integrations/external-bindings.v5.json`.
4. Add a health/readiness check to the Enterprise Deployment Manager.
5. Publish only reviewed model cards, configuration, and deployment artifacts.
6. Do not publish local memory, secrets, private logs, or user data.

## Communication binding

- Slack: `#helios-control-plane`
- Microsoft Teams: `Helios / Helios Ops`

Deployment, validation, rollback, security, and incident events should be sent to both destinations using the normalized integration event envelope.

## Approval boundary

No script may bypass:

- Azure subscription authorization;
- Entra or Graph admin consent;
- Hugging Face account authorization;
- GitHub environment approval;
- Teams, SharePoint, Slack, or Linear ownership controls.

## Completion criteria

- Azure what-if passes.
- Canonical resources exist from IaC.
- GitHub OIDC authenticates without client secrets.
- `yolkster1/helios-control` exists and is reachable.
- Hugging Face credentials resolve from Key Vault.
- Teams and Slack receive test events.
- Compliance evidence includes the v5 SHA-256 manifest.
