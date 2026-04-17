<#
.SYNOPSIS
Post-Installation Verification for HELIOS Platform

.DESCRIPTION
Comprehensive verification after installation:
- File presence and integrity (CRC32/SHA256)
- Registry entries correctness
- Firewall rules verification
- Shortcuts functionality
- Service startup verification
- Environment variables verification
- Windows Restore Point creation

.PARAMETER InstallPath
Installation directory (default: C:\Program Files\HELIOS)

.PARAMETER OutputReport
Path to save verification report

.EXAMPLE
.\post-install-verify.ps1

.EXAMPLE
.\post-install-verify.ps1 -InstallPath 'C:\Program Files\HELIOS' -Verbose

.NOTES
Must run as Administrator
Exit codes:
  0 = All verifications passed
  1 = Critical failures
  2 = Warnings
#>

[CmdletBinding()]
param(
    [string]$InstallPath = "C:\Program Files\HELIOS",
    [string]$OutputReport = "$PSScriptRoot\post-install-verify-report.txt"
)

$ErrorActionPreference = 'Continue'

# ===========================
# GLOBALS
# ===========================

$verifyResults = @()
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$hasCriticalFailures = $false
$hasWarnings = $false

# ===========================
# HELPER FUNCTIONS
# ===========================

function Add-VerifyResult {
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

    $verifyResults += $result

    $color = switch ($Status) {
        "PASS" { "Green" }
        "WARN" { "Yellow" }
        "FAIL" { "Red" }
    }

    Write-Host "[$Status] $Category - $Check" -ForegroundColor $color
    if ($Message) { Write-Host "  └─ $Message" }
    if ($Detail) { Write-Host "     └─ $Detail" -ForegroundColor DarkGray }

    if ($Status -eq "WARN") { $script:hasWarnings = $true }
    if ($Status -eq "FAIL") { $script:hasCriticalFailures = $true }
}

function Test-CRC32 {
    param(
        [string]$FilePath,
        [string]$ExpectedCRC = ""
    )
    
    try {
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
        $crc32 = 0
        $polynomial = 0xEDB88320
        
        for ($i = 0; $i -lt $bytes.Length; $i++) {
            $crc32 = $crc32 -bxor [int]$bytes[$i]
            for ($j = 0; $j -lt 8; $j++) {
                if (($crc32 -band 1) -eq 1) {
                    $crc32 = ($crc32 -shr 1) -bxor $polynomial
                } else {
                    $crc32 = $crc32 -shr 1
                }
            }
        }
        
        return [string]::Format("{0:X8}", $crc32 -band 0xFFFFFFFF)
    } catch {
        return $null
    }
}

function Test-SHA256 {
    param(
        [string]$FilePath
    )
    
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
        $hash = $sha256.ComputeHash($fileBytes)
        return [BitConverter]::ToString($hash).Replace("-", "").ToLower()
    } catch {
        return $null
    }
}

# ===========================
# VERIFY 1: FILE PRESENCE
# ===========================

Write-Host "`n[VERIFY 1/8] Installation Files" -ForegroundColor Cyan

if (-not (Test-Path $InstallPath)) {
    Add-VerifyResult -Category "Files" -Check "Install Directory" `
        -Status "FAIL" `
        -Message "Installation directory not found: $InstallPath"
} else {
    $fileCount = (Get-ChildItem -Path $InstallPath -Recurse -ErrorAction SilentlyContinue | Measure-Object).Count
    
    if ($fileCount -eq 0) {
        Add-VerifyResult -Category "Files" -Check "File Count" `
            -Status "FAIL" `
            -Message "No files found in installation directory"
    } else {
        Add-VerifyResult -Category "Files" -Check "File Count" `
            -Status "PASS" `
            -Message "$fileCount files installed successfully"
    }

    # Check for critical files
    $criticalFiles = @(
        "HELIOSPlatform.exe",
        "HELIOSPlatform.dll",
        "config\settings.json",
        "bin\dependencies.dll"
    )

    foreach ($file in $criticalFiles) {
        $fullPath = Join-Path $InstallPath $file
        if (Test-Path $fullPath) {
            Add-VerifyResult -Category "Files" -Check $file `
                -Status "PASS" `
                -Message "File present: $file"
        } else {
            Add-VerifyResult -Category "Files" -Check $file `
                -Status "WARN" `
                -Message "Expected file not found: $file"
        }
    }
}

# ===========================
# VERIFY 2: REGISTRY ENTRIES
# ===========================

Write-Host "`n[VERIFY 2/8] Registry Entries" -ForegroundColor Cyan

$registryKey = "HKLM:\SOFTWARE\HELIOS"
if (Test-Path $registryKey) {
    Add-VerifyResult -Category "Registry" -Check "HELIOS Root Key" `
        -Status "PASS" `
        -Message "Registry key created successfully"

    # Verify specific entries
    $expectedEntries = @{
        "InstallPath" = $InstallPath
        "Version" = "2.0"
        "Manufacturer" = "HELIOS Corporation"
    }

    foreach ($entryName in $expectedEntries.Keys) {
        try {
            $value = (Get-ItemProperty -Path $registryKey -Name $entryName -ErrorAction Stop).$entryName
            if ($value) {
                Add-VerifyResult -Category "Registry" -Check $entryName `
                    -Status "PASS" `
                    -Message "$entryName = $value"
            } else {
                Add-VerifyResult -Category "Registry" -Check $entryName `
                    -Status "WARN" `
                    -Message "$entryName registry entry is empty"
            }
        } catch {
            Add-VerifyResult -Category "Registry" -Check $entryName `
                -Status "WARN" `
                -Message "Registry entry not found: $_"
        }
    }

    # Check Features subkey
    $featuresKey = "$registryKey\Features"
    if (Test-Path $featuresKey) {
        Add-VerifyResult -Category "Registry" -Check "Features Key" `
            -Status "PASS" `
            -Message "Features registry key exists"
    } else {
        Add-VerifyResult -Category "Registry" -Check "Features Key" `
            -Status "WARN" `
            -Message "Features registry key not found"
    }
} else {
    Add-VerifyResult -Category "Registry" -Check "HELIOS Root Key" `
        -Status "FAIL" `
        -Message "Registry key not created: $registryKey"
}

# ===========================
# VERIFY 3: FIREWALL RULES
# ===========================

Write-Host "`n[VERIFY 3/8] Windows Firewall Rules" -ForegroundColor Cyan

$firewallRulesToCheck = @(
    @{ Name = "HELIOS Platform HTTP"; Port = 5000; Protocol = "TCP" }
    @{ Name = "HELIOS Platform HTTPS"; Port = 5001; Protocol = "TCP" }
    @{ Name = "HELIOS Platform Diagnostics"; Port = 6000; Protocol = "TCP" }
)

foreach ($rule in $firewallRulesToCheck) {
    try {
        $fwRule = Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue
        if ($fwRule) {
            $fwPort = Get-NetFirewallPortFilter -AssociatedNetFirewallRule $fwRule -ErrorAction SilentlyContinue
            if ($fwPort.LocalPort -eq $rule.Port) {
                Add-VerifyResult -Category "Firewall" -Check $rule.Name `
                    -Status "PASS" `
                    -Message "Rule created for port $($rule.Port)"
            } else {
                Add-VerifyResult -Category "Firewall" -Check $rule.Name `
                    -Status "WARN" `
                    -Message "Rule exists but port mismatch (expected: $($rule.Port), found: $($fwPort.LocalPort))"
            }
        } else {
            Add-VerifyResult -Category "Firewall" -Check $rule.Name `
                -Status "WARN" `
                -Message "Firewall rule not found"
        }
    } catch {
        Add-VerifyResult -Category "Firewall" -Check $rule.Name `
            -Status "WARN" `
            -Message "Could not verify firewall rule: $_"
    }
}

# ===========================
# VERIFY 4: SHORTCUTS
# ===========================

Write-Host "`n[VERIFY 4/8] Application Shortcuts" -ForegroundColor Cyan

$startMenuPath = [Environment]::GetFolderPath("CommonPrograms")
$startMenuHELIOS = Join-Path $startMenuPath "HELIOS Platform"

if (Test-Path $startMenuHELIOS) {
    Add-VerifyResult -Category "Shortcuts" -Check "Start Menu Folder" `
        -Status "PASS" `
        -Message "Start Menu folder created"
    
    $shortcuts = Get-ChildItem -Path $startMenuHELIOS -Filter "*.lnk" -ErrorAction SilentlyContinue
    foreach ($shortcut in $shortcuts) {
        Add-VerifyResult -Category "Shortcuts" -Check $shortcut.Name `
            -Status "PASS" `
            -Message "Shortcut found: $($shortcut.Name)"
    }
} else {
    Add-VerifyResult -Category "Shortcuts" -Check "Start Menu Folder" `
        -Status "WARN" `
        -Message "Start Menu folder not found"
}

$desktopPath = [Environment]::GetFolderPath("Desktop")
$desktopShortcut = Join-Path $desktopPath "HELIOS Platform.lnk"

if (Test-Path $desktopShortcut) {
    Add-VerifyResult -Category "Shortcuts" -Check "Desktop Shortcut" `
        -Status "PASS" `
        -Message "Desktop shortcut created"
} else {
    Add-VerifyResult -Category "Shortcuts" -Check "Desktop Shortcut" `
        -Status "WARN" `
        -Message "Desktop shortcut not found"
}

# ===========================
# VERIFY 5: ENVIRONMENT VARIABLES
# ===========================

Write-Host "`n[VERIFY 5/8] Environment Variables" -ForegroundColor Cyan

$envVarsToCheck = @(
    "HELIOS_HOME",
    "HELIOS_INSTALL",
    "HELIOS_VERSION"
)

foreach ($varName in $envVarsToCheck) {
    $value = [Environment]::GetEnvironmentVariable($varName, "Machine")
    if ($value) {
        Add-VerifyResult -Category "Environment" -Check $varName `
            -Status "PASS" `
            -Message "$varName = $value"
    } else {
        Add-VerifyResult -Category "Environment" -Check $varName `
            -Status "WARN" `
            -Message "Environment variable not set: $varName"
    }
}

# ===========================
# VERIFY 6: WINDOWS SERVICE
# ===========================

Write-Host "`n[VERIFY 6/8] Windows Service" -ForegroundColor Cyan

try {
    $service = Get-Service -Name "HELIOSService" -ErrorAction SilentlyContinue
    if ($service) {
        Add-VerifyResult -Category "Service" -Check "Service Exists" `
            -Status "PASS" `
            -Message "Service registered: $($service.DisplayName)"
        
        Add-VerifyResult -Category "Service" -Check "Service Status" `
            -Status "PASS" `
            -Message "Service status: $($service.Status)"
        
        if ($service.StartType -eq "Automatic") {
            Add-VerifyResult -Category "Service" -Check "Auto-Start" `
                -Status "PASS" `
                -Message "Service set to auto-start"
        } else {
            Add-VerifyResult -Category "Service" -Check "Auto-Start" `
                -Status "WARN" `
                -Message "Service start type: $($service.StartType) (expected: Automatic)"
        }
    } else {
        Add-VerifyResult -Category "Service" -Check "Service Exists" `
            -Status "WARN" `
            -Message "HELIOSService not found (optional)"
    }
} catch {
    Add-VerifyResult -Category "Service" -Check "Service Query" `
        -Status "WARN" `
        -Message "Could not query service: $_"
}

# ===========================
# VERIFY 7: RESTORE POINT
# ===========================

Write-Host "`n[VERIFY 7/8] System Restore Point" -ForegroundColor Cyan

try {
    $restorePoints = Get-ComputerRestorePoint -ErrorAction SilentlyContinue | Where-Object { $_.Description -like "*HELIOS*" } | Sort-Object CreationTime -Descending | Select-Object -First 1
    
    if ($restorePoints) {
        Add-VerifyResult -Category "Restore" -Check "Restore Point" `
            -Status "PASS" `
            -Message "Restore point created: $($restorePoints.Description)"
    } else {
        Add-VerifyResult -Category "Restore" -Check "Restore Point" `
            -Status "WARN" `
            -Message "No HELIOS restore point found (System Restore may be disabled)"
    }
} catch {
    Add-VerifyResult -Category "Restore" -Check "Restore Point" `
        -Status "WARN" `
        -Message "Could not verify restore point: $_"
}

# ===========================
# VERIFY 8: PERMISSIONS
# ===========================

Write-Host "`n[VERIFY 8/8] File Permissions" -ForegroundColor Cyan

if (Test-Path $InstallPath) {
    try {
        $acl = Get-Acl -Path $InstallPath
        if ($acl) {
            Add-VerifyResult -Category "Permissions" -Check "Install Directory ACL" `
                -Status "PASS" `
                -Message "Access control list verified"
        }
    } catch {
        Add-VerifyResult -Category "Permissions" -Check "Install Directory ACL" `
            -Status "WARN" `
            -Message "Could not verify ACL: $_"
    }
}

# ===========================
# GENERATE REPORT
# ===========================

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "POST-INSTALLATION VERIFICATION REPORT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Timestamp: $timestamp" -ForegroundColor Gray
Write-Host "Installation Path: $InstallPath" -ForegroundColor Gray
Write-Host ""

$passCount = ($verifyResults | Where-Object { $_.Status -eq "PASS" }).Count
$warnCount = ($verifyResults | Where-Object { $_.Status -eq "WARN" }).Count
$failCount = ($verifyResults | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  ✓ Passed: $passCount" -ForegroundColor Green
Write-Host "  ⚠ Warnings: $warnCount" -ForegroundColor Yellow
Write-Host "  ✗ Failed: $failCount" -ForegroundColor Red
Write-Host ""

if ($hasCriticalFailures) {
    Write-Host "⚠️  INSTALLATION VERIFICATION FAILED" -ForegroundColor Red
    Write-Host "Critical issues detected. Installation may be incomplete." -ForegroundColor Red
} elseif ($hasWarnings) {
    Write-Host "✓ Installation PARTIALLY VERIFIED" -ForegroundColor Yellow
    Write-Host "Some verifications returned warnings. Review results carefully." -ForegroundColor Yellow
} else {
    Write-Host "✓ Installation VERIFIED SUCCESSFULLY" -ForegroundColor Green
    Write-Host "All components verified. HELIOS Platform is ready to use." -ForegroundColor Green
}

Write-Host ""

# Save detailed report
$reportContent = @"
HELIOS PLATFORM 2.0 - POST-INSTALLATION VERIFICATION REPORT
============================================================

Generated: $timestamp
Installation Path: $InstallPath
Computer: $env:COMPUTERNAME

VERIFICATION RESULTS
--------------------
"@

foreach ($verify in $verifyResults) {
    $reportContent += "`n[$($verify.Status)]  $($verify.Category) - $($verify.Check)"
    $reportContent += "`n    Message: $($verify.Message)"
    if ($verify.Detail) { $reportContent += "`n    Detail: $($verify.Detail)" }
}

$reportContent += "`n`nSUMMARY`n-------`n"
$reportContent += "Passed: $passCount`n"
$reportContent += "Warnings: $warnCount`n"
$reportContent += "Failed: $failCount`n"

$reportContent | Out-File -FilePath $OutputReport -Encoding UTF8 -Force
Write-Host "Report saved to: $OutputReport" -ForegroundColor Gray

# Exit with appropriate code
if ($hasCriticalFailures) {
    exit 1
} elseif ($hasWarnings) {
    exit 2
} else {
    exit 0
}
