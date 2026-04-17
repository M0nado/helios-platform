#!/usr/bin/env pwsh
<#
.SYNOPSIS
    HELIOS Windows Service Management

.DESCRIPTION
    Manages the HELIOS Platform Windows Service installation,
    startup configuration, and background operations.

.PARAMETER Install
    Install the HELIOS Platform Windows Service

.PARAMETER Uninstall
    Remove the HELIOS Platform Windows Service

.PARAMETER Start
    Start the service

.PARAMETER Stop
    Stop the service

.PARAMETER Status
    Check service status

.PARAMETER ServicePath
    Path to the service executable

.EXAMPLE
    .\Manage-Service.ps1 -Install -ServicePath "C:\Program Files\HELIOS Platform\Service.exe"
    .\Manage-Service.ps1 -Status
    .\Manage-Service.ps1 -Start

.AUTHOR
    HELIOS Solutions
#>

param(
    [switch]$Install,
    [switch]$Uninstall,
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status,
    [switch]$AutoStart,
    [string]$ServicePath = "C:\Program Files\HELIOS Platform\HELIOS.Platform.Service.exe"
)

$ErrorActionPreference = "Continue"

$ServiceName = "HeliosPlatformService"
$DisplayName = "HELIOS Platform Service"
$Description = "Background service for HELIOS Platform system optimization and monitoring"

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$currentUser
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Ensure-Administrator {
    if (-not (Test-Administrator)) {
        Write-Host "Error: This operation requires administrator privileges" -ForegroundColor Red
        exit 1
    }
}

function Write-ServiceLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Write-Host $logMessage
}

function Get-ServiceStatus {
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($service) {
            return @{
                Name = $service.Name
                DisplayName = $service.DisplayName
                Status = $service.Status
                StartType = $service.StartType
                Running = $service.Status -eq "Running"
            }
        }
        return $null
    }
    catch {
        return $null
    }
}

# ============================================================================
# SERVICE OPERATIONS
# ============================================================================

function Install-HeliosService {
    Write-ServiceLog "Installing HELIOS Platform Service..." "INFO"
    
    if (-not (Test-Path $ServicePath)) {
        Write-ServiceLog "Service executable not found: $ServicePath" "ERROR"
        return $false
    }
    
    try {
        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($existingService) {
            Write-ServiceLog "Service already installed" "WARN"
            return $true
        }
        
        # Create the service
        New-Service -Name $ServiceName `
                   -DisplayName $DisplayName `
                   -BinaryPathName $ServicePath `
                   -Description $Description `
                   -StartupType Manual `
                   -ErrorAction Stop
        
        Write-ServiceLog "✓ Service installed successfully" "INFO"
        
        # Set service recovery options
        sc.exe failure $ServiceName reset= 60 actions= restart/60000
        
        Write-ServiceLog "✓ Service recovery options configured" "INFO"
        return $true
    }
    catch {
        Write-ServiceLog "Failed to install service: $_" "ERROR"
        return $false
    }
}

function Uninstall-HeliosService {
    Write-ServiceLog "Uninstalling HELIOS Platform Service..." "INFO"
    
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-ServiceLog "Service not found" "WARN"
            return $true
        }
        
        # Stop the service if it's running
        if ($service.Status -eq "Running") {
            Write-ServiceLog "Stopping service..." "INFO"
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
        
        # Remove the service
        sc.exe delete $ServiceName
        Start-Sleep -Seconds 1
        
        # Verify removal
        $verifyService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($verifyService) {
            Write-ServiceLog "Warning: Service may not have been fully removed" "WARN"
        }
        else {
            Write-ServiceLog "✓ Service uninstalled successfully" "INFO"
        }
        
        return $true
    }
    catch {
        Write-ServiceLog "Failed to uninstall service: $_" "ERROR"
        return $false
    }
}

function Start-HeliosService {
    Write-ServiceLog "Starting HELIOS Platform Service..." "INFO"
    
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-ServiceLog "Service not found. Install it first." "ERROR"
            return $false
        }
        
        if ($service.Status -eq "Running") {
            Write-ServiceLog "Service is already running" "INFO"
            return $true
        }
        
        Start-Service -Name $ServiceName -ErrorAction Stop
        Start-Sleep -Seconds 1
        
        $updatedService = Get-Service -Name $ServiceName
        if ($updatedService.Status -eq "Running") {
            Write-ServiceLog "✓ Service started successfully" "INFO"
            return $true
        }
        else {
            Write-ServiceLog "Service failed to start (Status: $($updatedService.Status))" "ERROR"
            return $false
        }
    }
    catch {
        Write-ServiceLog "Failed to start service: $_" "ERROR"
        return $false
    }
}

function Stop-HeliosService {
    Write-ServiceLog "Stopping HELIOS Platform Service..." "INFO"
    
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-ServiceLog "Service not found" "WARN"
            return $true
        }
        
        if ($service.Status -ne "Running") {
            Write-ServiceLog "Service is not running" "INFO"
            return $true
        }
        
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        Start-Sleep -Seconds 1
        
        Write-ServiceLog "✓ Service stopped successfully" "INFO"
        return $true
    }
    catch {
        Write-ServiceLog "Failed to stop service: $_" "ERROR"
        return $false
    }
}

function Show-ServiceStatus {
    Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  HELIOS SERVICE STATUS" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

    $status = Get-ServiceStatus
    
    if ($status) {
        Write-Host "  Service Name: $($status.Name)" -ForegroundColor White
        Write-Host "  Display Name: $($status.DisplayName)" -ForegroundColor White
        
        $statusColor = if ($status.Running) { "Green" } else { "Red" }
        Write-Host "  Status: $($status.Status)" -ForegroundColor $statusColor
        
        Write-Host "  Start Type: $($status.StartType)" -ForegroundColor Cyan
        Write-Host "  Running: $(if ($status.Running) { 'Yes ✓' } else { 'No ✗' })" -ForegroundColor $statusColor
    }
    else {
        Write-Host "  Service is not installed" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

function Set-AutoStart {
    Write-ServiceLog "Enabling auto-start..." "INFO"
    
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-ServiceLog "Service not found" "ERROR"
            return $false
        }
        
        Set-Service -Name $ServiceName -StartupType Automatic -ErrorAction Stop
        Write-ServiceLog "✓ Service will now start automatically" "INFO"
        
        # Also register in registry for recovery
        $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
        Set-ItemProperty -Path $regPath -Name "Start" -Value 2
        
        return $true
    }
    catch {
        Write-ServiceLog "Failed to enable auto-start: $_" "ERROR"
        return $false
    }
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host ""
Write-Host "HELIOS Platform Service Manager" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

if ($Install) {
    Ensure-Administrator
    Install-HeliosService
}
elseif ($Uninstall) {
    Ensure-Administrator
    Uninstall-HeliosService
}
elseif ($Start) {
    Ensure-Administrator
    Start-HeliosService
}
elseif ($Stop) {
    Ensure-Administrator
    Stop-HeliosService
}
elseif ($Status) {
    Show-ServiceStatus
}
elseif ($AutoStart) {
    Ensure-Administrator
    Set-AutoStart
}
else {
    Write-Host "HELIOS Platform Service Manager" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\Manage-Service.ps1 -Install       # Install the service" -ForegroundColor White
    Write-Host "  .\Manage-Service.ps1 -Start         # Start the service" -ForegroundColor White
    Write-Host "  .\Manage-Service.ps1 -Stop          # Stop the service" -ForegroundColor White
    Write-Host "  .\Manage-Service.ps1 -Status        # Show service status" -ForegroundColor White
    Write-Host "  .\Manage-Service.ps1 -AutoStart     # Enable auto-start" -ForegroundColor White
    Write-Host "  .\Manage-Service.ps1 -Uninstall     # Uninstall the service" -ForegroundColor White
    Write-Host ""
    
    Show-ServiceStatus
}
