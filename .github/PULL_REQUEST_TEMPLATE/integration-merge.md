# Integration Merge Checklist

Use this template for coordinated integration merges across Helios, Hermes, AIHub, XCore, and related platform branches.

## Required Validation

- [ ] Branch inventory attached.
- [ ] `helios-control` merged and validated.
- [ ] `hermes-fleet-production` merged and validated.
- [ ] AIHub/XCore tests passed.
- [ ] C#/.NET restore/build/test passed.
- [ ] WinUI/WPF build passed if UI files changed.
- [ ] C++ build and benchmarks passed if native files changed.
- [ ] F# analytics/prediction tests passed if F# files changed.
- [ ] Python AIHub tests passed if Python files changed.
- [ ] PowerShell ScriptAnalyzer passed.
- [ ] Azure dry-run validation passed.
- [ ] NuGet artifact generated without PR-time publishing.
- [ ] Wiki generated.
- [ ] Dashboard published.
- [ ] Auto-merge/autopull verified.
- [ ] Rollback plan attached.
- [ ] Workflow links included.

## Branch Inventory

Attach or link the branch inventory, including source branches, target branch, merge order, conflict notes, and commit SHAs.

## Validation Evidence

Add links to workflow runs, logs, benchmark summaries, Azure dry-run output, generated NuGet artifact, wiki output, and dashboard publication.

## Rollback Plan

Describe the rollback approach, expected impact, and owners for each affected area.
