param namePrefix string
param environmentName string
param hubVirtualNetworkName string
param platformVirtualNetworkId string
param azureFirewallPolicyName string
param enabledEgressProfiles array
param connectorRelayDestinations array

var networkPathPolicy = loadJsonContent('../../../monado/helios-control/config/network-paths.json')
var approvedDestinations = networkPathPolicy.egress.approvedDestinations
var selectedProfileRules = [for profile in enabledEgressProfiles: {
  name: 'Allow-${profile}'
  ruleType: 'ApplicationRule'
  protocols: [{ protocolType: 'Https', port: 443 }]
  sourceAddresses: ['10.42.0.0/16']
  targetFqdns: approvedDestinations[profile]
}]
var invalidRelayDestinations = filter(connectorRelayDestinations, destination => !contains(destination, 'profile') || !contains(destination, 'fqdn') || !contains(enabledEgressProfiles, destination.?profile ?? '') || empty(destination.?fqdn ?? '') || contains(destination.?fqdn ?? '', '://') || contains(destination.?fqdn ?? '', '/') || contains(destination.?fqdn ?? '', ':') || startsWith(destination.?fqdn ?? '', '.') || endsWith(destination.?fqdn ?? '', '.') || contains(destination.?fqdn ?? '', '*'))
var validatedRelayDestinations = empty(invalidRelayDestinations) ? connectorRelayDestinations : fail('Every connector relay destination must reference an enabled profile and supply a bare callback FQDN without a scheme, port, path, or wildcard.')
var relayRules = [for (destination, index) in validatedRelayDestinations: {
  name: 'Allow-${destination.profile}-relay-${index}'
  ruleType: 'ApplicationRule'
  protocols: [{ protocolType: 'Https', port: 443 }]
  sourceAddresses: ['10.42.0.0/16']
  targetFqdns: [destination.fqdn]
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
      rules: concat(selectedProfileRules, relayRules)
    }]
  }
}
