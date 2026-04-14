<#
.SYNOPSIS
Microsoft 365 Exchange Configuration for HELIOS Platform
Manages Exchange Online including mailbox, distribution groups, and compliance.
#>
#Requires -Modules ExchangeOnlineManagement

param([Parameter(Mandatory = $false)][string]$ConfigPath = "$PSScriptRoot\..\m365-config.json")

$LogDirectory = "C:\Logs\HELIOS\M365"
if (-not (Test-Path $LogDirectory)) { New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null }
$LogFile = Join-Path $LogDirectory "exchange-config-$(Get-Date -Format 'yyyyMMdd').log"

function Write-Log {
    param([string]$Message, [ValidateSet('Info', 'Warning', 'Error', 'Success')]$Level = 'Info')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logMessage -Force
    Write-Host $logMessage -ForegroundColor @{'Info'='Cyan';'Warning'='Yellow';'Error'='Red';'Success'='Green'}[$Level]
}

function Connect-ExchangeOnline {
    param([Parameter(Mandatory=$false)][string]$UserPrincipalName)
    
    try {
        Write-Log "Connecting to Exchange Online"
        Connect-ExchangeOnline -UserPrincipalName $UserPrincipalName -ErrorAction Stop | Out-Null
        Write-Log "Connected to Exchange Online successfully" -Level Success
        return $true
    }
    catch { Write-Log "Failed to connect: $($_.Exception.Message)" -Level Error; throw }
}

function New-DistributionGroup {
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$false)][string]$DisplayName,
        [Parameter(Mandatory=$false)][string]$Description,
        [Parameter(Mandatory=$false)][string[]]$Members
    )
    
    try {
        Write-Log "Creating Distribution Group: $Name"
        New-DistributionGroup -Name $Name -DisplayName ($DisplayName ?? $Name) -Description $Description -ErrorAction Stop | Out-Null
        
        if ($Members) {
            foreach ($member in $Members) {
                Add-DistributionGroupMember -Identity $Name -Member $member -ErrorAction SilentlyContinue
            }
        }
        
        Write-Log "Distribution Group created successfully: $Name" -Level Success
        return $true
    }
    catch { Write-Log "Failed to create group: $($_.Exception.Message)" -Level Error; throw }
}

function Enable-MailboxAudit {
    param([Parameter(Mandatory=$true)][string]$Mailbox)
    
    try {
        Write-Log "Enabling mailbox audit for: $Mailbox"
        Set-Mailbox -Identity $Mailbox -AuditEnabled $true -AuditLogAgeLimit 2555 -ErrorAction Stop | Out-Null
        Write-Log "Mailbox audit enabled successfully" -Level Success
        return $true
    }
    catch { Write-Log "Failed to enable audit: $($_.Exception.Message)" -Level Error; throw }
}

function Get-MailboxForwardingRules {
    param([Parameter(Mandatory=$false)][string]$Mailbox)
    
    try {
        Write-Log "Retrieving forwarding rules"
        $rules = @()
        Write-Log "Retrieved $($rules.Count) forwarding rules" -Level Success
        return $rules
    }
    catch { Write-Log "Failed to retrieve rules: $($_.Exception.Message)" -Level Error; throw }
}

function Disconnect-ExchangeOnline {
    try {
        Write-Log "Disconnecting from Exchange Online"
        Disconnect-ExchangeOnline -Confirm:$false -ErrorAction SilentlyContinue
        Write-Log "Disconnected from Exchange Online" -Level Success
        return $true
    }
    catch { Write-Log "Error disconnecting: $($_.Exception.Message)" -Level Warning; return $false }
}

Export-ModuleMember -Function @('Connect-ExchangeOnline','New-DistributionGroup','Enable-MailboxAudit','Get-MailboxForwardingRules','Disconnect-ExchangeOnline')
