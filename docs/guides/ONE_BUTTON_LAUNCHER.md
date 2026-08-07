# HELIOS one-button launcher

The HELIOS launcher packages the repository's existing safe automation entry
point as self-contained Windows and Linux executables. It is intended for local
setup, readiness checks, build/test orchestration, reports, and the local
dashboard. It does not bypass GitHub or Azure approval gates.

## Build the distributable package

From the repository root:

```bash
SKIP_FINISH=1 ./helios.sh package
```

`SKIP_FINISH=1` builds the launcher without first running the longer report and
readiness pipeline. Omit it when you want refreshed reports included in the
local run bundle.

The output path is printed at the end and recorded in the ignored local file
`.run/latest-helios-exe.txt`. The package includes:

- `win-x64/HELIOS.Platform.exe`
- `linux-x64/HELIOS.Platform`
- `run-helios.cmd`
- `run-helios.sh`
- platform archives and `SHA256SUMS`

Both executables are self-contained; the target does not need a separate .NET
runtime. The HELIOS checkout is required because the executable deliberately
runs the versioned scripts and configuration from that checkout.

## Start automatically

Run from anywhere inside the HELIOS checkout:

```bash
.run/<package>/run-helios.sh start
```

On Windows with Git for Windows, WSL, or another Bash installation:

```cmd
.run\<package>\run-helios.cmd start
```

When the package is outside the checkout, point it at the repository:

```bash
./run-helios.sh start --repo /path/to/helios-platform
```

The default `start` action bootstraps the approved local tools and runs the
safe, changed-project `quick` build-graph profile. It uses at most four workers
by default. Override the bounded worker count when needed:

```bash
./run-helios.sh start --max-workers 8
```

## Open the dashboard

```bash
./run-helios.sh dashboard
```

This runs setup and validation before serving the local dashboard. Press
`Ctrl+C` to stop it.

## Diagnose without setup

```bash
./run-helios.sh doctor
./run-helios.sh status
./run-helios.sh validate
```

Preview an action without executing it:

```bash
./run-helios.sh start --dry-run
```

Run the repository's safe read-only reporting pipeline:

```bash
./run-helios.sh all
```

## Safety and approvals

The launcher uses argument-list process execution and validates the repository
through the canonical integration-event schema. It never embeds credentials.
Authentication remains with the official GitHub and Azure CLI stores.

The launcher does **not** automatically:

- merge, rebase, reset, or delete branches;
- approve or merge pull requests;
- publish a GitHub release;
- change GitHub rules, environments, or secrets;
- change Azure resources, Entra/RBAC, Key Vault, firewall, Intune, or Purview;
- rotate secrets; or
- deploy to production.

Those operations require their existing review, protected-environment,
what-if, rollback, and Guardian gates.

## Troubleshooting

### Repository not found

Run inside the checkout or pass `--repo PATH`. A valid checkout must contain
`helios.sh` and `config/integrations/event-contract.schema.json`.

### Bash not found on Windows

Install or select Git for Windows, WSL, or another supported Bash environment.
The launcher reports this as a setup error rather than falling back to an
unreviewed shell implementation.

### A tool or authentication check fails

Run `doctor`. Follow the printed safe repair guidance. Sign in through the
official CLI browser flows and never copy resulting tokens into repository
files, issues, dashboards, or workflow logs.

### Setup is interrupted

Run `start` again. The underlying local bootstrap and build graph are designed
to be rerunnable. Generated output, caches, local databases, and authentication
state remain outside Git.
