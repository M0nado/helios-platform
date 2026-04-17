# Troubleshooting Tests - HELIOS Platform v2

## Overview

Diagnostic and troubleshooting tests to identify and resolve issues with HELIOS Platform v2.

## System Health Check

### Quick Health Assessment

```powershell
function Test-SystemHealth {
    Write-Host "Running system health check..."
    
    $Health = @{}
    
    # 1. Check HELIOS registry
    $Health.HeliosRegistry = Test-Path "HKLM:\Software\HELIOS"
    Write-Host "HELIOS Registry: $(if ($Health.HeliosRegistry) { '✓' } else { '✗' })"
    
    # 2. Check HELIOS services
    $Services = Get-Service -Name "HELIOS-*" -ErrorAction SilentlyContinue
    $Health.ServicesRunning = ($Services | Where-Object Status -eq "Running" | Measure-Object).Count
    Write-Host "Running Services: $($Health.ServicesRunning)"
    
    # 3. Check critical files
    $Health.FilesPresent = @(
        Test-Path "C:\HELIOS",
        Test-Path "C:\HELIOS\config.json",
        Test-Path "C:\HELIOS\logs"
    ) -contains $true
    Write-Host "Critical Files: $(if ($Health.FilesPresent) { '✓' } else { '✗' })"
    
    # 4. Check disk space
    $Volume = Get-Volume -ErrorAction SilentlyContinue | Select-Object -First 1
    $FreeSpace = $Volume.SizeRemaining / 1GB
    $Health.DiskSpace = $FreeSpace -gt 5  # At least 5GB
    Write-Host "Free Disk Space: $($FreeSpace.ToString('F1'))GB $(if ($Health.DiskSpace) { '✓' } else { '✗' })"
    
    # 5. Check permissions
    try {
        $TestFile = "C:\HELIOS\permission-test.txt"
        "test" | Set-Content $TestFile -ErrorAction Stop
        Remove-Item $TestFile -Force
        $Health.Permissions = $true
    }
    catch {
        $Health.Permissions = $false
    }
    Write-Host "File Permissions: $(if ($Health.Permissions) { '✓' } else { '✗' })"
    
    Write-Host ""
    
    $HealthScore = ($Health.Values | Where-Object { $_ -is [bool] } | Where-Object).Count
    Write-Host "Health Score: $HealthScore/5"
    
    return $Health
}
```

### Detailed Health Report

```powershell
function Get-DetailedHealthReport {
    Write-Host "═══════════════════════════════════════════"
    Write-Host "HELIOS Platform - Detailed Health Report"
    Write-Host "═══════════════════════════════════════════"
    Write-Host ""
    
    # System Information
    Write-Host "System Information"
    Write-Host "──────────────────"
    $OS = Get-WmiObject -Class Win32_OperatingSystem
    Write-Host "Hostname:      $env:COMPUTERNAME"
    Write-Host "OS:            $($OS.Caption)"
    Write-Host "Version:       $($OS.Version)"
    Write-Host "Arch:          $([Environment]::Is64BitOperatingSystem ? '64-bit' : '32-bit')"
    Write-Host ""
    
    # HELIOS Status
    Write-Host "HELIOS Status"
    Write-Host "─────────────"
    Write-Host "Registry:      $(if (Test-Path 'HKLM:\Software\HELIOS') { '✓ Present' } else { '✗ Missing' })"
    Write-Host "Data Dir:      $(if (Test-Path 'C:\HELIOS') { '✓ Present' } else { '✗ Missing' })"
    Write-Host ""
    
    # Services
    Write-Host "Services"
    Write-Host "────────"
    Get-Service -Name "HELIOS-*" -ErrorAction SilentlyContinue | 
        ForEach-Object {
            $Status = $_.Status -eq "Running" ? "✓" : "✗"
            Write-Host "$Status $($_.DisplayName): $($_.Status) ($($_.StartType))"
        }
    Write-Host ""
    
    # Recent Errors
    Write-Host "Recent Errors (Last 24 Hours)"
    Write-Host "───────────────────────────────"
    $Errors = Get-EventLog -LogName System -EntryType Error -After (Get-Date).AddDays(-1) -ErrorAction SilentlyContinue | 
        Select-Object -First 10
    
    if ($Errors) {
        $Errors | ForEach-Object {
            Write-Host "  - [$($_.TimeGenerated.ToString('MM-dd HH:mm'))] $($_.Source): $($_.Message.Substring(0, 50))..."
        }
    }
    else {
        Write-Host "  No errors found ✓"
    }
    Write-Host ""
}
```

## Security Verification

### Security Check

```powershell
function Test-SecurityPosture {
    Write-Host "Running security verification..."
    
    $Security = @{}
    
    # 1. Windows Defender
    Write-Host "  - Checking Windows Defender..."
    try {
        $Defender = Get-MpComputerStatus
        $Security.DefenderEnabled = -not $Defender.DisableRealtimeMonitoring
        $Security.DefenderSignatureAge = ((Get-Date) - $Defender.AntivirusSignatureLastUpdated).Days
    }
    catch {
        $Security.DefenderEnabled = $false
    }
    Write-Host "    Defender: $(if ($Security.DefenderEnabled) { '✓ Enabled' } else { '✗ Disabled' })"
    
    # 2. Firewall
    Write-Host "  - Checking Windows Firewall..."
    $Firewall = Get-NetFirewallProfile
    $Security.FirewallEnabled = $Firewall | Where-Object { $_.Enabled -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
    Write-Host "    Firewall: $(if ($Security.FirewallEnabled -eq 0) { '✓ Enabled' } else { '✗ Partially Disabled' })"
    
    # 3. UAC
    Write-Host "  - Checking User Access Control..."
    $UAC = Get-ItemProperty -Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Policies\System" -Name "EnableLUA" -ErrorAction SilentlyContinue
    $Security.UACEnabled = $UAC.EnableLUA -eq 1
    Write-Host "    UAC: $(if ($Security.UACEnabled) { '✓ Enabled' } else { '✗ Disabled' })"
    
    Write-Host ""
    
    return $Security
}
```

## Performance Diagnosis

### Performance Check

```powershell
function Get-PerformanceDiagnosis {
    Write-Host "Running performance diagnosis..."
    Write-Host ""
    
    # Boot Time
    Write-Host "Boot Time"
    Write-Host "─────────"
    $LastBoot = Get-WmiObject -Class Win32_OperatingSystem | Select-Object -ExpandProperty LastBootUpTime
    $BootTime = ([Management.ManagementDateTimeConverter]::ToDateTime($LastBoot))
    $Uptime = (Get-Date) - $BootTime
    Write-Host "Uptime:     $($Uptime.Days)d $($Uptime.Hours)h $($Uptime.Minutes)m"
    Write-Host ""
    
    # Memory
    Write-Host "Memory Usage"
    Write-Host "────────────"
    $MemInfo = Get-WmiObject -Class Win32_OperatingSystem
    $UsedMemory = $MemInfo.TotalVisibleMemorySize - $MemInfo.FreePhysicalMemory
    $UsedPercent = ($UsedMemory / $MemInfo.TotalVisibleMemorySize) * 100
    Write-Host "Used:       $(($UsedMemory / 1024 / 1024).ToString('F1')) GB ($(([int]$UsedPercent))%)"
    Write-Host "Available:  $(($MemInfo.FreePhysicalMemory / 1024 / 1024).ToString('F1')) GB"
    Write-Host ""
    
    # CPU
    Write-Host "CPU Usage"
    Write-Host "─────────"
    $CPU = Get-WmiObject -Class Win32_PerfFormattedData_PerfOS_Processor | 
        Where-Object { $_.Name -eq "_Total" } | 
        Select-Object -ExpandProperty PercentProcessorTime
    Write-Host "Current:    $CPU%"
    Write-Host ""
    
    # Disk
    Write-Host "Disk Usage"
    Write-Host "──────────"
    Get-WmiObject -Class Win32_LogicalDisk -Filter "DriveType = 3" | 
        ForEach-Object {
            $UsedPercent = ($_.Size - $_.FreeSpace) / $_.Size * 100
            Write-Host "$($_.Name): $(([int]$UsedPercent))% used ($(($_.FreeSpace / 1GB).ToString('F1')))GB free"
        }
    Write-Host ""
}
```

## Dependency Validation

### Check Dependencies

```powershell
function Test-Dependencies {
    Write-Host "Validating dependencies..."
    Write-Host ""
    
    $Dependencies = @()
    
    # PowerShell Version
    Write-Host "PowerShell Version"
    $PSVersion = $PSVersionTable.PSVersion.Major
    $PSCheck = $PSVersion -ge 5
    Write-Host "  Version: $($PSVersionTable.PSVersion) $(if ($PSCheck) { '✓' } else { '✗ (Requires 5+)' })"
    $Dependencies += @{ Name = "PowerShell"; Pass = $PSCheck }
    Write-Host ""
    
    # Windows Version
    Write-Host "Windows Version"
    $OSVersion = [int]((Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion").CurrentVersion)
    $OSCheck = $OSVersion -ge 10
    Write-Host "  Version: $OSVersion $(if ($OSCheck) { '✓' } else { '✗ (Requires 10+)' })"
    $Dependencies += @{ Name = "Windows"; Pass = $OSCheck }
    Write-Host ""
    
    # .NET Framework
    Write-Host ".NET Framework"
    $NetVersion = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue).Version
    $NetCheck = $NetVersion -ge "4.5.0.0"
    Write-Host "  Version: $NetVersion $(if ($NetCheck) { '✓' } else { '✗' })"
    $Dependencies += @{ Name = ".NET"; Pass = $NetCheck }
    Write-Host ""
    
    # Required Modules
    Write-Host "Required Modules"
    $Modules = @("Pester", "WindowsUpdate")
    foreach ($Module in $Modules) {
        $Installed = Get-Module -Name $Module -ListAvailable -ErrorAction SilentlyContinue
        $Status = if ($Installed) { "✓" } else { "✗" }
        Write-Host "  $Status $Module"
        $Dependencies += @{ Name = $Module; Pass = $Installed -ne $null }
    }
    Write-Host ""
    
    $PassCount = ($Dependencies | Where-Object Pass).Count
    Write-Host "Dependencies: $PassCount/$($Dependencies.Count) satisfied"
    
    return $Dependencies
}
```

## Common Issues and Fixes

### Issue: Phase Fails with Registry Error

**Symptom:** Registry modification fails

```powershell
# Diagnostic
Write-Host "Checking registry access..."
Test-RegistryAccess -Path "HKLM:\Software\HELIOS"

# Fix
# 1. Run as Administrator
# 2. Check registry permissions
# 3. Verify disk space available
# 4. Disable antivirus temporarily
```

### Issue: Service Won't Start

**Symptom:** HELIOS service fails to start

```powershell
# Diagnostic
Write-Host "Checking service logs..."
Get-EventLog -LogName System -Source "HELIOS" -Newest 20

# Fix
# 1. Check service dependencies
# 2. Verify file permissions
# 3. Check for port conflicts
# 4. Review error logs
```

### Issue: Performance Not Improved

**Symptom:** Expected improvements not seen

```powershell
# Diagnostic
Write-Host "Comparing metrics..."
Compare-Metrics -Baseline $BaselineMetrics -Current (Measure-CurrentMetrics)

# Fix
# 1. Verify phase execution completed
# 2. Check all settings were applied
# 3. Measure over longer period
# 4. Verify no conflicting software
```

## Troubleshooting Workflow

```
Problem Detected
        ↓
Run Health Check
        ↓
Are basics OK? ──NO──> Fix basic issues
        ↓ YES
Run Detailed Check
        ↓
Identify root cause
        ↓
Apply fix
        ↓
Verify solution
        ↓
Document solution
```

## Log Collection

```powershell
function Collect-DiagnosticLogs {
    param([string]$OutputPath = "C:\HELIOS\diagnostics")
    
    New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
    
    Write-Host "Collecting diagnostic logs..."
    
    # System info
    Get-ComputerInfo | Export-Clixml "$OutputPath\system-info.xml"
    
    # Event logs
    Get-EventLog -LogName System -Newest 1000 | Export-Clixml "$OutputPath\system-events.xml"
    
    # HELIOS registry
    if (Test-Path "HKLM:\Software\HELIOS") {
        reg export "HKLM\Software\HELIOS" "$OutputPath\helios-registry.reg"
    }
    
    # Health check
    Test-SystemHealth | ConvertTo-Json | Set-Content "$OutputPath\health-check.json"
    
    Write-Host "✓ Diagnostics collected: $OutputPath"
}
```

---

**Version:** 2.0  
**Last Updated:** 2024  
**Maintained By:** HELIOS Development Team
