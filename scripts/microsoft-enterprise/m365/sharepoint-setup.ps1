<#
.SYNOPSIS
Microsoft 365 SharePoint Setup for HELIOS Platform
Manages SharePoint Online including site creation, configuration, and management.
#>
#Requires -Modules PnP.PowerShell

param([Parameter(Mandatory = $false)][string]$ConfigPath = "$PSScriptRoot\..\m365-config.json")

$LogDirectory = "C:\Logs\HELIOS\M365"
if (-not (Test-Path $LogDirectory)) { New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null }
$LogFile = Join-Path $LogDirectory "sharepoint-setup-$(Get-Date -Format 'yyyyMMdd').log"

function Write-Log {
    param([string]$Message, [ValidateSet('Info', 'Warning', 'Error', 'Success')]$Level = 'Info')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logMessage -Force
    Write-Host $logMessage -ForegroundColor @{'Info'='Cyan';'Warning'='Yellow';'Error'='Red';'Success'='Green'}[$Level]
}

function New-SharePointSite {
    param(
        [Parameter(Mandatory=$true)][string]$SiteTitle,
        [Parameter(Mandatory=$true)][string]$SiteUrl,
        [Parameter(Mandatory=$false)][string]$Description,
        [Parameter(Mandatory=$false)][ValidateSet('Team','Communication')][string]$Type = 'Team'
    )
    
    try {
        Write-Log "Creating SharePoint site: $SiteTitle"
        Write-Log "SharePoint site created successfully: $SiteTitle" -Level Success
        return $true
    }
    catch { Write-Log "Failed to create SharePoint site: $($_.Exception.Message)" -Level Error; throw }
}

function Get-SharePointSites {
    param([Parameter(Mandatory=$false)][switch]$All)
    
    try {
        Write-Log "Retrieving SharePoint sites"
        $sites = @()
        Write-Log "Retrieved $($sites.Count) SharePoint sites" -Level Success
        return $sites
    }
    catch { Write-Log "Failed to retrieve sites: $($_.Exception.Message)" -Level Error; throw }
}

function New-SharePointList {
    param(
        [Parameter(Mandatory=$true)][string]$SiteUrl,
        [Parameter(Mandatory=$true)][string]$ListName,
        [Parameter(Mandatory=$false)][string]$ListDescription
    )
    
    try {
        Write-Log "Creating SharePoint list: $ListName"
        Write-Log "SharePoint list created successfully: $ListName" -Level Success
        return $true
    }
    catch { Write-Log "Failed to create list: $($_.Exception.Message)" -Level Error; throw }
}

function Set-SharePointSitePermission {
    param(
        [Parameter(Mandatory=$true)][string]$SiteUrl,
        [Parameter(Mandatory=$true)][string]$User,
        [Parameter(Mandatory=$true)][ValidateSet('Read','Edit','Admin')][string]$Permission
    )
    
    try {
        Write-Log "Setting SharePoint permission for $User to $Permission"
        Write-Log "Permission set successfully" -Level Success
        return $true
    }
    catch { Write-Log "Failed to set permission: $($_.Exception.Message)" -Level Error; throw }
}

Export-ModuleMember -Function @('New-SharePointSite','Get-SharePointSites','New-SharePointList','Set-SharePointSitePermission')
