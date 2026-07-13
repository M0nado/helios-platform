# HELIOS / MonadoBlade Copilot Instructions

## Product identity

HELIOS / MonadoBlade is a C#-first secure Windows development and control platform. `M0nado/helios-platform` is the canonical source of truth. PowerShell is used only as a controlled Windows adapter; JSON/YAML manifests hold declarative configuration.

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

The companion fleet repository is `Yolkster64/hermes-fleet-platforms`.

## Working rules

1. Create or use a scoped GitHub issue before substantial implementation.
2. Work on a feature branch and open a pull request.
3. Build and test before requesting review.
4. Keep disk formatting, BitLocker, WDAC, firewall lockdown, secret rotation, and production deployment behind explicit approval gates.
5. Default destructive Windows automation to dry-run or `-WhatIf` behavior.
6. Never commit credentials, tokens, recovery keys, tenant secrets, or private endpoint keys.
7. Use Azure Key Vault, GitHub environment secrets, federated identity, DPAPI, or Windows Credential Manager for secrets.
8. Keep generated logs, build output, caches, model weights, VHDX files, and local databases out of Git.
9. Prefer typed C# services and interfaces over monolithic scripts.
10. Record rollback behavior for every privileged system change.

## Collaboration system

- GitHub: code, architecture, issues, PRs, Actions, releases
- Linear: sprint execution and linked issue state
- Slack: alerts and rapid collaboration
- Microsoft Teams: Microsoft 365 operational collaboration
- SharePoint: governed runbooks, policies, and release evidence
- Azure: infrastructure, Key Vault, Foundry, telemetry, and deployment

## Completion criteria

A change is complete only when:

- implementation is scoped and documented;
- affected projects build;
- tests or validation scripts pass;
- security and rollback effects are documented;
- CI status is green;
- operational documentation is updated when needed.
