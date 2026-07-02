# MonadoBlade Stream 1: GitHub Repository & CI/CD Planning

**Document Version:** 1.0  
**Date:** 2026-04-23  
**Status:** PHASE 1 TASK 3 COMPLETE

---

## Table of Contents
1. [Repository Structure](#repository-structure)
2. [Branch Strategy](#branch-strategy)
3. [PR/Merge Requirements](#prmerge-requirements)
4. [CI/CD Pipeline](#cicd-pipeline)
5. [GitHub Actions Workflows](#github-actions-workflows)
6. [Code Quality Gates](#code-quality-gates)
7. [Release Process](#release-process)
8. [Team Collaboration](#team-collaboration)

---

## Repository Structure

### Top-Level Directory Layout

```
MonadoBlade/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                    (Pull request checks)
│   │   ├── deploy.yml                (Release pipeline)
│   │   ├── nightly.yml               (Extended testing)
│   │   └── security-scan.yml         (SAST/dependency checks)
│   └── ISSUE_TEMPLATE/
│       ├── bug-report.md
│       ├── feature-request.md
│       └── architecture-decision.md
│
├── src/
│   ├── MonadoBlade.Core/
│   │   ├── Abstractions/              (Service contracts, base classes)
│   │   ├── Services/                  (6 core service interfaces)
│   │   ├── Exceptions/                (Error contracts)
│   │   ├── Configuration/             (DI registration, settings)
│   │   ├── Logging/                   (Logging abstractions)
│   │   └── Messaging/                 (Event bus, pub/sub)
│   │
│   ├── MonadoBlade.Data/
│   │   ├── Repositories/              (Entity repositories)
│   │   ├── Models/                    (Data models)
│   │   ├── Migrations/                (Database migrations)
│   │   └── Context/                   (DbContext, session management)
│   │
│   ├── MonadoBlade.Services/
│   │   ├── Data/                      (IDataService implementation)
│   │   ├── CloudSync/                 (ICloudSyncService implementation)
│   │   ├── ML/                        (IMLService implementation)
│   │   ├── Plugin/                    (IPluginService implementation)
│   │   ├── Dashboard/                 (IDashboardService implementation)
│   │   └── Settings/                  (ISettingsService implementation)
│   │
│   ├── MonadoBlade.GUI/               (WPF UI + 190 components)
│   │   ├── ViewModels/                (MVVM ViewModels)
│   │   ├── Components/                (Custom UI components)
│   │   ├── Pages/                     (Routable pages)
│   │   ├── Themes/                    (Design system)
│   │   └── Resources/                 (Styles, templates)
│   │
│   ├── MonadoBlade.AI/                (AI/ML integration layer)
│   ├── MonadoBlade.Caching/           (Distributed cache)
│   ├── MonadoBlade.Boot/              (Application startup)
│   └── MonadoBlade.Security/          (Auth, permissions, encryption)
│
├── tests/
│   ├── Unit/
│   │   ├── Core.Tests/                (920+ unit tests)
│   │   ├── Services.Tests/
│   │   ├── GUI.Tests/
│   │   └── Fixtures/                  (Test data, mocks)
│   │
│   ├── Integration/
│   │   ├── Services.Integration.Tests/
│   │   ├── Data.Integration.Tests/
│   │   └── Fixtures/
│   │
│   └── E2E/
│       ├── Workflows.Tests/           (80+ end-to-end tests)
│       ├── Scenarios/
│       └── TestData/
│
├── docs/
│   ├── ARCHITECTURE.md                (System architecture overview)
│   ├── INTEGRATION_ARCHITECTURE.md    (Service boundaries & flows)
│   ├── API.md                         (Service API documentation)
│   ├── DEPLOYMENT.md                  (Deployment procedures)
│   ├── CONTRIBUTING.md                (Contribution guidelines)
│   ├── SECURITY.md                    (Security policies & procedures)
│   └── ADR/                           (Architecture Decision Records)
│
├── scripts/
│   ├── build.ps1                      (Build automation)
│   ├── test.ps1                       (Test execution)
│   ├── deploy.ps1                     (Deployment automation)
│   ├── rollback.ps1                   (Rollback procedures)
│   └── setup-local.ps1                (Local environment setup)
│
├── .gitignore
├── .editorconfig
├── .github-policies.yml               (GitHub organization policies)
├── CODEOWNERS                         (CODEOWNERS for PR review routing)
├── LICENSE                            (MIT License)
├── README.md                          (Project overview)
├── CHANGELOG.md                       (Version history)
├── MonadoBlade.sln                    (Solution file)
└── package.json / paket.lock          (Dependency management)

```

---

## Branch Strategy

### Branch Types and Naming Conventions

#### 1. **main** (Production-Ready)
- **Protection**: YES
  - Require 2 approvals
  - All CI checks must pass
  - Require status checks before merge
  - Dismiss stale pull request approvals on new commits
  - Require branches be up-to-date before merging
  - Require code reviews from code owners
  
- **Policy**: Only releases and hotfixes merged here
- **Naming**: N/A (immutable branch)
- **Merge**: Squash & merge with auto-delete

#### 2. **develop** (Integration/Staging)
- **Protection**: YES
  - Require 1 approval
  - All CI checks must pass
  - Require status checks before merge
  
- **Policy**: Integration branch for all features
- **Naming**: N/A (immutable branch)
- **Merge**: Create merge commits (preserve history)
- **Update Frequency**: Daily from main (via automation)

#### 3. **feature/** (Feature Development)
- **Naming**: `feature/JIRA-ID-short-description`
  - Example: `feature/MB-1234-dashboard-analytics`
  
- **Base**: `develop`
- **Policy**:
  - One feature per branch
  - PR required for merge to develop
  - Auto-delete on merge
  - Rebasing allowed but not required
  
- **Cleanup**: Deleted 7 days after merge (automation)

#### 4. **bugfix/** (Bug Fixes)
- **Naming**: `bugfix/JIRA-ID-short-description`
  - Example: `bugfix/MB-5678-sync-conflict-resolution`
  
- **Base**: `develop`
- **Policy**: Same as feature branches
- **Cleanup**: Auto-deleted on merge

#### 5. **hotfix/** (Emergency Fixes)
- **Naming**: `hotfix/JIRA-ID-critical-issue`
  - Example: `hotfix/MB-9999-data-corruption-fix`
  
- **Base**: `main` (for production fixes)
- **Policy**:
  - Require 2 approvals (critical review)
  - Fast-track to main
  - Auto-merge to develop after main merge
  - Triggers emergency deployment
  
- **Cleanup**: Auto-deleted on merge

#### 6. **release/** (Release Preparation)
- **Naming**: `release/v3.5.0`
  - Example: `release/v3.5.0`
  
- **Base**: `develop`
- **Policy**:
  - Created when feature freeze is declared
  - Version bump + release notes only
  - Quick merges to main for release
  - Cherry-picks back to develop
  
- **Cleanup**: Deleted after main merge

#### 7. **docs/** (Documentation)
- **Naming**: `docs/JIRA-ID-topic`
  - Example: `docs/MB-2000-service-api-docs`
  
- **Base**: `develop`
- **Policy**:
  - Documentation-only changes
  - Single approval sufficient
  - Auto-merge if all checks pass

### Branch Hierarchy and Merge Flows

```
┌──────────────────────────────────────────────────────────────┐
│                          MAIN (v3.5.0)                       │
│                    Production-Ready Releases                 │
│                   [2 approvals required]                     │
└────────────┬────────────────────────────┬────────────────────┘
             │ (release merge)            │ (hotfix merge)
             │                            │
    ┌────────▼──────────────┐   ┌────────▼──────────────┐
    │   RELEASE/v3.5.0      │   │  HOTFIX/critical-bug │
    │   [Version bump,      │   │  [Urgent fixes for   │
    │    release notes]     │   │   production]        │
    └────────┬──────────────┘   └────────┬──────────────┘
             │                           │
             └───────────────┬───────────┘
                             │
                ┌────────────▼────────────┐
                │    DEVELOP              │
                │   Integration Branch    │
                │ [1 approval required]   │
                └────────────┬────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
    ┌────▼──────┐    ┌──────▼────┐    ┌────────▼──────┐
    │ FEATURE/* │    │ BUGFIX/*  │    │ DOCS/*        │
    │ [auto-del]│    │ [auto-del]│    │ [auto-del]    │
    └───────────┘    └───────────┘    └───────────────┘
```

---

## PR/Merge Requirements

### Pull Request Checklist

Before a PR can be merged, it must satisfy:

- ✅ **Title Format**: `[TYPE] JIRA-ID: Brief Description`
  - Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`
  - Example: `[feat] MB-1234: Add dashboard analytics service`

- ✅ **Description**:
  - What does this PR change?
  - Why is the change needed?
  - Links to JIRA issue and related PRs
  - Checklist of testing performed

- ✅ **Code Changes**:
  - Code follows project style guide
  - No dead code or temporary files
  - Comments explain "why", not "what"
  - No console.log or debug code left

- ✅ **Tests**:
  - Unit tests added/updated (coverage ≥ 80%)
  - Integration tests if applicable
  - Test names are descriptive
  - All tests pass locally

- ✅ **Documentation**:
  - API changes documented
  - Architecture changes noted
  - README updated if relevant
  - Changelog entry added

- ✅ **Approvals**:
  - Required approvals obtained (1 for develop, 2 for main)
  - Code owners approved changes in their areas

- ✅ **CI Pipeline**:
  - Build succeeds
  - All tests pass (unit, integration, E2E)
  - Code coverage doesn't decrease
  - No security vulnerabilities
  - Linting checks pass

- ✅ **No Conflicts**:
  - Branch is up-to-date with target branch
  - No merge conflicts

### Merge Process

```
1. Author creates feature branch from develop
2. Author commits changes with atomic commits
3. Author creates PR with template
4. Automated checks run (CI pipeline)
5. Code owners/reviewers review code
6. Discussions resolved, changes requested made
7. Required approvals obtained
8. Author rebases/updates branch if needed
9. Reviewer/maintainer merges PR
10. Branch auto-deleted from GitHub
11. Local branches cleaned up by developers
```

---

## CI/CD Pipeline

### Continuous Integration (CI) - On Every Push

```
┌─────────────────────────────────────────────────────────────┐
│                    COMMIT PUSHED                            │
└────────────────────────┬────────────────────────────────────┘
                         │
          ┌──────────────▼───────────────┐
          │   Build & Compile (dotnet)  │
          │   • Restore NuGet packages  │
          │   • Compile C# code         │
          │   • Generate assemblies     │
          └──────────┬───────────────────┘
                     │
        ┌────────────▼───────────────────┐
        │   Lint & Style Check          │
        │   • StyleCop rules            │
        │   • Code analysis             │
        │   • Format checking           │
        └────────────┬───────────────────┘
                     │
        ┌────────────▼────────────────────────┐
        │   Unit Tests (920+)                │
        │   • Framework: xUnit              │
        │   • Timeout: 5 minutes            │
        │   • Coverage: >= 80% required     │
        └────────────┬────────────────────────┘
                     │
        ┌────────────▼────────────────────────┐
        │   Security Scanning                │
        │   • NuGet package vulnerabilities  │
        │   • SAST analysis (SonarQube)      │
        │   • Dependency check               │
        └────────────┬────────────────────────┘
                     │
        ┌────────────▼────────────────────────┐
        │   Artifact Generation              │
        │   • Create NuGet packages          │
        │   • Generate test reports          │
        │   • Upload to artifact store       │
        └────────────┬────────────────────────┘
                     │
              ┌──────▼──────┐
              │ CI PASSED ✓ │  (or FAILED ✗)
              └─────────────┘
```

### Continuous Deployment (CD) - On Release Tags

```
┌──────────────────────────────────────────┐
│     TAG CREATED: v3.5.0                 │
│     (Released from main branch)         │
└────────────┬─────────────────────────────┘
             │
    ┌────────▼───────────────────────┐
    │  Integration Tests (200+)       │
    │  • Real databases             │
    │  • Service interactions        │
    │  • Timeout: 15 minutes         │
    └────────┬───────────────────────┘
             │
    ┌────────▼───────────────────────┐
    │  E2E Tests (80+)               │
    │  • Full user workflows        │
    │  • Multi-service scenarios    │
    │  • Timeout: 30 minutes        │
    └────────┬───────────────────────┘
             │
    ┌────────▼───────────────────────┐
    │  Build Release Artifacts       │
    │  • Create installers          │
    │  • Sign binaries              │
    │  • Generate checksums         │
    └────────┬───────────────────────┘
             │
    ┌────────▼───────────────────────┐
    │  Deploy to Staging             │
    │  • Deploy to staging env       │
    │  • Smoke tests                │
    │  • Manual approval required   │
    └────────┬───────────────────────┘
             │
    ┌────────▼───────────────────────┐
    │  Deploy to Production          │
    │  • Blue-green deployment      │
    │  • Health checks              │
    │  • Rollback on failure        │
    └────────┬───────────────────────┘
             │
      ┌──────▼──────┐
      │DEPLOYED ✓   │  (or ROLLED BACK)
      └─────────────┘
```

---

## GitHub Actions Workflows

### 1. `ci.yml` - Pull Request Checks

```yaml
name: CI

on:
  pull_request:
    branches: [develop, main]
  push:
    branches: [develop, main]

jobs:
  build:
    runs-on: windows-latest
    timeout-minutes: 15
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Lint & Style
        run: dotnet format --verify-no-changes --verbosity diagnostic
      
      - name: Unit Tests
        run: dotnet test ./tests/Unit --no-build --logger "trx;LogFileName=test-results.trx"
      
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
      
      - name: Security Scan
        run: dotnet list package --vulnerable
```

### 2. `deploy.yml` - Release Pipeline

```yaml
name: Deploy

on:
  push:
    tags:
      - 'v*'

jobs:
  test:
    runs-on: windows-latest
    timeout-minutes: 45
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      
      - name: Integration Tests
        run: dotnet test ./tests/Integration --no-restore
      
      - name: E2E Tests
        run: dotnet test ./tests/E2E --no-restore
      
      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: '**/TestResults/'

  build-artifacts:
    needs: test
    runs-on: windows-latest
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      
      - name: Create Release Build
        run: dotnet build --configuration Release
      
      - name: Package for Distribution
        run: |
          dotnet publish -c Release -o ./dist
          Compress-Archive -Path ./dist -DestinationPath MonadoBlade.zip
      
      - name: Create Release
        uses: actions/create-release@v1
        with:
          tag_name: ${{ github.ref }}
          files: MonadoBlade.zip

  deploy:
    needs: build-artifacts
    runs-on: windows-latest
    
    steps:
      - name: Deploy to Staging
        run: ./scripts/deploy.ps1 -Environment Staging
      
      - name: Smoke Tests
        run: ./scripts/smoke-tests.ps1
      
      - name: Await Manual Approval
        uses: softprops/action-gh-release@v1
      
      - name: Deploy to Production
        run: ./scripts/deploy.ps1 -Environment Production
```

### 3. `nightly.yml` - Extended Testing

```yaml
name: Nightly Tests

on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM UTC daily

jobs:
  extended-tests:
    runs-on: windows-latest
    timeout-minutes: 120
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      
      - name: Full Test Suite
        run: |
          dotnet test ./tests/Unit
          dotnet test ./tests/Integration
          dotnet test ./tests/E2E
          dotnet test ./tests/Performance
```

---

## Code Quality Gates

### SonarQube Integration

```
PR → SonarQube Analysis → Quality Gate Check

Quality Gate Criteria:
  ✓ Coverage on New Code: >= 80%
  ✓ Duplicated Lines: < 3%
  ✓ Code Smells: A rating (0 issues)
  ✓ Bugs: A rating (0 issues)
  ✓ Vulnerabilities: A rating (0 security issues)
  ✓ Maintainability Index: >= 85
```

### Code Coverage Requirements

| Area | Minimum | Target |
|------|---------|--------|
| Core Services | 85% | 95% |
| Data Access | 80% | 90% |
| Business Logic | 85% | 95% |
| UI/ViewModels | 70% | 85% |
| Overall | 80% | 90% |

---

## Release Process

### Semantic Versioning

Format: `MAJOR.MINOR.PATCH-PRERELEASE+BUILD`

Example: `3.5.0`, `3.5.0-beta.1`, `3.5.0-rc.1`

### Release Checklist

1. **Prepare Release Branch**
   ```bash
   git checkout -b release/v3.5.0 develop
   ```

2. **Version Bumps**
   - Update version in all .csproj files
   - Update in appsettings.json
   - Update in README.md
   - Update CHANGELOG.md

3. **Create PR to main**
   - Title: `[release] v3.5.0`
   - Include release notes
   - Require 2 approvals

4. **Merge to main**
   - Use squash & merge
   - Create annotated tag: `git tag -a v3.5.0 -m "Release 3.5.0"`
   - Push tag: `git push origin v3.5.0`

5. **CI/CD Deployment**
   - GitHub Actions triggers automatically
   - Tests run (integration + E2E)
   - Build artifacts created
   - Manual approval required for production

6. **Post-Release**
   - Merge back to develop
   - Create GitHub Release with notes
   - Publish release notes to documentation site

---

## Team Collaboration

### CODEOWNERS File

```
# Global owners
* @team-leads

# Service-specific owners
/src/MonadoBlade.Services/Data/ @data-service-team
/src/MonadoBlade.Services/CloudSync/ @cloud-sync-team
/src/MonadoBlade.Services/ML/ @ml-team
/src/MonadoBlade.GUI/ @ui-team
/docs/ @documentation-team
```

### Communication Channels

| Channel | Purpose |
|---------|---------|
| **GitHub Discussions** | Architecture decisions, design proposals |
| **GitHub Issues** | Bug reports, feature requests, tasks |
| **GitHub Projects** | Sprint planning, roadmap tracking |
| **Pull Request Comments** | Code review feedback |
| **Slack #engineering** | Daily coordination |
| **Weekly Arch Sync** | Architecture review meetings |

### Contribution Workflow

1. **Assign yourself** to issue in GitHub Projects
2. **Create feature branch** from develop
3. **Commit with clear messages** (conventional commits)
4. **Push and create PR** (use template)
5. **Request reviewers** (code owners auto-requested)
6. **Address feedback** (push additional commits)
7. **Approve and merge** (squash & merge strategy)
8. **Close issue** (GitHub auto-closes on merge)

---

## Security Policies

### Secrets Management

- ✅ Use GitHub Secrets for sensitive data
- ✅ No hardcoded credentials in code
- ✅ Rotate secrets regularly
- ✅ Audit secret access logs

### Dependency Management

- ✅ Automated security vulnerability scanning
- ✅ Regular dependency updates (weekly)
- ✅ Breaking change reviews before update
- ✅ SBOM (Software Bill of Materials) generation

### Access Control

- ✅ Branch protection on main/develop
- ✅ Required code reviews
- ✅ Enforce MFA for administrators
- ✅ Audit all deployments

---

## Next Steps

### Before Go-Live

1. ✅ **Complete Service Interfaces** (PHASE 1 TASK 2)
2. ✅ **Set Up GitHub Organization** (this document)
3. ⏳ Implement service implementations
4. ⏳ Configure GitHub Actions workflows
5. ⏳ Set up SonarQube integration
6. ⏳ Establish team access & permissions
7. ⏳ Create initial project board

### Estimated Timeline

| Task | Owner | ETA | Duration |
|------|-------|-----|----------|
| Service implementations | Dev Team | Week 2 | 2 weeks |
| GitHub Actions setup | DevOps | Week 1 | 3 days |
| SonarQube integration | DevOps | Week 1 | 2 days |
| Team onboarding | Team Lead | Week 2 | 1 day |
| Go-live | Release Mgr | Week 3 | N/A |

---

## Document Metadata

**Created**: 2026-04-23  
**Last Updated**: 2026-04-23  
**Owner**: Architect Lead  
**Status**: ✅ APPROVED  
**Next Review**: Before PHASE 2 completion

---

**PHASE 1 COMPLETE** ✅  
**Deliverables Summary**:
- ✅ INTEGRATION_ARCHITECTURE.md (Task 1)
- ✅ 6 Service Interfaces + Error Contracts (Task 2)
- ✅ GITHUB_UNIFIED_PLAN.md (Task 3)
- ✅ 3 Key Architectural Insights Documented
