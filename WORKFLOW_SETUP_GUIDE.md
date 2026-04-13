# GitHub Actions Workflow Setup Guide

Complete documentation for HELIOS Platform GitHub Actions workflows.

---

## 📊 Workflow Overview

| Workflow | Trigger | Purpose | Schedule |
|----------|---------|---------|----------|
| **deploy.yml** | Push, PR, Manual | Main deployment pipeline (7 phases) | On-demand |
| **nuget.yml** | Push/Tags | Build & publish NuGet package | On release |
| **analysis.yml** | Push, Weekly | Component metrics validation | Weekly |
| **quality.yml** | Push, PR | Code quality & linting | Every push |
| **verify.yml** | Scheduled, Manual | Health check & verification | Every 6h |

---

## 🚀 Deploy Workflow (`deploy.yml`)

### What It Does

Orchestrates the complete HELIOS deployment across 7 phases with parallel execution where possible.

**Phases:**
1. **Preflight Checks** - Validates system requirements (10 min)
2. **Infrastructure** - Sets up base infrastructure on Azure (12 min)
3. **Agent Fleet** - Deploys 6 agents in parallel (25 min)
4. **AI Services** - Initializes 12+ AI integrations (18 min)
5. **Security** - Deploys 8-layer security framework (22 min)
6. **Monitoring** - Sets up dashboards & alerts (15 min)
7. **Verification** - Runs 42-point validation suite (10 min)

### Triggers

**Automatic:**
- `push` to `main` or `develop` branches
- `pull_request` to `main` branch

**Manual (Workflow Dispatch):**
- Choose specific phase to run (0-6)
- Or run all phases sequentially

### Environment Variables

```yaml
DEPLOYMENT_DIR: ./deployment
AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
```

**Required GitHub Secrets:**
```
AZURE_SUBSCRIPTION_ID    - Azure subscription ID
AZURE_TENANT_ID          - Azure tenant ID
AZURE_CLIENT_ID          - Service principal client ID
AZURE_CLIENT_SECRET      - Service principal secret
```

### How to Set Secrets

1. Go to: `https://github.com/M0nado/helios-platform/settings/secrets/actions`
2. Click **New repository secret**
3. Add each secret:
   - Name: `AZURE_SUBSCRIPTION_ID`
   - Value: (your subscription ID)
4. Repeat for all 4 secrets

### Running Manually

1. Go to: `https://github.com/M0nado/helios-platform/actions`
2. Select **HELIOS Platform Deployment Pipeline**
3. Click **Run workflow**
4. Choose phase from dropdown:
   - `preflight` - Run only preflight checks
   - `infrastructure` - Infrastructure only
   - `agents` - Deploy agent fleet
   - `ai-services` - AI services
   - `security` - Security hardening
   - `monitoring` - Monitoring setup
   - `verification` - Final verification
   - `all` - Full deployment (102 min)
5. Click **Run workflow**

### Job Dependencies

```
preflight
    ↓
infrastructure
    ↓
agents ────────────┐
    ↓              │
ai-services ────────┤
    ↓              │
security ──────────┤
    ↓              │
monitoring ────────┤
    ↓              │
verification ◄─────┘
    ↓
notify
```

### Outputs

- **Logs:** Uploaded to Actions artifacts
- **Report:** deployment-report.txt (after verification)
- **Status:** Real-time in Actions tab

---

## 📦 NuGet Workflow (`nuget.yml`)

### What It Does

Builds and publishes HELIOS as a NuGet package.

### Triggers

- `push` to `main` branch
- `push` with tags matching `v*` (e.g., `v1.0.0`)
- Manual trigger (workflow_dispatch)

### Environment Variables

```yaml
NUGET_FEED: https://api.nuget.org/v3/index.json
NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
PACKAGE_VERSION: 1.0.0
```

**Required Secret:**
- `NUGET_API_KEY` - NuGet.org API key

### Steps

1. **Build** - Compiles release binary
2. **Pack** - Creates NuGet package (.nupkg)
3. **Publish** - Uploads to NuGet.org (if tagged)
4. **Release** - Creates GitHub release

### Creating a Release

1. Create a git tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. Workflow automatically:
   - Builds package
   - Publishes to NuGet
   - Creates GitHub release with notes

### Installation

After publishing:
```powershell
dotnet add package HELIOS.Platform --version 1.0.0
```

---

## 📊 Analysis Workflow (`analysis.yml`)

### What It Does

Validates component metrics and generates analysis reports.

### Triggers

- `push` to `main` when `COMPONENT_*.json` or `COMPONENT_ANALYSIS.md` changes
- Weekly schedule (Sundays at midnight)
- Manual trigger

### Steps

1. **Validate JSON** - Checks COMPONENT_METRICS.json syntax
2. **Generate Report** - Creates analysis summary
3. **Upload Report** - Stores as artifact

### Outputs

- `analysis-report.txt` - Summary of components, time, performance

---

## 🔍 Quality Workflow (`quality.yml`)

### What It Does

Runs code quality checks on all code changes.

### Triggers

- `push` to `main` or `develop`
- `pull_request` to `main`

### Jobs

**PowerShell Linting:**
- Uses PSScriptAnalyzer
- Checks for errors, warnings
- Analyzes `src/` and `scripts/` directories

**Markdown Linting:**
- Validates markdown syntax
- Checks formatting

**JSON Validation:**
- Validates all `.json` files
- Checks syntax

**Security Scanning:**
- Uses GitHub Super Linter
- Scans for security issues
- Checks for secrets in code

### Outputs

- PowerShell analysis in artifacts
- Pass/fail status in PR checks

---

## ✅ Verification Workflow (`verify.yml`)

### What It Does

Continuous health checks and system validation.

### Triggers

- Scheduled: Every 6 hours (cron: `0 */6 * * *`)
- Manual trigger (workflow_dispatch)

### Jobs

**Health Check:**
- Verifies all required files exist
- Counts commits and files
- Validates deployment scripts
- Reports repository statistics

**Metrics Validation:**
- Validates COMPONENT_METRICS.json
- Checks analysis documents
- Verifies documentation

**Status Report:**
- Generates health report
- Shows component status
- Reports metrics summary

### Outputs

- `health-report.md` - Complete health status
- All checks passed/failed indication

---

## 🔧 Setting Up Workflows

### Prerequisites

1. GitHub repository: `M0nado/helios-platform`
2. Main branch with all files
3. GitHub Actions enabled (default)
4. Secrets configured (for deployment)

### Quick Setup (5 minutes)

#### Step 1: Add Azure Secrets

```bash
# If using GitHub CLI
gh secret set AZURE_SUBSCRIPTION_ID --body "your-subscription-id"
gh secret set AZURE_TENANT_ID --body "your-tenant-id"
gh secret set AZURE_CLIENT_ID --body "your-client-id"
gh secret set AZURE_CLIENT_SECRET --body "your-client-secret"
```

Or manually via GitHub UI:
1. Go to repository **Settings → Secrets and variables → Actions**
2. Click **New repository secret**
3. Add each required secret

#### Step 2: Enable Workflows

1. Go to **Actions** tab
2. All workflows are auto-enabled
3. No additional setup needed

#### Step 3: Test Workflows

1. Make a push to `main`:
   ```bash
   git commit --allow-empty -m "Trigger workflows"
   git push origin main
   ```

2. Go to **Actions** tab
3. Watch workflows run:
   - Quality check (5 min)
   - Analysis (2 min)
   - Deploy (optional)

### Step 4: Monitor Runs

1. Go to: `https://github.com/M0nado/helios-platform/actions`
2. Click any workflow to see details
3. View logs, artifacts, status

---

## 📈 Workflow Execution Times

| Workflow | Average Time | Max Time |
|----------|--------------|----------|
| Deploy (preflight only) | 2 min | 5 min |
| Deploy (all phases) | 102 min | 120 min |
| NuGet (build only) | 3 min | 10 min |
| Analysis | 2 min | 5 min |
| Quality (basic) | 5 min | 15 min |
| Verification | 3 min | 8 min |

---

## 🚨 Troubleshooting Workflows

### Workflow Won't Trigger

**Problem:** Actions not running on push

**Solutions:**
1. Check branch is `main` or `develop`
2. Verify Actions enabled in Settings
3. Check workflow file syntax (YAML)
4. Look for errors in Actions tab

### Deploy Phase Failing

**Problem:** Specific phase fails

**Solutions:**
1. Check Azure secrets are set correctly
2. Verify Azure subscription is active
3. Check phase dependencies in logs
4. Review error message in job logs

### NuGet Not Publishing

**Problem:** Package won't publish

**Solutions:**
1. Verify tag format: `v1.0.0` (with `v` prefix)
2. Check NUGET_API_KEY secret is set
3. Ensure package version is unique
4. Review publish logs for API errors

### Quality Check Failing

**Problem:** Linting or validation fails

**Solutions:**
1. Review artifact logs for specific issues
2. Fix PowerShell syntax in scripts
3. Validate JSON files: `python -m json.tool file.json`
4. Check markdown formatting

---

## 📊 Workflow Analytics

### View Workflow Runs

1. Go to **Actions** tab
2. Click workflow name
3. See all runs with:
   - Status (success/failure)
   - Duration
   - Date/time
   - Commit reference

### Export Workflow Data

```bash
# List all workflow runs
gh run list --repo M0nado/helios-platform

# Get specific workflow runs
gh run list --repo M0nado/helios-platform -w deploy.yml

# View job details
gh run view <RUN_ID> --repo M0nado/helios-platform
```

---

## 🔒 Security Best Practices

### Secrets Management

1. **Never commit secrets** - Use GitHub Secrets only
2. **Rotate secrets** - Update quarterly
3. **Use service principals** - For Azure auth
4. **Limit scope** - Give minimal required permissions
5. **Audit access** - Review who has access

### Workflow Security

1. **Pin action versions** - Use `@v4` not `@latest`
2. **Review workflows** - Before enabling
3. **Validate inputs** - Check workflow_dispatch inputs
4. **Monitor runs** - Check Actions logs regularly
5. **Disable if unused** - Remove unused workflows

---

## 📋 Workflow Checklist

Setup verification:

- [ ] Repository created and synced
- [ ] All workflows present in `.github/workflows/`
- [ ] Azure secrets configured (4 secrets)
- [ ] NuGet secret configured (if using)
- [ ] Test workflow triggered (push to main)
- [ ] Actions tab shows successful runs
- [ ] Artifacts generated and accessible
- [ ] Logs are readable and informative

---

## 📚 Related Documentation

- **GitHub Actions Docs:** https://docs.github.com/en/actions
- **Workflow Syntax:** https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions
- **Deploy Guide:** GITHUB_SETUP_GUIDE.md
- **Component Analysis:** COMPONENT_ANALYSIS.md

---

## ✅ Quick Reference

**Start deployment:**
- Manual: Go to Actions → Deploy → Run workflow
- Automatic: Push to main
- Individual phase: Use workflow_dispatch dropdown

**View logs:**
- Go to Actions → Workflow run → Click job

**Download artifacts:**
- Actions → Workflow run → Artifacts section

**Configure secrets:**
- Settings → Secrets and variables → Actions

**Disable workflow:**
- Actions → Click ... → Disable workflow

---

**Status:** ✅ **ALL WORKFLOWS CONFIGURED & READY**
