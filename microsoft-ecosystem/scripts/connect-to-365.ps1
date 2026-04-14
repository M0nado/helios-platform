# PowerShell: Connect to Microsoft 365
# Purpose: Authenticate to Microsoft 365 services
# Version: 1.0.0

param(
    [string]$TenantName = "company.onmicrosoft.com"
)

$ErrorColor = "Red"
$SuccessColor = "Green"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# Check and install Microsoft Graph module
Write-ColorOutput "Checking for Microsoft Graph module..." $InfoColor
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph)) {
    Write-ColorOutput "Installing Microsoft Graph module..." $WarningColor
    Install-Module -Name Microsoft.Graph -Force -AllowClobber
}

# Check and install ExchangeOnlineManagement module
Write-ColorOutput "Checking for ExchangeOnlineManagement module..." $InfoColor
if (-not (Get-Module -ListAvailable -Name ExchangeOnlineManagement)) {
    Write-ColorOutput "Installing ExchangeOnlineManagement module..." $WarningColor
    Install-Module -Name ExchangeOnlineManagement -Force -AllowClobber
}

# Connect to Microsoft Graph
Write-ColorOutput "`nConnecting to Microsoft Graph..." $InfoColor
try {
    Connect-MgGraph -Scopes `
        "User.ReadWrite.All", `
        "Group.ReadWrite.All", `
        "Mail.Send", `
        "Directory.ReadWrite.All"
    Write-ColorOutput "Connected to Microsoft Graph." $SuccessColor
} catch {
    Write-ColorOutput "Failed to connect to Microsoft Graph: $_" $ErrorColor
    exit 1
}

# Connect to Exchange Online
Write-ColorOutput "Connecting to Exchange Online..." $InfoColor
try {
    Connect-ExchangeOnline -ManagedIdentity
    Write-ColorOutput "Connected to Exchange Online." $SuccessColor
} catch {
    Write-ColorOutput "Failed to connect to Exchange Online: $_" $ErrorColor
    exit 1
}

# Display current context
Write-ColorOutput "`nCurrent Context:" $InfoColor
Get-MgContext | Format-List Account, TenantId, Environment

# Test connectivity - Get users
Write-ColorOutput "`nTesting connectivity..." $InfoColor
try {
    $users = Get-MgUser -Top 5
    Write-ColorOutput "Found $($users.Count) users (showing first 5)" $SuccessColor
    $users | Select-Object DisplayName, Mail | Format-Table
} catch {
    Write-ColorOutput "Failed to list users: $_" $ErrorColor
    exit 1
}

# Test connectivity - Get groups
Write-ColorOutput "`nListing groups..." $InfoColor
try {
    $groups = Get-MgGroup -Top 5
    Write-ColorOutput "Found $($groups.Count) groups (showing first 5)" $SuccessColor
    $groups | Select-Object DisplayName, Id | Format-Table
} catch {
    Write-ColorOutput "Failed to list groups: $_" $ErrorColor
    exit 1
}

Write-ColorOutput "`n✓ Microsoft 365 connection established successfully!" $SuccessColor

# Show next steps
Write-ColorOutput "`nNext steps:" $InfoColor
Write-ColorOutput "- Create Teams: New-MgTeam"
Write-ColorOutput "- Create Groups: New-MgGroup"
Write-ColorOutput "- Manage Users: Get-MgUser, New-MgUser, Update-MgUser"
Write-ColorOutput "- Send Email: Send-ExoMail or Send-MgMail"
