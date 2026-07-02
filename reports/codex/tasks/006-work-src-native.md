# Codex Task: src/native from work

- Priority: 94
- Task type: compare-selectively
- Allowed paths: `['src/native/HELIOS.Native.Performance/CMakeLists.txt', 'src/native/HELIOS.Native.Performance/README.md']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
