# Codex Task: .nuget from knowledge-base

- Priority: 23
- Task type: idea-review
- Allowed paths: `['.nuget/HELIOS.Platform.nuspec.md:6']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: Adds fleet agent telemetry and learning sources for distributed workload optimization.
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
