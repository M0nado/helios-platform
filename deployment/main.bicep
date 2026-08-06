targetScope = 'resourceGroup'

@description('Primary deployment location.')
param location string = resourceGroup().location

@description('Short environment label.')
param environmentName string = 'prod'

@description('Base name used for Azure resources.')
param baseName string = 'helios'

@description('Deploy Container Apps control plane resources.')
param deployContainerApps bool = true

@description('Deploy AKS control plane resources.')
param deployAks bool = false

@description('Deploy integration resources such as Service Bus and Logic Apps.')
param deployIntegrations bool = true

@description('Deploy Log Analytics, Application Insights, and Key Vault resources.')
param deployObservability bool = true

@description('Optional Log Analytics workspace name. Leave blank to create one from the base name.')
param logAnalyticsWorkspaceName string = ''

@description('Optional Application Insights component name. Leave blank to create one from the base name.')
param appInsightsName string = ''

@description('Container image for the HELIOS control plane API.')
param controlPlaneImage string = 'ghcr.io/m0nado/helios-platform/control-plane:latest'

@description('Container image for the HubSpot sync worker.')
param hubspotSyncImage string = 'ghcr.io/m0nado/helios-platform/hubspot-sync:latest'

@description('Container image used by the AKS deployment manifest.')
param aksImage string = controlPlaneImage

@description('Slack incoming webhook URL. Required to enable the Azure Monitor to Slack forwarder.')
@secure()
param slackWebhookUrl string = ''

@description('Slack channel label included in alert payloads.')
param slackChannel string = '#helios-alerts'

@description('HubSpot API base URL exposed to runtime workloads.')
param hubspotBaseUrl string = 'https://api.hubapi.com'

@description('HubSpot private app token exposed to the HubSpot sync worker as an ACA secret when provided.')
@secure()
param hubspotToken string = ''

@description('Tags applied to every provisioned resource.')
param tags object = {}

var normalizedBase = toLower(replace('${baseName}-${environmentName}', '_', '-'))
var workspaceName = empty(logAnalyticsWorkspaceName) ? '${take(normalizedBase, 18)}-log' : logAnalyticsWorkspaceName
var insightsComponentName = empty(appInsightsName) ? '${take(normalizedBase, 40)}-appi' : appInsightsName
var keyVaultName = toLower(replace('${take(normalizedBase, 18)}-kv', '--', '-'))
var serviceBusNamespaceName = toLower(replace('${take(normalizedBase, 18)}-sb', '--', '-'))
var actionGroupName = '${normalizedBase}-ag'
var logicAppName = '${normalizedBase}-alerts'
var containerEnvironmentName = '${normalizedBase}-aca-env'
var clusterName = '${normalizedBase}-aks'
var defaultTags = union({
  environment: environmentName
  managedBy: 'github-actions-or-azure-pipelines'
  workload: 'helios-platform'
}, tags)

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = if (deployObservability) {
  name: workspaceName
  location: location
  tags: defaultTags
  properties: {
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = if (deployObservability) {
  name: insightsComponentName
  location: location
  kind: 'web'
  tags: defaultTags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    Flow_Type: 'Bluefield'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = if (deployObservability) {
  name: keyVaultName
  location: location
  tags: defaultTags
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForTemplateDeployment: true
    sku: {
      family: 'A'
      name: 'standard'
    }
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
  }
}

module containerApps 'modules/container-apps.bicep' = if (deployContainerApps && deployObservability) {
  name: 'container-apps-${uniqueString(resourceGroup().id, environmentName)}'
  params: {
    location: location
    environmentName: environmentName
    containerEnvironmentName: containerEnvironmentName
    controlPlaneAppName: '${normalizedBase}-control-api'
    hubspotWorkerAppName: '${normalizedBase}-hubspot-sync'
    logAnalyticsWorkspaceId: logAnalytics!.id
    appInsightsConnectionString: reference(appInsights!.id, '2020-02-02').ConnectionString
    controlPlaneImage: controlPlaneImage
    hubspotSyncImage: hubspotSyncImage
    serviceBusNamespace: serviceBusNamespaceName
    hubspotBaseUrl: hubspotBaseUrl
    hubspotToken: hubspotToken
    tags: defaultTags
  }
}

module aks 'modules/aks.bicep' = if (deployAks && deployObservability) {
  name: 'aks-${uniqueString(resourceGroup().id, environmentName)}'
  params: {
    location: location
    clusterName: clusterName
    logAnalyticsWorkspaceId: logAnalytics!.id
    image: aksImage
    appInsightsConnectionString: reference(appInsights!.id, '2020-02-02').ConnectionString
    serviceBusNamespace: serviceBusNamespaceName
    tags: defaultTags
  }
}

module integrations 'modules/integration-stack.bicep' = if (deployIntegrations) {
  name: 'integrations-${uniqueString(resourceGroup().id, environmentName)}'
  params: {
    location: location
    serviceBusNamespaceName: serviceBusNamespaceName
    logicAppName: logicAppName
    actionGroupName: actionGroupName
    slackWebhookUrl: slackWebhookUrl
    slackChannel: slackChannel
    hubspotBaseUrl: hubspotBaseUrl
    tags: defaultTags
  }
}

output logAnalyticsWorkspaceId string = deployObservability ? logAnalytics.id : ''
output applicationInsightsName string = deployObservability ? appInsights.name : ''
output keyVaultName string = deployObservability ? keyVault.name : ''
output serviceBusNamespace string = deployIntegrations ? integrations!.outputs.serviceBusNamespace : serviceBusNamespaceName
output slackActionGroupId string = deployIntegrations ? integrations!.outputs.actionGroupId : ''
output containerAppsEnvironmentId string = (deployContainerApps && deployObservability) ? containerApps!.outputs.managedEnvironmentId : ''
output controlPlaneUrl string = (deployContainerApps && deployObservability) ? containerApps!.outputs.controlPlaneUrl : ''
output aksClusterName string = (deployAks && deployObservability) ? aks!.outputs.clusterName : ''
