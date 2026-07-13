# HELIOS / MonadoBlade Copilot Instructions

## Product identity

HELIOS / MonadoBlade is a C#-first secure Windows development and control platform. `M0nado/helios-platform` is the canonical product and execution source of truth. `Heli0s-Dynamics/adaptive-multibrain-bootstrap` is the shared policy, integration-contract, GitHub/Azure bootstrap, and Hermes/XCore evaluation control plane. PowerShell is used only as a controlled Windows adapter; JSON/YAML manifests hold declarative configuration.

Before cross-repository work, read:

- `AGENTS.md`
- `config/integrations/repositories.json`
- `config/integrations/event-contract.schema.json`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`

## Architecture

Primary modules:

- `HELIOS.Platform`
- `HELIOS.Installer`
- `HELIOS.Storage`
- `HELIOS.Vault`
- `HELIOS.Security`
- `HELIOS.Optimizer`
- `HELIOS.AIHub`
- `HELIOS.Hermes`
- `HELIOS.DockerFleet`
- `HELIOS.Azure`
- `MonadoBlade.GUI`

The companion fleet repository is `Yolkster64/hermes-fleet-platforms`. Satellite repositories must integrate through explicit contracts, packages, or tracked migrations rather than becoming competing platform roots.

## Working rules

1. Create or use a scoped GitHub issue before substantial implementation.
2. Work on a feature branch and open a pull request.
3. Build and test before requesting review.
4. Keep disk formatting, BitLocker, WDAC, firewall lockdown, secret rotation, tenant changes, and production deployment behind explicit approval gates.
5. Default destructive Windows automation to dry-run or `-WhatIf` behavior.
6. Never commit credentials, tokens, recovery keys, tenant secrets, or private endpoint keys.
7. Use Azure Key Vault, protected GitHub environments, workload identity federation, managed identity, DPAPI, or Windows Credential Manager for secrets.
8. Keep generated logs, build output, caches, model weights, VHDX files, and local databases out of Git.
9. Prefer typed C# services and interfaces over monolithic scripts.
10. Record rollback behavior for every privileged system change.
11. Use the shared normalized event envelope for GitHub, Azure, Copilot Studio, Microsoft Copilot, Codex, Hermes/XCore, AIHub, Control Center, and Monado Blade communication.
12. Microsoft Copilot/Copilot Studio requests must enter through approved broker APIs and cannot bypass GitHub or Azure approval gates.

## Collaboration system

- GitHub: code, architecture, issues, PRs, Actions, releases, and engineering evidence
- Azure: workload identity, integration broker, Service Bus/Event Grid, Key Vault, Foundry, telemetry, and deployment
- GitHub Copilot and Codex: repository implementation under shared instructions and pull-request review
- Hermes/XCore: bounded routing, evaluation, pruning, regression checks, and reviewable learning state
- Microsoft Copilot and Copilot Studio: governed business-facing requests through Azure APIs/connectors
- Microsoft Teams: operational notifications and approval-aware cards
- SharePoint: governed runbooks, policies, architecture decisions, and release evidence
- Power Platform and Fabric: approved workflows, apps, and curated telemetry datasets
- Linear and Slack: optional execution tracking and rapid engineering alerts

## Completion criteria

A change is complete only when:

- implementation is scoped and documented;
- the owning repository is correct;
- affected projects build;
- tests and integration-contract validation pass;
- security and rollback effects are documented;
- CI status is green;
- operational documentation is updated when needed;
- cross-system events preserve correlation IDs and evidence links.