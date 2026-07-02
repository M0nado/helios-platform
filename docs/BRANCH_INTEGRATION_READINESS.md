# HELIOS/HERMES Branch Integration Readiness

This note records the current repository integration state for the requested HELIOS/HERMES specialist setup.

## Current Git inventory

- Current branch: `work`.
- Local branch inventory: only `work` is present in this checkout.
- Configured remotes: none. Without a remote, the repository cannot fetch, merge, or push `helios-control`, `hermes-fleet-production`, or any other branch refs.
- Requested focus branches: `helios-control` and `hermes-fleet-production` should be treated as priority merge targets once branch refs are available.

## Durable integration workflow

1. Run `git status --short` and `./helios.sh prune-generated` before staging durable work.
2. Configure remotes automatically when a URL is available: `./helios.sh specialist-check --auto-setup-remote --remote-url <url>`; then fetch all branch refs with `git fetch --all --prune`.
3. Inspect priority branches first: `git log --oneline --decorate --graph work..origin/helios-control` and `git log --oneline --decorate --graph work..origin/hermes-fleet-production`.
4. Merge branches one at a time with tests between merges. Prefer `--no-ff` for auditable integration commits when merging long-lived work streams.
5. Resolve conflicts by preserving durable source, scripts, config manifests, Bicep templates, tests, and durable docs only.
6. Exclude generated outputs: `reports/**` except `reports/README.md`, `status-site/index.html`, `status-site/actions.md`, `status-site/wiki-export/**`, and `.github/PULL_REQUEST_BODY.md`.

## Specialist setup coverage

Use the testable cross-platform checker `./helios.sh specialist-check` (or `python3 scripts/setup/setup_specialist_environment.py`) to validate the local specialist workstation for:

- Git branch/remote readiness for HELIOS/HERMES integration.
- Azure CLI sign-in and optional default subscription/location setup.
- .NET SDK support for C# and WinUI 3 projects.
- Python support for AIHub integration and analytics automation.
- PowerShell automation support.
- CMake availability for C++ performance backend work.

The checker is intentionally safe by default: it reports missing prerequisites and only runs Azure login, Azure default configuration, remote setup, or push when explicit switches are provided. For CI or Codespaces, set `HELIOS_REMOTE_URL` or rely on `GITHUB_REPOSITORY` inference, then run `./helios.sh specialist-check --auto-setup-remote --push-current`. A PowerShell wrapper with matching intent is also available at `scripts/setup/setup-specialist-environment.ps1` for Windows-oriented operators.
