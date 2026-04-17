<#
.SYNOPSIS
Azure Network Setup and Management for HELIOS Platform

.DESCRIPTION
Manages Azure networking including:
- Virtual Network (VNet) creation and configuration
- Subnet management
- Network Security Groups (NSG) configuration
- Public IP management
- Load balancer setup
- VPN and ExpressRoute configuration

.NOTES
Author: HELIOS Platform
Version: 1.0.0
Requires: Az.Network module
#>

#Requires -Modules Az.Network

param(
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "$PSScriptRoot\..\azure-config.json"
)

$LogDirectory = "C:\Logs\HELIOS\Azure"
if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$LogFile = Join-Path $LogDirectory "network-setup-$(Get-Date -Format 'yyyyMMdd').log"

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

function New-VirtualNetwork {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VNetName,
        
        [Parameter(Mandatory = $true)]
        [string]$AddressSpace,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Tags
    )
    
    try {
        Write-Log "Creating Virtual Network: $VNetName with address space: $AddressSpace"
        
        $vnet = New-AzVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $VNetName `
            -AddressPrefix $AddressSpace -Location $Location -ErrorAction Stop
        
        if ($Tags) {
            $vnet | Update-AzTag -Tag $Tags -Operation Merge | Out-Null
        }
        
        Write-Log "Virtual Network created successfully: $VNetName" -Level Success
        return $vnet
    }
    catch {
        Write-Log "Failed to create Virtual Network: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-Subnet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VNetName,
        
        [Parameter(Mandatory = $true)]
        [string]$SubnetName,
        
        [Parameter(Mandatory = $true)]
        [string]$AddressPrefix
    )
    
    try {
        Write-Log "Creating Subnet: $SubnetName with address prefix: $AddressPrefix"
        
        $vnet = Get-AzVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $VNetName -ErrorAction Stop
        
        Add-AzVirtualNetworkSubnetConfig -VirtualNetwork $vnet -Name $SubnetName `
            -AddressPrefix $AddressPrefix -ErrorAction Stop | Out-Null
        
        $vnet | Set-AzVirtualNetwork -ErrorAction Stop | Out-Null
        
        Write-Log "Subnet created successfully: $SubnetName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to create Subnet: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-NetworkSecurityGroup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$NSGName,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Tags
    )
    
    try {
        Write-Log "Creating Network Security Group: $NSGName"
        
        $nsg = New-AzNetworkSecurityGroup -ResourceGroupName $ResourceGroupName -Name $NSGName `
            -Location $Location -ErrorAction Stop
        
        if ($Tags) {
            $nsg | Update-AzTag -Tag $Tags -Operation Merge | Out-Null
        }
        
        Write-Log "Network Security Group created successfully: $NSGName" -Level Success
        return $nsg
    }
    catch {
        Write-Log "Failed to create NSG: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Add-NSGRule {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$NSGName,
        
        [Parameter(Mandatory = $true)]
        [string]$RuleName,
        
        [Parameter(Mandatory = $true)]
        [ValidateSet('Allow', 'Deny')]
        [string]$Access,
        
        [Parameter(Mandatory = $true)]
        [ValidateSet('Inbound', 'Outbound')]
        [string]$Direction,
        
        [Parameter(Mandatory = $true)]
        [ValidateSet('TCP', 'UDP', '*')]
        [string]$Protocol,
        
        [Parameter(Mandatory = $true)]
        [string]$SourceAddressPrefix,
        
        [Parameter(Mandatory = $true)]
        [string]$DestinationAddressPrefix,
        
        [Parameter(Mandatory = $false)]
        [string]$SourcePortRange = '*',
        
        [Parameter(Mandatory = $false)]
        [string]$DestinationPortRange,
        
        [Parameter(Mandatory = $false)]
        [int]$Priority = 100
    )
    
    try {
        Write-Log "Adding NSG Rule: $RuleName to $NSGName"
        
        $nsg = Get-AzNetworkSecurityGroup -ResourceGroupName $ResourceGroupName -Name $NSGName `
            -ErrorAction Stop
        
        Add-AzNetworkSecurityRuleConfig -NetworkSecurityGroup $nsg -Name $RuleName `
            -Access $Access -Direction $Direction -Protocol $Protocol `
            -SourceAddressPrefix $SourceAddressPrefix -DestinationAddressPrefix $DestinationAddressPrefix `
            -SourcePortRange $SourcePortRange -DestinationPortRange $DestinationPortRange `
            -Priority $Priority -ErrorAction Stop | Out-Null
        
        $nsg | Set-AzNetworkSecurityGroup -ErrorAction Stop | Out-Null
        
        Write-Log "NSG Rule added successfully: $RuleName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to add NSG rule: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Remove-NSGRule {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$NSGName,
        
        [Parameter(Mandatory = $true)]
        [string]$RuleName
    )
    
    try {
        Write-Log "Removing NSG Rule: $RuleName from $NSGName"
        
        $nsg = Get-AzNetworkSecurityGroup -ResourceGroupName $ResourceGroupName -Name $NSGName `
            -ErrorAction Stop
        
        Remove-AzNetworkSecurityRuleConfig -NetworkSecurityGroup $nsg -Name $RuleName `
            -ErrorAction Stop | Out-Null
        
        $nsg | Set-AzNetworkSecurityGroup -ErrorAction Stop | Out-Null
        
        Write-Log "NSG Rule removed successfully: $RuleName" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to remove NSG rule: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-PublicIP {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$PublicIPName,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Static', 'Dynamic')]
        [string]$AllocationMethod = 'Static',
        
        [Parameter(Mandatory = $false)]
        [string]$DomainNameLabel,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Tags
    )
    
    try {
        Write-Log "Creating Public IP: $PublicIPName"
        
        $publicIP = New-AzPublicIpAddress -ResourceGroupName $ResourceGroupName `
            -Name $PublicIPName -Location $Location -AllocationMethod $AllocationMethod `
            -ErrorAction Stop
        
        if ($DomainNameLabel) {
            $publicIP.DnsSettings = New-Object Microsoft.Azure.Commands.Network.Models.PSPublicIpAddressDnsSettings
            $publicIP.DnsSettings.DomainNameLabel = $DomainNameLabel
            $publicIP | Set-AzPublicIpAddress -ErrorAction Stop | Out-Null
        }
        
        if ($Tags) {
            $publicIP | Update-AzTag -Tag $Tags -Operation Merge | Out-Null
        }
        
        Write-Log "Public IP created successfully: $PublicIPName" -Level Success
        return $publicIP
    }
    catch {
        Write-Log "Failed to create Public IP: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-NetworkInterface {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$NICName,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $true)]
        [string]$VNetName,
        
        [Parameter(Mandatory = $true)]
        [string]$SubnetName,
        
        [Parameter(Mandatory = $false)]
        [string]$PublicIPName,
        
        [Parameter(Mandatory = $false)]
        [string]$NSGName
    )
    
    try {
        Write-Log "Creating Network Interface: $NICName"
        
        $vnet = Get-AzVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $VNetName `
            -ErrorAction Stop
        $subnet = Get-AzVirtualNetworkSubnetConfig -VirtualNetwork $vnet -Name $SubnetName `
            -ErrorAction Stop
        
        $nicConfig = New-AzNetworkInterfaceIpConfig -Name "ipconfig1" -Subnet $subnet -Primary `
            -ErrorAction Stop
        
        $nicParams = @{
            ResourceGroupName = $ResourceGroupName
            Name              = $NICName
            Location          = $Location
            IpConfiguration   = $nicConfig
            ErrorAction       = 'Stop'
        }
        
        if ($PublicIPName) {
            $publicIP = Get-AzPublicIpAddress -ResourceGroupName $ResourceGroupName `
                -Name $PublicIPName -ErrorAction Stop
            $nicConfig.PublicIpAddress = $publicIP
        }
        
        $nic = New-AzNetworkInterface @nicParams
        
        if ($NSGName) {
            $nsg = Get-AzNetworkSecurityGroup -ResourceGroupName $ResourceGroupName `
                -Name $NSGName -ErrorAction Stop
            $nic.NetworkSecurityGroup = $nsg
            $nic | Set-AzNetworkInterface -ErrorAction Stop | Out-Null
        }
        
        Write-Log "Network Interface created successfully: $NICName" -Level Success
        return $nic
    }
    catch {
        Write-Log "Failed to create Network Interface: $($_.Exception.Message)" -Level Error
        throw
    }
}

function New-LoadBalancer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$LBName,
        
        [Parameter(Mandatory = $true)]
        [string]$Location,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Public', 'Internal')]
        [string]$Type = 'Public',
        
        [Parameter(Mandatory = $false)]
        [string]$PublicIPName
    )
    
    try {
        Write-Log "Creating Load Balancer: $LBName (Type: $Type)"
        
        if ($Type -eq 'Public' -and $PublicIPName) {
            $publicIP = Get-AzPublicIpAddress -ResourceGroupName $ResourceGroupName `
                -Name $PublicIPName -ErrorAction Stop
            $frontendConfig = New-AzLoadBalancerFrontendIpConfig -Name "frontend" `
                -PublicIpAddress $publicIP -ErrorAction Stop
        }
        else {
            $frontendConfig = New-AzLoadBalancerFrontendIpConfig -Name "frontend" `
                -PrivateIpAddress "10.0.1.10" -ErrorAction Stop
        }
        
        $backendConfig = New-AzLoadBalancerBackendAddressPoolConfig -Name "backend" `
            -ErrorAction Stop
        
        $lb = New-AzLoadBalancer -ResourceGroupName $ResourceGroupName -Name $LBName `
            -Location $Location -FrontendIpConfiguration $frontendConfig `
            -BackendAddressPool $backendConfig -ErrorAction Stop
        
        Write-Log "Load Balancer created successfully: $LBName" -Level Success
        return $lb
    }
    catch {
        Write-Log "Failed to create Load Balancer: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-VNetDetails {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VNetName
    )
    
    try {
        Write-Log "Retrieving VNet details: $VNetName"
        
        $vnet = Get-AzVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $VNetName `
            -ErrorAction Stop
        
        return @{
            Name           = $vnet.Name
            ResourceGroup  = $ResourceGroupName
            Location       = $vnet.Location
            AddressSpaces  = $vnet.AddressSpace.AddressPrefixes
            Subnets        = $vnet.Subnets | Select-Object -Property Name, AddressPrefix
            DnsServers     = $vnet.DhcpOptions.DnsServers
            Id             = $vnet.Id
        }
    }
    catch {
        Write-Log "Failed to get VNet details: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Get-NSGRules {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$NSGName
    )
    
    try {
        Write-Log "Retrieving NSG rules for: $NSGName"
        
        $nsg = Get-AzNetworkSecurityGroup -ResourceGroupName $ResourceGroupName `
            -Name $NSGName -ErrorAction Stop
        
        return $nsg.SecurityRules
    }
    catch {
        Write-Log "Failed to get NSG rules: $($_.Exception.Message)" -Level Error
        throw
    }
}

function Associate-NSGToSubnet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResourceGroupName,
        
        [Parameter(Mandatory = $true)]
        [string]$VNetName,
        
        [Parameter(Mandatory = $true)]
        [string]$SubnetName,
        
        [Parameter(Mandatory = $true)]
        [string]$NSGName
    )
    
    try {
        Write-Log "Associating NSG $NSGName to Subnet $SubnetName"
        
        $vnet = Get-AzVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $VNetName `
            -ErrorAction Stop
        $nsg = Get-AzNetworkSecurityGroup -ResourceGroupName $ResourceGroupName `
            -Name $NSGName -ErrorAction Stop
        
        $subnet = Get-AzVirtualNetworkSubnetConfig -VirtualNetwork $vnet -Name $SubnetName `
            -ErrorAction Stop
        
        $subnet.NetworkSecurityGroup = $nsg
        $vnet | Set-AzVirtualNetwork -ErrorAction Stop | Out-Null
        
        Write-Log "NSG associated successfully to Subnet" -Level Success
        return $true
    }
    catch {
        Write-Log "Failed to associate NSG: $($_.Exception.Message)" -Level Error
        throw
    }
}

Export-ModuleMember -Function @(
    'New-VirtualNetwork',
    'New-Subnet',
    'New-NetworkSecurityGroup',
    'Add-NSGRule',
    'Remove-NSGRule',
    'New-PublicIP',
    'New-NetworkInterface',
    'New-LoadBalancer',
    'Get-VNetDetails',
    'Get-NSGRules',
    'Associate-NSGToSubnet'
)
