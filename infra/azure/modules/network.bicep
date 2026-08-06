param location string
param namePrefix string
param environmentName string

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
var subnetDefinitions = [
  { name: 'ingress', prefix: '10.42.0.0/24', delegation: '' }
  { name: 'apim', prefix: '10.42.1.0/24', delegation: '' }
  { name: 'container-apps', prefix: '10.42.2.0/23', delegation: 'Microsoft.App/environments' }
  { name: 'functions', prefix: '10.42.4.0/24', delegation: 'Microsoft.Web/serverFarms' }
  { name: 'vm-runners', prefix: '10.42.5.0/24', delegation: '' }
  { name: 'private-endpoints', prefix: '10.42.6.0/24', delegation: '' }
  { name: 'management', prefix: '10.42.7.0/24', delegation: '' }
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
    addressSpace: { addressPrefixes: ['10.42.0.0/16'] }
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
output egressDecision string = egressMode
