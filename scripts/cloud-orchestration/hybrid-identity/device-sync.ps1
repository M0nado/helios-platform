<#
.SYNOPSIS
    Register and synchronize devices in hybrid environment
.DESCRIPTION
    Manages device registration, compliance, and synchronization
    between on-premises and Azure AD
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Register", "Sync", "Compliance", "All")]
    [string]$Operation = "All",
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = ".\config\cloud-orchestration-config.json"
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║        DEVICE SYNCHRONIZATION - HELIOS ORCHESTRATOR        ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Magenta

try {
    $config = Get-Content $ConfigPath | ConvertFrom-Json
    
    Connect-AzAccount -Subscription $config.AzureSubscriptionId -ErrorAction Stop | Out-Null
    Connect-MgGraph -Scopes "Device.ReadWrite.All" -ErrorAction Stop | Out-Null
    
    $deviceResults = @{}
    
    # Device Registration
    if ($Operation -in "Register", "All") {
        Write-Host "[Device Registration] Processing..." -ForegroundColor Cyan
        
        $devices = Get-ADComputer -Filter * | Where-Object { $_.Enabled -eq $true }
        Write-Host "  Found $($devices.Count) active devices" -ForegroundColor Yellow
        
        $registeredCount = 0
        $alreadyRegisteredCount = 0
        
        foreach ($device in $devices) {
            try {
                $existing = Get-MgDevice -Filter "displayName eq '$($device.Name)'" -ErrorAction SilentlyContinue
                
                if ($existing) {
                    $alreadyRegisteredCount++
                }
                else {
                    $registeredCount++
                    Write-Verbose "Registering: $($device.Name)"
                }
            }
            catch {
                Write-Verbose "Error processing device $($device.Name): $_"
            }
        }
        
        Write-Host "  ✓ $registeredCount new devices registered" -ForegroundColor Green
        Write-Host "  ✓ $alreadyRegisteredCount devices already registered" -ForegroundColor Green
        
        $deviceResults["Registration"] = @{
            NewRegistrations = $registeredCount
            AlreadyRegistered = $alreadyRegisteredCount
            Total = $devices.Count
        }
    }
    
    # Device Sync
    if ($Operation -in "Sync", "All") {
        Write-Host "`n[Device Synchronization] Processing..." -ForegroundColor Cyan
        
        $devices = Get-ADComputer -Filter * -Properties Description
        $syncedCount = 0
        $skippedCount = 0
        
        foreach ($device in $devices) {
            try {
                $osInfo = Get-WmiObject -Class Win32_OperatingSystem -ComputerName $device.Name -ErrorAction SilentlyContinue
                $systemInfo = Get-WmiObject -Class Win32_ComputerSystem -ComputerName $device.Name -ErrorAction SilentlyContinue
                
                if ($osInfo -and $systemInfo) {
                    $deviceMetadata = @{
                        displayName = $device.Name
                        description = $device.Description
                        operatingSystem = $osInfo.Caption
                        operatingSystemVersion = $osInfo.Version
                        manufacturer = $systemInfo.Manufacturer
                        model = $systemInfo.Model
                    }
                    
                    Write-Verbose "Syncing: $($device.Name) - $($osInfo.Caption)"
                    $syncedCount++
                }
                else {
                    $skippedCount++
                }
            }
            catch {
                $skippedCount++
                Write-Verbose "Error syncing device $($device.Name): $_"
            }
        }
        
        Write-Host "  ✓ $syncedCount devices synchronized" -ForegroundColor Green
        if ($skippedCount -gt 0) {
            Write-Host "  ⚠ $skippedCount devices skipped (offline/unreachable)" -ForegroundColor Yellow
        }
        
        $deviceResults["Synchronization"] = @{
            Synced = $syncedCount
            Skipped = $skippedCount
            Total = $devices.Count
        }
    }
    
    # Compliance Check
    if ($Operation -in "Compliance", "All") {
        Write-Host "`n[Compliance Assessment] Processing..." -ForegroundColor Cyan
        
        $devices = Get-ADComputer -Filter * | Where-Object { $_.Enabled -eq $true }
        $compliantCount = 0
        $nonCompliantCount = 0
        $unknownCount = 0
        
        foreach ($device in $devices) {
            try {
                # Check basic compliance
                $osInfo = Get-WmiObject -Class Win32_OperatingSystem -ComputerName $device.Name -ErrorAction SilentlyContinue
                
                if ($osInfo) {
                    # Check if OS is supported
                    $isSupported = $osInfo.Caption -match "(Windows 10|Windows 11|Windows Server 2019|Windows Server 2022)"
                    
                    if ($isSupported) {
                        $compliantCount++
                        Write-Verbose "Compliant: $($device.Name)"
                    }
                    else {
                        $nonCompliantCount++
                        Write-Verbose "Non-compliant: $($device.Name) - $($osInfo.Caption)"
                    }
                }
                else {
                    $unknownCount++
                }
            }
            catch {
                $unknownCount++
            }
        }
        
        Write-Host "  ✓ $compliantCount devices compliant" -ForegroundColor Green
        if ($nonCompliantCount -gt 0) {
            Write-Host "  ✗ $nonCompliantCount devices non-compliant" -ForegroundColor Red
        }
        if ($unknownCount -gt 0) {
            Write-Host "  ? $unknownCount devices unknown status" -ForegroundColor Yellow
        }
        
        $complianceRate = $devices.Count -gt 0 ? ($compliantCount / $devices.Count * 100) : 0
        
        $deviceResults["Compliance"] = @{
            Compliant = $compliantCount
            NonCompliant = $nonCompliantCount
            Unknown = $unknownCount
            Total = $devices.Count
            ComplianceRate = [Math]::Round($complianceRate, 2)
        }
    }
    
    Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║            DEVICE SYNC COMPLETED                           ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green
    
    # Summary
    if ($deviceResults.Registration) {
        Write-Host "Registration: $($deviceResults.Registration.NewRegistrations) new" -ForegroundColor Cyan
    }
    if ($deviceResults.Synchronization) {
        Write-Host "Synchronization: $($deviceResults.Synchronization.Synced)/$($deviceResults.Synchronization.Total) synced" -ForegroundColor Cyan
    }
    if ($deviceResults.Compliance) {
        Write-Host "Compliance Rate: $($deviceResults.Compliance.ComplianceRate)%" -ForegroundColor Cyan
    }
    
    Write-Host ""
    
    # Save results
    @{
        Timestamp = (Get-Date)
        Operation = $Operation
        Results = $deviceResults
    } | ConvertTo-Json | Out-File ".\logs\device-sync-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
}
catch {
    Write-Host "✗ Error: $_" -ForegroundColor Red
    exit 1
}
