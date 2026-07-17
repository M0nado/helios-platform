# Post-174 Consolidation Sequence

The canonical foundation merged through PR #174. Follow-up branches must be replayed onto that mainline rather than force-merging stale histories.

## Ordered work

1. Port PR #163 environment repair and legacy evidence onto current `main`.
2. Port PR #166 guarded Windows boot security onto current `main`.
3. Reconcile PR #176 Azure runtime ownership with canonical Bicep modules.
4. Reconcile PR #177 shared ChatGPT/Copilot app with the approved Azure runtime.
5. Reconcile PR #167 agent registries against the merged broker and communication contracts.
6. Review promotion PR #169 and close evidence-only branches once their records are preserved.
7. Close or archive superseded PRs after replacement commits and CI are verified.

## Porting rule

For each stale branch:

- create a branch from current `main`;
- copy only reviewed files or replay focused commits;
- resolve module placement using `module-boundaries.v1.json`;
- run required CI;
- compare behavior and evidence with the original PR;
- merge with expected-head SHA enforcement;
- close the superseded PR with a replacement link.

No force merge, synthetic history join or direct-to-main copy is permitted.
