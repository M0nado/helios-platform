<#
.SYNOPSIS
Conflict Detection for HELIOS Platform Installation

.DESCRIPTION
Detects potential conflicts before installation:
- Port conflicts (5000-6000 range)
- Service name conflicts
- Registry key conflicts
- File system conflicts
- Process conflicts

.PARAMETER PortRangeStart
Start of port range to check (default: 5000)

.PARAMETER PortRangeEnd
End of port range to check (default: 6010)

.PARAMETER OutputReport
Path to save conflict report

.EXAMPLE
.\conflict-detection.ps1

.EXAMPLE
.\conflict-detection.ps1 -PortRangeStart 5000 -PortRangeEnd 6000 -Verbose

.NOTES
Must run as Administrator
Exit codes:
  0 = No conflicts
  1 = Conflicts detected (may block installation)
  2 = Warnings (conflicts detected but non-critical)
#>

[CmdletBinding()]
param(
    [int]$PortRangeStart = 5000,
    [int]$PortRangeEnd = 6010,
    [string]$OutputReport = "$PSScriptRoot\conflict-detection-report.txt"
)

$ErrorActionPreference = 'Continue'

# ===========================
# GLOBALS
# ===========================

$conflicts = @()
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$hasCriticalConflicts = $false

# ===========================
# HELPER FUNCTIONS
# ===========================

function Add-ConflictResult {
    [CmdletBinding()]
    param(
        [string]$Type,
        [string]$Resource,
        [ValidateSet("CRITICAL", "WARNING", "INFO")]
        [string]$Severity,
        [string]$Description,
        [string]$Resolution = ""
    )

    $conflict = @{
        Timestamp = $timestamp
        Type = $Type
        Resource = $Resource
        Severity = $Severity
        Description = $Description
        Resolution = $Resolution
    }

    $conflicts += $conflict

    $color = switch ($Severity) {
        "CRITICAL" { "Red" }
        "WARNING" { "Yellow" }
        "INFO" { "Cyan" }
    }

    Write-Host "[$Severity] $Type - $Resource" -ForegroundColor $color
    Write-Host "  └─ $Description" -ForegroundColor Gray
    if ($Resolution) { Write-Host "     Resolution: $Resolution" -ForegroundColor DarkYellow }

    if ($Severity -eq "CRITICAL") { $script:hasCriticalConflicts = $true }
}

# ===========================
# CONFLICT 1: PORT CONFLICTS
# ===========================

Write-Host "`n[SCAN 1/5] Port Availability ($PortRangeStart-$PortRangeEnd)" -ForegroundColor Cyan

$usedPorts = @()
try {
    $tcpConnections = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue
    foreach ($conn in $tcpConnections) {
        if ($conn.LocalPort -ge $PortRangeStart -and $conn.LocalPort -le $PortRangeEnd) {
            $process = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
            $usedPorts += @{
                Port = $conn.LocalPort
                Process = $process.Name
                ProcessId = $process.Id
            }
        }
    }

    if ($usedPorts.Count -eq 0) {
        Write-Host "✓ All ports in range available" -ForegroundColor Green
    } else {
        foreach ($portInfo in $usedPorts) {
            Add-ConflictResult -Type "Port" -Resource $portInfo.Port `
                -Severity "WARNING" `
                -Description "Port $($portInfo.Port) is in use by $($portInfo.Process) (PID: $($portInfo.ProcessId))" `
                -Resolution "Change HELIOS port configuration or stop conflicting process"
        }
    }
} catch {
    Add-ConflictResult -Type "Port" -Resource "All" `
        -Severity "WARNING" `
        -Description "Could not scan port availability: $_" `
        -Resolution "Verify manually using 'netstat -ano' command"
}

# ===========================
# CONFLICT 2: SERVICE CONFLICTS
# ===========================

Write-Host "`n[SCAN 2/5] Service Name Conflicts" -ForegroundColor Cyan

$servicesToCheck = @(
    "HELIOSService",
    "HELIOSPlatform",
    "HELIOS",
    "HeliosAgent"
)

$conflicts_found = $false
foreach ($serviceName in $servicesToCheck) {
    try {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($service) {
            Add-ConflictResult -Type "Service" -Resource $serviceName `
                -Severity "CRITICAL" `
                -Description "Service '$serviceName' already exists" `
                -Resolution "Uninstall existing HELIOS or use different service name"
            $conflicts_found = $true
        }
    } catch {
        # Service doesn't exist - that's fine
    }
}

if (-not $conflicts_found) {
    Write-Host "✓ No conflicting services found" -ForegroundColor Green
}

# ===========================
# CONFLICT 3: REGISTRY CONFLICTS
# ===========================

Write-Host "`n[SCAN 3/5] Registry Key Conflicts" -ForegroundColor Cyan

$registryKeysToCheck = @(
    "HKLM:\SOFTWARE\HELIOS",
    "HKLM:\SOFTWARE\WOW6432Node\HELIOS",
    "HKCU:\SOFTWARE\HELIOS"
)

$reg_conflicts_found = $false
foreach ($regKey in $registryKeysToCheck) {
    try {
        if (Test-Path $regKey) {
            Add-ConflictResult -Type "Registry" -Resource $regKey `
                -Severity "WARNING" `
                -Description "Registry key already exists" `
                -Resolution "Previous installation detected. Run uninstaller first or proceed with upgrade"
            $reg_conflicts_found = $true
        }
    } catch {
        # Key doesn't exist - that's fine
    }
}

if (-not $reg_conflicts_found) {
    Write-Host "✓ No conflicting registry keys found" -ForegroundColor Green
}

# ===========================
# CONFLICT 4: FILE CONFLICTS
# ===========================

Write-Host "`n[SCAN 4/5] File System Conflicts" -ForegroundColor Cyan

$installPath = "C:\Program Files\HELIOS"
$dataPath = "C:\ProgramData\HELIOS"

$file_conflicts_found = $false
if (Test-Path $installPath) {
    $fileCount = (Get-ChildItem -Path $installPath -Recurse -ErrorAction SilentlyContinue | Measure-Object).Count
    Add-ConflictResult -Type "FileSystem" -Resource $installPath `
        -Severity "WARNING" `
        -Description "Installation directory already exists with $fileCount items" `
        -Resolution "Directory will be overwritten during upgrade or use custom installation path"
    $file_conflicts_found = $true
}

if (Test-Path $dataPath) {
    $dataFileCount = (Get-ChildItem -Path $dataPath -Recurse -ErrorAction SilentlyContinue | Measure-Object).Count
    Add-ConflictResult -Type "FileSystem" -Resource $dataPath `
        -Severity "INFO" `
        -Description "Data directory exists with $dataFileCount items" `
        -Resolution "User data will be preserved"
    $file_conflicts_found = $true
}

if (-not $file_conflicts_found) {
    Write-Host "✓ No conflicting file system directories found" -ForegroundColor Green
}

# ===========================
# CONFLICT 5: PROCESS CONFLICTS
# ===========================

Write-Host "`n[SCAN 5/5] Process Conflicts" -ForegroundColor Cyan

$processesToCheck = @(
    "HELIOSPlatform",
    "HELIOSService",
    "HELIOSAgent"
)

$proc_conflicts_found = $false
foreach ($procName in $processesToCheck) {
    try {
        $processes = Get-Process -Name $procName -ErrorAction SilentlyContinue
        if ($processes) {
            Add-ConflictResult -Type "Process" -Resource $procName `
                -Severity "WARNING" `
                -Description "Process '$procName' is currently running" `
                -Resolution "Close the application or use 'Stop-Process -Name $procName' before installation"
            $proc_conflicts_found = $true
        }
    } catch {
        # Process not running - that's fine
    }
}

if (-not $proc_conflicts_found) {
    Write-Host "✓ No conflicting processes found" -ForegroundColor Green
}

# ===========================
# GENERATE REPORT
# ===========================

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "CONFLICT DETECTION REPORT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Timestamp: $timestamp" -ForegroundColor Gray
Write-Host ""

if ($conflicts.Count -eq 0) {
    Write-Host "✓ NO CONFLICTS DETECTED" -ForegroundColor Green
    Write-Host "System is clear for HELIOS installation." -ForegroundColor Green
} else {
    $criticalCount = ($conflicts | Where-Object { $_.Severity -eq "CRITICAL" }).Count
    $warningCount = ($conflicts | Where-Object { $_.Severity -eq "WARNING" }).Count
    $infoCount = ($conflicts | Where-Object { $_.Severity -eq "INFO" }).Count

    Write-Host "CONFLICT SUMMARY:" -ForegroundColor Cyan
    Write-Host "  🔴 Critical: $criticalCount" -ForegroundColor Red
    Write-Host "  🟡 Warnings: $warningCount" -ForegroundColor Yellow
    Write-Host "  ℹ  Info: $infoCount" -ForegroundColor Cyan
    Write-Host ""

    if ($hasCriticalConflicts) {
        Write-Host "⚠️  INSTALLATION BLOCKED" -ForegroundColor Red
        Write-Host "Critical conflicts detected. Please resolve before installation." -ForegroundColor Red
    } else {
        Write-Host "✓ Installation CAN PROCEED" -ForegroundColor Yellow
        Write-Host "Non-critical conflicts detected. Review recommendations." -ForegroundColor Yellow
    }
}

Write-Host ""

# Save detailed report
$reportContent = @"
HELIOS PLATFORM 2.0 - CONFLICT DETECTION REPORT
================================================

Generated: $timestamp
Computer: $env:COMPUTERNAME
User: $env:USERNAME

PORT SCAN RANGE
---------------
Start: $PortRangeStart
End: $PortRangeEnd

DETECTED CONFLICTS
------------------
"@

if ($conflicts.Count -eq 0) {
    $reportContent += "`nNO CONFLICTS DETECTED`n"
} else {
    foreach ($conflict in $conflicts) {
        $reportContent += "`n[$($conflict.Severity)]  $($conflict.Type) - $($conflict.Resource)`n"
        $reportContent += "  Description: $($conflict.Description)`n"
        if ($conflict.Resolution) {
            $reportContent += "  Resolution: $($conflict.Resolution)`n"
        }
    }
}

$reportContent += "`n`nOVERALL RESULT`n--------------`n"
if ($hasCriticalConflicts) {
    $reportContent += "INSTALLATION BLOCKED - Critical conflicts detected`n"
} elseif ($conflicts.Count -gt 0) {
    $reportContent += "WARNINGS - Non-critical conflicts detected`n"
} else {
    $reportContent += "APPROVED - No conflicts detected`n"
}

$reportContent | Out-File -FilePath $OutputReport -Encoding UTF8 -Force
Write-Host "Report saved to: $OutputReport" -ForegroundColor Gray

# Exit with appropriate code
if ($hasCriticalConflicts) {
    exit 1
} elseif ($conflicts.Count -gt 0) {
    exit 2
} else {
    exit 0
}
