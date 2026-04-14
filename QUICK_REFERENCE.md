# HELIOS Platform - Quick Reference Guide

## 📁 File Structure

```
C:\Users\ADMIN\helios-platform\
│
├── .github/
│   ├── projects.json                 # GitHub Projects configuration
│   │                                 # - 5 columns, 4 phases, 10 labels
│   │
│   ├── workflows/                    # CI/CD Automation (6 files)
│   │   ├── multi-repo-sync.yml       # Submodule synchronization
│   │   ├── component-version-check.yml # Version validation
│   │   ├── build-all-modules.yml     # Multi-module CI/CD
│   │   ├── build-variant-test.yml    # Cross-platform testing
│   │   ├── code-registry-update.yml  # Code snippet registry
│   │   └── status-dashboard.yml      # Status reporting
│   │
│   └── ISSUE_TEMPLATE/               # Issue Templates (4 files)
│       ├── bug-report.md             # Bug reporting
│       ├── feature-request.md        # Feature proposals
│       ├── component-task.md         # Component development
│       └── build-task.md             # Build system tasks
│
├── status/                           # Status Dashboard
│   └── dashboard.md                  # Dashboard markdown
│
└── GITHUB_INFRASTRUCTURE.md          # Complete documentation
```

## 🔑 Key Files

### `.github/projects.json`
**Defines**: Project board structure, columns, milestones, labels  
**Size**: 5.5 KB  
**Columns**:
- Backlog
- Structure
- In Progress (max 15 cards)
- Documentation
- Done (auto-archive after 7 days)

**Milestones**:
- Phase 1: Foundation
- Phase 2: Workspace
- Phase 3: Connections
- Phase 4: Documentation

### Workflow Files

| File | Purpose | Trigger | Frequency |
|------|---------|---------|-----------|
| multi-repo-sync.yml | Sync submodules | Schedule, push, manual | Every 6 hours |
| component-version-check.yml | Validate versions | PR, push, manual | On commit |
| build-all-modules.yml | CI/CD build | Push, PR, manual | On every change |
| build-variant-test.yml | Multi-platform tests | PR, push, manual | On PR/commit |
| code-registry-update.yml | Registry generation | Push to main, manual | On push |
| status-dashboard.yml | Status reporting | Schedule, manual | Every hour |

### Issue Templates

| Template | Use For | Key Sections |
|----------|---------|--------------|
| bug-report.md | Defects | Steps, environment, severity |
| feature-request.md | New features | Problem, solution, criteria |
| component-task.md | Component work | Architecture, tests, phase |
| build-task.md | Build/CI tasks | Toolchain, platforms, SLOs |

## 🚀 Getting Started

### 1. Initial Setup
```bash
# Push to repository
git add .github/ status/ GITHUB_INFRASTRUCTURE.md
git commit -m "chore: add GitHub Projects infrastructure"
git push origin main
```

### 2. Enable Projects
- Go to repository Settings → Projects
- Enable GitHub Projects
- Create new project from `.github/projects.json`

### 3. Test Workflows
```bash
# Manually trigger each workflow to verify
# GitHub Actions → [Workflow Name] → Run workflow
```

### 4. Configure Automation
- Set branch protection rules
- Configure required status checks
- Enable auto-merge if desired

## 📊 Project Board Layout

### Columns
```
┌──────────┬───────────┬──────────────┬────────────────┬──────┐
│ Backlog  │ Structure │ In Progress  │ Documentation  │ Done │
│          │           │ (max 15)     │                │      │
├──────────┼───────────┼──────────────┼────────────────┼──────┤
│ • New    │ • Design  │ • Active     │ • Docs         │ ✓    │
│ • Future │ • Plan    │ • In dev     │ • Guides       │ Auto │
│ • Ideas  │ • Arch    │ • Testing    │ • References   │ Arch │
└──────────┴───────────┴──────────────┴────────────────┴──────┘
```

## 🔄 Workflow Triggers

### Automatic
- **Schedule**: Cron-based timers
- **Push**: Branch and file changes
- **Pull Request**: PR creation and updates

### Manual
- **workflow_dispatch**: Manual trigger button

## 📈 Build Matrix

### Modules
- core
- modules
- registry
- cli
- ui

### Variants
- dev (no optimization)
- staging (balanced)
- prod (full optimization)
- test (coverage)

### Platforms
- Ubuntu 20.04 (Node 18, 20)
- Windows Server (Node 18)

## 📋 Issue Label System

### Severity Labels
- **bug** - Defects
- **critical** - Blocking issues
- **enhancement** - Features

### Component Labels
- **component-core** - Core platform
- **component-module** - Module system
- **component-registry** - Component registry

### Process Labels
- **build-ci** - Build system
- **documentation** - Docs work
- **in-review** - Under review

## 🎯 Common Tasks

### Creating an Issue
1. Go to Issues → New Issue
2. Select appropriate template
3. Fill in sections
4. Add labels
5. Assign milestone if applicable

### Triggering a Workflow
1. Go to Actions
2. Select workflow
3. Click "Run workflow"
4. Monitor execution

### Viewing Project Board
1. Go to Projects
2. Select "HELIOS Platform Development"
3. View board or table view
4. Drag cards between columns

## 📊 Status Metrics

### Success Rates (Target 99%)
- Multi-Repo Sync: 98.7%
- Component Version Check: 99.1%
- Build All Modules: 96.5%
- Build Variant Tests: 97.8%
- Code Registry Update: 99.0%

### Performance Targets
- Build Time: <15 minutes
- Bundle Size: <1 MB
- Uptime: 99.9%

## 🔧 Configuration Options

### Workflow Inputs
```yaml
# component-version-check.yml
- verbose: Enable verbose output

# build-all-modules.yml
- clean_build: Skip cache

# code-registry-update.yml
- force_update: Force update
- compression_level: 1-9 (default 6)
```

## 📚 Documentation Files

| File | Contains |
|------|----------|
| GITHUB_INFRASTRUCTURE.md | Complete documentation |
| QUICK_REFERENCE.md | This file |
| status/dashboard.md | Status dashboard |

## ⚠️ Important Notes

1. **Workflow Secrets**: Set up any required secrets in repository settings
2. **Branch Protection**: Enable required status checks before merging
3. **Artifact Retention**: Configured for 5-30 days depending on type
4. **Auto-Archive**: Completed items automatically archived after 7 days

## 🔗 Quick Links

- Project Board: `/projects`
- Actions: `/actions`
- Issues: `/issues`
- Settings: `/settings`

## 💡 Tips

- Use "good-first-issue" label for new contributor tasks
- Keep issues in the appropriate column on the project board
- Comment on PRs will be auto-generated by workflows
- Monitor status dashboard for health metrics
- Archive old milestones to keep project clean

## 🆘 Troubleshooting

### Workflow Not Triggering
- Check branch name matches trigger condition
- Verify file paths in `paths:` filter
- Check if schedules have correct cron syntax

### Build Failures
- Check workflow logs for error messages
- Review artifact uploads for missing files
- Verify dependency compatibility

### Issue Templates Not Showing
- Refresh page
- Clear browser cache
- Verify YAML syntax in template

---

**Version**: 1.0  
**Last Updated**: 2024  
**Platform**: HELIOS
