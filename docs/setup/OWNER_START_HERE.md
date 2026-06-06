* `Heli0s-dynamics` = enterprise umbrella
* `M0nado` = business/product owner
* `helios-platform` = official source-of-truth repo
* `helios-control` = C# / WinUI / WPF control center
* `hermes-fleet-production` = Hermes fleet production automation

# Owner Start Here

This is the owner-friendly, click-by-click setup guide for making `helios-platform` the official source of truth and then safely connecting the related `helios-control` and `hermes-fleet-production` repositories.

Use it when the repositories are under a personal GitHub user, a business organization, or an enterprise account. If a button or tab is missing, it usually means you are not signed in as an owner, the repository is under a different account, or the feature is controlled by an enterprise policy.

## Quick map: where am I supposed to click?

| If the repo is under... | Example URL | Start here | What it means |
| --- | --- | --- | --- |
| Personal user | `github.com/M0nado/helios-platform` | Click your avatar → **Your repositories** → `helios-platform` → **Settings** | The user account owns settings directly. Use collaborators carefully because there are no organization teams. |
| Business / organization | `github.com/Heli0s-dynamics/helios-platform` | Click your avatar → **Your organizations** → `Heli0s-dynamics` → repository → **Settings** | The organization owns access, teams, billing, security defaults, and many policies. This is the preferred long-term home. |
| Enterprise | `github.com/enterprises/<enterprise-name>` plus an org/repo | Click your avatar → **Your enterprises** → enterprise → **Policies**, then go to the organization and repo settings | Enterprise policies can override organization and repository settings. Check enterprise policy first if a setting is locked. |
| Different owner / vendor repo | Any URL that is not controlled by `M0nado` or `Heli0s-dynamics` | Open the repo → check whether **Settings** is visible → ask the listed owner for transfer/admin access | Do not merge production automation until ownership, visibility, and branch rules are documented. |

## Owner fast path

1. Open `https://github.com/M0nado/helios-platform` or the current canonical repository URL.
2. Confirm the repository owner shown before the slash: `M0nado`, `Heli0s-dynamics`, or another organization.
3. Click **Settings**. If **Settings** is missing, stop and ask the current owner for `Admin` access or a repository transfer.
4. Complete sections 1 through 10 below in order.
5. Open or create matching records for `helios-control` and `hermes-fleet-production` only after `helios-platform` is protected.
6. Put every decision into an issue, milestone, or project item so the setup is auditable.

## Owner workstation preflight

Run these commands only after installing GitHub CLI and Azure CLI. They do not change the repository by themselves; they confirm which account and subscription you are using.

```powershell
# GitHub: confirms the signed-in GitHub account and token scopes
gh auth status

# Azure: confirms the signed-in tenant and subscription
az account show

# Azure: select the intended subscription before any deployment setup
az account set --subscription "<subscription-id-or-name>"
```

If `az account show` fails, open a terminal and run:

```powershell
az login
az account list --output table
az account set --subscription "<subscription-id-or-name>"
```

## If repositories are split across user, business, and enterprise accounts

Use this decision tree before changing settings:

1. **Find the source-of-truth owner.** The intended owner is the account that should own production history, branch protection, Actions permissions, releases, and environment secrets.
2. **Prefer organization ownership for production.** If `helios-platform` is under a personal user but production work belongs to `Heli0s-dynamics`, plan a transfer to the organization before connecting production secrets.
3. **Do not copy secrets between accounts.** Re-create secrets in the destination repository or environment after transfer.
4. **If `helios-control` is elsewhere**, document whether it is a downstream UI/control-center repo, a fork, or a separate product repo. It should not override `helios-platform` governance.
5. **If `hermes-fleet-production` is elsewhere**, treat it as production automation. Require private/internal visibility, branch protection, reviewed deployments, and restricted environments before connecting it.
6. **If an enterprise policy blocks a setting**, go to enterprise settings first, then organization settings, then repository settings.

## 1. Repository access

Goal: confirm who can read, triage, write, maintain, administer, and deploy from the source-of-truth repository.

### Click-by-click

1. Open the repository.
2. Click **Settings** in the repository navigation bar.
3. In the left sidebar, click **Collaborators and teams** or **Manage access**.
4. Review every person, team, bot, app, and deploy key.
5. Remove unknown users and stale service accounts.
6. Add access using least privilege:
   - **Read** for observers and auditors.
   - **Triage** for issue/project managers.
   - **Write** for normal contributors.
   - **Maintain** for release operators who manage settings but should not own everything.
   - **Admin** only for trusted owners who can change security, delete the repo, or transfer ownership.
7. For organizations, prefer adding teams instead of individual users.
8. Save or confirm changes.

### If the repo is under a personal user

1. Click **Settings** → **Collaborators**.
2. Invite individual users only when necessary.
3. If several people need access, move the repo to an organization so teams can be used.

### If the repo is under an organization

1. Click **Settings** → **Collaborators and teams**.
2. Add teams such as:
   - `platform-engineering`
   - `control-center-ui`
   - `hermes-fleet-ops`
   - `security-reviewers`
   - `release-managers`
3. Confirm each team has the minimum role needed.

### Done when

- Unknown users are removed.
- Admin access is limited.
- Teams or collaborators are documented.
- Service accounts have an owner, purpose, expiry, and rotation plan.

## 2. Organization ownership

Goal: make sure `Heli0s-dynamics` and `M0nado` have clear owner responsibility and recovery paths.

### Click-by-click for an organization

1. Click your avatar in the top-right corner.
2. Click **Your organizations**.
3. Click **Settings** next to `Heli0s-dynamics`.
4. In the left sidebar, review:
   - **People** for members and owners.
   - **Teams** for team structure.
   - **Member privileges** for base permissions.
   - **Authentication security** for MFA/security requirements.
   - **Billing and plans** for ownership and payment.
   - **Third-party access** / **GitHub Apps** for integrations.
5. Make sure at least two trusted human owners exist.
6. Set organization base permissions to the lowest practical level, commonly **No permission** or **Read**.
7. Document emergency contacts and recovery steps outside the repo.

### Click-by-click for enterprise-controlled orgs

1. Click your avatar.
2. Click **Your enterprises**.
3. Select the enterprise.
4. Review **Policies** and **Organizations**.
5. If repository settings are locked, check the enterprise policy before changing the organization or repository.

### Done when

- At least two human owners are assigned.
- Billing, security, app management, and recovery responsibilities are known.
- Enterprise policy ownership is known if applicable.

## 3. Repository visibility

Goal: decide whether each repository should be public, private, or internal.

### Click-by-click

1. Open the repository.
2. Click **Settings**.
3. Click **General** in the left sidebar.
4. Scroll to **Danger Zone**.
5. Find **Change repository visibility**.
6. Do not click it until the owner decision is documented.

### Recommended visibility

| Repository | Recommended starting visibility | Why |
| --- | --- | --- |
| `helios-platform` | Private or internal until scrubbed | It is the official source of truth and may contain deployment, Azure, or automation assumptions. |
| `helios-control` | Private or internal | C# / WinUI / WPF control-center UI may expose operational workflows. |
| `hermes-fleet-production` | Private or internal | Fleet production automation should be protected by default. |

### If changing visibility

1. Create an issue titled `Owner decision: repository visibility`.
2. Record current visibility, requested visibility, reason, owner approval, risk, and rollback plan.
3. Run secret scanning and remove sensitive data before making anything public.
4. Confirm Pages, packages, forks, Actions, and environments still behave as expected after the change.

### Done when

- Visibility is documented for every related repository.
- Secret scanning and push protection are enabled where available.
- Public exposure has a written approval and cleanup checklist.

## 4. Branch protection

Goal: prevent accidental direct changes to the source-of-truth branch.

### Click-by-click using rulesets (preferred when available)

1. Open the repository.
2. Click **Settings**.
3. In the left sidebar, click **Rules** → **Rulesets**.
4. Click **New ruleset** → **New branch ruleset**.
5. Name it `protect-main`.
6. Set enforcement to **Active**.
7. Add target branch pattern `main`.
8. Enable rules:
   - Require a pull request before merging.
   - Require approvals.
   - Require conversation resolution.
   - Require status checks to pass.
   - Block force pushes.
   - Block deletions.
   - Require linear history if the team uses squash/rebase merges.
9. Add bypass only for emergency owners or release automation that has a documented reason.
10. Click **Create** or **Save changes**.

### Click-by-click using classic branch protection

1. Open the repository.
2. Click **Settings**.
3. Click **Branches**.
4. Under **Branch protection rules**, click **Add rule**.
5. Type `main` in **Branch name pattern**.
6. Select the same protections listed above.
7. Click **Create** or **Save changes**.

### If merging many branches

1. Do not merge everything directly into `main`.
2. Create an integration branch such as `integration/owner-baseline`.
3. Merge one branch at a time.
4. Run build, test, security, and documentation checks after each merge.
5. Use the integration PR template and record rollback notes.

### Done when

- `main` cannot be pushed to directly by normal contributors.
- Pull requests require review and passing checks.
- Force pushes and deletions are blocked.

## 5. GitHub Actions permissions

Goal: make automation useful without giving every workflow broad write access.

### Click-by-click for repository Actions settings

1. Open the repository.
2. Click **Settings**.
3. In the left sidebar, click **Actions** → **General**.
4. Under **Actions permissions**, choose the most restrictive option that still allows approved workflows.
5. If third-party actions are allowed, prefer pinned versions or pinned SHAs for security-sensitive workflows.
6. Scroll to **Workflow permissions**.
7. Select **Read repository contents and packages permissions** as the default.
8. Turn off **Allow GitHub Actions to create and approve pull requests** unless a reviewed automation workflow requires it.
9. Click **Save**.

### Click-by-click for organization defaults

1. Click your avatar → **Your organizations**.
2. Click **Settings** next to `Heli0s-dynamics`.
3. Click **Actions** → **General**.
4. Set organization defaults first, then revisit repository settings.
5. If a repository option is disabled, the organization or enterprise probably controls it.

### Done when

- Default `GITHUB_TOKEN` permissions are read-only.
- Write permissions are declared inside individual workflow files only when needed.
- Fork PR workflow approval behavior is understood.
- Azure deployments use environments and preferably OIDC instead of long-lived secrets.

## 6. Environments

Goal: create safe gates for deployments, Azure resources, control-center releases, and Hermes fleet automation.

### Recommended baseline

| Environment | Purpose | Required protection |
| --- | --- | --- |
| `development` | Safe integration and preview deployments | Optional reviewer, short-lived secrets |
| `staging` | Release-candidate validation | Required reviewers and branch restrictions |
| `production` | Customer, fleet, or enterprise automation | Required reviewers, restricted branches, secret rotation plan |

### Click-by-click

1. Open the repository.
2. Click **Settings**.
3. In the left sidebar, click **Environments**.
4. Click **New environment**.
5. Enter `development`, then click **Configure environment**.
6. Add environment variables and secrets only for development.
7. Repeat for `staging` and `production`.
8. For `staging` and `production`, add **Required reviewers**.
9. Add deployment branch restrictions so only `main`, `release/*`, or approved deployment branches can deploy.
10. Save each environment.

### Azure CLI and Azure setup notes

1. Use local Azure CLI to confirm tenant/subscription only.
2. For repeatable CI/CD, prefer GitHub Actions OpenID Connect federation.
3. Create separate Azure resource groups for development, staging, and production.
4. Store Azure IDs as GitHub environment variables:
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`
   - `AZURE_CLIENT_ID`
   - `AZURE_RESOURCE_GROUP`
5. Store secrets only when OIDC is not available, and rotate them.

### Done when

- `development`, `staging`, and `production` exist.
- Production requires human approval.
- Secrets are scoped to environments, not shared globally by default.

## 7. Pages

Goal: decide whether GitHub Pages publishes owner-facing setup notes, dashboards, or generated docs.

### Click-by-click

1. Open the repository.
2. Click **Settings**.
3. In the left sidebar, click **Pages**.
4. Under **Build and deployment**, choose the publishing source:
   - **GitHub Actions** for generated docs and dashboards.
   - A branch/folder only if the team intentionally maintains static files that way.
5. Confirm the published URL.
6. Open the URL in a private/incognito window and verify no secrets, internal endpoints, customer identifiers, or tokens are visible.
7. Add a link back to `docs/setup/OWNER_START_HERE.md` or publish an owner checklist summary.

### Done when

- Pages is enabled or intentionally disabled.
- Published content is safe for the selected repository visibility.
- The owner setup guide is discoverable from the Pages dashboard.

## 8. Wiki

Goal: decide whether the GitHub Wiki is official documentation or just a scratch/reference area.

### Click-by-click

1. Open the repository.
2. Click **Settings**.
3. Click **General**.
4. Scroll to **Features**.
5. Check or uncheck **Wikis**.
6. If enabled, click the repository **Wiki** tab and create a landing page pointing back to versioned docs.

### Recommendation

- Use `docs/` as the source of truth for architecture, setup, release, security, and owner procedures.
- Use Wiki only for non-release notes, meeting summaries, or temporary references.
- Move anything official from Wiki into `docs/` through a pull request.

### Done when

- Wiki is enabled or disabled intentionally.
- Contributors know whether Wiki content is official.

## 9. Projects

Goal: make the owner setup and cross-repo work visible.

### Click-by-click for an organization project

1. Click your avatar → **Your organizations**.
2. Open `Heli0s-dynamics`.
3. Click **Projects**.
4. Click **New project**.
5. Choose a table or board layout.
6. Name it `Helios Owner Readiness`.
7. Add fields:
   - `Priority`
   - `Phase`
   - `Repository`
   - `Language stack`
   - `Risk`
   - `Owner`
   - `Target milestone`
   - `Environment`
8. Add views:
   - Owner baseline
   - GitHub security
   - Azure foundation
   - Control center readiness
   - Hermes fleet readiness
   - Release readiness

### Click-by-click for a repository project

1. Open the repository.
2. Click **Projects**.
3. Click **New project** or **Link a project**.
4. Link issues and pull requests to the project before merging.

### Done when

- The owner baseline is tracked in a project.
- `helios-platform`, `helios-control`, and `hermes-fleet-production` work can be filtered separately.

## 10. Labels and milestones

Goal: make triage consistent across source-of-truth, control-center, and fleet-production work.

### Click-by-click for labels

1. Open the repository.
2. Click **Issues**.
3. Click **Labels**.
4. Click **New label**.
5. Add the label name, description, and color.
6. Repeat for the baseline labels below.

Suggested label groups:

- Areas: `area:github`, `area:azure`, `area:security`, `area:docs`, `area:control-center`, `area:hermes-fleet`, `area:aihub`
- Stacks: `stack:csharp`, `stack:winui`, `stack:wpf`, `stack:cpp`, `stack:fsharp`, `stack:python`
- Types: `type:bug`, `type:feature`, `type:task`, `type:release`, `type:owner-decision`
- Priorities: `priority:p0`, `priority:p1`, `priority:p2`, `priority:p3`
- Status: `status:blocked`, `status:needs-owner`, `status:ready`, `status:in-review`

### Click-by-click for milestones

1. Open the repository.
2. Click **Issues**.
3. Click **Milestones**.
4. Click **New milestone**.
5. Add a title, due date if known, and description.
6. Create these first milestones:
   - `owner-baseline`
   - `azure-foundation`
   - `control-center-readiness`
   - `hermes-fleet-readiness`

### Done when

- Issues and PRs can be filtered by area, stack, type, priority, and status.
- Owner decisions are tracked in milestones.

## Final completion checklist

- [ ] Repository account location identified: personal, organization, or enterprise.
- [ ] Repository access reviewed and least privilege applied.
- [ ] Organization or enterprise owner and recovery model documented.
- [ ] Repository visibility confirmed for `helios-platform`, `helios-control`, and `hermes-fleet-production`.
- [ ] Branch protection or rulesets enabled for source-of-truth branches.
- [ ] GitHub Actions permissions reduced to least privilege.
- [ ] Environments created with required reviewers and scoped secrets.
- [ ] Azure CLI account and subscription verified.
- [ ] Pages setting documented and owner guide made discoverable.
- [ ] Wiki setting documented.
- [ ] Project board created or linked.
- [ ] Labels and milestones standardized.
- [ ] Integration merge process documented before combining branches.
