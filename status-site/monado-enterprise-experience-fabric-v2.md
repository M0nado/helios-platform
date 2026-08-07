# Monado enterprise experience fabric v2 status source

This page is the Pages/wiki-friendly status source for issue #206.

## Authoritative sources

- GitHub issue: <https://github.com/M0nado/helios-platform/issues/206>
- Architecture document: `docs/architecture/MONADO_ENTERPRISE_EXPERIENCE_FABRIC_V2.md`
- Contract index: `config/monado-enterprise/v2/index.json`
- Profile manifests: `config/monado-enterprise/v2/profile-manifests/*.xml`
- Schemas: `schemas/monado-enterprise/v2/*`

## Execution model

- Proposal-only by default
- Destructive apply disabled by default
- Privileged actions require explicit approval + immutable evidence + rollback notes

## Required profile catalog

- Core
- Developer
- Gamer
- Studio
- Personal
- SysOps
- AI/Server
- SysAdmin

Recovery/Quarantine are states and Airgap is an overlay.

## Synchronization boundaries

- GitHub: engineering source of truth
- Linear: work authority
- SharePoint: governed evidence authority
- Slack: concise checkpoint surface
- Azure DevOps: read-only mirror until approved write identity exists
- Adobe design: evidence-reference-only mode
