# HELIOS Submodule Consolidation and Autonomous Merge Pattern

This repository now separates automated work into **single-focus branches** and
**submodule lanes** so AI automation can change code without waiting for a human
only after the configured checks and branch protections pass.

## Branch pattern

All autonomous consolidation work uses this branch pattern:

```text
ai/submodule/{module_slug}/{work_item}
```

Examples:

- `ai/submodule/helios-control/continuous-consolidation`
- `ai/submodule/hermes-fleet-production/old-bug-autofix`

The automation focuses on one work item branch until it is pushed, validated, and
merged or marked conflicted. New work starts from a new focus branch instead of
mixing unrelated fixes.

## Priority repositories

The consolidation config prioritizes these external repositories when their URLs
are provided as secrets:

| Repository | Secret URL | Lane | Focus |
| --- | --- | --- | --- |
| `helios-control` | `HELIOS_CONTROL_REPO_URL` | `csharp-winui-control` | C# WinUI 3 front end, control dashboards, accessibility, UI-state cleanup. |
| `hermes-fleet-production` | `HERMES_FLEET_PRODUCTION_REPO_URL` | `hermes-xcore-fleet` | Hermes fleet production, XCore orchestration, C++ performance back end, deployment hardening. |

The script also inventories existing `.gitmodules` entries so current modules can
be brought into the same branch and report pattern.

## Lane organization

| Lane | Canonical areas |
| --- | --- |
| `csharp-winui-control` | `src/gui`, `docs/WinUI3-Design`, `components/ai-dashboard` |
| `cpp-performance-backend` | `components/performance-ai`, `src/core` |
| `fsharp-analytics` | `components/analytics-core` |
| `python-aihub-integration` | `ai-integration`, `scripts/ai-integration`, `scripts/automation` |
| `security-azure-automation` | `src/Security`, `cloud-integration`, `microsoft-ecosystem/azure-integration`, `.github/workflows` |

## Autonomous operation

The `AI Submodule Branch Consolidation` workflow runs weekly in plan mode by
default. It can execute without manual input only when the repository secret
`HELIOS_SUBMODULE_AUTONOMOUS_ENABLED` is set to `true` and the repository URLs are
configured. Execution performs this pattern:

1. Fetch and update the configured repository.
2. Check out the target branch.
3. Create or reset the single focus branch.
4. Merge configured source branches one at a time.
5. Stop immediately on conflicts.
6. Run validation.
7. Push the focus branch.

The old-bug autofix workflow follows the same idea: scheduled execution is only
enabled when `HELIOS_AI_AUTONOMOUS_ENABLED=true`; auto-merge is requested only
when `HELIOS_AI_AUTOMERGE_ENABLED=true` and GitHub branch protection checks pass.

## Cleanup, reduction, and promotion rules

- Promote unique capabilities from external repositories into the matching lane.
- Keep one canonical owner for duplicated file names and APIs.
- Remove redundant copies only after the promoted owner passes validation.
- Rename files to the lane naming pattern during a dedicated cleanup work item.
- Never bypass protected branches; use GitHub auto-merge so checks decide whether
  the PR lands on `main`.

## Computer-driven merge/pull/push order

When autonomous execution is enabled, the computer is allowed to pull, merge, push,
open PRs, and request auto-merge across configured repositories. It must follow
this exact order every time:

1. Inventory repository URLs, `.gitmodules`, remote branches, lane ownership, and
   stale history candidates.
2. Fetch all remotes and prune deleted refs.
3. Select exactly one focus branch using `ai/submodule/{module_slug}/{work_item}`.
4. Merge the configured source branches newest-first and stop on conflicts.
5. Classify unique code by lane and promote it into the owning submodule branch.
6. Run validation, including Python compile checks and GitHub Actions linting.
7. Push the focus branch.
8. Open or reuse a PR and request GitHub auto-merge.
9. Treat branches older than the configured stale threshold as history cleanup
   candidates so old work is not allowed to block optimized current work.

## Governance dashboard, wiki, pages, milestones, and graph data

The `AI Governance Dashboard and Control Plane` workflow builds the project control
plane around this automation. In plan mode it writes dashboard artifacts. In
execute mode, gated by `HELIOS_GOVERNANCE_AUTONOMOUS_ENABLED=true` or manual
`execute=true`, it creates/updates labels, milestones, and a GitHub Project where
available, and publishes Pages-ready dashboard JSON/Markdown under `.github/pages`.
The dashboard contains graph nodes for priority repositories and lanes plus edges
showing where unique code should be promoted.
