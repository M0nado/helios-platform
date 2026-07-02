# Codex Task: scripts from work

- Priority: 76
- Task type: extract-ideas
- Allowed paths: `['scripts/dashboard/generate-gui.py', 'scripts/integrations/cross_access_profiles.py', 'scripts/web/helios-web.py']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
