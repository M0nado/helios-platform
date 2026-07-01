param location string
param workloadName string
param environmentName string

// Scaffold module for HELIOS keyvault resources. Replace with production resource definitions after subscription policies are confirmed.
output moduleName string = 'keyvault'
output deploymentLocation string = location
output resourcePrefix string = '${workloadName}-${environmentName}'
