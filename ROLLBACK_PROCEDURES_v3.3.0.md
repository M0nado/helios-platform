# MonadoBlade v3.3.0 Rollback Procedures

## Overview

This document provides detailed procedures for rolling back MonadoBlade to the previous stable version (v3.1.0) in case of critical issues.

## Pre-Rollback Assessment

### Severity Classification

**P0 (Critical - Immediate Rollback)**
- Complete service unavailability
- Data loss or corruption
- Security breach
- Revenue-impacting issue
- >50% user impact

**P1 (High - Quick Rollback)**
- Major feature broken
- Performance degradation >30%
- >10% user impact
- Security vulnerability (non-critical)
- Data inconsistency

**P2 (Medium - Evaluate)**
- Specific feature broken for subset of users
- Minor performance issue
- Limited user impact (<5%)

**P3 (Low - Monitor)**
- Cosmetic issues
- Edge case behavior
- <1% user impact

### Go/No-Go Decision Tree

```
Is it P0 or P1?
├─ YES → ROLLBACK IMMEDIATELY
├─ NO  → Can it be hotfixed in < 2 hours?
│        ├─ YES → Proceed with fix + re-deploy
│        └─ NO  → Evaluate rollback vs. disable feature
└─ UNKNOWN → Escalate to incident commander
```

## Quick Rollback (< 15 minutes)

### Step 1: Stop Current Deployment
```powershell
# SSH to production server
cd /opt/monado-blade

# Stop services gracefully
systemctl stop monado-blade
systemctl stop monado-blade-api

# Verify stopped
systemctl status monado-blade
```

### Step 2: Revert Code
```powershell
# Check current commit
git log --oneline -1

# Revert to last stable tag
git checkout v3.1.0

# Verify checkout
git log --oneline -1
```

### Step 3: Rebuild and Deploy
```powershell
# Clean previous build
dotnet clean

# Rebuild application
dotnet build -c Release

# Run database migrations (if needed)
dotnet ef database update

# Copy to deployment directory
cp -r bin/Release/net8.0 /opt/monado-blade/current
```

### Step 4: Start Services
```powershell
# Start services
systemctl start monado-blade
systemctl start monado-blade-api

# Verify startup
systemctl status monado-blade
systemctl status monado-blade-api

# Check logs
tail -f /var/log/monado-blade/app.log
```

### Step 5: Health Checks
```powershell
# Check API health endpoint
curl -X GET http://localhost:5000/api/health

# Verify database connectivity
curl -X GET http://localhost:5000/api/health/db

# Check authentication
curl -X POST http://localhost:5000/api/auth/verify \
  -H "Content-Type: application/json" \
  -d '{"token":"<test_token>"}'
```

## Detailed Rollback Procedures

### Procedure 1: Git-Based Rollback (Recommended)

**Scenario**: Issue discovered shortly after deployment

```powershell
# 1. Identify issue commit
$lastGoodCommit = "2c92c24"  # v3.1.0
$currentCommit = "abc1234"   # v3.3.0

# 2. Create rollback branch
git checkout -b rollback/v3.3.0-issue-$(date +%Y%m%d-%H%M%S)

# 3. Revert commit
git revert $currentCommit --no-edit

# 4. Push rollback branch
git push origin rollback/v3.3.0-issue-*

# 5. Create and merge rollback PR
# Note: In production, this would be expedited through the PR process

# 6. Deploy rollback commit
git checkout master
git pull origin master
dotnet publish -c Release -o ./publish
# Copy publish directory to production server
```

### Procedure 2: Database Rollback (If Schema Changed)

**Important**: Only if database schema changed in v3.3.0

```powershell
# 1. Backup current database
mysqldump -u root -p monado_blade > monado_blade_v3.3.0_backup.sql

# 2. Restore from v3.1.0 backup
mysql -u root -p monado_blade < monado_blade_v3.1.0_backup.sql

# 3. Verify data integrity
mysql -u root -p monado_blade -e "SELECT COUNT(*) FROM users;"
mysql -u root -p monado_blade -e "SELECT COUNT(*) FROM sessions;"

# 4. Verify no corruption
mysql -u root -p monado_blade -e "CHECK TABLE users, sessions, logs;"
```

### Procedure 3: Feature Flag Rollback (Gradual)

**Scenario**: Can disable problematic feature without full rollback

```csharp
// In appsettings.json
{
  "FeatureFlags": {
    "Phase2Optimization": false,      // Disable Phase 2
    "Phase1Optimization": true,        // Keep Phase 1 active
    "Task11Implementation": false      // Disable Task 11
  }
}
```

## Post-Rollback Procedures

### Immediate Actions (First 30 minutes)

1. **Notify Stakeholders**
   - Send incident notification
   - Include brief summary of issue
   - Provide rollback completion time
   - Set expectation for root cause analysis

2. **Monitor System Stability**
   - Watch error rates (should return to baseline)
   - Monitor performance metrics
   - Check user activity/engagement
   - Review logs for any related errors

3. **Collect Evidence**
   - Save deployment logs
   - Export error logs from v3.3.0
   - Export performance metrics
   - Document system state

### Short-Term Actions (2-4 hours)

1. **Root Cause Analysis**
   - Review code changes in problematic commit
   - Check test coverage for changed areas
   - Identify missing test cases
   - Document root cause findings

2. **Incident Post-Mortem**
   - Schedule within 24 hours
   - Review what happened
   - Identify why it wasn't caught
   - Create action items to prevent recurrence

3. **Communication**
   - Send detailed incident report
   - Include timeline and impact analysis
   - Provide root cause summary
   - Outline remediation plan

### Medium-Term Actions (24-48 hours)

1. **Code Review**
   - Add additional test cases
   - Enhance monitoring/alerting
   - Update deployment procedures if needed
   - Document lessons learned

2. **Redeployment Planning**
   - Fix identified issues
   - Add test coverage
   - Enhance monitoring
   - Schedule re-release

## Rollback Verification Checklist

After rollback is complete, verify:

```
System Health
─────────────
[ ] API responding on all endpoints
[ ] Database queries executing successfully
[ ] Authentication system functional
[ ] Logging operational
[ ] Email notifications working
[ ] Background jobs processing

Performance
───────────
[ ] Response times at baseline
[ ] Error rates < 0.1%
[ ] Memory usage normal
[ ] CPU usage normal
[ ] Disk I/O normal

Features
────────
[ ] User login/logout working
[ ] Core features accessible
[ ] File uploads/downloads working
[ ] Export functionality working
[ ] API integrations working
[ ] Scheduled jobs running

User Impact
───────────
[ ] Active users online
[ ] No spike in error emails
[ ] Support tickets normal
[ ] User activity normal
```

## Emergency Contacts

| Role | Name | Phone | Email |
|------|------|-------|-------|
| Incident Commander | TBD | +1-XXX-XXX-XXXX | xxx@example.com |
| DevOps Lead | TBD | +1-XXX-XXX-XXXX | xxx@example.com |
| Database Admin | TBD | +1-XXX-XXX-XXXX | xxx@example.com |
| Product Manager | TBD | +1-XXX-XXX-XXXX | xxx@example.com |

## Tools & Access

```
Required Access:
- SSH access to production servers
- Database admin credentials
- Git repository admin access
- Slack/incident management system
- Monitoring dashboard (Datadog/NewRelic)
- Log aggregation (ELK/Splunk)

Tools Needed:
- dotnet CLI (v8.0+)
- git
- MySQL CLI
- curl
- SSH client
```

## Testing Rollback Procedures

**Recommendation**: Test rollback procedure quarterly in staging environment

```powershell
# 1. Create staging from production database
mysqldump -h prod-db -u root -p monado_blade | \
  mysql -h staging-db -u root -p monado_blade

# 2. Deploy v3.3.0 to staging
git checkout feature/phase-2-optimization
dotnet publish -c Release
# Deploy to staging

# 3. Simulate failure
# (introduce test error, simulate high error rate, etc.)

# 4. Execute rollback procedure
# (follow procedures above)

# 5. Verify success
curl http://staging-api/health

# 6. Document lessons learned
```

---

**Last Updated**: 2024-01-XX
**Tested**: 2024-01-XX
**Status**: Ready for Production
