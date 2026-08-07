# Scoped issue: approve pinned private-repository gitlinks

## Status

**APPROVED — scoped manifest is committed and validated.** The reviewed
40-character commit SHA for each declared submodule path is recorded in
`config/integrations/approved-submodules.json`.

## Exact proposed set

| Repository | Declared path | Reviewed commit |
| --- | --- | --- |
| `M0nado/helios-monado-blade` | `modules/helios-monado-blade` | `2bc1164fd22e0878e3166282c0470d25ebff6fc9` |
| `M0nado/helios-security-setup` | `modules/helios-security-setup` | `1e13db33c972d05170d85b2599d78b59eb7f35ea` |
| `M0nado/helios-ai-hub` | `modules/helios-ai-hub` | `30b1bc6956ffdc461d795df20f551eff5e3575b7` |
| `M0nado/helios-dev-ai-hub` | `modules/helios-dev-ai-hub` | `9e64eb55b1f4ada51c6f05b3fbeaa478f7a0841d` |
| `M0nado/helios-build-agents` | `modules/helios-build-agents` | `56ccd26a654a5dd852a0c26c235f61dfab256e8f` |
| `M0nado/helios-gui-framework` | `modules/helios-gui-framework` | `823c5fd7a331b436d6b5bbdd407fcabb2adc8a23` |
| `M0nado/helios-software-stack` | `modules/helios-software-stack` | `0b541a0997acff840d25216c3092cf6f5adc4995` |

This scope is derived only from `.gitmodules`. `Helios-Control-Center`,
`hermes-fleet-platforms`, the control-plane repository, and the canonical
platform itself are **not approved as submodules** because no declared paths or
reviewed commits were provided. Branch merging, Azure changes, deployment, and
polyglot optimization are explicitly out of scope until this gate passes.

## Approval record required

For each row, the reviewer must record the full SHA, evidence URL, ownership and
license decision, dependency/security review result, and contract/build/test
evidence. The manifest is schema-validated against
`config/integrations/approved-submodules.schema.json`; each `submodules[]` entry
must include `path`, `url`, `commit`, `evidenceUrl`, `ownershipDecision`,
`licenseDecision`, `dependencySecurityReview`, `contractEvidence`,
`buildEvidence`, and `testEvidence`. After approval, add the exact values to
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
performs the complete manifest, URL, SHA, indexed gitlink-set, clean worktree,
and recursive object-store integrity checks. Repository-specific ownership,
license, dependency, contract, security, build, and test evidence remains
mandatory in this issue before an approval label may be applied.

## Workflow enforcement

The `pinned-submodule-integrity` workflow runs on pull requests, pushes to
`main`, direct dispatch, and as a reusable workflow (`workflow_call`). Privileged
`workflow_dispatch` and scheduled workflows, including `azure-infra` and
`branch-absorption-multicloud`, now require this gate job before execution.

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
