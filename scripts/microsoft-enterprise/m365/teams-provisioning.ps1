<#
.SYNOPSIS
Microsoft 365 Teams Provisioning for HELIOS Platform

.DESCRIPTION
Manages Microsoft Teams provisioning including:
- Team creation and configuration
- Team settings management
- Channel management
- Member management
- Team templates
- Team governance

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: MicrosoftTeams module
#>

#Requires -Modules MicrosoftTeams

param(
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\m365-config.json"
)

$LogDirectory = "C:\Logs\HELIOS\M365"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "teams-provisioning-$(Get-Date -Format 'yyyyMMdd').log"

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

function Connect-MicrosoftTeams {
    try {
        Write-Log "Connecting to Microsoft Teams"
        
        Connect-MicrosoftTeams -ErrorAction Stop | Out-Null
        
        Write-Log "Connected to Microsoft Teams successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to connect to Teams: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-Team {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TeamName,
        
        [Parameter(Mandatory = $false)]
        [string]$TeamDescription,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Private', 'Public')]
        [string]$Visibility = 'Private',
        
        [Parameter(Mandatory = $false)]
        [string[]]$Owners,
        
        [Parameter(Mandatory = $false)]
        [string[]]$Members
    )
    
    try {
        Write-Log "Creating Team: $TeamName"
        
        $teamParams = @{
            DisplayName = $TeamName
            Visibility  = $Visibility
        }
        
        if ($TeamDescription) {
            $teamParams['Description'] = $TeamDescription
        }
        
        $team = New-Team @teamParams -ErrorAction Stop
        
        if ($Owners) {
            foreach ($owner in $Owners) {
                Add-TeamUser -GroupId $team.GroupId -User $owner -Role Owner -ErrorAction SilentlyContinue
            }
        }
        
        if ($Members) {
            foreach ($member in $Members) {
                Add-TeamUser -GroupId $team.GroupId -User $member -Role Member -ErrorAction SilentlyContinue
            }
        }
        
        Write-Log "Team created successfully: $TeamName" -Level Success
        return $team
    }
    catch {
        Write-Log "Failed to create Team: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-Team {
    param(
        [Parameter(Mandatory = $false)]
        [string]$TeamName,
        
        [Parameter(Mandatory = $false)]
        [string]$GroupId,
        
        [Parameter(Mandatory = $false)]
        [switch]$All
    )
    
    try {
        if ($TeamName) {
            Write-Log "Retrieving Team: $TeamName"
            $team = Get-Team -DisplayName $TeamName -ErrorAction Stop
        }
        elseif ($GroupId) {
            Write-Log "Retrieving Team: $GroupId"
            $team = Get-Team -GroupId $GroupId -ErrorAction Stop
        }
        elseif ($All) {
            Write-Log "Retrieving all Teams"
            $team = Get-Team -ErrorAction Stop
        }
        else {
            throw "Specify TeamName, GroupId, or -All flag"
        }
        
        Write-Log "Team(s) retrieved successfully" -Level Success
        return $team
    }
    catch {
        Write-Log "Failed to retrieve Team: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-TeamChannel {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupId,
        
        [Parameter(Mandatory = $true)]
        [string]$ChannelName,
        
        [Parameter(Mandatory = $false)]
        [string]$ChannelDescription,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Standard', 'Private')]
        [string]$ChannelType = 'Standard'
    )
    
    try {
        Write-Log "Creating Channel: $ChannelName in Team: $GroupId"
        
        $channelParams = @{
            GroupId = $GroupId
            DisplayName = $ChannelName
        }
        
        if ($ChannelDescription) {
            $channelParams['Description'] = $ChannelDescription
        }
        
        $channel = New-TeamChannel @channelParams -ErrorAction Stop
        
        Write-Log "Channel created successfully: $ChannelName" -Level Success
        return $channel
    }
    catch {
        Write-Log "Failed to create Channel: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Add-TeamMember {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupId,
        
        [Parameter(Mandatory = $true)]
        [string]$User,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Owner', 'Member')]
        [string]$Role = 'Member'
    )
    
    try {
        Write-Log "Adding $Role to Team: $GroupId (User: $User)"
        
        Add-TeamUser -GroupId $GroupId -User $User -Role $Role -ErrorAction Stop
        
        Write-Log "Member added successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to add member: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-TeamMembers {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupId
    )
    
    try {
        Write-Log "Retrieving members for Team: $GroupId"
        
        $members = Get-TeamUser -GroupId $GroupId -ErrorAction Stop
        
        Write-Log "Retrieved $($members.Count) members" -Level Success
        return $members
    }
    catch {
        Write-Log "Failed to retrieve members: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-TeamMember {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupId,
        
        [Parameter(Mandatory = $true)]
        [string]$User
    )
    
    try {
        Write-Log "Removing member from Team: $GroupId (User: $User)"
        
        Remove-TeamUser -GroupId $GroupId -User $User -ErrorAction Stop
        
        Write-Log "Member removed successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove member: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Update-Team {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupId,
        
        [Parameter(Mandatory = $false)]
        [string]$DisplayName,
        
        [Parameter(Mandatory = $false)]
        [string]$Description
    )
    
    try {
        Write-Log "Updating Team: $GroupId"
        
        $updateParams = @{
            GroupId = $GroupId
        }
        
        if ($DisplayName) { $updateParams['DisplayName'] = $DisplayName }
        if ($Description) { $updateParams['Description'] = $Description }
        
        Set-Team @updateParams -ErrorAction Stop
        
        Write-Log "Team updated successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to update Team: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-Team {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GroupId
    )
    
    try {
        Write-Log "Removing Team: $GroupId"
        
        Remove-Team -GroupId $GroupId -ErrorAction Stop
        
        Write-Log "Team removed successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove Team: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Disconnect-MicrosoftTeams {
    try {
        Write-Log "Disconnecting from Microsoft Teams"
        Disconnect-MicrosoftTeams -ErrorAction SilentlyContinue
        Write-Log "Disconnected from Teams" -Level Success
        return $true
    }
    catch {
        Write-Log "Error disconnecting: $($_.Exception.Message)" -Level Warning
        return $false
    }
}

Export-ModuleMember -Function @(
    'Connect-MicrosoftTeams',
    'New-Team',
    'Get-Team',
    'New-TeamChannel',
    'Add-TeamMember',
    'Get-TeamMembers',
    'Remove-TeamMember',
    'Update-Team',
    'Remove-Team',
    'Disconnect-MicrosoftTeams'
)
