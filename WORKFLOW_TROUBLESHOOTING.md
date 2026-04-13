# HELIOS Platform - Workflow Troubleshooting Guide

## Table of Contents
1. [Common Issues](#common-issues)
2. [Deploy Workflow Issues](#deploy-workflow-issues)
3. [NuGet Workflow Issues](#nuget-workflow-issues)
4. [Analysis Workflow Issues](#analysis-workflow-issues)
5. [Quality Workflow Issues](#quality-workflow-issues)
6. [Verify Workflow Issues](#verify-workflow-issues)
7. [Debugging Techniques](#debugging-techniques)
8. [Logs & Diagnostics](#logs--diagnostics)

---

## Common Issues

### Issue: Workflow Not Triggering

**Symptoms**
- Workflow doesn't start when pushing code
- Manual trigger shows "Create a workflow run"
- No runs visible in Actions tab

**Solutions**

1. **Verify Workflow File Syntax**
```bash
# Validate YAML
python3 -c "
import yaml
with open('.github/workflows/deploy.yml') as f:
    yaml.safe_load(f)
print('✅ YAML is valid')
"
```

2. **Check Branch Configuration**
```bash
# Verify push branch matches workflow trigger
git branch -a
git status

# For deploy.yml: must push to main or develop
# For nuget.yml: must push to main with tag
# For quality.yml: must push to main or develop
```

3. **Enable Workflow**
```bash
# Go to: Settings → Actions → General
# Ensure: "Actions permissions" = "Allow all actions"

# Or via CLI (requires admin token)
gh repo edit --enable-discussions
```

4. **Check File Path Triggers**
```bash
# Analysis workflow only triggers on specific paths
# Ensure you're changing:
# - COMPONENT_ANALYSIS.md
# - COMPONENT_METRICS.json
# - .github/workflows/analysis.yml
```

5. **Push to Correct Branch**
```bash
# Create feature branch from correct base
git checkout -b feature/my-feature main
git push origin feature/my-feature
```

---

### Issue: Workflow Fails Immediately

**Symptoms**
- Red X on workflow run
- "Failed" status
- Workflow doesn't enter any job steps

**Solutions**

1. **Check Workflow Syntax**
```bash
# Act can validate locally
act -l  # List workflows
act --dry-run  # Dry run
```

2. **Review Workflow Errors**
```bash
# Get detailed error
gh run view RUN_ID --log

# Look for: "Error: Unexpected input..."
# This usually means invalid trigger parameters
```

3. **Verify Required Fields**
```yaml
# Each job must have:
jobs:
  job_name:
    name: Job Display Name  # Required
    runs-on: windows-latest  # Required
    steps:
      - uses: actions/checkout@v4  # Usually required
```

---

### Issue: "Secrets Not Found" Error

**Symptoms**
- Error: `Invalid reference to undefined secret`
- Workflow fails in steps using secrets
- Azure authentication fails

**Solutions**

1. **Add Secrets to Repository**
```bash
# For deploy.yml, add these secrets:
gh secret set AZURE_SUBSCRIPTION_ID
gh secret set AZURE_TENANT_ID
gh secret set AZURE_CLIENT_ID
gh secret set AZURE_CLIENT_SECRET

# For nuget.yml:
gh secret set NUGET_API_KEY

# Verify secrets exist
gh secret list
```

2. **Check Secret Reference Syntax**
```yaml
# Correct syntax
env:
  SECRET_VAR: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

# Wrong syntax (won't work)
env:
  SECRET_VAR: ${AZURE_SUBSCRIPTION_ID}
  SECRET_VAR: $AZURE_SUBSCRIPTION_ID
```

3. **Case Sensitivity**
```yaml
# Secrets are case-sensitive
AZURE_CLIENT_ID       # Correct
azure_client_id       # Wrong
Azure_Client_Id       # Wrong
```

4. **Verify Secret Values**
```bash
# List secret names (not values)
gh secret list

# To update secret
gh secret set SECRET_NAME
# (paste new value when prompted)
```

---

### Issue: Artifact Not Found

**Symptoms**
- "Artifact not found" when downloading
- Can't find artifact in workflow run
- Upload failed silently

**Solutions**

1. **Check Upload Syntax**
```yaml
# Correct syntax
- uses: actions/upload-artifact@v3
  with:
    name: artifact-name
    path: ./path/to/files

# Common mistakes
- path must exist before upload
- name is required
- path supports wildcards
```

2. **Verify Path Exists**
```bash
# Before upload, ensure path exists
if [ -d "./nupkg" ]; then
  echo "✅ Directory exists"
else
  echo "❌ Directory not found"
fi

# Check files
ls -la ./nupkg/
```

3. **Download Artifact**
```bash
# List available artifacts
gh run view RUN_ID --json artifacts

# Download specific artifact
gh run download RUN_ID -n artifact-name

# Download all
gh run download RUN_ID
```

---

## Deploy Workflow Issues

### Issue: Azure Authentication Fails

**Error Message**
```
Error: One or more errors occurred. (AADSTS700016: Application with identifier 
'...' was not found in the directory)
```

**Solutions**

1. **Verify Azure Credentials**
```bash
# Test locally first
$securePassword = ConvertTo-SecureString "YOUR_SECRET" -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential (
  "YOUR_CLIENT_ID", 
  $securePassword
)
Connect-AzAccount -ServicePrincipal -Credential $credential -TenantId "YOUR_TENANT_ID"
```

2. **Check Service Principal**
```bash
# Verify service principal exists in Azure
# Azure Portal → Azure Active Directory → App registrations
# Or via Azure CLI:
az ad sp list --filter "appId eq 'YOUR_CLIENT_ID'"
```

3. **Verify Secret Values**
```bash
# Secrets must be exact values from Azure
# Get them from:
# 1. Azure Portal → App registrations
# 2. Certificates & secrets → Client secrets
# 3. Overview tab → Application (client) ID, Directory (tenant) ID
```

4. **Check Subscription Access**
```bash
# Service principal needs Contributor role
# Azure Portal → Subscriptions → Access control (IAM)
# Add Role Assignment → Contributor → Select your app
```

---

### Issue: Phase Fails - Previous Phase Had Issues

**Symptoms**
- Phase doesn't run with status "skipped"
- Job shows `needs` with red X
- Chain breaks at a specific phase

**Solutions**

1. **Check Phase Dependencies**
```yaml
# Each phase depends on previous success:
infrastructure:
  needs: preflight  # Must pass

agents:
  needs: infrastructure  # Must pass

# If preflight fails, infrastructure is skipped
```

2. **Debug Failed Phase**
```bash
# Get logs for specific job
gh run view RUN_ID --json jobs --jq '.jobs[] | select(.name | contains("Pre-flight"))'

# Get full log output
gh run view RUN_ID --log --name "Pre-flight Checks"
```

3. **Fix Phase Issues**
```bash
# Read the error message carefully
# Common issues:
# - PowerShell syntax error
# - Missing file/directory
# - Authentication failed
# - Timeout (increase timeout in workflow)
```

---

### Issue: Deployment Times Out

**Symptoms**
- Workflow cancelled after 6 hours
- Status: "Cancelled"
- Message: "The operation was cancelled"

**Solutions**

1. **Optimize Deployment Time**
```yaml
# In workflow file:
jobs:
  long_task:
    timeout-minutes: 120  # Increase from default 360

# Reduce parallel tasks
strategy:
  matrix:
    agent: [...]
  max-parallel: 2  # Reduce parallelization
```

2. **Stream Logs During Deployment**
```bash
# Monitor in real-time
gh run watch RUN_ID --exit-status

# Shows progress as it happens
```

3. **Break Into Smaller Jobs**
```yaml
# Instead of one massive job, split:
job1:
  name: Task A
  # ...

job2:
  needs: job1
  name: Task B
  # ...
```

---

## NuGet Workflow Issues

### Issue: Package Build Fails

**Symptoms**
- Build job fails
- Error in "Build package" step
- NuGet package not created

**Solutions**

1. **Check .NET Project File**
```bash
# Verify HELIOS.Platform.csproj exists
ls -la HELIOS.Platform.csproj

# Check project syntax
dotnet restore
dotnet build
```

2. **Verify Dependencies**
```bash
# Restore NuGet dependencies
dotnet restore

# Look for error messages
# Common issues:
# - Missing NuGet package source
# - Version conflicts
# - Offline build
```

3. **Check Build Configuration**
```bash
# Ensure Release configuration works
dotnet build --configuration Release

# Try locally:
dotnet pack --configuration Release --output ./nupkg
```

4. **Review Workflow Build Step**
```yaml
# Verify step matches project structure:
- name: Build package
  run: |
    dotnet build --configuration Release
    dotnet pack --configuration Release --output ./nupkg
```

---

### Issue: NuGet Publish Fails

**Symptoms**
- Build succeeds, publish fails
- Error: "API key invalid" or "Package already exists"
- Release not created

**Solutions**

1. **Verify NuGet API Key**
```bash
# Check secret is set
gh secret list | grep NUGET_API_KEY

# Update if expired
gh secret set NUGET_API_KEY
# (Paste new key from nuget.org)
```

2. **Check Package Version**
```bash
# Version must be unique or have --skip-duplicate
# In workflow:
dotnet nuget push "*.nupkg" \
  --source https://api.nuget.org/v3/index.json \
  --api-key ${{ secrets.NUGET_API_KEY }} \
  --skip-duplicate
```

3. **Verify Tag Format**
```bash
# Publish only triggers on tags matching v*
git tag v1.0.0          # ✅ Correct
git tag 1.0.0           # ❌ Won't trigger
git tag release-1.0.0   # ❌ Won't trigger

# Push tag
git push origin v1.0.0
```

---

### Issue: Release Notes Not Generated

**Symptoms**
- GitHub Release created but empty
- Body text missing
- Date shows as variable, not actual date

**Solutions**

1. **Fix Release Body Syntax**
```yaml
# Replace $(Get-Date) with actual date
body: |
  ## HELIOS Platform Release
  **Version:** ${{ env.PACKAGE_VERSION }}
  **Date:** January 15, 2024
  # Don't use PowerShell in YAML!
```

2. **Test Release Creation**
```bash
# Manual test before workflow
gh release create v1.0.0 \
  --title "Release v1.0.0" \
  --notes "Release notes here"
```

---

## Analysis Workflow Issues

### Issue: JSON Validation Fails

**Symptoms**
- Workflow fails in "Validate Metrics JSON" step
- Error: "JSON decode error"
- Invalid JSON message

**Solutions**

1. **Validate JSON Locally**
```bash
# Check JSON syntax
python3 -m json.tool COMPONENT_METRICS.json

# Or use jq
jq . COMPONENT_METRICS.json

# Common JSON errors:
# - Trailing comma: { "key": "value", }  ❌
# - Single quotes: { 'key': 'value' }    ❌
# - Unquoted keys: { key: "value" }       ❌
```

2. **Fix JSON Issues**
```bash
# Use Python to reformat
python3 -c "
import json
with open('COMPONENT_METRICS.json') as f:
    data = json.load(f)
with open('COMPONENT_METRICS.json', 'w') as f:
    json.dump(data, f, indent=2)
print('✅ JSON fixed')
"
```

3. **Check Required Fields**
```json
{
  "components": {},
  "phases": {},
  "metrics": {
    "performance": {},
    "security": {}
  }
}
```

---

### Issue: Analysis Report Empty

**Symptoms**
- analysis-report.txt created but empty
- No metrics printed
- Command output not captured

**Solutions**

1. **Fix Python Script**
```python
# Use explicit print statements
print('📊 HELIOS Component Analysis Report')
print(f'Components: {len(data)}')

# Not this:
# Just: len(data)
```

2. **Ensure File Outputs**
```yaml
# Make sure output is written
- name: Generate Report
  run: |
    python3 << 'EOF'
    print('Report content')
    EOF
    
# Write to file explicitly
    > report.txt
```

---

## Quality Workflow Issues

### Issue: PSScriptAnalyzer Errors

**Symptoms**
- "Module not found" error
- PSScriptAnalyzer not installed
- Invoke-ScriptAnalyzer command not found

**Solutions**

1. **Verify Installation**
```powershell
# Install PSScriptAnalyzer
pwsh -Command "Install-Module -Name PSScriptAnalyzer -Force -SkipPublisherCheck"

# Verify installation
pwsh -Command "Get-Module PSScriptAnalyzer"

# Update if needed
pwsh -Command "Update-Module -Name PSScriptAnalyzer -Force"
```

2. **Run Analyzer Correctly**
```powershell
# Correct syntax
Invoke-ScriptAnalyzer -Path './src' -Recurse

# With results
$results = Invoke-ScriptAnalyzer -Path './src' -Recurse
$results | Format-Table -AutoSize

# Export to file
Invoke-ScriptAnalyzer -Path './src' -Recurse | Tee-Object -FilePath analysis.txt
```

3. **Fix Script Issues**
```powershell
# Common PSScriptAnalyzer issues:
# - Missing [Parameter()] attributes
# - Not using approved verbs
# - Missing help documentation
# - Using aliases instead of full names

# Auto-fix some issues
Invoke-ScriptAnalyzer -Path './src' -Recurse -Fix
```

---

### Issue: Markdown Linter Failures

**Symptoms**
- Markdown linting fails
- Message about file formatting
- Lint configuration not found

**Solutions**

1. **Create Linting Config**
```json
// .markdownlint.json
{
  "extends": "markdownlint/style/relaxed",
  "no-hard-tabs": false,
  "line-length": false,
  "no-trailing-spaces": true
}
```

2. **Fix Common Markdown Issues**
```markdown
# Correct markdown

## Headings
- Proper spacing after #
- Don't skip levels (go ## → ### not ## → ####)

Paragraphs
- Blank line between sections
- Consistent list markers (- or * or +, not mixed)

* Lists
  * Proper indentation
  * 2 spaces per level
```

3. **Run Linter Locally**
```bash
# Install markdownlint
npm install -g markdownlint-cli

# Run locally
markdownlint '*.md'

# Fix automatically
markdownlint --fix '*.md'
```

---

### Issue: Super Linter Security Scan Fails

**Symptoms**
- Workflow fails with many errors
- "Too many issues found"
- Unrelated code flagged

**Solutions**

1. **Update Linter Configuration**
```yaml
- uses: github/super-linter/slim@v4
  env:
    DEFAULT_BRANCH: main
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    # Add these to exclude
    FILTER_REGEX_EXCLUDE: node_modules|\.nuget
```

2. **Create Linter Ignore Files**
```
# .gitignore to exclude directories
node_modules/
.nuget/
bin/
obj/
```

---

## Verify Workflow Issues

### Issue: Health Check Fails - Missing Files

**Symptoms**
- Verify workflow fails
- Message: "File not found"
- Status: "MISSING" for critical files

**Solutions**

1. **Check Required Files**
```bash
# Verify all expected files exist
files=(
  "COMPONENT_ANALYSIS.md"
  "COMPONENT_METRICS.json"
  "PROJECT_BOARD_QUICK_START.md"
  "GITHUB_PROJECT_SETUP.md"
  "DELIVERY_MANIFEST.md"
)

for file in "${files[@]}"; do
  if [ -f "$file" ]; then
    echo "✅ $file"
  else
    echo "❌ $file MISSING"
  fi
done
```

2. **Create Missing Files**
```bash
# Create minimal versions
echo "# Component Analysis" > COMPONENT_ANALYSIS.md
echo '{"components":{}, "phases":{}}' > COMPONENT_METRICS.json
echo "# Project Board" > PROJECT_BOARD_QUICK_START.md
```

3. **Verify Script Paths**
```bash
# Check deployment scripts exist
for phase in {0..6}; do
  find . -name "phase-$phase-*.ps1" -type f
done
```

---

### Issue: Metrics Validation Shows Inconsistent Data

**Symptoms**
- JSON valid but data inconsistent
- Component count mismatch
- Phase count incorrect

**Solutions**

1. **Audit Metrics File**
```bash
# Check structure
python3 << 'EOF'
import json

with open('COMPONENT_METRICS.json') as f:
    data = json.load(f)

print(f"Components: {len(data.get('components', {}))}")
print(f"Phases: {len(data.get('phases', {}))}")

# List all components
print("\nComponents:")
for comp in data.get('components', {}):
    print(f"  - {comp}")

# List all phases
print("\nPhases:")
for phase in data.get('phases', {}):
    print(f"  - {phase}")
EOF
```

2. **Update Metrics Data**
```json
{
  "components": {
    "storage": "Storage Agent",
    "security": "Security Agent",
    "software": "Software Agent",
    "gui": "GUI Agent",
    "optimization": "Optimization Agent",
    "testing": "Testing Agent"
  },
  "phases": {
    "preflight": {"time": 5},
    "infrastructure": {"time": 20},
    "agents": {"time": 15},
    "ai-services": {"time": 10},
    "security": {"time": 15},
    "monitoring": {"time": 10},
    "verification": {"time": 15}
  }
}
```

---

## Debugging Techniques

### Technique 1: View Full Logs

```bash
# Get complete log for workflow run
gh run view RUN_ID --log > full-logs.txt

# View in pager
gh run view RUN_ID --log | less

# Search for errors
gh run view RUN_ID --log | grep -i error

# Search for specific step
gh run view RUN_ID --log | grep -A 20 "Run Deploy Infrastructure"
```

### Technique 2: Run Locally with Act

```bash
# Install act (local workflow runner)
# https://github.com/nektos/act

# Run workflow locally
act -j preflight

# Run with debug logging
act -j preflight --verbose

# Run with input variables
act workflow_dispatch -i

# Test specific workflow
act -W .github/workflows/deploy.yml
```

### Technique 3: Add Debug Logging

```yaml
# Add to workflow for debugging
- name: Debug Info
  run: |
    echo "GitHub Context:"
    echo "Event: ${{ github.event_name }}"
    echo "Ref: ${{ github.ref }}"
    echo "Branch: ${{ github.ref_name }}"
    echo "Actor: ${{ github.actor }}"
    
    # List environment variables
    echo "Environment:"
    env | sort
```

### Technique 4: Create Minimal Test

```bash
# Create test workflow
cat > .github/workflows/test.yml << 'EOF'
name: Test
on: workflow_dispatch

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run test
        run: echo "Test successful"
EOF

# Run it
gh workflow run test.yml
```

---

## Logs & Diagnostics

### Accessing Workflow Logs

#### Via GitHub CLI
```bash
# Real-time logs (requires active session)
gh run watch RUN_ID --exit-status

# Download complete logs
gh run view RUN_ID --log > logs.txt

# Get logs for specific job
gh run view RUN_ID --log --name "Job Name" > job-logs.txt

# Get logs for specific step
gh run view RUN_ID --log | grep -A 50 "Run Deploy Infrastructure"
```

#### Via GitHub Web UI
1. Go to: `Actions` → Select workflow
2. Click on run ID
3. Select job
4. Expand step → view full output
5. Or download logs (.zip)

### Analyzing Logs

```bash
# Search for errors
grep -i "error\|fail\|exception" logs.txt

# Find timestamps
grep "^20" logs.txt

# Get context around error
grep -B 5 -A 5 "error" logs.txt

# Count occurrences
grep -c "error" logs.txt

# Parse PowerShell errors
grep "^VERBOSE\|^WARNING\|^ERROR" logs.txt
```

### Diagnostic Information to Collect

When reporting issues, gather:
```bash
# 1. Workflow file
cat .github/workflows/WORKFLOW_NAME.yml

# 2. Full logs
gh run view RUN_ID --log

# 3. Environment info
gh run view RUN_ID --json

# 4. Artifacts status
gh run view RUN_ID --json artifacts

# 5. Job status
gh run view RUN_ID --json jobs

# All in one
gh run view RUN_ID --json jobs,status,conclusion > diagnostics.json
```

---

## Quick Reference: Common Fixes

| Issue | Quick Fix |
|-------|-----------|
| Workflow not running | Check branch matches trigger, enable actions |
| Secrets not found | Add via `gh secret set`, check syntax |
| JSON errors | Validate with `python3 -m json.tool` |
| PowerShell errors | Check syntax with `pwsh -NoProfile -Command` |
| Timeout | Increase `timeout-minutes` in job config |
| Artifact not found | Verify path exists before upload, check name |
| Permission denied | Check permissions, use personal token, verify runner |
| Phase skipped | Check previous phase passed, verify dependencies |
| Local testing | Use `act` to run workflows locally |

---

## Getting Help

### Check Official Documentation
- GitHub Actions: https://docs.github.com/en/actions
- GitHub CLI: https://cli.github.com
- HELIOS Repository: Check README.md and related guides

### Debug Commands
```bash
# Validate configuration
gh workflow view WORKFLOW_NAME --yaml

# Check runner status
gh run list --limit 5

# Get detailed run info
gh run view RUN_ID --json

# Download and inspect
gh run download RUN_ID
ls -la artifacts/
```

### Report Issues
When reporting workflow issues, include:
1. Workflow name and run ID
2. Full error message from logs
3. YAML configuration
4. Recent commits that changed workflow
5. Current branch and base branch

