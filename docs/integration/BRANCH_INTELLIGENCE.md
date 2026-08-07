# HELIOS Branch Intelligence, Remote Setup, and Agent Queue

This workflow ranks branches, splits work by module/submodule, extracts unique ideas before merge/prune decisions, and records how each idea affects the platform.

## Safe policy

1. Configure remotes from `docs/integration/remote-manifest.json`.
2. Fetch branches only after remotes are reviewed.
3. Rank branches by CI closeness and module impact.
4. Split work by module/submodule.
5. Extract unique ideas before merging.
6. Score each idea with a bonus impact / “how it affects us” field.
7. Merge only high-value, low-risk branches.
8. Archive ideas from stale branches.
9. Prune only reviewed, low-value, already-merged branches.
10. Update the dashboard after every ranking run.

## Local setup

Install local tools without root access when needed:

```bash
scripts/setup/bootstrap-local-tools.sh
```

Run the analysis:

```bash
python3 scripts/analysis/branch_intelligence.py
```

To add configured/enabled remotes from the manifest:

```bash
python3 scripts/analysis/branch_intelligence.py --configure-remotes
```

To fetch branches after remotes are configured:

```bash
python3 scripts/analysis/branch_intelligence.py --fetch
```

To include optional Hermes fleet JSONL events and AI enrichment markers:

```bash
python3 scripts/analysis/branch_intelligence.py \
  --hermes-jsonl reports/fleet-learning/hermes-events.jsonl \
  --enrich-ideas
```

`--enrich-ideas` only marks records for enrichment unless OpenAI or Azure OpenAI credentials are present. The runner does not print secret values.

## Credentials and CLIs

The script checks local/CI availability for Git, GitHub CLI, Azure CLI, .NET, Python, and OpenAI/Azure OpenAI environment variables. It never prints secret values.

Recommended local authentication commands:

```bash
gh auth login
az login
export OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="..."
export AZURE_OPENAI_API_KEY="..."
```

## Reports

Generated reports are written to `reports/branch-intelligence/`:

- `branch-ranking.json` / `.md`
- `branch-source-manifest.md` (commits, authorship, files, conflicts, and disposition)
- `umbrella-issues.json` / `.md` (primary assignment and integration-train gates)
- `idea-impact.json` / `.md`
- `idea-impact-summary.json` / `.md`
- `agent-work-queue.json` / `.md`
- `analytics-metrics.json`
- `connectivity.json` / `.md`
- `remote-actions.json`
- `dashboard.md`

Each branch record includes its unique and patch-equivalent commits relative to
`origin/main`, the complete changed-file list, primary umbrella assignment,
module owner, merge-tree conflict result, security impact, temporary integration
branch, and disposition. The analyzer never creates, merges, pushes, or deletes
branches; issue creation and every integration train remain explicit reviewed
GitHub operations.
