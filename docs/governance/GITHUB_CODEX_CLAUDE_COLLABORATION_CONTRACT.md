# Governed GitHub Codex + Claude Code Collaboration Contract

## 1. Purpose

This contract defines how Codex, Claude Code, and human reviewers collaborate in
`M0nado/helios-platform` while keeping GitHub as the canonical execution and
approval surface.

Scope:

- repository planning, implementation, and review workflows;
- issue, branch, and pull request traceability standards;
- proposal-only handling for privileged operations;
- required evidence, approval, and security boundaries.

## 2. Role and decision boundaries

| Actor | Allowed actions | Not allowed |
| --- | --- | --- |
| Codex | Propose code/docs/workflow changes, run tests, draft review notes, prepare PR metadata | Merge PRs, deploy production, rotate secrets, change tenant/RBAC, bypass required checks |
| Claude Code | Propose code/docs/workflow changes, perform peer review, suggest refactors/tests | Merge PRs, deploy production, rotate secrets, change tenant/RBAC, bypass required checks |
| Human reviewer/maintainer | Approve/reject plans and PRs, request revisions, merge after checks pass, approve protected deployments | Delegating final approval to AI output without review evidence |

Human approval is mandatory for merge and any privileged operation promotion.

## 3. Naming and traceability standards

### 3.1 Issue linkage

- Every implementation PR must reference a scoped issue.
- Use `Fixes #<issue-number>` for completing work or `Relates to #<issue-number>`
  for partial work.

### 3.2 Branch naming

- Preferred pattern: `issue-<issue-number>-<short-kebab-scope>`.
- Worktree-generated owner-prefixed variant is also accepted:
  `<owner>-issue-<issue-number>-<short-kebab-scope>`.

### 3.3 PR title

- Preferred pattern: `<type>(<scope>): <summary> | Fixes #<issue-number>`.
- Allowed `type` values: `feat`, `fix`, `docs`, `refactor`, `perf`, `test`,
  `ci`, `chore`.

### 3.4 Correlation ID

- Required for every PR using this contract.
- Format: `hc-<issue-number>-<kebab-scope>`.
- Example: `hc-231-governed-ai-collab-contract`.

### 3.5 Evidence links

Each PR must include links to evidence, such as:

- relevant workflow runs;
- test output artifacts;
- contract-validation artifacts;
- privileged what-if/dry-run artifacts when applicable.

## 4. Proposal-only boundary for privileged operations

Privileged operations include (non-exhaustive):

- disk/BitLocker/WDAC/firewall enforcement changes;
- tenant, Entra, RBAC, and consent changes;
- production deployment or secret rotation changes;
- workflows/scripts that introduce or enable direct apply paths.

For privileged scope changes, Codex and Claude Code are restricted to
proposal-only behavior:

1. Produce a deterministic plan.
2. Include what-if or dry-run evidence.
3. Provide rollback notes.
4. Link the required approval gate issue/work item.
5. Do not execute or auto-enable direct apply behavior.

## 5. Required gates for AI-assisted pull requests

AI-assisted PRs must pass all of the following:

1. `ai-collaboration-governance` workflow validation.
2. `HELIOS Unified Agent Contract` validation for governance surfaces.
3. Required branch protection checks on the target branch.
4. At least one human code review approval before merge.

If privileged paths are touched, the PR must also include explicit
proposal-only declarations, rollback notes, and approval-gate linkage.

## 6. Secret handling and prohibited data flows

Never place credentials or sensitive material in source, PR text, workflow
logs, or generated artifacts. This includes API keys, tenant secrets, private
endpoint credentials, recovery keys, and tokens.

Required secret handling:

- Azure Key Vault, protected GitHub environments, workload identity federation,
  managed identity, DPAPI, or Windows Credential Manager.

Prohibited data flows:

- copying secrets into issues, PR comments, chat transcripts, or committed docs;
- using raw conversation data as training payloads;
- bypassing GitHub/Azure approval paths through direct local mutation.

## 7. References

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `config/integrations/event-contract.schema.json`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`
