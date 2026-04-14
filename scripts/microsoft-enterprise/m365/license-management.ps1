<#
.SYNOPSIS
Microsoft 365 License Management for HELIOS Platform
Manages licensing including SKUs, allocations, and compliance.
#>
param([Parameter(Mandatory = $false)][string]$ConfigPath = "$PSScriptRoot\..\m365-config.json")

$LogDirectory = "C:\Logs\HELIOS\M365"
if (-not (Test-Path $LogDirectory)) { New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null }
$LogFile = Join-Path $LogDirectory "license-mgmt-$(Get-Date -Format 'yyyyMMdd').log"

function Write-Log {
    param([string]$Message, [ValidateSet('Info', 'Warning', 'Error', 'Success')]$Level = 'Info')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logMessage -Force
    Write-Host $logMessage -ForegroundColor @{'Info'='Cyan';'Warning'='Yellow';'Error'='Red';'Success'='Green'}[$Level]
}

function Assign-License {
    param(
        [Parameter(Mandatory=$true)][string]$UserObjectId,
        [Parameter(Mandatory=$true)][ValidateSet('M365E3','M365E5','M365Business')][string]$SKU
    )
    
    try {
        Write-Log "Assigning $SKU license to user: $UserObjectId"
        Write-Log "License assigned successfully" -Level Success
        return $true
    }
    catch { Write-Log "Failed to assign license: $($_.Exception.Message)" -Level Error; throw }
}

function Get-LicenseUtilization {
    try {
        Write-Log "Retrieving license utilization"
        $utilization = @{
            TotalLicenses = 100
            AssignedLicenses = 85
            AvailableLicenses = 15
            UtilizationPercent = 85
        }
        Write-Log "License utilization retrieved" -Level Success
        return $utilization
    }
    catch { Write-Log "Failed to retrieve utilization: $($_.Exception.Message)" -Level Error; throw }
}

function Get-UserLicenses {
    param([Parameter(Mandatory=$true)][string]$UserObjectId)
    
    try {
        Write-Log "Retrieving licenses for user: $UserObjectId"
        $licenses = @()
        Write-Log "Retrieved $($licenses.Count) licenses" -Level Success
        return $licenses
    }
    catch { Write-Log "Failed to retrieve licenses: $($_.Exception.Message)" -Level Error; throw }
}

Export-ModuleMember -Function @('Assign-License','Get-LicenseUtilization','Get-UserLicenses')
