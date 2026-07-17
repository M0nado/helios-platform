# HELIOS Issues and Pull Requests 140–149 Traceability V1

Status: draft for review  
Audit date: 2026-07-14  
Repository: [M0nado/helios-platform](https://github.com/M0nado/helios-platform)  
Comparison point: main at bd32f53def6939ceebc48885d5a18e2913fea28d

## 1. Executive conclusion

Items 140 through 147 are pull requests. Items 148 and 149 are issues. They should not be treated as one merge queue.

The safe baseline is:

- [PR #144](https://github.com/M0nado/helios-platform/pull/144), already merged, for the mixed C#/F#/C++/Python AIHub and reporting foundation.
- [PR #147](https://github.com/M0nado/helios-platform/pull/147), already merged, for the collaboration-control-plane documentation and initial event workflow.
- [Issue #148](https://github.com/M0nado/helios-platform/issues/148) as the deployment, registration, validation, rollback, and compliance program.
- [Issue #149](https://github.com/M0nado/helios-platform/issues/149) as the repository and branch authority map.

The remaining open PR branches must not be merged wholesale. PRs #140 and #141 contain small, recoverable feature deltas. PRs #142, #143, and #145 are stale or superseded variants. PR #146 is not an executable pipeline and must be rewritten.

Most importantly, Issue #148 does not define a learning algorithm. The learning implementation is in merged PR #144. Issue #148 owns deployment and registration of AIHub/Hermes, their memory and health, and the evidence/rollback envelope around them.

## 2. Scope and evidence model

This traceability draft records:

- current GitHub state and branch divergence;
- effective delta against the audited main commit;
- stated requirements and implied acceptance criteria;
- test or review evidence;
- language/runtime ownership;
- dependencies and conflicts;
- recommended disposition;
- a safe consolidated implementation sequence.

No merge, branch deletion, repository archive, deployment, or external write was performed as part of this audit.

## 3. Top-level traceability matrix

| Item | Type and state | Effective delta against audited main | Primary requirement | Runtime role | Disposition |
| --- | --- | --- | --- | --- | --- |
| [#140](https://github.com/M0nado/helios-platform/pull/140) | PR, open, non-draft, GitHub reported mergeable | 1 ahead / 21 behind; 2 files; +203 / −0 | Repository analytics with correct Hermes empty/default behavior | C# .NET 8 consuming Python JSON | Rebase and salvage as a new narrow PR |
| [#141](https://github.com/M0nado/helios-platform/pull/141) | PR, open, non-draft, GitHub reported mergeable | 3 ahead / 21 behind; 2 files; +41 / −7 | Remote-priority command and opt-in filtered branch intelligence | Bash entrypoint plus Python | Reimplement after repository inventory is authoritative |
| [#142](https://github.com/M0nado/helios-platform/pull/142) | PR, open, draft, reported non-mergeable | 5 ahead / 28 behind; 27 files; +120 / −112 | Replace deprecated core GitHub Action pins | GitHub Actions YAML and docs | Audit current pins, then close as superseded |
| [#143](https://github.com/M0nado/helios-platform/pull/143) | PR, open, reported non-mergeable | 4 ahead / 21 behind; 90 files; +8,976 / −99 | Earlier AIHub multi-engine/control-plane variant | C#, F#, C++20, Python, YAML, Bicep | Superseded by #144; do not merge |
| [#144](https://github.com/M0nado/helios-platform/pull/144) | PR, merged | 207 changed files in merged PR; merge commit 266c4d51d92c317e23c1a0951496c5fe11e98ca9 | AIHub control plane, agents, build graph, reports, dashboard, engine contracts | Mixed runtime baseline | Keep as baseline; harden through focused PRs |
| [#145](https://github.com/M0nado/helios-platform/pull/145) | PR, open, reported non-mergeable | 2 ahead / 21 behind; 92 files; +9,602 / −99 | Alternative expanded form of #144 | Same mixed runtime stack | Close after auditing the few potentially unique helpers |
| [#146](https://github.com/M0nado/helios-platform/pull/146) | PR, open, GitHub reported mergeable, but invalid content | 1 ahead / 5 behind; 1 file; +172 / −0 | Claimed unified Azure/GitHub/Codex/Hermes/Slack/M365 deployment scaffold | One malformed workflow only | Do not merge; rewrite under #148 workstreams |
| [#147](https://github.com/M0nado/helios-platform/pull/147) | PR, merged | 4 files; merge commit ae0e4689a4a0008378f2235cdb0756c71548420d | Collaboration/control-plane contracts and an initial integration event | Docs plus GitHub Actions | Keep docs; repair workflow and use broker for delivery |
| [#148](https://github.com/M0nado/helios-platform/issues/148) | Issue, open, in progress | Program issue | Enterprise Deployment Manager | Azure/IaC, identity, broker, connectors, registration, GUI, compliance | Execute through #153–#159 |
| [#149](https://github.com/M0nado/helios-platform/issues/149) | Issue, open; inventory v2 published | Governance issue | Inventory/classify every HELIOS/Hermes repository and prevent competing publishers | GitHub governance | Complete before migration/archive decisions |

## 4. Detailed traceability

### 4.1 PR #140 — HELIOS.RepositoryAnalytics

Source: [PR #140](https://github.com/M0nado/helios-platform/pull/140)  
Title: Add HELIOS.RepositoryAnalytics project and Hermes empty/default handling  
Head: codex/update-helios.repositoryanalytics-handling at fcf008f678f111a17d6c4e68990c3b803aae1fe0  
State at audit: open, non-draft, reported mergeable  
Current comparison: diverged, 1 commit ahead and 21 behind; two effective files, 203 additions

#### Required behavior

- Add a .NET 8 console project at src/tools/HELIOS.RepositoryAnalytics.
- Read reports/branch-intelligence/analytics-metrics.json.
- Read Hermes ideas from reports/branch-intelligence/idea-impact.json.
- Restrict Hermes aggregation to category hermes and module hermes-fleet.
- Extract finite numeric values from metrics, metricValues, and numericMetrics, including nested properties.
- Produce repository-analytics.json and repository-analytics.md.
- When no Hermes numeric metrics are present:
  - hermesMetricSummaries must remain an empty object;
  - hermesFleetSummary must identify empty/default;
  - isDefault must be true;
  - the report must explicitly state that no Hermes fleet numeric metrics were present.
- Do not run anomaly detection for fewer than two samples or zero-variance samples.

Relevant source:

- [Program.cs on the PR head](https://github.com/M0nado/helios-platform/blob/fcf008f678f111a17d6c4e68990c3b803aae1fe0/src/tools/HELIOS.RepositoryAnalytics/Program.cs)
- [HELIOS.RepositoryAnalytics.csproj on the PR head](https://github.com/M0nado/helios-platform/blob/fcf008f678f111a17d6c4e68990c3b803aae1fe0/src/tools/HELIOS.RepositoryAnalytics/HELIOS.RepositoryAnalytics.csproj)

#### Evidence and acceptance gap

The Python branch-intelligence generator ran. The C# tool did not run because the authoring environment lacked dotnet. More seriously, the review found that the Python producer drops the numeric fields before writing idea-impact.json, so valid Hermes metrics are incorrectly reported as empty/default:

- [P1: preserve Hermes metric payloads](https://github.com/M0nado/helios-platform/pull/140#discussion_r3517321643)

Other review findings included unsafe path construction and exact floating-point zero comparison.

#### Required acceptance before merge

- Build and test on dotnet 8.
- Add JSON fixtures for:
  - missing file;
  - empty array;
  - non-Hermes ideas;
  - one numeric sample;
  - constant samples;
  - nested metrics;
  - non-finite values;
  - real Hermes JSONL propagated through branch intelligence.
- Assert deterministic JSON and Markdown output.
- Add schema/version validation.
- Ensure generated reports cannot recursively become future inventory input.

#### Disposition

Create a fresh branch from current main and port only the two feature files plus the producer fix and tests. Do not merge the stale branch directly.

### 4.2 PR #141 — remote-priority branch intelligence

Source: [PR #141](https://github.com/M0nado/helios-platform/pull/141)  
Head: codex/edit-helios.sh-for-remote-priority-case at 2ed4ca13ccbe204be45d481f65a871d43a25bc35  
State at audit: open, non-draft, reported mergeable  
Current comparison: diverged, 3 commits ahead and 21 behind; two effective files, 41 additions and 7 deletions

#### Required behavior

- Add helios.sh remote-priority.
- Invoke branch_intelligence.py for the helios-control and hermes-fleet-production remotes.
- Add repeatable --remote filters.
- Add --remote-inventory-only.
- Permit --fetch-remotes as an alias while keeping remote configuration and fetching opt-in.
- Filter branch ranking only when explicit remote filters are supplied; preserve default behavior otherwise.

#### Evidence and known defects

The PR recorded Bash syntax validation and a successful report-generation command. Reviews identified several correctness and security gaps:

- [Create the report directory before writing](https://github.com/M0nado/helios-platform/pull/141#discussion_r3517325402)
- [Export GH_TOKEN for GitHub CLI calls](https://github.com/M0nado/helios-platform/pull/141#discussion_r3517325409)
- [Resolve the actual target ref before diffing](https://github.com/M0nado/helios-platform/pull/141#discussion_r3517325412)
- [Correct the impossible CI threshold](https://github.com/M0nado/helios-platform/pull/141#discussion_r3517325425)
- [Preserve slash-qualified local branch names](https://github.com/M0nado/helios-platform/pull/141#discussion_r3517325428)
- [Exclude ignored/private paths from idea extraction](https://github.com/M0nado/helios-platform/pull/141#discussion_r3517325431)

#### Required acceptance before merge

- Unit-test local, remote-tracking, slash-qualified, and unusual valid Git ref names.
- Resolve target branches deterministically in fresh Actions checkouts.
- Pass GH_TOKEN only to read-only GitHub CLI calls.
- Do not scan .helios/private, ignored files, generated reports, credentials, or run bundles.
- Use a documented score scale.
- Keep configure, fetch, prune, merge, and delete actions separately gated.
- Produce identical ranking output for identical repo/ref/CI snapshots.

#### Dependency and disposition

Issue #149 must define which repositories/remotes are canonical, active, mirrors, or migration sources before this feature becomes the shared branch-ranking entrypoint. Reimplement the small delta on current main after that governance gate.

### 4.3 PR #142 — GitHub Action pin modernization

Source: [PR #142](https://github.com/M0nado/helios-platform/pull/142)  
Head: copilot/understand-codebase-structure at 85d0e06dc0d733a6ac22b2105e3e5f6c1975a319  
State at audit: open, draft, reported non-mergeable  
Current comparison: 5 commits ahead and 28 behind; 27 files, 120 additions and 112 deletions

#### Required behavior

- Replace actions/upload-artifact v3 with v4.
- Replace actions/download-artifact v3 with v4.
- Replace actions/cache v3 with v4.
- Replace actions/setup-dotnet v3 with v4.
- Replace selected checkout v3 references with v4.
- Avoid changing product/runtime behavior.

#### Current evidence

The audited main branch already uses v4 for the core actions above in the inspected workflow set. Historical build-variant reports on #142 were commonly skipped, and no product build evidence makes the stale branch preferable to a current audit.

#### Required acceptance

- Parse every workflow.
- Inventory all action refs, separating a third-party action whose own release is v3 from deprecated core actions.
- Verify every build lane finds a real project.
- Verify no required job can silently skip the repository.
- Apply least-privilege permissions per job.
- Pin sensitive third-party actions according to the repository's supply-chain policy.

#### Disposition

Audit current main, capture any remaining exceptions, then close #142 as superseded. Do not merge 27 stale workflow/doc changes.

### 4.4 PR #143 — earlier AIHub mixed-runtime variant

Source: [PR #143](https://github.com/M0nado/helios-platform/pull/143)  
Head: codex/update-build_graph.py-for-classification-and-reporting-lhug7d at 42941661ae595d5dce718e5387869325168b5abd  
State at audit: open, non-draft, reported non-mergeable  
Current comparison: 4 commits ahead and 21 behind; 90 files, 8,976 additions and 99 deletions

#### Intended capability

- Report-first AIHub control plane.
- Branch absorption, branch-fix agents, fleet planning, build-graph execution, grading, module placement, dashboards, and setup bundles.
- C# orchestration contracts.
- F# learning and ranking.
- C++ native assist.
- Python analysis, provider, agent, Azure, and report glue.

#### Evidence and blockers

The PR reported Python compile checks, JSON validation, one Python unittest module, and a quick build-graph smoke command. It did not establish full C#/F#/C++ runtime validation.

Major review findings included:

- [PowerShell here-string parser failure](https://github.com/M0nado/helios-platform/pull/143#discussion_r3521025058)
- [Azure client secret persisted into generated reports](https://github.com/M0nado/helios-platform/pull/143#discussion_r3521025063)
- [Changed-only workflow compares against the wrong base](https://github.com/M0nado/helios-platform/pull/143#discussion_r3521074276)
- [Generated branch commands do not quote refs](https://github.com/M0nado/helios-platform/pull/143#discussion_r3521074283)
- [Key Vault secret values are exposed on process arguments](https://github.com/M0nado/helios-platform/pull/143#discussion_r3521074288)
- [Quick profile excludes many quick-tagged nodes](https://github.com/M0nado/helios-platform/pull/143#discussion_r3521074289)

#### Disposition

PR #144 is the merged successor. Do not merge #143. Compare only for genuinely unique content, then close it as superseded.

### 4.5 PR #144 — merged AIHub baseline

Source: [PR #144](https://github.com/M0nado/helios-platform/pull/144)  
Head at merge: b9ae8f33f7ee60d50aaf4acc3c08f1c346c85d5e  
Merge commit: 266c4d51d92c317e23c1a0951496c5fe11e98ca9  
State: merged

#### Capability established

- GitHub Actions for self-learning growth, branch absorption, branch-fix agents, build graph, Azure planning, and dashboards.
- Python report and orchestration scripts.
- C# AIHub engine contracts and module blueprints.
- F# learning/scoring functions.
- C++20 native assist functions.
- GUI and remote-console previews.
- Report-only guardrails and opt-in live flags.

Key current sources:

- [F# AIHub learning engine](https://github.com/M0nado/helios-platform/blob/main/src/analytics/HELIOS.Analytics.FSharp/AIHub/AiHubLearningEngine.fs)
- [C# AIHub contracts](https://github.com/M0nado/helios-platform/blob/main/src/core/HELIOS.Platform.Contracts/AIHubEngineContracts.cs)
- [C++ native assist](https://github.com/M0nado/helios-platform/blob/main/src/native/HELIOS.Native.Performance/include/helios/aihub_native_engine.hpp)
- [Self-learning workflow](https://github.com/M0nado/helios-platform/blob/main/.github/workflows/aihub-self-learning-growth.yml)
- [Build-graph workflow](https://github.com/M0nado/helios-platform/blob/main/.github/workflows/build-graph-automation.yml)

#### Verified architectural intent

- C# owns policy, identity, vault, GUI state, typed APIs, and cross-engine dispatch.
- F# owns most learning/ranking mathematics.
- C++ assists only profiled hot paths.
- Python owns provider, Linux-tool, and report glue where useful.
- Self-learning is report-only by default; the workflow sets AIHUB_LIVE_MUTATION to false and uses read-only repository permissions.

#### Remaining acceptance gaps

- Existing F# tests cover general analytics, not AiHubLearningEngine.
- C++ is a header-only interface target with no compiled test executable.
- The self-learning workflow runs Python but does not build/test C#, F#, or C++.
- The build graph records failed nodes but main returns success after reporting them.
- The quick profile omits the quick tag.
- Python version is inconsistent across workflows: 3.11 and 3.12.
- The solution file omits the real WPF project, F# projects/tests, native target, and RepositoryAnalytics.
- The Windows application targets net8.0-windows while its test project targets net8.0.

Related review evidence:

- [Let .NET build failures fail the workflow](https://github.com/M0nado/helios-platform/pull/144#discussion_r3521518281)
- [Align tests with the Windows application TFM](https://github.com/M0nado/helios-platform/pull/144#discussion_r3521518282)
- [Include the actual platform project in the solution](https://github.com/M0nado/helios-platform/pull/144#discussion_r3521518286)
- [Fail build-graph runs when nodes fail](https://github.com/M0nado/helios-platform/pull/144#discussion_r3521518301)
- [Return engine rankings in score order](https://github.com/M0nado/helios-platform/pull/144#discussion_r3521518303)

#### Disposition

Treat #144 as the code baseline, not as evidence that the mixed-runtime system is production-ready. Harden it through narrow, tested follow-up PRs.

### 4.6 PR #145 — duplicate/alternative AIHub expansion

Source: [PR #145](https://github.com/M0nado/helios-platform/pull/145)  
Head: codex/update-build_graph.py-for-classification-and-reporting-kuifvm at 8bab299f77c6e50acbe9c483fe27b024ea0924bc  
State at audit: open, non-draft, reported non-mergeable  
Current comparison: 2 commits ahead and 21 behind; 92 files, 9,602 additions and 99 deletions

#### Duplication evidence

The following key blobs on #145 are identical to current main:

- F# AIHub learning engine.
- C# AIHub engine contracts.
- C++ native engine header.
- Python build graph.
- AIHub self-learning workflow.

One potentially unique helper observed during the audit was scripts/setup/auto-exe-web.sh. That helper does not justify merging the branch.

#### Review blockers

- [Propagate failed build-graph nodes](https://github.com/M0nado/helios-platform/pull/145#discussion_r3521667389)
- [Do not skip secret scans merely because a line contains angle brackets](https://github.com/M0nado/helios-platform/pull/145#discussion_r3521667390)
- [Include quick-tagged nodes in the quick profile](https://github.com/M0nado/helios-platform/pull/145#discussion_r3521667395)
- [Quote branch refs in generated commands](https://github.com/M0nado/helios-platform/pull/145#discussion_r3521667397)
- [Detect modern OpenAI and GitHub token prefixes](https://github.com/M0nado/helios-platform/pull/145#discussion_r3521667402)
- [Gate Azure create commands in plan-only wizard mode](https://github.com/M0nado/helios-platform/pull/145#discussion_r3521667405)
- [Load complete grading data before organizing the repository](https://github.com/M0nado/helios-platform/pull/145#discussion_r3521667409)

#### Disposition

Inventory files that are absent from main, review them independently, port only approved helpers into focused PRs, and close #145.

### 4.7 PR #146 — claimed unified pipeline scaffold

Source: [PR #146](https://github.com/M0nado/helios-platform/pull/146)  
Head: Yolkster64-patch-1feat/unified-pipeline-scaffold at 0b0000b588e5cdc608c324cd918c5a25604e1124  
State at audit: open, reported mergeable  
Current comparison: 1 commit ahead and 5 behind; exactly one changed file, 172 additions

#### Claimed scope

The PR body claims:

- seven-job GitHub deployment pipeline;
- Azure App Service, ACR, OIDC, Key Vault, OpenAI, and hybrid infrastructure;
- .NET 8 API;
- Python GUI, Slack bot, and smart router;
- Dockerfile and supervisor configuration;
- Copilot and M365 manifests;
- setup script and OpenAPI document.

#### Actual scope

Only .github/workflows/helios-deploy.yml exists in the PR delta. The claimed Dockerfile, infrastructure, services, tests, manifests, and setup files are absent.

The workflow itself is malformed and cannot be loaded. It references nonexistent paths including src/AIHub/AIHub.csproj, src/AIHub.Tests, scripts/requirements.txt, scripts/tests, and a misplaced Hermes readiness script. It also treats arbitrary workflow-dispatch refs as deployable and grants workflow-wide write permissions.

Evidence:

- [Invalid top-level YAML structure](https://github.com/M0nado/helios-platform/pull/146#discussion_r3538063575)
- [Build job references missing files](https://github.com/M0nado/helios-platform/pull/146#discussion_r3538063582)
- [No Dockerfile for the container job](https://github.com/M0nado/helios-platform/pull/146#discussion_r3538063587)
- [Manual runs can deploy arbitrary refs](https://github.com/M0nado/helios-platform/pull/146#discussion_r3538063594)
- [Write token reaches PR-controlled build steps](https://github.com/M0nado/helios-platform/pull/146#discussion_r3538063601)
- [Malformed Codex action reference](https://github.com/M0nado/helios-platform/pull/146#discussion_r3538063610)

#### Runtime conflict

The PR body says the Windows target was changed from net8.0-windows to net8.0 for Linux App Service. The only file in the branch is YAML, and current main still has a WPF net8.0-windows application. A Windows GUI must not be retargeted into a Linux server. The correct architecture is a cross-platform net8.0 service/broker plus a separate net8.0-windows client.

#### Disposition

Do not merge. Replace this mega-workflow with independently testable build, package, infrastructure-plan, deployment, connector, validation, and rollback workflows under Issue #148.

### 4.8 PR #147 — collaboration control plane

Source: [PR #147](https://github.com/M0nado/helios-platform/pull/147)  
Head at merge: 131fa18591c47793d75568b54191055cfba31eb9  
Merge commit: ae0e4689a4a0008378f2235cdb0756c71548420d  
State: merged

#### Capability established

- Repository-wide Copilot instructions.
- Reusable agent/automation skills.
- GitHub/Linear/Slack/Teams/SharePoint/Azure operating model.
- Initial normalized-event workflow.
- Azure workload-identity and production approval guidance.

#### Current implementation limit

The current [integration-sync workflow](https://github.com/M0nado/helios-platform/blob/main/.github/workflows/integration-sync.yml):

- writes a small JSON artifact;
- uploads that artifact;
- checks only that three Azure variables are nonempty;
- intentionally delegates Slack, Linear, Teams, and SharePoint delivery to a future service.

It therefore does not currently automate Slack or Linear.

#### Required corrections

- Emit the documented fields: source, eventType, entityId, correlationId, environment, occurredAt, and links.
- Generate JSON with a real encoder rather than shell interpolation.
- Subscribe to reopened PRs.
- Exercise an actual OIDC exchange and a read-only Azure account call.
- Keep delivery and connector credentials out of the workflow.

Evidence:

- [Event does not match the documented envelope](https://github.com/M0nado/helios-platform/pull/147#discussion_r3573935691)
- [Unsafe JSON interpolation of refs](https://github.com/M0nado/helios-platform/pull/147#discussion_r3573935705)
- [Reopened PRs are not synchronized](https://github.com/M0nado/helios-platform/pull/147#discussion_r3573935713)
- [Azure federation check does not test federation](https://github.com/M0nado/helios-platform/pull/147#discussion_r3573935717)

#### Disposition

Keep the documentation and operating model. Repair event creation in a focused workflow PR. Implement actual delivery through Issues #154 and #156.

### 4.9 Issue #148 — Enterprise Deployment Manager

Source: [Issue #148](https://github.com/M0nado/helios-platform/issues/148)  
Architecture: [ENTERPRISE_DEPLOYMENT_MANAGER.md](https://github.com/M0nado/helios-platform/blob/main/docs/integrations/ENTERPRISE_DEPLOYMENT_MANAGER.md)  
State: open, assigned, labeled in progress

#### Owned capabilities

- Azure provisioning.
- Entra integration.
- Azure Key Vault.
- Azure AI Foundry.
- GitHub Actions OIDC federation.
- Graph, Teams, SharePoint, and OneDrive connectors.
- Linear and Slack connectors.
- Hermes and AIHub registration.
- Validation, rollback, telemetry, and compliance evidence.

#### Canonical production resources

- rg-helios-prod
- kv-helios-core
- cosmos-helios-memory
- stheliosartifacts
- cae-helios
- log-helios

#### Explicit acceptance criteria

- Provision canonical resources from IaC.
- Use GitHub OIDC with no long-lived Azure client secret.
- Resolve connector credentials from Key Vault.
- Support bounded bidirectional GitHub/Linear synchronization.
- Alert Slack and Teams on approved failures/events.
- Publish approved release evidence to SharePoint via Graph.
- Scope OneDrive to approved export/draft workflows.
- Register and health-check Hermes and AIHub.
- Execute validation and rollback from Actions and the GUI.
- Require explicit production environment approval.
- Never bypass subscription access, tenant/admin consent, workspace ownership, or approval gates.

#### Linked implementation tracks

- [#153 — Azure foundation and GitHub OIDC](https://github.com/M0nado/helios-platform/issues/153)
- [#154 — Key Vault-backed integration broker](https://github.com/M0nado/helios-platform/issues/154)
- [#155 — Graph, Teams, SharePoint, and OneDrive](https://github.com/M0nado/helios-platform/issues/155)
- [#156 — GitHub/Linear and Slack synchronization](https://github.com/M0nado/helios-platform/issues/156)
- [#157 — Hermes fleet and AIHub registration](https://github.com/M0nado/helios-platform/issues/157)
- [#158 — Validation, rollback, and compliance evidence](https://github.com/M0nado/helios-platform/issues/158)
- [#159 — PowerShell syntax validation debt](https://github.com/M0nado/helios-platform/issues/159)

#### Learning traceability

Issue #148 refers to AIHub registration, model registry, task API, durable memory, health, and evidence. It does not specify model training or autonomous code mutation.

To preserve the merged #144 learning capability and expand it safely, #148/#157/#158 should record:

- scoring schema and weight/model version;
- source commit and input-data snapshot;
- redaction/security label;
- evaluation result and promotion gate;
- service image digest;
- health/readiness state;
- rollback version;
- evidence hashes and correlation ID.

### 4.10 Issue #149 — repository inventory and authority

Source: [Issue #149](https://github.com/M0nado/helios-platform/issues/149)  
Inventory-v2 evidence: [issue comment](https://github.com/M0nado/helios-platform/issues/149#issuecomment-4972438996)  
State: open

#### Required classification

Every accessible HELIOS/Hermes-related repository must be classified as:

- canonical;
- active module;
- mirror;
- migration source;
- archive candidate;
- unrelated.

#### Required deliverables

- machine-readable repository inventory;
- dependency/duplication map;
- migration plan for active code/assets;
- archive/read-only recommendations;
- branch-protection and Actions policy;
- GitHub App and secret-scope review;
- no destructive archive/delete action without separate approval.

#### Acceptance criteria

- Every HELIOS/Hermes-related repository has an owner and classification.
- Canonical source paths are documented.
- Duplicate repositories cannot independently publish production artifacts.
- CI and deployment permissions are limited to canonical repositories.

#### Current inventory-v2 result

The published comment reports:

- 33 accessible repositories;
- one canonical production publisher: M0nado/helios-platform;
- nine active modules;
- two mirrors;
- four migration sources;
- thirteen archive candidates;
- four unrelated repositories.

Issue #149 remains a gate because branch migration, ownership confirmation, protections, and any archive action must follow the governed inventory rather than an ad hoc merge sweep.

## 5. Language and runtime contract

| Runtime | Intended ownership | Current evidence | Required hardening |
| --- | --- | --- | --- |
| C# / .NET 8 cross-platform | Typed contracts, broker/API, policy, identity, Key Vault access, audit, GUI state, cross-engine dispatch | HELIOS.Platform.Contracts targets net8.0 | Add actual service projects, contract tests, schemas, and Linux build/publish |
| C# / .NET 8 Windows | WPF operator client and Windows/USB-specific capabilities | HELIOS.Platform targets net8.0-windows | Isolate from Linux service; use compatible Windows test target; no server deployment from WPF project |
| F# / .NET 8 | Pure scoring, ranking, prediction, and decision fusion | AiHubLearningEngine.fs in net8.0 library | Add direct unit/property/golden tests; version weights and thresholds |
| C++20 | Optional profiled hot-path/vector/graph/native assist | Header-only interface target | Add compiled tests, sanitizers, benchmarks, and explicit C ABI/P/Invoke boundary |
| Python | Report generation, provider/Linux integration, branch analysis, connector tooling | Workflows use Python 3.11 and 3.12 | Select one version; add unit/integration tests, JSON Schema, safe argv, deterministic outputs |
| YAML / GitHub Actions | Build/test/package/plan/deploy/evidence orchestration | Many workflows, including merged #144/#147 | Parse all files, least privilege, fail closed, pin actions, prevent arbitrary-ref production deploy |
| Bicep / azd | Repeatable Azure environments | #148/#153 requirements and existing templates | Build/lint/validate/what-if with environment parameters and rollback metadata |
| JSON / Markdown | Machine ledger and human evidence | Reports, configs, event artifacts | Version schemas; provenance, redaction, hashes, stable ordering, no recursive self-ingestion |

### 5.1 Required dependency direction

1. HELIOS.Platform.Contracts remains a cross-platform net8.0 assembly.
2. F# analytics references the contracts assembly.
3. Cross-platform C# services may reference both contracts and F# analytics.
4. The Windows client consumes service/contracts through typed APIs and does not become the Linux service.
5. C++ exposes a small stable ABI and never owns cloud identity, policy, or mutation.
6. Python communicates through versioned JSON/event contracts and does not bypass C# policy gates.

## 6. Learning preservation and expansion

### 6.1 What exists today

The current system is a report-first heuristic learning system:

- F# functions combine explicitly coded weights.
- Python synthesizes repository reports and “feedback” summaries.
- C# selects routes and defines policy/guardrails.
- C++ provides optional scoring accelerators.
- the workflow is scheduled and report-only.

It is not yet a trained self-updating model. Calling it adaptive production learning would exceed the evidence.

### 6.2 Required “keep plus more” acceptance

- Version every score schema, weight set, feature definition, and threshold.
- Create frozen golden datasets from approved repository/CI snapshots.
- Test determinism and ranking monotonicity.
- Evaluate precision/recall or decision quality against operator-reviewed outcomes.
- Record input commit, repository/ref SHA, report versions, and data lineage.
- Exclude generated reports, saved bundles, private paths, secrets, and quarantined artifacts from learning input.
- Redact and security-label memory before storage.
- Run new weights/models in shadow mode before promotion.
- Require a PR and human review for changes to production weights, schemas, or mutation policy.
- Add drift, stale-data, contradiction, and feedback-loop detection.
- Preserve the previous version and make rollback executable.
- Treat delete/archive recommendations as suggestions until separate approval.
- Store approved memory/evidence with correlation IDs and hashes.

## 7. GitHub, Linear, Slack, Teams, and SharePoint automation contract

### 7.1 Source of truth

- GitHub is the engineering source of truth.
- Linear mirrors approved work-item/status fields.
- Slack and Teams receive operational notifications and bounded commands.
- SharePoint stores approved governed release/deployment evidence.
- Azure broker state provides delivery audit, retries, and dead letters.

### 7.2 Normalized event

Every event must contain, at minimum:

- schemaVersion;
- source;
- eventType;
- entityId;
- idempotencyKey;
- correlationId;
- environment;
- occurredAt;
- repository and commit/ref where applicable;
- actor classification;
- links;
- redacted payload or payload reference.

### 7.3 Delivery guarantees

- Connector credentials come from Key Vault or managed identity.
- Every write is idempotent.
- Failed delivery is retryable and replayable without duplicate side effects.
- Comment/status echo loops are prevented.
- GitHub/Linear field ownership is documented.
- Slack/Teams destinations are allowlisted.
- Production/security alerts are rate-limited and deduplicated.
- All connector actions appear in telemetry under the same correlation ID.
- No raw secret appears in source, logs, artifacts, Slack, Teams, Linear, or SharePoint.

### 7.4 Current gap

PR #147 does not deliver to Linear, Slack, Teams, or SharePoint. It creates only an event artifact. Issues #154, #155, and #156 are the actual delivery implementation.

## 8. Consolidated safe build and delivery sequence

### Gate 0 — freeze authority and preserve evidence

1. Treat M0nado/helios-platform as the only production publisher.
2. Complete owner/classification/protection fields under #149.
3. Snapshot every candidate ref by repository, branch, SHA, owner, license, build result, and classification.
4. Do not delete, archive, force-push, or directly merge migration sources.

### Gate 1 — restore trustworthy CI

1. Complete #159 and the active PR #170 repair gate.
2. Make PowerShell validation enumerate exact paths and parser diagnostics.
3. Make every failed build-graph node exit nonzero.
4. Include the quick tag in the quick profile.
5. Parse every workflow and confirm required jobs cannot silently skip.
6. Finish the current Action-pin audit and supersede #142.

### Gate 2 — establish one real mixed-runtime build matrix

1. Cross-platform .NET:
   - build C# contracts/services;
   - build F# analytics;
   - run F# learning tests.
2. Windows .NET:
   - build the WPF application;
   - run compatible Windows tests.
3. Native:
   - configure/build C++20;
   - compile and run native tests.
4. Python:
   - choose one supported version;
   - compile and run unit/integration tests.
5. Infrastructure:
   - Bicep build/lint;
   - validate and what-if;
   - no apply on pull requests.
6. Include the real projects in the solution/build graph.

### Gate 3 — reconcile PRs #140–#147

1. Port #140 as a two-file feature plus producer fix, tests, and schema.
2. Port #141 as a two-file feature after #149 defines allowed remotes.
3. Close #142 after current Action-pin validation.
4. Close #143 as superseded by merged #144.
5. Harden #144 through narrow PRs.
6. Audit unique #145-only files, port approved helpers, and close #145.
7. Close/rewrite #146; do not reuse its malformed mega-workflow.
8. Correct #147 event generation and OIDC validation.

### Gate 4 — harden and version learning

1. Add F# learning tests and versioned weight manifests.
2. Add compiled native tests and a stable interop boundary.
3. Add Python learning/report tests and self-ingestion exclusions.
4. Add C# policy tests proving report-only and approval gates.
5. Produce signed, reproducible learning evaluation artifacts.
6. Promote only through local review, CI review, PR review, and environment approval.

### Gate 5 — execute Enterprise Deployment Manager

1. #153: provision dev/test foundation and validate GitHub OIDC.
2. #154: deploy the Key Vault-backed broker with health, idempotency, retries, dead letters, audit, and correlation IDs.
3. #155 and #156: implement M365 plus GitHub/Linear/Slack connectors against the broker.
4. #157: register versioned Hermes and AIHub endpoints, model registry, task API, memory, and readiness checks.
5. #158: validate end to end, capture rollback point, sign evidence, and publish approved evidence to SharePoint and stheliosartifacts.
6. Only after dev/test evidence passes may a production environment approval authorize deployment.

### Gate 6 — close program acceptance

Issue #148 closes only when:

- canonical resources are reproducibly provisioned;
- OIDC and least-privilege identity work;
- Key Vault-backed connectors pass;
- GitHub/Linear and Slack/Teams delivery pass without loops or duplicates;
- SharePoint publishing passes;
- Hermes/AIHub registration and learning-service version evidence pass;
- telemetry uses end-to-end correlation IDs;
- rollback is executable and preserves persistent stores;
- signed evidence contains commit, workflow, resource/identity inventory, permissions, validation, rollback point, and SHA-256 manifests;
- production still requires explicit environment approval.

Issue #149 closes only when every relevant repository has a verified owner/classification and all duplicate publishers are technically prevented from releasing production artifacts.

## 9. Non-negotiable safety boundaries

- No direct-to-main feature migration.
- No repository deletion or archive without separate approval.
- No automatic persistent-store deletion during rollback.
- No long-lived Azure client secret in GitHub.
- No broad Azure Owner or tenant-wide Graph permission without a time-bounded exception.
- No secrets in reports, process arguments, Actions logs, chat, or SharePoint evidence.
- No production deploy from an arbitrary workflow-dispatch ref.
- No autonomous code deletion, branch pruning, cloud apply, or production learning promotion.
- No claim that a workflow is integrated merely because it uploads a JSON artifact.

## 10. Decision record

Recommended decisions:

1. Preserve #144 learning and orchestration as the baseline.
2. Keep #148 as deployment/registration authority, not unrestricted self-modification authority.
3. Complete #149 governance before broad branch/repository movement.
4. Salvage #140 and #141 through fresh scoped branches.
5. Supersede #142, #143, and #145 after unique-delta verification.
6. Reject #146 in its present form.
7. Keep #147 documentation but replace its artifact-only workflow with a valid normalized event and broker delivery.
8. Make the mixed-runtime build matrix and CI truth the first implementation milestone.

