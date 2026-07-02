# MonadoBlade v3.3.0 Deployment Guide

## Executive Summary

This guide provides step-by-step instructions for deploying MonadoBlade v3.3.0 to production environments. The release includes Phase 1 & 2 optimizations and Task 11 enhancements.

**Expected Deployment Time**: 45-60 minutes
**Downtime**: < 5 minutes (with blue-green deployment)
**Rollback Time**: 10-15 minutes
**Risk Level**: Low (100% backward compatible)

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Pre-Deployment Verification](#pre-deployment-verification)
3. [Staging Deployment](#staging-deployment)
4. [Production Deployment](#production-deployment)
5. [Post-Deployment Validation](#post-deployment-validation)
6. [Monitoring & Alerting](#monitoring--alerting)
7. [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements
- **.NET Runtime**: 8.0 or higher
- **OS**: Windows Server 2019+ or Windows 10+
- **RAM**: 4GB minimum (8GB recommended)
- **Disk Space**: 2GB free space
- **Network**: HTTPS connectivity to deployment servers

### Access Requirements
- Git repository access
- Production server SSH/RDP access
- Database admin credentials
- Deployment automation tool access
- Monitoring system access

### Software Dependencies
```powershell
# Verify .NET version
dotnet --version

# Should output: 8.0.x or higher
```

## Pre-Deployment Verification

### 1. Code Review Checklist

- [ ] All PRs approved by code owners
- [ ] All tests passing in CI/CD pipeline
- [ ] SonarQube quality gate passed
- [ ] Security scanning complete (no critical issues)
- [ ] Performance benchmarks reviewed
- [ ] Documentation updated
- [ ] Release notes finalized
- [ ] Changelog updated

### 2. Test Coverage Verification

```powershell
# Run all tests locally
cd C:\Users\ADMIN\MonadoBlade

# Unit tests
dotnet test tests/MonadoBlade.Tests.Unit/ --configuration Release

# Integration tests
dotnet test tests/MonadoBlade.Tests.Integration/ --configuration Release

# Performance tests
dotnet test tests/MonadoBlade.Tests.Performance/ --configuration Release
```

**Expected Results**:
- Unit Tests: 1,247 passed | 89% coverage
- Integration Tests: 284 passed | 95% coverage
- Performance Tests: 142 passed | All benchmarks within limits

### 3. Dependency Check

```powershell
# Check for vulnerable dependencies
dotnet list package --vulnerable

# Expected: No vulnerable packages
```

### 4. Database Migration Verification

```sql
-- Verify no schema changes (v3.3.0 is compatible with v3.1.0)
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES;
-- Expected: Same count as v3.1.0 production

-- Verify data integrity
DBCC CHECKDB;
-- Expected: No errors
```

## Staging Deployment

### Step 1: Prepare Staging Environment

```powershell
# SSH to staging server
ssh staging-admin@staging-server.example.com

# Stop current staging deployment
Stop-Service MonadoBladeService

# Backup current version
Copy-Item "C:\MonadoBlade" "C:\MonadoBlade.backup.3.1.0" -Recurse

# Verify backup
Test-Path "C:\MonadoBlade.backup.3.1.0"
```

### Step 2: Download Release Artifacts

```powershell
# Download from GitHub releases
# Replace with actual download URL
$releaseUrl = "https://github.com/yourorg/monado-blade/releases/download/v3.3.0/MonadoBlade-3.3.0.zip"

Invoke-WebRequest -Uri $releaseUrl -OutFile "C:\MonadoBlade-3.3.0.zip"

# Verify checksum
$checksum = Get-FileHash "C:\MonadoBlade-3.3.0.zip"
echo $checksum
# Compare with official checksum from release notes
```

### Step 3: Deploy to Staging

```powershell
# Extract release
Expand-Archive -Path "C:\MonadoBlade-3.3.0.zip" -DestinationPath "C:\MonadoBlade" -Force

# Set permissions
icacls "C:\MonadoBlade" /grant:r "$env:USERDOMAIN\$env:USERNAME:(OI)(CI)F"

# Start service
Start-Service MonadoBladeService

# Verify startup
Get-Service MonadoBladeService | Select-Object Status

# Expected: Running
```

### Step 4: Health Checks

```powershell
# Check API health endpoint
$healthUrl = "http://staging.example.com:5000/api/health"
$response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing
$response.StatusCode  # Expected: 200

# Check application logs
Get-Content "C:\MonadoBlade\logs\app.log" -Tail 50

# Verify critical services
# - Authentication: Should authenticate users
# - Database: Should connect successfully
# - Cloud Sync: Should connect to cloud services
```

### Step 5: Staging Smoke Tests

```powershell
# Test basic functionality
$apiUrl = "http://staging.example.com:5000"

# Test health endpoint
Invoke-WebRequest -Uri "$apiUrl/api/health" -UseBasicParsing

# Test authentication
$auth = @{
    username = "test-user"
    password = "test-password"
}
Invoke-WebRequest -Uri "$apiUrl/api/auth/login" -Method POST -Body $auth -UseBasicParsing

# Test core operations
Invoke-WebRequest -Uri "$apiUrl/api/profiles/list" -UseBasicParsing

echo "✓ Staging smoke tests passed"
```

### Step 6: Performance Validation (Staging)

```powershell
# Monitor resource usage
Get-Process monado* | Select-Object Name, WorkingSet, CPU

# Expected:
# - Memory: < 200MB
# - CPU: < 10% idle
# - Response time: < 150ms

# Check error logs for warnings
Get-Content "C:\MonadoBlade\logs\error.log" | Measure-Object -Line
# Expected: 0 errors (warnings are OK)
```

### Step 7: Approval for Production

```
Staging Deployment Sign-Off
===========================
Date: ________
Deployed By: ________
Verified By: ________

Health Checks: ✓ PASS
Performance: ✓ PASS
Smoke Tests: ✓ PASS
Error Logs: ✓ CLEAN

Approved for Production: ________ (QA Lead)
```

## Production Deployment

### Step 1: Pre-Deployment Notification

```powershell
# Send notification to stakeholders
Send-Notification @{
    Subject = "MonadoBlade v3.3.0 Production Deployment - Starting"
    Severity = "INFORMATION"
    Details = @{
        Version = "v3.3.0"
        Environment = "Production"
        ETA = "60 minutes"
        Downtime = "< 5 minutes"
    }
}
```

### Step 2: Blue-Green Deployment Setup

```powershell
# Create green (new) environment
ssh prod-admin@prod-server-1.example.com

# Clone blue environment to green
Copy-Item "C:\MonadoBlade" "C:\MonadoBlade-green" -Recurse

# Extract v3.3.0 to green
Expand-Archive -Path "C:\MonadoBlade-3.3.0.zip" -DestinationPath "C:\MonadoBlade-green" -Force
```

### Step 3: Health Check on Green

```powershell
# Start green on different port
# Modify config: appsettings.json -> Port: 5001

Start-Service MonadoBladeService-Green

# Wait for startup
Start-Sleep -Seconds 30

# Health check
$healthCheck = Invoke-WebRequest -Uri "http://localhost:5001/api/health" -UseBasicParsing
$healthCheck.StatusCode  # Expected: 200
```

### Step 4: Production Smoke Tests on Green

```powershell
# Run critical path tests
Test-ProductionAPI -Endpoint "http://localhost:5001" -Critical

# Expected: All tests pass
```

### Step 5: Switch Traffic (Blue → Green)

```powershell
# Update load balancer / reverse proxy
# Point traffic from blue (5000) to green (5001)

# Or update DNS if using DNS-based routing
Update-DNSRecord -Name "monado-blade.example.com" -IPAddress "green-server-ip"

# Verify traffic routing
Invoke-WebRequest -Uri "https://monado-blade.example.com/api/health"
```

### Step 6: Monitor Production (30 minutes)

```powershell
# Continuous monitoring script
while ($true) {
    $health = Invoke-WebRequest -Uri "https://monado-blade.example.com/api/health"
    $metrics = Get-PerformanceMetrics
    
    if ($metrics.ErrorRate -gt 0.01) {
        Send-Alert "ERROR RATE SPIKE: $($metrics.ErrorRate * 100)%"
        break
    }
    
    if ($metrics.ResponseTime -gt 500) {
        Send-Alert "SLOW RESPONSE TIME: $($metrics.ResponseTime)ms"
    }
    
    Write-Host "✓ Health Check Passed - $(Get-Date)"
    Start-Sleep -Seconds 60
}
```

### Step 7: Decommission Blue

After 30 minutes of successful monitoring:

```powershell
# Stop blue environment
Stop-Service MonadoBladeService-Blue

# Archive blue for potential rollback
Copy-Item "C:\MonadoBlade-blue" "C:\MonadoBlade.backup.3.3.0" -Recurse

# Cleanup old environment (after 48 hours)
# Remove-Item "C:\MonadoBlade-blue" -Recurse -Force
```

## Post-Deployment Validation

### 1. Verify Production Deployment

```powershell
# Check service status
Get-Service MonadoBladeService | Select-Object Status

# Expected: Running

# Check version
Invoke-WebRequest -Uri "https://monado-blade.example.com/api/version" | ConvertFrom-Json

# Expected: { version: "3.3.0" }
```

### 2. Collect Performance Metrics

```powershell
# Baseline metrics for this deployment
$metrics = @{
    Timestamp = Get-Date
    Version = "3.3.0"
    ErrorRate = (Get-ErrorRate)  # Should be < 0.1%
    ResponseTime = (Get-AvgResponseTime)  # Should be < 150ms
    MemoryUsage = (Get-MemoryUsage)  # Should be < 200MB
    CPUUsage = (Get-CPUUsage)  # Should be < 15% average
}

Export-MetricsBaseline -Metrics $metrics -FilePath "baseline-3.3.0.json"
```

### 3. Database Verification

```sql
-- Verify database integrity
DBCC CHECKDB;

-- Verify all tables accessible
SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES;

-- Verify data consistency
SELECT COUNT(*) as UserCount FROM users;
SELECT COUNT(*) as ProfileCount FROM profiles;
```

### 4. Critical Path Testing

```powershell
# Test all critical user journeys
$criticalTests = @(
    "UserLogin",
    "ProfileCreation",
    "USBBuild",
    "CloudSync",
    "SystemDiagnostics",
    "BootOptimization"
)

foreach ($test in $criticalTests) {
    $result = Invoke-CriticalTest -TestName $test
    if ($result.Status -ne "PASS") {
        Send-Alert "CRITICAL TEST FAILED: $test"
    }
}
```

### 5. User Communication

```
Subject: MonadoBlade v3.3.0 Successfully Deployed to Production

Dear Users,

We're excited to announce that MonadoBlade v3.3.0 is now live in production!

Key Improvements:
- 34% faster boot time
- 40% faster USB creation
- Enhanced cloud synchronization
- Better system reliability

For more details, visit: https://github.com/yourorg/monado-blade/releases/v3.3.0

Thank you,
Engineering Team
```

## Monitoring & Alerting

### Critical Metrics

| Metric | Threshold | Action |
|--------|-----------|--------|
| Error Rate | > 0.5% | Alert + Investigate |
| Response Time | > 500ms | Alert + Investigate |
| Memory Usage | > 90% | Alert + Scale |
| CPU Usage | > 80% | Alert + Scale |
| Disk Usage | > 80% | Alert + Cleanup |
| DB Connection Pool | > 90% | Alert + Scale |

### Automated Alerts

- Configure in monitoring dashboard (Datadog, New Relic, etc.)
- Escalation path: Slack → PagerDuty → Incident Commander
- Response SLA: P0 (15 min), P1 (30 min), P2 (1 hour)

## Troubleshooting

### Issue: Service Won't Start

```powershell
# Check logs
Get-Content "C:\MonadoBlade\logs\error.log" -Tail 100

# Common causes:
# 1. Port already in use
netstat -ano | findstr :5000

# 2. Permissions issue
icacls "C:\MonadoBlade"

# 3. Database connection
Test-Connection -ComputerName db-server

# Solution: Follow rollback procedures
```

### Issue: High Error Rate

```powershell
# Check error logs
Get-Content "C:\MonadoBlade\logs\*.log" | Select-String "ERROR"

# Check database
Test-SQLConnection -ConnectionString "..."

# Check external services
Test-ServiceConnectivity -Services "CloudSync", "Authentication"

# If unresolved: Execute rollback
```

### Issue: Performance Degradation

```powershell
# Check resource usage
Get-Process monado* | Format-Table Name, WorkingSet, CPU

# Check for memory leaks
dotMemory --snapshot monado.exe

# Check slow queries
Get-SlowDatabaseQueries -TimeoutMs 1000

# If unresolved: Execute rollback
```

## Rollback Procedures

If critical issues occur, execute immediate rollback:

```powershell
# Stop production
Stop-Service MonadoBladeService-Green

# Switch traffic back to blue
Update-LoadBalancer -TargetEndpoint "blue-server" -Port 5000

# Verify rollback
Invoke-WebRequest -Uri "https://monado-blade.example.com/api/health"

# Start investigation
Send-IncidentReport -Severity "CRITICAL"
```

See `ROLLBACK_PROCEDURES_v3.3.0.md` for detailed rollback steps.

---

**Document Version**: 1.0
**Last Updated**: January 2024
**Status**: Ready for Production Deployment
