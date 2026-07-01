@description('Azure region for data resources.')
param location string

@description('Prefix used to build resource names.')
@minLength(1)
param namePrefix string

@description('Subnet used by App Service VNet integration and allowed to access data resources.')
param appSubnetId string

@description('Subnet allowed to access data resources.')
param dataSubnetId string

@description('Consistency level for Cosmos DB.')
param cosmosDbConsistency string = 'Session'

@description('Tags applied to the data resources.')
param tags object = {}

var sanitizedNamePrefix = replace(namePrefix, '-', '')
var storageAccountName = '${take(sanitizedNamePrefix, 21)}stg'
var cosmosAccountName = '${take(sanitizedNamePrefix, 38)}cosmos'
var cosmosDatabaseName = '${namePrefix}-db'
var cosmosContainerName = 'platformState'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      virtualNetworkRules: [
        {
          id: appSubnetId
          action: 'Allow'
        }
        {
          id: dataSubnetId
          action: 'Allow'
        }
      ]
      ipRules: []
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      keySource: 'Microsoft.Storage'
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: 'default'
  parent: storage
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 14
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 14
    }
  }
}

resource dataContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: 'data'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

resource logsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: 'logs'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

resource backupsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: 'backups'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: false
    enableFreeTier: false
    isVirtualNetworkFilterEnabled: true
    consistencyPolicy: {
      defaultConsistencyLevel: cosmosDbConsistency
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    virtualNetworkRules: [
      {
        id: appSubnetId
        ignoreMissingVNetServiceEndpoint: false
      }
      {
        id: dataSubnetId
        ignoreMissingVNetServiceEndpoint: false
      }
    ]
  }
}

resource cosmosSqlDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  name: cosmosDatabaseName
  parent: cosmos
  properties: {
    resource: {
      id: cosmosDatabaseName
    }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  name: cosmosContainerName
  parent: cosmosSqlDb
  properties: {
    resource: {
      id: cosmosContainerName
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
    }
    options: {
      throughput: 400
    }
  }
}

output storageAccountName string = storage.name
output cosmosAccountName string = cosmos.name
output cosmosEndpoint string = cosmos.properties.documentEndpoint
output cosmosDatabaseName string = cosmosDatabaseName
