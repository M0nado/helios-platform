# Governed GitHub Codex + Claude Code collaboration contract

## Purpose

This contract defines how Codex, Claude Code, and humans collaborate in
`M0nado/helios-platform` while GitHub remains the canonical execution and
approval surface.

## Roles and boundaries

- **Human reviewers/approvers** own final merge, deployment, RBAC, consent, and
  production approval.
- **Codex** performs scoped repository implementation, refactoring, and tests on
  issue-bound branches.
- **Claude Code** performs bounded analysis, alternative implementations, and
  review assistance in the same issue/branch scope.

Neither Codex nor Claude Code may bypass GitHub pull-request controls, protected
environments, or Azure approval gates.

## Required issue, branch, and PR evidence

1. Start from a scoped GitHub issue.
2. Use an issue-aligned branch and open a pull request.
3. Include a correlation ID and evidence links in the PR body.
4. Record Codex scope and Claude scope in the PR body.
5. Include rollback notes for privileged or high-risk changes.

The repository `.github/pull_request_template.md` is the canonical checklist.

## Proposal-only behavior for privileged operations

Privileged operations remain proposal-only unless a protected workflow plus
explicit human approval is used:

- production deployment/apply;
- tenant-wide consent or RBAC changes;
- secret rotation or direct secret writes;
- destructive Windows security/boot/network mutations.

Direct, unattended mutation from agent-authored pull requests is prohibited.

## Enforced workflow gates

- `.github/workflows/ai-collaboration-gate.yml` enforces PR body evidence fields
  and requires explicit proposal-only acknowledgement when privileged paths are
  touched.
- `.github/workflows/unified-agent-contract.yml` validates canonical ownership
  and integration-contract surfaces.
- Deployment/mutation workflows must remain protected-environment and approval
  gated.

## Secret and data handling

- Never commit credentials, tokens, recovery keys, tenant secrets, or private
  endpoint keys.
- Use managed identity, workload identity federation, and Key Vault references.
- Preserve correlation IDs and evidence links across GitHub, Azure, Teams, and
  SharePoint artifacts.
- Keep generated logs/caches and raw model artifacts out of Git.
