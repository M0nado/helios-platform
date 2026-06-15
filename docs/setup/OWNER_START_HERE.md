# Owner start here: canonical governance baseline

- **Canonical source:** Treat `helios-platform` as the owner-approved source of truth before downstream changes land in `helios-control` or `hermes-fleet-production`.
- **Owner-first rule:** Repository owners verify access, governance, and release controls before delegating implementation work or accepting integration changes.
- **Downstream gate:** Do not merge synchronization, deployment, or production-fleet changes downstream until this checklist is complete and linked in the pull request.
- **Evidence required:** Capture command output, screenshots, or GitHub settings links for every owner-controlled setting changed.
- **Least privilege:** Prefer teams, protected environments, scoped tokens, and read-only defaults over direct user grants or broad automation permissions.

Use this guide as the first owner review for this repository and any related project that consumes it. Keep the baseline small, auditable, and repeatable.

## Owner preflight snippet

Run these checks from an authenticated shell before changing repository settings. Replace placeholders with the owner organization, Azure subscription, and resource group used for HELIOS governance.

```bash
OWNER="<github-org-or-owner>"
REPO="helios-platform"
AZ_SUBSCRIPTION="<subscription-id-or-name>"
AZ_RESOURCE_GROUP="<governance-resource-group>"

# GitHub identity, repository visibility, default branch, Pages, and Actions defaults.
gh auth status
gh repo view "$OWNER/$REPO" --json nameWithOwner,visibility,defaultBranchRef,isPrivate,viewerPermission

gh api "repos/$OWNER/$REPO/actions/permissions" \
  --jq '{enabled, allowed_actions, default_workflow_permissions, can_approve_pull_request_reviews}'

gh api "repos/$OWNER/$REPO/branches/main/protection" \
  --jq '{required_status_checks, enforce_admins, required_pull_request_reviews, restrictions}'

# Azure identity and resource ownership baseline.
az account show --query '{name:name, id:id, tenantId:tenantId, user:user.name}' -o table
az role assignment list --assignee "$(az ad signed-in-user show --query id -o tsv)" \
  --scope "/subscriptions/$AZ_SUBSCRIPTION/resourceGroups/$AZ_RESOURCE_GROUP" \
  --query '[].{role:roleDefinitionName, scope:scope}' -o table
```

## 1. Repository access

- Confirm every human contributor is assigned through a GitHub team; avoid direct collaborator grants except break-glass owners.
- Keep owners/admins limited to accountable maintainers who can respond to security and production incidents.
- Require MFA for all organization members and verify service accounts are documented with purpose, owner, rotation date, and token scopes.
- Review deploy keys, fine-grained personal access tokens, GitHub Apps, Codespaces secrets, and repository secrets quarterly.

## 2. Organization ownership

- Name the owning organization/team for `helios-platform`, then map downstream owners for `helios-control` and `hermes-fleet-production`.
- Record who can approve repository settings, release promotions, environment deployments, and emergency bypasses.
- Keep billing, package publishing, Azure subscription access, and GitHub Enterprise settings under the same accountable owner group.
- Require downstream projects to cite this guide when requesting governance exceptions.

## 3. Repository visibility

- Set repository visibility intentionally and document the reason in the owner notes or governance issue.
- If public, confirm secret scanning, push protection, Dependabot alerts, code scanning, and release artifact review are enabled.
- If private/internal, verify license, package feeds, Pages access, and downstream automation still work for approved consumers.
- Re-check visibility before mirroring or syncing content to `helios-control` or `hermes-fleet-production`.

## 4. Branch protection

- Protect `main` as the canonical integration branch with pull requests required before merge.
- Require current status checks for build, test, security, and documentation validation jobs that gate downstream propagation.
- Require at least one code owner or owner-team approval for governance, deployment, workflow, and security-sensitive paths.
- Disable force pushes and deletions; allow bypasses only for named owners with documented incident references.
- Require signed commits or verified provenance where supported by contributor tooling.

## 5. GitHub Actions permissions

- Default workflow token permissions to read-only and elevate per workflow/job only when writes are required.
- Restrict allowed actions to GitHub-owned, verified, or explicitly approved actions; pin third-party actions to immutable SHAs where practical.
- Require owner review for workflow changes because workflows can alter deployment, package, and repository state.
- Separate CI validation from deployment workflows, and use environments for any job that reaches Azure, packages, Pages, or production fleets.

## 6. Environments

- Create environments for `development`, `staging`, and `production` with explicit reviewers and wait timers where appropriate.
- Store environment secrets at the narrowest environment scope; avoid organization-wide secrets for production credentials.
- Require production deployments to identify the upstream `helios-platform` commit and downstream target repository.
- Audit environment reviewers and deployment history after each production release or fleet sync.

## 7. Pages

- Confirm GitHub Pages is enabled only for intended documentation, metrics, and dashboards.
- Publish from a protected branch or workflow with read-only source generation and reviewed write permissions.
- Ensure Pages content links back to this guide so reviewers can find the governance baseline from published documentation.
- Do not expose secrets, private endpoint names, sensitive fleet topology, or customer data in generated Pages artifacts.

## 8. Wiki

- Decide whether the Wiki is enabled. If enabled, assign owners and mirror critical operational content back into versioned repository docs.
- Treat wiki pages as supplemental notes, not as the authoritative source for governance, deployment, or security controls.
- Disable wiki edits for broad audiences if the content could diverge from owner-reviewed repository documentation.

## 9. Projects

- Use GitHub Projects to track owner setup, downstream readiness, and exceptions before production rollout.
- Include fields for source repository, downstream target, owner approver, risk, validation evidence, and release milestone.
- Keep automation read-scoped unless it must update project metadata, and document any write token used by project automation.

## 10. Labels and milestones

- Create labels for `governance`, `owner-review`, `integration`, `downstream-sync`, `security`, `azure`, `actions`, and `production-fleet`.
- Use milestones to group owner baseline completion, control-plane integration, and fleet-production rollout.
- Require pull requests that modify downstream synchronization or deployment behavior to include the `owner-review` and `integration` labels.
- Close the milestone only after access, branch protection, Actions, environments, Pages, Wiki, Projects, labels, and release evidence are reviewed.

## Integration owner checklist

- [ ] `helios-platform` remains the source of truth for the proposed change.
- [ ] Downstream impact on `helios-control` and `hermes-fleet-production` is described.
- [ ] Owners reviewed access, visibility, branch protection, Actions permissions, and environments.
- [ ] Pages/Wiki/Projects/labels/milestones are either updated or marked not applicable.
- [ ] Validation evidence is attached to the issue, project item, or pull request.
