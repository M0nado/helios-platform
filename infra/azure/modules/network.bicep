param location string
param namePrefix string
param environmentName string
@description('Optional reviewed platform VNet address space (/16). Leave empty to use environment defaults that avoid overlap across dev/test/prod.')
param platformAddressSpace string = ''

@allowed([
  'natGateway'
  'azureFirewall'
])
@description('Approved egress design. Azure Firewall requires a separately approved hub firewall and route target.')
param egressMode string = 'natGateway'

@description('Private IP of the approved hub Azure Firewall when egressMode is azureFirewall.')
param azureFirewallPrivateIp string = ''
param hubVirtualNetworkId string = ''

var vnetName = take(toLower('${namePrefix}-${environmentName}-platform-vnet'), 64)
var tags = {
  system: 'HELIOS'
  environment: environmentName
  managedBy: 'Bicep'
  'helios-managed': 'true'
}
var resolvedPlatformAddressSpace = empty(platformAddressSpace)
  ? (environmentName == 'prod'
      ? '10.44.0.0/16'
      : environmentName == 'test'
        ? '10.43.0.0/16'
        : '10.42.0.0/16')
  : platformAddressSpace
var addressPlans = {
  '10.42.0.0/16': {
    ingress: '10.42.0.0/24'
    apim: '10.42.1.0/24'
    containerApps: '10.42.2.0/23'
    functions: '10.42.4.0/24'
    runners: '10.42.5.0/24'
    privateEndpoints: '10.42.6.0/24'
    management: '10.42.7.0/24'
  }
  '10.43.0.0/16': {
    ingress: '10.43.0.0/24'
    apim: '10.43.1.0/24'
    containerApps: '10.43.2.0/23'
    functions: '10.43.4.0/24'
    runners: '10.43.5.0/24'
    privateEndpoints: '10.43.6.0/24'
    management: '10.43.7.0/24'
  }
  '10.44.0.0/16': {
    ingress: '10.44.0.0/24'
    apim: '10.44.1.0/24'
    containerApps: '10.44.2.0/23'
    functions: '10.44.4.0/24'
    runners: '10.44.5.0/24'
    privateEndpoints: '10.44.6.0/24'
    management: '10.44.7.0/24'
  }
}
var selectedAddressPlan = contains(addressPlans, resolvedPlatformAddressSpace)
  ? addressPlans[resolvedPlatformAddressSpace]
  : fail('platformAddressSpace must be one of 10.42.0.0/16, 10.43.0.0/16, or 10.44.0.0/16.')
var subnetDefinitions = [
  { name: 'ingress', prefix: selectedAddressPlan.ingress, delegation: '' }
  { name: 'apim', prefix: selectedAddressPlan.apim, delegation: '' }
  { name: 'container-apps', prefix: selectedAddressPlan.containerApps, delegation: 'Microsoft.App/environments' }
  { name: 'functions', prefix: selectedAddressPlan.functions, delegation: 'Microsoft.Web/serverFarms' }
  { name: 'vm-runners', prefix: selectedAddressPlan.runners, delegation: '' }
  { name: 'private-endpoints', prefix: selectedAddressPlan.privateEndpoints, delegation: '' }
  { name: 'management', prefix: selectedAddressPlan.management, delegation: '' }
]

resource natPublicIp 'Microsoft.Network/publicIPAddresses@2023-11-01' = if (egressMode == 'natGateway') {
  name: '${namePrefix}-${environmentName}-egress-pip'
  location: location
  tags: tags
  sku: { name: 'Standard' }
  properties: { publicIPAllocationMethod: 'Static' }
}

resource natGateway 'Microsoft.Network/natGateways@2023-11-01' = if (egressMode == 'natGateway') {
  name: '${namePrefix}-${environmentName}-nat'
  location: location
  tags: tags
  sku: { name: 'Standard' }
  properties: {
    idleTimeoutInMinutes: 10
    publicIpAddresses: [{ id: natPublicIp.id }]
  }
}

resource nsgs 'Microsoft.Network/networkSecurityGroups@2023-11-01' = [for subnet in subnetDefinitions: {
  name: '${namePrefix}-${environmentName}-${subnet.name}-nsg'
  location: location
  tags: tags
  properties: {
    securityRules: concat(subnet.name == 'ingress' ? [{
      name: 'Allow-AzureFrontDoor-HTTPS'
      properties: {
        priority: 100
        direction: 'Inbound'
        access: 'Allow'
        protocol: 'Tcp'
        sourcePortRange: '*'
        destinationPortRange: '443'
        sourceAddressPrefix: 'AzureFrontDoor.Backend'
        destinationAddressPrefix: '*'
      }
    }] : [], subnet.name == 'apim' ? [{
      name: 'Allow-APIM-Control-Plane'
      properties: {
        priority: 110
        direction: 'Inbound'
        access: 'Allow'
        protocol: 'Tcp'
        sourcePortRange: '*'
        destinationPortRange: '3443'
        sourceAddressPrefix: 'ApiManagement'
        destinationAddressPrefix: 'VirtualNetwork'
      }
    }] : [], [
      {
        name: 'Allow-VNet-Inbound'
        properties: {
          priority: 200
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
      {
        name: 'Deny-Internet-Inbound'
        properties: {
          priority: 4096
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
        }
      }
    ])
  }
}]

resource routeTables 'Microsoft.Network/routeTables@2023-11-01' = [for subnet in subnetDefinitions: {
  name: '${namePrefix}-${environmentName}-${subnet.name}-rt'
  location: location
  tags: tags
  properties: {
    disableBgpRoutePropagation: false
    routes: egressMode == 'azureFirewall' ? concat(subnet.name == 'apim' ? [{
      name: 'apim-control-plane-return'
      properties: {
        addressPrefix: 'ApiManagement'
        nextHopType: 'Internet'
      }
    }] : [], [{
      name: 'approved-egress-via-firewall'
      properties: {
        addressPrefix: '0.0.0.0/0'
        nextHopType: 'VirtualAppliance'
        nextHopIpAddress: azureFirewallPrivateIp
      }
    }]) : []
  }
}]

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: [resolvedPlatformAddressSpace] }
    subnets: [for (subnet, i) in subnetDefinitions: {
      name: subnet.name
      properties: union({
        addressPrefix: subnet.prefix
        networkSecurityGroup: { id: nsgs[i].id }
        routeTable: { id: routeTables[i].id }
        natGateway: egressMode == 'natGateway' ? { id: natGateway.id } : null
        privateEndpointNetworkPolicies: subnet.name == 'private-endpoints' ? 'Disabled' : 'Enabled'
      }, !empty(subnet.delegation) ? {
        delegations: [{
          name: '${subnet.name}-delegation'
          properties: { serviceName: subnet.delegation }
        }]
      } : {})
    }]
  }
}

resource hubVirtualNetwork 'Microsoft.Network/virtualNetworks@2023-11-01' existing = if (egressMode == 'azureFirewall') {
  name: last(split(hubVirtualNetworkId, '/'))
  scope: resourceGroup(split(hubVirtualNetworkId, '/')[2], split(hubVirtualNetworkId, '/')[4])
}

resource platformToHub 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-11-01' = if (egressMode == 'azureFirewall') {
  parent: virtualNetwork
  name: 'platform-to-firewall-hub'
  properties: {
    remoteVirtualNetwork: { id: hubVirtualNetwork.id }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
    useRemoteGateways: false
  }
}

output virtualNetworkName string = virtualNetwork.name
output virtualNetworkId string = virtualNetwork.id
output ingressSubnetId string = virtualNetwork.properties.subnets[0].id
output apimSubnetId string = virtualNetwork.properties.subnets[1].id
output containerAppsSubnetId string = virtualNetwork.properties.subnets[2].id
output functionsSubnetId string = virtualNetwork.properties.subnets[3].id
output runnerSubnetId string = virtualNetwork.properties.subnets[4].id
output privateEndpointSubnetId string = virtualNetwork.properties.subnets[5].id
output managementSubnetId string = virtualNetwork.properties.subnets[6].id
output platformAddressSpace string = resolvedPlatformAddressSpace
output egressDecision string = egressMode
