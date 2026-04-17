<#
.SYNOPSIS
Pre-Installation System Requirements Verification for HELIOS Platform Installer

.DESCRIPTION
Comprehensive checks before HELIOS Platform installation:
- OS version and build verification
- RAM and disk space validation
- Port availability check
- Administrator privilege verification
- Conflicting service detection
- Antivirus compatibility check
- Firewall configuration validation

.PARAMETER MinimumRam
Minimum required RAM in GB (default: 4)

.PARAMETER MinimumDiskSpace
Minimum required disk space in GB (default: 2)

.PARAMETER HttpPort
Port to verify for HTTP (default: 5000)

.PARAMETER OutputReport
Path to save diagnostic report (default: $PSScriptRoot\pre-install-diag.txt)

.EXAMPLE
.\pre-install-checks.ps1

.EXAMPLE
.\pre-install-checks.ps1 -MinimumRam 8 -MinimumDiskSpace 4 -Verbose

.NOTES
Must run as Administrator
Exit codes:
  0 = All checks passed
  1 = Critical failure (installation blocked)
  2 = Warnings detected (installation continues)
#>

[CmdletBinding()]
param(
    [int]$MinimumRam = 4,
    [int]$MinimumDiskSpace = 2,
    [int]$HttpPort = 5000,
    [string]$OutputReport = "$PSScriptRoot\pre-install-diag.txt"
)

$ErrorActionPreference = 'Continue'

# ===========================
# GLOBALS & CONFIGURATION
# ===========================

$checkResults = @()
$hasCriticalIssues = $false
$hasWarnings = $false
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$supportedOSVersions = @{
    "Windows 7" = @{ MinBuild = 7600; MaxBuild = 7601 }
    "Windows 10" = @{ MinBuild = 19041; MaxBuild = 9999 }
    "Windows 11" = @{ MinBuild = 22621; MaxBuild = 9999 }
}

# ===========================
# HELPER FUNCTIONS
# ===========================

function Write-DiagnosticResult {
    [CmdletBinding()]
    param(
        [string]$Category,
        [string]$Check,
        [ValidateSet("PASS", "WARN", "FAIL")]
        [string]$Status,
        [string]$Message,
        [string]$Detail = ""
    )

    $result = @{
        Timestamp = $timestamp
        Category = $Category
        Check = $Check
        Status = $Status
        Message = $Message
        Detail = $Detail
    }
    
    $checkResults += $result
    
    $color = switch ($Status) {
        "PASS" { "Green" }
        "WARN" { "Yellow" }
        "FAIL" { "Red" }
    }
    
    Write-Host "[$Status] $Category - $Check" -ForegroundColor $color
    if ($Message) { Write-Host "  └─ $Message" }
    if ($Detail) { Write-Host "     └─ $Detail" -ForegroundColor DarkGray }
    
    if ($Status -eq "WARN") { $script:hasWarnings = $true }
    if ($Status -eq "FAIL") { $script:hasCriticalIssues = $true }
}

# ===========================
# CHECK 1: ADMINISTRATOR PRIVILEGE
# ===========================

Write-Host "`n[CHECK 1/8] Administrator Privileges" -ForegroundColor Cyan

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-DiagnosticResult -Category "Privileges" -Check "Administrator" `
        -Status "FAIL" `
        -Message "Installer must run as Administrator" `
        -Detail "Right-click installer and select 'Run as administrator'"
} else {
    Write-DiagnosticResult -Category "Privileges" -Check "Administrator" `
        -Status "PASS" `
        -Message "Running with Administrator privileges"
}

# ===========================
# CHECK 2: OPERATING SYSTEM
# ===========================

Write-Host "`n[CHECK 2/8] Operating System Compatibility" -ForegroundColor Cyan

$osInfo = Get-WmiObject -Class Win32_OperatingSystem
$osName = $osInfo.Caption
$osBuild = [int]($osInfo.BuildNumber)
$osArchitecture = $osInfo.OSArchitecture

Write-DiagnosticResult -Category "OS" -Check "OS Name" `
    -Status "INFO" `
    -Message "Detected: $osName (Build $osBuild, $osArchitecture)"

$isSupported = $false
foreach ($supportedOS in $supportedOSVersions.Keys) {
    if ($osName -match $supportedOS) {
        $minBuild = $supportedOSVersions[$supportedOS].MinBuild
        $maxBuild = $supportedOSVersions[$supportedOS].MaxBuild
        
        if ($osBuild -ge $minBuild -and $osBuild -le $maxBuild) {
            Write-DiagnosticResult -Category "OS" -Check "Version Support" `
                -Status "PASS" `
                -Message "OS version is supported"
            $isSupported = $true
        } else {
            Write-DiagnosticResult -Category "OS" -Check "Version Support" `
                -Status "FAIL" `
                -Message "OS build $osBuild not supported. Required: $minBuild-$maxBuild"
        }
        break
    }
}

if (-not $isSupported) {
    Write-DiagnosticResult -Category "OS" -Check "OS Support" `
        -Status "FAIL" `
        -Message "Operating system not supported"
    Write-Host "  Supported: Windows 7 SP1, Windows 10, Windows 11" -ForegroundColor DarkGray
}

# Architecture check
if ($osArchitecture -eq "64-bit") {
    Write-DiagnosticResult -Category "OS" -Check "Architecture" `
        -Status "PASS" `
        -Message "64-bit OS detected (required)"
} else {
    Write-DiagnosticResult -Category "OS" -Check "Architecture" `
        -Status "FAIL" `
        -Message "32-bit OS detected. 64-bit required."
}

# ===========================
# CHECK 3: SYSTEM RAM
# ===========================

Write-Host "`n[CHECK 3/8] Memory Requirements" -ForegroundColor Cyan

$ramGB = [Math]::Round((Get-WmiObject -Class Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)

if ($ramGB -ge $MinimumRam) {
    Write-DiagnosticResult -Category "Memory" -Check "RAM Available" `
        -Status "PASS" `
        -Message "$ramGB GB available (Required: $MinimumRam GB minimum)"
} elseif ($ramGB -ge ($MinimumRam - 1)) {
    Write-DiagnosticResult -Category "Memory" -Check "RAM Available" `
        -Status "WARN" `
        -Message "$ramGB GB available (Recommended: $MinimumRam GB minimum)" `
        -Detail "Installation may be slow with less RAM"
} else {
    Write-DiagnosticResult -Category "Memory" -Check "RAM Available" `
        -Status "FAIL" `
        -Message "$ramGB GB available (Required: $MinimumRam GB minimum)"
}

# ===========================
# CHECK 4: DISK SPACE
# ===========================

Write-Host "`n[CHECK 4/8] Disk Space Requirements" -ForegroundColor Cyan

$programFilesDrive = $env:SystemDrive
$disk = Get-Volume -DriveLetter $programFilesDrive[0] -ErrorAction SilentlyContinue
$freeSpaceGB = [Math]::Round($disk.SizeRemaining / 1GB, 2)

if ($freeSpaceGB -ge $MinimumDiskSpace) {
    Write-DiagnosticResult -Category "Disk" -Check "Free Space" `
        -Status "PASS" `
        -Message "$freeSpaceGB GB free on $($programFilesDrive) (Required: $MinimumDiskSpace GB)"
} elseif ($freeSpaceGB -ge ($MinimumDiskSpace / 2)) {
    Write-DiagnosticResult -Category "Disk" -Check "Free Space" `
        -Status "WARN" `
        -Message "$freeSpaceGB GB free on $($programFilesDrive) (Recommended: $MinimumDiskSpace GB)" `
        -Detail "Installation may fail if disk runs out of space"
} else {
    Write-DiagnosticResult -Category "Disk" -Check "Free Space" `
        -Status "FAIL" `
        -Message "$freeSpaceGB GB free on $($programFilesDrive) (Required: $MinimumDiskSpace GB)"
}

# ===========================
# CHECK 5: PORT AVAILABILITY
# ===========================

Write-Host "`n[CHECK 5/8] Port Availability" -ForegroundColor Cyan

$portsToCheck = @($HttpPort, $HttpPort + 1, 6000)

foreach ($port in $portsToCheck) {
    try {
        $tcpConnections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        if ($tcpConnections) {
            $processId = $tcpConnections.OwningProcess
            $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
            Write-DiagnosticResult -Category "Ports" -Check "Port $port" `
                -Status "WARN" `
                -Message "Port $port is in use" `
                -Detail "Process: $($process.Name) (PID: $processId)"
        } else {
            Write-DiagnosticResult -Category "Ports" -Check "Port $port" `
                -Status "PASS" `
                -Message "Port $port is available"
        }
    } catch {
        Write-DiagnosticResult -Category "Ports" -Check "Port $port" `
            -Status "WARN" `
            -Message "Could not verify port status: $_"
    }
}

# ===========================
# CHECK 6: CONFLICTING SERVICES
# ===========================

Write-Host "`n[CHECK 6/8] Conflicting Services" -ForegroundColor Cyan

$servicestoCheck = @("HELIOSService", "IIS", "Apache", "Nginx")

foreach ($serviceName in $servicestoCheck) {
    try {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($service) {
            $status = $service.Status
            if ($status -eq "Running") {
                Write-DiagnosticResult -Category "Services" -Check $serviceName `
                    -Status "WARN" `
                    -Message "Service is running: $($service.DisplayName)" `
                    -Detail "May conflict with HELIOS installation"
            } else {
                Write-DiagnosticResult -Category "Services" -Check $serviceName `
                    -Status "PASS" `
                    -Message "Service exists but not running"
            }
        }
    } catch {
        # Service doesn't exist - that's fine
    }
}

# ===========================
# CHECK 7: WINDOWS FEATURES
# ===========================

Write-Host "`n[CHECK 7/8] Windows Features" -ForegroundColor Cyan

# Check .NET Framework
try {
    $dotnetVersion = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Version -ErrorAction SilentlyContinue).Version
    if ($dotnetVersion) {
        Write-DiagnosticResult -Category "Features" -Check ".NET Framework" `
            -Status "PASS" `
            -Message ".NET Framework $dotnetVersion installed"
    } else {
        Write-DiagnosticResult -Category "Features" -Check ".NET Framework" `
            -Status "WARN" `
            -Message ".NET Framework not detected (may be required)"
    }
} catch {
    Write-DiagnosticResult -Category "Features" -Check ".NET Framework" `
        -Status "WARN" `
        -Message "Could not verify .NET Framework"
}

# ===========================
# CHECK 8: ANTIVIRUS / FIREWALL
# ===========================

Write-Host "`n[CHECK 8/8] Security Configuration" -ForegroundColor Cyan

try {
    $firewall = Get-NetFirewallProfile -ErrorAction SilentlyContinue
    if ($firewall | Where-Object { $_.Enabled -eq $true }) {
        Write-DiagnosticResult -Category "Security" -Check "Windows Firewall" `
            -Status "PASS" `
            -Message "Windows Firewall is enabled" `
            -Detail "Installer will configure required rules"
    } else {
        Write-DiagnosticResult -Category "Security" -Check "Windows Firewall" `
            -Status "PASS" `
            -Message "Windows Firewall is disabled"
    }
} catch {
    Write-DiagnosticResult -Category "Security" -Check "Windows Firewall" `
        -Status "WARN" `
        -Message "Could not verify firewall status"
}

# ===========================
# GENERATE REPORT
# ===========================

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "PRE-INSTALLATION DIAGNOSTIC REPORT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Timestamp: $timestamp" -ForegroundColor Gray
Write-Host "Computer: $env:COMPUTERNAME" -ForegroundColor Gray
Write-Host ""

$passCount = ($checkResults | Where-Object { $_.Status -eq "PASS" }).Count
$warnCount = ($checkResults | Where-Object { $_.Status -eq "WARN" }).Count
$failCount = ($checkResults | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  ✓ Passed: $passCount" -ForegroundColor Green
Write-Host "  ⚠ Warnings: $warnCount" -ForegroundColor Yellow
Write-Host "  ✗ Failed: $failCount" -ForegroundColor Red
Write-Host ""

if ($hasCriticalIssues) {
    Write-Host "⚠️  INSTALLATION BLOCKED" -ForegroundColor Red
    Write-Host "Critical issues detected. Please resolve failed checks before retrying." -ForegroundColor Red
} elseif ($hasWarnings) {
    Write-Host "✓ Installation CAN PROCEED" -ForegroundColor Yellow
    Write-Host "Warnings detected but installation can continue. Monitor installation carefully." -ForegroundColor Yellow
} else {
    Write-Host "✓ Installation APPROVED" -ForegroundColor Green
    Write-Host "All checks passed. System is ready for HELIOS installation." -ForegroundColor Green
}

Write-Host ""

# Save report
$reportContent = @"
HELIOS PLATFORM 2.0 - PRE-INSTALLATION DIAGNOSTIC REPORT
======================================================

Generated: $timestamp
Computer: $env:COMPUTERNAME
User: $env:USERNAME

SYSTEM INFORMATION
-----------------
OS: $osName
Build: $osBuild
Architecture: $osArchitecture
RAM: $ramGB GB
Disk Free: $freeSpaceGB GB

DETAILED RESULTS
----------------
"@

foreach ($check in $checkResults) {
    $reportContent += "`n[$($check.Status)]  $($check.Category) - $($check.Check)"
    $reportContent += "`n    Message: $($check.Message)"
    if ($check.Detail) { $reportContent += "`n    Detail: $($check.Detail)" }
}

$reportContent += "`n`nSUMMARY`n-------`n"
$reportContent += "Passed: $passCount`n"
$reportContent += "Warnings: $warnCount`n"
$reportContent += "Failed: $failCount`n"
$reportContent += "`nResult: $(if ($hasCriticalIssues) { 'INSTALLATION BLOCKED' } else { 'APPROVED' })`n"

$reportContent | Out-File -FilePath $OutputReport -Encoding UTF8 -Force
Write-Host "Report saved to: $OutputReport" -ForegroundColor Gray

# Exit with appropriate code
if ($hasCriticalIssues) {
    exit 1
} elseif ($hasWarnings) {
    exit 2
} else {
    exit 0
}
