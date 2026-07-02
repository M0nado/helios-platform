# Codex Task: docs/integration from work

- Priority: 86
- Task type: compare-selectively
- Allowed paths: `['docs/integration/BRANCH_INTELLIGENCE.md', 'docs/integration/CLOUDSHELL_GITHUB_AZURE_SETUP.md', 'docs/integration/CONTROL_PLANE_COMMANDS.md', 'docs/integration/VISUAL_STUDIO_MAUI_SETUP.md', 'docs/integration/WEB_CONTROL_PLANE.md', 'docs/integration/remote-manifest.json']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
