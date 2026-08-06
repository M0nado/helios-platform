---
name: azure-enterprise-control
description: Plan, validate, and approval-gate HELIOS Azure CLI, azd, Bicep, Entra, OIDC, Key Vault, Foundry, Container Apps, Functions, networking, telemetry, and data services.
---

# Azure Enterprise Control

## Trigger

Use this skill for Azure authentication readiness, subscription context, IaC, identity, networking, secrets, Foundry, broker runtime, telemetry, validation, or deployment planning.

## Inputs

- environment: `dev`, `test`, or `prod`;
- source issue/PR and immutable commit SHA;
- requested operation;
- approved subscription identifier or explicit unresolved placeholder;
- Bicep entrypoint and parameter file;
- production approval state;
- correlation ID.

## Procedure

1. Validate `az`, `azd`, Bicep, Functions Core Tools, PowerShell, .NET, and Docker versions.
2. Inspect authentication state without collecting tokens.
3. Require explicit subscription selection; never choose the first subscription automatically.
4. Compile Bicep and enumerate declared outputs.
5. Compare binding names against actual outputs and fail on drift.
6. Run Azure what-if and capture the immutable plan.
7. Review identity, RBAC, private networking, policy, cost, and destructive changes.
8. Verify a rollback point and permission-reversal plan.
9. For production, require a protected GitHub environment approval and Issue #162 resolved.
10. Apply only the reviewed plan; do not recompute an unreviewed production plan during apply.
11. Register resources and run synthetic validation.
12. Return correlation-linked evidence.

## Sub-agent delegation

- CLI health and login: `azure-cli-bootstrap`
- Bicep and what-if: `bicep-infrastructure`
- Entra and RBAC: `entra-identity`
- GitHub federation: `github-oidc`
- secrets: `keyvault-secret-broker`
- Foundry: `foundry-platform`
- broker runtime: `container-broker`
- end-to-end validation: `validation-observability`
- rollback: `rollback-controller`
- evidence: `compliance-evidence`

## Required checks

- no long-lived client secret;
- `id-token: write` only where OIDC login is used;
- no default Azure Owner assignment;
- production identity separate from dev/test;
- Key Vault RBAC and purge protection appropriate to environment;
- private endpoints/DNS modeled when required;
- Storage, Cosmos DB, Container Apps, Log Analytics, and Foundry names resolved from outputs;
- what-if contains no unapproved deletes or replacements;
- deployment tags include commit, environment, owner, and correlation ID;
- Application Insights can correlate the deployment and broker events.

## Output

Return:

- tool and authentication readiness;
- selected tenant/subscription metadata with sensitive values redacted;
- compiled template hash;
- what-if artifact link;
- resource/output map;
- identity/RBAC summary;
- warnings and blocked operations;
- approval state;
- rollback reference;
- evidence links.

## Prohibited

- fabricating login success;
- selecting a subscription without owner confirmation;
- storing an Azure client secret in GitHub;
- using subscription-wide Owner by default;
- bypassing admin consent;
- production apply while Issue #162 is open;
- deleting persistent data as part of rollback.
