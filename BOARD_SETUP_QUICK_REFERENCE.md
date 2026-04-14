# HELIOS Platform - Board Automation Scripts Quick Reference

## 📍 File Locations

```
C:\Users\ADMIN\helios-platform\
├── scripts/
│   ├── board-setup/
│   │   ├── setup-custom-fields.ps1    [Main board field setup]
│   │   ├── setup-templates.ps1         [8 phase templates]
│   │   ├── setup-automation-rules.ps1  [4 automation rules]
│   │   ├── setup-views.ps1             [6 board views]
│   │   ├── setup-board.ps1             [Master orchestrator]
│   │   └── validate-board.ps1          [Validation & health check]
│   ├── integration/
│   │   └── setup-github-ecosystem.ps1  [GitHub integration]
│   ├── optimization/
│   │   ├── run-optimization.ps1        [Performance tuning]
│   │   └── setup-monitoring.ps1        [Monitoring setup]
│   ├── setup/
│   │   └── complete-system-setup.ps1   [Master setup orchestrator]
│   ├── config/
│   │   ├── board-config.json           [Board configuration]
│   │   ├── integration-mapping.json    [Integration mappings]
│   │   ├── optimization-parameters.json [Optimization settings]
│   │   ├── monitoring-rules.json       [Monitoring thresholds]
│   │   └── alert-thresholds.json       [Alert configuration]
│   └── README.md                       [Complete documentation]
```

## 🚀 Quick Commands

### Test Setup (Safe - No Changes)
```powershell
cd C:\Users\ADMIN\helios-platform
.\scripts\setup\complete-system-setup.ps1 `
    -GitHubToken "ghp_YOUR_TOKEN" `
    -ProjectNumber 1 `
    -OrganizationName "your-org" `
    -RepositoryName "platform" `
    -RepositoryOwner "your-org" `
    -DryRun `
    -Verbose
```

### Full Setup (Production)
```powershell
.\scripts\setup\complete-system-setup.ps1 `
    -GitHubToken "ghp_YOUR_TOKEN" `
    -ProjectNumber 1 `
    -OrganizationName "your-org" `
    -RepositoryName "platform" `
    -RepositoryOwner "your-org" `
    -Verbose
```

### Board Setup Only
```powershell
.\scripts\board-setup\setup-board.ps1 `
    -GitHubToken "ghp_YOUR_TOKEN" `
    -ProjectNumber 1 `
    -OrganizationName "your-org" `
    -Verbose
```

### Validate Configuration
```powershell
.\scripts\board-setup\validate-board.ps1 `
    -GitHubToken "ghp_YOUR_TOKEN" `
    -ProjectNumber 1 `
    -OrganizationName "your-org" `
    -GenerateReport `
    -Verbose
```

### Custom Fields Only
```powershell
.\scripts\board-setup\setup-custom-fields.ps1 `
    -GitHubToken "ghp_YOUR_TOKEN" `
    -ProjectNumber 1 `
    -OrganizationName "your-org" `
    -Verbose
```

### GitHub Integration
```powershell
.\scripts\integration\setup-github-ecosystem.ps1 `
    -GitHubToken "ghp_YOUR_TOKEN" `
    -RepositoryName "platform" `
    -RepositoryOwner "your-org" `
    -ProjectNumber 1 `
    -OrganizationName "your-org" `
    -Verbose
```

## 📊 What Gets Created

### Custom Fields (25 Total)
**Tier 1**: Priority, Sprint, Effort, Component, DueDate
**Tier 2**: AssignedTo, ProgressStatus, QAStatus, BlockedBy, TimeEstimate
**Tier 3**: ReviewStatus, ReviewedBy, ApprovalRequired, RiskLevel, ComplianceCheck
**Tier 4**: DeploymentEnvironment, DeploymentStatus, IntegrationPoints, DependsOn, DataMigration
**Tier 5**: SuccessMetrics, UserImpact, PerformanceImpact, Documentation, ArchitectureDecision

### Phase Templates (8 Total)
1. Requirements & Planning
2. Design & Architecture
3. Development
4. Testing & QA
5. Code Review & Integration
6. Pre-Production Staging
7. Production Deployment
8. Post-Launch & Monitoring

### Automation Rules (4 Total)
1. Auto-assign Priority
2. Update on PR Merge
3. Escalate Critical Items
4. Deploy When QA Approved

### Board Views (6 Total)
1. All Tasks - By Priority
2. Sprint View (Current Sprint)
3. Critical & High Priority
4. Deployment Pipeline
5. Review Required
6. Team Workload

## 🔐 GitHub Token Requirements

Your token needs these scopes:
- ✓ repo - Full control of repositories
- ✓ workflow - Full control of workflows
- ✓ project - Full control of projects

Create token at: https://github.com/settings/tokens

## 📝 Output Files

### Logs (in logs/ directory)
- `board-setup_TIMESTAMP.log` - Master board setup
- `custom-fields-setup_TIMESTAMP.log` - Field creation
- `templates-setup_TIMESTAMP.log` - Template setup
- `automation-rules_TIMESTAMP.log` - Rules setup
- `views-setup_TIMESTAMP.log` - Views setup
- `validation_TIMESTAMP.log` - Validation results
- `github-ecosystem-integration_TIMESTAMP.log` - Integration
- `optimization_TIMESTAMP.log` - Optimization
- `complete-setup_TIMESTAMP.log` - Master orchestration

### Reports (JSON format)
- `*-report_TIMESTAMP.json` - Detailed execution report
- `*-backup_TIMESTAMP.json` - Configuration backup

### Documentation
- `*-documentation_TIMESTAMP.md` - Setup guides
- `*-guide_TIMESTAMP.md` - Feature guides

## ✅ Verification Checklist

After running setup, verify:
- [ ] 25 custom fields visible in GitHub Project
- [ ] 8 phase templates available
- [ ] 4 automation rules enabled
- [ ] 6 board views configured
- [ ] GitHub integration working
- [ ] Monitoring dashboards accessible
- [ ] All reports generated successfully
- [ ] Team can access and use board

## 🆘 Troubleshooting

| Problem | Solution |
|---------|----------|
| "GitHub Token invalid" | Verify token has correct scopes (repo, workflow, project) |
| "Project not found" | Verify project number and organization name |
| "Fields already exist" | Scripts handle duplicates; run validation to check status |
| "Integration not working" | Check GitHub token permissions; verify repository access |
| "Scripts won't run" | Ensure PowerShell 5.1+; Run as Administrator |
| "Logs show errors" | Review detailed log files in logs/ directory |

## 📚 Additional Resources

- **Full Documentation**: `scripts/README.md`
- **Implementation Guide**: `BOARD_AUTOMATION_IMPLEMENTATION.md`
- **Configuration Details**: See individual `.json` files in `scripts/config/`
- **Generated Guides**: Auto-generated documentation in `logs/` after each run

## 🎯 Typical Workflow

1. **Initial Test**
   ```powershell
   .\scripts\setup\complete-system-setup.ps1 -DryRun -Verbose
   ```

2. **Review Report**
   - Check logs/complete-setup-report_*.json
   - Review logs/complete-setup_*.log

3. **Run Setup**
   ```powershell
   .\scripts\setup\complete-system-setup.ps1 -Verbose
   ```

4. **Validate**
   ```powershell
   .\scripts\board-setup\validate-board.ps1 -GenerateReport
   ```

5. **Train Team**
   - Share scripts/README.md with team
   - Document any customizations

## 📞 Quick Help

```powershell
# Get help for any script
Get-Help .\scripts\setup\complete-system-setup.ps1 -Full

# Show examples
Get-Help .\scripts\setup\complete-system-setup.ps1 -Examples

# List all parameters
Get-Help .\scripts\setup\complete-system-setup.ps1 -Parameter *
```

## ⚡ Performance Tips

- Use `-DryRun` first to preview changes
- Run during off-peak hours for large projects
- Check logs for warnings and optimization suggestions
- Monitor resources during execution
- Disable non-essential GitHub Actions during setup

## 🔄 Update & Rollback

### To rollback changes:
1. Stop any running processes
2. Delete configuration from project
3. Restore from backup JSON files if needed
4. Re-run validation to confirm

### To update configuration:
1. Modify JSON configuration files in scripts/config/
2. Run affected setup script again
3. Validate changes with validate-board.ps1
4. Test before deployment to production

---

**Version**: 1.0  
**Last Updated**: April 13, 2026  
**Status**: Production Ready ✅

**For detailed information, see scripts/README.md and BOARD_AUTOMATION_IMPLEMENTATION.md**
