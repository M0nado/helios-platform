# HELIOS AI CI Code Review and Old-Bug Autofix Workflow

This workflow setup gives HELIOS a controlled automation path for AI-assisted code
review, old bug triage, candidate fixes, verification, pull-request creation, and
optional GitHub auto-merge back into `main`.

## Workflows

| Workflow | Purpose | Safety gate |
| --- | --- | --- |
| `AI CI Code Review` | Builds an AI-review-ready report for pull requests, scans changed C#, WinUI/XAML, C++, F#, Python, PowerShell, and GitHub Actions files, and posts the result on the PR. | Read-only repository permissions except PR comments. |
| `AI Old Bug Autofix and Optimization` | Reviews old `bug`, `performance`, and `optimization` issues, then optionally runs a configured AI fixing command per issue. | Read-only by default; execution requires manual dispatch plus a configured secret command. |

## Old-bug fix and merge flow

1. The scheduled weekly run inventories old open issues and writes a backlog
   artifact without changing code.
2. An operator can manually dispatch the workflow with `execute=true` after
   configuring `HELIOS_AI_FIX_COMMAND` as a repository secret, or scheduled runs
   can execute without human input when `HELIOS_AI_AUTONOMOUS_ENABLED=true`.
3. For each selected issue, the runner creates an isolated branch named
   `ai/bugfix-<issue-number>-<slug>` from `main`.
4. The configured fix command receives issue context through environment variables:
   `HELIOS_ISSUE_NUMBER`, `HELIOS_ISSUE_TITLE`, `HELIOS_ISSUE_URL`,
   `HELIOS_ISSUE_BODY`, `HELIOS_BASE_BRANCH`, and `HELIOS_WORK_BRANCH`.
5. The workflow runs validation before committing and pushing the branch.
6. A pull request is opened against `main`.
7. If `enable_automerge=true` or `HELIOS_AI_AUTOMERGE_ENABLED=true` on a scheduled
   autonomous run, the workflow asks GitHub to auto-merge the PR after required
   branch protection checks pass. It does not bypass protected branches.

## Recommended repository settings

- Enable branch protection on `main` with required status checks from the multi-language CI gate.
- Enable GitHub auto-merge for the repository if automated merges are desired.
- Add required environment approvals for Azure deployment or privileged cloud changes.
- Store all provider credentials in GitHub Actions secrets or Azure Key Vault.
- Keep `HELIOS_AI_FIX_COMMAND` scoped to a trusted internal agent, for example a script
  that invokes an approved OpenAI/Codex integration and never prints secret values.

## Example fix command secret

`HELIOS_AI_FIX_COMMAND` can point at any trusted non-interactive command that edits the
working tree and exits non-zero when it cannot produce a safe fix. For example:

```bash
python3 scripts/automation/internal_ai_fix_agent.py
```

The committed runner deliberately does not hard-code a provider or API key. This keeps
secrets out of the repository and lets HELIOS swap between GitHub Copilot, OpenAI,
Azure OpenAI, or an internal Hermes/XCore specialist implementation.

## Optimization scope

The old-bug workflow includes `performance` and `optimization` labels in addition to
`bug`. That lets HELIOS schedule AI-assisted cleanup for C++ back-end hot paths,
F# analytics and prediction code, Python AIHub integration, WinUI front-end polish,
Azure automation, and GitHub workflow hardening through the same PR-gated path.


## Submodule consolidation handoff

Before issue fixing, the workflow now writes a submodule consolidation plan. The
standalone `AI Submodule Branch Consolidation` workflow uses the same policy file
at `config/automation/submodule-consolidation.json` to focus on one branch, merge
new work from configured external repositories into submodule branches, and report
cleanup/redundancy candidates.

## Workflow validation tooling

Use `scripts/automation/validate_workflows.sh` instead of calling `actionlint`
directly. The wrapper installs `actionlint` into `.tools/bin` with `GOBIN` when it
is missing, so validation no longer depends on whether Go places binaries in
`GOPATH/bin` or a separate configured `GOBIN`.
