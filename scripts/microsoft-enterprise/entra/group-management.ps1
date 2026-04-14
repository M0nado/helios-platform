<#
.SYNOPSIS
Azure Entra Group Management for HELIOS Platform

.DESCRIPTION
Manages Entra groups including:
- Group creation and management
- Dynamic group rules
- Group membership management
- Group permissions
- Group lifecycle management
- Bulk group operations

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: AzureAD module
#>

#Requires -Modules AzureAD

param(
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\entra-config.json"
)

$LogDirectory = "C:\Logs\HELIOS\Entra"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "group-mgmt-$(Get-Date -Format 'yyyyMMdd').log"

function Write-Log {
    param([string]$Message, [ValidateSet('Info', 'Warning', 'Error', 'Success')]$Level = 'Info')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logMessage -Force
    
    $color = @{
        'Info'    = 'Cyan'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
        'Success' = 'Green'
    }
    Write-Host $logMessage -ForegroundColor $color[$Level]
}

function New-EntraGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $false)]
        [string]$MailNickname,
        
        [Parameter(Mandatory = $false)]
        [string]$Description,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Security', 'Office365')]
        [string]$GroupType = 'Security',
        
        [Parameter(Mandatory = $false)]
        [switch]$IsDynamic,
        
        [Parameter(Mandatory = $false)]
        [string]$MembershipRule
    )
    
    try {
        Write-Log "Creating Entra group: $DisplayName"
        
        if (-not $MailNickname) {
            $MailNickname = $DisplayName.Replace(' ', '').ToLower()
        }
        
        $groupParams = @{
            DisplayName     = $DisplayName
            MailNickname    = $MailNickname
            MailEnabled     = ($GroupType -eq 'Office365')
            SecurityEnabled = ($GroupType -eq 'Security')
            Description     = $Description
        }
        
        if ($IsDynamic -and $MembershipRule) {
            $groupParams['GroupTypes'] = 'DynamicMembership'
            $groupParams['MembershipRule'] = $MembershipRule
            $groupParams['MembershipRuleProcessingState'] = 'On'
        }
        
        $group = New-AzureADGroup @groupParams -ErrorAction Stop
        
        Write-Log "Entra group created successfully: $DisplayName" -Level Success
        return $group
    }
    catch {
        Write-Log "Failed to create Entra group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-EntraGroup {
    param(
        [Parameter(Mandatory = $false)]
        [string]$ObjectId,
        
        [Parameter(Mandatory = $false)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $false)]
        [switch]$All
    )
    
    try {
        if ($ObjectId) {
            Write-Log "Retrieving Entra group: $ObjectId"
            $group = Get-AzureADGroup -ObjectId $ObjectId -ErrorAction Stop
        }
        elseif ($DisplayName) {
            Write-Log "Retrieving Entra group: $DisplayName"
            $group = Get-AzureADGroup -Filter "displayName eq '$DisplayName'" -ErrorAction Stop
        }
        elseif ($All) {
            Write-Log "Retrieving all Entra groups"
            $group = Get-AzureADGroup -All $true -ErrorAction Stop
        }
        else {
            throw "Specify ObjectId, DisplayName, or -All flag"
        }
        
        Write-Log "Group(s) retrieved successfully" -Level Success
        return $group
    }
    catch {
        Write-Log "Failed to retrieve group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Add-EntraGroupMember {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupObjectId,
        
        [Parameter(Mandatory = $true)]
        [string]$UserObjectId
    )
    
    try {
        Write-Log "Adding member to Entra group: $GroupObjectId"
        
        Add-AzureADGroupMember -ObjectId $GroupObjectId -RefObjectId $UserObjectId -ErrorAction Stop
        
        Write-Log "Member added successfully to group" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to add member: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-EntraGroupMember {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupObjectId,
        
        [Parameter(Mandatory = $true)]
        [string]$UserObjectId
    )
    
    try {
        Write-Log "Removing member from Entra group"
        
        Remove-AzureADGroupMember -ObjectId $GroupObjectId -MemberId $UserObjectId -ErrorAction Stop
        
        Write-Log "Member removed successfully from group" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove member: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-EntraGroupMembers {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId,
        
        [Parameter(Mandatory = $false)]
        [switch]$All
    )
    
    try {
        Write-Log "Retrieving members for Entra group"
        
        $getParams = @{
            ObjectId = $ObjectId
            ErrorAction = 'Stop'
        }
        
        if ($All) {
            $getParams['All'] = $true
        }
        
        $members = Get-AzureADGroupMember @getParams
        
        Write-Log "Retrieved $($members.Count) members" -Level Success
        return $members
    }
    catch {
        Write-Log "Failed to retrieve group members: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Update-EntraGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId,
        
        [Parameter(Mandatory = $false)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $false)]
        [string]$Description
    )
    
    try {
        Write-Log "Updating Entra group: $ObjectId"
        
        $updateParams = @{
            ObjectId = $ObjectId
        }
        
        if ($DisplayName) { $updateParams['DisplayName'] = $DisplayName }
        if ($Description) { $updateParams['Description'] = $Description }
        
        Set-AzureADGroup @updateParams -ErrorAction Stop
        
        Write-Log "Entra group updated successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to update group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-EntraGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId
    )
    
    try {
        Write-Log "Removing Entra group: $ObjectId"
        
        Remove-AzureADGroup -ObjectId $ObjectId -ErrorAction Stop
        
        Write-Log "Entra group removed successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-DynamicGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $true)]
        [string]$MembershipRule,
        
        [Parameter(Mandatory = $false)]
        [string]$Description
    )
    
    try {
        Write-Log "Creating dynamic group: $DisplayName"
        
        $group = New-EntraGroup -DisplayName $DisplayName `
            -Description $Description `
            -GroupType 'Security' `
            -IsDynamic `
            -MembershipRule $MembershipRule
        
        Write-Log "Dynamic group created successfully" -Level Success
        return $group
    }
    catch {
        Write-Log "Failed to create dynamic group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-DynamicGroupMembers {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId
    )
    
    try {
        Write-Log "Retrieving dynamic group members"
        
        $members = Get-AzureADGroupMember -ObjectId $ObjectId -All $true -ErrorAction Stop
        
        Write-Log "Retrieved $($members.Count) dynamic group members" -Level Success
        return $members
    }
    catch {
        Write-Log "Failed to retrieve dynamic members: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Bulk-AddGroupMembers {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupObjectId,
        
        [Parameter(Mandatory = $true)]
        [string[]]$UserObjectIds
    )
    
    try {
        Write-Log "Bulk adding $($UserObjectIds.Count) members to group"
        
        $results = @()
        foreach ($userId in $UserObjectIds) {
            try {
                Add-AzureADGroupMember -ObjectId $GroupObjectId -RefObjectId $userId -ErrorAction Stop
                $results += @{ Status = 'Success'; UserId = $userId }
            }
            catch {
                $results += @{ Status = 'Failed'; UserId = $userId; Error = $_.Exception.Message }
            }
        }
        
        Write-Log "Bulk add completed. Success: $(($results | Where-Object Status -eq 'Success').Count)" -Level Success
        return $results
    }
    catch {
        Write-Log "Failed in bulk add: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'New-EntraGroup',
    'Get-EntraGroup',
    'Add-EntraGroupMember',
    'Remove-EntraGroupMember',
    'Get-EntraGroupMembers',
    'Update-EntraGroup',
    'Remove-EntraGroup',
    'New-DynamicGroup',
    'Get-DynamicGroupMembers',
    'Bulk-AddGroupMembers'
)
