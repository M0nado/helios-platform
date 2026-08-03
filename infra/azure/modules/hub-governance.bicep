param namePrefix string
param environmentName string
param hubVirtualNetworkName string
param platformVirtualNetworkId string
param azureFirewallPolicyName string
param enabledEgressProfiles array

var networkPathPolicy = loadJsonContent('../../../monado/helios-control/config/network-paths.json')
var approvedDestinations = networkPathPolicy.egress.approvedDestinations
var selectedProfileRules = [for profile in enabledEgressProfiles: {
  name: 'Allow-${profile}'
  ruleType: 'ApplicationRule'
  protocols: [{ protocolType: 'Https', port: 443 }]
  sourceAddresses: ['10.42.0.0/16']
  targetFqdns: approvedDestinations[profile]
}]

resource hubVirtualNetwork 'Microsoft.Network/virtualNetworks@2023-11-01' existing = {
  name: hubVirtualNetworkName
}

resource hubToPlatform 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-11-01' = {
  parent: hubVirtualNetwork
  name: '${namePrefix}-${environmentName}-hub-to-platform'
  properties: {
    remoteVirtualNetwork: { id: platformVirtualNetworkId }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
    useRemoteGateways: false
  }
}

resource firewallPolicy 'Microsoft.Network/firewallPolicies@2023-11-01' existing = {
  name: azureFirewallPolicyName
}

resource heliosRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-11-01' = {
  parent: firewallPolicy
  name: '${namePrefix}-${environmentName}-egress'
  properties: {
    priority: 300
    ruleCollections: [{
      name: 'enabled-integration-profiles'
      priority: 100
      ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
      action: { type: 'Allow' }
      rules: selectedProfileRules
    }]
  }
}
