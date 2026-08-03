# HELIOS Reusable Skills

These skills define repeatable agent workflows for Codex, Copilot, Hermes, and human operators.

## `architecture-map`

**Purpose:** map a requested feature to the canonical C# module, interfaces, tests, configuration, and migration impact.

**Inputs:** feature request, affected subsystem, safety level.

**Outputs:** module map, dependency list, implementation issue, test plan, rollback notes.

## `codex-task-packager`

**Purpose:** convert a GitHub or Linear issue into a bounded coding task.

**Required output:** objective, files to inspect, constraints, acceptance criteria, commands to validate, prohibited changes.

## `build-failure-triage`

**Purpose:** analyze GitHub Actions or local build failures.

**Flow:** classify failure → isolate first root cause → propose smallest patch → run targeted validation → summarize residual warnings.

## `security-review`

**Purpose:** review privileged Windows, Azure, identity, networking, VHDX, secrets, and agent changes.

**Checks:** least privilege, secret handling, dry-run behavior, logging, approval gates, rollback, auditability.

## `azure-deployment-planner`

**Purpose:** generate an Azure deployment plan using Bicep/`azd`, federated identity, environments, Key Vault, Foundry, telemetry, and cost controls.

**Rule:** production changes require environment approval.

## `sharepoint-release-publisher`

**Purpose:** publish release notes, test evidence, architecture decisions, and operator runbooks to the governed SharePoint library.

**Rule:** publish summaries and artifacts, not source secrets or raw credentials.

## `slack-incident-summarizer`

**Purpose:** turn failed workflows, security alerts, or deployment incidents into a concise Slack update with owner, impact, evidence, next action, and links.

## `linear-sync`

**Purpose:** mirror approved GitHub issues into Linear, keep status mappings consistent, and preserve GitHub as the engineering source of truth.

**Status mapping:**

- GitHub open + `planned` → Linear Backlog
- GitHub open + `in-progress` → Linear In Progress
- Pull request open → Linear In Review
- Pull request merged → Linear Done
- GitHub closed as not planned → Linear Canceled

## `hermes-fleet-evaluator`

**Purpose:** evaluate task routing, agent output, build/test results, token/cost use, and repeated failure patterns across the Hermes Docker fleet.

**Outputs:** scorecard, routing recommendation, reusable memory entry, and follow-up issue when needed.
