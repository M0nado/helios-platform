<#
.SYNOPSIS
Azure Authentication and Connection Management for HELIOS Platform

.DESCRIPTION
Provides comprehensive authentication mechanisms for Azure, including:
- Service Principal authentication
- Interactive user authentication
- Managed Identity authentication
- Token refresh and management
- Connection caching and validation

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: Az.Accounts module
#>

#Requires -Modules Az.Accounts

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('ServicePrincipal', 'Interactive', 'ManagedIdentity', 'Certificate')]
    [string]$AuthType = 'Interactive',
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$TenantId,
    
    [Parameter(Mandatory = $false)]
    [securestring]$ClientSecret,
    
    [Parameter(Mandatory = $false)]
    [string]$ClientId,
    
    [Parameter(Mandatory = $false)]
    [string]$CertificatePath,
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\azure-config.json"
)

# Initialize logging
$LogDirectory = "C:\Logs\HELIOS\Azure"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "auth-$(Get-Date -Format 'yyyyMMdd').log"

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
    param(
        [string]$Key,
        [string]$DefaultValue = $null
    )
    
    if (Test-Path $ConfigPath) {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        return $config.$Key ?? $DefaultValue
    }
    return $DefaultValue
}

function Connect-AzureServicePrincipal {
    param(
        [string]$TenantId,
        [string]$ClientId,
        [securestring]$ClientSecret,
        [string]$SubscriptionId
    )
    
    try {
        Write-Log "Attempting Service Principal authentication..."
        
        $credential = New-Object System.Management.Automation.PSCredential($ClientId, $ClientSecret)
        Connect-AzAccount -ServicePrincipal -Credential $credential -Tenant $TenantId -ErrorAction Stop | Out-Null
        
        if ($SubscriptionId) {
            Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
        }
        
        $context = Get-AzContext
        Write-Log "Service Principal authentication successful. Account: $($context.Account.Id)" -Level Success
        
        return @{
            Success         = $true
            Context         = $context
            AuthType        = 'ServicePrincipal'
            Timestamp       = Get-Date
            TenantId        = $context.Tenant.Id
            SubscriptionId  = $context.Subscription.Id
        }
    }
    catch {
        Write-Log "Service Principal authentication failed: $($_.Exception.Message)" -Level Error
        return @{
            Success  = $false
            Error    = $_.Exception.Message
            AuthType = 'ServicePrincipal'
        }
    }
}

function Connect-AzureInteractive {
    param(
        [string]$SubscriptionId,
        [string]$TenantId
    )
    
    try {
        Write-Log "Initiating interactive authentication..."
        
        $connectParams = @{}
        if ($TenantId) { $connectParams['Tenant'] = $TenantId }
        
        Connect-AzAccount @connectParams -ErrorAction Stop | Out-Null
        
        if ($SubscriptionId) {
            Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
        }
        
        $context = Get-AzContext
        Write-Log "Interactive authentication successful. Account: $($context.Account.Id)" -Level Success
        
        return @{
            Success         = $true
            Context         = $context
            AuthType        = 'Interactive'
            Timestamp       = Get-Date
            TenantId        = $context.Tenant.Id
            SubscriptionId  = $context.Subscription.Id
        }
    }
    catch {
        Write-Log "Interactive authentication failed: $($_.Exception.Message)" -Level Error
        return @{
            Success  = $false
            Error    = $_.Exception.Message
            AuthType = 'Interactive'
        }
    }
}

function Connect-AzureManagedIdentity {
    param(
        [string]$SubscriptionId
    )
    
    try {
        Write-Log "Attempting Managed Identity authentication..."
        
        Connect-AzAccount -Identity -ErrorAction Stop | Out-Null
        
        if ($SubscriptionId) {
            Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
        }
        
        $context = Get-AzContext
        Write-Log "Managed Identity authentication successful. Account: $($context.Account.Id)" -Level Success
        
        return @{
            Success         = $true
            Context         = $context
            AuthType        = 'ManagedIdentity'
            Timestamp       = Get-Date
            TenantId        = $context.Tenant.Id
            SubscriptionId  = $context.Subscription.Id
        }
    }
    catch {
        Write-Log "Managed Identity authentication failed: $($_.Exception.Message)" -Level Error
        return @{
            Success  = $false
            Error    = $_.Exception.Message
            AuthType = 'ManagedIdentity'
        }
    }
}

function Connect-AzureCertificate {
    param(
        [string]$TenantId,
        [string]$ClientId,
        [string]$CertificatePath,
        [string]$SubscriptionId
    )
    
    try {
        Write-Log "Attempting Certificate-based authentication..."
        
        if (-not (Test-Path $CertificatePath)) {
            throw "Certificate not found at path: $CertificatePath"
        }
        
        Connect-AzAccount -ServicePrincipal -CertificateThumbprint (Get-Item $CertificatePath).Thumbprint `
            -ApplicationId $ClientId -Tenant $TenantId -ErrorAction Stop | Out-Null
        
        if ($SubscriptionId) {
            Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
        }
        
        $context = Get-AzContext
        Write-Log "Certificate authentication successful. Account: $($context.Account.Id)" -Level Success
        
        return @{
            Success         = $true
            Context         = $context
            AuthType        = 'Certificate'
            Timestamp       = Get-Date
            TenantId        = $context.Tenant.Id
            SubscriptionId  = $context.Subscription.Id
        }
    }
    catch {
        Write-Log "Certificate authentication failed: $($_.Exception.Message)" -Level Error
        return @{
            Success  = $false
            Error    = $_.Exception.Message
            AuthType = 'Certificate'
        }
    }
}

function Test-AzureConnection {
    try {
        $context = Get-AzContext -ErrorAction Stop
        if ($null -eq $context) {
            return $false
        }
        
        Get-AzResourceGroup -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Get-AzureAccessToken {
    param(
        [string]$Resource = 'https://management.azure.com/'
    )
    
    try {
        $context = Get-AzContext -ErrorAction Stop
        $token = (Get-AzAccessToken -ResourceUrl $Resource -ErrorAction Stop).Token
        
        Write-Log "Successfully retrieved access token for resource: $Resource" -Level Success
        return $token
    }
    catch {
        Write-Log "Failed to retrieve access token: $($_.Exception.Message)" -Level Error
        return $null
    }
}

function Disconnect-Azure {
    try {
        Write-Log "Disconnecting from Azure..."
        Disconnect-AzAccount -ErrorAction SilentlyContinue | Out-Null
        Write-Log "Disconnected from Azure" -Level Success
        return $true
    }
    catch {
        Write-Log "Error disconnecting from Azure: $($_.Exception.Message)" -Level Warning
        return $false
    }
}

function Get-AzureConnectionInfo {
    try {
        $context = Get-AzContext -ErrorAction Stop
        
        return @{
            Account      = $context.Account.Id
            Environment  = $context.Environment.Name
            TenantId     = $context.Tenant.Id
            SubscriptionId = $context.Subscription.Id
            SubscriptionName = $context.Subscription.Name
            Connected    = $true
        }
    }
    catch {
        return @{
            Connected = $false
            Error     = $_.Exception.Message
        }
    }
}

function Initialize-AzureAuth {
    param(
        [string]$AuthType = 'Interactive',
        [string]$SubscriptionId = $null,
        [string]$TenantId = $null
    )
    
    try {
        Write-Log "============================================"
        Write-Log "Azure Authentication Initialization"
        Write-Log "============================================"
        Write-Log "Authentication Type: $AuthType"
        
        $result = switch ($AuthType) {
            'ServicePrincipal' {
                if (-not $ClientId -or -not $ClientSecret -or -not $TenantId) {
                    throw "Service Principal authentication requires ClientId, ClientSecret, and TenantId"
                }
                Connect-AzureServicePrincipal -TenantId $TenantId -ClientId $ClientId `
                    -ClientSecret $ClientSecret -SubscriptionId $SubscriptionId
            }
            'ManagedIdentity' {
                Connect-AzureManagedIdentity -SubscriptionId $SubscriptionId
            }
            'Certificate' {
                if (-not $ClientId -or -not $CertificatePath -or -not $TenantId) {
                    throw "Certificate authentication requires ClientId, CertificatePath, and TenantId"
                }
                Connect-AzureCertificate -TenantId $TenantId -ClientId $ClientId `
                    -CertificatePath $CertificatePath -SubscriptionId $SubscriptionId
            }
            default {
                Connect-AzureInteractive -SubscriptionId $SubscriptionId -TenantId $TenantId
            }
        }
        
        if ($result.Success) {
            Write-Log "============================================" -Level Success
            Write-Log "Authentication Summary:"
            Write-Log "Account: $($result.Context.Account.Id)" -Level Success
            Write-Log "Tenant: $($result.TenantId)" -Level Success
            Write-Log "Subscription: $($result.SubscriptionId)" -Level Success
            Write-Log "============================================" -Level Success
        }
        
        return $result
    }
    catch {
        Write-Log "Authentication initialization failed: $($_.Exception.Message)" -Level Error
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Main execution
if ($PSCmdlet.ParameterSetName -eq $null -or $AuthType) {
    $result = Initialize-AzureAuth -AuthType $AuthType -SubscriptionId $SubscriptionId -TenantId $TenantId
    $result
}

Export-ModuleMember -Function @(
    'Connect-AzureServicePrincipal',
    'Connect-AzureInteractive',
    'Connect-AzureManagedIdentity',
    'Connect-AzureCertificate',
    'Test-AzureConnection',
    'Get-AzureAccessToken',
    'Disconnect-Azure',
    'Get-AzureConnectionInfo',
    'Initialize-AzureAuth',
    'Write-Log'
)
