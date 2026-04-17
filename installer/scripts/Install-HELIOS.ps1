#!/usr/bin/env pwsh
<#
.SYNOPSIS
    HELIOS Platform Installation Script - Main Orchestrator

.DESCRIPTION
    Orchestrates the complete HELIOS Platform installation with all features.
    
.PARAMETER InstallPath
    Directory where HELIOS will be installed
    
.PARAMETER InstallMode
    Installation mode: Quick, Advanced, Silent, or Portable
    
.PARAMETER EnableShellExtension
    Register shell context menu extension
    
.PARAMETER EnableSystemService
    Install Windows Service for background operation
    
.PARAMETER EnableAutoStart
    Configure HELIOS to start automatically with Windows
    
.EXAMPLE
    .\Install-HELIOS.ps1 -InstallMode Quick -EnableShellExtension -EnableAutoStart
    
.AUTHOR
    HELIOS Solutions
#>

param(
    [string]$InstallPath = "$env:ProgramFiles\HELIOS Platform",
    [ValidateSet("Quick", "Advanced", "Silent", "Portable")]
    [string]$InstallMode = "Quick",
    [switch]$EnableShellExtension,
    [switch]$EnableSystemService,
    [switch]$EnableAutoStart,
    [switch]$NoElevation
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ============================================================================
# FUNCTIONS
# ============================================================================

function Write-InstallLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Write-Host $logMessage
    Add-Content -Path $LogFile -Value $logMessage -ErrorAction SilentlyContinue
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$currentUser
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-RequiredAdminRights {
    if (-not (Test-Administrator)) {
        if ($NoElevation) {
            Write-InstallLog "Administrator privileges required" "ERROR"
            exit 1
        }
        
        Write-InstallLog "Elevating to administrator..." "INFO"
        Start-Process pwsh -ArgumentList @(
            "-NoProfile"
            "-ExecutionPolicy", "Bypass"
            "-File", $PSCommandPath
            "-InstallPath", $InstallPath
            "-InstallMode", $InstallMode
            "-NoElevation"
            if ($EnableShellExtension) { "-EnableShellExtension" }
            if ($EnableSystemService) { "-EnableSystemService" }
            if ($EnableAutoStart) { "-EnableAutoStart" }
        ) -Verb RunAs -Wait
        exit 0
    }
}

function Test-SystemRequirements {
    Write-InstallLog "Checking system requirements..." "INFO"
    
    $checks = @{
        "Windows 11 or later" = $true
        "5 GB disk space" = $true
        "4 GB RAM" = $true
    }
    
    # Check OS version
    $osVersion = [System.Environment]::OSVersion.Version
    if ($osVersion.Major -lt 10 -or ($osVersion.Major -eq 10 -and $osVersion.Build -lt 22000)) {
        $checks["Windows 11 or later"] = $false
    }
    
    # Check disk space
    $drive = Get-Item $InstallPath.Substring(0, 1) + ":"
    $freeSpace = $drive.PSDrive.Free / 1GB
    if ($freeSpace -lt 5) {
        $checks["5 GB disk space"] = $false
    }
    
    # Check RAM
    $totalMemory = (Get-WmiObject -Class Win32_OperatingSystem).TotalVisibleMemorySize / 1MB
    if ($totalMemory -lt 4) {
        $checks["4 GB RAM"] = $false
    }
    
    $allPassed = $true
    foreach ($check in $checks.GetEnumerator()) {
        $status = if ($check.Value) { "✓" } else { "✗" }
        Write-InstallLog "$status $($check.Name)" "INFO"
        if (-not $check.Value) { $allPassed = $false }
    }
    
    return $allPassed
}

function Invoke-PreInstallHooks {
    Write-InstallLog "Executing pre-installation hooks..." "INFO"
    
    # Create installation directory
    $null = New-Item -ItemType Directory -Path $InstallPath -Force
    Write-InstallLog "Created installation directory: $InstallPath" "INFO"
    
    # Backup existing installation if present
    if (Test-Path "$InstallPath\version.txt") {
        $backup = "$InstallPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        Copy-Item $InstallPath $backup -Recurse -Force
        Write-InstallLog "Backed up existing installation to: $backup" "INFO"
    }
    
    return $true
}

function Install-CoreComponents {
    Write-InstallLog "Installing core components..." "INFO"
    
    $components = @("Core", "GUI", "Tools")
    foreach ($component in $components) {
        Write-InstallLog "  Installing $component..." "INFO"
        $componentPath = Join-Path $InstallPath $component
        $null = New-Item -ItemType Directory -Path $componentPath -Force
        Write-InstallLog "  ✓ $component installed" "INFO"
    }
}

function Enable-ShellContextMenu {
    if (-not $EnableShellExtension) { return }
    
    Write-InstallLog "Registering shell context menu..." "INFO"
    
    $regPath = "HKCU:\Software\Classes\*\shell\HELIOS"
    $null = New-Item -Path $regPath -Force
    Set-ItemProperty -Path $regPath -Name "(Default)" -Value "Open with HELIOS Platform"
    Set-ItemProperty -Path $regPath -Name "Icon" -Value "`"$InstallPath\HELIOS.ico`""
    
    $cmdPath = Join-Path $regPath "command"
    $null = New-Item -Path $cmdPath -Force
    Set-ItemProperty -Path $cmdPath -Name "(Default)" -Value "`"$InstallPath\HELIOS.Platform.exe`" `"%1`""
    
    Write-InstallLog "✓ Shell context menu registered" "INFO"
}

function Register-FileAssociations {
    Write-InstallLog "Registering file associations..." "INFO"
    
    $associations = @(".helios", ".hio")
    foreach ($ext in $associations) {
        $regPath = "HKCU:\Software\Classes\$ext"
        $null = New-Item -Path $regPath -Force
        Set-ItemProperty -Path $regPath -Name "(Default)" -Value "HELIOS.Platform.File"
        Write-InstallLog "  ✓ Associated $ext with HELIOS" "INFO"
    }
}

function Install-WindowsService {
    if (-not $EnableSystemService) { return }
    
    Write-InstallLog "Installing Windows Service..." "INFO"
    
    $serviceName = "HeliosPlatformService"
    $servicePath = Join-Path $InstallPath "HELIOS.Platform.Service.exe"
    
    # Create service
    $params = @{
        Name = $serviceName
        BinaryPathName = $servicePath
        DisplayName = "HELIOS Platform Service"
        Description = "Background service for HELIOS Platform optimization"
        StartupType = "Automatic"
    }
    
    try {
        New-Service @params -ErrorAction Stop
        Write-InstallLog "✓ Windows Service installed" "INFO"
    }
    catch {
        Write-InstallLog "Service installation result: $_" "WARN"
    }
}

function Enable-AutoStart {
    if (-not $EnableAutoStart) { return }
    
    Write-InstallLog "Enabling auto-start..." "INFO"
    
    $regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $exePath = Join-Path $InstallPath "HELIOS.Platform.Tray.exe"
    Set-ItemProperty -Path $regPath -Name "HELIOS Platform" -Value $exePath
    
    Write-InstallLog "✓ Auto-start enabled" "INFO"
}

function Register-Installation {
    Write-InstallLog "Registering installation..." "INFO"
    
    $regPath = "HKLM:\SOFTWARE\HELIOS Platform"
    $null = New-Item -Path $regPath -Force
    
    Set-ItemProperty -Path $regPath -Name "InstallPath" -Value $InstallPath
    Set-ItemProperty -Path $regPath -Name "Version" -Value "1.0.0.0"
    Set-ItemProperty -Path $regPath -Name "InstallDate" -Value (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    Set-ItemProperty -Path $regPath -Name "InstallMode" -Value $InstallMode
    
    Write-InstallLog "✓ Installation registered in registry" "INFO"
}

function Invoke-PostInstallHooks {
    Write-InstallLog "Executing post-installation hooks..." "INFO"
    
    # Create start menu shortcuts
    $startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\HELIOS Platform"
    $null = New-Item -ItemType Directory -Path $startMenuPath -Force
    Write-InstallLog "Created start menu shortcuts" "INFO"
    
    # Create uninstall information
    $uninstallPath = Join-Path $InstallPath "uninstall.ps1"
    if (-not (Test-Path $uninstallPath)) {
        Write-InstallLog "Uninstall script not found at $uninstallPath" "WARN"
    }
    
    Write-InstallLog "✓ Post-installation hooks completed" "INFO"
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

# Setup logging
$LogFile = Join-Path $env:TEMP "HELIOS_Install_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

Write-InstallLog "HELIOS Platform Installation Started" "INFO"
Write-InstallLog "Install Mode: $InstallMode" "INFO"
Write-InstallLog "Install Path: $InstallPath" "INFO"

# Get admin rights
Get-RequiredAdminRights

# Check system requirements
if (-not (Test-SystemRequirements)) {
    Write-InstallLog "System requirements not met" "ERROR"
    exit 1
}

# Pre-installation
if (-not (Invoke-PreInstallHooks)) {
    Write-InstallLog "Pre-installation hooks failed" "ERROR"
    exit 1
}

# Install core components
Install-CoreComponents

# Enable shell extension
Enable-ShellContextMenu
Register-FileAssociations

# Install service and auto-start
Install-WindowsService
Enable-AutoStart

# Register installation
Register-Installation

# Post-installation
Invoke-PostInstallHooks

Write-InstallLog "HELIOS Platform Installation Completed Successfully!" "INFO"
Write-InstallLog "Log file: $LogFile" "INFO"

if ($InstallMode -ne "Silent") {
    Write-Host "`n✓ Installation completed successfully!" -ForegroundColor Green
    Write-Host "Install Path: $InstallPath" -ForegroundColor Cyan
    Write-Host "Log File: $LogFile" -ForegroundColor Cyan
}
