---
name: helios-azure-cli
description: Safely prepare, validate, and preview the HELIOS Azure setup with Azure CLI, GitHub OIDC, and Bicep. Use for HELIOS Azure doctor checks, login, identity plans, and what-if previews.
---

# HELIOS Azure CLI

Use `scripts/helios_azure.py` as the single entrypoint. Read `assets/helios-targets.json` before choosing a destination.

## Safety contract

- Default to `doctor`, `plan`, or `what-if`; these do not deploy resources.
- Never pass `--execute` for `configure-oidc` or `deploy` unless the user explicitly authorizes that exact mutation in the current turn.
- Keep `AZURE_DEPLOY_ENABLED=false` until canonical PR #174 is merged, PR #176 is merged after hardening, and the `azure-dev` environment has reviewers and branch restrictions.
- Use the exact federation subject `repo:M0nado/helios-platform:environment:azure-dev` and issuer `https://token.actions.githubusercontent.com`.
- Do not request, store, echo, or transmit client secrets. This plugin uses interactive login locally and secretless workload identity for GitHub Actions.
- Treat the Linear project and Slack channel IDs in the targets file as canonical. Engineering authority is `#helios-control-plane`; `#all-helios` is announcements only.

## Normal workflow

1. Run `python3 scripts/helios_azure.py doctor`.
2. If Azure CLI is not installed, direct the user to Microsoft's official installer. Do not install packages without approval.
3. If login is needed, run `python3 scripts/helios_azure.py login --device-code` only with the user present for interactive authentication.
4. Run `python3 scripts/helios_azure.py plan` and verify the subscription, tenant, resource group, repository, environment, and OIDC subject.
5. For infrastructure, run `python3 scripts/helios_azure.py what-if --template <main.bicep>`. Review the output before considering deployment.
6. Report status against Linear `JOH-35` and coordinate operational changes in Slack `#helios-control-plane`. Use `#all-helios` only for a short availability announcement.

## Mutating commands

`configure-oidc` and `deploy` are deliberately guarded. They verify that PRs #174 and #176 are merged through GitHub CLI, require `--execute`, and require exact confirmation phrases printed by `plan`. Do not bypass these checks.
