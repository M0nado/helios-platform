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
- `connectivity.json` / `.md`
- `remote-actions.json`
- `dashboard.md`
