param location string
@minLength(1)
param namePrefix string
param environmentName string

// This is the established resource identity; changing it requires an explicit data migration.
var safeName = toLower(replace('${namePrefix}${environmentName}reports', '-', ''))

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  #disable-next-line BCP334
  name: take(safeName, 24)
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Disabled'
    supportsHttpsTrafficOnly: true
  }
}

output storageAccountName string = storage.name
output storageAccountId string = storage.id
