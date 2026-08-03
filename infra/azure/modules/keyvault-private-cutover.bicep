param location string
param namePrefix string
param environmentName string

var vaultName = take(toLower(replace('${namePrefix}-${environmentName}-kv', '-', '')), 24)

// This second-stage update intentionally repeats the vault's declarative settings.
// main.bicep orders it after private endpoint creation and gates it on approval.
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Disabled'
  }
}
