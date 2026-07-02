# HELIOS Doctor

Runbook for getting local setup as close to 100% as possible.

| Tool | Status | Fix |
| --- | --- | --- |
| `gh` | ⚠️ | scripts/setup/bootstrap-local-tools.sh then gh auth login |
| `az` | ⚠️ | scripts/setup/bootstrap-local-tools.sh then az login |
| `dotnet` | ⚠️ | scripts/setup/bootstrap-local-tools.sh |
| `cmake` | ✅ | Install CMake or use a GitHub-hosted runner with CMake available |
| `python3` | ✅ | Install Python 3.12+ or use GitHub Actions setup-python |
| `git` | ✅ | Install Git or use Codespaces/Cloud Shell |

## One-command path

```bash
scripts/setup/helios-dev.sh --serve
```

## PR update path

```bash
python3 scripts/github/update-pr-from-reports.py --dry-run
python3 scripts/github/update-pr-from-reports.py --apply
```
