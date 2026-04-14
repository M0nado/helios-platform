<#
.SYNOPSIS
Azure Entra User Management for HELIOS Platform

.DESCRIPTION
Manages Entra users including:
- User creation and provisioning
- User profile management
- User lifecycle management
- Bulk user operations
- User attributes management
- License provisioning

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: AzureAD, MSOnline modules
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
$LogFile = Join-Path $LogDirectory "user-mgmt-$(Get-Date -Format 'yyyyMMdd').log"

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

function New-EntraUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UserPrincipalName,
        
        [Parameter(Mandatory = $true)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $false)]
        [string]$FirstName,
        
        [Parameter(Mandatory = $false)]
        [string]$LastName,
        
        [Parameter(Mandatory = $false)]
        [string]$Department,
        
        [Parameter(Mandatory = $false)]
        [string]$JobTitle,
        
        [Parameter(Mandatory = $false)]
        [string]$MobilePhone,
        
        [Parameter(Mandatory = $false)]
        [string]$OfficeLocation,
        
        [Parameter(Mandatory = $false)]
        [securestring]$Password
    )
    
    try {
        Write-Log "Creating Entra user: $UserPrincipalName"
        
        $passwordProfile = New-Object Microsoft.Open.AzureAD.Model.PasswordProfile
        $passwordProfile.Password = if ($Password) { $Password } else { (New-Guid).ToString() + "!@#" }
        $passwordProfile.EnforceChangePasswordPolicy = $true
        $passwordProfile.ForceChangePasswordNextLogin = $true
        
        $user = New-AzureADUser -AccountEnabled $true `
            -DisplayName $DisplayName `
            -UserPrincipalName $UserPrincipalName `
            -PasswordProfile $passwordProfile `
            -GivenName $FirstName `
            -Surname $LastName `
            -Department $Department `
            -JobTitle $JobTitle `
            -Mobile $MobilePhone `
            -OfficeLocation $OfficeLocation `
            -ErrorAction Stop
        
        Write-Log "Entra user created successfully: $UserPrincipalName" -Level Success
        return $user
    }
    catch {
        Write-Log "Failed to create Entra user: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-EntraUser {
    param(
        [Parameter(Mandatory = $false)]
        [string]$UserPrincipalName,
        
        [Parameter(Mandatory = $false)]
        [string]$ObjectId,
        
        [Parameter(Mandatory = $false)]
        [switch]$All
    )
    
    try {
        if ($UserPrincipalName) {
            Write-Log "Retrieving Entra user: $UserPrincipalName"
            $user = Get-AzureADUser -Filter "userPrincipalName eq '$UserPrincipalName'" -ErrorAction Stop
        }
        elseif ($ObjectId) {
            Write-Log "Retrieving Entra user: $ObjectId"
            $user = Get-AzureADUser -ObjectId $ObjectId -ErrorAction Stop
        }
        elseif ($All) {
            Write-Log "Retrieving all Entra users"
            $user = Get-AzureADUser -All $true -ErrorAction Stop
        }
        else {
            throw "Specify UserPrincipalName, ObjectId, or -All flag"
        }
        
        Write-Log "User(s) retrieved successfully" -Level Success
        return $user
    }
    catch {
        Write-Log "Failed to retrieve user: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Update-EntraUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId,
        
        [Parameter(Mandatory = $false)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $false)]
        [string]$Department,
        
        [Parameter(Mandatory = $false)]
        [string]$JobTitle,
        
        [Parameter(Mandatory = $false)]
        [string]$MobilePhone
    )
    
    try {
        Write-Log "Updating Entra user: $ObjectId"
        
        $updateParams = @{}
        if ($DisplayName) { $updateParams['DisplayName'] = $DisplayName }
        if ($Department) { $updateParams['Department'] = $Department }
        if ($JobTitle) { $updateParams['JobTitle'] = $JobTitle }
        if ($MobilePhone) { $updateParams['Mobile'] = $MobilePhone }
        
        Set-AzureADUser -ObjectId $ObjectId @updateParams -ErrorAction Stop
        
        Write-Log "Entra user updated successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to update Entra user: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-EntraUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId
    )
    
    try {
        Write-Log "Removing Entra user: $ObjectId"
        
        Remove-AzureADUser -ObjectId $ObjectId -ErrorAction Stop
        
        Write-Log "Entra user removed successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove Entra user: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Enable-EntraUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId
    )
    
    try {
        Write-Log "Enabling Entra user: $ObjectId"
        
        Set-AzureADUser -ObjectId $ObjectId -AccountEnabled $true -ErrorAction Stop
        
        Write-Log "Entra user enabled successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to enable Entra user: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Disable-EntraUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId
    )
    
    try {
        Write-Log "Disabling Entra user: $ObjectId"
        
        Set-AzureADUser -ObjectId $ObjectId -AccountEnabled $false -ErrorAction Stop
        
        Write-Log "Entra user disabled successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to disable Entra user: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Reset-EntraUserPassword {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId,
        
        [Parameter(Mandatory = $false)]
        [switch]$ForceChangeNextLogin
    )
    
    try {
        Write-Log "Resetting password for Entra user: $ObjectId"
        
        $tempPassword = [System.Web.Security.Membership]::GeneratePassword(16, 3)
        $passwordProfile = New-Object Microsoft.Open.AzureAD.Model.PasswordProfile
        $passwordProfile.Password = $tempPassword
        $passwordProfile.ForceChangePasswordNextLogin = $ForceChangeNextLogin
        
        Set-AzureADUser -ObjectId $ObjectId -PasswordProfile $passwordProfile -ErrorAction Stop
        
        Write-Log "Password reset successfully for Entra user" -Level Success
        return @{
            ObjectId                = $ObjectId
            TemporaryPassword       = $tempPassword
            ForceChangeNextLogin    = $ForceChangeNextLogin
        }
    }
    catch {
        Write-Log "Failed to reset password: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-EntraUserGroups {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ObjectId
    )
    
    try {
        Write-Log "Retrieving groups for Entra user: $ObjectId"
        
        $groups = Get-AzureADUserMembership -ObjectId $ObjectId -ErrorAction Stop
        
        Write-Log "Retrieved $($groups.Count) groups for user" -Level Success
        return $groups
    }
    catch {
        Write-Log "Failed to retrieve user groups: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Bulk-ImportEntraUsers {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsvPath
    )
    
    try {
        if (-not (Test-Path $CsvPath)) {
            throw "CSV file not found: $CsvPath"
        }
        
        Write-Log "Bulk importing Entra users from: $CsvPath"
        
        $users = Import-Csv -Path $CsvPath -ErrorAction Stop
        $results = @()
        
        foreach ($user in $users) {
            try {
                $newUser = New-EntraUser -UserPrincipalName $user.UserPrincipalName `
                    -DisplayName $user.DisplayName `
                    -FirstName $user.FirstName `
                    -LastName $user.LastName `
                    -Department $user.Department `
                    -JobTitle $user.JobTitle `
                    -MobilePhone $user.MobilePhone
                
                $results += @{
                    Status  = 'Success'
                    User    = $user.UserPrincipalName
                    ObjectId = $newUser.ObjectId
                }
            }
            catch {
                $results += @{
                    Status = 'Failed'
                    User   = $user.UserPrincipalName
                    Error  = $_.Exception.Message
                }
            }
        }
        
        Write-Log "Bulk import completed. Success: $(($results | Where-Object Status -eq 'Success').Count), Failed: $(($results | Where-Object Status -eq 'Failed').Count)" -Level Success
        return $results
    }
    catch {
        Write-Log "Failed to bulk import users: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'New-EntraUser',
    'Get-EntraUser',
    'Update-EntraUser',
    'Remove-EntraUser',
    'Enable-EntraUser',
    'Disable-EntraUser',
    'Reset-EntraUserPassword',
    'Get-EntraUserGroups',
    'Bulk-ImportEntraUsers'
)
