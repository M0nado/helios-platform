# HELIOS Platform v3.6.0 - Deployment & Operations Guide

**Version**: 3.6.0  
**Status**: Production Ready ✅  
**Last Updated**: 2026-05-15

## Table of Contents

1. [Deployment Checklist](#deployment-checklist)
2. [Configuration Guide](#configuration-guide)
3. [Monitoring & Health Checks](#monitoring--health-checks)
4. [Updating to v3.6.0](#updating-to-v360)
5. [Rollback Procedures](#rollback-procedures)
6. [Performance Tuning](#performance-tuning)

---

## Deployment Checklist

### Pre-Deployment

- [ ] **Environment Assessment**
  - [ ] Windows Server 2022 or Windows 11 Pro
  - [ ] Minimum 4GB RAM, 8GB recommended
  - [ ] 10GB free disk space minimum
  - [ ] .NET 8.0 SDK installed
  - [ ] Network connectivity verified

- [ ] **Security Review**
  - [ ] All credentials removed from configuration files
  - [ ] SSL/TLS certificates prepared
  - [ ] Firewall rules planned
  - [ ] Access control list (ACL) requirements identified
  - [ ] Encryption keys generated and secured

- [ ] **Capacity Planning**
  - [ ] Expected user count determined
  - [ ] Storage requirements estimated
  - [ ] Network bandwidth provisioned
  - [ ] CPU and memory allocation planned
  - [ ] Backup storage configured

- [ ] **Backup Procedures**
  - [ ] Current state backed up
  - [ ] Rollback plan documented
  - [ ] Recovery tested
  - [ ] Backup location verified accessible
  - [ ] Backup retention policy established

### Deployment Phase

- [ ] **Pre-Flight Checks**
  - [ ] All prerequisites verified
  - [ ] Configuration files validated
  - [ ] Database migrations tested
  - [ ] Network connectivity confirmed
  - [ ] DNS resolution working

- [ ] **Installation**
  - [ ] Download latest release package
  - [ ] Verify package integrity (SHA-256 hash)
  - [ ] Run as administrator
  - [ ] Accept license agreement
  - [ ] Choose appropriate configuration tier

- [ ] **Configuration**
  - [ ] Admin credentials configured securely
  - [ ] Cloud providers configured (if needed)
  - [ ] SSL/TLS certificates installed
  - [ ] Database connections configured
  - [ ] Service accounts created with appropriate permissions

- [ ] **Service Startup**
  - [ ] HELIOS service starts without errors
  - [ ] All subsystems initialized successfully
  - [ ] Event logs show no critical errors
  - [ ] Dashboard accessible and responsive
  - [ ] Health checks pass (see Monitoring section)

### Post-Deployment

- [ ] **Verification**
  - [ ] Dashboard accessible at configured URL
  - [ ] All core features operational
  - [ ] Cloud sync tested
  - [ ] Plugins loading correctly
  - [ ] Performance metrics within acceptable range

- [ ] **Security Hardening**
  - [ ] Change default administrator password
  - [ ] Configure HTTPS only
  - [ ] Restrict dashboard network access
  - [ ] Enable audit logging
  - [ ] Configure backup encryption

- [ ] **Monitoring Setup**
  - [ ] Performance monitoring enabled
  - [ ] Log aggregation configured
  - [ ] Alerts configured
  - [ ] Health checks scheduled
  - [ ] Backup verification scheduled

- [ ] **Documentation**
  - [ ] Deployment documented
  - [ ] Configuration recorded
  - [ ] Access procedures documented
  - [ ] Escalation procedures defined
  - [ ] Support contact information shared

---

## Configuration Guide

### Core Configuration (appsettings.json)

```json
{
  "helios": {
    "environment": "Production",
    "version": "3.6.0",
    "logLevel": "Warning",
    "enableDiagnostics": false
  },
  "server": {
    "dashboardPort": 8080,
    "enableHttps": true,
    "certificatePath": "C:\\certs\\helios.pfx",
    "certificatePassword": "${CERT_PASSWORD}",
    "bindAddress": "0.0.0.0",
    "requestTimeout": 30000
  },
  "database": {
    "provider": "SqlServer",
    "connectionString": "Server=.;Database=HELIOS;Integrated Security=true;",
    "commandTimeout": 30,
    "connectionPoolSize": 20,
    "enableRetry": true,
    "maxRetryCount": 3
  },
  "cloudSync": {
    "enabled": true,
    "maxConcurrentSyncs": 3,
    "batchSize": 100,
    "timeout": 300000
  },
  "plugins": {
    "enabled": true,
    "pluginDirectory": "C:\\ProgramData\\HELIOS\\plugins",
    "autoLoadPlugins": true,
    "isolationLevel": "AppDomain"
  },
  "ml": {
    "enabled": true,
    "modelCachePath": "C:\\ProgramData\\HELIOS\\ml-cache",
    "maxCacheSize": "10GB",
    "gpuAcceleration": true
  },
  "security": {
    "encryptionAlgorithm": "AES-256",
    "passwordHashAlgorithm": "PBKDF2",
    "tlsVersion": "1.3",
    "requireMfa": false
  },
  "telemetry": {
    "enabled": true,
    "endpoint": "https://telemetry.helios-platform.io",
    "batchSize": 100,
    "flushInterval": 30000
  }
}
```

### Environment Variables

```powershell
# Set environment-specific configuration
$env:HELIOS_ENVIRONMENT = "Production"
$env:HELIOS_ADMIN_PASSWORD = "your-secure-password"
$env:DATABASE_CONNECTION_STRING = "Server=sqlserver.example.com;..."
$env:CERT_PASSWORD = "your-certificate-password"
$env:CLOUD_SYNC_ENABLED = "true"

# Make persistent (Windows)
[Environment]::SetEnvironmentVariable("HELIOS_ENVIRONMENT", "Production", "Machine")
```

### Service Account Permissions

```powershell
# Create dedicated service account
$password = ConvertTo-SecureString "ComplexPassword123!" -AsPlainText -Force
New-LocalUser -Name "HELIOS-SVC" -Password $password -Description "HELIOS Platform Service Account"

# Add to Administrators group for local operations
Add-LocalGroupMember -Group "Administrators" -Member "HELIOS-SVC"

# Grant folder permissions
$acl = Get-Acl "C:\ProgramData\HELIOS"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "HELIOS-SVC",
    "FullControl",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl "C:\ProgramData\HELIOS" $acl
```

---

## Monitoring & Health Checks

### Health Check Endpoints

```
GET /api/health/status
Response:
{
  "status": "Healthy",
  "version": "3.6.0",
  "timestamp": "2026-05-15T14:32:00Z",
  "uptime": "45.12:34:56",
  "components": {
    "database": "Healthy",
    "cloudSync": "Healthy",
    "pluginSystem": "Healthy",
    "mlService": "Healthy"
  }
}
```

### Performance Metrics Collection

```csharp
using HELIOS.Monitoring;

var monitoring = new HealthMonitoring();

// Enable metrics collection
await monitoring.StartAsync();

// Get current metrics
var metrics = await monitoring.GetMetricsAsync();
Console.WriteLine($"CPU Usage: {metrics.CpuUsage:P}");
Console.WriteLine($"Memory Usage: {metrics.MemoryUsage:P}");
Console.WriteLine($"Request Latency (p95): {metrics.RequestLatencyP95}ms");

// Get alerts
var alerts = await monitoring.GetActiveAlertsAsync();
foreach (var alert in alerts)
{
    Console.WriteLine($"Alert: {alert.Name} - {alert.Severity}");
}
```

### Alert Configuration

```json
{
  "monitoring": {
    "alerts": [
      {
        "name": "HighCPUUsage",
        "metric": "cpu_usage",
        "threshold": 85,
        "duration": 300000,
        "action": "notify",
        "recipients": ["admin@example.com"]
      },
      {
        "name": "LowDiskSpace",
        "metric": "disk_free_percent",
        "threshold": 10,
        "duration": 0,
        "action": "alert",
        "severity": "Critical"
      },
      {
        "name": "SyncFailures",
        "metric": "sync_failure_count",
        "threshold": 3,
        "duration": 600000,
        "action": "escalate"
      }
    ]
  }
}
```

### Log Aggregation

```powershell
# Configure log forwarding to ELK stack
$logConfig = @{
    "logging" = @{
        "logLevel" = "Information"
        "console" = @{
            "enabled" = $true
            "colorOutput" = $false
        }
        "file" = @{
            "enabled" = $true
            "path" = "C:\ProgramData\HELIOS\logs\helios-{date}.log"
            "maxFileSizeBytes" = 104857600  # 100 MB
            "maxRollingFiles" = 10
        }
        "elasticsearch" = @{
            "enabled" = $true
            "nodes" = @("https://elasticsearch.example.com:9200")
            "indexPattern" = "helios-{date}"
            "batchSize" = 100
        }
    }
}
```

---

## Updating to v3.6.0

### Pre-Update Backup

```powershell
# Backup current installation
$backupPath = "C:\Backups\HELIOS-$(Get-Date -Format yyyy-MM-dd-HHmmss)"
New-Item -ItemType Directory -Path $backupPath | Out-Null

# Copy application files
Copy-Item -Path "C:\Program Files\HELIOS" -Destination "$backupPath\Application" -Recurse

# Backup database
$dbBackupPath = "$backupPath\Database\HELIOS-$(Get-Date -Format yyyy-MM-dd-HHmmss).bak"
New-Item -ItemType Directory -Path (Split-Path $dbBackupPath) -Force | Out-Null
Backup-SqlDatabase -ServerInstance "." -Database "HELIOS" -BackupFile $dbBackupPath

# Backup configuration
Copy-Item -Path "C:\ProgramData\HELIOS\config" -Destination "$backupPath\Config" -Recurse
```

### Update Process

```powershell
# 1. Stop HELIOS services
Stop-Service -Name "HELIOS*" -Force
Start-Sleep -Seconds 5

# 2. Verify services stopped
Get-Service -Name "HELIOS*" | Select-Object Name, Status

# 3. Download update
$updateUrl = "https://github.com/M0nado/helios-platform/releases/download/v3.6.0/HELIOS-Platform-v3.6.0.zip"
Invoke-WebRequest -Uri $updateUrl -OutFile "$env:TEMP\helios-update.zip"

# 4. Extract update
Expand-Archive -Path "$env:TEMP\helios-update.zip" -DestinationPath "$env:TEMP\helios-update" -Force

# 5. Run update script
& "$env:TEMP\helios-update\update.ps1" -InstallPath "C:\Program Files\HELIOS"

# 6. Start services
Start-Service -Name "HELIOS.Core"
Start-Service -Name "HELIOS.Dashboard"
Start-Service -Name "HELIOS.CloudSync"

# 7. Verify health
Start-Sleep -Seconds 10
Invoke-WebRequest -Uri "https://localhost:8080/api/health/status" -SkipCertificateCheck
```

### Database Migration

```powershell
# Run database migrations
& "C:\Program Files\HELIOS\bin\helios-db-migrate.exe" `
    -ConnectionString "Server=.;Database=HELIOS;Integrated Security=true;" `
    -Environment "Production" `
    -Verbose

# Verify migrations applied
Get-Content "C:\ProgramData\HELIOS\logs\migration-$(Get-Date -Format yyyy-MM-dd).log" | Tail -20
```

### Post-Update Verification

```powershell
# Check version
& "C:\Program Files\HELIOS\bin\helios.exe" --version

# Verify all services healthy
$health = Invoke-WebRequest -Uri "https://localhost:8080/api/health/status" -SkipCertificateCheck
$healthData = $health.Content | ConvertFrom-Json
$healthData.components

# Run smoke tests
& "C:\Program Files\HELIOS\tests\smoke-tests.ps1"
```

---

## Rollback Procedures

### Rollback Decision Criteria

Rollback if any of the following occur:

- Critical services fail to start
- Database migration fails
- Performance degradation >50%
- Data loss or corruption detected
- Security vulnerabilities introduced

### Rollback Steps

```powershell
# 1. Stop current version
Stop-Service -Name "HELIOS*" -Force

# 2. Restore from backup
$backupDate = "2026-05-14-143200"  # Before update
$backupPath = "C:\Backups\HELIOS-$backupDate"

# 3. Restore application files
Remove-Item -Path "C:\Program Files\HELIOS\*" -Recurse -Force
Copy-Item -Path "$backupPath\Application\*" -Destination "C:\Program Files\HELIOS\" -Recurse

# 4. Restore database
Restore-SqlDatabase `
    -ServerInstance "." `
    -Database "HELIOS" `
    -BackupFile "$backupPath\Database\*.bak" `
    -ReplaceDatabase

# 5. Restore configuration
Remove-Item -Path "C:\ProgramData\HELIOS\config\*" -Recurse -Force
Copy-Item -Path "$backupPath\Config\*" -Destination "C:\ProgramData\HELIOS\config\" -Recurse

# 6. Start services
Start-Service -Name "HELIOS.Core"
Start-Service -Name "HELIOS.Dashboard"
Start-Service -Name "HELIOS.CloudSync"

# 7. Verify health
Start-Sleep -Seconds 10
$health = Invoke-WebRequest -Uri "https://localhost:8080/api/health/status" -SkipCertificateCheck
$health.Content | ConvertFrom-Json
```

---

## Performance Tuning

### CPU Optimization

```json
{
  "performance": {
    "cpu": {
      "threadPoolMinThreads": 20,
      "threadPoolMaxThreads": 100,
      "taskScheduler": "ThreadPool",
      "parallelProcessing": {
        "enabled": true,
        "maxDegreeOfParallelism": -1
      }
    }
  }
}
```

### Memory Optimization

```json
{
  "performance": {
    "memory": {
      "cachingStrategy": "Distributed",
      "maxCacheSize": "2GB",
      "gcMode": "Workstation",
      "heapCount": "Auto"
    }
  }
}
```

### Disk I/O Optimization

```json
{
  "performance": {
    "disk": {
      "compression": "Enabled",
      "readAheadSize": 65536,
      "batchWriteSize": 1000,
      "fsyncInterval": 10000
    }
  }
}
```

### Network Optimization

```json
{
  "performance": {
    "network": {
      "connectionPooling": true,
      "maxConnections": 100,
      "keepAliveInterval": 60000,
      "bufferSize": 65536,
      "compression": "gzip"
    }
  }
}
```

### Monitoring Tuning Impact

```powershell
# Baseline before tuning
$beforeBaseline = Invoke-WebRequest -Uri "https://localhost:8080/api/metrics/baseline" -SkipCertificateCheck
$beforeData = $beforeBaseline.Content | ConvertFrom-Json

# Apply tuning configuration

# Measure after tuning
$afterBaseline = Invoke-WebRequest -Uri "https://localhost:8080/api/metrics/baseline" -SkipCertificateCheck
$afterData = $afterBaseline.Content | ConvertFrom-Json

# Compare results
Write-Host "CPU Usage: $($beforeData.cpuUsage)% → $($afterData.cpuUsage)%"
Write-Host "Memory Usage: $($beforeData.memoryUsage)% → $($afterData.memoryUsage)%"
Write-Host "Request Latency: $($beforeData.requestLatency)ms → $($afterData.requestLatency)ms"
```

---

## Summary

HELIOS Platform v3.6.0 deployment follows enterprise-grade practices with comprehensive checklists, robust monitoring, and safe update/rollback procedures. Use this guide to ensure reliable deployments in production environments.

For support, contact [support.helios-platform.io](https://support.helios-platform.io) or visit [GitHub Discussions](https://discussions.github.com/M0nado/helios-platform).
