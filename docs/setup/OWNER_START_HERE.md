* `Heli0s-dynamics` = enterprise umbrella
* `M0nado` = GitHub organization / business-product owner
* `helios-platform` = official source-of-truth repo
* `helios-control` = C# / WinUI / WPF control center
* `hermes-fleet-production` = Hermes fleet production automation

# Owner Start Here

This guide is written for the owner who needs to click through GitHub and Azure settings without relying on GitHub Copilot. It assumes `Heli0s-dynamics` is the enterprise umbrella, `M0nado` is the GitHub organization and business/product owner, `helios-platform` is the official source-of-truth repository, `helios-control` is the C# / WinUI / WPF control center, and `hermes-fleet-production` is the production automation repository for the Hermes fleet.

Follow the sections in order. Do not connect production Azure credentials, deployment environments, fleet automation, or release workflows until repository ownership, branch protection, Actions permissions, and environments are locked down.

## Read this first: where everything belongs

| Item | Intended owner | What it is for | First place to click |
| --- | --- | --- | --- |
| `Heli0s-dynamics` | Enterprise umbrella | Enterprise policy, billing, SSO/SAML, audit, global security posture | GitHub avatar → **Your enterprises** → `Heli0s-dynamics` |
| `M0nado` | GitHub organization / business-product owner | Organization teams, repositories, Projects, org secrets, repo transfer destination | GitHub avatar → **Your organizations** → `M0nado` |
| `helios-platform` | `M0nado` organization, under the `Heli0s-dynamics` enterprise when enterprise features are available | Source of truth for shared platform governance, AIHub, setup docs, release rules | `https://github.com/M0nado/helios-platform` → **Settings** |
| `helios-control` | `M0nado` organization unless there is a documented exception | Control center UI repo for C# / WinUI 3 / WPF work | `https://github.com/M0nado/helios-control` → **Settings** |
| `hermes-fleet-production` | `M0nado` organization unless there is a documented exception | Production automation, deployment, fleet operations, secrets, runbooks | `https://github.com/M0nado/hermes-fleet-production` → **Settings** |

If a repository is still under a personal user or a different organization, treat that as temporary. Document the current owner, who has `Admin`, and the plan to transfer it into `M0nado` before you attach production secrets or production Azure resources.

## Zero-Copilot setup path

You can complete this guide without GitHub Copilot. Use:

1. **GitHub web settings** for ownership, visibility, branch rules, Actions permissions, environments, Pages, Wiki, Projects, labels, and milestones.
2. **Azure Portal** for tenant, subscription, resource groups, app registrations, managed identities, Key Vault, budgets, and role assignments.
3. **Azure CLI** for repeatable setup commands from your own terminal.
4. **GitHub CLI** only if you want a command-line audit. The web clicks are still the source of truth in this guide.

## Owner fast path: do these in order

1. Open the source-of-truth repository: `https://github.com/M0nado/helios-platform`.
2. Confirm the owner before the slash is `M0nado`. If it is not, write down the actual owner before changing anything.
3. Click **Settings**. If **Settings** is missing, you do not have admin access. Ask an owner for `Admin` or request a repository transfer.
4. Complete **1. Repository access**.
5. Complete **2. Organization ownership**.
6. Complete **3. Repository visibility**.
7. Complete **4. Branch protection**.
8. Complete **5. GitHub Actions permissions**.
9. Complete **6. Environments**.
10. Complete **Azure direct setup** before adding deployment workflows.
11. Complete **7. Pages**, **8. Wiki**, **9. Projects**, and **10. Labels and milestones**.
12. Repeat the same baseline for `helios-control` and `hermes-fleet-production`.
13. Only after the baseline is done, merge long-running branches and connect production automation.

## If repositories are under different places

| Situation | What to click | What to do |
| --- | --- | --- |
| Repo is under `M0nado` | GitHub avatar → **Your organizations** → `M0nado` → **Repositories** → repo → **Settings** | Continue with this guide. Use organization teams, org Projects, and org-level security defaults. |
| Repo is under a personal account | Repo → **Settings** → **General** → **Danger Zone** → **Transfer ownership** | Transfer to `M0nado` before adding production secrets. If transfer is not possible today, document the personal owner and add branch protection now. |
| Repo is under another organization | Repo → **Settings** → **Collaborators and teams**; then organization owner settings | Ask that organization owner for `Admin` access or transfer to `M0nado`. Do not assume you can enforce enterprise policy there. |
| Repo is under `M0nado` but enterprise settings are locked | Avatar → **Your enterprises** → `Heli0s-dynamics` → **Policies** | Enterprise policy is overriding organization/repo settings. Change policy at the enterprise level first. |
| You can see code but not Settings | Repo page only | You have read/write but not admin. Stop owner setup and request admin/maintain access. |
| You cannot access a related repo | Browser returns 404 or access denied | Ask the current owner to invite you or transfer the repo. Do not create a duplicate production repo unless the owner approves. |

## Repository roles to create first

Create teams in `M0nado` before adding individual collaborators everywhere.

| Team | Suggested permission | Repos | Purpose |
| --- | --- | --- | --- |
| `owners` | Admin | All three repos | Final ownership, emergency recovery, policy changes |
| `platform-engineering` | Maintain or Write | `helios-platform` | Shared C#/.NET, AIHub, core services, CI |
| `control-center-ui` | Write | `helios-control` | C# front end, WinUI 3, WPF, installer UI |
| `cpp-performance` | Write | `helios-platform`, performance repos if split | C++ performance backend and native integration work |
| `fsharp-analytics` | Write | Analytics/prediction code paths | F# math, predictions, analytics, parallel computation |
| `python-aihub` | Write | AIHub and integration repos | Python AIHub integrations, model tooling, automation scripts |
| `security-ops` | Maintain | All three repos | Code scanning, Dependabot, secret scanning, release gates |
| `hermes-xcore` | Write or Maintain | `hermes-fleet-production` | Hermes XCore specialist setup and fleet production automation |
| `release-managers` | Maintain | All three repos | Versioning, releases, deployments, rollback coordination |

## Azure direct setup: owner workstation

Use this when you cannot access GitHub Copilot but can set up Azure directly.

### Install or update tools

#### Windows PowerShell

```powershell
winget install --id Microsoft.AzureCLI -e
winget install --id GitHub.cli -e
winget install --id Microsoft.PowerShell -e
az version
gh --version
```

#### macOS

```bash
brew update
brew install azure-cli gh
az version
gh --version
```

#### Linux

```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
sudo apt-get update && sudo apt-get install -y gh
az version
gh --version
```

### Sign in and select the correct Azure tenant/subscription

```powershell
az login
az account tenant list --output table
az account list --output table
az account set --subscription "<subscription-id-or-name>"
az account show --output table
```

Write down:

- Tenant ID
- Subscription ID
- Subscription name
- Billing owner
- Production resource group naming pattern
- Whether this is commercial Azure, Azure Government, or another cloud

### Create baseline resource groups

Use names that clearly separate owner setup, non-production, and production.

```powershell
$location = "eastus"
az group create --name "rg-helios-platform-dev" --location $location
az group create --name "rg-helios-platform-staging" --location $location
az group create --name "rg-helios-platform-prod" --location $location
az group create --name "rg-hermes-fleet-prod" --location $location
```

### Create deployment identities for GitHub Actions OIDC

Prefer OpenID Connect over long-lived client secrets. Create separate app registrations for non-production and production so development or staging workflows cannot inherit production Azure roles after login.

```powershell
$subscriptionId = az account show --query id -o tsv
$tenantId = az account show --query tenantId -o tsv

$nonProdAppName = "app-github-m0nado-helios-platform-nonprod"
$nonProdAppId = az ad app create --display-name $nonProdAppName --query appId -o tsv
az ad sp create --id $nonProdAppId
az role assignment create --assignee $nonProdAppId --role Contributor --scope "/subscriptions/$subscriptionId/resourceGroups/rg-helios-platform-dev"
az role assignment create --assignee $nonProdAppId --role Contributor --scope "/subscriptions/$subscriptionId/resourceGroups/rg-helios-platform-staging"

$prodAppName = "app-github-m0nado-helios-platform-prod"
$prodAppId = az ad app create --display-name $prodAppName --query appId -o tsv
az ad sp create --id $prodAppId
az role assignment create --assignee $prodAppId --role Contributor --scope "/subscriptions/$subscriptionId/resourceGroups/rg-helios-platform-prod"

Do not add production resource group roles to the non-production app registration, and do not add development or staging federated credentials to the production app registration. GitHub environment approvals control when jobs can request tokens, but Azure roles are granted to the service principal after login; keep each identity scoped to only the resource groups that environment should manage.

Create one federated credential per GitHub environment on the app registration for that environment. The `subject` value must match the repository and environment/branch that will request the token.
$devParameters = @{
  name = "github-helios-platform-development"
  issuer = "https://token.actions.githubusercontent.com"
  subject = "repo:M0nado/helios-platform:environment:development"
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json

$devParameters | Out-File -FilePath federated-credential.json -Encoding utf8
az ad app federated-credential create --id $nonProdAppId --parameters federated-credential.json
Remove-Item federated-credential.json

$stagingParameters = @{
  name = "github-helios-platform-staging"
  issuer = "https://token.actions.githubusercontent.com"
  subject = "repo:M0nado/helios-platform:environment:staging"
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json

$stagingParameters | Out-File -FilePath federated-credential.json -Encoding utf8
az ad app federated-credential create --id $nonProdAppId --parameters federated-credential.json
Remove-Item federated-credential.json

$prodParameters = @{
$prodParameters | Out-File -FilePath federated-credential.json -Encoding utf8
az ad app federated-credential create --id $prodAppId --parameters federated-credential.json
```

| `AZURE_CLIENT_ID` | Environment variable or secret | `$nonProdAppId` for development/staging; `$prodAppId` for production |

az role assignment create --assignee $prodAppId --role "Key Vault Secrets User" --scope "$(az keyvault show --name kv-helios-platform-prod --query id -o tsv)"

```powershell
$devParameters = @{
  name = "github-helios-platform-development"
  issuer = "https://token.actions.githubusercontent.com"
  subject = "repo:M0nado/helios-platform:environment:development"
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json

$devParameters | Out-File -FilePath federated-credential.json -Encoding utf8
az ad app federated-credential create --id $nonProdAppId --parameters federated-credential.json
Remove-Item federated-credential.json

$stagingParameters = @{
  name = "github-helios-platform-staging"
  issuer = "https://token.actions.githubusercontent.com"
  subject = "repo:M0nado/helios-platform:environment:staging"
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json

$stagingParameters | Out-File -FilePath federated-credential.json -Encoding utf8
az ad app federated-credential create --id $nonProdAppId --parameters federated-credential.json
Remove-Item federated-credential.json

$prodParameters = @{
  name = "github-helios-platform-production"
  issuer = "https://token.actions.githubusercontent.com"
  subject = "repo:M0nado/helios-platform:environment:production"
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json

$prodParameters | Out-File -FilePath federated-credential.json -Encoding utf8
az ad app federated-credential create --id $prodAppId --parameters federated-credential.json
Remove-Item federated-credential.json
```

Store these as GitHub environment variables or secrets in `helios-platform` → **Settings** → **Environments** → target environment:

| Name | Store as | Value |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | Environment variable or secret | `$nonProdAppId` for development/staging; `$prodAppId` for production |
| `AZURE_TENANT_ID` | Environment variable or secret | Tenant ID from `$tenantId` |
| `AZURE_SUBSCRIPTION_ID` | Environment variable or secret | Subscription ID from `$subscriptionId` |
| `AZURE_RESOURCE_GROUP` | Environment variable | Environment-specific resource group |

For `hermes-fleet-production`, create a separate app registration or managed identity with the narrowest roles possible. Do not reuse broad platform credentials for fleet production unless the owner explicitly approves it.

### Add Key Vault for secrets that should not live in GitHub

```powershell
az keyvault create --name "kv-helios-platform-prod" --resource-group "rg-helios-platform-prod" --location $location --enable-rbac-authorization true
az role assignment create --assignee $prodAppId --role "Key Vault Secrets User" --scope "$(az keyvault show --name kv-helios-platform-prod --query id -o tsv)"
```

Put only non-sensitive IDs in GitHub variables. Put production credentials, connection strings, certificates, and fleet secrets in Key Vault.

### Add budgets and basic monitoring

```powershell
az consumption budget create \
  --budget-name "helios-platform-monthly" \
  --amount 100 \
  --time-grain Monthly \
  --start-date "2026-06-01" \
  --end-date "2036-06-01" \
  --category Cost \
  --resource-group "rg-helios-platform-prod"
```

If the budget command is not available in your tenant, create the budget in Azure Portal: **Cost Management + Billing** → **Cost Management** → **Budgets** → **Add**.

## 1. Repository access

Goal: confirm who can read, triage, write, maintain, administer, and deploy from each repository.

### Click-by-click for `M0nado` organization repos

1. Open `https://github.com/M0nado/helios-platform`.
2. Click **Settings**.
3. Click **Collaborators and teams**.
4. Review every user, team, GitHub App, deploy key, and pending invitation.
5. Remove unknown users and stale automation accounts.
6. Add teams instead of individual users whenever possible.
7. Set permissions using least privilege:
   1. **Read** for auditors and observers.
   2. **Triage** for issue/project managers.
   3. **Write** for regular contributors.
   4. **Maintain** for release operators and repo managers.
   5. **Admin** only for owners who can change security, transfer, archive, or delete the repo.
8. Repeat for `helios-control`.
9. Repeat for `hermes-fleet-production` with stricter access because it controls production automation.

### Done when

- Only expected owners, teams, apps, and deploy keys have access.
- `hermes-fleet-production` has the smallest access list.
- Every individual admin has a documented reason.

## 2. Organization ownership

Goal: make `M0nado` the operational GitHub organization and connect it to the `Heli0s-dynamics` enterprise umbrella when enterprise features are available.

### Click-by-click for `M0nado`

1. Click your GitHub avatar.
2. Click **Your organizations**.
3. Click `M0nado`.
4. Click **Settings**.
5. Click **People**.
6. Confirm at least two trusted people are organization owners for recovery.
7. Click **Teams** and create the baseline teams from this guide.
8. Click **Member privileges**.
9. Set repository creation, forking, Pages, Actions, and outside-collaborator policies intentionally.
10. Click **Security** and enable available security defaults.

### Click-by-click for `Heli0s-dynamics` enterprise

1. Click your GitHub avatar.
2. Click **Your enterprises**.
3. Click `Heli0s-dynamics`.
4. Click **Policies**.
5. Review repository visibility, Actions, Codespaces, SSO/SAML, IP allow list, and security policies.
6. If a repo setting is locked inside `M0nado`, come back here and change the enterprise policy first.
7. Click **Organizations** and confirm `M0nado` is listed if enterprise membership is already configured.

### Done when

- `M0nado` is documented as the GitHub organization / business-product owner.
- `Heli0s-dynamics` is documented as the enterprise umbrella.
- At least two trusted owners can recover access.

## 3. Repository visibility

Goal: decide whether each repository is public, private, or internal before secrets or production workflows are connected.

### Recommended visibility

| Repository | Recommended visibility | Reason |
| --- | --- | --- |
| `helios-platform` | Private or internal until owner baseline is complete | It is the source of truth and may reference architecture/security setup. |
| `helios-control` | Private or internal | Control center UI can expose internal workflows, endpoints, and deployment assumptions. |
| `hermes-fleet-production` | Private | Production automation should not expose fleet operations, secrets, schedules, or infrastructure names. |

### Click-by-click

1. Open the repository.
2. Click **Settings**.
3. Click **General**.
4. Scroll to **Danger Zone**.
5. Click **Change visibility** only if the owner intentionally approved the change.
6. Pick **Private**, **Internal**, or **Public**.
7. Type the required confirmation text.
8. Save the decision in an issue or project item.

### Done when

- Visibility is recorded for all three repos.
- Public content has been checked for secrets, endpoints, customer data, and internal automation details.

## 4. Branch protection

Goal: protect source branches and stop accidental direct pushes, force pushes, deleted release branches, or unreviewed production automation changes.

### Preferred: repository rulesets

1. Open the repository.
2. Click **Settings**.
3. Click **Rules**.
4. Click **Rulesets**.
5. Click **New ruleset**.
6. Choose **New branch ruleset**.
7. Name it `protect-main-and-release`.
8. Set **Enforcement status** to **Active** after testing.
9. Under **Target branches**, add:
   - `main`
   - `release/*`
   - `hotfix/*` if used
10. Enable these rules:
    - Restrict deletions.
    - Require a pull request before merging.
    - Require approvals.
    - Dismiss stale pull request approvals when new commits are pushed.
    - Require conversation resolution before merging.
    - Require status checks to pass.
    - Require branches to be up to date before merging when checks depend on current base.
    - Block force pushes.
11. Add bypass actors only for emergency owner accounts or release managers.
12. Save the ruleset.

### If rulesets are unavailable: classic branch protection

1. Open the repository.
2. Click **Settings**.
3. Click **Branches**.
4. Click **Add branch protection rule**.
5. Branch name pattern: `main`.
6. Enable pull request review, required checks, conversation resolution, signed commits if used, and force-push/deletion restrictions.
7. Create another rule for `release/*`.

### Suggested required checks by repo

| Repository | Required checks |
| --- | --- |
| `helios-platform` | .NET build/test, JavaScript tests if present, security scan, docs/link check when docs change |
| `helios-control` | Windows build, C# compile, WinUI/WPF UI tests where available, installer/package validation |
| `hermes-fleet-production` | Infrastructure validation, deployment dry run, secret scan, production approval gate |

### Done when

- `main` and release branches cannot be pushed directly by normal contributors.
- PR review is required before merge.
- Force pushes and branch deletion are blocked.

## 5. GitHub Actions permissions

Goal: make workflow permissions safe by default and grant write/deploy access only where required.

### Organization-level settings

1. Click your avatar.
2. Click **Your organizations**.
3. Click `M0nado`.
4. Click **Settings**.
5. Click **Actions** → **General**.
6. Under **Actions permissions**, choose the allowed policy for the organization.
7. Under **Workflow permissions**, choose **Read repository contents and packages permissions** by default.
8. Leave **Allow GitHub Actions to create and approve pull requests** unchecked unless a specific automation is approved.
9. Save.

### Repository-level settings

1. Open the repository.
2. Click **Settings**.
3. Click **Actions** → **General**.
4. Confirm the repository does not override organization policy in an unsafe way.
5. Set default workflow token permissions to read-only.
6. Require approval for outside collaborators when available.
7. Disable or restrict self-hosted runners unless you manage them.

### Workflow file rule

Any workflow that needs Azure OIDC must include explicit permissions:

```yaml
permissions:
  id-token: write
  contents: read
```

Do not give every workflow broad write access. Put deploy workflows behind environments with reviewers.

### Done when

- Default `GITHUB_TOKEN` permissions are read-only.
- Azure deployment workflows use OIDC, not long-lived secrets, unless an owner documents an exception.
- Production workflows require environment approval.

## 6. Environments

Goal: separate development, staging, and production secrets, variables, reviewers, and deployment branches.

### Click-by-click

1. Open the repository.
2. Click **Settings**.
3. Click **Environments**.
4. Click **New environment**.
5. Create `development`.
6. Add development-only variables and secrets.
7. Create `staging`.
8. Add required reviewers for `staging`.
9. Add deployment branch restrictions for `staging`, such as `main` and `release/*`.
10. Create `production`.
11. Add required reviewers for `production`.
12. Add deployment branch restrictions for `production`, such as `release/*` or protected `main` only.
13. Add environment variables:
    - `AZURE_CLIENT_ID`
    - `AZURE_TENANT_ID`
    - `AZURE_SUBSCRIPTION_ID`
    - `AZURE_RESOURCE_GROUP`
14. Add environment secrets only when there is no safer option.

### Environment recommendation

| Environment | Reviewers | Branches | Secrets |
| --- | --- | --- | --- |
| `development` | Optional | feature branches or `main` | Non-production only |
| `staging` | Required | `main`, `release/*` | Staging only |
| `production` | Required, owner/release manager | protected `main` or `release/*` | Production only, prefer Key Vault |

### Done when

- `development`, `staging`, and `production` exist.
- Production requires human approval.
- Production Azure details are not stored as broad repository-wide secrets.

## 7. Pages

Goal: make the owner guide visible without publishing sensitive information.

### Click-by-click

1. Open `helios-platform`.
2. Click **Settings**.
3. Click **Pages**.
4. Under **Build and deployment**, choose **GitHub Actions** if generated docs or dashboards will publish.
5. Use branch/folder publishing only if the team intentionally maintains static files that way.
6. Open the published URL in a private/incognito window.
7. Confirm the page does not show secrets, private endpoints, customer identifiers, internal tokens, or production URLs.
8. Keep the visible page limited to owner steps, links, dashboards, and safe status summaries.

### Done when

- Pages is enabled or intentionally disabled.
- The dashboard links to this guide.
- Published content is safe for the repository visibility.

## 8. Wiki

Goal: decide whether the GitHub Wiki is official documentation or temporary notes.

### Click-by-click

1. Open the repository.
2. Click **Settings**.
3. Click **General**.
4. Scroll to **Features**.
5. Check or uncheck **Wikis**.
6. If Wiki is enabled, click the repository **Wiki** tab.
7. Create a landing page that says official procedures live in `docs/` and links back to this guide.

### Recommendation

- Use `docs/` as the source of truth for setup, architecture, release, security, and owner decisions.
- Use Wiki only for meeting notes, scratch references, and temporary notes.
- Move anything official from Wiki into `docs/` through a pull request.

### Done when

- Wiki is intentionally enabled or disabled.
- Contributors know whether Wiki content is official.

## 9. Projects

Goal: track owner setup, Azure setup, branch merges, UI readiness, AIHub integration, and Hermes fleet production work across repositories.

### Click-by-click for `M0nado` organization project

1. Click your avatar.
2. Click **Your organizations**.
3. Click `M0nado`.
4. Click **Projects**.
5. Click **New project**.
6. Choose table or board layout.
7. Name it `Helios Owner Readiness`.
8. Add fields:
   - `Priority`
   - `Phase`
   - `Repository`
   - `Language stack`
   - `Risk`
   - `Owner`
   - `Target milestone`
   - `Environment`
   - `Azure subscription/resource group`
9. Add views:
   - Owner baseline
   - GitHub security
   - Azure foundation
   - Branch merge readiness
   - Control center readiness
   - Hermes fleet readiness
   - AIHub integration
   - Release readiness

### Suggested first project items

1. `helios-platform`: owner baseline and branch protection.
2. `helios-platform`: Azure OIDC setup.
3. `helios-control`: C# / WinUI 3 / WPF build validation.
4. `helios-platform`: C++ performance backend integration plan.
5. `helios-platform`: F# analytics/predictions plan.
6. `helios-platform`: Python AIHub integration plan.
7. `hermes-fleet-production`: Hermes XCore production automation baseline.
8. `hermes-fleet-production`: production secret and Key Vault review.
9. All repos: combine/merge branch inventory and conflict plan.

### Done when

- Owner work is tracked in a project.
- Each repository can be filtered separately.
- Branch merge work is visible before code is merged.

## 10. Labels and milestones

Goal: make triage consistent across the source-of-truth, control center, and fleet production repos.

### Click-by-click for labels

1. Open the repository.
2. Click **Issues**.
3. Click **Labels**.
4. Click **New label**.
5. Enter the label name, description, and color.
6. Repeat for the baseline labels.

### Baseline labels

| Group | Labels |
| --- | --- |
| Areas | `area:github`, `area:azure`, `area:security`, `area:docs`, `area:control-center`, `area:hermes-fleet`, `area:aihub`, `area:xcore` |
| Stacks | `stack:csharp`, `stack:winui`, `stack:wpf`, `stack:cpp`, `stack:fsharp`, `stack:python`, `stack:powershell` |
| Types | `type:bug`, `type:feature`, `type:task`, `type:release`, `type:owner-decision`, `type:branch-merge` |
| Priorities | `priority:p0`, `priority:p1`, `priority:p2`, `priority:p3` |
| Status | `status:blocked`, `status:needs-owner`, `status:ready`, `status:in-review`, `status:approved` |

### Click-by-click for milestones

1. Open the repository.
2. Click **Issues**.
3. Click **Milestones**.
4. Click **New milestone**.
5. Add title, description, and due date if known.
6. Create these first milestones:
   - `owner-baseline`
   - `azure-foundation`
   - `branch-merge-readiness`
   - `control-center-readiness`
   - `aihub-integration`
   - `hermes-fleet-readiness`
   - `production-release-readiness`

### Done when

- Issues and PRs can be filtered by area, stack, type, priority, and status.
- Owner decisions and branch merge work are tracked in milestones.

## Branch merge and optimization readiness

Use this before combining branches. This repository currently has only the checked-out `work` branch locally, so fetch remote branches before planning a full merge.

### Click-by-click

1. Open the repository.
2. Click **Branches**.
3. Review active branches, stale branches, protected branches, and recently updated branches.
4. Open each branch and read its latest commits.
5. Create a project item for each branch that might be merged.
6. Require a PR for each branch into `main`.
7. Use the integration merge PR template.
8. Merge smallest/safest branches first.
9. Merge security and build fixes before feature branches.
10. Do not merge production automation until `hermes-fleet-production` environments and Azure credentials are locked down.

### Local command checklist

```bash
git fetch --all --prune
git branch --all --verbose --no-abbrev
git status --short
git log --oneline --decorate --graph --all --max-count=50
```

For each branch:

```bash
git switch <branch-name>
dotnet build HELIOS.Platform.slnx
# Add repo-specific JavaScript, Python, C++, F#, or UI checks before merging.
```

### Optimization ownership by stack

| Stack | Primary repo | Owner team | Gate before merge |
| --- | --- | --- | --- |
| C# platform services | `helios-platform` | `platform-engineering` | `dotnet build`, unit tests, security review |
| C# / WinUI 3 / WPF front end | `helios-control` | `control-center-ui` | Windows UI build, packaging validation, accessibility review |
| C++ performance backend | `helios-platform` or dedicated native repo | `cpp-performance` | Native build, benchmarks, memory/thread safety review |
| F# analytics/predictions | `helios-platform` or analytics repo | `fsharp-analytics` | Math validation, deterministic test data, performance checks |
| Python AIHub integration | `helios-platform` or AIHub repo | `python-aihub` | lint/test, model/provider safety, secret handling review |
| Hermes XCore production automation | `hermes-fleet-production` | `hermes-xcore` and `security-ops` | staging dry run, owner approval, rollback plan |

## Final completion checklist

- [ ] `Heli0s-dynamics` is documented as the enterprise umbrella.
- [ ] `M0nado` is documented as the GitHub organization / business-product owner.
- [ ] Repository account location is identified for `helios-platform`, `helios-control`, and `hermes-fleet-production`.
- [ ] Repository access reviewed and least privilege applied.
- [ ] Organization owners and recovery model documented.
- [ ] Enterprise policy blockers reviewed.
- [ ] Repository visibility confirmed.
- [ ] Branch protection or rulesets enabled for source-of-truth branches.
- [ ] GitHub Actions permissions reduced to least privilege.
- [ ] `development`, `staging`, and `production` environments created with required reviewers and scoped secrets.
- [ ] Azure CLI account, tenant, and subscription verified.
- [ ] Azure resource groups created or intentionally deferred.
- [ ] Azure OIDC identity created or intentionally deferred.
- [ ] Key Vault and budget plan documented.
- [ ] Pages setting documented and owner guide made discoverable.
- [ ] Wiki setting documented.
- [ ] Project board created or linked.
- [ ] Labels and milestones standardized.
- [ ] Branch merge inventory created before combining branches.
- [ ] Integration merge process documented before production-impacting merges.

## Official references

- GitHub rulesets: <https://docs.github.com/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets>
- GitHub repository rulesets creation: <https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/creating-rulesets-for-a-repository>
- Azure Login for GitHub Actions: <https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure>
- Azure Bicep deployment with GitHub Actions and OIDC: <https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deploy-github-actions>
- Azure CLI federated credential commands: <https://learn.microsoft.com/en-us/cli/azure/identity/federated-credential?view=azure-cli-latest>
- Azure Key Vault with GitHub Actions: <https://learn.microsoft.com/en-us/azure/developer/github/github-actions-key-vault>
