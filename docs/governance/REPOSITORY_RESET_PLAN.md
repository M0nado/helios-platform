# Repository reset and five-lane delivery plan

This plan is the clean starting point for reducing branch, issue, workflow, and
documentation sprawl without throwing away unreviewed work. It does **not** merge
or delete remote branches blindly. Branch retirement requires unique-delta
evidence, a pull request, passing checks, and approval.

## Five durable integration branches

`main` remains the protected, releasable source of truth. Work uses short-lived
`feature/`, `fix/`, `chore/`, or `docs/` branches and squash-merges into exactly
one of five integration lanes:

1. `integration/product`: WinUI 3, C# presentation, accessibility, packaging.
2. `integration/platform`: C# services, C++ performance boundaries, storage, runtime.
3. `integration/intelligence`: F# analytics, Python AIHub, Hermes, and XCore.
4. `integration/security`: Guardian, identity, policy, threat and compliance work.
5. `integration/operations`: GitHub, CI, Azure, releases, docs and repository hygiene.

The lanes merge to `main` through reviewed PRs. They are not five independent
products, permanent personal branches, or a reason to bypass checks. Hotfixes may
target `main`, but must back-merge to the affected lane.

## Ordered top 15

The executable source of this backlog is
[`config/repository-backlog.json`](../../config/repository-backlog.json). Its order
is intentional: inventory precedes deletion, CI consolidation precedes deep
feature changes, and identity/security gates precede deployment.

| # | ID | Lane | Outcome |
|---:|---|---|---|
| 1 | ORG-001 | operations | Inventory and safely absorb non-default branches. |
| 2 | ORG-002 | operations | Apply protected five-lane rules and squash merging. |
| 3 | ORG-003 | operations | Replace overlapping Actions with one required CI entry point. |
| 4 | ORG-004 | platform | Establish a canonical solution and polyglot build graph. |
| 5 | ORG-005 | operations | Quarantine generated, historical, and root-level clutter. |
| 6 | ORG-006 | security | Audit secrets, binaries, dependencies, and privileged actions. |
| 7 | ORG-007 | operations | Pin and make C#, F#, C++, Python, and WinUI CI deterministic. |
| 8 | ORG-008 | intelligence | Unify AIHub, Hermes, and XCore event/evaluation contracts. |
| 9 | ORG-009 | platform | Reconcile `monado/helios-control` with canonical modules. |
| 10 | ORG-010 | intelligence | Contract-audit the external Hermes fleet boundary. |
| 11 | ORG-011 | security | Bootstrap Azure with CLI, OIDC, Key Vault, and Bicep what-if. |
| 12 | ORG-012 | product | Select one tested WinUI 3 shell and retire prototypes. |
| 13 | ORG-013 | platform | Keep C++ only at measured, safe performance boundaries. |
| 14 | ORG-014 | intelligence | Validate F# math and Python prediction pipelines. |
| 15 | ORG-015 | operations | Automate backlog, releases, and hygiene drift reporting. |

Preview the issue set locally; mutation is explicit:

```bash
python3 scripts/github/sync_repository_backlog.py
python3 scripts/github/sync_repository_backlog.py --apply --repo M0nado/helios-platform
```

The apply mode is idempotent by stable `ORG-nnn` marker. It requires an
authenticated GitHub CLI and the `repository-reset` label. It creates missing
issues but deliberately never closes, merges, or deletes anything.

## Safe reset sequence

1. Export remote branches, open issues/PRs, required checks, and last activity.
2. Compare every branch to `main` using patch IDs, paths, build evidence, and an owner.
3. Label duplicates instead of closing them until a canonical issue is accepted.
4. Create the five lanes from a green `main`; install the JSON rules as GitHub rulesets.
5. Absorb unique work through small lane PRs. Never use an unreviewed octopus merge.
6. Consolidate CI by changed-path matrices, then disable superseded workflows.
7. Move historical reports to release artifacts or an archive in one reviewed batch.
8. Delete merged short-lived branches automatically; archive unmerged branches first.
9. Promote each green lane to `main`; tag the reset baseline and publish evidence.

Cross-repository work remains contract-based. `helios-platform` owns product and
execution code; the bootstrap control plane owns shared policy. External fleet
repositories must be inspected with authenticated inventory evidence before any
claim that all branches have been reconciled.
