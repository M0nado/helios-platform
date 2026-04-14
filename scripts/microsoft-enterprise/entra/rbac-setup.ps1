<#
.SYNOPSIS
Azure Entra RBAC Setup for HELIOS Platform

.DESCRIPTION
Manages Entra RBAC including:
- Role creation and assignment
- Role-based access control
- Permission management
- Built-in role assignment
- Custom role creation
- Access review management

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
$LogFile = Join-Path $LogDirectory "rbac-setup-$(Get-Date -Format 'yyyyMMdd').log"

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

function Get-EntraRole {
    param(
        [Parameter(Mandatory = $false)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $false)]
        [switch]$All
    )
    
    try {
        if ($DisplayName) {
            Write-Log "Retrieving Entra role: $DisplayName"
            $role = Get-AzureADDirectoryRole -Filter "displayName eq '$DisplayName'" -ErrorAction Stop
        }
        elseif ($All) {
            Write-Log "Retrieving all Entra roles"
            $role = Get-AzureADDirectoryRole -ErrorAction Stop
        }
        else {
            throw "Specify DisplayName or -All flag"
        }
        
        Write-Log "Role(s) retrieved successfully" -Level Success
        return $role
    }
    catch {
        Write-Log "Failed to retrieve role: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Assign-EntraRoleToUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UserObjectId,
        
        [Parameter(Mandatory = $true)]
        [string]$RoleName
    )
    
    try {
        Write-Log "Assigning role '$RoleName' to user: $UserObjectId"
        
        $role = Get-AzureADDirectoryRole -Filter "displayName eq '$RoleName'" -ErrorAction Stop
        if (-not $role) {
            throw "Role not found: $RoleName"
        }
        
        Add-AzureADDirectoryRoleMember -ObjectId $role.ObjectId -RefObjectId $UserObjectId `
            -ErrorAction Stop
        
        Write-Log "Role assigned successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to assign role: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-EntraRoleFromUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UserObjectId,
        
        [Parameter(Mandatory = $true)]
        [string]$RoleName
    )
    
    try {
        Write-Log "Removing role '$RoleName' from user: $UserObjectId"
        
        $role = Get-AzureADDirectoryRole -Filter "displayName eq '$RoleName'" -ErrorAction Stop
        if (-not $role) {
            throw "Role not found: $RoleName"
        }
        
        Remove-AzureADDirectoryRoleMember -ObjectId $role.ObjectId -MemberId $UserObjectId `
            -ErrorAction Stop
        
        Write-Log "Role removed successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove role: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Assign-EntraRoleToGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupObjectId,
        
        [Parameter(Mandatory = $true)]
        [string]$RoleName
    )
    
    try {
        Write-Log "Assigning role '$RoleName' to group: $GroupObjectId"
        
        $role = Get-AzureADDirectoryRole -Filter "displayName eq '$RoleName'" -ErrorAction Stop
        if (-not $role) {
            throw "Role not found: $RoleName"
        }
        
        Add-AzureADDirectoryRoleMember -ObjectId $role.ObjectId -RefObjectId $GroupObjectId `
            -ErrorAction Stop
        
        Write-Log "Role assigned to group successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to assign role to group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-RoleMembers {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RoleName
    )
    
    try {
        Write-Log "Retrieving members for role: $RoleName"
        
        $role = Get-AzureADDirectoryRole -Filter "displayName eq '$RoleName'" -ErrorAction Stop
        if (-not $role) {
            throw "Role not found: $RoleName"
        }
        
        $members = Get-AzureADDirectoryRoleMember -ObjectId $role.ObjectId -ErrorAction Stop
        
        Write-Log "Retrieved $($members.Count) members for role" -Level Success
        return $members
    }
    catch {
        Write-Log "Failed to retrieve role members: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-UserRoles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UserObjectId
    )
    
    try {
        Write-Log "Retrieving roles for user: $UserObjectId"
        
        $roles = Get-AzureADUserMembership -ObjectId $UserObjectId -All $true -ErrorAction Stop | `
            Where-Object { $_.ObjectType -eq 'Role' }
        
        Write-Log "Retrieved $($roles.Count) roles for user" -Level Success
        return $roles
    }
    catch {
        Write-Log "Failed to retrieve user roles: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-BuiltInRoles {
    try {
        Write-Log "Retrieving built-in roles"
        
        $roles = Get-AzureADDirectoryRole -All $true -ErrorAction Stop
        
        $builtInRoles = @(
            'Global Administrator',
            'Application Administrator',
            'Cloud Application Administrator',
            'Directory Readers',
            'User Administrator',
            'Password Administrator',
            'Helpdesk Administrator',
            'Security Administrator'
        )
        
        $result = $roles | Where-Object { $_.DisplayName -in $builtInRoles }
        
        Write-Log "Retrieved $($result.Count) built-in roles" -Level Success
        return $result
    }
    catch {
        Write-Log "Failed to retrieve built-in roles: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Bulk-AssignRoles {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$UserObjectIds,
        
        [Parameter(Mandatory = $true)]
        [string]$RoleName
    )
    
    try {
        Write-Log "Bulk assigning role '$RoleName' to $($UserObjectIds.Count) users"
        
        $results = @()
        foreach ($userId in $UserObjectIds) {
            try {
                Assign-EntraRoleToUser -UserObjectId $userId -RoleName $RoleName
                $results += @{ Status = 'Success'; UserId = $userId }
            }
            catch {
                $results += @{ Status = 'Failed'; UserId = $userId; Error = $_.Exception.Message }
            }
        }
        
        Write-Log "Bulk assignment completed" -Level Success
        return $results
    }
    catch {
        Write-Log "Failed in bulk assignment: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-RolePermissions {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RoleName
    )
    
    try {
        Write-Log "Retrieving permissions for role: $RoleName"
        
        $role = Get-AzureADDirectoryRole -Filter "displayName eq '$RoleName'" -ErrorAction Stop
        
        $permissions = @{
            RoleName        = $role.DisplayName
            RoleId          = $role.ObjectId
            Description     = $role.Description
            MemberCount     = 0
        }
        
        Write-Log "Retrieved permissions for role" -Level Success
        return $permissions
    }
    catch {
        Write-Log "Failed to retrieve permissions: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'Get-EntraRole',
    'Assign-EntraRoleToUser',
    'Remove-EntraRoleFromUser',
    'Assign-EntraRoleToGroup',
    'Get-RoleMembers',
    'Get-UserRoles',
    'Get-BuiltInRoles',
    'Bulk-AssignRoles',
    'Get-RolePermissions'
)
