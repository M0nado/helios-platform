# HELIOS Platform GitHub Actions Workflows - Complete Guide

## Overview

The HELIOS Platform uses 5 interconnected GitHub Actions workflows to automate deployment, package management, code quality, analysis, and verification. This guide documents all workflows, their configurations, triggers, and required secrets.

---

## 1. Deploy Workflow (`deploy.yml`)

### Purpose
7-phase orchestrated deployment pipeline for complete infrastructure and platform setup.

### Trigger Events
- **Push**: Branches `main`, `develop`
- **Pull Request**: Branch `main`
- **Manual**: `workflow_dispatch` with phase selection

### Workflow Phases

#### Phase 0: Pre-flight Checks
- **Job**: `preflight`
- **Runner**: `windows-latest`
- **Tasks**:
  - System information verification
  - Environment variable checks
  - Prerequisite validation
- **Artifacts**: `preflight-logs`

#### Phase 1: Infrastructure Deployment
- **Job**: `infrastructure`
- **Depends On**: `preflight`
- **Runner**: `windows-latest`
- **Tasks**:
  - Azure authentication
  - Resource group creation
  - Network and compute setup
- **Environment Variables**:
  - `AZURE_SUBSCRIPTION_ID`
  - `AZURE_TENANT_ID`
  - `AZURE_CLIENT_ID`
  - `AZURE_CLIENT_SECRET`
- **Artifacts**: `infrastructure-logs`

#### Phase 2: Agent Fleet Deployment
- **Job**: `agents`
- **Depends On**: `infrastructure`
- **Runner**: `windows-latest`
- **Parallel Execution**: 6 agents (max 3 parallel)
- **Agents Deployed**:
  - Storage Agent
  - Security Agent
  - Software Agent
  - GUI Agent
  - Optimization Agent
  - Testing Agent
- **Matrix Strategy**: `max-parallel: 3`

#### Phase 3: AI Services Initialization
- **Job**: `ai-services`
- **Depends On**: `agents`
- **Runner**: `windows-latest`
- **Services Initialized**:
  - Ollama
  - Azure OpenAI
  - Claude
  - Gemini
  - Copilot
  - Fabric

#### Phase 4: Security Layer Deployment
- **Job**: `security`
- **Depends On**: `ai-services`
- **Runner**: `windows-latest`
- **Security Layers**:
  1. Physical (USB + TPM)
  2. Authentication (MFA)
  3. Secrets (Dual Vault)
  4. Code Signing (RSA 2048)
  5. Execution Isolation (Docker)
  6. Change Management (7-stage)
  7. Audit Logging (WORM)
  8. AI Security (Consensus)

#### Phase 5: Monitoring Setup
- **Job**: `monitoring`
- **Depends On**: `security`
- **Runner**: `windows-latest`
- **Dashboards Created**:
  - Cost Dashboard
  - Performance Dashboard
  - Security Dashboard
  - Compliance Dashboard
  - AI Metrics Dashboard
  - Agents Dashboard
  - Health Dashboard

#### Phase 6: Verification & Go-Live
- **Job**: `verification`
- **Depends On**: `agents`, `ai-services`, `security`, `monitoring`
- **Runner**: `windows-latest`
- **42-Point Verification**:
  - Infrastructure: 6/6 checks
  - Security Compliance: 8/8 checks
  - Performance Baseline: 6/6 checks
  - Integration Tests: 7/7 checks
  - Disaster Recovery: 7/7 checks
  - Documentation: 7/7 checks
  - Go-Live Approval: 6/6 signatures

### Workflow Manual Inputs
```
phase:
  - preflight          (Phase 0)
  - infrastructure     (Phase 1)
  - agents            (Phase 2)
  - ai-services       (Phase 3)
  - security          (Phase 4)
  - monitoring        (Phase 5)
  - verification      (Phase 6)
  - all               (All phases)
```

### Required Secrets
```
AZURE_SUBSCRIPTION_ID  - Azure subscription ID
AZURE_TENANT_ID        - Azure tenant ID
AZURE_CLIENT_ID        - Service principal client ID
AZURE_CLIENT_SECRET    - Service principal secret
```

### Environment Variables
```
DEPLOYMENT_DIR         - ./deployment
AZURE_SUBSCRIPTION_ID  - (from secret)
AZURE_TENANT_ID        - (from secret)
AZURE_CLIENT_ID        - (from secret)
AZURE_CLIENT_SECRET    - (from secret)
```

---

## 2. NuGet Package Workflow (`nuget.yml`)

### Purpose
Build and publish .NET packages to NuGet.org with automated release management.

### Trigger Events
- **Push**: Branch `main`, tags matching `v*`
- **Manual**: `workflow_dispatch`

### Jobs

#### Job: Build
- **Runner**: `windows-latest`
- **Tasks**:
  1. Checkout repository
  2. Setup .NET 8.0
  3. Restore dependencies
  4. Build Release configuration
  5. Pack NuGet package
  6. Upload artifacts
- **Artifacts**: `nupkg/` directory

#### Job: Publish
- **Depends On**: `build`
- **Trigger Condition**: `git push` with tag `refs/tags/v*`
- **Runner**: `windows-latest`
- **Tasks**:
  1. Download NuGet package artifact
  2. Setup .NET
  3. Push to NuGet.org
  4. Create GitHub Release
  5. Tag release with package version
- **Release Notes Include**:
  - Version number
  - Deployment automation info
  - Agent fleet system details
  - AI services list
  - Security framework details
  - Monitoring capabilities

### Configuration
```
NUGET_FEED              - https://api.nuget.org/v3/index.json
NUGET_API_KEY           - (from secret)
PACKAGE_VERSION         - 1.0.0
```

### Required Secrets
```
NUGET_API_KEY           - NuGet.org API key for publishing
GITHUB_TOKEN            - GitHub token (auto-provided)
```

### Publish Trigger
```bash
# Create tag and push to trigger publish
git tag v1.0.0
git push origin v1.0.0
```

---

## 3. Analysis Workflow (`analysis.yml`)

### Purpose
Weekly component analysis and metrics validation with generated reports.

### Trigger Events
- **Push**: When `COMPONENT_ANALYSIS.md`, `COMPONENT_METRICS.json`, or workflow file changes
- **Schedule**: Weekly (Sunday at 00:00 UTC)

### Jobs

#### Job: Analyze
- **Runner**: `ubuntu-latest`
- **Python Version**: 3.11
- **Tasks**:
  1. Validate `COMPONENT_METRICS.json` syntax
  2. Parse and verify metrics structure
  3. Generate analysis report
  4. Calculate deployment totals
  5. Analyze performance metrics
  6. Comment on commit
- **Artifacts**: `analysis-report.txt`

### Metrics JSON Structure
```json
{
  "components": {
    "name": "details"
  },
  "phases": {
    "phase_name": {
      "time": minutes,
      "status": "completed"
    }
  },
  "metrics": {
    "performance": {
      "boot_time_reduction": percentage
    },
    "security": {
      "layers": count
    }
  }
}
```

### Analysis Report Content
- Total components analyzed
- Total deployment time (minutes)
- Performance improvement percentage
- Security layer count
- System status

---

## 4. Quality Workflow (`quality.yml`)

### Purpose
Continuous code quality checks on push and pull requests.

### Trigger Events
- **Push**: Branches `main`, `develop`
- **Pull Request**: Branch `main`

### Jobs

#### Job: PowerShell Linting
- **Runner**: `ubuntu-latest`
- **Tool**: PSScriptAnalyzer
- **Scope**: `./src`, `./scripts` (recursive)
- **Severity**: Error, Warning
- **Artifacts**: `ps-analysis.txt`

#### Job: Markdown Linting
- **Runner**: `ubuntu-latest`
- **Tool**: nosborn/github-action-markdown-cli@v3.3.0
- **Config**: `.markdownlint.json`
- **Scope**: All markdown files

#### Job: JSON Validation
- **Runner**: `ubuntu-latest`
- **Tool**: Python json.tool
- **Scope**: All `*.json` files (excluding node_modules)

#### Job: Security Scanning
- **Runner**: `ubuntu-latest`
- **Tool**: github/super-linter/slim@v4
- **Checks**: Multiple linters for security issues
- **Environment**: `DEFAULT_BRANCH: main`

---

## 5. Verify Workflow (`verify.yml`)

### Purpose
Continuous 42-point verification and health checks every 6 hours.

### Trigger Events
- **Schedule**: Every 6 hours (0 */6 * * *)
- **Manual**: `workflow_dispatch`

### Jobs

#### Job: Health Check
- **Runner**: `windows-latest`
- **Checks**:
  1. Essential files existence:
     - `COMPONENT_ANALYSIS.md`
     - `COMPONENT_METRICS.json`
     - `PROJECT_BOARD_QUICK_START.md`
     - `GITHUB_PROJECT_SETUP.md`
     - `DELIVERY_MANIFEST.md`
  2. Repository statistics
  3. Deployment scripts validation:
     - `master-deploy.ps1`
     - 8 phase scripts (Phase 0-6)
     - Each script size verification

#### Job: Metrics Validation
- **Runner**: `ubuntu-latest`
- **Python Version**: 3.x
- **Validation**:
  1. `COMPONENT_METRICS.json` JSON validity
  2. Component count verification
  3. Phase count verification
  4. Deployment options count
  5. Documentation files presence

#### Job: Generate Status Report
- **Depends On**: health-check, metrics-validation
- **Condition**: Always (even if previous jobs fail)
- **Output**: `health-report.md`
- **Report Content**:
  - Timestamp
  - System status
  - Component list
  - Phase list
  - Metrics summary

---

## Summary Table

| Workflow | Trigger | Runner | Purpose |
|----------|---------|--------|---------|
| deploy.yml | push/PR/manual | windows | 7-phase deployment |
| nuget.yml | push/tag/manual | windows | Package build/publish |
| analysis.yml | push/schedule | ubuntu | Metrics analysis |
| quality.yml | push/PR | ubuntu | Code quality |
| verify.yml | schedule/manual | windows/ubuntu | Health verification |

---

## Quick Reference: Secrets & Environment Variables

### Required GitHub Secrets
```
AZURE_SUBSCRIPTION_ID   - deploy.yml
AZURE_TENANT_ID         - deploy.yml
AZURE_CLIENT_ID         - deploy.yml
AZURE_CLIENT_SECRET     - deploy.yml
NUGET_API_KEY           - nuget.yml
GITHUB_TOKEN            - (auto-provided)
```

### Workflow Configuration Files
- `.github/workflows/deploy.yml` - Main deployment
- `.github/workflows/nuget.yml` - Package management
- `.github/workflows/analysis.yml` - Metrics/analysis
- `.github/workflows/quality.yml` - Code quality
- `.github/workflows/verify.yml` - Verification

---

## Status
✅ All 5 workflows verified and documented
✅ YAML syntax valid
✅ Job dependencies configured
✅ Triggers properly defined
✅ Secrets reference validated

