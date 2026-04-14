# PowerShell: Deploy HELIOS to Azure
# Purpose: Automated Azure infrastructure and HELIOS deployment
# Version: 1.0.0

param(
    [string]$EnvironmentName = "production",
    [string]$ResourceGroupName = "helios-platform-prod",
    [string]$Location = "eastus2",
    [string[]]$Phases = @("All")
)

# Color codes for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Log-Action {
    param(
        [string]$Action,
        [string]$Status = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-ColorOutput "[$timestamp] [$Status] $Action" $(if ($Status -eq "ERROR") { $ErrorColor } elseif ($Status -eq "SUCCESS") { $SuccessColor } else { $InfoColor })
}

# Check prerequisites
Log-Action "Checking prerequisites..."
$modules = @("Az", "Az.Compute", "Az.Storage", "Az.Sql")
foreach ($module in $modules) {
    if (-not (Get-Module -ListAvailable -Name $module)) {
        Log-Action "Module $module not found. Installing..." "WARNING"
        Install-Module -Name $module -Force -AllowClobber
    }
}
Log-Action "Prerequisites check completed" "SUCCESS"

# Connect to Azure
Log-Action "Connecting to Azure..."
try {
    Connect-AzAccount | Out-Null
    Log-Action "Connected to Azure" "SUCCESS"
} catch {
    Log-Action "Failed to connect to Azure: $_" "ERROR"
    exit 1
}

# Create or get resource group
Log-Action "Creating/checking resource group: $ResourceGroupName"
try {
    $rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
    if ($null -eq $rg) {
        $rg = New-AzResourceGroup -Name $ResourceGroupName -Location $Location
        Log-Action "Resource group created" "SUCCESS"
    } else {
        Log-Action "Resource group already exists" "SUCCESS"
    }
} catch {
    Log-Action "Failed to create resource group: $_" "ERROR"
    exit 1
}

# Phase 1: Create VNet
if ($Phases -contains "All" -or $Phases -contains "Networking") {
    Log-Action "Phase 1: Creating Virtual Network..."
    try {
        $vnetName = "helios-vnet-$EnvironmentName"
        $vnet = Get-AzVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $vnetName -ErrorAction SilentlyContinue
        
        if ($null -eq $vnet) {
            $vnet = New-AzVirtualNetwork `
                -ResourceGroupName $ResourceGroupName `
                -Name $vnetName `
                -AddressPrefix "10.0.0.0/16" `
                -Location $Location
            
            Add-AzVirtualNetworkSubnetConfig `
                -Name "frontend-subnet" `
                -AddressPrefix "10.0.1.0/24" `
                -VirtualNetwork $vnet | Out-Null
            
            Add-AzVirtualNetworkSubnetConfig `
                -Name "backend-subnet" `
                -AddressPrefix "10.0.2.0/24" `
                -VirtualNetwork $vnet | Out-Null
            
            $vnet | Set-AzVirtualNetwork | Out-Null
            Log-Action "Virtual Network created with subnets" "SUCCESS"
        } else {
            Log-Action "Virtual Network already exists" "SUCCESS"
        }
    } catch {
        Log-Action "Failed to create VNet: $_" "ERROR"
    }
}

# Phase 2: Create Storage Account
if ($Phases -contains "All" -or $Phases -contains "Storage") {
    Log-Action "Phase 2: Creating Storage Account..."
    try {
        $storageAccountName = "heliosplatform$($EnvironmentName)sa"
        $storageAccount = Get-AzStorageAccount `
            -ResourceGroupName $ResourceGroupName `
            -Name $storageAccountName `
            -ErrorAction SilentlyContinue
        
        if ($null -eq $storageAccount) {
            $storageAccount = New-AzStorageAccount `
                -ResourceGroupName $ResourceGroupName `
                -Name $storageAccountName `
                -SkuName "Standard_GRS" `
                -Location $Location `
                -Kind "StorageV2"
            Log-Action "Storage Account created" "SUCCESS"
        } else {
            Log-Action "Storage Account already exists" "SUCCESS"
        }
    } catch {
        Log-Action "Failed to create Storage Account: $_" "ERROR"
    }
}

# Phase 3: Create Key Vault
if ($Phases -contains "All" -or $Phases -contains "Secrets") {
    Log-Action "Phase 3: Creating Key Vault..."
    try {
        $keyVaultName = "helios-vault-$EnvironmentName"
        $keyVault = Get-AzKeyVault -Name $keyVaultName -ResourceGroupName $ResourceGroupName -ErrorAction SilentlyContinue
        
        if ($null -eq $keyVault) {
            $keyVault = New-AzKeyVault `
                -VaultName $keyVaultName `
                -ResourceGroupName $ResourceGroupName `
                -Location $Location `
                -EnabledForDeployment `
                -EnabledForDiskEncryption `
                -EnabledForTemplateDeployment
            Log-Action "Key Vault created" "SUCCESS"
        } else {
            Log-Action "Key Vault already exists" "SUCCESS"
        }
    } catch {
        Log-Action "Failed to create Key Vault: $_" "ERROR"
    }
}

# Phase 4: Create SQL Database
if ($Phases -contains "All" -or $Phases -contains "Database") {
    Log-Action "Phase 4: Creating SQL Database..."
    try {
        $sqlServerName = "helios-sqlserver-$EnvironmentName"
        $sqlDbName = "helios-db-$EnvironmentName"
        
        $sqlServer = Get-AzSqlServer -ResourceGroupName $ResourceGroupName -ServerName $sqlServerName -ErrorAction SilentlyContinue
        
        if ($null -eq $sqlServer) {
            # Get SQL admin credentials securely
            $sqlAdminUser = Read-Host "Enter SQL Server admin username"
            $sqlAdminPassword = Read-Host "Enter SQL Server admin password" -AsSecureString
            $credentials = New-Object System.Management.Automation.PSCredential($sqlAdminUser, $sqlAdminPassword)
            
            $sqlServer = New-AzSqlServer `
                -ResourceGroupName $ResourceGroupName `
                -ServerName $sqlServerName `
                -Location $Location `
                -SqlAdministratorCredentials $credentials
            Log-Action "SQL Server created" "SUCCESS"
        } else {
            Log-Action "SQL Server already exists" "SUCCESS"
        }
        
        # Create database
        $db = Get-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $sqlServerName -DatabaseName $sqlDbName -ErrorAction SilentlyContinue
        
        if ($null -eq $db) {
            $db = New-AzSqlDatabase `
                -ResourceGroupName $ResourceGroupName `
                -ServerName $sqlServerName `
                -DatabaseName $sqlDbName `
                -Edition "Standard" `
                -Collation "SQL_Latin1_General_CP1_CI_AS"
            Log-Action "SQL Database created" "SUCCESS"
        } else {
            Log-Action "SQL Database already exists" "SUCCESS"
        }
    } catch {
        Log-Action "Failed to create SQL Database: $_" "ERROR"
    }
}

# Phase 5: Create VMs
if ($Phases -contains "All" -or $Phases -contains "Compute") {
    Log-Action "Phase 5: Creating Virtual Machines..."
    try {
        # This is a simplified example - real deployment would include
        # more sophisticated VM configuration, custom scripts, etc.
        
        $vmName = "helios-vm-01-$EnvironmentName"
        $vm = Get-AzVM -ResourceGroupName $ResourceGroupName -Name $vmName -ErrorAction SilentlyContinue
        
        if ($null -eq $vm) {
            Log-Action "VM creation would be done here ($vmName)" "INFO"
            Log-Action "For production, use ARM templates or Terraform" "WARNING"
        } else {
            Log-Action "VM already exists" "SUCCESS"
        }
    } catch {
        Log-Action "Failed to create VMs: $_" "ERROR"
    }
}

# Summary
Log-Action "`n=== Deployment Summary ===" "INFO"
Write-ColorOutput "Environment: $EnvironmentName" $InfoColor
Write-ColorOutput "Resource Group: $ResourceGroupName" $InfoColor
Write-ColorOutput "Location: $Location" $InfoColor
Write-ColorOutput "Phases Deployed: $($Phases -join ', ')" $InfoColor

Log-Action "Deployment completed successfully!" "SUCCESS"
Log-Action "Next steps:" "INFO"
Log-Action "1. Configure monitoring and alerting" "INFO"
Log-Action "2. Set up backups and disaster recovery" "INFO"
Log-Action "3. Deploy HELIOS application code" "INFO"
Log-Action "4. Configure autoscaling policies" "INFO"
