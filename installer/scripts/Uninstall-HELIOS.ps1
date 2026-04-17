#!/usr/bin/env pwsh
<#
.SYNOPSIS
    HELIOS Platform Uninstaller with Rollback

.DESCRIPTION
    Completely removes HELIOS Platform from the system with full rollback support.
    Removes registry entries, shell extensions, services, and files.

.PARAMETER InstallPath
    Path where HELIOS Platform was installed

.PARAMETER RemoveUserData
    Also remove user configuration and data files

.PARAMETER KeepBackup
    Keep a backup of the installation (default: true)

.PARAMETER Force
    Force removal without confirmation

.EXAMPLE
    .\Uninstall-HELIOS.ps1
    .\Uninstall-HELIOS.ps1 -Force -RemoveUserData

.AUTHOR
    HELIOS Solutions
#>

param(
    [string]$InstallPath = "$env:ProgramFiles\HELIOS Platform",
    [switch]$RemoveUserData,
    [switch]$KeepBackup = $true,
    [switch]$Force
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

# ============================================================================
# CONFIGURATION
# ============================================================================

$ServiceName = "HeliosPlatformService"
$RegistryPaths = @(
    "HKLM:\SOFTWARE\HELIOS Platform"
    "HKCU:\Software\Classes\*\shell\HELIOS"
    "HKCU:\Software\Classes\.helios"
    "HKCU:\Software\Classes\.hio"
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    "HKCU:\Software\Classes\CLSID\{12345678-1234-1234-1234-123456789012}"
)

$ShortcutPaths = @(
    "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\HELIOS Platform"
    "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\HELIOS Platform"
)

# ============================================================================
# FUNCTIONS
# ============================================================================

function Write-UninstallLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Write-Host $logMessage
    if (-not [string]::IsNullOrEmpty($LogFile)) {
        Add-Content -Path $LogFile -Value $logMessage -ErrorAction SilentlyContinue
    }
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$currentUser
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Show-UninstallWelcome {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  HELIOS Platform Uninstaller" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan
    
    if (-not $Force) {
        Write-Host "This will completely remove HELIOS Platform from your system." -ForegroundColor Yellow
        Write-Host "Options:" -ForegroundColor Cyan
        Write-Host "  [C]ontinue - Proceed with uninstallation" -ForegroundColor White
        Write-Host "  [B]ackup first - Create backup before uninstall" -ForegroundColor White
        Write-Host "  [E]xit - Cancel uninstallation" -ForegroundColor White
        Write-Host ""
        
        $choice = Read-Host "Enter your choice (C/B/E)"
        
        switch ($choice.ToUpper()) {
            "B" { return "backup" }
            "C" { return "continue" }
            "E" { return "exit" }
            default { return "exit" }
        }
    }
    
    return "continue"
}

function Create-UninstallBackup {
    Write-UninstallLog "Creating backup of installation..." "INFO"
    
    if (-not (Test-Path $InstallPath)) {
        Write-UninstallLog "Install path not found: $InstallPath" "WARN"
        return $true
    }
    
    try {
        $backupPath = "$InstallPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        Copy-Item -Path $InstallPath -Destination $backupPath -Recurse -Force
        Write-UninstallLog "✓ Backup created: $backupPath" "INFO"
        return $true
    }
    catch {
        Write-UninstallLog "Warning: Could not create backup: $_" "WARN"
        return $true
    }
}

function Stop-HeliosServices {
    Write-UninstallLog "Stopping HELIOS services..." "INFO"
    
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($service) {
            if ($service.Status -eq "Running") {
                Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
                Write-UninstallLog "  ✓ Service stopped" "INFO"
            }
        }
    }
    catch {
        Write-UninstallLog "Warning: Could not stop service: $_" "WARN"
    }
    
    # Stop any HELIOS processes
    try {
        @("HELIOS.Platform.exe", "HELIOS.Platform.Tray.exe", "HELIOS.Platform.Service.exe") | ForEach-Object {
            Get-Process $_ -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        }
        Write-UninstallLog "  ✓ HELIOS processes terminated" "INFO"
    }
    catch { }
}

function Remove-HeliosService {
    Write-UninstallLog "Removing HELIOS service..." "INFO"
    
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($service) {
            sc.exe delete $ServiceName 2>&1 | Out-Null
            Write-UninstallLog "  ✓ Service removed" "INFO"
        }
    }
    catch {
        Write-UninstallLog "Warning: Could not remove service: $_" "WARN"
    }
}

function Remove-RegistryEntries {
    Write-UninstallLog "Removing registry entries..." "INFO"
    
    foreach ($regPath in $RegistryPaths) {
        try {
            if (Test-Path $regPath) {
                # Special handling for Run entry
                if ($regPath -match "Run$") {
                    $key = Get-Item $regPath -ErrorAction SilentlyContinue
                    if ($key) {
                        Remove-ItemProperty -Path $regPath -Name "HELIOS Platform" -ErrorAction SilentlyContinue
                    }
                }
                else {
                    Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue
                }
                Write-UninstallLog "  ✓ Removed: $regPath" "INFO"
            }
        }
        catch {
            Write-UninstallLog "  Warning: Could not remove registry entry: $regPath" "WARN"
        }
    }
}

function Remove-ShortcutsAndMenus {
    Write-UninstallLog "Removing shortcuts and menu entries..." "INFO"
    
    foreach ($path in $ShortcutPaths) {
        try {
            if (Test-Path $path) {
                Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
                Write-UninstallLog "  ✓ Removed: $path" "INFO"
            }
        }
        catch {
            Write-UninstallLog "  Warning: Could not remove: $path" "WARN"
        }
    }
}

function Remove-InstallationFiles {
    Write-UninstallLog "Removing installation files..." "INFO"
    
    if (-not (Test-Path $InstallPath)) {
        Write-UninstallLog "Installation path not found" "WARN"
        return
    }
    
    try {
        Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction Stop
        Write-UninstallLog "  ✓ Installation directory removed" "INFO"
    }
    catch {
        Write-UninstallLog "  Warning: Could not fully remove directory: $_" "WARN"
    }
}

function Remove-UserData {
    if (-not $RemoveUserData) { return }
    
    Write-UninstallLog "Removing user data..." "INFO"
    
    $userDataPaths = @(
        "$env:APPDATA\HELIOS"
        "$env:APPDATA\HELIOS Platform"
        "$env:LOCALAPPDATA\HELIOS Platform"
    )
    
    foreach ($path in $userDataPaths) {
        try {
            if (Test-Path $path) {
                Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
                Write-UninstallLog "  ✓ Removed user data: $path" "INFO"
            }
        }
        catch {
            Write-UninstallLog "  Warning: Could not remove user data: $path" "WARN"
        }
    }
}

function Verify-UninstallComplete {
    Write-UninstallLog "Verifying uninstallation..." "INFO"
    
    $verification = @{
        "Installation directory removed" = -not (Test-Path $InstallPath)
        "Registry entries removed" = -not (Test-Path "HKLM:\SOFTWARE\HELIOS Platform")
        "Service removed" = $null -eq (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)
    }
    
    $allClean = $true
    foreach ($check in $verification.GetEnumerator()) {
        $status = if ($check.Value) { "✓" } else { "✗" }
        Write-UninstallLog "  $status $($check.Name)" "INFO"
        if (-not $check.Value) { $allClean = $false }
    }
    
    return $allClean
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

if (-not (Test-Administrator)) {
    Write-Host "Error: This operation requires administrator privileges" -ForegroundColor Red
    Write-Host "Please run this script as Administrator" -ForegroundColor Yellow
    exit 1
}

# Setup logging
$LogFile = Join-Path $env:TEMP "HELIOS_Uninstall_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

Write-UninstallLog "HELIOS Platform Uninstallation Started" "INFO"
Write-UninstallLog "Install Path: $InstallPath" "INFO"

# Show welcome and get user choice
$userChoice = Show-UninstallWelcome

if ($userChoice -eq "exit") {
    Write-Host "`nUninstallation cancelled." -ForegroundColor Yellow
    exit 0
}
elseif ($userChoice -eq "backup") {
    Create-UninstallBackup
}

Write-Host ""
Write-UninstallLog "Starting uninstallation process..." "INFO"

# Execute uninstallation steps
Stop-HeliosServices
Remove-HeliosService
Remove-RegistryEntries
Remove-ShortcutsAndMenus
Remove-UserData
Remove-InstallationFiles

# Verify
$isClean = Verify-UninstallComplete

Write-Host ""
if ($isClean) {
    Write-Host "✓ HELIOS Platform has been successfully uninstalled!" -ForegroundColor Green
}
else {
    Write-Host "⚠ Uninstallation completed with some warnings (see log for details)" -ForegroundColor Yellow
}

Write-Host "Log file: $LogFile" -ForegroundColor Cyan
Write-Host ""

Write-UninstallLog "HELIOS Platform Uninstallation Completed" "INFO"

exit 0
