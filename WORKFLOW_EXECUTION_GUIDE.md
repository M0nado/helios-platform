# HELIOS Platform - Workflow Execution Guide

## Quick Start

This guide shows how to execute each GitHub Actions workflow and monitor their progress.

---

## Prerequisites

### GitHub CLI Setup
```bash
# Install GitHub CLI (if not already installed)
# Windows: choco install gh
# macOS: brew install gh
# Linux: sudo apt-get install gh

# Authenticate with GitHub
gh auth login

# Verify authentication
gh auth status
```

### Repository Setup
```bash
# Clone the repository
git clone https://github.com/yourusername/helios-platform-repo.git
cd helios-platform-repo

# Verify workflows exist
gh workflow list
```

---

## 1. Execute Deploy Workflow

### Via GitHub CLI

#### Run Full Deployment (All 7 Phases)
```bash
# Trigger full deployment
gh workflow run deploy.yml -f phase=all

# Or using the workflow name
gh workflow run "HELIOS Platform Deployment Pipeline" -f phase=all
```

#### Run Specific Phase
```bash
# Phase options: preflight, infrastructure, agents, ai-services, security, monitoring, verification, all

# Example: Run only preflight checks
gh workflow run deploy.yml -f phase=preflight

# Example: Run infrastructure phase
gh workflow run deploy.yml -f phase=infrastructure

# Example: Run agent fleet deployment
gh workflow run deploy.yml -f phase=agents

# Example: Run verification phase
gh workflow run deploy.yml -f phase=verification
```

### Via GitHub Web UI
1. Navigate to: `Settings` → `Actions` → `General` → enable "Allow all actions"
2. Go to: `Actions` → `Workflows` → select "HELIOS Platform Deployment Pipeline"
3. Click "Run workflow" → select phase from dropdown → "Run workflow"

### Monitor Deployment

#### Check Workflow Status
```bash
# List recent runs
gh workflow view deploy.yml --json runs --jq '.runs[0:5] | .[] | "\(.databaseId) - \(.status) - \(.conclusion)"'

# Get run details (replace RUN_ID with actual ID)
gh run view RUN_ID

# Watch real-time progress
gh run watch RUN_ID --exit-status

# Get full logs
gh run view RUN_ID --log
```

#### Download Artifacts
```bash
# List artifacts
gh run view RUN_ID --json artifacts

# Download specific artifact (replace ARTIFACT_ID)
gh run download RUN_ID -n deployment-report

# Download all artifacts
gh run download RUN_ID
```

#### Check Phase Status
```bash
# Get detailed job information
gh run view RUN_ID --json jobs --jq '.jobs[] | "\(.name) - \(.status) - \(.conclusion)"'

# Get logs for specific job
gh run view RUN_ID --log --name "Deploy Infrastructure"
```

---

## 2. Execute NuGet Package Workflow

### Build & Publish Package

#### Automatic Publish (via Git Tag)
```bash
# Create version tag
git tag v1.0.0

# Push tag to trigger workflow
git push origin v1.0.0

# Workflow automatically:
# 1. Builds NuGet package
# 2. Publishes to NuGet.org
# 3. Creates GitHub Release
```

#### Manual Package Build Only
```bash
# Trigger workflow without publish
gh workflow run nuget.yml

# This will:
# 1. Build the package
# 2. Upload as artifact
# 3. Wait for manual push
```

### Monitor NuGet Build

#### Check Build Status
```bash
# View workflow runs
gh workflow view nuget.yml

# Get specific run details
gh run view RUN_ID --log

# Check if published
gh release list
```

#### Download Built Package
```bash
# Download NuGet package
gh run download RUN_ID -n nuget-package

# List package contents
unzip -l nuget-package/HELIOS.Platform.*.nupkg
```

#### Manual NuGet Publish
```bash
# If automatic publish fails, publish manually
dotnet nuget push "*.nupkg" --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY --skip-duplicate
```

---

## 3. Execute Analysis Workflow

### Trigger Analysis

#### Manual Trigger
```bash
# Run analysis immediately
gh workflow run analysis.yml

# This will generate metrics report and validate JSON
```

#### Scheduled Run
```bash
# Analysis runs automatically:
# - On schedule: Weekly (Sunday 00:00 UTC)
# - On push to: COMPONENT_ANALYSIS.md, COMPONENT_METRICS.json
# - On workflow file change

# View scheduled runs
gh workflow view analysis.yml
```

### Monitor Analysis

#### Check Analysis Results
```bash
# Get latest analysis run
RUN_ID=$(gh workflow view analysis.yml --json runs --jq '.runs[0].databaseId')

# View status
gh run view $RUN_ID

# Get analysis report
gh run download $RUN_ID -n analysis-report
cat analysis-report/analysis-report.txt
```

#### Validate Metrics
```bash
# Python validation of metrics
python3 -c "
import json
with open('COMPONENT_METRICS.json') as f:
    metrics = json.load(f)
print(f'Components: {len(metrics.get(\"components\", {}))}')
print(f'Phases: {len(metrics.get(\"phases\", {}))}')
print(f'Total Time: {sum(p.get(\"time\", 0) for p in metrics.get(\"phases\", {}).values())} minutes')
"
```

---

## 4. Execute Quality Workflow

### Trigger Code Quality Checks

#### Automatic Trigger
```bash
# Quality checks run on:
# - Push to main or develop
# - Pull request to main

# No manual action required
```

#### Manual Trigger
```bash
# Force quality check run
gh workflow run quality.yml

# This will run all linters
```

### Monitor Quality Results

#### Check Linting Status
```bash
# Get latest quality run
RUN_ID=$(gh workflow view quality.yml --json runs --jq '.runs[0].databaseId')

# View all jobs
gh run view $RUN_ID --json jobs

# Check specific job status
gh run view $RUN_ID --json jobs --jq '.jobs[] | select(.name | contains("PowerShell"))'
```

#### Download Linting Reports
```bash
# Download all analyses
gh run download $RUN_ID

# View PowerShell analysis
cat powershell-analysis/ps-analysis.txt

# Check Markdown linting
ls -la markdown-lint/
```

#### Fix Issues
```bash
# PowerShell issues
pwsh -Command "Invoke-ScriptAnalyzer -Path './src', './scripts' -Recurse -Fix"

# JSON validation
python3 -m json.tool COMPONENT_METRICS.json > COMPONENT_METRICS.json

# Markdown linting (if using markdownlint-cli2)
markdownlint-cli2 "*.md"
```

---

## 5. Execute Verify Workflow

### Trigger Verification

#### Automatic Verification
```bash
# Verify runs automatically:
# - Every 6 hours (0 */6 * * *)
# - Manual trigger anytime

# View scheduled runs
gh workflow view verify.yml
```

#### Manual Health Check
```bash
# Run verification immediately
gh workflow run verify.yml

# This will:
# 1. Check repository health
# 2. Validate all scripts
# 3. Verify metrics JSON
# 4. Generate status report
```

### Monitor Verification

#### Check Verification Status
```bash
# Get latest verification run
RUN_ID=$(gh workflow view verify.yml --json runs --jq '.runs[0].databaseId')

# View complete status
gh run view $RUN_ID

# Watch real-time progress
gh run watch $RUN_ID

# Get full logs
gh run view $RUN_ID --log
```

#### Download Health Report
```bash
# Download the health report
gh run download $RUN_ID -n health-report

# View report
cat health-report/health-report.md
```

#### Verify Components
```bash
# Check critical files
for file in COMPONENT_ANALYSIS.md COMPONENT_METRICS.json PROJECT_BOARD_QUICK_START.md; do
  if [ -f "$file" ]; then
    echo "✅ $file exists"
  else
    echo "❌ $file missing"
  fi
done

# Check deployment scripts
for i in {0..6}; do
  script="src/phases/phase-$i-*.ps1"
  if [ -f $script ]; then
    echo "✅ Phase $i script found"
  else
    echo "❌ Phase $i script missing"
  fi
done
```

---

## Advanced Commands

### Get Workflow Summary
```bash
# List all workflows with their status
gh workflow list

# Get detailed workflow info
gh workflow view deploy.yml

# View workflow file content
gh workflow view deploy.yml --yaml
```

### Monitor Multiple Runs
```bash
# Get all runs across workflows
gh run list --json status,conclusion,name,updatedAt --limit 20

# Filter by workflow
gh run list --workflow deploy.yml --limit 10

# Filter by status
gh run list --status in_progress
gh run list --status completed
```

### Download All Artifacts from Run
```bash
# Create directory and download
mkdir -p artifacts
gh run download RUN_ID -D artifacts

# List all artifacts
ls -la artifacts/
```

### Cancel Running Workflow
```bash
# If workflow is stuck
gh run cancel RUN_ID

# Verify cancellation
gh run view RUN_ID
```

### Rerun Failed Workflow
```bash
# Rerun specific workflow
gh run rerun RUN_ID

# Rerun with verbose logging
gh run rerun RUN_ID --debug
```

---

## Troubleshooting Commands

### Check Workflow Syntax
```bash
# Validate YAML syntax
python3 -c "
import yaml
workflows = [
    '.github/workflows/deploy.yml',
    '.github/workflows/nuget.yml',
    '.github/workflows/analysis.yml',
    '.github/workflows/quality.yml',
    '.github/workflows/verify.yml'
]
for wf in workflows:
    try:
        with open(wf) as f:
            yaml.safe_load(f)
        print(f'✅ {wf} is valid')
    except yaml.YAMLError as e:
        print(f'❌ {wf} syntax error: {e}')
"
```

### Verify Secrets Configuration
```bash
# Check if secrets are set (requires GitHub CLI with repo admin)
gh secret list

# Add/update secrets (interactive)
gh secret set AZURE_SUBSCRIPTION_ID
gh secret set AZURE_TENANT_ID
gh secret set AZURE_CLIENT_ID
gh secret set AZURE_CLIENT_SECRET
gh secret set NUGET_API_KEY
```

### Local Workflow Testing
```bash
# Install Act (runs workflows locally)
# macOS: brew install act
# Windows: choco install act-cli

# Run workflow locally
act -j build

# Run specific workflow
act -W .github/workflows/deploy.yml

# Run with input
act workflow_dispatch -i
```

---

## CI/CD Pipeline Workflow

### Typical Development Workflow
```bash
# 1. Make changes
git checkout -b feature/my-feature
# ... make changes ...

# 2. Commit and push
git add .
git commit -m "Add new feature"
git push origin feature/my-feature

# 3. Quality checks run automatically
gh run list --workflow quality.yml

# 4. Create pull request
gh pr create --title "Add new feature" --body "Description"

# 5. Verify deployment workflow passes
gh run list --workflow deploy.yml

# 6. Merge when ready
gh pr merge --auto --squash

# 7. Publish release
git tag v1.0.1
git push origin v1.0.1

# 8. NuGet publishes automatically
gh run list --workflow nuget.yml
```

---

## Performance & Optimization

### Parallel Execution
- Deploy workflow runs phases sequentially (waits for dependencies)
- Agent fleet deploys with max 3 parallel (agent job)
- Quality checks run all jobs in parallel

### Artifact Management
```bash
# Set artifact retention
# Settings → Actions → General → Artifacts retention → set days

# Cleanup old artifacts (requires script)
gh api repos/OWNER/REPO/actions/artifacts \
  --paginate --jq '.artifacts[] | "\(.id) - \(.name) - \(.created_at)"'
```

### Cost Optimization
- Ubuntu runners: Free for public repos
- Windows runners: 2x cost
- Limit workflow frequency
- Set appropriate retention policies

---

## Common Patterns

### Deploy to Production
```bash
# 1. Tag release
git tag v1.0.0
git push origin v1.0.0

# 2. NuGet publish (automatic)
gh run list --workflow nuget.yml --status completed

# 3. Full deployment
gh workflow run deploy.yml -f phase=all

# 4. Monitor progress
gh run watch $(gh workflow view deploy.yml --json runs --jq '.runs[0].databaseId')

# 5. Verify health
gh run wait $(gh workflow view verify.yml --json runs --jq '.runs[0].databaseId')
```

### Emergency Rollback
```bash
# 1. Identify failed run
gh run list --workflow deploy.yml --status failure

# 2. Check logs
gh run view RUN_ID --log

# 3. Fix issues
# ... make corrections ...

# 4. Rerun or push new tag
git tag v1.0.1
git push origin v1.0.1
```

---

## Success Indicators

### Successful Deploy Workflow
```
✅ Preflight: All checks passed
✅ Infrastructure: Deployed to Azure
✅ Agents: 6 agents running healthy
✅ AI Services: 6+ services initialized
✅ Security: 8 layers enabled
✅ Monitoring: 7 dashboards created
✅ Verification: 42/42 tests passed
✅ Go-Live: AUTHORIZED
```

### Successful Quality Checks
```
✅ PowerShell: No errors/warnings
✅ Markdown: All files valid
✅ JSON: All files valid
✅ Security: No vulnerabilities
```

### Successful Verification
```
✅ Repository health: All critical files present
✅ Metrics: Valid JSON, data consistent
✅ Scripts: All 8 deployment scripts present
✅ Status: OPERATIONAL
```

---

## Reference URLs

### GitHub Actions Documentation
- https://docs.github.com/en/actions
- https://docs.github.com/en/actions/using-workflows

### GitHub CLI Reference
- https://cli.github.com
- https://docs.github.com/en/github-cli/github-cli/using-github-cli

### HELIOS Repository
- Main: `https://github.com/yourusername/helios-platform-repo`
- Releases: `https://github.com/yourusername/helios-platform-repo/releases`
- Workflows: `https://github.com/yourusername/helios-platform-repo/actions`

