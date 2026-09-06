# Run This First

The package is **plan-first**. Local validation and repository preview do not
write to GitHub, Azure, Slack, Linear, SharePoint, or Teams.

## 1. Validate locally

```powershell
py -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install -e ".[test]"
fabricctl validate --root .
pytest
fabricctl simulate --root . --fixture tests/fixtures/deployment-plan.json --include-disabled
```

## 2. Preview repository application

```powershell
pwsh -NoProfile -File .\scripts\bootstrap\Install-HeliosFabricOverlay.ps1 `
  -PackRoot (Resolve-Path .) `
  -RepositoryRoot C:\src\helios-platform
```

The first invocation only writes a conflict report. After review:

```powershell
pwsh -NoProfile -File .\scripts\bootstrap\Install-HeliosFabricOverlay.ps1 `
  -PackRoot (Resolve-Path .) `
  -RepositoryRoot C:\src\helios-platform `
  -Apply `
  -Confirm 'APPLY HELIOS AUTOMATION FABRIC'
```

## 3. Commit to a review branch

```powershell
git switch -c feat/helios-enterprise-automation-fabric
git add .
git commit -m "Add governed HELIOS enterprise automation fabric"
git push -u origin HEAD
```

## 4. Bootstrap GitHub controls

Review `config/github/environments.json`, then run the bootstrap in preview
mode. It targets these protected environments:

- `helios-dev`
- `helios-stage`
- `helios-prod`
- `helios-emergency`

`helios-prod` and `helios-emergency` prohibit self-review. Production requires
two GitHub reviewers plus an unchanged source SHA and Azure what-if hash.

```powershell
pwsh -NoProfile -File .\scripts\bootstrap\Initialize-HeliosGitHub.ps1 `
  -Repository 'M0nado/helios-platform'
```

## 5. Create Azure OIDC identities and run plan-only deployment

The OIDC script defaults to preview. It creates no client secret. Review its
output, grant only the documented scope, then invoke `HELIOS Azure plan` with an
immutable source SHA. Keep `deployWorkloads=false` until Key Vault references,
managed identities, and connector health checks are complete.

## 6. Activate connectors in evidence-first order

```text
1. SharePoint evidence sink
2. Slack outbound notifications
3. Teams Workflows outbound cards
4. Linear outbound synchronization
5. GitHub App callback/update path
6. Slack, Linear, and GitHub inbound webhooks
7. Teams approval callback
```

Each connector remains `enabled: false` in
`config/fabric/connector-registry.json` until its scoped credential, health
check, rollback path, and GitHub-reviewed configuration change are complete.

## 7. Production deployment

Production may only run through `.github/workflows/azure-deploy.yml` with:

- `apply=true`
- exact reviewed `source_sha`
- exact approved `plan_run_id`
- exact canonical `plan_sha256`
- closed production blocker issue
- protected `helios-prod` approval

Slack, Teams, Linear, and SharePoint can record or display approvals; none of
them can authorize Azure deployment by themselves.
