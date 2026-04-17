# PowerShell: Connect to Azure
# Purpose: Authenticate to Azure and set up environment
# Version: 1.0.0

param(
    [string]$EnvironmentName = "AzureCloud",
    [string]$SubscriptionName = "Production"
)

# Color codes for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Check if Az module is installed
Write-ColorOutput "Checking for Az module..." $InfoColor
if (-not (Get-Module -ListAvailable -Name Az)) {
    Write-ColorOutput "Az module not found. Installing..." $WarningColor
    Install-Module -Name Az -Force -AllowClobber
} else {
    Write-ColorOutput "Az module found." $SuccessColor
}

# Import Azure module
try {
    Import-Module Az
    Write-ColorOutput "Azure module imported successfully." $SuccessColor
} catch {
    Write-ColorOutput "Failed to import Azure module: $_" $ErrorColor
    exit 1
}

# Connect to Azure
Write-ColorOutput "Connecting to Azure ($EnvironmentName)..." $InfoColor
try {
    $context = Connect-AzAccount -Environment $EnvironmentName
    Write-ColorOutput "Connected successfully." $SuccessColor
} catch {
    Write-ColorOutput "Failed to connect to Azure: $_" $ErrorColor
    exit 1
}

# Get subscription
Write-ColorOutput "Getting subscription '$SubscriptionName'..." $InfoColor
try {
    $subscription = Get-AzSubscription -SubscriptionName $SubscriptionName
    if ($null -eq $subscription) {
        Write-ColorOutput "Subscription '$SubscriptionName' not found." $ErrorColor
        Write-ColorOutput "Available subscriptions:" $WarningColor
        Get-AzSubscription | Select-Object Name, Id | Format-Table
        exit 1
    }
    Set-AzContext -SubscriptionId $subscription.Id
    Write-ColorOutput "Subscription set to: $($subscription.Name)" $SuccessColor
} catch {
    Write-ColorOutput "Failed to set subscription: $_" $ErrorColor
    exit 1
}

# Set environment variables
Write-ColorOutput "Setting environment variables..." $InfoColor
$context = Get-AzContext
$env:HELIOS_AZURE_SUBSCRIPTION_ID = $subscription.Id
$env:HELIOS_AZURE_TENANT_ID = $context.Tenant.Id
$env:HELIOS_ENVIRONMENT = "production"
$env:HELIOS_LOCATION = "eastus2"
Write-ColorOutput "Environment variables set." $SuccessColor

# Display current context
Write-ColorOutput "`nCurrent Azure Context:" $InfoColor
Get-AzContext | Format-List Account, SubscriptionName, SubscriptionId, TenantId

# Test connectivity
Write-ColorOutput "`nTesting connectivity..." $InfoColor
try {
    $resourceGroups = Get-AzResourceGroup | Measure-Object
    Write-ColorOutput "Found $($resourceGroups.Count) resource groups" $SuccessColor
} catch {
    Write-ColorOutput "Failed to list resource groups: $_" $ErrorColor
    exit 1
}

Write-ColorOutput "`n✓ Azure connection established successfully!" $SuccessColor
