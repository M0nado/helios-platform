# HELIOS Platform - Component Deployment Guide

Complete end-to-end deployment procedures for all 7 components, parallel vs sequential strategies, rollback procedures, and failure recovery.

---

## 🚀 Deployment Overview

### Deployment Phases

```
Total Time: 35-40 minutes (production-ready)

Phase 0 (5 min)   - Pre-flight validation
Phase 1 (5 min)   - Infrastructure setup
Phase 2 (25 min)  - Agent fleet deployment
Phase 3 (18 min)  - AI services activation
Phase 4 (12 min)  - Security hardening
Phase 5 (15 min)  - Monitoring deployment
Phase 6 (1 min)   - Verification & go-live
```

### Component Deployment Sequence

| Order | Component | Phase | Time | Dependencies |
|-------|-----------|-------|------|--------------|
| 1 | Dev AI Hub | 1 | 5 min | None (root) |
| 2 | Build Agents | 2 | 25 min | Dev AI Hub complete |
| 3 | Monado Blade | 2 | 8 min | Build Agents running |
| 4 | Software Stack | 2-3 | 45 min | Build Agents orchestrating |
| 5 | AI Hub | 3 | 18 min | Network ready, secrets |
| 6 | Security Setup | 4 | 12 min | All agents running |
| 7 | GUI Framework | 5 | 15 min | All components online |

---

## 📋 Pre-Deployment Checklist

### System Requirements

```bash
# Check system resources
Get-WmiObject Win32_ComputerSystem | Select-Object -Property SystemFamily, TotalPhysicalMemory
Get-Volume | Select-Object DriveLetter, SizeRemaining

# Minimum requirements
- Windows 11 Pro or Server 2022+
- CPU: 8 cores (16 recommended)
- RAM: 16 GB (32 GB recommended)
- Disk: 50 GB free (SSD preferred)
- .NET: 7.0+
- PowerShell: 7.4+
- Azure CLI: 2.40+
- Docker: 24.0+
```

### Connectivity Tests

```bash
# Test Azure connectivity
az account show

# Test Docker
docker ps

# Test network connectivity
Test-NetConnection -ComputerName github.com -Port 443

# Verify firewall rules
Get-NetFirewallProfile

# Test DNS resolution
nslookup api.github.com
nslookup management.azure.com
```

### Credential Verification

```bash
# Verify Azure authentication
az account show
# Should show your subscription

# Verify Key Vault access
az keyvault list --resource-group $RG_NAME

# Verify GitHub authentication
git config --global user.email
git config --global user.name

# Verify SSH key (if using SSH)
ssh -T git@github.com
```

### Pre-Deployment Checklist

```
Infrastructure:
  ☐ Azure subscription active
  ☐ Resource group created
  ☐ Key Vault provisioned
  ☐ Storage account created
  ☐ Network configured
  
Software:
  ☐ .NET 7.0+ installed
  ☐ PowerShell 7.4+ installed
  ☐ Azure CLI 2.40+ installed
  ☐ Docker 24.0+ installed
  ☐ Git 2.40+ installed
  
Credentials:
  ☐ Azure CLI authenticated
  ☐ GitHub SSH key configured
  ☐ Azure Key Vault accessible
  ☐ Docker registry credentials stored
  ☐ API keys configured
  
Repository:
  ☐ Clone complete
  ☐ All 7 submodules initialized
  ☐ No uncommitted changes
  ☐ Latest versions fetched
  ☐ Deployment scripts executable
  
Environment:
  ☐ 50 GB disk space available
  ☐ 16 GB RAM available
  ☐ 8+ CPU cores available
  ☐ Network connectivity verified
  ☐ Firewall rules configured
```

---

## 🔀 Deployment Strategies

### Strategy 1: Sequential Deployment (Safest)

**Best for:** First-time deployment, conservative approach

```bash
#!/bin/bash

echo "=== HELIOS Sequential Deployment ==="

# Phase 0: Pre-flight
.\scripts\phase-0-preflight.ps1
if ($? -ne 0) { exit 1 }

# Phase 1: Infrastructure (Dev AI Hub)
.\scripts\phase-1-infrastructure.ps1
if ($? -ne 0) { .\scripts\rollback-phase-1.ps1; exit 1 }

# Phase 2: Agent Fleet
.\scripts\phase-2-agents.ps1
if ($? -ne 0) { .\scripts\rollback-phase-2.ps1; exit 1 }

# Phase 3: AI Services
.\scripts\phase-3-ai-hub.ps1
if ($? -ne 0) { .\scripts\rollback-phase-3.ps1; exit 1 }

# Phase 4: Security
.\scripts\phase-4-security.ps1
if ($? -ne 0) { .\scripts\rollback-phase-4.ps1; exit 1 }

# Phase 5: Monitoring
.\scripts\phase-5-monitoring.ps1
if ($? -ne 0) { .\scripts\rollback-phase-5.ps1; exit 1 }

# Phase 6: Verification
.\scripts\phase-6-verification.ps1
if ($? -ne 0) { exit 1 }

echo "✅ Deployment complete!"
```

**Time:** 35-40 minutes  
**Risk:** Low  
**Rollback:** Easy (use phase-specific rollback scripts)

---

### Strategy 2: Parallel Deployment (Faster)

**Best for:** Experienced teams, faster deployments

```bash
#!/bin/bash

echo "=== HELIOS Parallel Deployment ==="

# Phase 0: Pre-flight (sequential)
.\scripts\phase-0-preflight.ps1

# Phase 1: Infrastructure (sequential)
.\scripts\phase-1-infrastructure.ps1

# Phase 2: Agent Fleet Components (parallel with different start times)
# Start with sequential lock to ensure safety
.\scripts\phase-2-base-setup.ps1  # Dev AI Hub templates

# Then start Build Agents and Monado in parallel
$job1 = Start-Job -ScriptBlock { .\scripts\phase-2-build-agents.ps1 } -Name "BuildAgents"
$job2 = Start-Job -ScriptBlock { .\scripts\phase-2-monado.ps1 } -Name "MonadoBlade"
$job3 = Start-Job -ScriptBlock { .\scripts\phase-2-software-stack.ps1 } -Name "SoftwareStack"

# Wait for all to complete
Wait-Job -Job $job1, $job2, $job3
$result1 = Receive-Job -Job $job1
$result2 = Receive-Job -Job $job2
$result3 = Receive-Job -Job $job3

# Check results
if ($result1 -and $result2 -and $result3) {
    # Phase 3 & 4 can run after Phase 2 complete
    $job4 = Start-Job -ScriptBlock { .\scripts\phase-3-ai-hub.ps1 } -Name "AIHub"
    $job5 = Start-Job -ScriptBlock { .\scripts\phase-4-security.ps1 } -Name "Security"
    
    Wait-Job -Job $job4, $job5
    # Continue...
} else {
    exit 1
}
```

**Time:** 25-30 minutes  
**Risk:** Medium  
**Rollback:** Moderate complexity

---

### Strategy 3: Canary Deployment (Staged)

**Best for:** Production environment, risk-averse

```bash
#!/bin/bash

echo "=== HELIOS Canary Deployment ==="

# Stage 1: Dev environment
echo "Stage 1: Deploying to DEV..."
.\scripts\deploy.ps1 -Environment dev -Phase all

# Verify
.\scripts\verify-integration.ps1 -Environment dev

# Stage 2: Staging environment
echo "Stage 2: Deploying to STAGING..."
.\scripts\deploy.ps1 -Environment staging -Phase all

# Run full test suite
.\scripts\run-tests.ps1 -Environment staging

# Stage 3: Production (25% traffic)
echo "Stage 3: Production canary (25%)..."
.\scripts\deploy.ps1 -Environment prod -Canary 0.25

# Monitor for 1 hour
Start-Sleep -Seconds 3600
.\scripts\monitor-health.ps1 -Environment prod

# If all good, go to 100%
echo "Stage 4: Full production deployment..."
.\scripts\deploy.ps1 -Environment prod -Canary 1.0

echo "✅ Canary deployment complete!"
```

**Time:** 2-4 hours (with waiting periods)  
**Risk:** Very low  
**Rollback:** Instant (use previous version traffic switch)

---

## 🔧 Component-Specific Deployment

### 1. Dev AI Hub Deployment

```bash
./modules/helios-dev-ai-hub/scripts/deploy.ps1 `
    -SubscriptionId $SUBSCRIPTION_ID `
    -ResourceGroup $RESOURCE_GROUP `
    -Location "eastus" `
    -Environment "production"

# Verify
./modules/helios-dev-ai-hub/scripts/verify.ps1
```

**Deployment Time:** 4-5 minutes  
**Output:** Infrastructure templates, policies  
**Verification:** Azure resources created

---

### 2. Build Agents Deployment

```bash
./modules/helios-build-agents/scripts/deploy.ps1 `
    -AgentCount 11 `
    -ConcurrencyLimit 3 `
    -TemplatesPath "./modules/helios-dev-ai-hub/templates"

# Monitor agents
./modules/helios-build-agents/scripts/monitor.ps1 -Interval 5
```

**Deployment Time:** 25 minutes (11 agents, 3 concurrent)  
**Output:** 11 agents running, task queue operational  
**Verification:** `GET /api/v1/agents` responds

---

### 3. Monado Blade Deployment

```bash
./modules/helios-monado-blade/scripts/deploy.ps1 `
    -ModelType "XGBoost" `
    -TrainingInterval "hourly" `
    -DataRetention "30days"

# Start learning
./modules/helios-monado-blade/scripts/start-learning.ps1
```

**Deployment Time:** 8 minutes  
**Output:** Learning models initialized  
**Verification:** `/api/v1/patterns/analyze` responds

---

### 4. AI Hub Deployment

```bash
./modules/helios-ai-hub/scripts/deploy.ps1 `
    -Models @("gpt-4", "claude-3", "gemini", "ollama") `
    -EnableCaching $true `
    -CostLimit 5000

# Verify all services
./modules/helios-ai-hub/scripts/verify-services.ps1
```

**Deployment Time:** 18 minutes  
**Output:** 12+ AI services online  
**Verification:** `/api/v1/models/list` shows all available

---

### 5. Software Stack Deployment

```bash
./modules/helios-software-stack/scripts/deploy.ps1 `
    -Tools @("vscode", "python", "docker", "terraform") `
    -ParallelInstalls 3 `
    -AutoUpdate $false

# Monitor installation
./modules/helios-software-stack/scripts/monitor-install.ps1
```

**Deployment Time:** 45 minutes (40+ tools)  
**Output:** All tools installed and configured  
**Verification:** Tool versions match requirements

---

### 6. Security Setup Deployment

```bash
./modules/helios-security-setup/scripts/deploy.ps1 `
    -ApplyAllLayers $true `
    -StrictMode $false `
    -AuditLogging $true

# Verify security posture
./modules/helios-security-setup/scripts/verify-security.ps1
```

**Deployment Time:** 12 minutes  
**Output:** 8 security layers active  
**Verification:** `POST /api/v1/security/verify` returns pass

---

### 7. GUI Framework Deployment

```bash
./modules/helios-gui-framework/scripts/deploy.ps1 `
    -Theme "xenoblade-dark" `
    -EnableRealtime $true `
    -EnableAlerts $true

# Verify dashboard
./modules/helios-gui-framework/scripts/verify-dashboard.ps1
```

**Deployment Time:** 15 minutes  
**Output:** 8 dashboards operational  
**Verification:** Dashboard accessible at `https://localhost:5005`

---

## 🔄 Rollback Procedures

### Quick Rollback (All Components)

```bash
# Immediate rollback to previous version
$previousTag = "v1.0.0"

git checkout $previousTag
git submodule update --init --recursive

# Stop all components
.\scripts\stop-all.ps1

# Rollback infrastructure
./modules/helios-dev-ai-hub/scripts/rollback.ps1

# Restart in correct order
.\scripts\deploy.ps1 -Phase all

echo "✅ Rollback complete"
```

**Time:** 5-10 minutes  
**Data Loss:** Minimal (use WORM backups)

---

### Component-Specific Rollback

```bash
# Example: Rollback Security Setup to previous version

cd modules/helios-security-setup
git checkout v0.9.5

# Stop security services
.\scripts/stop.ps1

# Restore previous policies
.\scripts/restore-policies.ps1 -Backup "backup-v0.9.5"

# Verify
.\scripts/verify.ps1
```

---

### Database Rollback

```bash
# If configuration database corrupted

# 1. Stop all components
.\scripts\stop-all.ps1

# 2. Restore from backup
Restore-AzStorageAccount -ResourceGroup $RG -StorageAccount $SA `
    -BackupId "backup-2024-01-20-14-30-00"

# 3. Restart deployment
.\scripts\deploy.ps1 -Phase all
```

---

## 🚨 Failure Recovery

### Agent Failure Detection

```bash
# Automatic health check every 30 seconds
./modules/helios-build-agents/scripts/health-check.ps1

# Output:
# Agent 1 (Storage):     ✅ Healthy
# Agent 2 (Security):    ❌ Unhealthy (high latency)
# Agent 3 (Config):      ✅ Healthy
```

### Auto-Recovery Procedure

```bash
# If agent fails:
1. Health check detects failure (30-60 seconds)
2. Auto-restart triggered
3. Rollback current task to queue
4. Retry with exponential backoff
5. Alert sent if 3 consecutive failures

# Max retry attempts: 3
# Retry backoff: 5s, 10s, 20s
```

### Manual Recovery

```bash
# If auto-recovery fails:

# 1. Identify failed component
./modules/helios-build-agents/scripts/list-agents.ps1 | grep -i "error"

# 2. Get failure details
./modules/helios-build-agents/scripts/get-agent-error.ps1 -AgentId 2

# 3. Fix underlying issue
# (e.g., free disk space, kill stuck process)

# 4. Restart agent
./modules/helios-build-agents/scripts/restart-agent.ps1 -AgentId 2

# 5. Resume tasks
./modules/helios-build-agents/scripts/resume-tasks.ps1
```

### Crisis Recovery

```bash
# If deployment severely corrupted:

# 1. Emergency backup activation
Stop-Job -Name "*"  # Stop all jobs
.\scripts\emergency-stop.ps1

# 2. Restore from disaster recovery backup
Restore-FromDRBackup -Location "backup-dr-2024-01-20"

# 3. Verify infrastructure integrity
.\scripts\verify-infrastructure.ps1

# 4. Restart from Phase 0
.\scripts\phase-0-preflight.ps1
# Continue deployment...

# Contact support if above fails
```

---

## 📊 Deployment Monitoring

### Real-time Monitoring

```bash
# Open monitoring dashboard
Start-Process "https://localhost:5005"

# Or command-line monitoring
.\scripts\monitor-deployment.ps1 -Refresh 5

# Output:
# [14:30] Phase 1: Infrastructure ████████░░ 80% (4/5 min)
# [14:30] Phase 2: Agents        ██░░░░░░░░ 10% (3/30 min)
#   └─ Storage Agent: ✅ Complete (8m)
#   └─ Security Agent: ⏳ Running (6/12m)
#   └─ Config Agent: ⏳ Running (3/4m)
```

### Health Metrics

```bash
# Monitor component health
$health = .\scripts\get-health-metrics.ps1

# Shows:
- Overall system health: 98%
- Component status: All green
- Agent throughput: 12 tasks/sec
- Error rate: 0.05%
- Latency: 245ms (p95)
```

### Performance Metrics

```bash
# Track deployment performance
.\scripts\get-performance-metrics.ps1 | Format-Table

# Shows:
Component      Deployment Time    CPU Peak    Memory Peak    Disk Used
──────────────────────────────────────────────────────────────────────
Dev AI Hub     4m 12s            25%         2 GB           3 GB
Build Agents   15m 45s           75%         6 GB           2 GB
Monado Blade   8m 30s            60%         8 GB           2.5 GB
AI Hub         16m 20s           85%         5 GB           3 GB
Software Stack 35m 10s           80%         4 GB           25 GB
Security Setup 12m 15s           15%         1 GB           0.5 GB
GUI Framework  14m 50s           30%         2 GB           1 GB
──────────────────────────────────────────────────────────────────────
TOTAL          35m 42s           85% peak    25 GB          37 GB
```

---

## ✅ Post-Deployment Verification

### 42-Point Verification Checklist

```bash
# Run full verification
.\scripts\phase-6-verification.ps1

# Tests:
Infrastructure (5 points)
  ✅ Dev AI Hub: Resources deployed
  ✅ Networking: All rules configured
  ✅ Storage: Accounts accessible
  ✅ Key Vault: Secrets readable
  ✅ Firewall: Rules applied

Agents (6 points)
  ✅ Build Agents: All 11 running
  ✅ Health checks: All passing
  ✅ Communication: Inter-agent working
  ✅ Task queue: Functional
  ✅ Failure recovery: Tested
  ✅ Rollback: Ready

AI Services (6 points)
  ✅ LLM services: Connected
  ✅ Cost tracking: Active
  ✅ Rate limiting: Working
  ✅ Fallback chains: Tested
  ✅ Quality monitoring: Enabled
  ✅ API responses: < 500ms

Security (8 points)
  ✅ Layer 1: TPM verified
  ✅ Layer 2: MFA working
  ✅ Layer 3: Vault operational
  ✅ Layer 4: Signing verified
  ✅ Layer 5: Container isolation
  ✅ Layer 6: Approval workflow
  ✅ Layer 7: Audit logging
  ✅ Layer 8: AI consensus

Monitoring (8 points)
  ✅ Dashboard: Responsive
  ✅ 8 tabs: All functional
  ✅ Real-time updates: Flowing
  ✅ Alerts: Triggering
  ✅ Export: Working
  ✅ Performance: Acceptable
  ✅ Data accuracy: Verified
  ✅ Accessibility: WCAG compliant

Integration (5 points)
  ✅ Component communication: Working
  ✅ Data flow: Verified
  ✅ API endpoints: Responding
  ✅ Event streaming: Active
  ✅ Webhooks: Functional

Performance (3 points)
  ✅ Latency: < 500ms
  ✅ Throughput: > 100 tasks/sec
  ✅ Error rate: < 0.1%

TOTAL: 42/42 TESTS PASSING ✅
```

---

## 📝 Deployment Documentation

### Generate Deployment Report

```bash
# Create deployment report
.\scripts\generate-deployment-report.ps1

# Report includes:
- Deployment strategy used
- Timeline (actual vs estimated)
- Components deployed
- Configuration applied
- Issues encountered
- Resolutions applied
- Verification results
- Performance metrics
- Resource utilization
- Cost analysis
```

### Create Deployment Snapshot

```bash
# Snapshot current deployment state
.\scripts\snapshot-deployment.ps1

# Creates:
- Docker image of all components
- Configuration export
- Database backup
- Policy export
- Secret references (not actual secrets)
- Deployment metadata
```

---

## 🔗 Related Documentation

- **COMPONENT_INTEGRATION_GUIDE.md** - Component specifications
- **COMPONENT_USAGE_MATRIX.md** - Phase mapping
- **MULTI_REPO_SYNC_GUIDE.md** - Repository sync
- **VERSION_COMPATIBILITY_GUIDE.md** - Version management

---

## 🎯 Quick Start

### Deploy in 35 Minutes

```bash
# 1. Verify system ready
.\scripts\phase-0-preflight.ps1

# 2. Run automated deployment
.\scripts\deploy.ps1 -Strategy Sequential -Monitor $true

# 3. Verify completion
.\scripts\phase-6-verification.ps1

# All done! ✅
```

### Deploy in Development

```bash
# Minimal deployment (15 minutes)
.\scripts\deploy.ps1 -Environment dev -Components @("dev-ai-hub", "build-agents", "security-setup")
```

---

**Last Updated:** 2024  
**Status:** ✅ Complete  
**Strategies:** 3 (Sequential, Parallel, Canary)  
**Rollback Options:** 4  
**Verification Tests:** 42  
**Estimated Time:** 35-40 minutes
