<#
.SYNOPSIS
Azure Resource Manager for HELIOS Platform

.DESCRIPTION
Manages Azure resources including:
- Resource groups creation and management
- Resource deployment and updates
- Resource tagging and organization
- Resource monitoring and cleanup
- ARM template deployment

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: Az.Resources module
#>

#Requires -Modules Az.Resources, Az.Accounts

param(
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\azure-config.json"
)

$LogDirectory = "C:\Logs\HELIOS\Azure"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "resource-mgmt-$(Get-Date -Format 'yyyyMMdd').log"

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

function Get-ConfigValue {
    param([string]$Key, [string]$DefaultValue = $null)
    
    if (Test-Path $ConfigPath) {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        return $config.$Key ?? $DefaultValue
    }
    return $DefaultValue
}

function New-ResourceGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Tags,
        
        [Parameter(Mandatory = $false)]
        [string]$Description
    )
    
    try {
        Write-Log "Creating Resource Group: $ResourceGroupName in $Location"
        
        $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName -Location $Location `
            -Tag $Tags -ErrorAction Stop
        
        Write-Log "Resource Group created successfully: $ResourceGroupName" -Level Success
        return $resourceGroup
    }
    catch {
        Write-Log "Failed to create Resource Group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-ResourceGroupDetails {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName
    )
    
    try {
        $rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction Stop
        $resources = Get-AzResource -ResourceGroupName $ResourceGroupName -ErrorAction Stop
        
        return @{
            Name              = $rg.ResourceGroupName
            Location          = $rg.Location
            Id                = $rg.ResourceId
            Tags              = $rg.Tags
            ResourceCount     = $resources.Count
            ProvisioningState = $rg.ProvisioningState
            Resources         = $resources
        }
    }
    catch {
        Write-Log "Failed to retrieve Resource Group details: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-ResourceGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    
    try {
        Write-Log "Removing Resource Group: $ResourceGroupName"
        
        if (-not $Force) {
            $resources = Get-AzResource -ResourceGroupName $ResourceGroupName
            if ($resources.Count -gt 0) {
                Write-Log "Resource Group contains $($resources.Count) resources. Use -Force to confirm deletion." -Level Warning
                return $false
            }
        }
        
        Remove-AzResourceGroup -Name $ResourceGroupName -Force:$Force -ErrorAction Stop
        
        Write-Log "Resource Group removed successfully: $ResourceGroupName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove Resource Group: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Update-ResourceGroupTags {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [hashtable]$Tags
    )
    
    try {
        Write-Log "Updating tags for Resource Group: $ResourceGroupName"
        
        $rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction Stop
        $newTags = $rg.Tags
        
        foreach ($key in $Tags.Keys) {
            $newTags[$key] = $Tags[$key]
        }
        
        $rg | Update-AzResourceGroup -Tag $newTags -ErrorAction Stop | Out-Null
        
        Write-Log "Tags updated successfully for Resource Group: $ResourceGroupName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to update tags: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Deploy-ArmTemplate {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$TemplateFile,
        
        [Parameter(Mandatory = $false)]
        [string]$ParametersFile,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$TemplateParameters,
        
        [Parameter(Mandatory = $false)]
        [string]$DeploymentName
    )
    
    try {
        if (-not $DeploymentName) {
            $DeploymentName = "deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"
        }
        
        Write-Log "Deploying ARM template to Resource Group: $ResourceGroupName"
        Write-Log "Template: $TemplateFile"
        
        $deployParams = @{
            ResourceGroupName       = $ResourceGroupName
            TemplateFile            = $TemplateFile
            Name                    = $DeploymentName
            ErrorAction             = 'Stop'
        }
        
        if ($ParametersFile) {
            $deployParams['TemplateParameterFile'] = $ParametersFile
        }
        
        if ($TemplateParameters) {
            $deployParams['TemplateParameterObject'] = $TemplateParameters
        }
        
        $deployment = New-AzResourceGroupDeployment @deployParams
        
        Write-Log "ARM template deployed successfully: $DeploymentName" -Level Success
        
        return @{
            DeploymentName    = $deployment.DeploymentName
            ResourceGroupName = $deployment.ResourceGroupName
            ProvisioningState = $deployment.ProvisioningState
            Timestamp         = $deployment.Timestamp
            Outputs           = $deployment.Outputs
        }
    }
    catch {
        Write-Log "Failed to deploy ARM template: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-ResourcesByType {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceType,
        
        [Parameter(Mandatory = $false)]
        [string]$ResourceGroupName
    )
    
    try {
        $getParams = @{
            ResourceType = $ResourceType
            ErrorAction  = 'Stop'
        }
        
        if ($ResourceGroupName) {
            $getParams['ResourceGroupName'] = $ResourceGroupName
        }
        
        $resources = Get-AzResource @getParams
        
        Write-Log "Retrieved $($resources.Count) resources of type: $ResourceType" -Level Success
        return $resources
    }
    catch {
        Write-Log "Failed to retrieve resources: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-Resource {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceId,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    
    try {
        Write-Log "Removing resource: $ResourceId"
        
        Remove-AzResource -ResourceId $ResourceId -Force:$Force -ErrorAction Stop
        
        Write-Log "Resource removed successfully: $ResourceId" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove resource: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-ResourceTagSummary {
    param(
        [Parameter(Mandatory = $false)]
        [string]$ResourceGroupName
    )
    
    try {
        $getParams = @{
            ErrorAction = 'Stop'
        }
        
        if ($ResourceGroupName) {
            $getParams['ResourceGroupName'] = $ResourceGroupName
        }
        
        $resources = Get-AzResource @getParams
        $tagSummary = @{}
        
        foreach ($resource in $resources) {
            if ($resource.Tags) {
                foreach ($key in $resource.Tags.Keys) {
                    if (-not $tagSummary[$key]) {
                        $tagSummary[$key] = @{}
                    }
                    $value = $resource.Tags[$key]
                    if (-not $tagSummary[$key][$value]) {
                        $tagSummary[$key][$value] = 0
                    }
                    $tagSummary[$key][$value]++
                }
            }
        }
        
        Write-Log "Retrieved tag summary for resources" -Level Success
        return $tagSummary
    }
    catch {
        Write-Log "Failed to retrieve tag summary: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Export-ResourceGroupTemplate {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )
    
    try {
        Write-Log "Exporting template for Resource Group: $ResourceGroupName"
        
        $export = Export-AzResourceGroup -ResourceGroupName $ResourceGroupName -Path $OutputPath `
            -Force -ErrorAction Stop
        
        Write-Log "Template exported successfully to: $OutputPath" -Level Success
        return $export
    }
    catch {
        Write-Log "Failed to export template: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-ResourceLocks {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName
    )
    
    try {
        $locks = Get-AzResourceLock -ResourceGroupName $ResourceGroupName -ErrorAction Stop
        
        Write-Log "Retrieved $($locks.Count) locks for Resource Group: $ResourceGroupName" -Level Success
        return $locks
    }
    catch {
        Write-Log "Failed to retrieve resource locks: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-ResourceLock {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $false)]
        [string]$ResourceId,
        
        [Parameter(Mandatory = $true)]
        [ValidateSet('CanNotDelete', 'ReadOnly')]
        [string]$LockLevel,
        
        [Parameter(Mandatory = $false)]
        [string]$LockNotes
    )
    
    try {
        Write-Log "Creating $LockLevel lock on resource"
        
        if ($ResourceId) {
            New-AzResourceLock -ResourceId $ResourceId -LockLevel $LockLevel `
                -LockNotes $LockNotes -Force -ErrorAction Stop | Out-Null
        }
        else {
            New-AzResourceLock -ResourceGroupName $ResourceGroupName -LockLevel $LockLevel `
                -LockNotes $LockNotes -Force -ErrorAction Stop | Out-Null
        }
        
        Write-Log "Lock created successfully" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to create lock: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-DeploymentHistory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $false)]
        [int]$MaxResults = 10
    )
    
    try {
        $deployments = Get-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
            -ErrorAction Stop | Sort-Object -Property Timestamp -Descending | Select-Object -First $MaxResults
        
        Write-Log "Retrieved $($deployments.Count) deployment(s) for Resource Group: $ResourceGroupName" -Level Success
        return $deployments
    }
    catch {
        Write-Log "Failed to retrieve deployment history: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-ResourceHealth {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName
    )
    
    try {
        $resources = Get-AzResource -ResourceGroupName $ResourceGroupName -ErrorAction Stop
        $healthReport = @()
        
        foreach ($resource in $resources) {
            $healthReport += @{
                Name               = $resource.Name
                Type               = $resource.Type
                ProvisioningState  = $resource.Properties.provisioningState ?? 'Unknown'
                Location           = $resource.Location
                Tags               = $resource.Tags
            }
        }
        
        Write-Log "Generated health report for $($healthReport.Count) resources" -Level Success
        return $healthReport
    }
    catch {
        Write-Log "Failed to generate health report: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'New-ResourceGroup',
    'Get-ResourceGroupDetails',
    'Remove-ResourceGroup',
    'Update-ResourceGroupTags',
    'Deploy-ArmTemplate',
    'Get-ResourcesByType',
    'Remove-Resource',
    'Get-ResourceTagSummary',
    'Export-ResourceGroupTemplate',
    'Get-ResourceLocks',
    'New-ResourceLock',
    'Get-DeploymentHistory',
    'Get-ResourceHealth'
)
