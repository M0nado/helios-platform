# Updating the PR and Integration Reports

Use this workflow to refresh all reports and prepare a PR body from the current command-center state.

```bash
./helios.sh all
./helios.sh pr-update --dry-run
```

When GitHub CLI is installed and authenticated, update the current PR body:

```bash
./helios.sh pr-update --apply
```

The generated body is written to `.github/PULL_REQUEST_BODY.md` and includes readiness, cross-access profiles, merge/prune recommendations, and hybrid gap analysis.

Safety: this updates PR text only when `--apply` is passed. It does not mutate repo/org/enterprise/Azure/provider settings.
