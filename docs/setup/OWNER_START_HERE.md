* `Heli0s-dynamics` = enterprise umbrella
* `M0nado` = business/product owner
* `helios-platform` = official source-of-truth repo
* `helios-control` = C# / WinUI / WPF control center
* `hermes-fleet-production` = Hermes fleet production automation

# Owner Start Here

Use this guide as the first owner checklist for bringing GitHub, Azure, and repository governance into a controlled baseline. Treat `helios-platform` as the canonical repository before changing downstream automation in `helios-control` or `hermes-fleet-production`.

## Before you begin

- Sign in as the `M0nado` owner account or an explicitly delegated organization owner.
- Keep one browser session open to GitHub organization settings and one terminal session authenticated with GitHub CLI and Azure CLI.
- Prefer changes through pull requests unless the action is an organization-level setting that cannot be represented as code.
- Record every setting change in the related issue, project item, or milestone so operations can audit the setup later.

```powershell
# Optional owner workstation preflight
gh auth status
az account show
az account set --subscription "<subscription-id-or-name>"
```

## 1. Repository access

First step: confirm who can read, triage, maintain, administer, and deploy from `M0nado/helios-platform`.

- Go to **Repository settings > Collaborators and teams**.
- Grant least-privilege access: `Read` for observers, `Triage` for issue managers, `Write` for contributors, `Maintain` for release operators, and `Admin` only for trusted owners.
- Create or reuse teams for platform engineering, control-center UI, Hermes fleet automation, security, and release operations.
- Require every human owner to use MFA before granting write or admin access.
- Confirm service accounts and deploy keys are named by purpose, owner, expiry, and rotation date.

## 2. Organization ownership

First step: verify the organization-level owner chain for `Heli0s-dynamics` and the business-owner accountability for `M0nado`.

- Keep at least two human organization owners to avoid lockout.
- Assign billing, security manager, and app manager roles separately when possible.
- Review installed GitHub Apps, OAuth Apps, fine-grained PATs, deploy keys, and organization secrets.
- Document emergency contacts, recovery methods, domain verification, and billing ownership outside the repo.
- Confirm whether `helios-control` and `hermes-fleet-production` should live in the same organization, separate private repos, or external vendor-owned repositories.

## 3. Repository visibility

First step: decide whether `helios-platform` is public, private, or internal and document the reason.

- Use **private** for code that contains proprietary automation, security-sensitive deployment logic, internal fleet operations, or unreleased product plans.
- Use **public** only after secrets, customer data, proprietary infrastructure names, and private package endpoints are removed.
- Re-check visibility for `helios-control` and `hermes-fleet-production`; UI control centers and fleet automation commonly require stricter defaults.
- Enable secret scanning, push protection, dependency graph, Dependabot alerts, and code scanning before broadening visibility.
- If visibility changes, announce the exact date, owner, reason, and rollback plan in an issue.

## 4. Branch protection

First step: protect the source-of-truth branch before merging integration work.

- Protect `main` and any release branches.
- Require pull requests before merging.
- Require at least one approving review; use code owners for security, Azure, C#, C++, F#, Python, and documentation areas when ownership is defined.
- Require status checks for build, test, lint, security scan, and documentation validation workflows that are stable in this repository.
- Require conversation resolution, linear history, signed commits or vigilant mode if the organization mandates them, and administrator enforcement for production branches.
- Do not combine unrelated branch histories until ownership, test status, and rollback expectations are clear.

## 5. GitHub Actions permissions

First step: set the default workflow token permission to least privilege.

- Go to **Organization settings > Actions > General** and **Repository settings > Actions > General**.
- Set default `GITHUB_TOKEN` permissions to **Read repository contents**.
- Allow write permissions only in workflows that explicitly need releases, Pages publishing, packages, deployments, or issue comments.
- Restrict third-party actions to verified publishers or pinned SHAs for security-sensitive workflows.
- Require approval for workflows from forked pull requests.
- Store Azure publish credentials in environments or OpenID Connect federation rather than long-lived secrets whenever possible.

## 6. Environments

First step: create deployment environments before connecting Azure or fleet automation.

Recommended baseline:

| Environment | Purpose | Required protection |
| --- | --- | --- |
| `development` | Safe integration and preview deployments | Optional reviewer, short-lived secrets |
| `staging` | Release-candidate validation | Required reviewers and branch restrictions |
| `production` | Customer, fleet, or enterprise automation | Required reviewers, restricted branches, secret rotation plan |

- Store environment-specific secrets and variables under the matching GitHub environment.
- Use Azure CLI locally only for owner setup and verification; prefer GitHub Actions OIDC for repeatable deployments.
- Add `helios-control` and `hermes-fleet-production` deployment dependencies only after their repositories, branches, and environments are protected.

## 7. Pages

First step: decide whether GitHub Pages should publish repository documentation, dashboards, or owner-facing setup notes.

- Go to **Repository settings > Pages**.
- Select the intended source, usually a GitHub Actions Pages workflow for generated documentation.
- Keep operational dashboards free of secrets, internal endpoint names, customer identifiers, and live tokens.
- Link Pages output back to this owner guide and the canonical README.
- If Pages is disabled, record the reason and the replacement documentation location.

## 8. Wiki

First step: decide whether the GitHub Wiki is authoritative or supplemental.

- Prefer versioned Markdown in `docs/` for source-of-truth setup, architecture, and release documentation.
- Use Wiki pages only for high-level notes, meeting summaries, and non-release operational references.
- If enabled, assign maintainers, review cadence, and migration rules for content that becomes official.
- If disabled, point contributors to `docs/`, `.github/`, and project boards.

## 9. Projects

First step: create a GitHub Project that maps owner work to repository readiness.

- Create views for owner setup, security hardening, Azure integration, control-center UI, Hermes automation, and release readiness.
- Add fields for priority, phase, repository, language stack, risk, owner, target milestone, and environment.
- Track cross-repo work explicitly: `helios-platform` for governance/source of truth, `helios-control` for C# / WinUI / WPF user operations, and `hermes-fleet-production` for fleet production automation.
- Connect issues and pull requests to project items so branch protection and release approvals have visible context.

## 10. Labels and milestones

First step: standardize labels and milestones before triaging incoming work.

Suggested label groups:

- `area:github`, `area:azure`, `area:security`, `area:docs`, `area:control-center`, `area:hermes-fleet`, `area:aihub`
- `stack:csharp`, `stack:winui`, `stack:wpf`, `stack:cpp`, `stack:fsharp`, `stack:python`
- `type:bug`, `type:feature`, `type:task`, `type:release`, `type:owner-decision`
- `priority:p0`, `priority:p1`, `priority:p2`, `priority:p3`
- `status:blocked`, `status:needs-owner`, `status:ready`, `status:in-review`

Recommended first milestones:

- `owner-baseline` for access, visibility, protections, Actions, environments, Pages, Wiki, Projects, labels, and milestones.
- `azure-foundation` for subscription, tenant, identity, resource groups, OIDC, and CLI validation.
- `control-center-readiness` for C# / WinUI / WPF governance and release requirements.
- `hermes-fleet-readiness` for production automation, safety gates, and deployment approvals.

## Completion checklist

- [ ] Repository access reviewed and least privilege applied.
- [ ] Organization owner and recovery model documented.
- [ ] Repository visibility confirmed.
- [ ] Branch protection enabled for source-of-truth branches.
- [ ] GitHub Actions permissions reduced to least privilege.
- [ ] Environments created with required reviewers and scoped secrets.
- [ ] Pages setting documented.
- [ ] Wiki setting documented.
- [ ] Project board created or linked.
- [ ] Labels and milestones standardized.
