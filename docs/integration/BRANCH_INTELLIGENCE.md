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
- `idea-impact.json` / `.md`
- `idea-impact-summary.json` / `.md`
- `agent-work-queue.json` / `.md`
- `analytics-metrics.json`
- `connectivity.json` / `.md`
- `remote-actions.json`
- `dashboard.md`

## Safe remote activation

Remote inventory is safe by default. Keep remote URLs out of git and provide them through environment variables only:

```bash
export HELIOS_CONTROL_REMOTE_URL=<helios-control-git-url>
export HERMES_FLEET_REMOTE_URL=<hermes-fleet-production-git-url>
export XCORE_REMOTE_URL=<xcore-git-url>
python3 scripts/analysis/branch_intelligence.py --remote-inventory-only
```

To add configured remotes, first review `reports/branch-intelligence/remote-inventory.md`, then opt in explicitly:

```bash
python3 scripts/analysis/branch_intelligence.py --configure-remotes
python3 scripts/analysis/branch_intelligence.py --fetch-remotes
```

Do not enable merge or prune work from remote branches until the dashboard recommends a safe action and the branch-specific quality gates have passed.
