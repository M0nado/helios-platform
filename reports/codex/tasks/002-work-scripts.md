# Codex Task: scripts from work

- Priority: 76
- Task type: extract-ideas
- Allowed paths: `['scripts/analysis/merge_prune_recommendations.py', 'scripts/azure/azure-inventory.py', 'scripts/azure/sync-keyvault-secrets.sh', 'scripts/cloudshell/helios-cloudshell.sh', 'scripts/codex/generate-codex-tasks.py', 'scripts/dashboard/generate-actions.py', 'scripts/github/github-inventory.py', 'scripts/integrations/check-connections.py', 'scripts/setup/helios-dev.sh', 'scripts/web/helios-web.py']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
