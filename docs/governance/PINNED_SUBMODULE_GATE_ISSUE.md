# Scoped issue: approve pinned private-repository gitlinks

## Status

**BLOCKED — approval has not been granted.** A reviewer must supply and approve a
40-character commit SHA for every repository below. Branch tips, abbreviated
SHAs, and SHAs discovered by automation are not acceptable substitutes for
reviewed commits.

## Exact proposed set

| Repository | Declared path | Reviewed commit |
| --- | --- | --- |
| `M0nado/helios-monado-blade` | `modules/helios-monado-blade` | **Required** |
| `M0nado/helios-security-setup` | `modules/helios-security-setup` | **Required** |
| `M0nado/helios-ai-hub` | `modules/helios-ai-hub` | **Required** |
| `M0nado/helios-dev-ai-hub` | `modules/helios-dev-ai-hub` | **Required** |
| `M0nado/helios-build-agents` | `modules/helios-build-agents` | **Required** |
| `M0nado/helios-gui-framework` | `modules/helios-gui-framework` | **Required** |
| `M0nado/helios-software-stack` | `modules/helios-software-stack` | **Required** |

This scope is derived only from `.gitmodules`. `Helios-Control-Center`,
`hermes-fleet-platforms`, the control-plane repository, and the canonical
platform itself are **not approved as submodules** because no declared paths or
reviewed commits were provided. Branch merging, Azure changes, deployment, and
polyglot optimization are explicitly out of scope until this gate passes.

## Approval record required

For each row, the reviewer must record the full SHA, evidence URL, ownership and
license decision, dependency/security review result, and contract/build/test
evidence. After approval, add the exact values to
`config/integrations/approved-submodules.json`, initialize each declared path,
check out the approved commit in detached-HEAD state, and stage the resulting
mode-`160000` gitlink.

Approval is valid only when all of these commands succeed:

```bash
git submodule sync --recursive
git submodule update --init --recursive
git submodule status --recursive
git ls-files --stage
git diff --cached --submodule=log --check
python3 scripts/integrations/validate_pinned_submodules.py
```

The first five lines are the requested Git-command gate. The Python validator
performs the complete manifest, URL, SHA, gitlink, worktree, and recursive
integrity checks. Repository-specific ownership, license, dependency, contract,
security, build, and test evidence remains mandatory in this issue before an
approval label may be applied.

## Least-privilege GitHub App

Create a development-only GitHub App installed **only** on the seven repositories
in the table. Grant repository permission `Contents: Read-only`; grant no
organization, administration, issues, pull-request, workflow, deployment,
secrets, or metadata-write permissions. Store the App ID as the repository
variable `SUBMODULE_APP_ID` and its private key as the environment secret
`SUBMODULE_APP_PRIVATE_KEY` in the protected `development` environment. The CI
workflow exchanges that key for a short-lived installation token and checkout
uses `persist-credentials: false`; no installation token or private key is
written to the repository.

## Rollback

Remove the staged gitlinks and manifest, restore `.gitmodules`, and rerun the
validator. App installation removal and secret deletion are external,
reviewer-approved operations.
