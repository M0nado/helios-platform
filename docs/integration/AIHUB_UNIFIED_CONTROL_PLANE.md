# AIHub Unified Control Plane

The AIHub unified control plane is the report-first bridge between the C# orchestration backbone, F# learning/scoring, C++ native hot-path assist, Python integration glue, and GitHub Actions automation.

## Backbone responsibilities

- **C#** owns identity, policy, GUI state, typed contracts, route decisions, and safe dispatch.
- **F#** owns score fusion, ranking, prediction, module learning, branch learning, and agent XP math.
- **C++** owns native assist only when C# detects hot-path comparison pressure such as speed, memory, graph, or small-file fan-out.
- **Python** owns report ingestion, provider/tool readiness, CLI glue, and dashboard artifact generation.
- **YAML** owns recurring, manual, and PR-triggered report-only automation.

## Generated JSON and Markdown

Run the unified control-plane generator to produce the live report artifacts:

```bash
python3 scripts/analysis/aihub_self_learning_notes.py
python3 scripts/integrations/aihub_unified_control_plane.py
python3 scripts/dashboard/generate-gui.py
```

The generator writes:

- `reports/integrations/aihub-unified-control-plane.json`
- `reports/integrations/aihub-unified-control-plane.md`
- `status-site/index.html`

Those files are generated artifacts and are normally ignored by Git, while `config/aihub-unified-control-plane.example.json` is the committed stable JSON shape used for review and integration.

## Report-only guardrails

- Keep `AIHUB_LIVE_MUTATION=false` unless an operator explicitly enables live automation.
- Never place pasted keys or service credentials in the static dashboard or generated Markdown.
- Treat delete/prune/fix actions as advice until security preflight and apply gates pass.
- Use GitHub and Azure CLIs only after authentication, cost checks, and rollback plans are reviewed.

## Control lanes

| Lane | Workflow | Main reports | Purpose |
| --- | --- | --- | --- |
| Self-learning growth | `.github/workflows/aihub-self-learning-growth.yml` | `reports/learning/aihub-self-learning-notes.*`, `complex-code-grading.*`, `module-submodule-organizer.*` | Grow notes, placement guidance, and grading context without mutation. |
| Branch absorption multicloud | `.github/workflows/branch-absorption-multicloud.yml` | `reports/branch-intelligence/branch-absorption-multicloud-plan.*`, `reports/branch-agents/branch-test-autofix-plan.*` | Compare branch work, preserve unique value, and model GitHub/Azure lanes. |
| Agent fleet autopilot | `.github/workflows/branch-fix-agents.yml` | `reports/branch-agents/agent-fleet-control-catalog.*`, `agent-fleet-autopilot.*` | Assign Hermes/XCore specialists to branch, build, cloud, and GUI report-only tasks. |
| Build graph automation | `.github/workflows/build-graph-automation.yml` | `reports/build-graph/latest.*`, `reports/security/*` | Run static checks, dependency-aware nodes, and next-fix suggestions. |

## Stable template fields

The committed JSON template includes:

- `csharpBackbone`: the C# contract that owns policy, GUI state, typed APIs, and route dispatch.
- `engineRoutes`: the F#/C++/Python/YAML engines and their precise source files.
- `selfLearningBackboneBridge`: generated report inputs/outputs and safe commands.
- `controlLanes`: repeatable workflow-to-report mappings for learning, branch absorption, fleet planning, and build graph automation.
- `liveFlagPolicy`: allowed automation levels and required preflight scripts before any live operation.

## Operator flow

1. Run `scripts/setup/simple-build.sh quick` to validate core report nodes.
2. Run `scripts/setup/simple-build.sh learn` or the three generator commands above to refresh self-learning and unified-control reports.
3. Open `status-site/index.html` and inspect the unified control plane, learning notes, branch absorption, and agent fleet cards.
4. Keep actions report-only unless live flags, CLI authentication, security preflight, apply gate, cost caps, and rollback notes all pass.
