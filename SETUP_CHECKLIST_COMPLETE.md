# SETUP_CHECKLIST_COMPLETE.md

# HELIOS Platform - Complete Setup Checklist
**End-to-End Configuration Guide for Production Deployment**

**Version:** 1.0.0  
**Last Updated:** April 2026  
**Audience:** DevOps, Infrastructure, Operations Teams  

---

## 📋 TABLE OF CONTENTS

1. [Pre-Setup Prerequisites](#pre-setup-prerequisites)
2. [GitHub Configuration](#github-configuration)
3. [Azure Configuration](#azure-configuration)
4. [Local Development Setup](#local-development-setup)
5. [Deployment Execution](#deployment-execution)
6. [Post-Deployment Verification](#post-deployment-verification)
7. [Success Criteria](#success-criteria)

---

## 🚀 PRE-SETUP PREREQUISITES

### System Requirements Checklist

**Hardware:**
- [ ] CPU: 4 cores minimum (8 recommended)
- [ ] Memory: 4 GB minimum (8 GB recommended)
- [ ] Storage: 10 GB free disk space
- [ ] Network: 10 Mbps minimum internet speed
- [ ] Firewall: Outbound HTTPS (443) allowed

**Operating System:**
- [ ] Windows 10 (Build 19041+) OR Windows 11
- [ ] OR WSL2 with Ubuntu 20.04+
- [ ] OR macOS 11+
- [ ] System updated to latest patches

**Software Requirements:**
- [ ] Git 2.35+ installed
- [ ] PowerShell 5.1+ or pwsh 7.0+
- [ ] .NET SDK 8.0+ (for building)
- [ ] Azure CLI 2.40+ installed
- [ ] Docker (optional, for containers)

**Verification Steps:**
```powershell
# Check each requirement
$checks = @{
    'PowerShell' = $PSVersionTable.PSVersion.Major
    'Git' = (git --version).Split()[2]
    'Azure CLI' = (az --version).Split()[0]
    'Disk Space' = (Get-Volume | Where-Object DriveLetter -EQ C).SizeRemaining / 1GB
    'Memory' = (Get-ComputerInfo).CsTotalPhysicalMemory / 1GB
}

foreach ($check in $checks.GetEnumerator()) {
    Write-Host "$($check.Key): $($check.Value)"
}
```

**✅ Checklist Item:** All system requirements verified

---

### Account & Access Requirements

**GitHub Access:**
- [ ] Active GitHub account (free, pro, or enterprise)
- [ ] Access to M0nado/helios-platform repository
- [ ] Ability to create personal access tokens
- [ ] Repository admin access (for secrets)

**Azure Access:**
- [ ] Active Azure subscription
- [ ] Subscription owner or contributor role
- [ ] Ability to create service principals
- [ ] Ability to create resource groups
- [ ] Ability to manage Key Vault

**NuGet Access (for publishing):**
- [ ] Active NuGet.org account (if publishing)
- [ ] API key generated
- [ ] Publishing permissions confirmed

**Verification Steps:**
```powershell
# GitHub
gh auth status

# Azure
az login
az account show

# Verify roles
az role assignment list --include-inherited
```

**✅ Checklist Item:** All access requirements verified

---

## 🔧 GITHUB CONFIGURATION

### Repository Setup

**Step 1: Clone Repository**
```bash
# Clone with all submodules
git clone --recurse-submodules https://github.com/M0nado/helios-platform.git
cd helios-platform

# Verify submodules
git submodule status
```

**Expected Output:**
```
 modules/helios-monado-blade (main)
 modules/helios-security-setup (main)
 modules/helios-ai-hub (main)
 modules/helios-dev-ai-hub (main)
 modules/helios-build-agents (main)
 modules/helios-gui-framework (main)
 modules/helios-software-stack (main)
```

- [ ] Repository cloned successfully
- [ ] All 7 submodules present
- [ ] Working directory clean (git status shows nothing)

**Step 2: Verify Git Configuration**
```powershell
git config --global user.name
git config --global user.email

# If not set:
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

- [ ] Git user name configured
- [ ] Git user email configured
- [ ] Credential manager working

**✅ Checklist Item:** Repository setup complete

---

### GitHub Secrets Configuration (⚠️ CRITICAL)

**Step 1: Generate Required Values**

**For Azure Authentication:**
```powershell
# If you don't have a service principal, create one:
az ad sp create-for-rbac --name "helios-platform-sp" --role "Contributor" --scopes "/subscriptions/{subscriptionId}"

# Output will include:
# - clientId (AZURE_CLIENT_ID)
# - clientSecret (AZURE_CLIENT_SECRET)
# - tenantId (AZURE_TENANT_ID)
# - subscriptionId (AZURE_SUBSCRIPTION_ID)
```

**For NuGet:**
```
1. Go to https://www.nuget.org/account/ApiKeys
2. Create new key
3. Copy and save (NUGET_API_KEY)
```

**Step 2: Add Secrets to GitHub**

Navigate to: **Settings → Secrets and variables → Actions**

**Add these 5 secrets:**

| Secret Name | Value | Notes |
|---|---|---|
| AZURE_SUBSCRIPTION_ID | xxxxx-xxxxx-xxxxx | From `az account show` |
| AZURE_TENANT_ID | xxxxx-xxxxx-xxxxx | From service principal output |
| AZURE_CLIENT_ID | xxxxx-xxxxx-xxxxx | From service principal output |
| AZURE_CLIENT_SECRET | xxxxxx | From service principal output (⚠️ Save securely) |
| NUGET_API_KEY | xxxxxx | From NuGet.org (⚠️ Save securely) |

**Verification:**
```bash
# Test GitHub authentication
gh auth status

# Test secret access (will show masked)
gh secret list
```

Expected to see all 5 secrets listed.

- [ ] AZURE_SUBSCRIPTION_ID added
- [ ] AZURE_TENANT_ID added
- [ ] AZURE_CLIENT_ID added
- [ ] AZURE_CLIENT_SECRET added
- [ ] NUGET_API_KEY added
- [ ] All secrets verified in GitHub UI

**✅ Checklist Item:** GitHub secrets configured

---

### GitHub Workflows Enablement

**Step 1: Enable Actions**

Navigate to: **Settings → Actions → General**

- [ ] Actions enabled
- [ ] Allow all actions and reusable workflows
- [ ] Set retention to 30 days

**Step 2: Verify Workflow Files**

```powershell
cd .github/workflows
ls -la *.yml

# Expected: 5 files
# - analysis.yml
# - deploy.yml
# - nuget.yml
# - quality.yml
# - verify.yml
```

- [ ] All 5 workflow files present
- [ ] No YAML syntax errors (check Actions page)
- [ ] Workflows visible in Actions tab

**Step 3: Test Workflow Trigger**

```bash
# Check workflow syntax
gh workflow list

# Expected output shows all 5 workflows with status "active"
```

- [ ] All 5 workflows show as active
- [ ] No workflow errors on Actions page

**Step 4: Configure Branch Protection (Optional)**

Navigate to: **Settings → Branches → main**

- [ ] Require status checks to pass before merging
- [ ] Require successful completion of:
  - [ ] HELIOS Platform Deployment Pipeline
  - [ ] HELIOS Platform Code Quality & Linting

**✅ Checklist Item:** GitHub workflows enabled

---

### GitHub Project Board Setup

Navigate to: **Projects → New project**

**Step 1: Create New Project**
- [ ] Name: "HELIOS Platform Main"
- [ ] Template: "Table" (start blank)
- [ ] Create

**Step 2: Add Custom Fields**

Open project → Settings → Custom fields

Add these 20+ fields:

**Basic Fields:**
- [ ] Priority (Single select: Critical, High, Medium, Low)
- [ ] Status (Status field: Backlog, In Progress, Review, Done)
- [ ] Phase (Single select: 0, 1, 2, 3, 4, 5, 6)
- [ ] Component (Single select: Monado, Security, AI Hub, Dev AI, Agents, GUI, Software Stack)

**Planning Fields:**
- [ ] Effort (Number: story points)
- [ ] Due Date (Date)
- [ ] Sprint (Single select: Sprint 1, Sprint 2, etc.)
- [ ] Owner (Person field)

**Tracking Fields:**
- [ ] Impact (Single select: High, Medium, Low)
- [ ] Risk (Single select: High, Medium, Low)
- [ ] Dependencies (Text)
- [ ] Related Issues (Text)

**Quality Fields:**
- [ ] Documentation (Checkbox)
- [ ] Testing (Checkbox)
- [ ] Security Review (Checkbox)
- [ ] Performance Impact (Text)

**Business Fields:**
- [ ] Tier (Single select: Professional, Enterprise, Ultimate)
- [ ] Cost (Currency)
- [ ] ROI (Percentage)
- [ ] Security Level (Single select: Public, Internal, Confidential, Secret)

**Step 3: Create Views**

- [ ] **Board View** - Columns: Backlog, In Progress, Review, Done
- [ ] **Table View** - All items with all fields sortable
- [ ] **Roadmap View** - Timeline by phase
- [ ] **Priority Queue** - Sorted by priority + effort
- [ ] **Component View** - Grouped by component
- [ ] **Phase Timeline** - Timeline by phase

**Step 4: Set Up Automation**

Project → Settings → Workflows

Add these automation rules:
- [ ] Auto-add issues to project when created
- [ ] Auto-update status when PR is merged
- [ ] Auto-close issues when 30 days in Done
- [ ] Auto-notify on status changes

- [ ] Custom fields added (20+)
- [ ] Views created (6)
- [ ] Automation rules configured (4)

**✅ Checklist Item:** Project board setup complete

---

### GitHub Pages Configuration

Navigate to: **Settings → Pages**

**Step 1: Enable Pages**
- [ ] Source: Deploy from a branch
- [ ] Branch: main
- [ ] Folder: / (root)
- [ ] Save

**Step 2: Verify Configuration**
- [ ] _config.yml present in repo
- [ ] index.md has Jekyll front matter
- [ ] Theme: Slate selected

**Step 3: Wait for Deployment**
- [ ] Wait 3-5 minutes
- [ ] Check "Your site is live at" URL
- [ ] Verify site loads

**Step 4: Custom Domain (Optional)**
- [ ] If using custom domain:
  - [ ] Add CNAME record to DNS
  - [ ] Add domain in Pages settings
  - [ ] Wait for HTTPS certificate

**Expected Result:**
```
Your site is published at https://m0nado.github.io/helios-platform
```

- [ ] Pages published successfully
- [ ] HTTPS enabled
- [ ] Site accessible

**✅ Checklist Item:** GitHub Pages configured

---

## ☁️ AZURE CONFIGURATION

### Subscription Setup

**Step 1: Verify Subscription**
```powershell
az account show

# Should display your subscription info
```

- [ ] Subscription accessible
- [ ] Owner or Contributor role confirmed
- [ ] Subscription ID noted

**Step 2: Create Resource Group** (Optional - scripts will create)
```powershell
az group create `
  --name "helios-platform-rg" `
  --location "eastus"
```

- [ ] Resource group created (or ready for scripts)

**✅ Checklist Item:** Subscription verified

---

### Service Principal Setup

**Step 1: Create Service Principal**
```powershell
$sp = az ad sp create-for-rbac --name "helios-platform-sp" `
  --role "Contributor" `
  --scope "/subscriptions/{subscriptionId}" | ConvertFrom-Json

# Save these values:
# $sp.clientId → AZURE_CLIENT_ID
# $sp.clientSecret → AZURE_CLIENT_SECRET  
# $sp.tenantId → AZURE_TENANT_ID
```

- [ ] Service principal created
- [ ] Client ID saved
- [ ] Client secret saved securely
- [ ] Tenant ID saved

**Step 2: Test Service Principal**
```powershell
az login --service-principal `
  -u {clientId} `
  -p {clientSecret} `
  --tenant {tenantId}

az account show  # Should work

az logout
```

- [ ] Service principal authentication tested
- [ ] Can access subscription

**✅ Checklist Item:** Service principal configured

---

### Azure Resource Quotas

**Step 1: Check Quotas**
```powershell
az vm list-usage --location eastus -o table

# Check:
# - vCPU quota
# - Storage accounts
# - Public IPs
# - Network interfaces
```

- [ ] vCPU quota: 10+ available
- [ ] Storage: No quota issues
- [ ] Networking: No quota issues

**Step 2: Request Quota Increase (if needed)**

Navigate to: **Azure Portal → Quotas**

- [ ] Request increases if any quota < 20

**✅ Checklist Item:** Azure quotas verified

---

## 💻 LOCAL DEVELOPMENT SETUP

### PowerShell Environment

**Step 1: Verify PowerShell Version**
```powershell
$PSVersionTable.PSVersion

# Required: 5.1 or higher
# Recommended: 7.x (PowerShell Core)
```

- [ ] PowerShell 5.1+ installed
- [ ] Latest version available

**Step 2: Set Execution Policy**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

- [ ] Execution policy set to RemoteSigned

**Step 3: Install Required Modules**
```powershell
# Azure modules
Install-Module -Name Az -AllowClobber -Force

# Script analysis
Install-Module -Name PSScriptAnalyzer -Force

# Verify
Get-Module -ListAvailable | grep -i Az, PSScriptAnalyzer
```

- [ ] Az module installed
- [ ] PSScriptAnalyzer installed
- [ ] Modules verified

**✅ Checklist Item:** PowerShell environment configured

---

### NuGet Package Installation (Optional)

**For using in projects:**
```powershell
# Install to project
dotnet add package HELIOS.Platform --version 1.0.0

# Or update existing
dotnet package update HELIOS.Platform
```

- [ ] Package installed (if using)
- [ ] Builds successfully

**For publishing (maintainers only):**
```powershell
# Set API key
dotnet nuget update source nuget.org -u __myname__ -p {NUGET_API_KEY}

# Build and push
dotnet pack --configuration Release
dotnet nuget push bin/Release/HELIOS.Platform.1.0.0.nupkg --source https://api.nuget.org/v3/index.json
```

- [ ] API key configured (if publishing)
- [ ] Package builds successfully

**✅ Checklist Item:** NuGet setup complete

---

## 🚀 DEPLOYMENT EXECUTION

### Pre-Deployment Checklist

Before running deployment, verify:

**Environment:**
- [ ] PowerShell admin console open
- [ ] Current directory: C:\helios-platform-repo (or your clone)
- [ ] Internet connected
- [ ] No proxies/VPNs blocking Azure access

**Credentials:**
- [ ] Azure CLI authenticated: `az account show` works
- [ ] GitHub authenticated: `gh auth status` works
- [ ] All 5 GitHub secrets configured

**Resources:**
- [ ] 10 GB free disk space
- [ ] No critical applications running
- [ ] System clock synchronized
- [ ] No active deployments in Azure

**Documentation:**
- [ ] Deployment guide reviewed
- [ ] Runbooks available
- [ ] Team trained
- [ ] Escalation contacts listed

- [ ] All pre-deployment items verified

---

### Deployment Phase Selection

**Professional Tier (50 min)** - Development/Testing
```powershell
.\master-deploy.ps1 -Tier Professional
```
Use if:
- [ ] Testing/POC environment
- [ ] Limited budget
- [ ] Staging deployment

**Enterprise Tier (80 min)** - Production Standard ✅ RECOMMENDED
```powershell
.\master-deploy.ps1 -Tier Enterprise
```
Use if:
- [ ] Production environment
- [ ] Need comprehensive features
- [ ] Standard SLA (99%)

**Ultimate Tier (112 min)** - Maximum Features
```powershell
.\master-deploy.ps1 -Tier Ultimate
```
Use if:
- [ ] Critical production
- [ ] Need 99.9% SLA
- [ ] Advanced features required

**Selected Tier: _________** (Professional / Enterprise / Ultimate)

---

### Deployment Execution

**Step 1: Start Deployment**

```powershell
# Navigate to repo
cd C:\helios-platform-repo

# Run deployment (replace tier with your choice)
.\master-deploy.ps1 -Tier Enterprise -Verbose
```

**Expected Output:**
```
🚀 HELIOS Platform Deployment Started
Tier: Enterprise
Environment: Production
Phases: 0-5
Estimated Duration: 80 minutes

Phase 0: Preflight Checks... [████████░░] 50%
Phase 1: Infrastructure.... [██████░░░░] 30%
... (continues)
```

- [ ] Deployment started successfully
- [ ] Console output shows progress
- [ ] No immediate errors

**Step 2: Monitor Deployment**

```powershell
# In a second window, monitor logs
Get-Content ./logs/deployment-progress.log -Wait

# Check phase status
Get-ChildItem ./logs/ | Sort-Object LastWriteTime -Descending | Select-Object Name, LastWriteTime -First 10
```

**Monitoring Frequency:**
- [ ] Check status every 10 minutes
- [ ] Watch for warnings/errors
- [ ] Note any manual interventions needed

**Step 3: Handle Interruptions**

If deployment stops or times out:

```powershell
# Option 1: Resume deployment
.\master-deploy.ps1 -Tier Enterprise -ResumeFromPhase 3

# Option 2: Run individual phase
.\scripts\phase-3-ai-services.ps1

# Option 3: Rollback and retry
.\scripts\rollback-deployment.ps1 -TargetPhase 2
.\master-deploy.ps1 -Tier Enterprise
```

- [ ] Deployment completes without critical errors
- [ ] Manual interventions documented
- [ ] Rollback not needed (or successfully recovered)

---

### Real-Time Monitoring

**Option A: GitHub Actions (Recommended)**
```bash
# In second terminal window
gh workflow run deploy.yml -r main -f tier=enterprise
gh run watch
```

**Option B: Local Monitoring**
```powershell
# Monitor deployment progress
Watch-Content ./logs/deployment-progress.log

# Monitor Azure resources
az resource list --resource-group helios-platform-rg -o table

# Monitor containers
az container list --resource-group helios-platform-rg -o table
```

**Key Metrics to Watch:**
- Phase completion time (should match estimates)
- Error messages (should be minimal)
- Resource creation (should see new resources in Azure)
- Agent health (should show RUNNING)

- [ ] Deployment progress monitored
- [ ] No critical errors during execution
- [ ] Completion time within estimate ±20%

---

## ✅ POST-DEPLOYMENT VERIFICATION

### Immediate Verification (5 minutes)

**Step 1: Check Deployment Status**
```powershell
# Review deployment report
Get-Content ./deployment-report.txt

# Check all phases completed
Get-ChildItem ./logs/ | Measure-Object | Select-Object Count

# Expected: 7-8 phase log files
```

- [ ] Deployment report generated
- [ ] All phase logs present
- [ ] No CRITICAL errors in any log

**Step 2: Verify Azure Resources**
```powershell
# List created resources
az resource list --resource-group helios-platform-rg --output table

# Expected: 20+ resources including:
# - Storage account
# - Virtual network
# - Key Vault
# - Container registries
# - Monitoring resources
```

- [ ] Resource group contains 20+ resources
- [ ] All core resources present
- [ ] No failed deployments

**Step 3: Check Dashboard Health**
```
Navigate to: Azure Portal → Resource Groups → helios-platform-rg

Visual Verification:
☑️ All resources show "Succeeded" status
☑️ No warning triangles (!)
☑️ No error icons (X)
```

- [ ] Azure Portal shows healthy resource group
- [ ] No failed deployments

---

### System Health Checks (10 minutes)

**Step 1: Agent Health**
```powershell
# Check each agent
$agents = @('storage', 'security', 'software', 'configuration', 'optimization', 'testing')
foreach ($agent in $agents) {
    $response = Invoke-WebRequest -Uri "http://helios-$agent-agent:8080/health" -Method GET -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ $agent agent: HEALTHY"
    } else {
        Write-Host "❌ $agent agent: UNHEALTHY"
    }
}

# Expected: All 6 agents show HEALTHY
```

- [ ] All 6 agents report healthy
- [ ] No agent health errors
- [ ] Response times acceptable

**Step 2: Service Verification**
```powershell
# Check Azure services
az container list --resource-group helios-platform-rg -o json | ConvertFrom-Json | Select-Object name, properties.instanceView.state

# Expected: All containers show "Running" state
```

- [ ] All containers in Running state
- [ ] No failed containers
- [ ] No containers in Restarting state

**Step 3: Connectivity Tests**
```powershell
# Test Key Vault access
az keyvault secret list --vault-name helios-vault

# Test Storage access
az storage account list --resource-group helios-platform-rg

# Test Network connectivity
Test-NetConnection -ComputerName azure.microsoft.com -Port 443

# Expected: All commands succeed
```

- [ ] Key Vault accessible
- [ ] Storage accounts accessible
- [ ] Network connectivity verified

---

### Security Verification (10 minutes)

**Step 1: Check Security Policies**
```powershell
# Run security audit
.\scripts\security-audit.ps1

# Check AppLocker rules
Get-AppLockerPolicy -Effective | Format-List

# Check encryption status
Get-BitLockerVolume | Select-Object MountPoint, VolumeStatus

# Expected: All encryption enabled, policies in place
```

- [ ] Security audit passes
- [ ] AppLocker rules active
- [ ] Encryption enabled on all volumes

**Step 2: Compliance Verification**
```powershell
# Run compliance check
.\scripts\compliance-audit.ps1

# Expected output:
# HIPAA: ✅
# PCI-DSS: ✅
# SOC 2: ✅
# GDPR: ✅
```

- [ ] All compliance checks pass
- [ ] No policy violations
- [ ] Audit logging enabled

**Step 3: Vulnerability Scan**
```powershell
# Run vulnerability assessment
.\scripts\vulnerability-scan.ps1

# Expected: 0 critical vulnerabilities
```

- [ ] 0 critical vulnerabilities
- [ ] High vulnerabilities: < 3
- [ ] Medium vulnerabilities: documented

---

### Performance Baseline (10 minutes)

**Step 1: Establish Baselines**
```powershell
# Collect current metrics
.\scripts\collect-baselines.ps1

# Expected output saved to: ./baselines/initial-metrics.json
```

- [ ] Performance baseline collected
- [ ] Baseline file created

**Step 2: Compare to Targets**
```
Target Performance Metrics:
├─ CPU Usage: < 35% ✅
├─ Memory Usage: < 60% ✅
├─ Disk I/O: < 45% ✅
├─ Network Latency: < 50ms ✅
└─ API Response: < 200ms ✅

Acceptable Range:
├─ CPU: 20-50%
├─ Memory: 40-75%
├─ Disk I/O: 20-60%
├─ Latency: 30-100ms
└─ API Response: 100-300ms
```

- [ ] All metrics within acceptable range
- [ ] No performance bottlenecks
- [ ] System ready for production load

---

## 🎯 SUCCESS CRITERIA

### Deployment Success

**All of the following must be TRUE:**

**Infrastructure:**
- ✅ All Azure resources deployed
- ✅ Resource group contains 20+ resources
- ✅ All resources show "Succeeded" status
- ✅ No failed deployments in Azure

**Agents:**
- ✅ All 6 agents deployed
- ✅ All agents report "HEALTHY"
- ✅ Response time < 500ms
- ✅ Error rate 0%

**Services:**
- ✅ All AI services configured
- ✅ All external APIs responding
- ✅ Model inference working
- ✅ Token usage tracked

**Security:**
- ✅ All 8 security layers active
- ✅ Encryption enabled
- ✅ Policies enforced
- ✅ Audit logging active
- ✅ 0 critical vulnerabilities

**Monitoring:**
- ✅ All 7 dashboards operational
- ✅ Metrics flowing
- ✅ Alerts configured
- ✅ Baseline established

**Documentation:**
- ✅ Deployment logged
- ✅ Procedures documented
- ✅ Runbooks ready
- ✅ Team trained

### Go-Live Sign-Off

**Required Approvals:**
```
Deployment Lead: _________________ Date: _______
Security Lead: _________________ Date: _______
Operations Lead: _________________ Date: _______
Executive Sponsor: _________________ Date: _______
Compliance Officer: _________________ Date: _______
Project Manager: _________________ Date: _______
```

**Final Checklist:**
- [ ] All success criteria met
- [ ] All required approvals obtained
- [ ] Team ready for go-live
- [ ] Rollback plan approved
- [ ] On-call team assigned
- [ ] Communication sent to stakeholders

---

## 📊 DEPLOYMENT SUMMARY

**Date Started:** _______________  
**Date Completed:** _______________  
**Total Duration:** _______________  

**Deployment Tier Selected:** _______________  
**Final Status:** ✅ SUCCESSFUL / ⚠️ PARTIAL / ❌ FAILED

**Key Metrics:**
- Total Resources Created: _____
- Total Cost (Month 1): $ _____
- Total Time (All Phases): _____ minutes
- Errors Encountered: _____
- Manual Interventions: _____

**Post-Deployment Notes:**
```
[Space for notes and observations]
```

---

## 📞 SUPPORT & TROUBLESHOOTING

**If Deployment Fails:**

1. **Check the logs:**
   ```powershell
   Get-Content ./logs/deployment-*.log | Tail -100
   ```

2. **Review the error:**
   - Is it a credential issue?
   - Is it a resource quota issue?
   - Is it a network connectivity issue?

3. **Consult troubleshooting guide:**
   - See FINAL_DEPLOYMENT_PLAYBOOK.md § TROUBLESHOOTING GUIDE

4. **Get help:**
   - Documentation: https://github.com/M0nado/helios-platform/wiki
   - Issues: https://github.com/M0nado/helios-platform/issues
   - Email: support@helios-platform.dev

---

**Setup Checklist Version:** 1.0.0  
**Last Updated:** April 2026  
**Status:** ✅ PRODUCTION READY

**Print this checklist and use as your go-live guide.**

---

[Back to Checklist Top](#helios-platform---complete-setup-checklist)
