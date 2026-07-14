---
name: deployment-assurance
description: Validate HELIOS deployments, correlate telemetry, prepare rollback, and publish versioned compliance evidence.
---

# Deployment Assurance

## Trigger

Use after a plan is generated, before any apply, after deployment, during incident response, or when publishing release evidence.

## Pre-apply validation

1. Confirm the source commit and artifact hashes.
2. Confirm Issue #162 is closed for production.
3. Validate the agent and connector registry schemas.
4. Compile Bicep and compare declared outputs with binding targets.
5. Review what-if for creates, updates, replacements, and deletes.
6. Validate OIDC subject, audience, protected environment, and least-privilege RBAC.
7. Validate Key Vault, Graph, Teams, SharePoint, Linear, Slack, Hermes, AIHub, and OpenAI connection readiness.
8. Confirm cost, networking, policy, and data-retention implications.
9. Confirm an executable rollback reference exists.
10. Produce an approval packet.

## Post-apply validation

- resource inventory matches approved plan;
- managed identities and RBAC resolve correctly;
- Key Vault access succeeds without secret exposure;
- broker health endpoint succeeds;
- Application Insights receives a correlated synthetic event;
- Teams and Slack receive the test event;
- Linear receives or updates the linked work item;
- SharePoint receives a versioned evidence artifact;
- Hermes and AIHub registration health checks pass;
- no agent has undeclared write capability.

## Rollback preparation

The rollback controller must record:

- previous Bicep parameters and deployment name;
- previous Container Apps/Functions revision;
- previous Foundry deployment/agent configuration;
- identity/RBAC additions to reverse;
- connector registrations to disable;
- SharePoint/Linear/Slack/Teams evidence links;
- persistent data that must be retained;
- a stop condition and verification plan.

Rollback never deletes Cosmos DB, Storage, Key Vault, or other persistent stores without a separate destructive-data approval.

## Evidence bundle

Create both machine-readable JSON and human-readable Markdown containing:

- correlation and deployment IDs;
- commit SHA and pull request;
- workflow run and protected approval;
- template and parameter hashes;
- Azure what-if and deployment correlation IDs;
- resource inventory and Bicep outputs;
- identity, RBAC, OIDC, and Graph permission summaries;
- secret-reference inventory without values;
- validation scorecard;
- connector test links;
- rollback point;
- exceptions and expirations;
- final SHA-256 manifest.

Publish approved evidence to SharePoint and store the machine-readable copy in artifact storage.

## Failure handling

- halt downstream agents on integrity, identity, or permission failure;
- do not retry privileged writes indefinitely;
- open/update GitHub and Linear work with the first root cause;
- notify Teams and Slack with impact, evidence, owner, and next action;
- preserve logs and correlation IDs;
- request rollback approval when the current state is unsafe.

## Prohibited

- marking a deployment compliant before validation;
- rewriting evidence without creating a new version;
- including raw secrets, tokens, private prompts, or user data;
- declaring rollback-ready without testing or validating the rollback path;
- suppressing failed checks to enable production.
